using UdonSharp;
using UnityEngine;

public class SimpleObjectPool : UdonSharpBehaviour
{
    [Header("Pool")]
    public GameObject prefab;
    public Transform parentForSpawned;
    public int size = 16;

    [Header("Runtime")]
    public GameObject[] items;
    public bool[] inUse;

    void Start()
    {
        if (items == null || items.Length != size)
        {
            items = new GameObject[size];
            inUse = new bool[size];
            for (int i = 0; i < size; i++)
            {
                GameObject go = GameObject.Instantiate(prefab, parentForSpawned);
                go.SetActive(false);
                items[i] = go;
                inUse[i] = false;
            }
        }
    }

    public bool TrySpawn(out GameObject instance)
    {
        for (int i = 0; i < size; i++)
        {
            if (!inUse[i])
            {
                inUse[i] = true;
                instance = items[i];
                return true;
            }
        }
        instance = null;
        return false;
    }

    public void Despawn(GameObject instance)
    {
        for (int i = 0; i < size; i++)
        {
            if (items[i] == instance)
            {
                inUse[i] = false;
                instance.SetActive(false);
                return;
            }
        }
    }
}