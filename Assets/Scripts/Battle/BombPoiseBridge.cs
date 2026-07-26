using UnityEngine;

public class BombPoiseBridge : MonoBehaviour
{
    private EnemyPoise enemyPoise;
    private CombatActor enemyActor;

    private void Awake()
    {
        enemyPoise = GetComponent<EnemyPoise>();
        enemyActor = GetComponent<CombatActor>();
    }

    private void OnEnable()
    {
        if (enemyPoise != null)
            enemyPoise.OnPoiseBroken += HandlePoiseBroken;
    }

    private void OnDisable()
    {
        if (enemyPoise != null)
            enemyPoise.OnPoiseBroken -= HandlePoiseBroken;
    }

    private void HandlePoiseBroken()
    {
        BattleBombManager.Instance?.ConsumeAllBombsForDamage(enemyActor);
    }
}