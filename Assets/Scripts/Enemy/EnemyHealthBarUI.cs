using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBarUI : MonoBehaviour
{
    public CombatActor enemyActor;
    public Slider hpSlider;
    public Text hpText;
    public float smoothSpeed = 8f;

    private void Start()
    {
        if (enemyActor != null && hpSlider != null)
            hpSlider.maxValue = enemyActor.maxHp;
        RefreshImmediate();
    }

    private void Update()
    {
        if (enemyActor == null || hpSlider == null) return;
        hpSlider.maxValue = enemyActor.maxHp;
        hpSlider.value = Mathf.Lerp(hpSlider.value, enemyActor.currentHp, Time.deltaTime * smoothSpeed);
        if (hpText != null) hpText.text = $"{enemyActor.currentHp}/{enemyActor.maxHp}";
    }

    public void RefreshImmediate()
    {
        if (enemyActor == null || hpSlider == null) return;
        hpSlider.maxValue = enemyActor.maxHp;
        hpSlider.value = enemyActor.currentHp;
        if (hpText != null) hpText.text = $"{enemyActor.currentHp}/{enemyActor.maxHp}";
    }
}