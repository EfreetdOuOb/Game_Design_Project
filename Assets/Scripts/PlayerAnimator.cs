using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [System.Serializable]
    public class SkillAnimationBinding
    {
        public string skillId;
        public string animationStateName;
    }

    [SerializeField] private Animator animator;
    [Header("Skill Animations")]
    [Tooltip("Use the skill ID as the key and assign the Animator state name to play.")]
    public List<SkillAnimationBinding> skillAnimations = new List<SkillAnimationBinding>();
    [Header("Basic Animations")]
    public string attackAnimationStateName = "rig|攻擊";
    public string idleAnimationStateName = "rig|idle";
    public GestureCombatActionHandler combatActionHandler;
    public float attackAnimationDuration = 0.3f;
    public float animationDelay = 0.4f;
    public float skillAnimationDuration = 0.4f;
    public bool useLegacyAttackAnimation = true;

    private void Awake()
    {
        // 先嘗試在同一個 GameObject 上找
        if (animator == null)
            animator = GetComponent<Animator>();
        
        // 如果找不到，嘗試在子物件中找
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        // 如果還是找不到，嘗試在父物件中找
        if (animator == null)
            animator = GetComponentInParent<Animator>();
        
        combatActionHandler = GetComponent<GestureCombatActionHandler>();

        if (animator == null)
            Debug.LogError("[PlayerAnimator] 找不到 Animator 組件，請在 Inspector 中手動設置或確保 Animator 在同一/子/父物件");
        if (combatActionHandler == null)
            Debug.LogError("[PlayerAnimator] 找不到 GestureCombatActionHandler 組件");
    }

    private void OnEnable()
    {
        if (combatActionHandler != null)
        {
            // OnGestureResolved 會在攻擊時被調用
            Debug.Log("[PlayerAnimator] 已訂閱 GestureCombatActionHandler");
        }
    }

    private void OnDisable()
    {
    }

    // 由 GestureCombatActionHandler 的 ExecuteAttack 直接調用
    public void OnPlayerAttack()
    {
        if (!useLegacyAttackAnimation)
            return;

        StartCoroutine(PlayAttackAnimation());
    }

    public IEnumerator PlayAttackAnimationAndWait()
    {
        if (animator == null || !animator.isActiveAndEnabled)
            yield break;

        if (!string.IsNullOrEmpty(attackAnimationStateName))
        {
            animator.Play(attackAnimationStateName, 0, 0f);
            yield return null;

            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
            if (state.IsName(attackAnimationStateName) && state.length > 0f)
            {
                yield return new WaitForSeconds(state.length);
                yield break;
            }
        }

        yield return PlayAttackAnimation();
    }

    public IEnumerator PlayActionAnimationAndWait(GesturePointSnapshot snapshot)
    {
        if (snapshot == null)
            yield break;

        if (snapshot.finalFunction == PointFunction.Attack)
            yield return PlayAttackAnimationAndWait();
        else if (snapshot.finalFunction == PointFunction.Skill)
            yield return PlaySkillAnimationAndWait(snapshot.resolvedSkillId);
    }

    public IEnumerator PlaySkillAnimationAndWait(string skillId)
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrEmpty(skillId))
            yield break;

        string stateName = FindSkillAnimationState(skillId);
        if (string.IsNullOrEmpty(stateName))
            yield break;

        animator.Play(stateName, 0, 0f);
        yield return null;

        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName(stateName) && state.length > 0f)
        {
            yield return new WaitForSeconds(state.length);
        }
        else
        {
            yield return new WaitForSeconds(skillAnimationDuration);
        }

        PlayIdleAnimation();
    }

    private string FindSkillAnimationState(string skillId)
    {
        for (int i = 0; i < skillAnimations.Count; i++)
        {
            SkillAnimationBinding binding = skillAnimations[i];
            if (binding != null && binding.skillId == skillId)
                return binding.animationStateName;
        }

        Debug.LogWarning($"[PlayerAnimator] 找不到技能動畫對應：{skillId}");
        return string.Empty;
    }

    private void PlayIdleAnimation()
    {
        if (animator == null || !animator.isActiveAndEnabled || string.IsNullOrEmpty(idleAnimationStateName))
            return;

        animator.Play(idleAnimationStateName, 0, 0f);
    }

    private IEnumerator PlayAttackAnimation()
    {
        if (animator != null && animator.isActiveAndEnabled)
        {
            animator.SetBool("Attack", true);
            yield return new WaitForSeconds(attackAnimationDuration);
            animator.SetBool("Attack", false);
        }
    }
}
