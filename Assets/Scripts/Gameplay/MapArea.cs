using System.Collections.Generic;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] private MapPreset mapPreset;

    public void SetMapPreset(MapPreset preset)
    {
        mapPreset = preset;
    }

    public Monster GetRandomWildMonster()
    {
        if (mapPreset == null || mapPreset.WildMonsters == null || mapPreset.WildMonsters.Count == 0)
        {
            Debug.LogWarning("No wild monsters defined in this map preset!");
            return null;
        }

        var wildMonster = WeightedRandom.Pick(mapPreset.WildMonsters);
        wildMonster.Init();
        return wildMonster;
    }
}