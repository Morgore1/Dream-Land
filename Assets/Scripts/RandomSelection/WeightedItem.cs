using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeightedItem<T>
{
    public T item;
    [Min(0)]
    public int weight = 1;
}
