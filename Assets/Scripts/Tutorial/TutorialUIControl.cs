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

    private struct PendingPage
    {
        public GameObject page;
        public int index;
        public int total;
    }

    public static TutorialUIControl Instance { get; private set; }

    [Header("彈出式教學畫面")]
    [Tooltip("每個觸發時機可設定多個畫面；符合的畫面會依陣列順序逐一顯示。")]
    [SerializeField] private TutorialPageEntry[] _tutorialPages;
    [Tooltip("可選的共用繼續按鈕；也可以在各畫面的 Button OnClick 呼叫 ContinueTutorial。")]
    [SerializeField] private Button _continueButton;

    [Header("顯示設定")]
    [Tooltip("彈出教學的最外層容器。每次顯示畫面時會把它移到 Canvas 最上層，避免被其他面板蓋住；留空則使用繼續按鈕的父物件。")]
    [SerializeField] private Transform _popupRoot;
    [Tooltip("畫面出現後，這段時間內忽略「繼續」點擊，避免玩家剛好連點而不小心跳過教學。")]
    [SerializeField, Min(0f)] private float _continueInputDelay = 0.35f;

    private readonly Queue<PendingPage> _pendingPages = new();
    private readonly HashSet<TutorialMoment> _triggeredMoments = new();
    private GameObject _currentPage;
    private BattleBombManager _subscribedBombManager;
    private int _battleCount;
    private float _pageShownTime;
    private bool _holdUntilPlayerTurn;

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
        // 回到玩家回合了：先前在敵人回合中被延後的教學（例如炸彈）可以放行
        _holdUntilPlayerTurn = false;

        if (_battleCount == 1 && turnNumber == 2)
            TriggerMoment(TutorialMoment.FirstBattleSecondTurnStarted);

        if (_currentPage == null)
            ShowNextPendingPage();
    }

    public void ResetForNewRun()
    {
        _battleCount = 0;
        _holdUntilPlayerTurn = false;
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
        // 剛彈出的前一小段時間忽略點擊，避免玩家原本在點的那下（或連點）直接把教學跳掉
        if (_currentPage != null && Time.unscaledTime - _pageShownTime < _continueInputDelay)
            return;

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

        int total = CountPagesForMoment(moment);
        int index = 0;

        for (int i = 0; i < _tutorialPages.Length; i++)
        {
            TutorialPageEntry entry = _tutorialPages[i];
            if (entry == null || entry.moment != moment || entry.pageRoot == null)
                continue;

            index++;
            _pendingPages.Enqueue(new PendingPage { page = entry.pageRoot, index = index, total = total });
        }

        if (ShouldHoldUntilPlayerTurn(moment))
        {
            _holdUntilPlayerTurn = true;
            return;
        }

        if (_currentPage == null && !_holdUntilPlayerTurn)
            ShowNextPendingPage();
    }

    private int CountPagesForMoment(TutorialMoment moment)
    {
        int count = 0;
        for (int i = 0; i < _tutorialPages.Length; i++)
        {
            TutorialPageEntry entry = _tutorialPages[i];
            if (entry != null && entry.moment == moment && entry.pageRoot != null)
                count++;
        }

        return count;
    }

    // 炸彈是敵人在「敵人回合中」放下的。若這時馬上彈出教學，會蓋住敵人的行動與炸彈出現的動畫，
    // 所以等回到玩家回合再彈，玩家才看得到炸彈出現在法鎮上，教學文字的說明也才對得上畫面。
    private bool ShouldHoldUntilPlayerTurn(TutorialMoment moment)
    {
        return moment == TutorialMoment.FirstBombPlaced
            && TurnManager.Instance != null
            && TurnManager.Instance.CurrentPhase != TurnPhase.PlayerTurn;
    }

    private void ShowNextPendingPage()
    {
        // 還在等回到玩家回合的教學，不要被「關掉上一頁」的流程提早放出來
        if (_holdUntilPlayerTurn)
            return;

        while (_pendingPages.Count > 0)
        {
            PendingPage next = _pendingPages.Dequeue();
            if (next.page == null)
                continue;

            BringPopupToFront();

            _currentPage = next.page;
            _currentPage.SetActive(true);

            TutorialPageContent content = _currentPage.GetComponent<TutorialPageContent>();
            if (content != null)
                content.SetProgress(next.index, next.total);

            SetContinueButtonVisible(true);
            _pageShownTime = Time.unscaledTime;
            return;
        }
    }

    // 其他面板（事件、休息、勝利…）在 Canvas 裡排在教學容器後面，會蓋住教學；每次顯示前先拉到最上層
    private void BringPopupToFront()
    {
        Transform root = _popupRoot != null
            ? _popupRoot
            : (_continueButton != null ? _continueButton.transform.parent : null);

        if (root != null)
            root.SetAsLastSibling();

        // 「點擊任意處繼續」的全螢幕按鈕要在各頁面之上，才接得到點擊
        if (_continueButton != null)
            _continueButton.transform.SetAsLastSibling();
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
