using UnityEngine;

public class EnemyDead : MonoBehaviour
{
    public CombatActor enemyActor;
    private Animator animator;
    private bool isDead = false;
    private float deathAnimationDuration = 2f;

    void Start()
    {
        // 尝试获取Animator用于播放死亡动画
        animator = GetComponent<Animator>();

        // 订阅回合结束事件
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnCleanup += CheckDeath;
        }
    }

    void FixedUpdate()
    {
        // FixedUpdate 中不进行任何操作，死亡检查在回合结束时进行
    }

    // 在回合结束时检查死亡
    void CheckDeath()
    {
        // 已死亡则不再检查
        if (isDead)
            return;

        // 检查血量是否为零
        if (enemyActor != null && enemyActor.currentHp <= 0)
        {
            HandleDeath();
        }
    }

    void HandleDeath()
    {
        isDead = true;

        // 禁用敌人的其他行为（如AI、移动等）
        enabled = false;
        
        // 如果有动画系统，播放死亡动画
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger("Dead");
            // 设置一个延迟销毁，等待动画播放完
            Destroy(gameObject, deathAnimationDuration);
        }
        else
        {
            // 没有动画则直接删除
            Destroy(gameObject);
        }
    }

    // 用于从其他脚本设置死亡动画时长
    public void SetDeathAnimationDuration(float duration)
    {
        deathAnimationDuration = duration;
    }

    void OnDestroy()
    {
        // 取消订阅，避免内存泄漏
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnTurnCleanup -= CheckDeath;
        }
    }
}
