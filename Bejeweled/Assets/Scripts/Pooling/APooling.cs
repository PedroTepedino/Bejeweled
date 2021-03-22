using UnityEngine;

public abstract class APooling<T> : MonoBehaviour where T: MonoBehaviour
{
    [Tooltip("Number of objects should spawn prior to usage")]
    [SerializeField] protected int _prewarmValue = 3;
    [SerializeField] protected GameObject _objectPrefab;

    protected Pool<T> _pool;

    protected void Awake()
    {
        _pool = new Pool<T>(ObjFactory, _prewarmValue);        
    }

    public T SpawnObj(Vector3 position)
    {
        var obj = _pool.GetObject();
        obj.transform.position = position;
        return obj;
    }

    public T ObjFactory()
    {
        var obj = Instantiate(_objectPrefab).GetComponent<T>();
        obj.gameObject.SetActive(false);
        return obj;
    }

    protected void OnValidate()
    {
        if (_objectPrefab != null && _objectPrefab.GetComponent<T>() == null)
        {
            Debug.LogError($"{this.gameObject} Object prefab Missing {typeof(T).Name} Component", this.gameObject); 
        }
    }
}
