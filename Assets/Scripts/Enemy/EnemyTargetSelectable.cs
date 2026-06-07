using UnityEngine;

public class EnemyTargetSelectable : MonoBehaviour
{
    [SerializeField] private CombatActor enemyActor;
    [SerializeField] private GestureCombatActionHandler playerHandler;

    private void Awake()
    {
        if (enemyActor == null)
            enemyActor = GetComponentInParent<CombatActor>();
    }

    public void Setup(GestureCombatActionHandler handler)
    {
        playerHandler = handler;
    }

    private void OnMouseDown()
    {
        if (enemyActor == null)
        {
            Debug.LogWarning($"{name} 找不到 enemyActor");
            return;
        }

        if (enemyActor.currentHp <= 0)
        {
            Debug.Log($"{name} 已死亡，不能被選取");
            return;
        }

        if (playerHandler == null)
        {
            Debug.LogWarning($"{name} 找不到 playerHandler");
            return;
        }

        playerHandler.SetTarget(enemyActor);
        Debug.Log($"[Target] 已選取敵人：{enemyActor.actorId}");
    }
}