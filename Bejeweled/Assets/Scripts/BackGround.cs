using UnityEngine;
using UnityEngine.EventSystems;

public class BackGround : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private GridManager _gridManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        _gridManager.DeselectGems();
    }

    private void OnValidate()
    {
        if (_gridManager == null)
        {
            _gridManager = FindObjectOfType<GridManager>();
        }
    }
}
