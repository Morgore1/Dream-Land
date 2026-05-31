using System.Collections;
using System.Collections.Generic;
using Language.Lua;
using PixelCrushers;

#if UNITY_EDITOR
using TreeEditor;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.Tilemaps;

#endif
using UnityEngine;
#if UNITY_IOS
using UnityEngine.SocialPlatforms.GameCenter;
#endif
using UnityEngine.Tilemaps;
using UnityEngine.Video;

#if UNITY_EDITOR
using static UnityEditor.PlayerSettings;
#endif

public class WalkerGenerator : MonoBehaviour
{
    [System.Serializable]
    public class TileSaveData
    {
        public string layerName; // e.g., "wallTileMap", "roofTileMap"
        public Vector2Int position;
        public string tileName; // Name or ID to identify the tile
    }
    public List<TileSaveData> savedTileData = new List<TileSaveData>();

    public enum Grid { FLOOR, WALL, EMPTY, DOOR, ROAD, SIDEWALK, HOUSE, HOUSEu, PATH, TUNNEL, GRASS, ITEM, SOLID, TREE, TREEu, OBSTACLE, OBSTACLEu, RIVER }
    [Header("Parent Objects")]
    [SerializeField] private Transform mapRoot;
    [SerializeField] private Transform spawnedObjectsRoot;
    [Header("Tilemaps")]
    [SerializeField] private Tilemap floorTileMap; // Floor tiles
    [SerializeField] private Tilemap tallGrassTileMap;
    [SerializeField] private Tilemap wallTileMap; // House walls
    [SerializeField] private Tilemap roofTileMap;
    [SerializeField] private Tilemap itemsTileMap;
    [SerializeField] private Tilemap solidObjectsTileMap;
    [SerializeField] private Tilemap doorTileMap;
    [SerializeField] private Tilemap roadTileMap;
    [SerializeField] private Tilemap mapWallTileMap;
    [SerializeField] private Tilemap pathTileMap;
    [SerializeField] private Tilemap backgroundItemsMap;
    [SerializeField] private Tilemap roadTunnelEntranceMap;
    [SerializeField] private Tilemap leafTileMap;
    [SerializeField] private Tilemap rootTileMap;
    [SerializeField] private Tilemap nonCollisionLowTileMap;
    [SerializeField] private Tilemap nonCollisionHighTileMap;
    [SerializeField] private Tilemap nonCollisionLowTileMap2;
    [SerializeField] private Tilemap riverTileMap;

    [Header("Road Config")]
    [SerializeField] private int RoadWidth = 1; // How wide the road is (center strip)
    [SerializeField] private int SidewalkWidth = 1; // Width on each side


    [Header("River Tiles")]
    [SerializeField] private TileBase riverHorizontal;
    [SerializeField] private TileBase riverVertical;

    [SerializeField] private TileBase riverCorner;
    [Header("Road Tiles")]
    [SerializeField] private Tile RoadMiddle;
    [SerializeField] private Tile CrossRoad;
    [SerializeField] private Tile Sidewalk;
    public TileBase EastRoadTunnelEntrance;
    public TileBase WestRoadTunnelEntrance;
    public TileBase SouthRoadTunnelEntrance;
    public TileBase NorthRoadTunnelEntrance;
    [SerializeField] private Tile RoadTunnel;
    [SerializeField] private bool isGateTeleport = false;
    [SerializeField] private bool isNormalStreet = false;
    public Vector2Int lastEntryDirection;
    [SerializeField] private bool isShoreline = false;
    [SerializeField] private bool hasRiver = false;

    [Header("Transition Tiles")]
    [SerializeField] private Tile TransitionUp;
    [SerializeField] private Tile TransitionDown;
    [SerializeField] private Tile TransitionLeft;
    [SerializeField] private Tile TransitionRight;
    [SerializeField] private Tile CornerNW;
    [SerializeField] private Tile CornerNE;
    [SerializeField] private Tile CornerSW;
    [SerializeField] private Tile CornerSE;
    [SerializeField] private bool EnableNorthSouthRoads = true;
    [SerializeField] private bool EnableEastWestRoads = true;
    [SerializeField] private bool EnableSidewalks = true;

    [Header("Basic Tiles")]
    [SerializeField] private Tile Path;
    [SerializeField] private List<WeightedItem<Tile>> floorTiles;
    [SerializeField] private List<WeightedItem<Tile>> wallTiles;
    [SerializeField] private List<WeightedItem<Tile>> TallGrass;

    [Header("Grass Variants")]
    [SerializeField] private List<GrassByFloor> grassByFloor;

    [System.Serializable]
    public class GrassByFloor
    {
        public Tile floorTile;
        public List<WeightedItem<TileBase>> grassTile;
    }

    [Header("House Prefabs (HouseTileData SO)")]
    [SerializeField] private List<WeightedItem<HouseTileData>> HousePrefabs;

    [Header("Items & NPCs")]
    [SerializeField] private List<WeightedItem<Tile>> ObstacleTiles;
    [SerializeField] private List<WeightedItem<TileBase>> AnimatedObstacleTiles;
    [SerializeField] private List<WeightedItem<Tile>> Tidepools;
    [SerializeField] private List<WeightedItem<TileBase>> Puddles;
    [SerializeField] private List<WeightedItem<Tile>> BackgroundItemTiles;
    [SerializeField] private List<WeightedItem<GameObject>> NPCPrefabs;
    [SerializeField] private List<WeightedItem<GameObject>> ItemPrefabs;
    [SerializeField] private List<WeightedItem<TreeTileData>> TreePrefabs;
    [SerializeField] private List<WeightedItem<ObstacleTileData>> MultipleTileObstaclePrefabs;

    [Header("Generation Settings")]
    [SerializeField] private int MapWidth = 300;
    [SerializeField] private int MapHeight = 300;
    [SerializeField] private int MaximumWalkers = 100;
    [SerializeField] private float FillPercentage = 0.4f;
    [SerializeField] private float WaitTime = 0.01f;

    [SerializeField] int minClusters = 3;
    [SerializeField] int maxClusters = 6;

    [SerializeField] int minClusterWidth = 8;
    [SerializeField] int maxClusterWidth = 16;

    [SerializeField] int minClusterHeight = 8;
    [SerializeField] int maxClusterHeight = 16;

    [SerializeField] float clusterFillPercent = 0.25f;

    private Grid[,] gridHandler;
    private List<WalkerObject> Walkers;
    private int TileCount = 0;
    private List<WalkerCluster> clusters;
    [SerializeField] private int NumberOfHouses = 5;
    [SerializeField] private int NumberOfTrees = 5;

    // For NPC and item placement
    [SerializeField] private int NumberOfNPCs = 10;
    [SerializeField] private int NumberOfObtainableItems = 10;
    [SerializeField] private int NumberOfAnimatedObstacles = 5;
    [SerializeField] private int NumberOfItems = 15;
    [SerializeField] private int NumberOfMTItems = 15;
    [SerializeField] private int NumberOfBackgroundItems = 15;
    [SerializeField] private int grassPatchSize = 10;
    [SerializeField] private int grassPatchCount = 10;
    [SerializeField] private int tidepoolCount = 10;
    [SerializeField] private int puddleCount = 10;

    public VideoClip videoForThisMap;

    public static VideoClip SelectedVideoClip;

    // Tilemaps
    public Tilemap FloorTileMap => floorTileMap;
    public Tilemap TallGrassTileMap => tallGrassTileMap;
    public Tilemap WallTileMap => wallTileMap;
    public Tilemap RoofTileMap => roofTileMap;
    public Tilemap ItemsTileMap => ItemsTileMap;
    public Tilemap SolidObjectsTileMap => SolidObjectsTileMap;
    public Tilemap DoorTileMap => doorTileMap;
    public Tilemap RoadTileMap => roadTileMap;
    public Tilemap MapWallTileMap => mapWallTileMap;
    public Tilemap PathTileMap => pathTileMap;
    public Tilemap RoadTunnelEntranceMap => roadTunnelEntranceMap;

    // Road Config
    public int RoadWidthValue => RoadWidth;
    public int SidewalkWidthValue => SidewalkWidth;
    public Vector2Int LastEntryDirection => lastEntryDirection;
    // Road Tiles
    public Tile RoadMiddleTile => RoadMiddle;
    public Tile CrossRoadTile => CrossRoad;
    public Tile SidewalkTile => Sidewalk;
    public TileBase EastRoadTunnelEntranceTile => EastRoadTunnelEntrance;
    public TileBase WestRoadTunnelEntranceTile => WestRoadTunnelEntrance;
    public TileBase SouthRoadTunnelEntranceTile => SouthRoadTunnelEntrance;
    public TileBase NorthRoadTunnelEntranceTile => NorthRoadTunnelEntrance;
    public Tile RoadTunnelTile => RoadTunnel;
    public bool IsGateTeleport => isGateTeleport;

    // Transition Tiles
    public Tile TransitionUpTile => TransitionUp;
    public Tile TransitionDownTile => TransitionDown;
    public Tile TransitionLeftTile => TransitionLeft;
    public Tile TransitionRightTile => TransitionRight;
    public Tile CornerNWTile => CornerNW;
    public Tile CornerNETile => CornerNE;
    public Tile CornerSWTile => CornerSW;
    public Tile CornerSETile => CornerSE;
    public bool EnableNorthSouthRoadsValue => EnableNorthSouthRoads;
    public bool EnableEastWestRoadsValue => EnableEastWestRoads;
    public bool EnableSidewalksValue => EnableSidewalks;

    // Basic Tiles
    public Tile PathTile => Path;
    public List<WeightedItem<Tile>> FloorTilesList => floorTiles;
    public List<WeightedItem<Tile>> WallTilesList => wallTiles;
    public List<WeightedItem<Tile>> TallGrassTile => TallGrass;

    // House Prefabs
    public List<WeightedItem<HouseTileData>> HousePrefabsList => HousePrefabs;

    // Items & NPCs
    public List<WeightedItem<Tile>> ItemTilesList => ObstacleTiles;
    public List<WeightedItem<Tile>> BackgroundItemTilesList => BackgroundItemTiles;
    public List<WeightedItem<GameObject>> NPCPrefabsList => NPCPrefabs;
    public List<WeightedItem<GameObject>> ItemPrefabsList => ItemPrefabs;
    public List<WeightedItem<ObstacleTileData>> MultipleItemTilesList => MultipleTileObstaclePrefabs;

    // Generation Settings
    public int MapWidthValue => MapWidth;
    public int MapHeightValue => MapHeight;
    public int MaximumWalkersValue => MaximumWalkers;
    public float FillPercentageValue => FillPercentage;
    public float WaitTimeValue => WaitTime;

    // Other Settings
    public int NumberOfHousesValue => NumberOfHouses;
    public int NumberOfNPCsValue => NumberOfNPCs;
    public int NumberOfObtainableItemsValue => NumberOfObtainableItems;
    public int NumberOfItemsValue => NumberOfItems;
    public int NumberOfBackgroundItemsValue => NumberOfBackgroundItems;
    public int GrassPatchSizeValue => grassPatchSize;
    public int GrassPatchCountValue => grassPatchCount;

    private Dictionary<Vector2Int, TunnelTeleportData> tunnelTeleportPoints = new Dictionary<Vector2Int, TunnelTeleportData>();

    public void GenerateMapWithPreset(MapPreset preset)
    {
        videoForThisMap = preset.videoForThisMap;
        MapWidth = preset.MapWidth;
        MapHeight = preset.MapHeight;
        MaximumWalkers = preset.MaximumWalkers;
        FillPercentage = preset.FillPercentage;
        WaitTime = preset.WaitTime;

        minClusters = preset.minClusters;
        maxClusters = preset.maxClusters;
        minClusterWidth = preset.minClusterWidth;
        maxClusterWidth = preset.maxClusterWidth;
        minClusterHeight = preset.minClusterHeight;
        maxClusterHeight = preset.maxClusterHeight;
        clusterFillPercent = preset.clusterFillPercent;

        HousePrefabs = new List<WeightedItem<HouseTileData>>(preset.HousePrefabs);

        Path = preset.Path;
        floorTiles = new List<WeightedItem<Tile>>(preset.FloorTiles);
        wallTiles = new List<WeightedItem<Tile>>(preset.WallTiles);
        TallGrass = preset.TallGrass;
        RoadMiddle = preset.RoadMiddle;
        Sidewalk = preset.Sidewalk;
        CrossRoad = preset.CrossRoad;
        WestRoadTunnelEntrance = preset.WestRoadTunnelEntrance;
        EastRoadTunnelEntrance = preset.EastRoadTunnelEntrance;
        NorthRoadTunnelEntrance = preset.NorthRoadTunnelEntrance;
        SouthRoadTunnelEntrance = preset.SouthRoadTunnelEntrance;
        RoadTunnel = preset.RoadTunnel;
        isGateTeleport = preset.isGateTeleport;
        isNormalStreet = preset.isNormalStreet;
        isShoreline = preset.isShoreline;
        hasRiver = preset.hasRiver;

        riverHorizontal = preset.riverHorizontal;
        riverVertical = preset.riverVertical;
        riverCorner = preset.riverCorner;

        TransitionUp = preset.TransitionUp;
        TransitionDown = preset.TransitionDown;
        TransitionLeft = preset.TransitionLeft;
        TransitionRight = preset.TransitionRight;
        CornerNW = preset.CornerNW;
        CornerNE = preset.CornerNE;
        CornerSW = preset.CornerSW;
        CornerSE = preset.CornerSE;
        EnableNorthSouthRoads = preset.EnableNorthSouthRoads;
        EnableEastWestRoads = preset.EnableEastWestRoads;
        EnableSidewalks = preset.EnableSidewalks;

        grassByFloor = new List<GrassByFloor>(preset.grassByFloor);
        ObstacleTiles = new List<WeightedItem<Tile>>(preset.ObstacleTiles);
        AnimatedObstacleTiles = new List<WeightedItem<TileBase>>(preset.AnimatedObstacleTiles);
        BackgroundItemTiles = new List<WeightedItem<Tile>>(preset.BackgroundItemTiles);
        NPCPrefabs = new List<WeightedItem<GameObject>>(preset.NPCPrefabs);
        ItemPrefabs = new List<WeightedItem<GameObject>>(preset.ItemPrefabs);
        MultipleTileObstaclePrefabs = new List<WeightedItem<ObstacleTileData>>(preset.MultipleItemObstaclePrefabs);
        TreePrefabs = new List<WeightedItem<TreeTileData>>(preset.TreePrefabs);
        Tidepools = new List<WeightedItem<Tile>>(preset.Tidepools);
        Puddles = new List<WeightedItem<TileBase>>(preset.Puddles);

        NumberOfTrees = preset.NumberOfTrees;
        NumberOfHouses = preset.NumberOfHouses;
        NumberOfObtainableItems = preset.NumberOfObtainableItems;
        NumberOfNPCs = preset.NumberOfNPCs;
        NumberOfItems = preset.NumberOfObstacles;
        NumberOfAnimatedObstacles = preset.NumberOfAnimatedObstacles;
        NumberOfMTItems = preset.NumberOfMTObstacles;
        NumberOfBackgroundItems = preset.NumberOfBackgroundItems;
        grassPatchSize = preset.grassPatchSize;
        grassPatchCount = preset.grassPatchCount;
        tidepoolCount = preset.tidepoolCount;
        puddleCount = preset.puddleCount;


        InitializeGrid(); // reset grid before generation
        SelectedVideoClip = videoForThisMap;

        Debug.Log("Video selected: " + SelectedVideoClip.name);
    }
    public void ClearMap()
    {
        // Clear tiles
        floorTileMap?.ClearAllTiles();
        tallGrassTileMap?.ClearAllTiles();
        wallTileMap?.ClearAllTiles();
        roofTileMap?.ClearAllTiles();
        itemsTileMap?.ClearAllTiles();
        solidObjectsTileMap?.ClearAllTiles();
        doorTileMap?.ClearAllTiles();
        roadTileMap?.ClearAllTiles();
        mapWallTileMap?.ClearAllTiles();
        pathTileMap?.ClearAllTiles();
        backgroundItemsMap?.ClearAllTiles();
        roadTunnelEntranceMap?.ClearAllTiles();
        leafTileMap?.ClearAllTiles();
        rootTileMap?.ClearAllTiles();
        nonCollisionLowTileMap?.ClearAllTiles();
        nonCollisionHighTileMap?.ClearAllTiles();
        nonCollisionLowTileMap2?.ClearAllTiles();
        riverTileMap?.ClearAllTiles();

        // Clear spawned GameObjects ONLY
        for (int i = spawnedObjectsRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(spawnedObjectsRoot.GetChild(i).gameObject);
        }

        savedTileData.Clear();
        TileCount = 0;

        // IMPORTANT: actually reset grid instead of nulling it
        ResetGrid();

        Walkers = new List<WalkerObject>();
    }
    void ResetGrid()
    {
        gridHandler = new Grid[MapWidth, MapHeight];

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                gridHandler[x, y] = Grid.EMPTY;
            }
        }
    }

    void InitializeGrid()
    {

        gridHandler = new Grid[MapWidth, MapHeight];

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                gridHandler[x, y] = Grid.EMPTY;
            }
        }

        Walkers = new List<WalkerObject>();
        
        GenerateClusters();
        if (hasRiver)
        {
            GenerateRiver();
        }
        GenerateTunnelsFromEntry(lastEntryDirection);
        
        

    }
    void SaveTile(string layerName, Vector3Int pos, TileBase tile)
    {
        if (tile == null) return;

        TileSaveData data = new TileSaveData
        {
            layerName = layerName,
            position = new Vector2Int(pos.x, pos.y),
            tileName = tile.name
        };

        savedTileData.Add(data);
    }

    Vector2 GetDirection()
    {
        int choice = Mathf.FloorToInt(Random.value * 4);
        switch (choice)
        {
            case 0: return Vector2.down;
            case 1: return Vector2.left;
            case 2: return Vector2.up;
            case 3: return Vector2.right;
            default: return Vector2.zero;
        }
    }
    class WalkerCluster
    {
        public RectInt bounds;
        public Vector2Int center;
    }
    void GenerateClusters()
    {
        int clusterCount = Random.Range(minClusters, maxClusters + 1);
        List<WalkerCluster> clusters = new List<WalkerCluster>();

        for (int i = 0; i < clusterCount; i++)
        {
            int width = Random.Range(minClusterWidth, maxClusterWidth);
            int height = Random.Range(minClusterHeight, maxClusterHeight);

            int x = Random.Range(2, MapWidth - width - 2);
            int y = Random.Range(2, MapHeight - height - 2);

            RectInt bounds = new RectInt(x, y, width, height);
            Vector2Int center = new Vector2Int(x + width / 2, y + height / 2);

            clusters.Add(new WalkerCluster
            {
                bounds = bounds,
                center = center
            });

            RunWalkerCluster(bounds, center);
        }

        ConnectClusters(clusters);
        this.clusters = clusters;
    }
    void RunWalkerCluster(RectInt bounds, Vector2Int startPos)
    {
        Walkers.Clear();
        TileCount = 0;

        WalkerObject walker = new WalkerObject(startPos, GetDirection(), 0.5f);
        Walkers.Add(walker);

        int maxTiles = Mathf.FloorToInt(bounds.width * bounds.height * clusterFillPercent);

        int safety = 0;
        int maxIterations = 50000;

        while (TileCount < maxTiles && safety < maxIterations)
        {
            safety++;

           
            if (Walkers.Count == 0)
            {
                Debug.LogWarning("All walkers removed. Breaking.");
                break;
            }

            foreach (WalkerObject curWalker in Walkers)
            {
                Vector3Int pos = new Vector3Int(
                    Mathf.RoundToInt(curWalker.Position.x),
                    Mathf.RoundToInt(curWalker.Position.y),
                    0
                );

                if (!bounds.Contains(new Vector2Int(pos.x, pos.y)))
                    continue;

                if (gridHandler[pos.x, pos.y] != Grid.FLOOR)
                {
                    Tile floor = WeightedRandom.Pick(floorTiles);
                    floorTileMap.SetTile(pos, floor);
                    gridHandler[pos.x, pos.y] = Grid.FLOOR;
                    SaveTile("floorTileMap", pos, floor);
                    TileCount++;
                }
            }

            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePositionWithinBounds(bounds);
        }

        if (safety >= maxIterations)
        {
            Debug.LogWarning("Cluster generation hit safety limit.");
        }
    }
    void UpdatePositionWithinBounds(RectInt bounds)
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            WalkerObject w = Walkers[i];
            w.Position += w.Direction;

            w.Position.x = Mathf.Clamp(w.Position.x, bounds.xMin + 1, bounds.xMax - 2);
            w.Position.y = Mathf.Clamp(w.Position.y, bounds.yMin + 1, bounds.yMax - 2);

            Walkers[i] = w;
        }
    }
    void ConnectClusters(List<WalkerCluster> clusters)
    {
        for (int i = 0; i < clusters.Count - 1; i++)
        {
            Vector2Int a = clusters[i].center;
            Vector2Int b = clusters[i + 1].center;

            CarveCorridor(a, b);
        }
    }
    void CarveCorridor(Vector2Int start, Vector2Int end)
    {
        Vector2Int pos = start;

        while (pos.x != end.x)
        {
            pos.x += (end.x > pos.x) ? 1 : -1;
            CarveTile(pos);
        }

        while (pos.y != end.y)
        {
            pos.y += (end.y > pos.y) ? 1 : -1;
            CarveTile(pos);
        }
    }
    void CarveTile(Vector2Int pos)
    {
        if (gridHandler[pos.x, pos.y] == Grid.EMPTY)
        {
            Tile floor = WeightedRandom.Pick(floorTiles);
            floorTileMap.SetTile(new Vector3Int(pos.x, pos.y, 0), floor);
            gridHandler[pos.x, pos.y] = Grid.FLOOR;
            SaveTile("floorTileMap", new Vector3Int(pos.x, pos.y, 0), floor);
        }
    }
    IEnumerator CreateFloors()
    {
        while ((float)TileCount / (float)gridHandler.Length < FillPercentage)
        {
            bool hasCreatedFloor = false;
            foreach (WalkerObject curWalker in Walkers)
            {
                Vector3Int curPos = new Vector3Int((int)curWalker.Position.x, (int)curWalker.Position.y, 0);

                if (gridHandler[curPos.x, curPos.y] != Grid.FLOOR)
                {
                    
                    Tile randomFloor = WeightedRandom.Pick(floorTiles);

                    floorTileMap.SetTile(curPos, randomFloor);
                    TileCount++;
                    gridHandler[curPos.x, curPos.y] = Grid.FLOOR;
                    SaveTile("floorTileMap", curPos, randomFloor);
                    hasCreatedFloor = true;
                }
            }

            ChanceToRemove();
            ChanceToRedirect();
            ChanceToCreate();
            UpdatePosition();

            if (hasCreatedFloor)
            {
                yield return new WaitForSeconds(WaitTime);
            }
        }
        if (isNormalStreet) 
        { 
            GenerateRoads(); 
        }
        else
        {
            GenerateTunnelsFromEntry(lastEntryDirection);
        }
    }

    void ChanceToRemove()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (Random.value < Walkers[i].ChanceToChange && Walkers.Count > 1)
            {
                Walkers.RemoveAt(i);
                break;
            }
        }
    }

    void ChanceToRedirect()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            if (Random.value < Walkers[i].ChanceToChange)
            {
                WalkerObject curWalker = Walkers[i];
                curWalker.Direction = GetDirection();
                Walkers[i] = curWalker;
            }
        }
    }

    void ChanceToCreate()
    {
        int updatedCount = Walkers.Count;
        for (int i = 0; i < updatedCount; i++)
        {
            if (Random.value < Walkers[i].ChanceToChange && Walkers.Count < MaximumWalkers)
            {
                Vector2 newDirection = GetDirection();
                Vector2 newPosition = Walkers[i].Position;

                WalkerObject newWalker = new WalkerObject(newPosition, newDirection, 0.5f);
                Walkers.Add(newWalker);
            }
        }
    }

    void UpdatePosition()
    {
        for (int i = 0; i < Walkers.Count; i++)
        {
            WalkerObject foundWalker = Walkers[i];
            foundWalker.Position += foundWalker.Direction;
            foundWalker.Position.x = Mathf.Clamp(foundWalker.Position.x, 1, MapWidth - 2);
            foundWalker.Position.y = Mathf.Clamp(foundWalker.Position.y, 1, MapHeight - 2);
            Walkers[i] = foundWalker;
        }
    }

    void CreateWalls()
    {
        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight ; y++)
            {
                if (gridHandler[x, y] == Grid.EMPTY)
                {
                    Tile randomFloor = WeightedRandom.Pick(floorTiles);
                    Tile randomWall = WeightedRandom.Pick(wallTiles);

                    Vector3Int pos = new Vector3Int(x, y, 0);
                    mapWallTileMap.SetTile(pos, randomWall);
                    floorTileMap.SetTile(pos, randomFloor);
                    SaveTile("floorTileMap", pos, randomFloor);
                    gridHandler[x, y] = Grid.WALL;
                    SaveTile("mapWallTileMap", pos, randomWall);
                }
            }
        }

        // Once walls are generated, generate other features
        PlaceHouses();
        GenerateGrassPatches();
        
    }
    List<Vector2Int> riverTiles = new List<Vector2Int>();
    void GenerateRiver()
    {
        riverTiles.Clear();

        Vector2Int start = new Vector2Int(0, Random.Range(5, MapHeight - 5));
        Vector2Int end = new Vector2Int(MapWidth - 1, Random.Range(5, MapHeight - 5));

        Vector2Int current = start;
        Vector2Int dir = Vector2Int.right;

        int safety = MapWidth * MapHeight;

        int verticalRun = 0;

        while (current != end && safety-- > 0)
        {
            riverTiles.Add(current);

            int dx = end.x - current.x;
            int dy = end.y - current.y;

            float r = Random.value;

            // Mostly continue right
            if (r < 0.65f)
            {
                dir = Vector2Int.right;
                verticalRun = 0;
            }
            // Small upward bend
            else if (r < 0.82f && verticalRun < 3 && current.y < MapHeight - 3)
            {
                dir = Vector2Int.up;
                verticalRun++;
            }
            // Small downward bend
            else if (verticalRun < 3 && current.y > 3)
            {
                dir = Vector2Int.down;
                verticalRun++;
            }
            else
            {
                dir = Vector2Int.right;
                verticalRun = 0;
            }

            current += dir;

            if (!InBounds(current))
                break;
        }

        riverTiles.Add(end);

        BuildRiverTiles();
    }

    void BuildRiverTiles()
    {
        HashSet<Vector2Int> riverSet = new HashSet<Vector2Int>(riverTiles);

        foreach (Vector2Int pos in riverTiles)
        if (gridHandler[pos.x, pos.y] != Grid.ROAD)
        {
            bool up = riverSet.Contains(pos + Vector2Int.up);
            bool down = riverSet.Contains(pos + Vector2Int.down);
            bool left = riverSet.Contains(pos + Vector2Int.left);
            bool right = riverSet.Contains(pos + Vector2Int.right);

            TileBase tile = null;
            Matrix4x4 matrix = Matrix4x4.identity;

            // Straight
            if (up && down)
            {
                tile = riverVertical;
            }
            else if (left && right)
            {
                tile = riverHorizontal;
            }

            // Corners
            else if (down && right)
            {
                tile = riverCorner;
                matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 0));
            }
            else if (up && right)
            {
                tile = riverCorner;
                matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90));
            }
            else if (up && left)
            {
                tile = riverCorner;
                matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 180));
            }
            else if (down && left)
            {
                tile = riverCorner;
                matrix = Matrix4x4.Rotate(Quaternion.Euler(0, 0, 270));
            }

            // Ends
            else if (up || down)
            {
                tile = riverVertical;
            }
            else if (left || right)
            {
                tile = riverHorizontal;
            }

            Vector3Int v3 = (Vector3Int)pos;

            Tile floor = WeightedRandom.Pick(floorTiles);
            floorTileMap.SetTile(v3, floor);
            SaveTile("floorTileMap", v3, floor);

            riverTileMap.SetTile(v3, tile);
            riverTileMap.SetTransformMatrix(v3, matrix);

            gridHandler[pos.x, pos.y] = Grid.RIVER;

            SaveTile(riverTileMap.name, v3, tile);
        }
    }
    bool InBounds(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < MapWidth && pos.y >= 0 && pos.y < MapHeight;
    }
    Vector2Int NormalizeDirection(Vector2Int dir)
    {
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return new Vector2Int(Mathf.Sign(dir.x) > 0 ? 1 : -1, 0);

        if (Mathf.Abs(dir.y) > Mathf.Abs(dir.x))
            return new Vector2Int(0, Mathf.Sign(dir.y) > 0 ? 1 : -1);

        // fallback: treat (0,0) as down
        return Vector2Int.down;
    }
    Vector2Int GetRandomPositionForDirection(Vector2Int dir)
    {
        int minX = MapWidth / 4;
        int maxX = MapWidth - MapWidth / 4;
        int minY, maxY;

        if (dir == Vector2Int.up)
        {
            minY = MapHeight * 3 / 4;
            maxY = MapHeight - 5;
        }
        else if (dir == Vector2Int.down)
        {
            minY = 5;
            maxY = MapHeight / 4;
        }
        else if (dir == Vector2Int.left)
        {
            minX = 5;
            maxX = MapWidth / 4;
            minY = MapHeight / 4;
            maxY = MapHeight * 3 / 4;
        }
        else // right
        {
            minX = MapWidth * 3 / 4;
            maxX = MapWidth - 5;
            minY = MapHeight / 4;
            maxY = MapHeight * 3 / 4;
        }

        Vector2Int pos;
        int safety = 1000;
        do
        {
            pos = new Vector2Int(Random.Range(minX, maxX), Random.Range(minY, maxY));
            safety--;
        } while (!InBounds(pos) || gridHandler[pos.x, pos.y] != Grid.EMPTY && safety > 0);

        return pos;
    }
    Vector2Int GetFixedGatePositionForDirection(Vector2Int dir)
    {
        int midY = MapHeight / 2;
        int midX = MapWidth / 2;

        if (dir == Vector2Int.left)
            return new Vector2Int(4, midY);
        if (dir == Vector2Int.right)
            return new Vector2Int(MapWidth - 5, midY);
        if (dir == Vector2Int.up)
            return new Vector2Int(midX, MapHeight - 5);
        if (dir == Vector2Int.down)
            return new Vector2Int(midX, 4);

        return new Vector2Int(midX, midY);
    }
    void GenerateTunnelsFromEntry(Vector2Int entryDirRaw)
    {
        if (isShoreline)
        {
            GenerateShorelineWestGates();
        }
        Vector2Int entryDir = NormalizeDirection(entryDirRaw);
        List<(Vector2Int pos, Vector2Int dir)> tunnels = new();

        // Main exit (opposite side of where you came from)
        Vector2Int exitDir = -entryDir;
        Vector2Int mainExitPos = GetFixedGatePositionForDirection(exitDir);

        Vector2Int mainEntrance = PlaceGates(mainExitPos, exitDir);
        tunnels.Add((mainEntrance, exitDir));

        // Extra tunnels
        int extraCount = Random.Range(1, 4);

        List<Vector2Int> possibleDirs = new()
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };
        possibleDirs.Remove(exitDir);
        possibleDirs.Shuffle();

        for (int i = 0; i < extraCount && i < possibleDirs.Count; i++)
        {
            Vector2Int dir = possibleDirs[i];
            Vector2Int pos = GetRandomPositionForDirection(dir);

            Vector2Int extraEntrance = PlaceGates(pos, dir);
            tunnels.Add((extraEntrance, dir));
        }

        // Now roads will use correct entrance positions
        GenerateConnectingRoads(tunnels);
    }
    void GenerateShorelineWestGates()
    {
        Vector2Int dir = Vector2Int.left;

        for (int x = 0; x <= 6; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                Vector2Int gatePos = new Vector2Int(x, y);

                // Don't overwrite walls or existing tunnels
                if (gridHandler[x, y] == Grid.WALL || gridHandler[x, y] == Grid.TUNNEL)
                    continue;

                PlaceShorelineGate(gatePos, dir);
            }
        }
    }
    void PlaceShorelineGate(Vector2Int entrance, Vector2Int dir)
    {
        Vector3Int pos = (Vector3Int)entrance;

        // Floor underneath
        Tile floor = WeightedRandom.Pick(floorTiles);
        floorTileMap.SetTile(pos, floor);
        SaveTile("floorTileMap", pos, floor);

        // Gate tile
        roadTunnelEntranceMap.SetTile(pos, WestRoadTunnelEntrance);
        SaveTile(roadTunnelEntranceMap.name, pos, WestRoadTunnelEntrance);

        Matrix4x4 matrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 0f, 180f));
        roadTunnelEntranceMap.SetTransformMatrix(pos, matrix);

        gridHandler[entrance.x, entrance.y] = Grid.TUNNEL;

        // Teleport data
        tunnelTeleportPoints[entrance] = new TunnelTeleportData
        {
            destination = GetTunnelDestinationForDirection(entrance, dir),
            toProceduralMap = true
        };

        // Small road going INTO the map
        Vector2Int roadStart = entrance + Vector2Int.right;
        if (pos.y > 10 && pos.y < MapHeight - 10)
        {
            for (int i = 0; i < 2; i++)
            {
                if (!InBounds(roadStart))
                    break;

                SetRoadTile(roadStart);
                roadStart += Vector2Int.right;
            }
        }
        
    }
    Vector2Int PlaceGates(Vector2Int gatePos, Vector2Int dir)
    {
        // gatePos IS the entrance tile
        Vector2Int entrance = gatePos;

        Vector3Int entrancePos = (Vector3Int)entrance;
        TileBase entranceTile = GetDirectionalTunnelEntranceTile(dir);

        // 1. Place floor under the gate (IMPORTANT)
        Tile floor = WeightedRandom.Pick(floorTiles);
        floorTileMap.SetTile(entrancePos, floor);
        SaveTile("floorTileMap", entrancePos, floor);

        // 2. Now place the gate tile itself on the entrance tilemap
        if (isShoreline)
        {
            if (entranceTile != null && entranceTile != WestRoadTunnelEntrance)
            {
                roadTunnelEntranceMap.SetTile(entrancePos, entranceTile);
                gridHandler[entrance.x, entrance.y] = Grid.TUNNEL;
                SaveTile(roadTunnelEntranceMap.name, entrancePos, entranceTile);
            }
        }
        else if (entranceTile != null)
        {
            roadTunnelEntranceMap.SetTile(entrancePos, entranceTile);
            gridHandler[entrance.x, entrance.y] = Grid.TUNNEL;
            SaveTile(roadTunnelEntranceMap.name, entrancePos, entranceTile);
        }

        // Teleport point keyed on the entrance tile the player stands on
        tunnelTeleportPoints[entrance] = new TunnelTeleportData
        {
            destination = GetTunnelDestinationForDirection(entrance, dir),
            toProceduralMap = true
        };

        

        // --- Road going INTO the map, behind the tunnel entrance ---

        // dir = which way the tunnel "points" (outwards).
        // So inside-the-map is the opposite: -dir.
        Vector2Int roadStart = entrance - dir * 2;
        SetRoadTile(entrance - dir);  
        Vector2Int roadEnd = entrance;              // end at the entrance
        Vector2Int current = roadStart;

        while (current != roadEnd)
        {
            SetRoadTile(current);
            current += dir; // step toward the entrance
        }

        return roadStart; // this is the true tunnel entrance position
    }
    void GenerateOrganicPath(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        int safety = MapWidth * MapHeight;

        SetRoadTile(current);

        while (current != end && safety-- > 0)
        {
            int dx = end.x - current.x;
            int dy = end.y - current.y;

            Vector2Int step = Vector2Int.zero;

            // Decide whether to move horizontally or vertically this step.
            bool canMoveX = dx != 0;
            bool canMoveY = dy != 0;

            if (canMoveX && canMoveY)
            {
                // Randomly choose axis when both are possible
                if (Random.value < 0.5f)
                {
                    step = new Vector2Int(dx > 0 ? 1 : -1, 0);
                }
                else
                {
                    step = new Vector2Int(0, dy > 0 ? 1 : -1);
                }
            }
            else if (canMoveX)
            {
                step = new Vector2Int(dx > 0 ? 1 : -1, 0);
            }
            else if (canMoveY)
            {
                step = new Vector2Int(0, dy > 0 ? 1 : -1);
            }
            else
            {
                // Already at end (shouldn't really hit this because of loop condition)
                break;
            }

            Vector2Int next = current + step;

            if (!InBounds(next))
            {
                // If somehow out of bounds, bail to avoid infinite loop
                break;
            }

            current = next;
            SetRoadTile(current);
        }
    }

    void GenerateConnectingRoads(List<(Vector2Int pos, Vector2Int dir)> tunnels)
    {
        if (tunnels == null || tunnels.Count == 0)
            return;

        // --- Compute a hub position (center of a random cluster) ---
        Vector2Int hub;
        if (this.clusters != null && this.clusters.Count > 0)
        {
            int randomIndex = Random.Range(0, this.clusters.Count);
            hub = this.clusters[randomIndex].center;
        }
        else
        {
            // fallback to average
            Vector2 sum = Vector2.zero;
            foreach (var t in tunnels)
            {
                sum += (Vector2)t.pos;
            }
            Vector2 average = sum / tunnels.Count;
            hub = new Vector2Int(
                Mathf.RoundToInt(average.x),
                Mathf.RoundToInt(average.y)
            );
        }

        // Clamp hub so it's safely inside the map
        hub.x = Mathf.Clamp(hub.x, 2, MapWidth - 3);
        hub.y = Mathf.Clamp(hub.y, 2, MapHeight - 3);

        // Make sure the hub itself is a road tile
        SetRoadTile(hub);

        // --- Connect each tunnel entrance to the hub with a clean path ---
        foreach (var t in tunnels)
        {
            GenerateOrganicPath(t.pos, hub);
        }

        CreateWalls();
    }

    void SetRoadTile(Vector2Int pos)
    {
        if (!InBounds(pos))
            return;

        // Don't overwrite walls or tunnel entrances
        if (gridHandler[pos.x, pos.y] == Grid.WALL ||
            gridHandler[pos.x, pos.y] == Grid.TUNNEL)
            return;

        Vector3Int v3 = (Vector3Int)pos;

        roadTileMap.SetTile(v3, RoadMiddleTile);
        gridHandler[pos.x, pos.y] = Grid.ROAD;

        SaveTile(roadTileMap.name, v3, RoadMiddleTile);
    }
    void GenerateRoads()
    {
        Vector2Int center = new Vector2Int(MapWidth / 2, MapHeight / 2);
        int roadLength = Mathf.Min(MapWidth, MapHeight) / 2 - SidewalkWidth - 2;

        Dictionary<Vector2Int, string> directions = new Dictionary<Vector2Int, string>();
        if (EnableNorthSouthRoads)
        {
            directions.Add(Vector2Int.up, "Up");
            directions.Add(Vector2Int.down, "Down");
        }
        if (EnableEastWestRoads)
        {
            directions.Add(Vector2Int.left, "Left");
            directions.Add(Vector2Int.right, "Right");
        }

        List<(Vector2Int endPos, Vector2Int dir)> roadEnds = new(); // Store ends for tunnel placement

        foreach (var kvp in directions)
        {
            Vector2Int dir = kvp.Key;
            Vector2Int perp = new Vector2Int(-dir.y, dir.x);

            for (int i = 0; i < roadLength; i++)
            {
                Vector2Int basePos = center + dir * i;

                // Center road strip
                for (int w = -RoadWidth / 2; w <= RoadWidth / 2; w++)
                {
                    Vector2Int roadPos = basePos + perp * w;
                    if (InBounds(roadPos))
                    {
                        Vector3Int pos = (Vector3Int)roadPos;
                        roadTileMap.SetTile(pos, RoadMiddle);
                        gridHandler[roadPos.x, roadPos.y] = Grid.ROAD;

                        if (dir == Vector2Int.up || dir == Vector2Int.down)
                            roadTileMap.SetTransformMatrix(pos, Matrix4x4.Rotate(Quaternion.Euler(0, 0, 90)));
                        else
                            roadTileMap.SetTransformMatrix(pos, Matrix4x4.identity);
                    }
                }

                if (EnableSidewalks == true)
                {
                    // Sidewalks
                    bool IsInCenter5x5ButNotCorner(Vector2Int pos, Vector2Int center)
                    {
                        int minX = center.x - 2;
                        int maxX = center.x + 2;
                        int minY = center.y - 2;
                        int maxY = center.y + 2;

                        bool in5x5 = pos.x >= minX && pos.x <= maxX && pos.y >= minY && pos.y <= maxY;
                        bool isCorner = (pos.x == minX || pos.x == maxX) && (pos.y == minY || pos.y == maxY);

                        return in5x5 && !isCorner;
                    }

                    for (int s = 2; s <= SidewalkWidth + 1; s++)
                    {
                        Vector2Int sidewalkPos1 = basePos + perp * (RoadWidth / 2 + s);
                        Vector2Int sidewalkPos2 = basePos - perp * (RoadWidth / 2 + s);

                        bool skipCenter = EnableNorthSouthRoads && EnableEastWestRoads;

                        if (InBounds(sidewalkPos1) &&
                            gridHandler[sidewalkPos1.x, sidewalkPos1.y] != Grid.ROAD &&
                            !(skipCenter && IsInCenter5x5ButNotCorner(sidewalkPos1, center)))
                        {
                            Vector3Int pos = (Vector3Int)sidewalkPos1;
                            roadTileMap.SetTile(pos, Sidewalk);
                            gridHandler[sidewalkPos1.x, sidewalkPos1.y] = Grid.SIDEWALK;
                            SaveTile("floorWallTileMap", pos, Sidewalk);
                        }

                        if (InBounds(sidewalkPos2) &&
                            gridHandler[sidewalkPos2.x, sidewalkPos2.y] != Grid.ROAD &&
                            !(skipCenter && IsInCenter5x5ButNotCorner(sidewalkPos2, center)))
                        {
                            Vector3Int pos = (Vector3Int)sidewalkPos2;
                            roadTileMap.SetTile(pos, Sidewalk);
                            gridHandler[sidewalkPos2.x, sidewalkPos2.y] = Grid.SIDEWALK;
                            SaveTile("floorWallTileMap", pos, Sidewalk);
                        }
                    }
                    
                }
            }

            Vector2Int endOfRoad = center + dir * roadLength;
            roadEnds.Add((endOfRoad + dir, dir)); 
        }

        if (EnableNorthSouthRoads && EnableEastWestRoads && (EnableSidewalks == true))
        {
            Vector3Int pos = (Vector3Int)center;
            roadTileMap.SetTile(pos, CrossRoad);
            gridHandler[center.x, center.y] = Grid.ROAD;
            SaveTile("floorWallTileMap", pos, Sidewalk);
            PlaceIntersectionCorners(center);
        }

        PlaceRoadSidewalkTransitions();

        foreach (var roadEnd in roadEnds)
        {
            PlaceTunnelAtRoadEnd(roadEnd.endPos, roadEnd.dir);
        }
    }
    void PlaceTunnelAtRoadEnd(Vector2Int roadEnd, Vector2Int dir)
    {
        if (!this.isGateTeleport)
        {
            // Default 5x5 behavior: entrance at edge
            for (int dx = -2; dx <= 2; dx++)
            {
                for (int dy = -2; dy <= 2; dy++)
                {
                    Vector2Int offset = new Vector2Int(dx, dy);
                    Vector2Int tilePos = roadEnd + offset;

                    if (!InBounds(tilePos)) continue;

                    Vector3Int pos = (Vector3Int)tilePos;

                    bool isEntranceEdge =
                        (dir == Vector2Int.up && dy == -2) ||
                        (dir == Vector2Int.down && dy == 2) ||
                        (dir == Vector2Int.left && dx == 2) ||
                        (dir == Vector2Int.right && dx == -2);

                    if (isEntranceEdge)
                    {
                        TileBase entranceTile = GetDirectionalTunnelEntranceTile(dir);
                        if (entranceTile != null)
                        {
                            roadTunnelEntranceMap.SetTile(pos, entranceTile);
                            gridHandler[tilePos.x, tilePos.y] = Grid.TUNNEL;
                            SaveTile(roadTunnelEntranceMap.name, pos, entranceTile);
                        }

                        tunnelTeleportPoints[tilePos] = new TunnelTeleportData
                        {
                            destination = GetTunnelDestinationForDirection(tilePos, dir),
                            toProceduralMap = false
                        };
                    }
                    else
                    {
                        if (RoadTunnel != null)
                        {
                            wallTileMap.SetTile(pos, RoadTunnel);
                            gridHandler[tilePos.x, tilePos.y] = Grid.WALL;
                            SaveTile(wallTileMap.name, pos, RoadTunnel);
                        }
                    }
                }
            }
        }
        else
        {
            // Gate teleport logic: move entrance 2 tiles *towards* the road
            Vector2Int shiftedRoadEnd = roadEnd - dir * 2;

            Vector3Int entrancePos = (Vector3Int)shiftedRoadEnd;
            TileBase entranceTile = GetDirectionalTunnelEntranceTile(dir);

            if (entranceTile != null)
            {
                roadTunnelEntranceMap.SetTile(entrancePos, entranceTile);
                gridHandler[shiftedRoadEnd.x, shiftedRoadEnd.y] = Grid.TUNNEL;
                SaveTile(roadTunnelEntranceMap.name, entrancePos, entranceTile);
            }

            tunnelTeleportPoints[shiftedRoadEnd] = new TunnelTeleportData
            {
                destination = GetTunnelDestinationForDirection(shiftedRoadEnd, dir),
                toProceduralMap = true
            };

            // Determine perpendicular direction to road direction
            Vector2Int perpDir;

            if (dir == Vector2Int.left || dir == Vector2Int.right)
            {
                // Road runs horizontally -> tunnels go up/down
                perpDir = Vector2Int.up;
            }
            else
            {
                // Road runs vertically -> tunnels go left/right
                perpDir = Vector2Int.right;
            }

            // Place tunnels perpendicular to road direction, 2 tiles each side
            for (int i = 1; i <= 2; i++)
            {
                Vector2Int forward = shiftedRoadEnd + perpDir * i;
                Vector2Int backward = shiftedRoadEnd - perpDir * i;

                if (InBounds(forward))
                {
                    Vector3Int forwardPos = (Vector3Int)forward;
                    wallTileMap.SetTile(forwardPos, RoadTunnel);
                    gridHandler[forward.x, forward.y] = Grid.WALL;
                    SaveTile(wallTileMap.name, forwardPos, RoadTunnel);
                }

                if (InBounds(backward))
                {
                    Vector3Int backwardPos = (Vector3Int)backward;
                    wallTileMap.SetTile(backwardPos, RoadTunnel);
                    gridHandler[backward.x, backward.y] = Grid.WALL;
                    SaveTile(wallTileMap.name, backwardPos, RoadTunnel);
                }
            }
        }
    }
    private TileBase GetDirectionalTunnelEntranceTile(Vector2Int dir)
    {
        if (dir == Vector2Int.up && NorthRoadTunnelEntrance != null) return NorthRoadTunnelEntrance;
        if (dir == Vector2Int.down && SouthRoadTunnelEntrance != null) return SouthRoadTunnelEntrance;
        if (dir == Vector2Int.left && WestRoadTunnelEntrance != null) return WestRoadTunnelEntrance;
        if (dir == Vector2Int.right && EastRoadTunnelEntrance != null) return EastRoadTunnelEntrance;

        Debug.LogWarning($"No tunnel entrance tile assigned for direction: {dir}");
        return null;
    }
    Vector2Int GetTunnelDestinationForDirection(Vector2Int entrance, Vector2Int dir)
    {
        Vector2Int mirrorEntrance = entrance;

        if (dir == Vector2Int.up)
            mirrorEntrance = new Vector2Int(entrance.x, 5); // Near bottom
        else if (dir == Vector2Int.down)
            mirrorEntrance = new Vector2Int(entrance.x, MapHeight - 6); // Near top
        else if (dir == Vector2Int.left)
            mirrorEntrance = new Vector2Int(MapWidth - 6, entrance.y); // Near right
        else if (dir == Vector2Int.right)
            mirrorEntrance = new Vector2Int(5, entrance.y); // Near left

        // Move one step forward from the entrance in the direction the tunnel would lead out
        Vector2Int destination = mirrorEntrance + dir;

        return destination;
    }

    void PlaceIntersectionCorners(Vector2Int center)
    {
        // Corners around the center are at +/- 1 on X and Y axes
        for (int dx = -1; dx <= 1; dx += 2)
        {
            for (int dy = -1; dy <= 1; dy += 2)
            {
                Vector2Int corner = center + new Vector2Int(dx, dy);
                if (!InBounds(corner)) continue;

                Tile cornerTile = (dx, dy) switch
                {
                    (-1, 1) => CornerNW, // top-left corner
                    (1, 1) => CornerNE,  // top-right corner
                    (-1, -1) => CornerSW, // bottom-left corner
                    (1, -1) => CornerSE,  // bottom-right corner

                    _ => null
                };

                if (cornerTile != null)
                {
                    Vector3Int pos = (Vector3Int)corner;
                    roadTileMap.SetTile(pos, cornerTile);
                    gridHandler[corner.x, corner.y] = Grid.ROAD;
                    SaveTile("roadTileMap", pos, cornerTile);
                }
            }
        }
    }
    void PlaceRoadSidewalkTransitions()
    {
        if (EnableSidewalks == true)
        {
            for (int x = 1; x < MapWidth - 1; x++)
            {
                for (int y = 1; y < MapHeight - 1; y++)
                {
                    if (gridHandler[x, y] == Grid.ROAD)
                    {
                        // Up
                        if (gridHandler[x, y + 2] == Grid.SIDEWALK)
                        {
                            Vector3Int pos = new Vector3Int(x, y + 1, 0);
                            roadTileMap.SetTile(pos, TransitionUp);
                            gridHandler[x, y + 1] = Grid.ROAD;
                            SaveTile("roadTileMap", pos, TransitionUp);
                        }

                        // Down
                        if (gridHandler[x, y - 2] == Grid.SIDEWALK)
                        {
                            Vector3Int pos = new Vector3Int(x, y - 1, 0);
                            roadTileMap.SetTile(pos, TransitionDown);
                            gridHandler[x, y - 1] = Grid.ROAD;
                            SaveTile("roadTileMap", pos, TransitionDown);
                        }

                        // Right
                        if (gridHandler[x + 2, y] == Grid.SIDEWALK)
                        {
                            Vector3Int pos = new Vector3Int(x + 1, y, 0);
                            roadTileMap.SetTile(pos, TransitionRight);
                            gridHandler[x + 1, y] = Grid.ROAD;
                            SaveTile("roadTileMap", pos, TransitionRight);
                        }

                        // Left
                        if (gridHandler[x - 2, y] == Grid.SIDEWALK)
                        {
                            Vector3Int pos = new Vector3Int(x - 1, y, 0);
                            roadTileMap.SetTile(pos, TransitionLeft);
                            gridHandler[x - 1, y] = Grid.ROAD;
                            SaveTile("roadTileMap", pos, TransitionLeft);
                        }
                    }
                }
            }
        }
        

        CreateWalls(); // still safe here
    }
    bool IsEmpty(int x, int y)
    {
        return (gridHandler[x, y] == Grid.FLOOR)
            && (gridHandler[x, y] != Grid.WALL)
            && (gridHandler[x, y] != Grid.DOOR)
            && (gridHandler[x, y] != Grid.SIDEWALK)
            && (gridHandler[x, y] != Grid.HOUSE)
            && (gridHandler[x, y] != Grid.OBSTACLE)
            && (gridHandler[x, y] != Grid.TREE)
            && (gridHandler[x, y] != Grid.ITEM)
            && (gridHandler[x, y] != Grid.TUNNEL)
            && (gridHandler[x, y] != Grid.PATH)
            && (gridHandler[x, y] != Grid.ROAD)
            && (gridHandler[x, y] != Grid.GRASS)
            && (gridHandler[x, y] != Grid.RIVER);
    }
    void GenerateGrassPatches()
    {
        int patchesPlaced = 0;
        int attempts = 0;

        while (patchesPlaced < grassPatchCount && attempts < 1000)
        {
            int x = Random.Range(1, MapWidth - 1);
            int y = Random.Range(1, MapHeight - 1);

            // Start only on floor tiles and avoid houses, roads, sidewalks
            if (IsEmpty(x, y))
            {
                GrowGrassPatch(x, y, grassPatchSize);
                patchesPlaced++;
            }

            attempts++;
        }
        if (tidepoolCount > 0)
        {
            GenerateTidepool();
        }
        PlaceTrees();
        PlaceMultipleTileItems();
        SpawnObtainableItems();
        PlaceItems();
        SpawnNPCs();
    }
    private TileBase GetGrassTileForPosition(Vector3Int pos)
    {
        TileBase floorTile = floorTileMap.GetTile(pos);

        if (floorTile == null)
            return WeightedRandom.Pick(TallGrass);

        foreach (var entry in grassByFloor)
        {
            if (entry.floorTile == floorTile)
                return WeightedRandom.Pick(entry.grassTile); 
        }

        return WeightedRandom.Pick(TallGrass); // default fallback
    }

    void GrowGrassPatch(int startX, int startY, int size)
    {
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(startX, startY));

        int count = 0;
        while (toCheck.Count > 0 && count < size)
        {
            Vector2Int pos = toCheck.Dequeue();
            if (visited.Contains(pos)) continue;
            visited.Add(pos);

            if (IsEmpty(pos.x, pos.y))
            {
                gridHandler[pos.x, pos.y] = Grid.GRASS;
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                TileBase grassTile = GetGrassTileForPosition(tilePos);

                tallGrassTileMap.SetTile(tilePos, grassTile);
                SaveTile("tallGrassTileMap", tilePos, grassTile);
                count++;

                List<Vector2Int> neighbors = new List<Vector2Int>()
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };

                foreach (var neighbor in neighbors)
                {
                    if (Random.value < 0.8f) // controls blob spread
                        toCheck.Enqueue(neighbor);
                }
            }
        }
    }
    void GenerateTidepool()
    {
        int tidepoolsPlaced = 0;
        int attempts = 0;
        while (tidepoolsPlaced < tidepoolCount && attempts < 1000)
        {
            int x = Random.Range(1, MapWidth - 1);
            int y = Random.Range(1, MapHeight - 1);


            int tidepoolSize = Mathf.RoundToInt(Random.Range(2f, 8f));

            // Start only on floor tiles and avoid houses, roads, sidewalks
            if (IsEmpty(x, y))
            {
                GrowTidepool(x, y, tidepoolSize);
                tidepoolsPlaced++;
            }

            attempts++;
        }
    }
    void GeaneratePuddle()
    {
        int puddlesPlaced = 0;
        int attempts = 0;
        while (puddlesPlaced < puddleCount && attempts < 1000)
        {
            int x = Random.Range(1, MapWidth - 1);
            int y = Random.Range(1, MapHeight - 1);


            int puddleSize = Mathf.RoundToInt(Random.Range(2f, 6f));

            // Start only on floor tiles and avoid houses, roads, sidewalks
            if (IsEmpty(x, y))
            {
                GrowPuddle(x, y, puddleSize);
                puddlesPlaced++;
            }

            attempts++;
        }
    }

    void GrowTidepool(int startX, int startY, int size)
    {
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(startX, startY));

        int count = 0;
        while (toCheck.Count > 0 && count < size)
        {
            Vector2Int pos = toCheck.Dequeue();
            if (visited.Contains(pos)) continue;
            visited.Add(pos);

            // Only grow on floor tiles that are NOT road, sidewalk, or house tiles
            if (IsEmpty(pos.x, pos.y))
            {
                gridHandler[pos.x, pos.y] = Grid.SOLID;
                Tile Tidepool = WeightedRandom.Pick(Tidepools);
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                solidObjectsTileMap.SetTile(tilePos, Tidepool);
                SaveTile("solidObjectsTileMap", tilePos, Tidepool);
                count++;

                List<Vector2Int> neighbors = new List<Vector2Int>()
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };

                foreach (var neighbor in neighbors)
                {
                    if (Random.value < 0.8f) // controls blob spread
                        toCheck.Enqueue(neighbor);
                }
            }
        }
    }
    void GrowPuddle(int startX, int startY, int size)
    {
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        toCheck.Enqueue(new Vector2Int(startX, startY));

        int count = 0;
        while (toCheck.Count > 0 && count < size)
        {
            Vector2Int pos = toCheck.Dequeue();
            if (visited.Contains(pos)) continue;
            visited.Add(pos);

            // Only grow on floor tiles that are NOT road, sidewalk, or house tiles
            if (IsEmpty(pos.x, pos.y))
            {
                gridHandler[pos.x, pos.y] = Grid.SOLID;
                TileBase Puddle = WeightedRandom.Pick(Puddles);
                Vector3Int tilePos = new Vector3Int(pos.x, pos.y, 0);
                solidObjectsTileMap.SetTile(tilePos, Puddle);
                SaveTile("solidObjectsTileMap", tilePos, Puddle);
                count++;

                List<Vector2Int> neighbors = new List<Vector2Int>()
            {
                new Vector2Int(pos.x + 1, pos.y),
                new Vector2Int(pos.x - 1, pos.y),
                new Vector2Int(pos.x, pos.y + 1),
                new Vector2Int(pos.x, pos.y - 1)
            };

                foreach (var neighbor in neighbors)
                {
                    if (Random.value < 0.8f) // controls blob spread
                        toCheck.Enqueue(neighbor);
                }
            }
        }
    }


    void PlaceHouses()
    {
        int housesPlaced = 0;
        int attempts = 0;

        // Max house size to check for space clearance
        int maxHouseWidth = 0;
        int maxHouseHeight = 0;
        if (HousePrefabs != null &&  HousePrefabs.Count > 0)
        {
            while (housesPlaced < NumberOfHouses && attempts < 1000)
            {
                attempts++;

                int x = Random.Range(1, MapWidth - maxHouseWidth);
                int y = Random.Range(1, MapHeight - maxHouseHeight);

                HouseTileData selectedHouse = WeightedRandom.Pick(HousePrefabs);

                if (IsAreaClearForHouse(x, y, selectedHouse) && IsEmpty(x, y))
                {
                    // Place roof tiles
                    foreach (var tileData in selectedHouse.roofTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        roofTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.HOUSEu;
                        SaveTile("roofTileMap", pos, tileData.tile);
                    }

                    // Place solid tiles (walls, collision)
                    foreach (var tileData in selectedHouse.solidTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        wallTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.HOUSE; // mark as occupied
                        SaveTile("wallTileMap", pos, tileData.tile);
                    }

                    // Place background tiles
                    foreach (var tileData in selectedHouse.backgroundTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        nonCollisionLowTileMap2.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.HOUSE;
                        SaveTile("nonCollisionLowTileMap2", pos, tileData.tile);
                    }

                    // Place door tiles
                    foreach (var tileData in selectedHouse.doorTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);

                        doorTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.DOOR; // mark as occupied
                        int doorX = pos.x;
                        int doorY = pos.y;
                        SaveTile("doorTileMap", pos, tileData.tile);

                        GeneratePathToSidewalk(doorX, doorY);
                    }

                    housesPlaced++;
                }
            }
        }
        
    }
    void PlaceMultipleTileItems()
    {
        int multipleTileItemsPlaced = 0;
        int attempts = 0;

        // Max house size to check for space clearance
        int maxItemWidth = 0;
        int maxItemHeight = 0;
        if (MultipleTileObstaclePrefabs != null && MultipleTileObstaclePrefabs.Count > 0)
        {
            while (multipleTileItemsPlaced < NumberOfMTItems && attempts < 1000)
            {
                attempts++;

                int x = Random.Range(1, MapWidth - maxItemWidth);
                int y = Random.Range(1, MapHeight - maxItemHeight);

                ObstacleTileData selectedItem = WeightedRandom.Pick(MultipleTileObstaclePrefabs);

                if (IsAreaClearForObstacle(x, y, selectedItem) && IsEmpty(x,y))
                {
                    foreach (var tileData in selectedItem.nonCollisionHighTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        nonCollisionHighTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.OBSTACLEu;
                        SaveTile("nonCollisionHighTileMap", pos, tileData.tile);
                    }
                    // Place solid tiles (walls, collision)
                    foreach (var tileData in selectedItem.solidTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        wallTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.OBSTACLE; // mark as occupied
                        SaveTile("wallTileMap", pos, tileData.tile);
                    }
                    foreach (var tileData in selectedItem.nonCollisionLowTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        nonCollisionLowTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.OBSTACLE; // mark as occupied
                        SaveTile("nonCollisionLowTileMap", pos, tileData.tile);
                    }
                    foreach (var tileData in selectedItem.backgroundTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        nonCollisionLowTileMap2.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.OBSTACLE;
                        SaveTile("nonCollisionLowTileMap2", pos, tileData.tile);
                    }



                    multipleTileItemsPlaced++;
                }
            }
        }
        
    }
    void PlaceTrees()
    {
        int treesPlaced = 0;
        int attempts = 0;

        // Max house size to check for space clearance
        int maxTreeWidth = 0;
        int maxTreeHeight = 0;
        if (TreePrefabs != null && TreePrefabs.Count > 0)
        {
            while (treesPlaced < NumberOfTrees && attempts < 1000)
            {
                attempts++;

                int x = Random.Range(1, MapWidth - maxTreeWidth);
                int y = Random.Range(1, MapHeight - maxTreeHeight);

                TreeTileData selectedTree = WeightedRandom.Pick(TreePrefabs);

                if (IsAreaClearForTree(x, y, selectedTree) && IsEmpty(x, y))
                {
                    foreach (var tileData in selectedTree.leafTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        leafTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.TREEu;
                        SaveTile("leafTileMap", pos, tileData.tile);
                    }
                    // Place solid tiles (walls, collision)
                    foreach (var tileData in selectedTree.solidTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        wallTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.TREE;
                        SaveTile("wallTileMap", pos, tileData.tile);
                    }
                    foreach (var tileData in selectedTree.rootTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        rootTileMap.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.TREE;
                        SaveTile("rootTileMap", pos, tileData.tile);
                    }
                    foreach (var tileData in selectedTree.backgroundTiles)
                    {
                        Vector3Int pos = new Vector3Int(x + tileData.offset.x, y + tileData.offset.y, 0);
                        nonCollisionLowTileMap2.SetTile(pos, tileData.tile);
                        gridHandler[pos.x, pos.y] = Grid.TREE;
                        SaveTile("nonCollisionLowTileMap2.", pos, tileData.tile);
                    }



                    treesPlaced++;
                }
            }
        }

    }

    bool InBounds(int x, int y)
    {
        return x >= 0 && x < MapWidth && y >= 0 && y < MapHeight;
    }
    // Helper: Check if all tiles of the house fit in empty space
    bool IsAreaClearForHouse(int originX, int originY, HouseTileData house)
    {
        foreach (var tileData in house.roofTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] == Grid.HOUSEu) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false; // Only place on floor return false;
        }
        foreach (var tileData in house.solidTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in house.backgroundTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in house.doorTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }

        return true;
    }
    bool IsAreaClearForObstacle(int originX, int originY, ObstacleTileData obstacle)
    {
        foreach (var tileData in obstacle.nonCollisionHighTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] == Grid.OBSTACLEu) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in obstacle.solidTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in obstacle.backgroundTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in obstacle.nonCollisionLowTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }

        return true;
    }
    bool IsAreaClearForTree(int originX, int originY, TreeTileData tree)
    {
        foreach (var tileData in tree.leafTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] == Grid.TREEu) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false; 
        }
        foreach (var tileData in tree.solidTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in tree.backgroundTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }
        foreach (var tileData in tree.rootTiles)
        {
            int x = originX + tileData.offset.x;
            int y = originY + tileData.offset.y;
            if (x < 0 || x >= MapWidth || y < 0 || y >= MapHeight) return false;
            if (gridHandler[x, y] != Grid.FLOOR) return false;
        }

        return true;
    }

    void SpawnNPCs()
    {
        int npcsSpawned = 0;
        int attempts = 0;
        
        if (NPCPrefabs.Count > 0)
        {
            while (npcsSpawned < NumberOfNPCs && attempts < 1000)
            {
                attempts++;
                int x = Random.Range(1, MapWidth - 1);
                int y = Random.Range(1, MapHeight - 1);

                if (IsEmpty(x, y) && (gridHandler[x, y] != Grid.GRASS))
                {
                    Vector3 spawnPos = new Vector3(x + 0.5f, y + 0.7f, 0f); // Center of tile

                    GameObject npcPrefab = WeightedRandom.Pick(NPCPrefabs);
                    GameObject npc = Instantiate(npcPrefab, spawnPos, Quaternion.identity);

                    // Make the NPC a child of the map root GameObject
                    npc.transform.SetParent(mapRoot, true);

                    npcsSpawned++;
                }
            }
        }
        
    }
    void SpawnObtainableItems()
    {
        int obtainableItemsSpawned = 0;
        int attempts = 0;

        if (ItemPrefabs.Count > 0)
        {
            while (obtainableItemsSpawned < NumberOfObtainableItems && attempts < 1000)
            {
                attempts++;
                int x = Random.Range(1, MapWidth - 1);
                int y = Random.Range(1, MapHeight - 1);

                if (IsEmpty(x, y))
                {
                    Vector3 spawnPos = new Vector3(x + 0.5f, y + 0.5f, 0f); // Center of tile

                    GameObject itemPrefab = WeightedRandom.Pick(ItemPrefabs);
                    GameObject item = Instantiate(itemPrefab, spawnPos, Quaternion.identity);
                    gridHandler[x, y] = Grid.ITEM;

                    // Make the NPC a child of the map root GameObject
                    item.transform.SetParent(spawnedObjectsRoot, true);

                    obtainableItemsSpawned++;
                }
            }
        }
        
    }
    void GeneratePathToSidewalk(int doorX, int doorY)
    {
        int range = 10;
        Vector2Int? nearestSidewalk = null;
        float minDist = float.MaxValue;

        for (int dx = -range; dx <= range; dx++)
        {
            for (int dy = -range; dy <= range; dy++)
            {
                int checkX = doorX + dx;
                int checkY = doorY + dy;

                if (InBounds(checkX, checkY) && gridHandler[checkX, checkY] == Grid.SIDEWALK)
                {
                    float dist = Vector2Int.Distance(new Vector2Int(doorX, doorY), new Vector2Int(checkX, checkY));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        nearestSidewalk = new Vector2Int(checkX, checkY);
                    }
                }
            }
        }

        if (nearestSidewalk.HasValue)
        {
            Vector2Int current = new Vector2Int(doorX, doorY);
            Vector2Int target = nearestSidewalk.Value;

            // Simple straight-line path generation
            while (current != target)
            {
                if (current.x < target.x) current.x++;
                else if (current.x > target.x) current.x--;
                else if (current.y < target.y) current.y++;
                else if (current.y > target.y) current.y--;

                // Don't overwrite sidewalk or door
                if (gridHandler[current.x, current.y] == Grid.EMPTY || gridHandler[current.x, current.y] == Grid.FLOOR )
                {
                    gridHandler[current.x, current.y] = Grid.PATH;
                    Vector3Int pos = new Vector3Int(current.x, current.y, 0);
                    pathTileMap.SetTile(pos, Path); // Optional: use a special path tile
                    SaveTile("pathTileMap", pos, Path);
                    Debug.Log($"Generating path from door at ({doorX}, {doorY}) to sidewalk at ({target.x}, {target.y})");
                }
            }
        }
    }

    void PlaceItems()
    {
        int itemsPlaced = 0;
        int attempts = 0;

        while (itemsPlaced < NumberOfItems && attempts < 1000)
        {
            attempts++;
            int x = Random.Range(1, MapWidth - 1);
            int y = Random.Range(1, MapHeight - 1);

            if (IsEmpty(x, y))
            {
                Tile itemTile = WeightedRandom.Pick(ObstacleTiles);
                Vector3Int pos = new Vector3Int(x, y, 0);

                gridHandler[x, y] = Grid.OBSTACLE;
                itemsTileMap.SetTile(pos, itemTile);
                SaveTile("itemsTileMap", pos, itemTile);
                itemsPlaced++;
            }
        }
        PlaceAnimatedObstacles();
    }
    void PlaceAnimatedObstacles()
    {
        int AnimObstPlaced = 0;
        int attempts = 0;

        while (AnimObstPlaced < NumberOfAnimatedObstacles && attempts < 1000)
        {
            attempts++;
            int x = Random.Range(1, MapWidth - 1);
            int y = Random.Range(1, MapHeight - 1);

            if (IsEmpty(x, y))
            {
                TileBase animObstTile = WeightedRandom.Pick(AnimatedObstacleTiles);
                Vector3Int pos = new Vector3Int(x, y, 0);

                gridHandler[x, y] = Grid.OBSTACLE;
                itemsTileMap.SetTile(pos, animObstTile);
                SaveTile("itemsTileMap", pos, animObstTile);
                AnimObstPlaced ++;
            }
        }
        PlaceBackgroundItems();
    }
    void PlaceBackgroundItems()
    {
        int itemsPlaced = 0;
        int attempts = 0;

        if (BackgroundItemTiles.Count > 0)
        {
            while (itemsPlaced < NumberOfBackgroundItems && attempts < 1000)
            {
                attempts++;
                int x = Random.Range(1, MapWidth - 1);
                int y = Random.Range(1, MapHeight - 1);

                if (IsEmpty(x, y))
                {
                    Tile backgroundItemsTile = WeightedRandom.Pick(BackgroundItemTiles);
                    Vector3Int pos = new Vector3Int(x, y, 0);

                    gridHandler[x, y] = Grid.ITEM;
                    backgroundItemsMap.SetTile(pos, backgroundItemsTile);
                    SaveTile("backgroundItemsTileMap", pos, backgroundItemsTile);
                    itemsPlaced++;
                }
            }
        }
        
    }
    bool IsNearRoad(int x, int y)
    {
        for (int offsetX = -5; offsetX <= 5; offsetX++)
        {
            for (int offsetY = -5; offsetY <= 5; offsetY++)
            {
                int checkX = x + offsetX;
                int checkY = y + offsetY;

                if (InBounds(checkX, checkY) && gridHandler[checkX, checkY] == Grid.ROAD)
                {
                    return true;
                }
            }
        }
        return false;
    }
}