using System.Collections;
using UnityEngine;

public class PlayerActionAnimationSequence : MonoBehaviour
{
    [SerializeField] private PlayerAnimator playerAnimator;

    private void Awake()
    {
        if (playerAnimator == null)
            playerAnimator = GetComponent<PlayerAnimator>();

        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<PlayerAnimator>();

        if (playerAnimator != null)
            playerAnimator.useLegacyAttackAnimation = false;
    }

    public bool IsPlaying { get; private set; }

    public IEnumerator Play(GestureResult result, GestureActionDispatcher actionDispatcher)
    {
        if (result == null || playerAnimator == null)
            yield break;

        IsPlaying = true;

        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            GesturePointSnapshot snapshot = result.pointSnapshots[i];
            if (snapshot == null)
                continue;

            if (snapshot.finalFunction == PointFunction.None || snapshot.finalFunction == PointFunction.Transform)
                continue;

            GestureResult singleResult = new GestureResult
            {
                resolvedFunction = snapshot.finalFunction,
                pointIds = new System.Collections.Generic.List<int> { snapshot.pointId },
                pointSnapshots = new System.Collections.Generic.List<GesturePointSnapshot> { snapshot }
            };

            if (snapshot.finalFunction == PointFunction.Attack)
            {
                yield return playerAnimator.PlayActionAnimationAndWait(snapshot);
            }
            else if (snapshot.finalFunction == PointFunction.Skill)
            {
                GestureCombatActionHandler combat = playerAnimator.combatActionHandler;
                string skillId = combat != null ? combat.ResolveSkillIdForAnimation(snapshot) : snapshot.resolvedSkillId;
                yield return playerAnimator.PlaySkillAnimationAndWait(skillId);
            }

            actionDispatcher?.Dispatch(singleResult);

            if (snapshot.finalFunction == PointFunction.Attack)
            {
                yield return PlayTargetHitAnimationAndWait();

                if (IsCurrentTargetDead())
                {
                    yield return PlayTargetDeathAnimationAndWait();
                    break;
                }
            }
            else if (snapshot.finalFunction == PointFunction.Skill && IsCurrentTargetDead())
            {
                yield return PlayTargetDeathAnimationAndWait();
                break;
            }

        }

        IsPlaying = false;
    }

    private IEnumerator PlayTargetHitAnimationAndWait()
    {
        GestureCombatActionHandler combat = playerAnimator.combatActionHandler;
        if (combat == null || combat.CurrentTarget == null)
            yield break;

        EnemyHitAnimator hitAnimator = combat.CurrentTarget.GetComponent<EnemyHitAnimator>();
        if (hitAnimator == null)
            hitAnimator = combat.CurrentTarget.GetComponentInParent<EnemyHitAnimator>();

        if (hitAnimator == null)
            hitAnimator = combat.CurrentTarget.gameObject.AddComponent<EnemyHitAnimator>();

        if (hitAnimator != null)
            yield return hitAnimator.PlayHitAnimationAndWait();
    }

    private bool IsCurrentTargetDead()
    {
        GestureCombatActionHandler combat = playerAnimator.combatActionHandler;
        return combat != null && combat.CurrentTarget != null && combat.CurrentTarget.IsDead;
    }

    private IEnumerator PlayTargetDeathAnimationAndWait()
    {
        GestureCombatActionHandler combat = playerAnimator.combatActionHandler;
        if (combat == null || combat.CurrentTarget == null)
            yield break;

        EnemyDead enemyDead = combat.CurrentTarget.GetComponent<EnemyDead>();
        if (enemyDead == null)
            enemyDead = combat.CurrentTarget.GetComponentInParent<EnemyDead>();

        if (enemyDead != null)
            yield return enemyDead.PlayDeathAnimationAndWait();
    }
}
