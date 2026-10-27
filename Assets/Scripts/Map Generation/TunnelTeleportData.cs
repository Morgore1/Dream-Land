using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ProceduralMapSide
{
    Left,
    Right
}

[System.Serializable]
public class TunnelTeleportData
{
    public Vector2Int destination;
    public bool toProceduralMap;// true = to procedural map, false = to main map
    public List<WeightedItem<MapPreset>> mapPreset;

    [Tooltip("Side of the procedural world this gate enters. Initial coordinates are (-1, 0) for Left and (1, 0) for Right.")]
    public ProceduralMapSide side = ProceduralMapSide.Left;

    public Vector2Int direction;
    public TunnelTeleportData()
    {
        mapPreset = new List<WeightedItem<MapPreset>>();
    }
}

