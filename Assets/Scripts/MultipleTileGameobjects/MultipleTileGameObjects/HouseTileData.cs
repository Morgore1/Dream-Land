using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(fileName = "NewHouseTileData", menuName = "House Tiles/House Tile Data")]
public class HouseTileData : ScriptableObject
{
    public TileLayerData[] roofTiles;       // Each tile + offset on roof layer
    public TileLayerData[] solidTiles;      // tiles for walls/solid layer
    public TileLayerData[] backgroundTiles; // background layer tiles
    public TileLayerData[] doorTiles;        // door layer tiles

    // Other existing fields like size or offset can stay or be removed depending on usage
}