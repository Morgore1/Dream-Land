using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldCoordinateManager : MonoBehaviour
{
    public static WorldCoordinateManager Instance;

    public Vector2Int worldCoord = Vector2Int.zero;
    public bool IsInProceduralMap;

    void Awake()
    {
        Instance = this;
    }

    public void Move(Vector2Int direction)
    {
        worldCoord += direction;
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