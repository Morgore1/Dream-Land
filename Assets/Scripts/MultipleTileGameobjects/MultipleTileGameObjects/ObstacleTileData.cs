using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;


[CreateAssetMenu(fileName = "NewObstacleTileData", menuName = "Obstacle Tiles/Obstacle Tile Data")]
public class ObstacleTileData : ScriptableObject
{
    public TileLayerData[] nonCollisionHighTiles;
    public TileLayerData[] solidTiles;
    public TileLayerData[] nonCollisionLowTiles;
    public TileLayerData[] backgroundTiles;

    // Other existing fields like size or offset can stay or be removed depending on usage
}