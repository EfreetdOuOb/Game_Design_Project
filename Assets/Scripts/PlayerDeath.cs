using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private CombatActor combatActor;

    private void Awake()
    {
        combatActor = GetComponent<CombatActor>();
        if (combatActor == null)
        {
            Debug.LogError("PlayerDeath requires a CombatActor on the same GameObject.");
        }
    }

    private void OnEnable()
    {
        if (combatActor != null)
        {
            combatActor.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (combatActor != null)
        {
            combatActor.OnDeath -= HandleDeath;
        }
    }

    private void Start()
    {
        if (combatActor != null && combatActor.currentHp <= 0)
        {
            HandleDeath();
        }
    }

    private void HandleDeath()
    {
        gameObject.SetActive(false);
    }

}
