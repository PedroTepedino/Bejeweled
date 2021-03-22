using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using Object = UnityEngine.Object;

public class GemGrid
{
    private readonly int _width;
    private readonly int _height;
    private readonly float _cellSize;

    private Gem[] _gems; 
    private GridSlot[] _slots;

    private Queue<Gem> _disabledGems;

    private GridManager _gridManager;

    //// Variables Strictilly to Find Sequences
    private Gem[] _gemsInSequences;
    private bool _foundSequences;

    public GemGrid(int width, int height,float cellSize, GameObject _gemPrefab, GridSlot[] slots, GridManager gridManager)
    {
        _width = width;
        _height = height;
        _cellSize = cellSize;

        _slots = slots;

        _gridManager = gridManager;

        _disabledGems = new Queue<Gem>();

        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Setup(this, i);
        }

        CreateGems(_gemPrefab);
    }

    private void CreateGems(GameObject gemPrefab)
    {
        _gems = new Gem[_width * _height];
        for (int i = 0; i < _gems.Length; i++)
        {
            var newGem = Object.Instantiate(gemPrefab).GetComponent<Gem>();

            _gridManager.SpawnGem(newGem, _slots[i]);

            _gems[i] = newGem;
        }
    }

    public IEnumerator MoveAllGemsToCurrentSlots()
    {
        for (int i = 0; i < _gems.Length; i++)
        {
            _gems[i].transform.position = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            _gems[i].GoToSlot(_slots[i]);
            yield return null;
        }
    }

    public Gem GetGemInSlot(Vector2Int index) => GetGemInSlot(index.x, index.y);
    
    public Gem GetGemInSlot(int x, int y)
    {
        if (x < 0 || x >= _width || y < 0 || y >= _height) return null;

        var indexInList = x + (y * 8);
        if (indexInList < _slots.Length && indexInList >= 0)
        {
            Debug.Log(indexInList);
            return _slots[indexInList].CurrentGem;
        }
        else
        {
            return null;
        }
    }

    public GridSlot GetSlot(int x, int y)
    {
        var indexInList = x + (y * 8);
        if (indexInList <= _slots.Length)
        {
            return _slots[indexInList];
        }
        else
        {
            return null;
        }
    }

    public bool GemsCanBeChanged(Gem current, Gem other) => current != null && current != other && AreAdjacent(current.ListIndex, other.ListIndex);

    public Vector2Int ListToMatrixIndex(int Listindex) => new Vector2Int(Listindex % _width, Listindex / _height);
    
    private bool AreAdjacent(int index, int other)
    {
        Vector2 indexOnArray = new Vector2(index % 8, (index / 8));
        Vector2 otherOnArray = new Vector2(other % 8, (other / 8));

        return Mathf.Abs(1f - (indexOnArray - otherOnArray).magnitude) < 0.001f;
    }

    public void ChangeGemPositions(Gem current, Gem other)
    {
        var currentSlot = _slots[current.ListIndex];
        var otherSlot = _slots[other.ListIndex];

        current.GoToSlot(otherSlot);
        other.GoToSlot(currentSlot);
    }

    public void ChangeBack(GemChangePair change)
    {
        ChangeGemPositions(change.GemA, change.GemB);
    }

    public bool CheckForSequence(Gem gem, out Gem[] sequenceList)
    {
        List<Gem> gemsInsequence = new List<Gem>();
        gemsInsequence.Add(gem);

        foreach (Gem g in CheckCollums(gem))
        {
            gemsInsequence.Add(g);
        }

        foreach (Gem g in CheckRows(gem))
        {
            gemsInsequence.Add(g);
        }

        if (gemsInsequence.Count >= 3)
        {
            sequenceList = gemsInsequence.ToArray();
            return true;
        }
        else
        {
            sequenceList = new Gem[0];
            return false;
        }
    }

    private Gem[] CheckCollums(Gem gem)
    {
        var initialIndex = gem.MatrixIndex;

        List<Gem> gemsInsequence = new List<Gem>();
        int verticalCount = 1;

        for (int y = initialIndex.y + 1; y < _height; y++)
        {
            var nextGem = GetGemInSlot(initialIndex.x, y);

            if (gem.Type != nextGem.Type)
                break;

            gemsInsequence.Add(nextGem);
            verticalCount++;
        }

        for (int y = initialIndex.y - 1; y >= 0; y--)
        {
            var nextGem = GetGemInSlot(initialIndex.x, y);

            if (gem.Type != nextGem.Type)
                break;

            gemsInsequence.Add(nextGem);
            verticalCount++;
        }

        if (verticalCount >= 3)
        {
            return gemsInsequence.ToArray();
        }
        else
        {
            return new Gem[0];
        }
    }

    private Gem[] CheckRows(Gem gem)
    {
        var initialIndex = gem.MatrixIndex;

        List<Gem> gemsInsequence = new List<Gem>();
        int horizontalCount = 1;

        for (int x = initialIndex.x + 1; x < _height; x++)
        {
            var nextGem = GetGemInSlot(x, initialIndex.y);

            if (gem.Type != nextGem.Type)
                break;

            gemsInsequence.Add(nextGem);
            horizontalCount++;
        }

        for (int x = initialIndex.x - 1; x >= 0; x--)
        {
            var nextGem = GetGemInSlot(x, initialIndex.y);

            if (gem.Type != nextGem.Type)
                break;

            gemsInsequence.Add(nextGem);
            horizontalCount++;
        }

        if (horizontalCount >= 3)
        {
            return gemsInsequence.ToArray();
        }
        else
        {
            return new Gem[0];
        }
    }

    public void ShiftGemsDown()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].CurrentGem == null)
            {
                var index = ListToMatrixIndex(_slots[i].ListIndex);
                for (int y = index.y + 1; y < _height; y++)
                {
                    var gemInSlot = GetGemInSlot(index.x, y);
                    if (gemInSlot == null)
                        continue;

                    if (GetGemInSlot(index.x, y).IsEnabled)
                    {
                        var slot = GetSlot(index.x, y);
                        GetGemInSlot(index.x, y).GoToSlot(_slots[i]);
                        slot.CurrentGem = null;
                        break;
                    }
                }
            }
        }
    }

    public bool FindPossibleSequence(out Gem[] gemsInPossibleSequence)
    {
        foreach (var centerGem in _gems)
        {
            var neighbourGems = GetNeighbourGemsOfTheSameType(centerGem); // this includes the current Gem

            if (neighbourGems.Length > 1)
            {
                List<Tuple<Gem, Gem>> gemPairs = GetGemPairs(neighbourGems);

                foreach (var pair in gemPairs)
                {
                    foreach (var otherGem in neighbourGems.Where(g => g != pair.Item1 && g != pair.Item2))
                    {
                        if (otherGem.MatrixIndex.x != pair.Item1.MatrixIndex.x &&
                            otherGem.MatrixIndex.y != pair.Item1.MatrixIndex.y &&
                            otherGem.MatrixIndex.x != pair.Item2.MatrixIndex.x &&
                            otherGem.MatrixIndex.y != pair.Item2.MatrixIndex.y)
                        {
                            gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem };
                            return true;
                        }
                    }

                    if ((pair.Item1.MatrixIndex - pair.Item2.MatrixIndex).magnitude == 1)
                    {
                        // Loops for -2 and 2 only
                        for (int sum = -2; sum <= 2; sum += 4)
                        {
                            Vector2Int indexToVerify;
                            if (pair.Item1.MatrixIndex.y == pair.Item2.MatrixIndex.y)
                            {
                                indexToVerify = new Vector2Int(pair.Item1.MatrixIndex.x + sum, pair.Item1.MatrixIndex.y);
                            }
                            else
                            {
                                indexToVerify = new Vector2Int(pair.Item1.MatrixIndex.x, pair.Item1.MatrixIndex.y + sum);
                            }

                            Gem otherGemUpper = GetGemInSlot(indexToVerify);

                            if (otherGemUpper != null && otherGemUpper.Type == pair.Item1.Type)
                            {
                                gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGemUpper };
                                return true;
                            }
                        }
                    }
                }
            }
        }

        gemsInPossibleSequence = new Gem[0];
        return false;
    }
    private static List<Tuple<Gem, Gem>> GetGemPairs(Gem[] neighbourGems)
    {
        List<Tuple<Gem, Gem>> gemPairs = new List<Tuple<Gem, Gem>>();
        for (int i = 0; i < neighbourGems.Length; i++)
        {
            for (int j = i + 1; j < neighbourGems.Length; j++)
            {
                if (neighbourGems[i].MatrixIndex.x == neighbourGems[j].MatrixIndex.x || neighbourGems[i].MatrixIndex.y == neighbourGems[j].MatrixIndex.y)
                {
                    if (neighbourGems[i].MatrixIndex.x > neighbourGems[j].MatrixIndex.x || neighbourGems[i].MatrixIndex.y > neighbourGems[j].MatrixIndex.y)
                    {
                        gemPairs.Add(new Tuple<Gem, Gem>(neighbourGems[i], neighbourGems[j]));
                    }
                    else
                    {
                        gemPairs.Add(new Tuple<Gem, Gem>(neighbourGems[j], neighbourGems[i]));
                    }
                }
            }
        }
        return gemPairs;
    }
    private Gem[] GetNeighbourGemsOfTheSameType(Gem gem)
    {
        List<Gem> gemsWithSameType = new List<Gem>();
        gemsWithSameType.Add(gem);
        var index = gem.MatrixIndex;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue;

                var gemInSlot = GetGemInSlot(index.x + x, index.y + y);
                if (gemInSlot != null && gemInSlot.Type == gem.Type)
                {
                    gemsWithSameType.Add(gemInSlot);
                }
            }
        }

        return gemsWithSameType.ToArray();
    }

    public bool IsAnyGemMoving => _gems.Any(gem => gem.IsMoving);

    
    public IEnumerator FindAndRemoveSequences()
    {
        do
        {
            yield return FindSequences();

            yield return null;

            if (_foundSequences)
            {
                foreach (var gem in _gemsInSequences)
                {
                    gem.DisableGem();
                    _disabledGems.Enqueue(gem);

                    yield return null;
                }
            }

            yield return RefillBoard();

        } while (_foundSequences);
    }

    private IEnumerator FindSequences()
    {
        var gemsToDisapear = new List<Gem>();
        var sequenceFound = false;
        foreach (var gem in _gems)
        {
            yield return null;
            var hasSequence = CheckForSequence(gem, out Gem[] gemsInSequence);

            if (hasSequence)
            {
                foreach (var g in gemsInSequence.Where(gem => !gemsToDisapear.Contains(gem)))
                {

                    yield return null;
                    gemsToDisapear.Add(g);
                }

                sequenceFound = true;
            }
        }

        _gemsInSequences = gemsToDisapear.ToArray();

        _foundSequences = sequenceFound;
    }

    private IEnumerator RefillBoard()
    {
        ShiftGemsDown();

        foreach (var slot in _slots)
        {
            if (slot.CurrentGem == null)
            {
                _gridManager.SpawnGem(_disabledGems.Dequeue(), slot);
            }

            yield return null;
        }

        while (_gems.ToList().Where(g => g.IsEnabled).Any(g => g.IsMoving))
        {
            yield return null;
        }
    }
}
