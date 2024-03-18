using System.Collections.Generic;
using UnityEngine;

public class PoolingObjects<T> where T : MonoBehaviour
{
    private T spawnObject;
    private Transform spawnParent;

    private Queue<T> availablePool = new Queue<T>();
    private List<T> occupiedPool = new List<T>();

    public PoolingObjects(T spawnObject, Transform spawnParent)
    {
        this.spawnObject = spawnObject;
        this.spawnParent = spawnParent;
    }

    public T GetFromPool()
    {
        T item = null;

        if (availablePool.Count > 0)
            item = availablePool.Dequeue();
        else
        {
            if (spawnObject && spawnParent)
                item = Object.Instantiate(spawnObject, spawnParent) as T;
            else
                Debug.LogWarning("No Prefab to spawn or Parent to set");
        }

        if (item != null)
        {
            occupiedPool.Add(item);
            item.gameObject.SetActive(true);
        }

        return item;
    }

    public void ReturnToPool(T item)
    {
        item.gameObject.SetActive(false);
        occupiedPool.Remove(item);
        availablePool.Enqueue(item);
    }
}
