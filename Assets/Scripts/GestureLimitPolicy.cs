using System;

[Serializable]
public class GestureLimitPolicy
{
    public int maxPointsWithoutMiddle = 3;
    public int extraPointsWhenTouchMiddle = 1;

    public bool CanAddPoint(int selectedPointCount, bool touchedMiddlePoint, bool nextIsMiddlePoint)
    {
        bool willTouchMiddle = touchedMiddlePoint || nextIsMiddlePoint;
        int maxPoints = maxPointsWithoutMiddle + (willTouchMiddle ? extraPointsWhenTouchMiddle : 0);
        return selectedPointCount + 1 <= maxPoints;
    }

    public bool CanPreviewFingerLine(int selectedPointCount)
    {
        int hardMaxPoints = maxPointsWithoutMiddle + extraPointsWhenTouchMiddle;
        return selectedPointCount < hardMaxPoints;
    }
}
