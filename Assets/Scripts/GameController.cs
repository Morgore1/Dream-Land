using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum GameState { FreeRoam, Battle, Dialogue, Cutscene }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    GameState state;

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        ConditionsDB.Init();
    }

    private void Start()
    {
        playerController.OnEncountered += StartBattle;
        battleSystem.OnBattleOver += EndBattle;

        playerController.OnEnterTrainersView += (Collider2D trainerCollider) =>
        {
            var trainer = trainerCollider.GetComponentInParent<TrainerController>();
            if (trainer != null)
            {
                state = GameState.Cutscene;
                StartCoroutine(trainer.TriggerTrainerBattle(playerController));
            }
        };
        playerController.OnEnterNPCsView += (Collider2D NPCCollider) =>
        {
            var trainer = NPCCollider.GetComponentInParent<NPCController>();
            if (trainer != null)
            {
                state = GameState.Cutscene;
                StartCoroutine(trainer.TriggerEncounterDialogue(playerController));
            }
        };
        var player = GameObject.FindGameObjectWithTag("Player");
        var party = player.GetComponent<MonsterParty>();

        var pauseMenu = FindObjectOfType<PauseMenu>();
        pauseMenu.Init(party);
    }

    void StartBattle()
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<MonsterParty>();
        var wildMonster = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildMonster();

        var wildMonsterCopy = new Monster(wildMonster.Base, wildMonster.Level);

        battleSystem.StartBattle(playerParty, wildMonsterCopy);
    }
    public void StartItemEncounterBattle(Monster encounterMonster)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<MonsterParty>();

        var encounterMonsterCopy = new Monster(encounterMonster.Base, encounterMonster.Level);
        battleSystem.StartBattle(playerParty, encounterMonsterCopy);
    }

    TrainerController trainer;
    public void StartTrainerBattle(TrainerController trainer)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        this.trainer = trainer;
        var playerParty = playerController.GetComponent<MonsterParty>();
        var trainerParty = trainer.GetComponent<MonsterParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    void EndBattle(bool won)
    {
        if (trainer != null && won == true)
        {
            trainer.BattleLost();
            trainer = null;
        }

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }

    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();
        }
        else if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
    }

}
