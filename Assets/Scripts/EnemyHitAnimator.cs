using System.Collections;
using UnityEngine;

public class EnemyHitAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Hit Animation")]
    public string hitAnimationStateName = "Armature|受擊";
    public string idleAnimationStateName = "Armature|idle";
    public float hitAnimationDuration = 0.3f;

    private CombatActor combatActor;
    private bool attackAnimationPlaying;
    private bool hitAnimationPlaying;
    private bool hitAnimationPending;

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
        if (attackAnimationPlaying || hitAnimationPlaying)
        {
            hitAnimationPending = true;
            return;
        }

        StartCoroutine(PlayHitAnimationAndWait());
    }

    public IEnumerator PlayHitAnimationAndWait()
    {
        while (attackAnimationPlaying)
            yield return null;

        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        hitAnimationPlaying = true;

        if (!string.IsNullOrEmpty(hitAnimationStateName))
        {
            animator.Play(hitAnimationStateName, 0, 0f);
            yield return null;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(hitAnimationStateName) && state.length > 0f)
            {
                yield return new WaitForSeconds(state.length);
            }
            else
            {
                yield return new WaitForSeconds(hitAnimationDuration);
            }
        }

        PlayIdleAnimation();
        hitAnimationPlaying = false;

        if (hitAnimationPending)
        {
            hitAnimationPending = false;
            yield return PlayHitAnimationAndWait();
        }
    }

    public IEnumerator PlayAttackAnimationAndWait(string attackStateName, float fallbackDuration)
    {
        while (hitAnimationPlaying)
            yield return null;

        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        attackAnimationPlaying = true;

        if (!string.IsNullOrEmpty(attackStateName))
        {
            animator.Play(attackStateName, 0, 0f);
            yield return null;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(attackStateName) && state.length > 0f)
            {
                yield return new WaitForSeconds(state.length);
            }
            else
            {
                yield return new WaitForSeconds(fallbackDuration);
            }
        }
        else
        {
            yield return new WaitForSeconds(fallbackDuration);
        }

        PlayIdleAnimation();
        attackAnimationPlaying = false;

        if (hitAnimationPending)
        {
            hitAnimationPending = false;
            yield return PlayHitAnimationAndWait();
        }
    }

    private void PlayIdleAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrEmpty(idleAnimationStateName))
            return;

        animator.Play(idleAnimationStateName, 0, 0f);
    }
}
