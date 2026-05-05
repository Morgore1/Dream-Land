using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TunnelTeleportData
{
    public Vector2Int destination;
    public bool toProceduralMap;// true = to procedural map, false = to main map
    public List<WeightedItem<MapPreset>> mapPreset;

    public Vector2Int direction;
    public TunnelTeleportData()
    {
        mapPreset = new List<WeightedItem<MapPreset>>();
    }
}

