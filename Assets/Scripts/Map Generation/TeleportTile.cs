using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "TeleportTile", menuName = "Tiles/Teleport Tile")]
public class TeleportTile : TileBase
{
    public TileBase visualTile;
    public TunnelTeleportData teleportData;

    public override void GetTileData(
        Vector3Int position,
        ITilemap tilemap,
        ref TileData tileData)
    {
        if (visualTile == null)
            return;

        visualTile.GetTileData(position, tilemap, ref tileData);
    }

    public override bool GetTileAnimationData(
        Vector3Int position,
        ITilemap tilemap,
        ref TileAnimationData tileAnimationData)
    {
        if (visualTile is AnimatedTile animated)
        {
            return animated.GetTileAnimationData(
                position,
                tilemap,
                ref tileAnimationData);
        }

        return false;
    }
}