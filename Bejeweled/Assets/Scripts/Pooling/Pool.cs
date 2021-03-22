using UnityEngine;
using System.Collections.Generic;

public class Pool<T> where T : MonoBehaviour
{
    private readonly System.Func<T> CreateObject;

    private Queue<T> _poolQueue;

    public Pool(System.Func<T> factory, int prewarmValue)
    {
        CreateObject = factory;

        _poolQueue = new Queue<T>();

        for (int i = 0; i < prewarmValue; i++)
        {
            
           
            _poolQueue.Enqueue(CreateObject());
        }
    }

    public T GetObject()
    {
        T obj;
        if (_poolQueue.Peek().gameObject.activeInHierarchy)
        {
            obj = CreateObject();
        }
        else
        {
            obj = _poolQueue.Dequeue();
        }

        obj.gameObject.SetActive(true);
        _poolQueue.Enqueue(obj);

        return obj;
    }
}
