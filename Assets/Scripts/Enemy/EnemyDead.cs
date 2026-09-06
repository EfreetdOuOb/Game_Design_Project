using System.Collections;
using UnityEngine;

public class EnemyDead : MonoBehaviour
{
    public CombatActor enemyActor;

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

        battleController?.NotifyEnemyDeath(this);
        Destroy(gameObject);
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