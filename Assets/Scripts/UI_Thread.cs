using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ui_thread : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IEndDragHandler
{
    [Header("Dependencies")]
    public GesturePointRegistry pointRegistry;
    public GestureActionDispatcher actionDispatcher;
    public GestureLimitPolicy limitPolicy = new GestureLimitPolicy();

    [Header("View")]
    public Canvas canvas;
    public RectTransform fingerLine;
    public RectTransform jointPrefab;

    public List<GameObject> games = new List<GameObject>();
    public List<GameObject> joints = new List<GameObject>();

    public RectTransform start;

    private Vector3 touchPos;
    private bool isPress = false;

    private int selectedPointCount = 0;
    private bool touchedMiddlePoint = false;
    private readonly List<int> selectedPointIds = new List<int>();

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

        return limitPolicy.CanAddPoint(selectedPointCount, touchedMiddlePoint, IsMiddlePoint(nextPoint));
    }

    private bool CanPreviewFingerLine()
    {
        if (limitPolicy == null)
        {
            return true;
        }

        return limitPolicy.CanPreviewFingerLine(selectedPointCount);
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

    private void NotifyActionHandlers(PointFunction resolvedFunction)
    {
        if (actionDispatcher == null || resolvedFunction == PointFunction.None)
        {
            return;
        }

        GestureResult result = new GestureResult
        {
            resolvedFunction = resolvedFunction,
            pointIds = new List<int>(selectedPointIds)
        };

        actionDispatcher.Dispatch(result);
    }

    private void RegisterSelectedPoint(RectTransform point)
    {
        selectedPointCount++;

        int pointId = GetPointId(point);
        if (pointId > 0)
        {
            selectedPointIds.Add(pointId);
        }

        if (IsMiddlePoint(point))
        {
            touchedMiddlePoint = true;
        }
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
            RegisterSelectedPoint(start);
            CreateJoint(start.position);
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (start != null)
        {
            isPress = true;
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
        NotifyActionHandlers(resolvedFunction);
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