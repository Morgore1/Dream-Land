using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.Video;
using static WalkerGenerator;

[CreateAssetMenu(fileName = "NewMapPreset", menuName = "Map Preset")]
public class MapPreset : ScriptableObject
{
    [Header("Map Size & Generation Settings")]
    public int MapWidth = 300;
    public int MapHeight = 300;
    public int MaximumWalkers = 100;
    public float FillPercentage = 0.4f;
    public float WaitTime = 0.01f;


    public int minClusters = 3;
    public int maxClusters = 6;

    public int minClusterWidth = 8;
    public int maxClusterWidth = 16;

    public int minClusterHeight = 8;
    public int maxClusterHeight = 16;

    public float clusterFillPercent = 0.25f;

    [Header("Procedural Layout Variation")]
    [Range(0f, 1f)] public float townLayoutChance = 0.4f;
    [Range(0.1f, 1f)] public float compactClusterFillMultiplier = 0.55f;
    [Min(8)] public int townRadius = 26;
    [Range(1f, 4f)] public float townHouseMultiplier = 2f;
    [Min(0)] public int extraRouteCount = 2;
    [Header("Route Surroundings")]
    [Min(1)] public int compactRouteFloorWidth = 3;
    [Min(1)] public int townRouteFloorWidth = 9;
    [Range(0, 4)] public int routeEdgeVariation = 2;

    [Header("Wilderness Variation")]
    [Range(0f, 1f)] public float wildernessChance = 0.2f;
    [Min(0.1f)] public float wildernessTreeMultiplier = 2f;
    [Min(0.1f)] public float wildernessGrassPatchCountMultiplier = 2f;
    [Min(0.1f)] public float wildernessGrassPatchSizeMultiplier = 1.5f;
    [Min(0.1f)] public float wildernessClusterCountMultiplier = 1.5f;
    [Range(0.25f, 1f)] public float wildernessClusterSizeMultiplier = 0.75f;


    [Header("Special Map Conditions")]
    public bool isShoreline;
    public bool hasRiver;

    [Header("Houses")]
    public List<WeightedItem<HouseTileData>> HousePrefabs;  // your house prefab SO list

    [Header("Tiles")]
    public List<WeightedItem<Tile>> FloorTiles;
    public List<WeightedItem<Tile>> WallTiles;
    public List<GrassByFloor> grassByFloor;
    public List<WeightedItem<Tile>> TallGrass;
    public Tile RoadMiddle;
    public Tile Sidewalk;
    public TileBase EastRoadTunnelEntrance;
    public TileBase WestRoadTunnelEntrance;
    public TileBase SouthRoadTunnelEntrance;
    public TileBase NorthRoadTunnelEntrance;
    public Tile RoadTunnel;
    public Tile CrossRoad;
    public Tile Path;
    public bool EnableNorthSouthRoads = true;
    public bool EnableEastWestRoads = true;
    public bool isGateTeleport = true;
    public bool isNormalStreet = true;
    public bool EnableSidewalks = true;

    [Header("River Tiles")]
    public TileBase riverHorizontal;
    public TileBase riverVertical;

    public TileBase riverCorner;

    [Header("Transition Tiles")]
    public Tile TransitionUp;
    public Tile TransitionDown;
    public Tile TransitionLeft;
    public Tile TransitionRight;
    public Tile CornerNW;
    public Tile CornerNE;
    public Tile CornerSW;
    public Tile CornerSE;

    [Header("Items & NPCs")]
    public List<WeightedItem<Tile>> ObstacleTiles;
    public List<WeightedItem<TileBase>> AnimatedObstacleTiles;
    public List<WeightedItem<Tile>> BackgroundItemTiles;
    public List<WeightedItem<GameObject>> NPCPrefabs;
    public List<WeightedItem<GameObject>> ItemPrefabs;
    public List<WeightedItem<TreeTileData>> TreePrefabs;
    public List<WeightedItem<ObstacleTileData>> MultipleItemObstaclePrefabs;
    public List<WeightedItem<Tile>> Tidepools;
    public List<WeightedItem<TileBase>> Puddles;

    [Header("Wild Monsters")]
    public List<WeightedItem<Monster>> WildMonsters;

    [Header("Other Settings")]
    public int NumberOfTrees = 5;
    public int NumberOfHouses = 5;
    public int NumberOfNPCs = 10;
    public int NumberOfObtainableItems = 10;
    public int NumberOfObstacles = 15;
    public int NumberOfAnimatedObstacles = 5;
    public int NumberOfMTObstacles = 15;
    public int NumberOfBackgroundItems = 15;
    public int grassPatchSize = 10;
    public int grassPatchCount = 10;
    public int tidepoolCount = 10;
    public int puddleCount = 10;

    public VideoClip videoForThisMap;

    [Header("Music")]
    public AudioClip mapMusic;

    public static VideoClip SelectedVideoClip;
}