using System;
using System.Collections.Generic;

public enum PointFunction
{
    None = 0,
    Attack = 1,
    Skill = 2,
    Transform = 3,
    Defense = 4
}

[Serializable]
public class GesturePointSnapshot
{
    public int pointId;
    public PointFunction baseFunction = PointFunction.None;
    public PointFunction finalFunction = PointFunction.None;
    public bool lockedBeforeTransform;
    public string resolvedSkillId = string.Empty;
}

[Serializable]
public class GestureResult
{
    public PointFunction resolvedFunction = PointFunction.None;
    public List<int> pointIds = new List<int>();
    public bool hasTransform;
    public int transformPointIndex = -1;
    public List<GesturePointSnapshot> pointSnapshots = new List<GesturePointSnapshot>();
}

public interface IGestureActionHandler
{
    void OnGestureResolved(GestureResult result);
}

public interface IGestureRuntimeActionHandler
{
    void OnTransformActivated(GestureResult previewResult);
}
