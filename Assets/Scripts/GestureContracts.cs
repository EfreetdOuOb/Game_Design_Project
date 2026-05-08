using System;
using System.Collections.Generic;

public enum PointFunction
{
    None = 0,
    Attack = 1,
    Skill = 2,
    Transform = 3
}

[Serializable]
public class GestureResult
{
    public PointFunction resolvedFunction = PointFunction.None;
    public List<int> pointIds = new List<int>();
}

public interface IGestureActionHandler
{
    void OnGestureResolved(GestureResult result);
}
