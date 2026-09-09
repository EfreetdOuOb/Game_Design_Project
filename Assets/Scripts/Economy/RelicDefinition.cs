using UnityEngine;

/// <summary>
/// 遺物的「身份資料」：商店顯示用的資訊 + 綁定一個實際的效果實作。
/// </summary>
[CreateAssetMenu(menuName = "Game/Relics/Relic", fileName = "Relic_")]
public class RelicDefinition : ScriptableObject
{
    [Tooltip("唯一識別碼，用來判斷玩家是否已經擁有這個遺物")]
    public string relicId;

    public string displayName;

    [Tooltip("風味文字，給玩家看的敘述性描述")]
    [TextArea(2, 4)]
    public string flavorText;

    [Tooltip("效果說明，給玩家看的具體數值描述")]
    [TextArea(1, 3)]
    public string effectDescription;

    public Sprite icon;

    [Tooltip("商店基礎售價（實際售價還會再套用遺物的打折效果）")]
    public int price = 100;

    [Tooltip("這個遺物實際的效果實作")]
    public RelicEffectDefinition effect;
}
