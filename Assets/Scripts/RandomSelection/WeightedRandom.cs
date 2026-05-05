using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class WeightedRandom
{
    public static T Pick<T>(List<WeightedItem<T>> items)
    {
        int totalWeight = 0;

        foreach (var i in items)
            totalWeight += i.weight;

        int rand = Random.Range(0, totalWeight);

        foreach (var i in items)
        {
            rand -= i.weight;
            if (rand < 0)
                return i.item;
        }

        return default;
    }
}
