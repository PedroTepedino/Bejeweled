using System;
using UnityEngine;

public class Gem : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private GridManager _parentGrid;

    private bool _hasTargetPosition = false;
    private Vector3 _targetPosition;
    private Vector3 velocity = Vector3.zero;

    public int Index { get; private set; }
    public bool IsChanging { get; private set; } = false;
    public int Type { get; private set; }
    public GridSlot CurrentSlot { get; private set; } = null;
    public bool IsMoving => _hasTargetPosition ;
    public bool IsEnabled => CurrentSlot != null;

    private MaterialPropertyBlock _propertyBlock = null;
    private readonly int _blinkMultiplierID = Shader.PropertyToID("_BlinkColorMultiplier");
    private readonly int _mainTexID = Shader.PropertyToID("_MainTex");

    public void Setup(GridManager grid, Vector3 initialPosition = default, GemTypeSO gemType = null, GridSlot initialSlot = null)
    {
        _parentGrid = grid;
        this.transform.position = initialPosition;
        this.gameObject.SetActive(true);

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
        Move();
    }

    private void Move()
    {
        if (!_hasTargetPosition) return;

        this.transform.position = Vector3.SmoothDamp(this.transform.position, _targetPosition, ref velocity, Time.deltaTime * _moveSpeed);

        if (Vector3.Distance(this.transform.position, _targetPosition) < 0.01f)
        {
            this.transform.position = _targetPosition;
            _hasTargetPosition = false;
            IsChanging = false;
        }
    }

    public void SetGemType(GemTypeSO newType)
    {
        _spriteRenderer.sprite = newType.Sprite;
        this.Type = newType.Type;
    }

    public void GoToSlot(GridSlot slot)
    {
        _targetPosition = slot.transform.position;
        _hasTargetPosition = true;
        Index = slot.Index;
        slot.CurrentGem = this;
        CurrentSlot = slot;
    }

    public void MarkAsChanging()
    {
        IsChanging = true;
    }

    public void DisableGem()
    {
        this.SetBlinkState(false);
        this.gameObject.SetActive(false);
        CurrentSlot.CurrentGem = null;
        this.CurrentSlot = null;
    }

    public void SetBlinkState(bool isBlinking)
    {
        _propertyBlock = new MaterialPropertyBlock();
        _propertyBlock.SetFloat(_blinkMultiplierID, isBlinking ? 1 : 0);
        _propertyBlock.SetTexture(_mainTexID, _spriteRenderer.sprite.texture);

        _spriteRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void OnValidate()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
        }
    }
}
