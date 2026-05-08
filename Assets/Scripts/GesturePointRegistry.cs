using System;
using System.Collections.Generic;
using UnityEngine;

public class GesturePointRegistry : MonoBehaviour
{
    [Serializable]
    public class PointRule
    {
        public RectTransform point;
        [Range(1, 9)] public int pointId;
        public bool isMiddlePoint;
        public PointFunction pointFunction = PointFunction.None;
    }

    [Header("Point Rules (1~9)")]
    public PointRule[] pointRules = new PointRule[9];

    private readonly Dictionary<RectTransform, PointRule> ruleMap = new Dictionary<RectTransform, PointRule>();

    private void Awake()
    {
        Rebuild();
    }

    public void Rebuild()
    {
        ruleMap.Clear();

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

            if (!ruleMap.ContainsKey(rule.point))
            {
                ruleMap.Add(rule.point, rule);
            }
        }
    }

    public bool IsMiddlePoint(RectTransform point)
    {
        return TryGetRule(point, out PointRule rule) && rule.isMiddlePoint;
    }

    public int GetPointId(RectTransform point)
    {
        return TryGetRule(point, out PointRule rule) ? rule.pointId : -1;
    }

    public PointFunction GetPointFunction(RectTransform point)
    {
        return TryGetRule(point, out PointRule rule) ? rule.pointFunction : PointFunction.None;
    }

    private bool TryGetRule(RectTransform point, out PointRule rule)
    {
        if (point == null)
        {
            rule = null;
            return false;
        }

        return ruleMap.TryGetValue(point, out rule);
    }
}
