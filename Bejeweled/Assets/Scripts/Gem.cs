using UnityEngine;
using UnityEngine.UI;


public class Gem : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 1;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private bool _hasTargetPosition = false;
    private Vector3 _targetPosition;
    private Vector3 velocity = Vector3.zero;

    public Vector2Int Index;

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

    public void SetGemType(GemTypeScriptableObject newType)
    {
        _spriteRenderer.sprite = newType.Sprite;
    }

    public void GoToSlot(GridSlot slot)
    {
        _targetPosition = slot.transform.position;
        _hasTargetPosition = true;
    }

    private void OnValidate()
    {
        if (_spriteRenderer == null)
        {
            _spriteRenderer = this.GetComponent<SpriteRenderer>();
        }
    }
}
