using System;
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

    public int CurrentPageIndex { get; private set; }
    public int PageCount => _pages == null ? 0 : _pages.Length;
    public bool IsOpen => _panelRoot != null && _panelRoot.activeSelf;

    public event Action<int> PageChanged;

    private void Awake()
    {
        if (_togglePanelButton != null)
            _togglePanelButton.onClick.AddListener(TogglePanel);

        if (_previousPageButton != null)
            _previousPageButton.onClick.AddListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.AddListener(ShowNextPage);

        ShowPage(0);
        ClosePanel();
    }

    private void OnDestroy()
    {
        if (_togglePanelButton != null)
            _togglePanelButton.onClick.RemoveListener(TogglePanel);

        if (_previousPageButton != null)
            _previousPageButton.onClick.RemoveListener(ShowPreviousPage);

        if (_nextPageButton != null)
            _nextPageButton.onClick.RemoveListener(ShowNextPage);
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
        if (_pages == null || _pages.Length == 0)
        {
            CurrentPageIndex = 0;
            RefreshNavigationButtons();
            return;
        }

        CurrentPageIndex = Mathf.Clamp(pageIndex, 0, _pages.Length - 1);

        for (int i = 0; i < _pages.Length; i++)
        {
            if (_pages[i] != null)
                _pages[i].SetActive(i == CurrentPageIndex);
        }

        RefreshNavigationButtons();
        PageChanged?.Invoke(CurrentPageIndex);
    }

    private void RefreshNavigationButtons()
    {
        if (_previousPageButton != null)
            _previousPageButton.interactable = PageCount > 0 && CurrentPageIndex > 0;

        if (_nextPageButton != null)
            _nextPageButton.interactable = PageCount > 0 && CurrentPageIndex < PageCount - 1;
    }
}