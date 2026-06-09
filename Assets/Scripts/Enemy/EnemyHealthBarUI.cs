using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    public CombatActor enemyActor;
    public Slider hpSlider;
    public Text hpText;
    
    public Slider poiseSlider;
    public Text poiseText;
    
    public GameObject attackIntent;
    public GameObject protectIntent;
    
    public float smoothSpeed = 8f;

    private EnemyPoise enemyPoise;

    private void Start()
    {
        enemyPoise = GetComponent<EnemyPoise>();
        
        if (enemyActor != null && hpSlider != null)
            hpSlider.maxValue = enemyActor.maxHp;
        if (enemyPoise != null && poiseSlider != null)
            poiseSlider.maxValue = enemyPoise.MaxPoise;
        RefreshImmediate();
    }

    private void Update()
    {
        if (enemyActor == null) return;
        
        // Update HP
        if (hpSlider != null)
        {
            hpSlider.maxValue = enemyActor.maxHp;
            hpSlider.value = Mathf.Lerp(hpSlider.value, enemyActor.currentHp, Time.deltaTime * smoothSpeed);
        }
        if (hpText != null) hpText.text = $"{enemyActor.currentHp}/{enemyActor.maxHp}";
        
        // Update Poise
        if (enemyPoise != null && poiseSlider != null)
        {
            poiseSlider.maxValue = enemyPoise.MaxPoise;
            poiseSlider.value = Mathf.Lerp(poiseSlider.value, enemyPoise.CurrentPoise, Time.deltaTime * smoothSpeed);
        }
        if (enemyPoise != null && poiseText != null)
            poiseText.text = $"{enemyPoise.CurrentPoise}/{enemyPoise.MaxPoise}";
        
        // Update Intent Display
        UpdateIntentDisplay();
    }

    public void RefreshImmediate()
    {
        if (enemyActor == null) return;
        
        if (hpSlider != null)
        {
            hpSlider.maxValue = enemyActor.maxHp;
            hpSlider.value = enemyActor.currentHp;
        }
        if (hpText != null) hpText.text = $"{enemyActor.currentHp}/{enemyActor.maxHp}";
        
        if (enemyPoise != null)
        {
            if (poiseSlider != null)
            {
                poiseSlider.maxValue = enemyPoise.MaxPoise;
                poiseSlider.value = enemyPoise.CurrentPoise;
            }
            if (poiseText != null)
                poiseText.text = $"{enemyPoise.CurrentPoise}/{enemyPoise.MaxPoise}";
        }
        
        UpdateIntentDisplay();
    }
    
    private void UpdateIntentDisplay()
    {
        if (TurnManager.Instance == null) return;
        
        // Determine enemy intent based on turn number
        // Odd turn = Attack, Even turn = Defend
        int turn = TurnManager.Instance.TurnNumber;
        bool isAttackTurn = (turn % 2 == 1);
        bool isDefenseTurn = (turn % 2 == 0);
        
        if (attackIntent != null)
            attackIntent.SetActive(isAttackTurn);
        if (protectIntent != null)
            protectIntent.SetActive(isDefenseTurn);
    }
}