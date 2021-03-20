using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;
using System.Linq;

public class GridManager : MonoBehaviour
{
    public const int WIDTH = 8;
    public const int HEIGHT = 8;

    [SerializeField] private float _cellSize = 1f;

    [SerializeField] private GameObject _gemPrefab;
    [SerializeField] private GemTypeSO[] _gemTypes;

    private Gem[] _gems;
    [SerializeField] private GridSlot[] _slots;
    public GridSlot[] Slots => _slots;

    private Gem _currentGem = null;
    private Queue<Gem> _disabledGems = new Queue<Gem>();

    private bool IsChangeHappening = false;
    private GemChangePair _currentChange;

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

            SpawnGem(newGem, _slots[i]);

            _gems[i] = newGem;
        }
    }

    private IEnumerator Start()
    {
        for (int i = 0; i < _gems.Length; i++)
        {
            _gems[i].transform.position = _slots[i].transform.position + (Vector3.up * _cellSize * 8f);
            _gems[i].GoToSlot(_slots[i]);
            yield return null;
        }
        Debug.Log("Pass");

        while (_gems[0].IsMoving)
        {
            yield return null;
        }

        Debug.Log("Gems finished moving");

        for(int i = 0; i < _gems.Length; i++)
        { 
            yield return null;
            if (!_gems[i].IsEnabled)
                continue;

            if (CheckForSequence(_gems[i], out Gem[] gemsInSequence))
            {
                foreach(var g in gemsInSequence)
                {
                    yield return null;
                    g.DisableGem();
                    _disabledGems.Enqueue(g);
                }

                yield return RefillBoard();

                i = 0;
            }
        }
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
        var indexInList= x + (y * 8);
        if (indexInList <= _slots.Length)
        {
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
        if (!IsChangeHappening && GemsCanBeChanged(_currentGem, nextGem))
        {
            ChangeGems(_currentGem, nextGem);
            _currentGem = null;
        }
        else
        {
            _currentGem = nextGem;
        }
    }

    private bool GemsCanBeChanged(Gem current, Gem other) => current != null && current != other && AreAdjacent(current.Index, other.Index);

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
        var currentSlot = _slots[current.Index];
        var otherSlot = _slots[other.Index];

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
            foreach (var gem in gemsA)
            {
                gem.DisableGem();
                _disabledGems.Enqueue(gem);
            }

            foreach (var gem in gemsB)
            {
                gem.DisableGem();
                _disabledGems.Enqueue(gem);
            }

            StartCoroutine(WhileSequenceExist());
        }
        else
        {
            ChangeBack(_currentChange);
        }
    }

    private IEnumerator WhileSequenceExist()
    {
        yield return RefillBoard();

        for (int i = 0; i < _gems.Length; i++)
        {
            yield return null;
            if (!_gems[i].IsEnabled)
                continue;

            if (CheckForSequence(_gems[i], out Gem[] gemsInSequence))
            {
                foreach (var g in gemsInSequence)
                {
                    yield return null;
                    g.DisableGem();
                    _disabledGems.Enqueue(g);
                }

                yield return RefillBoard();

                i = 0;
            }
        }
    }

    private void ShiftGemsDown()
    {
        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].CurrentGem == null)
            {
                var index = ToMatrixIndex(_slots[i].Index);
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
        var initialIndex = ToMatrixIndex(gem.Index);

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
        var initialIndex = ToMatrixIndex(gem.Index);

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
        var initialIndex = ToMatrixIndex(gem.Index);

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

    public Vector2Int ToMatrixIndex(int Listindex) => new Vector2Int(Listindex % WIDTH, Listindex / HEIGHT);
  
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
