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

    private struct MapCacheKey
    {
        public readonly Vector2Int coordinate;
        public readonly ProceduralMapSide side;

        public MapCacheKey(Vector2Int coordinate, ProceduralMapSide side)
        {
            this.coordinate = coordinate;
            this.side = side;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is MapCacheKey))
                return false;

            MapCacheKey other = (MapCacheKey)obj;
            return coordinate == other.coordinate && side == other.side;
        }

        public override int GetHashCode() => (coordinate.GetHashCode() * 397) ^ (int)side;
    }

    private readonly Dictionary<MapCacheKey, MixedMapSelection> generatedSelections =
        new Dictionary<MapCacheKey, MixedMapSelection>();

    void Awake()
    {
        Instance = this;
    }

    private void OnValidate()
    {
        ValidateRules(ProceduralMapSide.Left, mapRules != null ? mapRules.leftRules : null);
        ValidateRules(ProceduralMapSide.Right, mapRules != null ? mapRules.rightRules : null);
    }

    public MapPreset GetMapForCurrentPosition()
    {
        MixedMapSelection selection = GetMapSelectionForCurrentPosition();
        return selection != null ? selection.primary : null;
    }

    public MixedMapSelection GetMapSelectionForCurrentPosition(List<WeightedItem<MapPreset>> explicitMaps = null)
    {
        WorldCoordinateManager coordinateManager = WorldCoordinateManager.Instance;
        if (coordinateManager == null)
        {
            Debug.LogWarning("Cannot select a procedural map because WorldCoordinateManager is missing.", this);
            return null;
        }

        Vector2Int coord = coordinateManager.worldCoord;
        ProceduralMapSide side = coordinateManager.EffectiveSide;
        var cacheKey = new MapCacheKey(coord, side);

        // Once the player is on a procedural trip, the coordinate rules always
        // determine the map. Explicit lists are only used outside that trip.
        bool hasExplicitOverride = !coordinateManager.IsInProceduralMap
            && explicitMaps != null
            && explicitMaps.Count > 0;
        if (!hasExplicitOverride && generatedSelections.TryGetValue(cacheKey, out MixedMapSelection cachedSelection))
            return cachedSelection;

        List<WeightedItem<MapPreset>> candidateMaps;
        if (hasExplicitOverride)
        {
            candidateMaps = GetValidCandidates(explicitMaps);
            if (candidateMaps.Count == 0)
            {
                Debug.LogWarning($"Explicit map override has no valid presets at {coord}.", this);
                return null;
            }
        }
        else
        {
            long distance = System.Math.Abs((long)coord.x) + System.Math.Abs((long)coord.y);
            candidateMaps = GetCandidateMapsForDistance(distance, side);
            if (candidateMaps.Count == 0)
            {
                Debug.LogWarning($"No valid map candidates configured for {side} at distance {distance} (coordinate {coord}).", this);
                return null;
            }
        }

        MapPreset primary = PickWeightedPreset(candidateMaps);
        if (primary == null)
        {
            Debug.LogWarning($"Could not select a primary map preset at {coord}.", this);
            return null;
        }

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
                MapPreset secondary = PickWeightedPreset(secondaryCandidates);
                if (secondary != null)
                {
                    selection.secondary = secondary;
                    selection.primaryRatio = 0.7f;
                    selection.secondaryRatio = 0.3f;
                    selection.isMixed = true;
                }
            }
        }

        // Explicit teleport overrides are gate-specific and must not affect
        // the distance-rule selection cached for this coordinate.
        if (!hasExplicitOverride)
            generatedSelections[cacheKey] = selection;
        return selection;
    }

    private List<WeightedItem<MapPreset>> GetCandidateMapsForDistance(long distance, ProceduralMapSide side)
    {
        if (mapRules == null)
            return new List<WeightedItem<MapPreset>>();

        List<DistanceMapRule> sideRules = side == ProceduralMapSide.Left
            ? mapRules.leftRules
            : mapRules.rightRules;

        if (sideRules == null)
            return new List<WeightedItem<MapPreset>>();

        foreach (DistanceMapRule rule in sideRules)
        {
            if (rule != null && distance >= rule.minDistance && distance <= rule.maxDistance)
            {
                return GetValidCandidates(rule.possibleMaps);
            }
        }

        return new List<WeightedItem<MapPreset>>();
    }

    private static List<WeightedItem<MapPreset>> GetValidCandidates(List<WeightedItem<MapPreset>> candidates)
    {
        var validCandidates = new List<WeightedItem<MapPreset>>();
        if (candidates == null)
            return validCandidates;

        foreach (WeightedItem<MapPreset> candidate in candidates)
        {
            if (candidate != null && candidate.item != null && candidate.weight > 0)
                validCandidates.Add(candidate);
        }

        return validCandidates;
    }

    private static MapPreset PickWeightedPreset(List<WeightedItem<MapPreset>> candidates)
    {
        double totalWeight = 0d;
        foreach (WeightedItem<MapPreset> candidate in candidates)
            totalWeight += candidate.weight;

        if (totalWeight <= 0d)
            return null;

        double roll = Random.value * totalWeight;
        foreach (WeightedItem<MapPreset> candidate in candidates)
        {
            roll -= candidate.weight;
            if (roll < 0d)
                return candidate.item;
        }

        // Random.value can return its inclusive upper bound.
        return candidates[candidates.Count - 1].item;
    }

    private void ValidateRules(ProceduralMapSide side, List<DistanceMapRule> rules)
    {
        if (rules == null)
            return;

        for (int i = 0; i < rules.Count; i++)
        {
            DistanceMapRule rule = rules[i];
            if (rule == null)
                continue;

            if (rule.minDistance < 0 || rule.maxDistance < rule.minDistance)
                Debug.LogWarning($"{side} map rule {i} has an invalid distance range.", this);

            if (GetValidCandidates(rule.possibleMaps).Count == 0)
                Debug.LogWarning($"{side} map rule {i} has no valid weighted map presets.", this);

            for (int j = 0; j < i; j++)
            {
                DistanceMapRule other = rules[j];
                if (other != null && rule.minDistance <= other.maxDistance && rule.maxDistance >= other.minDistance)
                    Debug.LogWarning($"{side} map rules {j} and {i} overlap; the first matching rule is used.", this);
            }
        }
    }
}