using UnityEngine;

public class EventEncounterPanelUI : MonoBehaviour
{
    [SerializeField] private NodeContentManager _nodeContentManager;

    public void OnClickNextStep()
    {
        _nodeContentManager?.CloseEventPanel();
    }

    public void OnClickViewMap()
    {
        _nodeContentManager?.OnClickViewMap();
    }
}
