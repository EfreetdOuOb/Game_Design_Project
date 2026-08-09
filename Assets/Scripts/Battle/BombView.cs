using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// 在法陣點位上顯示炸彈圖示與剩餘回合數，訂閱 BattleBombManager 的事件來同步狀態。
public class BombView : MonoBehaviour
{
    [Header("依賴")]
    [SerializeField] private GesturePointRegistry pointRegistry;

    [Header("圖示設定")]
    [SerializeField] private Sprite bombIconSprite;
    [SerializeField] private Vector2 iconSize = new Vector2(36, 36);
    [Tooltip("圖示相對於法陣點位右上角的偏移量，正值會讓圖示往外突出，避免蓋住點位本體的圖案")]
    [SerializeField] private Vector2 cornerOffset = new Vector2(8f, 8f);
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color dangerColor = Color.red;
    [Tooltip("剩餘回合小於等於此值時，圖示會切換為警示樣式")]
    [SerializeField] private int dangerTurnThreshold = 1;
    [Tooltip("倒數數字的黑色描邊粗細")]
    [SerializeField] private float countdownOutlineThickness = 1.3f;

    [Header("動畫設定")]
    [SerializeField] private float popInDuration = 0.3f;
    [SerializeField] private float popOutDuration = 0.2f;
    [SerializeField] private float pulseScale = 1.15f;
    [SerializeField] private float pulseDuration = 0.5f;
    [SerializeField] private float dangerPulseDuration = 0.25f;

    private class IndicatorInstance
    {
        public RectTransform root;
        public Image icon;
        public Text countdownText;
        public Tween pulseTween;
    }

    private readonly Dictionary<int, IndicatorInstance> indicators = new Dictionary<int, IndicatorInstance>();
    private BattleBombManager subscribedManager;

    private void Awake()
    {
        if (pointRegistry == null)
            pointRegistry = GesturePointRegistry.Instance;

        if (pointRegistry == null)
            pointRegistry = Object.FindAnyObjectByType<GesturePointRegistry>();
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void Start()
    {
        // Unity 不保證不同物件的 Awake 執行順序，
        // 若 BombView.OnEnable 早於 BattleBombManager.Awake 執行，Instance 會是 null 而訂閱失敗。
        // Start 保證在場景所有 Awake 之後才執行，這裡補一次確保一定訂閱得到。
        TrySubscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
        ClearAllIndicators(instant: true);
    }

    private void TrySubscribe()
    {
        if (subscribedManager != null)
            return;

        BattleBombManager manager = BattleBombManager.Instance;
        if (manager == null)
            return;

        manager.OnBombRegistered += HandleBombRegistered;
        manager.OnBombTurnsChanged += HandleBombTurnsChanged;
        manager.OnBombDetonatedByTimer += HandleBombRemoved;
        manager.OnBombsConsumedForDamage += HandleAllBombsConsumed;
        manager.OnAllBombsCleared += HandleAllBombsCleared;
        subscribedManager = manager;

        SyncExistingBombs(manager);
    }

    private void Unsubscribe()
    {
        if (subscribedManager == null)
            return;

        subscribedManager.OnBombRegistered -= HandleBombRegistered;
        subscribedManager.OnBombTurnsChanged -= HandleBombTurnsChanged;
        subscribedManager.OnBombDetonatedByTimer -= HandleBombRemoved;
        subscribedManager.OnBombsConsumedForDamage -= HandleAllBombsConsumed;
        subscribedManager.OnAllBombsCleared -= HandleAllBombsCleared;
        subscribedManager = null;
    }

    private void SyncExistingBombs(BattleBombManager manager)
    {
        foreach (int pointId in manager.ActiveBombPointIds)
        {
            if (manager.TryGetRemainingTurns(pointId, out int remainingTurns))
                ShowOrUpdateIndicator(pointId, remainingTurns);
        }
    }

    private void HandleBombRegistered(int pointId, int remainingTurns)
    {
        ShowOrUpdateIndicator(pointId, remainingTurns);
    }

    private void HandleBombTurnsChanged(int pointId, int remainingTurns)
    {
        ShowOrUpdateIndicator(pointId, remainingTurns);
    }

    private void HandleBombRemoved(int pointId)
    {
        RemoveIndicator(pointId);
    }

    private void HandleAllBombsConsumed(int bombCount, int totalDamage)
    {
        ClearAllIndicators(instant: false);
    }

    private void HandleAllBombsCleared()
    {
        ClearAllIndicators(instant: false);
    }

    private void ShowOrUpdateIndicator(int pointId, int remainingTurns)
    {
        bool isNew = !indicators.TryGetValue(pointId, out IndicatorInstance instance);
        if (isNew)
        {
            instance = CreateIndicator(pointId);
            if (instance == null)
                return;

            indicators[pointId] = instance;
        }

        bool isDanger = remainingTurns <= dangerTurnThreshold;

        if (instance.countdownText != null)
            instance.countdownText.text = remainingTurns.ToString();

        if (instance.icon != null)
            instance.icon.color = isDanger ? dangerColor : normalColor;

        if (isNew)
            PlayEntranceThenPulse(instance, isDanger);
        else
            PlayPulse(instance, isDanger);
    }

    private IndicatorInstance CreateIndicator(int pointId)
    {
        RectTransform pointTransform = pointRegistry != null ? pointRegistry.GetPointTransformById(pointId) : null;
        if (pointTransform == null)
        {
            Debug.LogWarning($"[炸彈UI] 找不到 pointId={pointId} 對應的法陣點位，無法顯示炸彈圖示");
            return null;
        }

        GameObject root = new GameObject($"BombIndicator_{pointId}", typeof(RectTransform));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.SetParent(pointTransform, false);
        // 錨點設在點位右上角，圖示以徽章（badge）的方式往外突出，
        // 才不會蓋住法陣點位本身的圖案／文字，避免資訊互相重疊。
        rootRect.anchorMin = new Vector2(1f, 1f);
        rootRect.anchorMax = new Vector2(1f, 1f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.anchoredPosition = cornerOffset;
        rootRect.sizeDelta = iconSize;
        rootRect.localScale = Vector3.zero;

        Image icon = root.AddComponent<Image>();
        icon.sprite = bombIconSprite;
        icon.color = normalColor;
        icon.raycastTarget = false;
        icon.preserveAspect = true;

        GameObject textGo = new GameObject("Countdown", typeof(RectTransform));
        RectTransform textRect = (RectTransform)textGo.transform;
        textRect.SetParent(rootRect, false);
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text countdownText = textGo.AddComponent<Text>();
        countdownText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        countdownText.alignment = TextAnchor.MiddleCenter;
        countdownText.color = Color.white;
        countdownText.fontStyle = FontStyle.Bold;
        countdownText.raycastTarget = false;
        AddTextOutline(textGo, Color.black, countdownOutlineThickness);

        return new IndicatorInstance
        {
            root = rootRect,
            icon = icon,
            countdownText = countdownText
        };
    }

    // 單一 Outline 元件只會往一個方向描邊，八個方向各疊一層 Shadow 才能做出完整的黑色外框效果。
    private static void AddTextOutline(GameObject textGo, Color color, float thickness)
    {
        Vector2[] directions =
        {
            new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1), new Vector2(0, -1),
            new Vector2(1, 1), new Vector2(-1, 1), new Vector2(1, -1), new Vector2(-1, -1)
        };

        foreach (Vector2 dir in directions)
        {
            Shadow outline = textGo.AddComponent<Shadow>();
            outline.effectColor = color;
            outline.effectDistance = dir * thickness;
            outline.useGraphicAlpha = true;
        }
    }

    private void PlayEntranceThenPulse(IndicatorInstance instance, bool isDanger)
    {
        instance.pulseTween?.Kill();
        instance.root.DOKill();

        // 無限循環的 tween 不能放進 Sequence（DOTween 只允許 Sequence 本身無限循環），
        // 所以進場動畫播完後用 OnComplete 另外啟動一個獨立的循環 tween。
        instance.pulseTween = instance.root
            .DOScale(1f, popInDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() => StartPulseLoop(instance, isDanger));
    }

    private void StartPulseLoop(IndicatorInstance instance, bool isDanger)
    {
        if (instance.root == null)
            return;

        float duration = isDanger ? dangerPulseDuration : pulseDuration;
        float scale = isDanger ? pulseScale * 1.15f : pulseScale;

        instance.pulseTween = instance.root
            .DOScale(scale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void PlayPulse(IndicatorInstance instance, bool isDanger)
    {
        instance.pulseTween?.Kill();
        instance.root.DOKill();

        float duration = isDanger ? dangerPulseDuration : pulseDuration;
        float scale = isDanger ? pulseScale * 1.15f : pulseScale;

        instance.root.localScale = Vector3.one;
        instance.pulseTween = instance.root
            .DOScale(scale, duration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void RemoveIndicator(int pointId)
    {
        if (!indicators.TryGetValue(pointId, out IndicatorInstance instance))
            return;

        indicators.Remove(pointId);
        PlayPopOutAndDestroy(instance);
    }

    private void PlayPopOutAndDestroy(IndicatorInstance instance)
    {
        if (instance.root == null)
            return;

        instance.pulseTween?.Kill();
        instance.root.DOKill();

        RectTransform root = instance.root;
        root.DOScale(0f, popOutDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                if (root != null)
                    Destroy(root.gameObject);
            });
    }

    private void ClearAllIndicators(bool instant)
    {
        List<int> pointIds = new List<int>(indicators.Keys);
        foreach (int pointId in pointIds)
        {
            if (!indicators.TryGetValue(pointId, out IndicatorInstance instance))
                continue;

            indicators.Remove(pointId);

            if (instance.root == null)
                continue;

            instance.pulseTween?.Kill();
            instance.root.DOKill();

            if (instant)
                Destroy(instance.root.gameObject);
            else
                PlayPopOutAndDestroy(instance);
        }
    }
}
