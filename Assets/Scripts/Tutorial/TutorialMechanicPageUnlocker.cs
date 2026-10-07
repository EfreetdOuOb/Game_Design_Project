using System;
using UnityEngine;

public class TutorialMechanicPageUnlocker : MonoBehaviour
{
    private static readonly System.Collections.Generic.HashSet<string> ResetBooksThisSession = new System.Collections.Generic.HashSet<string>();

    [Serializable]
    public class TutorialPageEntry
    {
        public string pageId;
        public GameObject pageRoot;
    }

    [Header("教學書")]
    [SerializeField] private TutorialReplayPanel _tutorialPanel;
    [SerializeField] private TutorialPageEntry[] _pages;
    [SerializeField] private string _saveKey = "MainTutorialBook";
    [SerializeField] private bool _resetRecordsOnStartupForTesting;

    [Header("炸彈首次出現")]
    [SerializeField] private string _bombPageId = "bomb";

    private BattleBombManager _subscribedBombManager;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetSessionTracking()
    {
        ResetBooksThisSession.Clear();
    }

    private void OnEnable()
    {
        TrySubscribeToBombManager();
    }

    private void Start()
    {
        ResetRecordsIfTestingEnabled();
        RegisterPreviouslyUnlockedPages();
        TrySubscribeToBombManager();
    }

    private void OnDisable()
    {
        if (_subscribedBombManager != null)
            _subscribedBombManager.OnBombRegistered -= HandleBombRegistered;

        _subscribedBombManager = null;
    }

    public bool UnlockPage(string pageId)
    {
        if (_tutorialPanel == null || !TryGetPage(pageId, out GameObject page))
            return false;

        string preferenceKey = GetPreferenceKey(pageId);
        if (PlayerPrefs.GetInt(preferenceKey, 0) == 1)
        {
            _tutorialPanel.RegisterPage(pageId, page, false, false);
            return false;
        }

        if (!_tutorialPanel.RegisterPage(pageId, page))
            return false;

        PlayerPrefs.SetInt(preferenceKey, 1);
        PlayerPrefs.Save();
        return true;
    }

    private void TrySubscribeToBombManager()
    {
        if (_subscribedBombManager != null || BattleBombManager.Instance == null)
            return;

        _subscribedBombManager = BattleBombManager.Instance;
        _subscribedBombManager.OnBombRegistered += HandleBombRegistered;
    }

    private void HandleBombRegistered(int pointId, int remainingTurns)
    {
        UnlockPage(_bombPageId);
    }

    private void RegisterPreviouslyUnlockedPages()
    {
        if (_tutorialPanel == null || _pages == null)
            return;

        for (int i = 0; i < _pages.Length; i++)
        {
            TutorialPageEntry entry = _pages[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.pageId) || entry.pageRoot == null)
                continue;

            if (PlayerPrefs.GetInt(GetPreferenceKey(entry.pageId), 0) == 1)
                _tutorialPanel.RegisterPage(entry.pageId, entry.pageRoot, false, false);
        }
    }

    private void ResetRecordsIfTestingEnabled()
    {
        if (!_resetRecordsOnStartupForTesting || !ResetBooksThisSession.Add(GetBookKey()))
            return;

        if (_pages != null)
        {
            for (int i = 0; i < _pages.Length; i++)
            {
                TutorialPageEntry entry = _pages[i];
                if (entry != null && !string.IsNullOrWhiteSpace(entry.pageId))
                    PlayerPrefs.DeleteKey(GetPreferenceKey(entry.pageId));
            }
        }

        if (_tutorialPanel != null)
            _tutorialPanel.ResetDiscoveredPagesForTesting();
        else
            PlayerPrefs.Save();
    }

    private bool TryGetPage(string pageId, out GameObject page)
    {
        page = null;
        if (string.IsNullOrWhiteSpace(pageId) || _pages == null)
            return false;

        for (int i = 0; i < _pages.Length; i++)
        {
            TutorialPageEntry entry = _pages[i];
            if (entry == null || entry.pageId != pageId || entry.pageRoot == null)
                continue;

            page = entry.pageRoot;
            return true;
        }

        return false;
    }

    private string GetPreferenceKey(string pageId)
    {
        return "TutorialPageUnlocked." + GetBookKey() + "." + pageId;
    }

    private string GetBookKey()
    {
        return string.IsNullOrWhiteSpace(_saveKey) ? name : _saveKey;
    }
}