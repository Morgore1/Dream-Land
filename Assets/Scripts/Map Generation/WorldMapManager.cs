using System.Collections.Generic;
using UnityEngine;

public class WorldMapManager : MonoBehaviour
{
    public static WorldMapManager Instance;

    public DirectionMapRule mapRules;

    private Dictionary<Vector2Int, MapPreset> generatedMaps =
        new Dictionary<Vector2Int, MapPreset>();

    void Awake()
    {
        Instance = this;
    }

    public MapPreset GetMapForCurrentPosition()
    {
        Vector2Int coord = WorldCoordinateManager.Instance.worldCoord;

        if (generatedMaps.ContainsKey(coord))
            return generatedMaps[coord];

        foreach (var rule in mapRules.rules)
        {
            if (coord.x >= rule.minX && coord.x <= rule.maxX &&
                coord.y >= rule.minY && coord.y <= rule.maxY)
            {
                MapPreset chosen = WeightedRandom.Pick(rule.possibleMaps);
                generatedMaps[coord] = chosen;
                return chosen;
            }
        }

        Debug.LogWarning("No rule matched for coord: " + coord);

        MapPreset fallback = WeightedRandom.Pick(mapRules.rules[0].possibleMaps);
        generatedMaps[coord] = fallback;
        return fallback;
    }
}