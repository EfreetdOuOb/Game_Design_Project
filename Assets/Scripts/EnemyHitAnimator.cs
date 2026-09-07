using System.Collections;
using UnityEngine;

public class EnemyHitAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Hit Animation")]
    public string hitAnimationStateName = "Armature|受擊";
    public float hitAnimationDuration = 0.3f;

    private CombatActor combatActor;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();

        combatActor = GetComponent<CombatActor>();
        if (combatActor == null)
            combatActor = GetComponentInParent<CombatActor>();
    }

    private void OnEnable()
    {
        if (combatActor != null)
            combatActor.OnDamaged += HandleDamaged;
    }

    private void OnDisable()
    {
        if (combatActor != null)
            combatActor.OnDamaged -= HandleDamaged;
    }

    private void HandleDamaged(int damage)
    {
        StartCoroutine(PlayHitAnimationAndWait());
    }

    public IEnumerator PlayHitAnimationAndWait()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        if (!string.IsNullOrEmpty(hitAnimationStateName))
        {
            animator.Play(hitAnimationStateName, 0, 0f);
            yield return null;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(hitAnimationStateName) && state.length > 0f)
            {
                yield return new WaitForSeconds(state.length);
                yield break;
            }
        }

        yield return new WaitForSeconds(hitAnimationDuration);
    }
}
