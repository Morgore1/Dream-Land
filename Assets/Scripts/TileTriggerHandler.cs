using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileTriggerHandler : MonoBehaviour
{
    public List<Tilemap> tilemaps; // Multiple tilemaps now
    public float revertTime = 2f;

    [System.Serializable]
    public class TileSwapPair
    {
        public TileBase originalTile;
        public TileBase replacementTile;
    }

    public TileSwapPair[] tileSwapPairs;

    private Dictionary<TileBase, TileBase> tileSwapDict;

    // Use Tuple<Tilemap, Vector3Int> as key to uniquely track per tilemap
    private Dictionary<(Tilemap, Vector3Int), Coroutine> activeReverts = new();

    void Start()
    {
        tileSwapDict = new Dictionary<TileBase, TileBase>();

        foreach (var pair in tileSwapPairs)
        {
            if (pair.originalTile == null || pair.replacementTile == null)
            {
                Debug.LogWarning("Missing tile in swap pair! Skipping.");
                continue;
            }
            tileSwapDict[pair.originalTile] = pair.replacementTile;
        }
    }

    void Update()
    {
        foreach (var tilemap in tilemaps)
        {
            Vector3Int playerTilePos = tilemap.WorldToCell(transform.position);
            TileBase currentTile = tilemap.GetTile(playerTilePos);

            if (currentTile != null && tileSwapDict.TryGetValue(currentTile, out TileBase replacement))
            {
                var key = (tilemap, playerTilePos);

                if (activeReverts.ContainsKey(key)) continue;

                tilemap.SetTile(playerTilePos, replacement);
                Coroutine revertRoutine = StartCoroutine(RevertTile(tilemap, playerTilePos, currentTile));
                activeReverts[key] = revertRoutine;
            }
        }
    }

    IEnumerator RevertTile(Tilemap tilemap, Vector3Int pos, TileBase originalTile)
    {
        
        while (tilemap.WorldToCell(transform.position) == pos)
        {
            yield return null; 
        }

        yield return new WaitForSeconds(revertTime);

        tilemap.SetTile(pos, originalTile);
        activeReverts.Remove((tilemap, pos));
    }

}
