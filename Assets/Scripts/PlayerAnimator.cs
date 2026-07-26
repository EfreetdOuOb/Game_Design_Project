using System.Collections;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public GestureCombatActionHandler combatActionHandler;
    public float attackAnimationDuration = 0.3f;
    public float animationDelay = 0.4f;

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
        StartCoroutine(PlayAttackAnimation());
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
