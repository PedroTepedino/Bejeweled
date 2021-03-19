using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Random = UnityEngine.Random;

public class GameManager : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 8;

    [SerializeField] private float _cellSize = 1f;
    
    [SerializeField] private float _slotChangeSpeed = 1f;

    [SerializeField] private GameObject _gemPrefab;
    [SerializeField] private GemTypeScriptableObject[] _gemTypes; 

    [SerializeField] private GridSlot[] _slots;

    private Gem[] _gems;

    private int _currentSelectedIndex = -1;

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
            newGem.SetGemType(_gemTypes[Random.Range(0, _gemTypes.Length)]);

            _gems[i] = newGem;

            newGem.transform.position = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            newGem.GoToSlot(_slots[i]);
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

    public void SlotSelected(int slotIndex)
    {
        if (_currentSelectedIndex >= 0 && _currentSelectedIndex != slotIndex)
        {
            TryChangeSlots(_currentSelectedIndex, slotIndex);
            _currentSelectedIndex = -1;
        }
        else
        {
            _currentSelectedIndex = slotIndex;
        }
    }

    private void TryChangeSlots(int currentSelectedIndex, int slotIndex)
    {
        if (!IsAdjacent(currentSelectedIndex, slotIndex)) return;

        StartCoroutine(AnimateSlotsChange(currentSelectedIndex, slotIndex));

        _slots[_currentSelectedIndex].SetIndex(slotIndex);
        _slots[slotIndex].SetIndex(currentSelectedIndex);

        var selectedSlot = _slots[currentSelectedIndex];
        _slots[currentSelectedIndex] = _slots[slotIndex];
        _slots[slotIndex] = selectedSlot;
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

    private bool IsAdjacent(int index, int other)
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
