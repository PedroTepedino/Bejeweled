using UnityEngine;

public class GridSlot : MonoBehaviour
{
    private GemGrid _parentGrid;
    public int ListIndex { get; private set; }

    public Gem CurrentGem = null;

    public void Setup(GemGrid grid, int index)
    {
        _parentGrid = grid;
        ListIndex = index;
    }

    public Vector2Int MatrixIndex => _parentGrid.ListToMatrixIndex(ListIndex);
}
