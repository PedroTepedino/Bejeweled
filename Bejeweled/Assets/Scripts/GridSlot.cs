using UnityEngine;

public class GridSlot : MonoBehaviour
{
    private GridManager _parentGrid;
    public int Index { get; private set; }

    public Gem CurrentGem = null;

    public void Setup(GridManager grid, int index)
    {
        _parentGrid = grid;
        SetIndex(index);
    }

    public void SetIndex(int newIndex)
    {
        Index = newIndex;
    }
}
