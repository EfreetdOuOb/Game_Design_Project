using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ui_thread : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IEndDragHandler, IPointerUpHandler
{
    [Header("Dependencies")]
    public GesturePointRegistry pointRegistry;
    public GestureActionDispatcher actionDispatcher;
    public GestureLimitPolicy limitPolicy = new GestureLimitPolicy();

    [Header("View")]
    public Canvas canvas;
    public RectTransform fingerLine;
    public RectTransform jointPrefab;

    [Header("Debug Colors")]
    [Range(0f, 1f)]
    public float lockedOverlayStrength = 0.55f;
    public bool useDebugColors = true;
    public Color idleColor = Color.white;
    public Color attackColor = new Color(1f, 0.3f, 0.3f, 1f);
    public Color skillColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color transformColor = new Color(0.9f, 0.4f, 1f, 1f);
    public Color defenseColor = new Color(0.3f, 1f, 0.5f, 1f);
    public Color lockedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Debug Logs")]
    public bool enableDebugLogs = false;

    public List<GameObject> games = new List<GameObject>();
    public List<GameObject> joints = new List<GameObject>();

    public RectTransform start;

    private Vector3 touchPos;
    private bool isPress = false;

    private int selectedPointCount = 0;
    private bool touchedMiddlePoint = false;
    private readonly List<int> selectedPointIds = new List<int>();
    private readonly List<RectTransform> selectedPoints = new List<RectTransform>();
    private bool transformTriggeredThisGesture = false;
    private bool hasDraggedThisGesture = false;

    private void Awake()
    {
        if (pointRegistry != null)
        {
            pointRegistry.Rebuild();
        }

        
    }

    private bool IsMiddlePoint(RectTransform point)
    {
        return pointRegistry != null && pointRegistry.IsMiddlePoint(point);
    }

    private bool CanAddPoint(RectTransform nextPoint)
    {
        if (limitPolicy == null)
        {
            return true;
        }

        return limitPolicy.CanAddPoint(selectedPointCount, touchedMiddlePoint);
    }

    private bool CanPreviewFingerLine()
    {
        if (limitPolicy == null)
        {
            return true;
        }

        return limitPolicy.CanPreviewFingerLine(selectedPointCount, touchedMiddlePoint);
    }

    private int GetPointId(RectTransform point)
    {
        if (pointRegistry == null)
        {
            return -1;
        }

        return pointRegistry.GetPointId(point);
    }

    private PointFunction ResolveFunctionByPath()
    {
        if (start == null || pointRegistry == null)
        {
            return PointFunction.None;
        }

        // Default strategy: last selected point decides action.
        return pointRegistry.GetPointFunction(start);
    }

    private void DispatchTransformActivatedIfNeeded()
    {
        if (transformTriggeredThisGesture || !touchedMiddlePoint || actionDispatcher == null)
        {
            return;
        }

        GestureResult previewResult = BuildGestureResult(ResolveFunctionByPath());
        actionDispatcher.DispatchTransformActivated(previewResult);
        transformTriggeredThisGesture = true;
    }


// 抽出來的輔助方法，讓邏輯只寫一次
private Color GetBaseColorForFunction(PointFunction func)
{
    switch (func)
    {
        case PointFunction.Attack:    return attackColor;
        case PointFunction.Skill:     return skillColor;
        case PointFunction.Transform: return transformColor;
        case PointFunction.Defense:   return defenseColor;
        default:                      return idleColor;
    }
}

    private GestureResult BuildGestureResult(PointFunction resolvedFunction)
    {
        GestureResult result = new GestureResult
        {
            resolvedFunction = resolvedFunction,
            pointIds = new List<int>(selectedPointIds)
        };

        int transformIndex = -1;
        for (int i = 0; i < selectedPoints.Count; i++)
        {
            if (IsMiddlePoint(selectedPoints[i]))
            {
                transformIndex = i;
                break;
            }
        }

        result.hasTransform = transformIndex >= 0;
        result.transformPointIndex = transformIndex;

        for (int i = 0; i < selectedPoints.Count; i++)
        {
            RectTransform point = selectedPoints[i];
            int pointId = GetPointId(point);
            PointFunction baseFunction = pointRegistry != null ? pointRegistry.GetPointFunction(point) : PointFunction.None;

            bool lockedBeforeTransform = result.hasTransform && i < transformIndex;
            bool transformedPhase = result.hasTransform && i > transformIndex;

            PointFunction finalFunction = baseFunction;
            if (!lockedBeforeTransform && transformedPhase)
            {
                if (baseFunction == PointFunction.Attack)
                {
                    finalFunction = PointFunction.Defense;
                }
                else if (baseFunction == PointFunction.Skill)
                {
                    finalFunction = PointFunction.Skill;
                }
            }

            result.pointSnapshots.Add(new GesturePointSnapshot
            {
                pointId = pointId,
                baseFunction = baseFunction,
                finalFunction = finalFunction,
                lockedBeforeTransform = lockedBeforeTransform
            });
        }

        if (result.pointSnapshots.Count > 0)
        {
            result.resolvedFunction = result.pointSnapshots[result.pointSnapshots.Count - 1].finalFunction;
        }

        return result;
    }

    // 修改 NotifyActionHandlers，對每個 snapshot 依序執行
    private void NotifyActionHandlers(GestureResult result)
    {
        if (actionDispatcher == null || result == null) return;

    foreach (var snap in result.pointSnapshots)
    {
        // lockedBeforeTransform 只是視覺鎖定，效果仍然要執行
        // 只跳過 Transform 點本身和 None
        if (snap.finalFunction == PointFunction.None) continue;
        if (snap.finalFunction == PointFunction.Transform) continue; // Transform點只觸發形態變換，不執行戰鬥效果

        GestureResult singleResult = new GestureResult
        {
            resolvedFunction = snap.finalFunction,
            pointIds = new List<int> { snap.pointId },
            pointSnapshots = new List<GesturePointSnapshot> { snap }
        };
        actionDispatcher.Dispatch(singleResult);
    }
    }

    private void RegisterSelectedPoint(RectTransform point)
    {
        selectedPointCount++;

        int pointId = GetPointId(point);
        if (pointId > 0)
        {
            selectedPointIds.Add(pointId);
        }

        selectedPoints.Add(point);

        if (IsMiddlePoint(point))
        {
            touchedMiddlePoint = true;
        }

        DispatchTransformActivatedIfNeeded();
        
    }

    private void LogGestureSummary(GestureResult result)
    {
        if (!enableDebugLogs || result == null)
        {
            return;
        }

        string pathText = "";
        for (int i = 0; i < result.pointSnapshots.Count; i++)
        {
            if (i > 0)
            {
                pathText += " -> ";
            }

            GesturePointSnapshot snap = result.pointSnapshots[i];
            pathText += $"{snap.pointId}({snap.finalFunction})";
        }

        Debug.Log($"[總結] 路徑={pathText}，有轉換={result.hasTransform}，最終功能={result.resolvedFunction}");
    }



    private void UpdateFingerLine(Vector3 startPos)
    {
        Vector3 uiStartPos = Vector3.zero;
        Vector3 uitouchPos = Vector3.zero;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            uiStartPos = startPos;
            uitouchPos = touchPos;
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Camera camera = canvas.worldCamera;
            Vector2 screenStartPos = RectTransformUtility.WorldToScreenPoint(camera, startPos);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), screenStartPos,
                camera, out uiStartPos);
            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), touchPos,
                camera, out uitouchPos);
        }
        fingerLine.pivot = new Vector2(0, 0.5f);
        fingerLine.position = startPos;
        fingerLine.eulerAngles = new Vector3(0, 0, GetAngle(uiStartPos, uitouchPos));
        fingerLine.sizeDelta = new Vector2(GetDistance(uiStartPos, uitouchPos), fingerLine.sizeDelta.y);
    }

    private RectTransform SetLine(RectTransform lineSource, Vector3 startPos, Vector3 endPos)
    {
        RectTransform line = lineSource;
        line.pivot = new Vector2(0, 0.5f);
        line.position = startPos;
        line.eulerAngles = new Vector3(0, 0, GetAngle(startPos, endPos));
        line.sizeDelta = new Vector2(GetDistance(startPos, endPos), lineSource.sizeDelta.y);
        return line;
    }
    private float GetAngle(Vector3 startPos, Vector3 endPos)
    {
        Vector3 dir = endPos - startPos;
        float angle = Vector3.Angle(Vector3.right, dir);
        Vector3 cross = Vector3.Cross(Vector3.right, dir);
        float dirF = cross.z > 0 ? 1 : -1;
        angle = angle * dirF;
        return angle;
    }
    private float GetDistance(Vector3 startPos, Vector3 endPos)
    {
        float distance = Vector3.Distance(endPos, startPos);
        return distance * 1 / canvas.transform.localScale.x;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<sta>() != null)
        {
            if (pointRegistry != null)
            {
                pointRegistry.Rebuild();
            }

            start = (RectTransform)eventData.pointerEnter.transform;
            selectedPointCount = 0;
            touchedMiddlePoint = false;
            selectedPointIds.Clear();
            selectedPoints.Clear();
            transformTriggeredThisGesture = false;
            hasDraggedThisGesture = false;
            RegisterSelectedPoint(start);
            CreateJoint(start.position);
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (start != null)
        {
            isPress = true;
            hasDraggedThisGesture = true;
            touchPos = eventData.position;
        }
        if (isPress && CanPreviewFingerLine())
        {
            fingerLine.gameObject.SetActive(true);
            UpdateFingerLine(start.position);
        }
        else
        {
            fingerLine.gameObject.SetActive(false);
        }
        if (eventData.pointerEnter != null && eventData.pointerEnter.GetComponent<sta>() != null)
        {
            if (eventData.pointerEnter.name != start.name)
            {
                bool cs = true;
                for (int i = 0; i < games.Count; i++)
                {
                    if(games[i].name == eventData.pointerEnter.name+"cs")
                    {
                        cs = false;
                    }
                }
                if (cs)
                {
                    RectTransform nextPoint = (RectTransform)eventData.pointerEnter.transform;
                    if (!CanAddPoint(nextPoint))
                    {
                        return;
                    }
                    GameObject game = Instantiate(fingerLine.gameObject, start.position, Quaternion.identity, fingerLine.parent);
                    game.name = start.gameObject.name + "cs";
                    SetLine((RectTransform)game.transform, start.position, nextPoint.position);
                    games.Add(game);
                    CreateJoint(nextPoint.position);
                    start = nextPoint;
                    RegisterSelectedPoint(nextPoint);
                }
            }
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        PointFunction resolvedFunction = ResolveFunctionByPath();
        GestureResult result = BuildGestureResult(resolvedFunction);
        NotifyActionHandlers(result);
        LogGestureSummary(result);

        if (result.hasTransform &&
        (hasDraggedThisGesture || result.pointSnapshots.Count > 1) &&
        actionDispatcher != null)
        {
        
        GestureResult transformResult = result;
        transformResult.resolvedFunction = PointFunction.Transform;
        actionDispatcher.Dispatch(transformResult);
        }
        ResetGestureState();

        Debug.Log("[UI] 手勢結束，呼叫 EndPlayerTurn");
        TurnManager.Instance?.EndPlayerTurn();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // Tap-only fallback: if this gesture never dragged, EndDrag might not fire.
        if (!hasDraggedThisGesture)
        {
            ResetGestureState();
        }
    }

    private void ResetGestureState()
    {
        while (games.Count > 0)
        {
            Destroy(games[0]);
            games.RemoveAt(0);
        }
        while (joints.Count > 0)
        {
            Destroy(joints[0]);
            joints.RemoveAt(0);
        }
        start = null;
        selectedPointCount = 0;
        touchedMiddlePoint = false;
        selectedPointIds.Clear();
        selectedPoints.Clear();
        transformTriggeredThisGesture = false;
        hasDraggedThisGesture = false;
        isPress = false;
        
        fingerLine.gameObject.SetActive(false);
    }
    private void CreateJoint(Vector3 worldPos)
    {
        if (jointPrefab == null)
        {
            return;
        }
        GameObject joint = Instantiate(jointPrefab.gameObject, worldPos, Quaternion.identity, fingerLine.parent);
        joint.SetActive(true);
        joints.Add(joint);
    }
}