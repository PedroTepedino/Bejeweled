using System;
using UnityEngine;

public class SelectedGemCursor : MonoBehaviour
{
    [SerializeField] private GridManager _grid;
    [SerializeField] private SpriteRenderer _spriteRenderer;

    private void Start()
    {
        _spriteRenderer.enabled = false;
    }

    private void OnEnable()
    {
        _grid.OnCurrentGemChanged += ListenOnCurrentGemChanged;
    }

    private void OnDisable()
    {
        _grid.OnCurrentGemChanged -= ListenOnCurrentGemChanged;
    }

    private void ListenOnCurrentGemChanged(Gem gem)
    {
        if (gem == null)
        {
            _spriteRenderer.enabled = false;
        }
        else
        {
            this.transform.position = gem.transform.position;
            _spriteRenderer.enabled = true;
        }
    }

    private void OnValidate()
    {
        if (_grid == null)
        {
            _grid = FindObjectOfType<GridManager>();
        }

        if (_spriteRenderer == null)
        {
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
