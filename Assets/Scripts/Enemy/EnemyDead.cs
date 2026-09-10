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
    [Header("死亡動畫")]
    public string deathAnimationStateName = "Dead";
    public float deathAnimationDuration = 2f;
    private BattleController battleController;

    public bool IsDead => isDead;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (enemyActor == null)
            enemyActor = GetComponent<CombatActor>();
    }

    private void OnEnable()
    {
        if (enemyActor != null)
            enemyActor.OnDeath += HandleDeath;
    }

    private void Start()
    {
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
            if (!string.IsNullOrEmpty(deathAnimationStateName))
            {
                animator.Play(deathAnimationStateName, 0, 0f);
                yield return null;

                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.IsName(deathAnimationStateName) && state.length > 0f)
                    yield return new WaitForSeconds(state.length);
                else
                    yield return new WaitForSeconds(deathAnimationDuration);
            }
            else
            {
                yield return new WaitForSeconds(deathAnimationDuration);
            }
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
        if (enemyActor != null)
            enemyActor.OnDeath -= HandleDeath;

        if (TurnManager.Instance != null)
            TurnManager.Instance.OnTurnCleanup -= CheckDeath;
    }
}