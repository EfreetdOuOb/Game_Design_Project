using System;

[Serializable]
public class GestureLimitPolicy
{
    public int maxPointsWithoutMiddle = 3;
    public int extraPointsWhenTouchMiddle = 1;

    public bool CanAddPoint(int selectedPointCount, bool touchedMiddlePoint)
    {
        int maxPoints = maxPointsWithoutMiddle + (touchedMiddlePoint ? extraPointsWhenTouchMiddle : 0);
        return selectedPointCount + 1 <= maxPoints;
    }

    public bool CanPreviewFingerLine(int selectedPointCount, bool touchedMiddlePoint)
    {
        int hardMaxPoints = maxPointsWithoutMiddle + (touchedMiddlePoint ? extraPointsWhenTouchMiddle : 0);
        return selectedPointCount < hardMaxPoints;
    }
}
