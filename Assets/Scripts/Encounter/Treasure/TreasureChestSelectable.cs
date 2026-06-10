using UnityEngine;

public class TreasureChestSelectable : MonoBehaviour
{
    private NodeContentManager _owner;
    private bool _canInteract;

    public void Setup(NodeContentManager owner)
    {
        _owner = owner;
        _canInteract = owner != null;
    }

    public void SetInteractable(bool canInteract)
    {
        _canInteract = canInteract;
    }

    private void OnMouseDown()
    {
        if (!_canInteract || _owner == null)
            return;

        _owner.OpenTreasureChest();
    }
}