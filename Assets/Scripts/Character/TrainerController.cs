using System.Collections;
using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public class TrainerController : MonoBehaviour, Interactable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialogue dialogue; 
    [SerializeField] Dialogue dialogueAfterBattle;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject Fov;

    //State
    bool battleLost = false;

    Character character;

    private void Awake()
    {
        character = GetComponent<Character>();
    }
    private void Update()
    {
        character.HandleUpdate();
    }

    private void Start()
    {
        SetFovRotation(character.Animator.DefaultDirection);
    }
    public void Interact(Transform initiator)
    {
        character.LookTowards(initiator.position);

        var dialogueTrigger = GetComponent<NPCDialogueTrigger>();

        if (!battleLost)
        {
            dialogueTrigger.SetUseAlternate(false);
            dialogueTrigger.InteractWithCallback(() => {
                StartTrainerBattle();
            });
        }
        else
        {
            dialogueTrigger.SetUseAlternate(true);
        }

        dialogueTrigger.Interact();
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        exclamation.SetActive(false);

        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));
        yield return character.Move(moveVec);

        // Trigger default dialogue
        GetComponent<PixelCrushers.DialogueSystem.DialogueSystemTrigger>().OnUse();

        // Wait until the dialogue ends
        yield return new WaitUntil(() => !PixelCrushers.DialogueSystem.DialogueManager.IsConversationActive);

        // Then start the trainer battle
        StartTrainerBattle();
    }
    public void StartTrainerBattle()
    {

        GameController.Instance.StartTrainerBattle(this);
    }

    public void BattleLost()
    {
        battleLost = true;
        Fov.SetActive(false);
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

    public string Name
    {
        get => name;
    }

    public Sprite Sprite
    {
        get => sprite;
    }
}
