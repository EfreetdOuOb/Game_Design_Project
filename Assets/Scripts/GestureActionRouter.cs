using UnityEngine;
using UnityEngine.Events;

public class GestureActionRouter : MonoBehaviour, IGestureActionHandler
{
    [Header("Actions")]
    public UnityEvent onAttack;
    public UnityEvent onSkill;
    public UnityEvent onTransform;

    public void OnGestureResolved(GestureResult result)
    {
        switch (result.resolvedFunction)
        {
            case PointFunction.Attack:
                onAttack?.Invoke();
                break;
            case PointFunction.Skill:
                onSkill?.Invoke();
                break;
            case PointFunction.Transform:
                onTransform?.Invoke();
                break;
        }
    }
}
