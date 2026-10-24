using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TutorialReplayPanel : MonoBehaviour
{
    [Serializable]
    public class TutorialBigPage
    {
        [Tooltip("提供給解鎖器辨識此大頁的 ID，例如 bomb。")]
        public string pageId;
        [Tooltip("未設定解鎖器名稱時，解鎖後顯示的名稱。")]
        public string displayName;
        [Tooltip("尚未解鎖時，目錄和書籤按鈕顯示的文字。")]
        public string lockedDisplayName = "???";
        public GameObject bigPage;
        public GameObject[] smallPages;
        public Button contentsButton;
        public Button bookmarkButton;
    }

    private struct ButtonListener
    {
        public Button Button;
        public UnityAction Listener;
    }

    [Header("面板與頁面")]
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("目錄畫面；設定後，開啟教學書會先顯示目錄。")]
    [SerializeField] private GameObject _contentsRoot;
    [Tooltip("每個項目設定一個大頁、小頁，以及對應的目錄和書籤按鈕。")]
    [SerializeField] private TutorialBigPage[] _bigPages;
    [SerializeField, HideInInspector] private GameObject[] _pages;
    [SerializeField] private bool _resetToFirstPageWhenOpened = true;

    [Header("按鈕")]
    [SerializeField] private Button _togglePanelButton;
    [Tooltip("教學書內的關閉按鈕。")]
    [SerializeField] private Button _closePanelButton;
    [SerializeField] private Button _previousPageButton;
    [SerializeField] private Button _nextPageButton;
    [Tooltip("返回目錄按鈕；按鈕位置可在 UI 中放到左下角。")]
    [SerializeField] private Button _returnToContentsButton;

    [Header("新內容提示")]
    public GameObject FirstOpenIndicator;
    [SerializeField] private string _notificationKey = "TutorialReplayPanel";
    [SerializeField, Min(0f)] private float _blinkFrequency = 1.5f;

    private const float BlinkIntervalSeconds = 0.03f;
    private readonly HashSet<string> _registeredPageIds = new HashSet<string>();
    private readonly Dictionary<GameObject, int> _pageSequenceIndexes = new Dictionary<GameObject, int>();
    private readonly Dictionary<int, string> _pageBigPageIds = new Dictionary<int, string>();
    private readonly HashSet<string> _lockedBigPageIds = new HashSet<string>();
    private readonly Dictionary<Button, TMP_Text> _tmpButtonLabels = new Dictionary<Button, TMP_Text>();
    private readonly Dictionary<Button, Text> _legacyButtonLabels = new Dictionary<Button, Text>();
    private readonly List<GameObject> _pageSequence = new List<GameObject>();
    private readonly List<GameObject> _registeredPages = new List<GameObject>();
    private readonly List<int> _bigPageStartIndexes = new List<int>();
    private readonly List<ButtonListener> _navigationButtonListeners = new List<ButtonListener>();
    private Coroutine _blinkCoroutine;
    private CanvasGroup _indicatorCanvasGroup;
    private bool _hasUnreadContent;
    private bool _isShowingContents;

    public int CurrentPageIndex { get; private set; }
    public int PageCount => _pageSequence.Count;
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    public event Action<int> PageChanged;
    public event Action<string, int> PageRegistered;

    private void Awake()
    {
        RebuildPageSequence();

        if (FirstOpenIndicator != null)
        {
            _indicatorCanvasGroup = FirstOpenIndicator.GetComponent<CanvasGroup>();
            if (_indicatorCanvasGroup == null)
                _indicatorCanvasGroup = FirstOpenIndicator.AddComponent<CanvasGroup>();

            _indicatorCanvasGroup.blocksRaycasts = false;
            _indicatorCanvasGroup.interactable = false;
        }

        _hasUnreadContent = PlayerPrefs.GetInt(GetNotificationPreferenceKey(), 1) == 1;

        if (_togglePanelButton != null)
            _togglePanelButton.onClick.AddListener(TogglePanel);

        if (_closePanelButton != null)
            _closePanelButton.onClick.AddListener(ClosePanel);

        if (_previousPageButton != null)
            _previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.AddListener(ShowNextPage);

        if (_returnToContentsButton != null)
            _returnToContentsButton.onClick.AddListener(ShowContents);

        BindBigPageButtons();

        if (_contentsRoot != null)
            ShowContents();
        else
            ShowPage(0);

        ClosePanel();
        RefreshIndicator();
    }

    private void OnEnable()
    {
        if (_hasUnreadContent)
            StartBlinking();
    }

    private void OnDestroy()
    {
        if (_togglePanelButton != null)
            _togglePanelButton.onClick.RemoveListener(TogglePanel);

        if (_closePanelButton != null)
            _closePanelButton.onClick.RemoveListener(ClosePanel);

        if (_previousPageButton != null)
            _previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.RemoveListener(ShowNextPage);

        if (_returnToContentsButton != null)
            _returnToContentsButton.onClick.RemoveListener(ShowContents);

        for (int i = 0; i < _navigationButtonListeners.Count; i++)
        {
            ButtonListener binding = _navigationButtonListeners[i];
            if (binding.Button != null)
                binding.Button.onClick.RemoveListener(binding.Listener);
        }

        StopBlinking();
    }

    private void OnDisable()
    {
        StopBlinking();
    }

    public void TogglePanel()
    {
        if (IsOpen)
            ClosePanel();
        else
            OpenPanel();
    }

    public void OpenPanel()
    {
        if (_panelRoot == null)
            return;

        SetUnreadContent(false);

        if (_resetToFirstPageWhenOpened)
        {
            if (_contentsRoot != null)
                ShowContents();
            else
                ShowPage(0);
        }

        _panelRoot.SetActive(true);
        RefreshNavigationButtons();
    }

    public void ClosePanel()
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(false);
    }

    public void ShowPreviousPage()
    {
        ShowPage(CurrentPageIndex - 1);
    }

    public void ShowNextPage()
    {
        ShowPage(CurrentPageIndex + 1);
    }

    public void ShowPage(int pageIndex)
    {
        if (PageCount == 0)
        {
            CurrentPageIndex = 0;
            RefreshNavigationButtons();
            return;
        }

        if (_pageBigPageIds.TryGetValue(pageIndex, out string pageId) && _lockedBigPageIds.Contains(pageId))
            return;

        _isShowingContents = false;
        if (_contentsRoot != null)
            _contentsRoot.SetActive(false);

        CurrentPageIndex = Mathf.Clamp(pageIndex, 0, PageCount - 1);

        for (int i = 0; i < PageCount; i++)
        {
            GameObject page = GetPageAt(i);
            if (page != null)
                page.SetActive(i == CurrentPageIndex);
        }

        RefreshNavigationButtons();
        PageChanged?.Invoke(CurrentPageIndex);
    }

    public void ShowBigPage(int bigPageIndex)
    {
        if (bigPageIndex < 0 || bigPageIndex >= _bigPageStartIndexes.Count)
            return;

        TutorialBigPage bigPage = _bigPages[bigPageIndex];
        if (bigPage != null && _lockedBigPageIds.Contains(bigPage.pageId))
            return;

        int pageIndex = _bigPageStartIndexes[bigPageIndex];
        if (pageIndex >= 0)
            ShowPage(pageIndex);
    }

    public bool SetBigPageUnlocked(string pageId, bool unlocked, bool showUnreadNotification = false)
    {
        int bigPageIndex = FindBigPageIndex(pageId);
        if (bigPageIndex < 0)
            return false;

        if (unlocked)
            _lockedBigPageIds.Remove(pageId);
        else
            _lockedBigPageIds.Add(pageId);

        RefreshBigPageButton(bigPageIndex);
        if (unlocked && showUnreadNotification)
            SetUnreadContent(true);

        return true;
    }

    public void ShowContents()
    {
        if (_contentsRoot == null)
            return;

        _isShowingContents = true;
        for (int i = 0; i < PageCount; i++)
        {
            GameObject page = GetPageAt(i);
            if (page != null)
                page.SetActive(false);
        }

        _contentsRoot.SetActive(true);
        RefreshNavigationButtons();
    }

    public bool RegisterPage(string pageId, GameObject page, bool openImmediately = false)
    {
        return RegisterPage(pageId, page, openImmediately, true);
    }

    public bool RegisterPage(string pageId, GameObject page, bool openImmediately, bool showUnreadNotification)
    {
        if (string.IsNullOrWhiteSpace(pageId) || page == null)
            return false;

        if (_pageSequenceIndexes.ContainsKey(page) || _registeredPageIds.Contains(pageId))
            return false;

        _registeredPageIds.Add(pageId);
        _registeredPages.Add(page);
        RebuildPageSequence();
        int pageIndex = _pageSequenceIndexes[page];
        page.SetActive(false);
        RefreshNavigationButtons();
        if (showUnreadNotification)
            SetUnreadContent(true);
        PageRegistered?.Invoke(pageId, pageIndex);

        if (openImmediately)
        {
            OpenPanel();
            ShowPage(pageIndex);
        }

        return true;
    }

    public void ResetDiscoveredPagesForTesting()
    {
        for (int i = 0; i < _registeredPages.Count; i++)
            if (_registeredPages[i] != null)
                _registeredPages[i].SetActive(false);

        _registeredPages.Clear();
        _registeredPageIds.Clear();
        RebuildPageSequence();

        if (PageCount > 0)
            ShowPage(Mathf.Clamp(CurrentPageIndex, 0, PageCount - 1));
        else
        {
            CurrentPageIndex = 0;
            RefreshNavigationButtons();
        }

        SetUnreadContent(true);
        PlayerPrefs.Save();
    }

    private GameObject GetPageAt(int pageIndex)
    {
        return _pageSequence[pageIndex];
    }

    private void RebuildPageSequence()
    {
        _pageSequence.Clear();
        _pageSequenceIndexes.Clear();
        _pageBigPageIds.Clear();
        _bigPageStartIndexes.Clear();
        bool hasConfiguredBigPages = false;

        if (_bigPages != null)
        {
            for (int i = 0; i < _bigPages.Length; i++)
            {
                TutorialBigPage bigPage = _bigPages[i];
                _bigPageStartIndexes.Add(-1);

                if (bigPage == null)
                    continue;

                if (bigPage.bigPage != null)
                {
                    _bigPageStartIndexes[i] = AddBigPageToSequence(bigPage.bigPage, bigPage.pageId);
                    hasConfiguredBigPages = true;
                }

                if (bigPage.smallPages == null)
                    continue;

                for (int j = 0; j < bigPage.smallPages.Length; j++)
                {
                    GameObject smallPage = bigPage.smallPages[j];
                    if (smallPage == null)
                        continue;

                    if (_bigPageStartIndexes[i] < 0)
                        _bigPageStartIndexes[i] = AddBigPageToSequence(smallPage, bigPage.pageId);
                    else
                        AddBigPageToSequence(smallPage, bigPage.pageId);
                    hasConfiguredBigPages = true;
                }
            }
        }

        if (!hasConfiguredBigPages && _pages != null)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                GameObject page = _pages[i];
                if (page == null)
                    _pageSequence.Add(null);
                else
                    AddPageToSequence(page);
            }
        }

        for (int i = 0; i < _registeredPages.Count; i++)
            if (_registeredPages[i] != null)
                AddPageToSequence(_registeredPages[i]);
    }

    private int AddPageToSequence(GameObject page)
    {
        if (_pageSequenceIndexes.TryGetValue(page, out int existingIndex))
            return existingIndex;

        int pageIndex = _pageSequence.Count;
        _pageSequence.Add(page);
        _pageSequenceIndexes.Add(page, pageIndex);
        return pageIndex;
    }

    private int AddBigPageToSequence(GameObject page, string pageId)
    {
        int pageIndex = AddPageToSequence(page);
        if (!string.IsNullOrWhiteSpace(pageId))
            _pageBigPageIds[pageIndex] = pageId;

        return pageIndex;
    }

    private int FindBigPageIndex(string pageId)
    {
        if (string.IsNullOrWhiteSpace(pageId) || _bigPages == null)
            return -1;

        for (int i = 0; i < _bigPages.Length; i++)
            if (_bigPages[i] != null && _bigPages[i].pageId == pageId)
                return i;

        return -1;
    }

    private void BindBigPageButtons()
    {
        if (_bigPages == null)
            return;

        for (int i = 0; i < _bigPages.Length; i++)
        {
            TutorialBigPage bigPage = _bigPages[i];
            if (bigPage == null || _bigPageStartIndexes[i] < 0)
                continue;

            int index = i;
            // Buttons are wired here so each chapter's serialized button needs no separate OnClick setup.
            BindNavigationButton(bigPage.contentsButton, () => ShowBigPage(index));
            BindNavigationButton(bigPage.bookmarkButton, () => ShowBigPage(index));
            RefreshBigPageButton(i);
        }
    }

    private void RefreshBigPageButton(int bigPageIndex)
    {
        TutorialBigPage bigPage = _bigPages[bigPageIndex];
        bool isUnlocked = !_lockedBigPageIds.Contains(bigPage.pageId);
        string label = isUnlocked
            ? bigPage.displayName
            : (string.IsNullOrWhiteSpace(bigPage.lockedDisplayName) ? "???" : bigPage.lockedDisplayName);

        RefreshBigPageButton(bigPage.contentsButton, label, isUnlocked);
        RefreshBigPageButton(bigPage.bookmarkButton, label, isUnlocked);
    }

    private void RefreshBigPageButton(Button button, string label, bool isUnlocked)
    {
        if (button == null)
            return;

        button.interactable = isUnlocked;

        if (string.IsNullOrEmpty(label))
            return;

        _tmpButtonLabels.TryGetValue(button, out TMP_Text tmpText);
        _legacyButtonLabels.TryGetValue(button, out Text legacyText);

        if (tmpText == null && legacyText == null)
        {
            tmpText = button.GetComponentInChildren<TMP_Text>(true);
            if (tmpText != null)
                _tmpButtonLabels.Add(button, tmpText);
            else
            {
                legacyText = button.GetComponentInChildren<Text>(true);
                _legacyButtonLabels.Add(button, legacyText);
            }
        }

        if (tmpText != null)
            tmpText.text = label;
        else if (legacyText != null)
            legacyText.text = label;
    }

    private void BindNavigationButton(Button button, UnityAction listener)
    {
        if (button == null)
            return;

        button.onClick.AddListener(listener);
        _navigationButtonListeners.Add(new ButtonListener { Button = button, Listener = listener });
    }

    private void RefreshNavigationButtons()
    {
        if (_previousPageButton != null)
            _previousPageButton.interactable = !_isShowingContents && PageCount > 0 && CurrentPageIndex > 0;

        if (_nextPageButton != null)
            _nextPageButton.interactable = !_isShowingContents && PageCount > 0 && CurrentPageIndex < PageCount - 1;

        if (_returnToContentsButton != null)
            _returnToContentsButton.interactable = _contentsRoot != null && !_isShowingContents;
    }

    private void SetUnreadContent(bool unread)
    {
        _hasUnreadContent = unread;
        PlayerPrefs.SetInt(GetNotificationPreferenceKey(), unread ? 1 : 0);
        RefreshIndicator();
    }

    private string GetNotificationPreferenceKey()
    {
        string key = string.IsNullOrWhiteSpace(_notificationKey) ? name : _notificationKey;
        return "TutorialReplayPanel." + key + ".Unread";
    }

    private void RefreshIndicator()
    {
        if (FirstOpenIndicator == null)
            return;

        FirstOpenIndicator.SetActive(_hasUnreadContent);
        if (_hasUnreadContent)
            StartBlinking();
        else
            StopBlinking();
    }

    private void StartBlinking()
    {
        if (FirstOpenIndicator == null || _indicatorCanvasGroup == null || _blinkCoroutine != null)
            return;

        FirstOpenIndicator.SetActive(true);
        if (_blinkFrequency <= 0f)
        {
            _indicatorCanvasGroup.alpha = 1f;
            return;
        }

        _blinkCoroutine = StartCoroutine(BlinkIndicator());
    }

    private void StopBlinking()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        if (_indicatorCanvasGroup != null)
            _indicatorCanvasGroup.alpha = 1f;
    }

    private IEnumerator BlinkIndicator()
    {
        while (_hasUnreadContent && FirstOpenIndicator != null)
        {
            float phase = (Mathf.Sin(Time.unscaledTime * _blinkFrequency * Mathf.PI * 2f) + 1f) * 0.5f;
            _indicatorCanvasGroup.alpha = Mathf.Lerp(0.2f, 1f, phase);
            yield return new WaitForSecondsRealtime(BlinkIntervalSeconds);
        }

        _blinkCoroutine = null;
    }
}