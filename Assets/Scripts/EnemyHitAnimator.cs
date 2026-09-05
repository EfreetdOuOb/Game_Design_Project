using System.Collections;
using UnityEngine;

public class EnemyHitAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [Header("Hit Animation")]
    public string hitParameterName = "Hit";
    public bool playStateDirectly = false;
    public string hitAnimationStateName = "";
    public float hitAnimationDuration = 0.3f;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        if (animator == null)
            animator = GetComponentInParent<Animator>();
    }

    public IEnumerator PlayHitAnimationAndWait()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        if (playStateDirectly && !string.IsNullOrEmpty(hitAnimationStateName))
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

        if (!string.IsNullOrEmpty(hitParameterName))
        {
            int previousStateHash = animator.GetCurrentAnimatorStateInfo(0).fullPathHash;
            animator.SetBool(hitParameterName, false);
            animator.SetBool(hitParameterName, true);

            float waitForStateTime = 0f;
            AnimatorStateInfo hitState = default;
            bool foundHitState = false;
            while (waitForStateTime < 2f)
            {
                yield return null;
                waitForStateTime += Time.deltaTime;
                hitState = animator.GetCurrentAnimatorStateInfo(0);

                if (hitState.fullPathHash != previousStateHash && !animator.IsInTransition(0))
                {
                    foundHitState = true;
                    break;
                }
            }

            if (foundHitState && hitState.length > 0f)
                yield return new WaitForSeconds(hitState.length);
            else
                yield return new WaitForSeconds(hitAnimationDuration);

            animator.SetBool(hitParameterName, false);
            yield break;
        }

        yield return new WaitForSeconds(hitAnimationDuration);
    }
}
