using System;

[Serializable]
public class BattleBomb
{
    public string bombId;
    public int linkedPointId;
    public int remainingTurns;
    public int damage;
    public bool isResolved;
    public bool touchedThisTurn;
}