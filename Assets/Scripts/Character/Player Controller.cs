using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Presets;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class PlayerController : MonoBehaviour
{
    public MapManager mapManager;
    public WalkerGenerator walkerGenerator;

    public bool canMove = true;

    public Vector2 input;

    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    public event Action OnEncountered;
    public event Action OnEnterProceduralMap;
    public event Action OnExitProceduralMap;
    public event Action<Collider2D> OnEnterTrainersView;
    public event Action<Collider2D> OnEnterNPCsView;

    private Character character;

    public Vector2Int lastMoveDir = Vector2Int.down;
    private void Awake()
    {
        character = GetComponent<Character>();
    }

    public void HandleUpdate()
    {
        if (!canMove) return;

        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                // ⭐ THIS IS NEW — TRACK MOVEMENT DIRECTION
                lastMoveDir = new Vector2Int(
                    Mathf.RoundToInt(input.x),
                    Mathf.RoundToInt(input.y)
                );

                StartCoroutine(character.Move(input, CheckFor));
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
            Interact();
    }

    void Interact()
    {
        if (!canMove) return; 

        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer);
        if (collider != null)
        {
            collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    private void CheckFor()
    {
        if (!canMove) return; // Extra safeguard

        CheckForEncounter();
        CheckForTeleport();
        CheckIfInTrainersView();
        CheckIfInNPCsView();
    }

    private void CheckForEncounter()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.GrassLayer) != null)
        {
            if (UnityEngine.Random.Range(1, 101) <= 10)
            {
                character.Animator.IsMoving = false;
                OnEncountered();
            }
        }
    }

    private void CheckForTeleport()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.HouseDoorLayer) != null)
        {
            StartCoroutine(HandleInteriorTeleport());
        }
        else if (Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.TunnelDoorLayer) != null)
        {
            StartCoroutine(HandleTunnelTeleport());
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            Collider2D col = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.HouseDoorLayer);
            Debug.Log("InteriorDoorLayer Collider: " + (col != null ? col.name : "None"));
        }
    }

    private IEnumerator HandleTunnelTeleport()
    {
        canMove = false;
        character.Animator.IsMoving = false;
        yield return new WaitForSeconds(0.1f);

        Vector3Int playerTilePos = Vector3Int.FloorToInt(transform.position);
        TileBase tile = null;
        TeleportTile teleportTile = null;

        tile = mapManager.walkerGenerator.RoadTunnelEntranceMap.GetTile(playerTilePos);
        if (tile is TeleportTile tpTile1 && tpTile1.teleportData != null)
        {
            teleportTile = tpTile1;
        }
        else
        {
            tile = mapManager.mainRoadTunnelEntranceMap.GetTile(playerTilePos);
            if (tile is TeleportTile tpTile2 && tpTile2.teleportData != null)
            {
                teleportTile = tpTile2;
            }
        }

        if (teleportTile != null)
        {
            TunnelTeleportData teleportData = teleportTile.teleportData;

            // Detect direction player entered from
            Vector2Int entryDir = lastMoveDir;
            if (teleportData.toProceduralMap)
            {
                
                if (!WorldCoordinateManager.Instance.IsInProceduralMap)
                {
                    WorldCoordinateManager.Instance.IsInProceduralMap = true;


                    mapManager.ToggleMapVisibility(true);

                    MapPreset preset = WeightedRandom.Pick(teleportData.mapPreset);

                    mapManager.SetMapPreset(preset);

                    walkerGenerator.lastEntryDirection = lastMoveDir;
                    walkerGenerator.ClearMap();
                    walkerGenerator.GenerateMapWithPreset(preset);

                    
                    Vector2Int destination = teleportData.destination;
                    transform.position = new Vector3(
                        destination.x + 0.5f,
                        destination.y + 0.7f,
                        0f
                    );

                    OnEnterProceduralMap?.Invoke();
                }
                
                else
                {
                    Vector2Int dir = entryDir;

                    
                    WorldCoordinateManager.Instance.Move(dir);

                    mapManager.ToggleMapVisibility(true);

                    MapPreset preset =
                        WorldMapManager.Instance.GetMapForCurrentPosition();
                    Vector2Int destination = teleportData.destination;
                    transform.position = new Vector3(
                        destination.x + 0.5f,
                        destination.y + 0.7f,
                        0f
                    );

                    mapManager.SetMapPreset(preset);
                    walkerGenerator.lastEntryDirection = lastMoveDir;
                    // Generate map
                    walkerGenerator.ClearMap();
                    walkerGenerator.GenerateMapWithPreset(preset);
                }

                yield return new WaitForSeconds(0.5f);
            }

            else
            {
                // Leaving procedural → reset state
                if (WorldCoordinateManager.Instance.IsInProceduralMap)
                {
                    WorldCoordinateManager.Instance.IsInProceduralMap = false;
                }

                mapManager.ToggleMapVisibility(false);

                Vector2Int destination = teleportData.destination;
                transform.position = new Vector3(
                    destination.x + 0.5f,
                    destination.y + 0.7f,
                    0f
                );

                yield return new WaitForSeconds(0.3f);
                OnExitProceduralMap?.Invoke();
            }
        }
        else
        {
            Debug.LogWarning("No teleport tile or teleport data found under player");
        }

        canMove = true;
    }

    private IEnumerator HandleInteriorTeleport()
    {
        canMove = false;
        character.Animator.IsMoving = false;
        yield return new WaitForSeconds(0.1f);

        Vector3Int playerTilePos = Vector3Int.FloorToInt(transform.position);
        TileBase tile = null;
        DoorTeleportTile tpTile = null;

        tile = mapManager.interiorDoorTileMap.GetTile(playerTilePos);
        if (tile is DoorTeleportTile t1 && t1.teleportData != null)
        {
            tpTile = t1;
        }
        else
        {
            tile = mapManager.mainDoorTileMap.GetTile(playerTilePos);
            if (tile is DoorTeleportTile t2 && t2.teleportData != null)
            {
                tpTile = t2;
            }
            else
            {
                tile = mapManager.proceduralDoorTileMap.GetTile(playerTilePos);
                if (tile is DoorTeleportTile t3 && t3.teleportData != null)
                {
                    tpTile = t3;
                }
            }
        }

        if (tpTile != null)
        {
            DoorTeleportData teleportData = tpTile.teleportData;

            if (teleportData.goesToMainMap)
            {
                mapManager.ShowInteriorMap(false);
                mapManager.ToggleMapVisibility(false);
            }
            else if (teleportData.goesToProceduralMap)
            {
                mapManager.ShowInteriorMap(false);
                mapManager.ToggleMapVisibility(true);
            }
            else
            {
                mapManager.ShowInteriorMap(true);
            }

            transform.position = new Vector3(teleportData.destination.x + 0.5f, teleportData.destination.y + 0.7f, 0f);
            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Debug.LogWarning("No interior teleport tile or data found under player");
        }

        canMove = true;
    }

    private void CheckIfInTrainersView()
    {
        if (!canMove) return;

        var collider = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.FOVLayer);
        if (collider != null)
        {
            character.Animator.IsMoving = false;
            OnEnterTrainersView?.Invoke(collider);
        }
    }
    private void CheckIfInNPCsView()
    {
        if (!canMove) return;

        var collider = Physics2D.OverlapCircle(transform.position, 0.2f, GameLayers.i.NPCFOVLayer);
        if (collider != null)
        {
            character.Animator.IsMoving = false;
            OnEnterNPCsView?.Invoke(collider);
        }
    }
    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }
}
