using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialReplayPanel : MonoBehaviour
{
    [Header("面板與頁面")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private GameObject[] _pages;
    [SerializeField] private bool _resetToFirstPageWhenOpened = true;

    [Header("按鈕")]
    [SerializeField] private Button _togglePanelButton;
    [SerializeField] private Button _previousPageButton;
    [SerializeField] private Button _nextPageButton;

    [Header("新內容提示")]
    public GameObject FirstOpenIndicator;
    [SerializeField] private string _notificationKey = "TutorialReplayPanel";
    [SerializeField, Min(0f)] private float _blinkFrequency = 1.5f;

    private const float BlinkIntervalSeconds = 0.03f;
    private readonly HashSet<string> _registeredPageIds = new HashSet<string>();
    private Coroutine _blinkCoroutine;
    private CanvasGroup _indicatorCanvasGroup;
    private bool _hasUnreadContent;
    private int _initialPageCount;

    public int CurrentPageIndex { get; private set; }
    public int PageCount => _pages == null ? 0 : _pages.Length;
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    public event Action<int> PageChanged;
    public event Action<string, int> PageRegistered;

    private void Awake()
    {
        _initialPageCount = PageCount;

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

        if (_previousPageButton != null)
            _previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.AddListener(ShowNextPage);

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

        if (_previousPageButton != null)
            _previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.RemoveListener(ShowNextPage);

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
            ShowPage(0);

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

    public bool RegisterPage(string pageId, GameObject page, bool openImmediately = false)
    {
        return RegisterPage(pageId, page, openImmediately, true);
    }

    public bool RegisterPage(string pageId, GameObject page, bool openImmediately, bool showUnreadNotification)
    {
        if (string.IsNullOrWhiteSpace(pageId) || page == null)
            return false;

        if ((_pages != null && Array.IndexOf(_pages, page) >= 0) || _registeredPageIds.Contains(pageId))
            return false;

        _registeredPageIds.Add(pageId);
        int pageIndex = PageCount;
        Array.Resize(ref _pages, pageIndex + 1);
        _pages[pageIndex] = page;
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
        for (int i = _initialPageCount; i < PageCount; i++)
        {
            if (_pages[i] != null)
                _pages[i].SetActive(false);
        }

        Array.Resize(ref _pages, _initialPageCount);
        _registeredPageIds.Clear();

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
        return _pages[pageIndex];
    }

    private void RefreshNavigationButtons()
    {
        if (_previousPageButton != null)
            _previousPageButton.interactable = PageCount > 0 && CurrentPageIndex > 0;

        if (_nextPageButton != null)
            _nextPageButton.interactable = PageCount > 0 && CurrentPageIndex < PageCount - 1;
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