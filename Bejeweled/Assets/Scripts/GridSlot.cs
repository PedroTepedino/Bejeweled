using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class GridSlot : Selectable, IPointerClickHandler, IDragHandler, IDropHandler
{
    private RectTransform _transform;

    private Grid _parentGrid;
    public int Index { get; private set; }

    private void Awake()
    {
        _transform = this.GetComponent<RectTransform>();
    }

    public void Setup(Grid grid, int index)
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
        Debug.Log("Pointer Click");
        Grid.CurrentEventSystem.SetSelectedGameObject(this.gameObject);
        _parentGrid.SlotSelected(Index);
    }

    protected override void OnValidate()
    {
        this.GetComponent<Image>().color = Random.ColorHSV();
    }
}
