using UnityEngine;
using UnityEngine.UI;

public class GestureInputVisualState : MonoBehaviour
{
    [SerializeField] private ui_thread input;
    [SerializeField] private Graphic[] graphics;
    [SerializeField] private Color lockedColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField, Range(0f, 1f)] private float lockedStrength = 1f;

    private Color[] originalColors;

    private void Awake()
    {
        if (input == null)
            input = GetComponent<ui_thread>();

        if (graphics == null || graphics.Length == 0)
            graphics = GetComponentsInChildren<Graphic>(true);

        originalColors = new Color[graphics.Length];
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                originalColors[i] = graphics[i].color;
        }
    }

    private void OnEnable()
    {
        if (input != null)
            input.OnInputLockChanged += HandleInputLockChanged;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart += HandlePlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart += HandleEnemyTurnStart;
        }

        ApplyState(input != null && input.IsInputLocked);
    }

    private void OnDisable()
    {
        if (input != null)
            input.OnInputLockChanged -= HandleInputLockChanged;

        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart -= HandlePlayerTurnStart;
            TurnManager.Instance.OnEnemyTurnStart -= HandleEnemyTurnStart;
        }
    }

    private void HandlePlayerTurnStart()
    {
        input?.SetInputLocked(false);
        ApplyState(false);
    }

    private void HandleEnemyTurnStart()
    {
        input?.SetInputLocked(true);
        ApplyState(true);
    }

    private void HandleInputLockChanged(bool locked)
    {
        ApplyState(locked);
    }

    private void ApplyState(bool locked)
    {
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            graphic.color = locked
                ? Color.Lerp(originalColors[i], lockedColor, lockedStrength)
                : originalColors[i];
        }
    }
}