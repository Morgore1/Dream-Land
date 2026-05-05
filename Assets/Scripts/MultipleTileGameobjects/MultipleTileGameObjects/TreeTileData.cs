using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


[System.Serializable]
public struct TileLayerData
{
    public Vector2Int offset;    // Position offset relative to house origin
    public TileBase tile;        // The tile at this position
}

[CreateAssetMenu(fileName = "TreeTileData", menuName = "Tree Tiles/Tree Tile Data")]
public class TreeTileData : ScriptableObject
{
    public TileLayerData[] leafTiles;       
    public TileLayerData[] solidTiles;
    public TileLayerData[] rootTiles;
    public TileLayerData[] backgroundTiles;

    // Other existing fields like size or offset can stay or be removed depending on usage
}
