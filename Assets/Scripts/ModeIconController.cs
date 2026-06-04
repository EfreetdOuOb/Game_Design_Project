using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 模式切换 UI 控制器
/// 管理攻击/防御/技能的 UI 显示和状态切换
/// </summary>
public class ModeSwitchImageController : MonoBehaviour
{
    #region ========== 序列化字段 ==========
    
    [Header("▶ 图片组件引用")]
    public Image[] attackDefenseImages = new Image[4];  // 点1-4的Image（攻击/防御切换）
    public Image[] skillImages = new Image[4];           // 点5-8的Image（技能显示）
    
    [Header("▶ 攻击/防御 精灵")]
    public Sprite attackSprite;   // 攻击模式时的图标
    public Sprite defenseSprite;  // 防御模式时的图标
    
    [Header("▶ 必要组件")]
    public ui_thread uiThread;  // UI绘制线程控制器
    public GestureActionRouter gestureRouter;  // 手势路由器（获取技能）
    
    [Header("▶ 技能字典")]
    public SkillDefinition[] 所有技能 = new SkillDefinition[0];  // 技能列表（直接在Inspector中指派）
    
    #endregion

    #region ========== 常量定义 ==========
    
    private const int ATTACK_POINT_START = 1;   // 攻击点开始（1）
    private const int ATTACK_POINT_END = 4;     // 攻击点结束（4）
    private const int SKILL_POINT_START = 5;    // 技能点开始（5）
    private const int SKILL_POINT_END = 8;      // 技能点结束（8）
    private const int MODE_SWITCH_POINT = 9;    // 模式转换点（9）
    
    #endregion

    #region ========== 私有字段 ==========
    
    private HashSet<int> usedAttackPoints = new HashSet<int>();  // 已使用过的攻击点位（需要固定）
    private List<int> previousPointIds = new List<int>();  // 上一帧的点位列表（用于历史追踪）
    private bool isDefenseMode = false;  // 当前是否为防御模式
    private Dictionary<string, Sprite> 技能图片缓存 = new Dictionary<string, Sprite>();  // 技能ID -> 图片的缓存
    
    #endregion

    #region ========== 生命周期 ==========
    
    private void Start()
    {
        初始化检查();
        初始化UI();
        订阅事件();
    }

    private void OnDestroy()
    {
        取消订阅事件();
    }

    private void Update()
    {
        更新模式状态();
        更新技能显示();
    }
    
    #endregion

    #region ========== 初始化 ==========
    
    /// <summary>
    /// 检查必要的组件是否已指派
    /// </summary>
    private void 初始化检查()
    {
        if (attackDefenseImages == null || attackDefenseImages.Length != 4)
        {
            //Debug.LogError("[模式切换控制器] attackDefenseImages 未正确指派！");
            enabled = false;
            return;
        }

        if (uiThread == null)
        {
            //Debug.LogError("[模式切换控制器] uiThread 未指派！");
            enabled = false;
            return;
        }

        if (attackSprite == null || defenseSprite == null)
        {
            //Debug.LogError("[模式切换控制器] 攻击/防御精灵未指派！");
            enabled = false;
            return;
        }

        if (gestureRouter == null)
        {
            Debug.LogWarning("[模式切换控制器] gestureRouter 未指派！技能显示功能将不可用");
        }

        if (所有技能 == null || 所有技能.Length == 0)
        {
            //Debug.LogWarning("[模式切换控制器] 所有技能 未在Inspector中指派！");
        }
    }

    /// <summary>
    /// 初始化 UI 显示
    /// </summary>
    private void 初始化UI()
    {
        isDefenseMode = false;
        usedAttackPoints.Clear();
        previousPointIds.Clear();
        技能图片缓存.Clear();
        刷新所有Image();
    }

    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void 订阅事件()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += 重置回合状态;
        }
    }

    /// <summary>
    /// 取消订阅游戏事件
    /// </summary>
    private void 取消订阅事件()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= 重置回合状态;
        }
    }
    
    #endregion

    #region ========== 主逻辑更新 ==========
    
    /// <summary>
    /// 每帧检查并更新攻击/防御模式
    /// </summary>
    private void 更新模式状态()
    {
        if (uiThread == null) return;

        List<int> 当前点位列表 = uiThread.GetSelectedPointIds();
        
        // 追踪所有曾经出现过的点位（包括已离开的）
        追踪所有经过的点位(当前点位列表);
        
        bool 应该防御模式 = 检查是否应该防御(当前点位列表);

        // 模式发生变化时更新UI
        if (应该防御模式 != isDefenseMode)
        {
            isDefenseMode = 应该防御模式;
            刷新未锁定Image();
            
            string 模式文本 = isDefenseMode ? "防御" : "攻击";
            //Debug.Log($"[模式切换] 切换为：{模式文本}");
        }
    }

    /// <summary>
    /// 实时更新技能显示
    /// 从 GestureActionRouter 获取点5-8的技能信息
    /// </summary>
    private void 更新技能显示()
    {
        if (skillImages == null || skillImages.Length == 0) return;
        if (gestureRouter == null) return;

        // 从 GestureActionRouter 获取技能列表
        var 技能列表 = gestureRouter.ShownSkills;
        
        if (技能列表 == null || 技能列表.Count == 0) return;

        // 更新点5-8的Image显示
        for (int i = 0; i < skillImages.Length && i < 技能列表.Count; i++)
        {
            if (skillImages[i] == null) continue;

            string 技能ID = 技能列表[i];
            
            // 获取技能对象和对应的图片
            if (获取技能图片(技能ID, out Sprite 技能精灵))
            {
                skillImages[i].sprite = 技能精灵;
                //Debug.Log($"[技能显示] 点 {5 + i}：{技能ID}");
            }
            else
            {
                skillImages[i].sprite = null;
            }
        }
    }
    
    #endregion

    #region ========== 检查判断 ==========
    
    /// <summary>
    /// 检查当前是否应该处于防御模式
    /// 条件：只要接触到点9就进入防御模式（不管从哪个点开始）
    /// </summary>
    private bool 检查是否应该防御(List<int> 点位列表)
    {
        return 点位列表.Contains(MODE_SWITCH_POINT);
    }

    /// <summary>
    /// 检查某个点位是否已被使用过
    /// </summary>
    private bool 点位已使用(int 点位)
    {
        return usedAttackPoints.Contains(点位);
    }
    
    #endregion

    #region ========== UI 更新 ==========
    
    /// <summary>
    /// 刷新所有攻击/防御 Image
    /// </summary>
    private void 刷新所有Image()
    {
        for (int i = 0; i < attackDefenseImages.Length; i++)
        {
            if (attackDefenseImages[i] == null) continue;

            int 点位 = ATTACK_POINT_START + i;
            刷新单个Image(attackDefenseImages[i], 点位);
        }
    }

    /// <summary>
    /// 只刷新未被锁定的 Image
    /// （已使用过的点位图像保持不变）
    /// </summary>
    private void 刷新未锁定Image()
    {
        for (int i = 0; i < attackDefenseImages.Length; i++)
        {
            if (attackDefenseImages[i] == null) continue;

            int 点位 = ATTACK_POINT_START + i;
            
            // 跳过已使用的点位（保持锁定）
            if (点位已使用(点位))
            {
                continue;
            }

            刷新单个Image(attackDefenseImages[i], 点位);
        }
    }

    /// <summary>
    /// 刷新单个 Image 的显示
    /// </summary>
    private void 刷新单个Image(Image image, int 点位)
    {
        image.sprite = isDefenseMode ? defenseSprite : attackSprite;
    }
    
    #endregion

    #region ========== 技能管理 ==========
    
    /// <summary>
    /// 根据技能 ID 获取技能的图片
    /// </summary>
    private bool 获取技能图片(string 技能ID, out Sprite 技能精灵)
    {
        技能精灵 = null;

        if (string.IsNullOrEmpty(技能ID))
        {
            return false;
        }

        // 检查缓存中是否已有
        if (技能图片缓存.ContainsKey(技能ID))
        {
            技能精灵 = 技能图片缓存[技能ID];
            return 技能精灵 != null;
        }

        // 检查是否指派了技能
        if (所有技能 == null || 所有技能.Length == 0)
        {
            //Debug.LogWarning("[技能显示] 未在Inspector中指派任何技能！");
            return false;
        }

        //Debug.Log($"[技能显示] 已加载 {所有技能.Length} 个技能，正在查找技能 ID: {技能ID}");

        // 在指派的技能中查找匹配的 skillId
        foreach (SkillDefinition 技能 in 所有技能)
        {
            if (技能 == null)
            {
                //Debug.LogWarning("[技能显示] 发现空的技能对象！");
                continue;
            }

            // 缓存此技能以加快查询
            if (!string.IsNullOrEmpty(技能.skillId))
            {
                if (!技能图片缓存.ContainsKey(技能.skillId))
                {
                    技能图片缓存[技能.skillId] = 技能.icon;
                }
            }

            // 检查是否是要找的技能
            if (技能.skillId == 技能ID)
            {
                技能精灵 = 技能.icon;
                //Debug.Log($"[技能显示] 成功找到技能: {技能ID}，icon: {(技能精灵 != null ? 技能精灵.name : "null")}");
                return 技能精灵 != null;
            }
        }

        //Debug.LogWarning($"[技能显示] 找不到技能 ID: {技能ID}");
        //Debug.Log($"[技能显示] 已有的技能 ID 列表: {string.Join(", ", System.Linq.Enumerable.Select(所有技能, t => t?.skillId))}");
        return false;
    }
    
    #endregion

    #region ========== 点位记录 ==========
    
    /// <summary>
    /// 追踪所有曾经经过过的攻击点
    /// 通过比较当前点位和上一帧点位，记录新出现的点位
    /// 这样即使点1已经离开，也不会被漏掉
    /// </summary>
    private void 追踪所有经过的点位(List<int> 当前点位列表)
    {
        // 记录当前帧的所有点位
        foreach (int 点位 in 当前点位列表)
        {
            if (点位 >= ATTACK_POINT_START && 点位 <= ATTACK_POINT_END)
            {
                if (!usedAttackPoints.Contains(点位))
                {
                    usedAttackPoints.Add(点位);
                    Debug.Log($"[已使用点] 点位 {点位} 已被记录");
                }
            }
        }
        
        // 更新历史记录（下一帧比较用）
        previousPointIds = new List<int>(当前点位列表);
    }
    
    #endregion

    #region ========== 回合管理 ==========
    
    /// <summary>
    /// 新回合开始时重置状态
    /// </summary>
    private void 重置回合状态()
    {
        usedAttackPoints.Clear();
        previousPointIds.Clear();
        技能图片缓存.Clear();
        isDefenseMode = false;
        刷新所有Image();
        
        //Debug.Log("[模式切换] 新回合开始，已重置所有状态");
    }
    
    #endregion
}