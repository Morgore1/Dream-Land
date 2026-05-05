using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathNode
{
    public Vector2Int position;
    public PathNode parent;
    public int gCost;
    public int hCost;
    public int FCost => gCost + hCost;

    public PathNode(Vector2Int pos)
    {
        position = pos;
    }
}
