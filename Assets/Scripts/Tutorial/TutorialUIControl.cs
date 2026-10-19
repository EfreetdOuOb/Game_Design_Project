using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TutorialUIControl : MonoBehaviour
{
    public enum TutorialMoment
    {
        MapDisplayed,
        FirstBattleStarted,
        FirstBattleSecondTurnStarted,
        SecondBattleStarted,
        FirstBombPlaced
    }

    [Serializable]
    public class TutorialPageEntry
    {
        public TutorialMoment moment;
        public GameObject pageRoot;
    }

    public static TutorialUIControl Instance { get; private set; }

    [Header("彈出式教學畫面")]
    [Tooltip("每個觸發時機可設定多個畫面；符合的畫面會依陣列順序逐一顯示。")]
    [SerializeField] private TutorialPageEntry[] _tutorialPages;
    [Tooltip("可選的共用繼續按鈕；也可以在各畫面的 Button OnClick 呼叫 ContinueTutorial。")]
    [SerializeField] private Button _continueButton;

    private readonly Queue<GameObject> _pendingPages = new();
    private readonly HashSet<TutorialMoment> _triggeredMoments = new();
    private GameObject _currentPage;
    private BattleBombManager _subscribedBombManager;
    private int _battleCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetInstance()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        HideAllPages();
        SetContinueButtonVisible(false);

        if (_continueButton != null)
            _continueButton.onClick.AddListener(ContinueTutorial);
    }

    private void OnEnable()
    {
        TrySubscribeToBombManager();
    }

    private void Start()
    {
        TrySubscribeToBombManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromBombManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromBombManager();

        if (_continueButton != null)
            _continueButton.onClick.RemoveListener(ContinueTutorial);

        if (Instance == this)
            Instance = null;
    }

    public void NotifyMapDisplayed()
    {
        TriggerMoment(TutorialMoment.MapDisplayed);
    }

    public void NotifyBattleStarted()
    {
        _battleCount++;

        if (_battleCount == 1)
            TriggerMoment(TutorialMoment.FirstBattleStarted);
        else if (_battleCount == 2)
            TriggerMoment(TutorialMoment.SecondBattleStarted);
    }

    public void NotifyPlayerTurnStarted(int turnNumber)
    {
        if (_battleCount == 1 && turnNumber == 2)
            TriggerMoment(TutorialMoment.FirstBattleSecondTurnStarted);
    }

    public void ResetForNewRun()
    {
        _battleCount = 0;
        _triggeredMoments.Clear();
        _pendingPages.Clear();

        if (_currentPage != null)
        {
            _currentPage.SetActive(false);
            _currentPage = null;
        }

        SetContinueButtonVisible(false);
        HideAllPages();
    }

    public void ContinueTutorial()
    {
        SetContinueButtonVisible(false);

        if (_currentPage != null)
        {
            _currentPage.SetActive(false);
            _currentPage = null;
        }

        ShowNextPendingPage();
    }

    private void TriggerMoment(TutorialMoment moment)
    {
        if (!_triggeredMoments.Add(moment) || _tutorialPages == null)
            return;

        for (int i = 0; i < _tutorialPages.Length; i++)
        {
            TutorialPageEntry entry = _tutorialPages[i];
            if (entry != null && entry.moment == moment && entry.pageRoot != null)
                _pendingPages.Enqueue(entry.pageRoot);
        }

        if (_currentPage == null)
            ShowNextPendingPage();
    }

    private void ShowNextPendingPage()
    {
        while (_pendingPages.Count > 0)
        {
            GameObject nextPage = _pendingPages.Dequeue();
            if (nextPage == null)
                continue;

            _currentPage = nextPage;
            _currentPage.SetActive(true);
            SetContinueButtonVisible(true);
            return;
        }
    }

    private void SetContinueButtonVisible(bool visible)
    {
        if (_continueButton != null)
            _continueButton.gameObject.SetActive(visible);
    }

    private void HideAllPages()
    {
        if (_tutorialPages == null)
            return;

        for (int i = 0; i < _tutorialPages.Length; i++)
        {
            TutorialPageEntry entry = _tutorialPages[i];
            if (entry != null && entry.pageRoot != null)
                entry.pageRoot.SetActive(false);
        }
    }

    private void TrySubscribeToBombManager()
    {
        if (_subscribedBombManager != null || BattleBombManager.Instance == null)
            return;

        _subscribedBombManager = BattleBombManager.Instance;
        _subscribedBombManager.OnBombRegistered += HandleBombRegistered;

        if (_subscribedBombManager.ActiveBombCount > 0)
            TriggerMoment(TutorialMoment.FirstBombPlaced);
    }

    private void UnsubscribeFromBombManager()
    {
        if (_subscribedBombManager == null)
            return;

        _subscribedBombManager.OnBombRegistered -= HandleBombRegistered;
        _subscribedBombManager = null;
    }

    private void HandleBombRegistered(int pointId, int remainingTurns)
    {
        TriggerMoment(TutorialMoment.FirstBombPlaced);
    }
}
