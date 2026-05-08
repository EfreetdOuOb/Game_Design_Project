using System;

using System.Collections;

using System.Collections.Generic;

using UnityEngine;

using UnityEngine.EventSystems;

using UnityEngine.UI;



public class ui_thread : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerEnterHandler, IEndDragHandler

{
    [Serializable]
    public class PointRule
    {
        public RectTransform point;
        [Range(1, 9)] public int pointId;
        public bool isMiddlePoint;
    }

    [Header("Point Rules (1~9)")]
    public PointRule[] pointRules = new PointRule[9];

    [Header("Draw Limits")]
    public int maxPointsWithoutMiddle = 3;
    public int extraPointsWhenTouchMiddle = 1;

    private Dictionary<RectTransform, PointRule> pointRuleMap = new Dictionary<RectTransform, PointRule>();
    private int selectedPointCount = 0;
    private bool touchedMiddlePoint = false;

    public Canvas canvas;

    public RectTransform fingerLine;

    // Optional round joint marker used to hide seams at turning points.
    public RectTransform jointPrefab;



    public List<GameObject> games = new List<GameObject>();
    public List<GameObject> joints = new List<GameObject>();



    //测试起点位置

    public RectTransform start;



    //手指或鼠标在屏幕上的点击位置

    private Vector3 touchPos;



    private bool isPress = false;

    private void Awake()

    {

        BuildPointRuleMap();

    }

    private void BuildPointRuleMap()

    {

        pointRuleMap.Clear();

        if (pointRules == null)

        {

            return;

        }

        for (int i = 0; i < pointRules.Length; i++)

        {

            PointRule rule = pointRules[i];

            if (rule == null || rule.point == null)

            {

                continue;

            }

            if (!pointRuleMap.ContainsKey(rule.point))

            {

                pointRuleMap.Add(rule.point, rule);

            }

        }

    }

    private bool IsMiddlePoint(RectTransform point)

    {

        if (point == null)

        {

            return false;

        }

        if (!pointRuleMap.TryGetValue(point, out PointRule rule))

        {

            return false;

        }

        return rule.isMiddlePoint;

    }

    private bool CanAddPoint(RectTransform nextPoint)

    {

        bool willTouchMiddle = touchedMiddlePoint || IsMiddlePoint(nextPoint);
        int maxPoints = maxPointsWithoutMiddle + (willTouchMiddle ? extraPointsWhenTouchMiddle : 0);

        return selectedPointCount + 1 <= maxPoints;

    }

    private bool CanPreviewFingerLine(PointerEventData eventData)

    {
        int hardMaxPoints = maxPointsWithoutMiddle + extraPointsWhenTouchMiddle;
        return selectedPointCount < hardMaxPoints;

    }

    private void RegisterSelectedPoint(RectTransform point)

    {

        selectedPointCount++;

        if (IsMiddlePoint(point))

        {

            touchedMiddlePoint = true;

        }

    }



    //针对手指位置和对应UI控件之间的连线需要转换坐标处理

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



            //UI世界的起点世界坐标转换为UGUI坐标

            Vector2 screenStartPos = RectTransformUtility.WorldToScreenPoint(camera, startPos);

            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), screenStartPos,

                camera, out uiStartPos);



            //鼠标坐标转换为UGUI坐标

            RectTransformUtility.ScreenPointToWorldPointInRectangle(canvas.GetComponent<RectTransform>(), touchPos,

                camera, out uitouchPos);

        }



        fingerLine.pivot = new Vector2(0, 0.5f);

        fingerLine.position = startPos;

        fingerLine.eulerAngles = new Vector3(0, 0, GetAngle(uiStartPos, uitouchPos));

        fingerLine.sizeDelta = new Vector2(GetDistance(uiStartPos, uitouchPos), fingerLine.sizeDelta.y);

    }





    //通用设置线的，如果只设置两点之间连线，只需要初入对应的ui控件的Position

    //针对手指位置和对应UI控件之间的连线需要转换坐标处理

    private RectTransform SetLine(RectTransform lineSource, Vector3 startPos, Vector3 endPos)

    {

        RectTransform line = lineSource;//Instantiate(lineSource, lineSource.parent);

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

            BuildPointRuleMap();
            start = (RectTransform)eventData.pointerEnter.transform;
            selectedPointCount = 0;
            touchedMiddlePoint = false;
            RegisterSelectedPoint(start);

            CreateJoint(start.position);



        }

    }



    public void OnDrag(PointerEventData eventData)

    {

        if (start != null)

        {

            // Use EventSystem pointer data so this works with the new Input System package.
            isPress = true;

            touchPos = eventData.position;



        }



        if (isPress && CanPreviewFingerLine(eventData))

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

        //泥嚎

    }



    public void OnEndDrag(PointerEventData eventData)

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