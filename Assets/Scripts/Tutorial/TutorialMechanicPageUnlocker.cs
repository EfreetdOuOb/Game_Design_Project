using System.Collections.Generic;
using UnityEngine;

public class TutorialMechanicPageUnlocker : MonoBehaviour
{
    private const string BombPageId = "bomb";
    private static readonly HashSet<string> ResetBooksThisSession = new HashSet<string>();

    [Header("教學書")]
    [SerializeField] private TutorialReplayPanel _tutorialPanel;
    [Tooltip("填入 TutorialReplayPanel 大頁設定的 pageId。")]
    [SerializeField] private string[] _unlockablePageIds;
    [SerializeField] private string _saveKey = "MainTutorialBook";
    [SerializeField] private bool _resetRecordsOnStartupForTesting;

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
        ApplySavedUnlockStates();
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
        if (_tutorialPanel == null || string.IsNullOrWhiteSpace(pageId))
            return false;

        string preferenceKey = GetPreferenceKey(pageId);
        bool alreadyUnlocked = PlayerPrefs.GetInt(preferenceKey, 0) == 1;
        if (!SetBigPageUnlocked(pageId, true, !alreadyUnlocked))
            return false;

        if (alreadyUnlocked)
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
        UnlockPage(BombPageId);
    }

    private void ApplySavedUnlockStates()
    {
        if (_tutorialPanel == null || _unlockablePageIds == null)
            return;

        for (int i = 0; i < _unlockablePageIds.Length; i++)
        {
            string pageId = _unlockablePageIds[i];
            if (string.IsNullOrWhiteSpace(pageId))
                continue;

            bool isUnlocked = PlayerPrefs.GetInt(GetPreferenceKey(pageId), 0) == 1;
            SetBigPageUnlocked(pageId, isUnlocked);
        }
    }

    private void ResetRecordsIfTestingEnabled()
    {
        if (!_resetRecordsOnStartupForTesting || !ResetBooksThisSession.Add(GetBookKey()))
            return;

        if (_unlockablePageIds != null)
        {
            for (int i = 0; i < _unlockablePageIds.Length; i++)
            {
                string pageId = _unlockablePageIds[i];
                if (!string.IsNullOrWhiteSpace(pageId))
                    PlayerPrefs.DeleteKey(GetPreferenceKey(pageId));
            }
        }

        if (_tutorialPanel != null)
            _tutorialPanel.ResetDiscoveredPagesForTesting();
        else
            PlayerPrefs.Save();
    }

    private bool SetBigPageUnlocked(string pageId, bool unlocked, bool showUnreadNotification = false)
    {
        if (_tutorialPanel == null)
            return false;

        bool updated = _tutorialPanel.SetBigPageUnlocked(pageId, unlocked, showUnreadNotification);
        if (!updated)
            Debug.LogWarning($"Tutorial page '{pageId}' is not configured in TutorialReplayPanel big pages.", this);

        return updated;
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