using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleBombManager : MonoBehaviour
{
    public static BattleBombManager Instance { get; private set; }

    private class BombState
    {
        public int pointId;
        public int remainingTurns;
        public bool touchedThisTurn;
    }

    [Header("炸彈數量上限")]
    [Tooltip("場上同時最多允許存在的炸彈數量")]
    [SerializeField] private int maxActiveBombs = 3;

    [Header("引爆傷害設定")]
    [SerializeField] private int damageOnTimerDetonate = 15;
    [SerializeField] private int damagePerBombOnPoiseBreak = 20;

    [Header("延緩設定")]
    [Tooltip("玩家碰到炸彈時，額外增加的回合數（在本回合正常倒數之前套用）")]
    [SerializeField] private int delayAmountOnTouch = 2;

    [SerializeField] private CombatActor playerActor;

    private readonly Dictionary<int, BombState> activeBombs = new Dictionary<int, BombState>();
    private bool subscribedToTurnManager;

    public event Action<int> OnBombDetonatedByTimer;
    public event Action<int, int, CombatActor> OnBombsConsumedForDamage; // bombCount, totalDamage, targetEnemy
    public event Action<int, int> OnBombRegistered;   // pointId, remainingTurns
    public event Action<int, int> OnBombTurnsChanged; // pointId, remainingTurns
    public event Action OnAllBombsCleared;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        SubscribeToTurnManager();
    }

    private void OnDisable()
    {
        UnsubscribeFromTurnManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTurnManager();

        if (Instance == this)
            Instance = null;
    }

    private void SubscribeToTurnManager()
    {
        if (subscribedToTurnManager)
            return;

        if (TurnManager.Instance == null)
        {
            Debug.LogWarning("[炸彈] 找不到 TurnManager，無法訂閱 OnTurnCleanup，炸彈將不會倒數！");
            return;
        }

        TurnManager.Instance.OnTurnCleanup += ResolveTurn;
        subscribedToTurnManager = true;
        Debug.Log("[炸彈] 已成功訂閱 TurnManager.OnTurnCleanup");
    }

    private void UnsubscribeFromTurnManager()
    {
        if (!subscribedToTurnManager || TurnManager.Instance == null)
            return;

        TurnManager.Instance.OnTurnCleanup -= ResolveTurn;
        subscribedToTurnManager = false;
    }

    // 外部（例如 EnemyCombatAI）在放置炸彈前，先呼叫這個確認是否還有名額
    public bool CanRegisterMoreBombs()
    {
        return activeBombs.Count < maxActiveBombs;
    }

    public int MaxActiveBombs => maxActiveBombs;

    public bool RegisterBomb(int pointId, int initialTurns)
    {
        if (activeBombs.ContainsKey(pointId))
        {
            Debug.LogWarning($"[炸彈] pointId={pointId} 已存在炸彈，略過重複註冊");
            return false;
        }

        if (!CanRegisterMoreBombs())
        {
            Debug.Log($"[炸彈] 場上已達炸彈上限（{activeBombs.Count}/{maxActiveBombs}），無法再放置");
            return false;
        }

        activeBombs[pointId] = new BombState
        {
            pointId = pointId,
            remainingTurns = Mathf.Max(1, initialTurns),
            touchedThisTurn = false
        };

        Debug.Log($"[炸彈] 註冊炸彈 pointId={pointId}，倒數={initialTurns}（目前 {activeBombs.Count}/{maxActiveBombs}）");
        OnBombRegistered?.Invoke(pointId, activeBombs[pointId].remainingTurns);
        return true;
    }

    public void NotifyPointTouched(int pointId)
    {
        if (activeBombs.TryGetValue(pointId, out BombState bomb))
        {
            bomb.touchedThisTurn = true;
            Debug.Log($"[炸彈] pointId={pointId} 本回合被碰到，倒數將延緩");
        }
    }

    public void ClearAllBombs()
    {
        int clearedCount = activeBombs.Count;
        activeBombs.Clear();
        Debug.Log($"[炸彈] 戰鬥重置，清空 {clearedCount} 顆炸彈記錄");

        if (clearedCount > 0)
            OnAllBombsCleared?.Invoke();
    }

    private void ResolveTurn()
    {
        Debug.Log($"[炸彈] ResolveTurn 被呼叫，目前場上炸彈數={activeBombs.Count}");

        List<BombState> detonatedThisResolve = new List<BombState>();

        foreach (BombState bomb in activeBombs.Values.ToList())
        {
            if (bomb.touchedThisTurn)
            {
                // 第一步：先套用延緩加成
                bomb.remainingTurns += delayAmountOnTouch;
                bomb.touchedThisTurn = false;
                Debug.Log($"[炸彈] pointId={bomb.pointId} 被延緩 +{delayAmountOnTouch}，暫時剩餘回合={bomb.remainingTurns}");
            }

            // 第二步：最後才扣本回合的正常倒數（無論本回合是否被摸過都要扣）
            bomb.remainingTurns--;
            Debug.Log($"[炸彈] pointId={bomb.pointId} 倒數，最終剩餘回合={bomb.remainingTurns}");

            if (bomb.remainingTurns <= 0)
            {
                detonatedThisResolve.Add(bomb);
            }
            else
            {
                OnBombTurnsChanged?.Invoke(bomb.pointId, bomb.remainingTurns);
            }
        }

        foreach (BombState bomb in detonatedThisResolve)
        {
            DetonateByTimer(bomb);
        }
    }

    private void DetonateByTimer(BombState bomb)
    {
        activeBombs.Remove(bomb.pointId);

        Debug.Log($"[炸彈] pointId={bomb.pointId} 倒數結束，引爆！");
        CombatUI.Instance?.AppendBattleLog($"炸彈引爆，造成 {damageOnTimerDetonate} 傷害");

        if (playerActor != null)
        {
            playerActor.ReceiveDamage(damageOnTimerDetonate);
        }

        OnBombDetonatedByTimer?.Invoke(bomb.pointId);
    }

    public void ConsumeAllBombsForDamage(CombatActor targetEnemy)
    {
        if (activeBombs.Count == 0)
        {
            Debug.Log("[炸彈] 敵人暈眩，但場上沒有未爆炸彈可利用");
            return;
        }

        int bombCount = activeBombs.Count;
        int totalDamage = bombCount * damagePerBombOnPoiseBreak;

        activeBombs.Clear();

        if (targetEnemy != null)
        {
            int actualDamage = targetEnemy.ReceiveDamage(totalDamage);
            Debug.Log($"[炸彈] 敵人暈眩，消耗 {bombCount} 顆未爆炸彈，造成 {actualDamage} 傷害");
            CombatUI.Instance?.AppendBattleLog($"{targetEnemy.actorId} 暈眩，{bombCount} 顆未爆炸彈反打造成 {actualDamage} 傷害");
        }

        OnBombsConsumedForDamage?.Invoke(bombCount, totalDamage, targetEnemy);
    }

    public bool HasBombAt(int pointId) => activeBombs.ContainsKey(pointId);
    public int ActiveBombCount => activeBombs.Count;
    public IReadOnlyCollection<int> ActiveBombPointIds => activeBombs.Keys;

    public bool TryGetRemainingTurns(int pointId, out int remainingTurns)
    {
        if (activeBombs.TryGetValue(pointId, out BombState bomb))
        {
            remainingTurns = bomb.remainingTurns;
            return true;
        }

        remainingTurns = 0;
        return false;
    }
}