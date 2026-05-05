using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DoorTeleportData
{
    public Vector2Int destination;
    public string interiorName; // Optional identifier

    public bool goesToMainMap;
    public bool goesToProceduralMap;
}
