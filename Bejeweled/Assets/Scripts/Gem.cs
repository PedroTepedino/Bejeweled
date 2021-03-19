using UnityEngine;
using UnityEngine.EventSystems;


public class Gem : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private GridManager _parentGrid;

    private bool _hasTargetPosition = false;
    private Vector3 _targetPosition;
    private Vector3 velocity = Vector3.zero;

    public int Index { get; private set; }

    public void Setup(GridManager grid, Vector3 initialPosition = default, GemTypeSO gemType = null, GridSlot initialSlot = null)
    {
        _parentGrid = grid;
        this.transform.position = initialPosition;

        if (gemType != null)
        {
            this.SetGemType(gemType);
        }

        if (initialSlot != null)
        {
            this.GoToSlot(initialSlot);
        }
    }

    private void OnMouseDown()
    {
        _parentGrid.GemSelected(this);
    }

    private void Update()
    {
        if (!_hasTargetPosition) return;
        
        this.transform.position = Vector3.SmoothDamp(this.transform.position, _targetPosition, ref velocity,  Time.deltaTime * _moveSpeed);

        if (Vector3.Distance(this.transform.position, _targetPosition) < 0.01f )
        {
            this.transform.position = _targetPosition;
            _hasTargetPosition = false;
        }
    }

    public void SetGemType(GemTypeSO newType)
    {
        _spriteRenderer.sprite = newType.Sprite;
    }

    public void GoToSlot(GridSlot slot)
    {
        _targetPosition = slot.transform.position;
        _hasTargetPosition = true;
        Index = slot.Index;
    }

    private void OnValidate()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
        }
    }
}
