using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DistanceMapRule
{
    [Min(0)]
    public int minDistance;

    [Min(0)]
    public int maxDistance;

    public List<WeightedItem<MapPreset>> possibleMaps = new List<WeightedItem<MapPreset>>();
}
