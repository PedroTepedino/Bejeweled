using System;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class Grid : MonoBehaviour
{
    [SerializeField] private float _slotChangeSpeed = 1f;
    [SerializeField] private Vector2Int _gridSize = Vector2Int.one;
    [SerializeField] private GameObject _slotPrefab;

    [SerializeField] private GridSlot[] _slots;

    private int _currentSelectedIndex = -1;

    private readonly WaitForEndOfFrame _waitForEndOfFrame = new WaitForEndOfFrame();

    public static EventSystem CurrentEventSystem { get; private set; }

    private void Awake()
    {
        for(int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Setup(this, i);
        }
    }

    private void OnEnable()
    {
        CurrentEventSystem = EventSystem.current;
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
        //var selectedSlotPosition = _slots[currentSelectedIndex].transform.position;
        //_slots[_currentSelectedIndex].transform.position = _slots[slotIndex].transform.position;
        //_slots[slotIndex].transform.position = selectedSlotPosition;
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
}
