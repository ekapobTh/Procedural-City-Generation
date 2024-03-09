using System.Collections.Generic;
using UnityEngine;

public class PoolingObjects<T> where T : MonoBehaviour
{
    private Queue<T> availablePool = new Queue<T>();
    private List<T> occupiedPool = new List<T>();

    public T GetFromPool()
    {
        T item;

        if (availablePool.Count > 0)
            item = availablePool.Dequeue();
        else
        {
            item = availablePool.Dequeue();
        }

        occupiedPool.Add(item);
        item.gameObject.SetActive(true);

        return item;
    }

    public void ReturnToPool(T item)
    {
        occupiedPool.Remove(item);
        availablePool.Enqueue(item);
        item.gameObject.SetActive(false);
    }
}
