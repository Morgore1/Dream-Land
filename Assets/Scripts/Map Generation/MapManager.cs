using System.Collections.Generic;
using UnityEngine.Tilemaps;
using UnityEngine;
using System.Collections;

public class MapManager : MonoBehaviour
{
    [Header("Map Generation")]
    public WalkerGenerator walkerGenerator;
    public List<WeightedItem<MapPreset>> mapPresets;

    [Header("Map Roots")]
    public GameObject mainMapRoot;
    public GameObject proceduralMapRoot;
    public GameObject interiorMapRoot;

    [Header("Tilemaps")]
    public Tilemap mainRoadTunnelEntranceMap;
    public Tilemap mainDoorTileMap;
    public Tilemap proceduralDoorTileMap;
    public Tilemap interiorDoorTileMap;

    [Header("Map Music")]
    [SerializeField] private AudioSource mainMapAudio;
    [SerializeField] private AudioClip mainMapClip;
    [SerializeField] private AudioSource routeMapAudio;
    [SerializeField] private AudioClip routeMapClip;

    [SerializeField] private SpriteScreenTransition screenTransition;
    public enum MapType { Main, Procedural }
    [HideInInspector]
    public MapType previousMapType = MapType.Main;

    private void Start()
    {
        if (MusicManager.Instance == null)
        {
            return;
        }

        if (proceduralMapRoot != null && proceduralMapRoot.activeSelf)
        {
            MusicManager.Instance.PlayRouteMapMusic(routeMapAudio);
            return;
        }

        if (mainMapRoot != null && mainMapRoot.activeSelf)
        {
            MusicManager.Instance.PlayMainMapMusic(mainMapAudio);
        }
    }

    public void ToggleMapVisibility(bool showProcedural)
    {
        bool isProceduralToProcedural = previousMapType == MapType.Procedural && showProcedural;

        if (isProceduralToProcedural && GameController.Instance != null)
        {
            GameController.Instance.SpendEnergy(1);
        }

        // Only start transition if going to/from procedural map
        if (screenTransition != null && (previousMapType == MapType.Procedural || showProcedural))
        {
            StartCoroutine(ToggleMapWithTransition(showProcedural));
            return; // exit here, coroutine handles the rest
        }

        // Normal toggle if no transition needed
        ApplyMapVisibility(showProcedural);
    }
    private void Awake()
    {
        if (mainMapAudio != null && mainMapClip != null)
        {
            mainMapAudio.clip = mainMapClip;
            mainMapAudio.loop = true;
            mainMapAudio.playOnAwake = false;
        }

        if (routeMapAudio != null && routeMapClip != null)
        {
            routeMapAudio.clip = routeMapClip;
            routeMapAudio.loop = true;
            routeMapAudio.playOnAwake = false;
        }
    }

    private void ApplyMapVisibility(bool showProcedural)
    {
        if (mainMapRoot != null) mainMapRoot.SetActive(!showProcedural);
        if (proceduralMapRoot != null) proceduralMapRoot.SetActive(showProcedural);
        if (interiorMapRoot != null) interiorMapRoot.SetActive(false);

        ResetAllNPCs(showProcedural ? proceduralMapRoot : mainMapRoot);

        previousMapType = showProcedural ? MapType.Procedural : MapType.Main;

        if (MusicManager.Instance != null)
        {
            if (showProcedural)
            {
                MusicManager.Instance.PlayRouteMapMusic(routeMapAudio);
            }
            else
            {
                MusicManager.Instance.PlayMainMapMusic(mainMapAudio);
            }
        }
    }
    private IEnumerator ToggleMapWithTransition(bool showProcedural)
    {
        // Play first half of transition (frames 0�7)
        yield return screenTransition.PlayFrames(0, 7);

        // Actually switch maps
        ApplyMapVisibility(showProcedural);

        // Play second half of transition (frames 8�15)
        yield return screenTransition.PlayFrames(8, 15);
    }
    private void ResetAllNPCs(GameObject mapRoot)
    {
        if (mapRoot == null) return;

        NPCController[] npcs = mapRoot.GetComponentsInChildren<NPCController>(true);
        foreach (var npc in npcs)
        {
            npc.CaptureStartingPosition(); // ensure start position is captured
            npc.ResetToStartPosition();
        }
    }

    public void ShowInteriorMap(bool show)
    {
        if (interiorMapRoot != null) interiorMapRoot.SetActive(show);

        if (!show) return;

        if (mainMapRoot != null) mainMapRoot.SetActive(false);
        if (proceduralMapRoot != null) proceduralMapRoot.SetActive(false);

        ResetAllNPCs(interiorMapRoot);
    }

    public void SetMapPreset(MapPreset preset)
    {
        
        MapArea mapArea = proceduralMapRoot.GetComponent<MapArea>();
        if (mapArea != null)
        {
            mapArea.SetMapPreset(preset);
        }

    }
}