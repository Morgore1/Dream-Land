using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLayers : MonoBehaviour
{
    [SerializeField] LayerMask solidObjectsLayer;
    [SerializeField] LayerMask grassLayer;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask houseDoorLayer;
    [SerializeField] LayerMask tunnelDoorLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask fovLayer; 
    [SerializeField] LayerMask npcFovLayer;
    public static GameLayers i { get; set; }

    private void Awake()
    {
        i = this;
    }

    public LayerMask SolidLayer
    {
        get => solidObjectsLayer;
    }
    public LayerMask GrassLayer
    {
        get => grassLayer;
    }
    public LayerMask InteractableLayer
    {
        get => interactableLayer;
    }
    public LayerMask HouseDoorLayer
    {
        get => houseDoorLayer;
    }
    public LayerMask TunnelDoorLayer
    {
        get => tunnelDoorLayer;
    }
    public LayerMask PlayerLayer
    {
        get => playerLayer;
    }
    public LayerMask FOVLayer
    {
        get => fovLayer;
    }
    public LayerMask NPCFOVLayer
    {
        get => npcFovLayer;
    }
}
