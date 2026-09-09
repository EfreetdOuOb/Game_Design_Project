using System.Collections;
using UnityEngine;

public class EnemyDead : MonoBehaviour
{
    public CombatActor enemyActor;

    [Header("金幣掉落")]
    [SerializeField] private int minGoldReward = 3;
    [SerializeField] private int maxGoldReward = 8;

    private Animator animator;
    private bool isDead = false;
    private float deathAnimationDuration = 2f;
    private BattleController battleController;

    public bool IsDead => isDead;

    private void Start()
    {
        animator = GetComponent<Animator>();

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnCleanup += CheckDeath;
    }

    public void SetBattleController(BattleController controller)
    {
        battleController = controller;
    }

    private void CheckDeath()
    {
        if (isDead) return;

        if (enemyActor != null && enemyActor.currentHp <= 0)
            HandleDeath();
    }

    private void HandleDeath()
    {
        StartCoroutine(PlayDeathAnimationAndWait());
    }

    public IEnumerator PlayDeathAnimationAndWait()
    {
        if (isDead)
            yield break;

        isDead = true;

        EnemyCombatAI ai = GetComponent<EnemyCombatAI>();
        if (ai != null)
            ai.enabled = false;

        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetTrigger("Dead");
            yield return new WaitForSeconds(deathAnimationDuration);
        }

        GrantGoldDrop();

        battleController?.NotifyEnemyDeath(this);
        Destroy(gameObject);
    }

    private void GrantGoldDrop()
    {
        int minReward = Mathf.Min(minGoldReward, maxGoldReward);
        int maxReward = Mathf.Max(minGoldReward, maxGoldReward);
        int goldDropped = Random.Range(minReward, maxReward + 1);

        PlayerCurrency.Instance?.AddGold(goldDropped);
    }

    public void SetDeathAnimationDuration(float duration)
    {
        deathAnimationDuration = duration;
    }

    private void OnDestroy()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnCleanup -= CheckDeath;
    }
}