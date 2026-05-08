using System.Collections.Generic;
using UnityEngine;

public class GestureActionDispatcher : MonoBehaviour
{
    public List<MonoBehaviour> actionHandlers = new List<MonoBehaviour>();

    public void Dispatch(GestureResult result)
    {
        if (result == null || result.resolvedFunction == PointFunction.None)
        {
            return;
        }

        for (int i = 0; i < actionHandlers.Count; i++)
        {
            IGestureActionHandler handler = actionHandlers[i] as IGestureActionHandler;
            if (handler == null)
            {
                continue;
            }

            handler.OnGestureResolved(result);
        }
    }

    public void DispatchTransformActivated(GestureResult previewResult)
    {
        for (int i = 0; i < actionHandlers.Count; i++)
        {
            IGestureRuntimeActionHandler handler = actionHandlers[i] as IGestureRuntimeActionHandler;
            if (handler == null)
            {
                continue;
            }

            handler.OnTransformActivated(previewResult);
        }
    }
}
