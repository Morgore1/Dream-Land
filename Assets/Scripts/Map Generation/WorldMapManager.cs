using System.Collections.Generic;
using UnityEngine;

public class WorldMapManager : MonoBehaviour
{
    public static WorldMapManager Instance;

    public DirectionMapRule mapRules;

    [Range(0f, 1f)]
    public float mixChance = 1f;

    [System.Serializable]
    public class MixedMapSelection
    {
        public MapPreset primary;
        public MapPreset secondary;
        public bool isMixed;
        public float primaryRatio = 0.7f;
        public float secondaryRatio = 0.3f;
    }

    private Dictionary<Vector2Int, MapPreset> generatedMaps =
        new Dictionary<Vector2Int, MapPreset>();

    private Dictionary<Vector2Int, MixedMapSelection> generatedSelections =
        new Dictionary<Vector2Int, MixedMapSelection>();

    void Awake()
    {
        Instance = this;
    }

    public MapPreset GetMapForCurrentPosition()
    {
        return GetMapSelectionForCurrentPosition().primary;
    }

    public MixedMapSelection GetMapSelectionForCurrentPosition(List<WeightedItem<MapPreset>> explicitMaps = null)
    {
        Vector2Int coord = WorldCoordinateManager.Instance.worldCoord;

        if (generatedSelections.ContainsKey(coord))
            return generatedSelections[coord];

        List<WeightedItem<MapPreset>> candidateMaps = explicitMaps;
        if (candidateMaps == null || candidateMaps.Count == 0)
        {
            candidateMaps = GetCandidateMapsForCoord(coord);
        }

        if (candidateMaps == null || candidateMaps.Count == 0)
        {
            Debug.LogWarning("No map candidates found for coord: " + coord);
            return null;
        }

        MapPreset primary = WeightedRandom.Pick(candidateMaps);
        var selection = new MixedMapSelection
        {
            primary = primary,
            primaryRatio = 1f,
            secondaryRatio = 0f,
            isMixed = false
        };

        if (candidateMaps.Count > 1 && Random.value <= mixChance)
        {
            var secondaryCandidates = new List<WeightedItem<MapPreset>>();
            foreach (var candidate in candidateMaps)
            {
                if (candidate.item != null && candidate.item != primary)
                    secondaryCandidates.Add(candidate);
            }

            if (secondaryCandidates.Count > 0)
            {
                MapPreset secondary = WeightedRandom.Pick(secondaryCandidates);
                selection.secondary = secondary;
                selection.primaryRatio = 0.7f;
                selection.secondaryRatio = 0.3f;
                selection.isMixed = true;
            }
        }

        generatedMaps[coord] = primary;
        generatedSelections[coord] = selection;
        return selection;
    }

    private List<WeightedItem<MapPreset>> GetCandidateMapsForCoord(Vector2Int coord)
    {
        foreach (var rule in mapRules.rules)
        {
            if (coord.x >= rule.minX && coord.x <= rule.maxX &&
                coord.y >= rule.minY && coord.y <= rule.maxY)
            {
                return rule.possibleMaps;
            }
        }

        Debug.LogWarning("No rule matched for coord: " + coord);

        if (mapRules.rules != null && mapRules.rules.Count > 0)
        {
            return mapRules.rules[0].possibleMaps;
        }

        return new List<WeightedItem<MapPreset>>();
    }
}