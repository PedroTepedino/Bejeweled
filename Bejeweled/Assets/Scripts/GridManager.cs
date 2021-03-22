using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using Random = UnityEngine.Random;
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

    [SerializeField] private GridSlot[] _slots;

    [SerializeField] private EffectsPoolling _effectsPooling;

    private Gem _currentGem = null;

    private bool IsChangeHappening = false;
    private GemChangePair _currentChange;

    private Gem[] _possibleSequence;
    private float _inactivityTimer = 0f;
    [SerializeField] private float _timeInectiveBeforeHint = 1f;

    public event Action<Gem> OnCurrentGemChanged;

    private GemGrid _grid;

    private void Awake()
    {
        _grid = new GemGrid(WIDTH, HEIGHT, _cellSize, _gemPrefab, _slots, this);

        _gemsTypesDict = new Dictionary<int, GemTypeSO>();

        foreach(var gemType in _gemTypes)
        {
            _gemsTypesDict.Add(gemType.Type, gemType);
        }
    }

    private IEnumerator Start()
    {
        MainCam = Camera.main;

        yield return _grid.MoveAllGemsToCurrentSlots();

        while (_grid.IsAnyGemMoving)
        {
            yield return null;
        }

        yield return _grid.FindAndRemoveSequences();
    }

    private void Update()
    {
        if (_grid.IsAnyGemMoving)
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
            bool sequenceFound = _grid.FindPossibleSequence(out _possibleSequence);

            if (sequenceFound)
            {
                foreach (var gem in _possibleSequence)
                {
                    gem.SetBlinkState(true);
                }
            }
        }

        if (!IsChangeHappening) return;

        if (_currentChange.BothDone)
        {
            FinishChangeProcess();
        }
    }

    public void SpawnGem(Gem gemToSpawn, GridSlot initialSlot)
    {
        var initialPostion = initialSlot.transform.position + (Vector3.up * _cellSize * 8f);
        var gemType = _gemTypes[Random.Range(0, _gemTypes.Length)];

        gemToSpawn.Setup(this, initialPostion, gemType, initialSlot);
    }

    public void GemSelected(Gem nextGem)
    {
        if (_grid.IsAnyGemMoving) return; 

        if (!IsChangeHappening && _grid.GemsCanBeChanged(_currentGem, nextGem))
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
        var index = gem.MatrixIndex + neighbourDirection;

        DeselectGems();

        GemSelected(gem);
        GemSelected(_grid.GetGemInSlot(index.x, index.y));
    }

    public void DeselectGems()
    {
        _currentGem = null;
        OnCurrentGemChanged?.Invoke(_currentGem);
    }

    public void ChangeGems(Gem current, Gem other)
    {
        _grid.ChangeGemPositions(current, other);

        current.MarkAsChanging();
        other.MarkAsChanging();

        IsChangeHappening = true;
        _currentChange = new GemChangePair(current, other);
    }

    private void FinishChangeProcess()
    {
        IsChangeHappening = false;

        var gemAInSequence = _grid.CheckForSequence(_currentChange.GemA, out Gem[] gemsA);
        var gemBInSequence = _grid.CheckForSequence(_currentChange.GemB, out Gem[] gemsB);

        if (gemAInSequence || gemBInSequence)
        {
            StartCoroutine(_grid.FindAndRemoveSequences()) ;
        }
        else
        {
            _grid.ChangeBack(_currentChange);
        }
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
}

public struct GemChangePair
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
