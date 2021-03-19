using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridSlot : MonoBehaviour, IPointerClickHandler, IDragHandler, IDropHandler
{
    private GameManager _parentGrid;
    public int Index { get; private set; }

    public void Setup(GameManager grid, int index)
    {
        _parentGrid = grid;
        SetIndex(index);
    }

    public void SetIndex(int newIndex)
    {
        Index = newIndex;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("Drag");
        //_transform.position = Input.mousePosition;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Drop");
    }

    public void OnPointerClick(PointerEventData eventData)
    { 
        _parentGrid.SlotSelected(Index);
    }
}
