using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable
{
    [SerializeField] List<Vector2> movementPattern;
    [SerializeField] float timeBetweenPattern;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject Fov;
    [SerializeField] bool isEncounter;

    float idleTimer = 0f;
    NPCState state;
    int currentPattern = 0;

    Character character;

    private Vector3 startingLocalPosition;
    private bool hasCapturedStart = false;

    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void OnEnable()
    {
        CaptureStartingPosition();
    }
    private void Start()
    {
        // FOV setup remains
        if (isEncounter)
        {
            Fov.SetActive(true);
            SetFovRotation(character.Animator.DefaultDirection);
        }
        else
        {
            Fov.SetActive(false);
        }
    }

    /// <summary>
    /// Capture the NPC's starting local position relative to its parent map
    /// </summary>
    public void CaptureStartingPosition()
    {
        if (!hasCapturedStart)
        {
            startingLocalPosition = transform.localPosition;
            hasCapturedStart = true;
        }
    }

    public void ResetToStartPosition()
    {
        StopAllCoroutines();      // stop walking
        state = NPCState.Idle;
        currentPattern = 0;
        idleTimer = 0f;

        transform.localPosition = startingLocalPosition; // teleport back

        character.ForceIdle();
    }

    public void SetIdle()
    {
        state = NPCState.Idle;
        idleTimer = 0f;
    }

    public void Interact(Transform initiator)
    {
        if (state == NPCState.Idle)
        {
            state = NPCState.Dialogue;
            character.LookTowards(initiator.position);

            // Dialogue trigger
            GetComponent<PixelCrushers.DialogueSystem.DialogueSystemTrigger>()?.OnUse();
            Fov.SetActive(false);
        }
    }

    public void NPCEncounterDialogue()
    {
        if (state == NPCState.Idle)
        {
            state = NPCState.Dialogue;
            GetComponent<PixelCrushers.DialogueSystem.DialogueSystemTrigger>()?.OnUse();
        }
    }

    public IEnumerator TriggerEncounterDialogue(PlayerController player)
    {
        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));

        yield return character.Move(moveVec);

        NPCEncounterDialogue();
        Fov.SetActive(false);
    }

    private void Update()
    {
        if (state == NPCState.Idle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer > timeBetweenPattern)
            {
                idleTimer = 0f;
                if (movementPattern.Count > 0)
                    StartCoroutine(Walk());
            }
        }

        character.HandleUpdate();
    }

    IEnumerator Walk()
    {
        state = NPCState.Walking;

        var oldPos = transform.position;

        yield return character.Move(movementPattern[currentPattern]);

        if (transform.position != oldPos)
            currentPattern = (currentPattern + 1) % movementPattern.Count;

        state = NPCState.Idle;
    }

    public void SetFovRotation(FacingDirection dir)
    {
        float angle = 0f;
        if (dir == FacingDirection.Right)
            angle = 90f;
        else if (dir == FacingDirection.Up)
            angle = 180f;
        else if (dir == FacingDirection.Left)
            angle = 270f;

        Fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }
}

public enum NPCState { Idle, Walking, Dialogue }