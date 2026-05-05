using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "DoorTeleportTile", menuName = "Tiles/Door Teleport Tile")]
public class DoorTeleportTile : Tile
{
    public DoorTeleportData teleportData;
}