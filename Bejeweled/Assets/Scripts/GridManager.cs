using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;
using System.Linq;
using System;

public class GridManager : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 8;

    [SerializeField] private float _cellSize = 1f;
    public float CellSize => _cellSize;

    public static Camera MainCam;

    [SerializeField] private GameObject _gemPrefab;
    [SerializeField] private GemTypeSO[] _gemTypes;
    private Dictionary<int, GemTypeSO> _gemsTypesDict;

    private Gem[] _gems;
    [SerializeField] private GridSlot[] _slots;
    public GridSlot[] Slots => _slots;

    [SerializeField] private EffectsPoolling _effectsPooling;
    public EffectsPoolling EffectsPool => _effectsPooling;

    private Gem _currentGem = null;
    private Queue<Gem> _disabledGems = new Queue<Gem>();

    private bool IsChangeHappening = false;
    private GemChangePair _currentChange;

    private Gem[] _possibleSequence;
    private float _inactivityTimer = 0f;
    [SerializeField] private float _timeInectiveBeforeHint = 1f;

    public event Action<Gem> OnCurrentGemChanged;

    private void Awake()
    {
        for(int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Setup(this, i);
        }

        CreateGems();

        _gemsTypesDict = new Dictionary<int, GemTypeSO>();

        foreach(var gemType in _gemTypes)
        {
            _gemsTypesDict.Add(gemType.Type, gemType);
        }
    }

    private void CreateGems()
    {
        _gems = new Gem[WIDTH * HEIGHT];
        for (int i = 0; i < _gems.Length; i++)
        {
            var newGem = Instantiate(_gemPrefab).GetComponent<Gem>();

            SpawnGem(newGem, _slots[i]);

            _gems[i] = newGem;
        }
    }

    private IEnumerator Start()
    {
        MainCam = Camera.main;

        for (int i = 0; i < _gems.Length; i++)
        {
            _gems[i].transform.position = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            _gems[i].GoToSlot(_slots[i]);
            yield return null;
        }

        while (_gems[0].IsMoving)
        {
            yield return null;
        }

        yield return FindAndRemoveSequences();
    }

    private IEnumerator FindAndRemoveSequences()
    {
        var sequenceFound = false;
        do
        {
            yield return null;

            sequenceFound = FindSequences(out Gem[] gemsToDisapear);

            yield return null;

            if (sequenceFound)
            {
                foreach (var gem in gemsToDisapear)
                {
                    gem.DisableGem();
                    _disabledGems.Enqueue(gem);

                    yield return null;
                }
            }

            yield return RefillBoard();

        } while (sequenceFound);
    }

    private bool FindSequences(out Gem[] gemsInSequences)
    {
        var gemsToDisapear = new List<Gem>();
        var sequenceFound = false;
        foreach (var gem in _gems)
        {
            var hasSequence = CheckForSequence(gem, out Gem[] gemsInSequence);

            if (hasSequence)
            {
                foreach (var g in gemsInSequence.Where(gem => !gemsToDisapear.Contains(gem)))
                {
                    gemsToDisapear.Add(g);
                }

                sequenceFound = true;
            }
        }

        gemsInSequences = gemsToDisapear.ToArray();

        return sequenceFound;
    }

    private IEnumerator RefillBoard()
    {
        ShiftGemsDown();

        foreach (var slot in _slots)
        {
            if (slot.CurrentGem == null)
            {
                SpawnGem(_disabledGems.Dequeue(), slot);
            }

            yield return null;
        }

        while (_gems.ToList().Where(g => g.IsEnabled).Any(g => g.IsMoving))
        {
            yield return null;
        }
    }

    private void Update()
    {
        if (_gems.Any(g => g.IsMoving))
        {
            if (_possibleSequence != null)
            {
                for (int i = 0; i < _possibleSequence.Length; i++)
                {
                    _possibleSequence[i].SetBlinkState(false);
                }
            }

            _possibleSequence = null;
            _inactivityTimer = 0;
        }
        else
        {
            _inactivityTimer += Time.deltaTime;
        }

        if (_inactivityTimer > _timeInectiveBeforeHint && _possibleSequence == null)
        {
            bool sequenceFound = FindPossibleSequence(out _possibleSequence); 

            if (sequenceFound )
            {
                Debug.LogWarning("Found Hint");
            }
        }

        if (!IsChangeHappening) return;

        if (_currentChange.BothDone)
        {
            FinishChangeProcess();
        }
    }

    private void SpawnGem(Gem gemToSpawn, GridSlot initialSlot)
    {
        var initialPostion = initialSlot.transform.position + (Vector3.up * _cellSize * 8f);
        var gemType = _gemTypes[Random.Range(0, _gemTypes.Length)];

        gemToSpawn.Setup(this, initialPostion, gemType, initialSlot);
    }

    private Gem GetGemInSlot(int x, int y)
    {
        if (x < 0 || x >= WIDTH || y < 0 || y >= HEIGHT) return null;

        var indexInList= x + (y * 8);
        if (indexInList < _slots.Length && indexInList >= 0 )
        {
            Debug.Log(indexInList);
            return _slots[indexInList].CurrentGem;
        }
        else
        {
            return null;
        }
    }

    private GridSlot GetSlot(int x, int y)
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

    public void GemSelected(Gem nextGem)
    {
        if (_gems.Any(gem => gem.IsMoving)) return; 

        if (!IsChangeHappening && GemsCanBeChanged(_currentGem, nextGem))
        {
            ChangeGems(_currentGem, nextGem);
            _currentGem = null;
        }
        else
        {
            _currentGem = nextGem;
        }

        OnCurrentGemChanged?.Invoke(_currentGem);
    }

    public void SelectNeighbourGem(Gem gem, Vector2Int neighbourDirection)
    {
        var index = gem.Index + neighbourDirection;

        DeselectGems();

        GemSelected(gem);
        GemSelected(GetGemInSlot(index.x, index.y));
    }

    public void DeselectGems()
    {
        _currentGem = null;
        OnCurrentGemChanged?.Invoke(_currentGem);
    }

    private bool GemsCanBeChanged(Gem current, Gem other) => current != null && current != other && AreAdjacent(current.ListIndex, other.ListIndex);

    public void ChangeGems(Gem current, Gem other)
    {
        ChangeGemPostions(current, other);

        current.MarkAsChanging();
        other.MarkAsChanging();

        IsChangeHappening = true;
        _currentChange = new GemChangePair(current, other);
    }

    private void ChangeGemPostions(Gem current, Gem other)
    {
        var currentSlot = _slots[current.ListIndex];
        var otherSlot = _slots[other.ListIndex];

        current.GoToSlot(otherSlot);
        other.GoToSlot(currentSlot);
    }
    
    private void ChangeBack(GemChangePair change)
    {
        ChangeGemPostions(change.GemA, change.GemB);
    }

    private void FinishChangeProcess()
    {
        IsChangeHappening = false;

        var gemAInSequence = CheckForSequence(_currentChange.GemA, out Gem[] gemsA);
        var gemBInSequence = CheckForSequence(_currentChange.GemB, out Gem[] gemsB);

        if (gemAInSequence || gemBInSequence)
        {
            StartCoroutine(FindAndRemoveSequences());
        }
        else
        {
            ChangeBack(_currentChange);
        }
    }

    private void ShiftGemsDown()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].CurrentGem == null)
            {
                var index = ListToMatrixIndex(_slots[i].Index);
                for (int y = index.y + 1; y < HEIGHT; y++)
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

    private bool CheckForSequence(Gem gem, out Gem[] sequenceList)
    {
        List<Gem> gemsInsequence = new List<Gem>();
        gemsInsequence.Add(gem);

        foreach(Gem g in CheckCollums(gem))
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
        var initialIndex = gem.Index;

        List<Gem> gemsInsequence = new List<Gem>();
        int verticalCount = 1;

        for (int y = initialIndex.y + 1; y < HEIGHT; y++)
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
        var initialIndex = gem.Index;

        List<Gem> gemsInsequence = new List<Gem>();
        int horizontalCount = 1;

        for (int x = initialIndex.x + 1; x < HEIGHT; x++)
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

    private bool FindPossibleSequence(out Gem[] gemsInPossibleSequence)
    {
        foreach (var centerGem in _gems)
        {
            var neighbourGems = GetNeighbourGemsOfTheSameType(centerGem); // this includes the current Gem

            if (neighbourGems.Length > 1)
            {
                List<Tuple<Gem, Gem>> gemPairs = new List<Tuple<Gem, Gem>>();
                for (int i = 0; i < neighbourGems.Length; i++)
                {
                    for(int j = i + 1; j < neighbourGems.Length; j++)
                    {
                        if (neighbourGems[i].Index.x == neighbourGems[j].Index.x || neighbourGems[i].Index.y == neighbourGems[j].Index.y)
                        {
                            if (neighbourGems[i].Index.x > neighbourGems[j].Index.x || neighbourGems[i].Index.y > neighbourGems[j].Index.y)
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

                foreach(var pair in gemPairs)
                {
                    foreach(var otherGem in neighbourGems.Where(g => g != pair.Item1 && g != pair.Item2))
                    {
                        if (otherGem.Index.x != pair.Item1.Index.x &&
                            otherGem.Index.y != pair.Item1.Index.y &&
                            otherGem.Index.x != pair.Item2.Index.x &&
                            otherGem.Index.y != pair.Item2.Index.y)
                        {
                            otherGem.SetBlinkState(true);
                            pair.Item1.SetBlinkState(true);
                            pair.Item2.SetBlinkState(true);
                            gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem};
                            return true;
                        }
                    }

                    if ((pair.Item1.Index - pair.Item2.Index).magnitude == 1)
                    {
                        if (pair.Item1.Index.y == pair.Item2.Index.y)
                        {
                            var otherGem = GetGemInSlot(pair.Item1.Index.x + 2, pair.Item1.Index.y);

                            if (otherGem != null && otherGem.Type == pair.Item1.Type)
                            {
                                otherGem.SetBlinkState(true);
                                pair.Item1.SetBlinkState(true);
                                pair.Item2.SetBlinkState(true);
                                gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem };
                                return true;
                            }
                            else
                            {
                                otherGem = GetGemInSlot(pair.Item2.Index.x - 2, pair.Item1.Index.y);
                                if (otherGem != null && otherGem.Type == pair.Item1.Type)
                                {
                                    otherGem.SetBlinkState(true);
                                    pair.Item1.SetBlinkState(true);
                                    pair.Item2.SetBlinkState(true);
                                    gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem };
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            var otherGem = GetGemInSlot(pair.Item1.Index.x, pair.Item1.Index.y + 2);

                            if (otherGem != null && otherGem.Type == pair.Item1.Type)
                            {
                                otherGem.SetBlinkState(true);
                                pair.Item1.SetBlinkState(true);
                                pair.Item2.SetBlinkState(true);
                                gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem };
                                return true;
                            }
                            else
                            {
                                otherGem = GetGemInSlot(pair.Item1.Index.x, pair.Item2.Index.y - 2);
                                if (otherGem != null && otherGem.Type == pair.Item1.Type)
                                {
                                    otherGem.SetBlinkState(true);
                                    pair.Item1.SetBlinkState(true);
                                    pair.Item2.SetBlinkState(true);
                                    gemsInPossibleSequence = new Gem[] { pair.Item1, pair.Item2, otherGem };
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
        }

        gemsInPossibleSequence = new Gem[0];
        return false;
    }

    private Gem[] GetNeighbourGemsOfTheSameType(Gem gem)
    {
        List<Gem> gemsWithSameType = new List<Gem>();
        gemsWithSameType.Add(gem);
        var index = gem.Index;
        for(int x = -1; x <= 1; x++)
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

    public Vector2Int ListToMatrixIndex(int Listindex) => new Vector2Int(Listindex % WIDTH, Listindex / HEIGHT);
  
    private bool AreAdjacent(int index, int other)
    {
        Vector2 indexOnArray = new Vector2(index % 8, (index/8));
        Vector2 otherOnArray = new Vector2(other % 8, (other/8));

        return Mathf.Abs(1f - (indexOnArray - otherOnArray).magnitude) < 0.001f;
    }

    public void SpawnExplosionEffect(Gem gem)
    {
        _effectsPooling.SpawnObj(_gemsTypesDict[gem.Type], gem.transform.position);
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

        if (_effectsPooling == null)
        {
            _effectsPooling = FindObjectOfType<EffectsPoolling>();
        }
    }

    private struct GemChangePair
    { 
        public GemChangePair(Gem gemA, Gem gemB)
        {
            GemA = gemA;
            GemB = gemB;
        }

        public Gem GemA { get; }
        public Gem GemB { get; }

        public bool BothDone => !GemA.IsChanging && !GemB.IsChanging;
    }
}
