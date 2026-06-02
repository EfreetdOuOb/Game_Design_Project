using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ModeSwitchImageController : MonoBehaviour
{
    [Header("Image References")]
    public Image[] modeImages = new Image[4];  // 四个 Image 组件

    [Header("Mode Sprites")]
    public Sprite attackSprite;  // 攻击图
    public Sprite defenseSprite; // 防御图

    [Header("UI Thread")]
    public ui_thread uiThread;

    private HashSet<int> usedPointIds = new HashSet<int>();  // 已使用的点位
    private bool isDefenseMode = false;

    private void Start()
    {
        if (modeImages == null || modeImages.Length == 0)
        {
            Debug.LogError("[ModeSwitchImageController] modeImages 未指派！");
            return;
        }

        if (uiThread == null)
        {
            Debug.LogError("[ModeSwitchImageController] uiThread 未指派！");
            return;
        }

        // 初始化：显示攻击图
        isDefenseMode = false;
        UpdateAllModeImages();

        // 订阅回合开始事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += ResetForNewTurn;
        }
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= ResetForNewTurn;
        }
    }

    private void Update()
    {
        CheckAndUpdateModeImages();
    }

    private void CheckAndUpdateModeImages()
    {
        if (uiThread == null) return;

        // 获取当前绘制的点位
        List<int> currentPoints = uiThread.GetSelectedPointIds();
        
        // 检查是否同时包含攻击(1-4)和防御(点9)
        bool hasAttack = currentPoints.Exists(id => id >= 0 && id <= 9);
        bool hasDefense = currentPoints.Exists(id => id == 9);

        // 如果同时有攻击和防御点位，进入防御模式
        bool shouldBeDefense = hasAttack && hasDefense;

        // 检测刚接触到点9的瞬间（从false变成true）
        if (shouldBeDefense && !isDefenseMode)
        {
            // 瞬间1：接触到点9，记录当前的攻击点位为已使用
            foreach (int pointId in currentPoints)
            {
                if (pointId >= 0 && pointId <= 4)
                {
                    usedPointIds.Add(pointId);
                    Debug.Log($"[ModeSwitchImageController] 接触到点9，点位 {pointId} 已锁住");
                }
            }
        }

        if (shouldBeDefense != isDefenseMode)
        {
            isDefenseMode = shouldBeDefense;
            UpdateUnlockedModeImages();  // 只更新未锁住的 Image
            string mode = isDefenseMode ? "防御" : "攻击";
            Debug.Log($"[ModeSwitchImageController] 四个 Image 切换为：{mode}");
        }
    }

    // 只更新未锁住的 Image
    private void UpdateUnlockedModeImages()
    {
        for (int i = 0; i < modeImages.Length; i++)
        {
            if (modeImages[i] == null) continue;

            int pointId = i + 1;
            
            // 如果这个点位已被使用过，跳过不改变
            if (usedPointIds.Contains(pointId))
            {
                continue;
            }

            // 否则根据当前模式改变图片
            if (isDefenseMode)
            {
                modeImages[i].sprite = defenseSprite;
            }
            else
            {
                modeImages[i].sprite = attackSprite;
            }
        }
    }

    private void UpdateAllModeImages()
    {
        if (usedPointIds.Contains(0))
        {
        // 点位 0 已锁住
        }
    
        for (int i = 0; i < modeImages.Length; i++)
        {
            if (modeImages[i] == null) continue;

            int pointId = i + 1;
            
            // 如果这个点位已被使用过，锁住不改变
            if (usedPointIds.Contains(pointId))
            {
                continue;
            }

            // 否则根据当前模式改变图片
            if (isDefenseMode)
            {
                modeImages[i].sprite = defenseSprite;
            }
            else
            {
                modeImages[i].sprite = attackSprite;
            }
        }
    }

    private void ResetForNewTurn()
    {
        usedPointIds.Clear();
        isDefenseMode = false;
        UpdateAllModeImages();
        Debug.Log("[ModeSwitchImageController] 新回合开始，四个 Image 已重置");
    }
}