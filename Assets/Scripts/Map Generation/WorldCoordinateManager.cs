using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCoordinateManager : MonoBehaviour
{
    public static WorldCoordinateManager Instance;

    public Vector2Int worldCoord = Vector2Int.zero;
    public bool IsInProceduralMap;

    [SerializeField] ProceduralMapSide lastHorizontalSide = ProceduralMapSide.Right;

    public ProceduralMapSide EffectiveSide => worldCoord.x < 0
        ? ProceduralMapSide.Left
        : worldCoord.x > 0
            ? ProceduralMapSide.Right
            : lastHorizontalSide;

    void Awake()
    {
        Instance = this;
    }

    public void Move(Vector2Int direction)
    {
        int previousX = worldCoord.x;
        worldCoord += direction;

        if (worldCoord.x < 0)
            lastHorizontalSide = ProceduralMapSide.Left;
        else if (worldCoord.x > 0)
            lastHorizontalSide = ProceduralMapSide.Right;
        else if (previousX != 0)
            lastHorizontalSide = previousX < 0 ? ProceduralMapSide.Left : ProceduralMapSide.Right;
    }

    public void StartProceduralTrip(ProceduralMapSide side)
    {
        lastHorizontalSide = side;
        worldCoord = new Vector2Int(side == ProceduralMapSide.Left ? -1 : 1, 0);
        IsInProceduralMap = true;
    }

    public void RestorePosition(Vector2Int coordinate, ProceduralMapSide side)
    {
        worldCoord = coordinate;
        lastHorizontalSide = side;
    }

    public int GetDistance(Vector2Int direction)
    {
        if (direction == Vector2Int.left)
            return Mathf.Abs(Mathf.Min(0, worldCoord.x));

        if (direction == Vector2Int.right)
            return Mathf.Max(0, worldCoord.x);

        if (direction == Vector2Int.up)
            return Mathf.Max(0, worldCoord.y);

        if (direction == Vector2Int.down)
            return Mathf.Abs(Mathf.Min(0, worldCoord.y));

        return 0;
    }
}