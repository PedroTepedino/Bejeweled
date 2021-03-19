using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 8;

    [SerializeField] private float _cellSize = 1f;
    
    [SerializeField] private float _slotChangeSpeed = 1f;

    [SerializeField] private GameObject _gemPrefab;
    [SerializeField] private GemTypeSO[] _gemTypes; 

    [SerializeField] private GridSlot[] _slots;

    private Gem[] _gems;

    private Gem _currentGem = null;

    private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();
    public static EventSystem CurrentEventSystem { get; private set; }

    private void Awake()
    {
        for(int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Setup(this, i);
        }

        CreateGems();
    }

    private void CreateGems()
    {
        _gems = new Gem[WIDTH * HEIGHT];
        for (int i = 0; i < _gems.Length; i++)
        {
            var newGem = Instantiate(_gemPrefab).GetComponent<Gem>();

            var initialPostion = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            var gemType = _gemTypes[Random.Range(0, _gemTypes.Length)];            

            newGem.Setup(this, initialPostion, gemType, _slots[i]);

            _gems[i] = newGem;
        }
    }

    private void OnEnable()
    {
        CurrentEventSystem = EventSystem.current;
        for (int i = 0; i < _gems.Length; i++)
        {
            _gems[i].transform.position = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            _gems[i].GoToSlot(_slots[i]);
        }
    }

    public GridSlot GetGridSlot(int x, int y)
    {
        var aux = x + (y * 8);
        if (aux <= _slots.Length)
        {
            return _slots[aux];
        }
        else
        {
            return null;
        }
    }

    public void GemSelected(Gem nextGem)
    {
        if (GemsCanBeChanged(_currentGem, nextGem))
        {
            ChangeGemPlaces(_currentGem, nextGem);
            _currentGem = null;
        }
        else
        {
            _currentGem = nextGem;
        }
    }

    private bool GemsCanBeChanged(Gem current, Gem other) => current != null && current != other && AreAdjacent(current.Index, other.Index);

    private void ChangeGemPlaces(Gem current, Gem other)
    {
        var currentSlot = _slots[current.Index];
        var otherSlot = _slots[other.Index];

        current.GoToSlot(otherSlot);
        other.GoToSlot(currentSlot);
    }

    private IEnumerator AnimateSlotsChange(int from, int to)
    { 
        Transform fromTransform = _slots[from].transform;
        Transform toTransform = _slots[to].transform;
        Vector3 fromInitialPosition = fromTransform.position;
        Vector3 toInitialPosition = toTransform.position;

        float transitionIndex = 0f;
        while (1f - transitionIndex > 0.001f)
        {
            fromTransform.position = Vector3.Lerp(fromInitialPosition, toInitialPosition, transitionIndex);
            toTransform.position = Vector3.Lerp(toInitialPosition, fromInitialPosition, transitionIndex);

            yield return _waitForEndOfFrame;

            transitionIndex += Time.deltaTime * _slotChangeSpeed;
        }

        fromTransform.position = toInitialPosition;
        toTransform.position = fromInitialPosition;
    }

    private bool AreAdjacent(int index, int other)
    {
        Vector2 indexOnArray = new Vector2(index % 8, (index/8));
        Vector2 otherOnArray = new Vector2(other % 8, (other/8));

        return Mathf.Abs(1f - (indexOnArray - otherOnArray).magnitude) < 0.001f;
    }

    private void OnValidate()
    {
        for (int y = 0; y < HEIGHT; y++)
        {
            for (int x = 0; x < WIDTH; x++)
            {
                _slots[x + (y * 8)].transform.position = this.transform.position + (new Vector3(x, y, 0) * _cellSize);
            }
        }
    }
}
