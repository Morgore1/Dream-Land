using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DistanceMapRule
{
    public int minX;
    public int maxX;

    public int minY;
    public int maxY;

    public List<WeightedItem<MapPreset>> possibleMaps;
}
