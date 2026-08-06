using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

public enum GameState { FreeRoam, Battle, Dialogue, Cutscene }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    [Header("Procedural Encounter")]
    [SerializeField] List<TrainerController> proceduralEncounterOpponents = new List<TrainerController>();
    [SerializeField] int proceduralRounds = 1;
    [SerializeField] int Energy = 10;
    [SerializeField] int proceduralPlayerLives = 1;
    [SerializeField] int proceduralNpcLives = 1;
    [SerializeField] TMP_Text energyText;

    GameState state;

    bool isProceduralEncounterActive;
    bool isProceduralBattle;
    int remainingPlayerLives;
    int remainingNpcLives;
    int currentProceduralRound;
    GameObject proceduralOpponentObject;

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

        playerController.OnEnterProceduralMap += EnableEnergyOverlay;
        playerController.OnExitProceduralMap += DisableEnergyOverlay;
    }

    private void OnDestroy()
    {
        if (playerController != null)
        {
            playerController.OnEnterProceduralMap -= EnableEnergyOverlay;
            playerController.OnExitProceduralMap -= DisableEnergyOverlay;
        }
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
        if (isProceduralBattle)
        {
            if (proceduralOpponentObject != null)
            {
                Destroy(proceduralOpponentObject);
                proceduralOpponentObject = null;
            }

            if (!won)
            {
                remainingPlayerLives--;
            }
            else
            {
                remainingNpcLives--;
            }

            HealPlayerParty();
            isProceduralBattle = false;

            if (remainingPlayerLives > 0 && remainingNpcLives > 0 && currentProceduralRound < proceduralRounds)
            {
                StartNextProceduralRound();
                return;
            }

            EndProceduralEncounter();
            return;
        }

        if (trainer != null && won == true)
        {
            trainer.BattleLost();
            trainer = null;
        }

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);
    }

    private void EnableEnergyOverlay()
    {
        UpdateEnergyText();

        if (energyText != null)
        {
            energyText.gameObject.SetActive(true);
        }
    }

    private void DisableEnergyOverlay()
    {
        if (energyText != null)
        {
            energyText.gameObject.SetActive(false);
        }
    }

    private void UpdateEnergyText()
    {
        if (energyText != null)
        {
            energyText.text = $"Energy ({Energy})";
        }
    }
    public void SpendEnergy(int amount)
    {   
        Energy = Mathf.Max(0, Energy - amount);
        UpdateEnergyText();
    }
    private void StartProceduralEncounter()
    {
        if (Energy > 0)
        {
            return;
        }

        DisableEnergyOverlay();

        if (proceduralEncounterOpponents == null || proceduralEncounterOpponents.Count == 0)
        {
            Debug.LogWarning("Procedural encounter opponent prefabs are not assigned.");
            return;
        }

        if (isProceduralEncounterActive)
            return;

        isProceduralEncounterActive = true;
        remainingPlayerLives = proceduralPlayerLives;
        remainingNpcLives = proceduralNpcLives;
        currentProceduralRound = 0;

        StartNextProceduralRound();
    }

    private void StartNextProceduralRound()
    {
        if (remainingPlayerLives <= 0 || remainingNpcLives <= 0 || currentProceduralRound >= proceduralRounds)
        {
            EndProceduralEncounter();
            return;
        }

        currentProceduralRound++;
        HealPlayerParty();
        SpawnProceduralOpponentAndStartRound();
    }

    private void SpawnProceduralOpponentAndStartRound()
    {
        var opponentPrefab = GetProceduralOpponentPrefabForRound();
        if (opponentPrefab == null)
        {
            Debug.LogWarning("Procedural encounter opponent prefab is not assigned for this round.");
            EndProceduralEncounter();
            return;
        }

        proceduralOpponentObject = Instantiate(opponentPrefab.gameObject);
        proceduralOpponentObject.SetActive(true);

        var trainerController = proceduralOpponentObject.GetComponent<TrainerController>();
        if (trainerController == null)
        {
            Debug.LogWarning("Procedural encounter opponent prefab does not contain TrainerController.");
            Destroy(proceduralOpponentObject);
            proceduralOpponentObject = null;
            EndProceduralEncounter();
            return;
        }

        var trainerParty = proceduralOpponentObject.GetComponent<MonsterParty>() ?? proceduralOpponentObject.GetComponentInChildren<MonsterParty>();
        if (trainerParty == null)
        {
            Debug.LogWarning("Procedural encounter opponent prefab does not contain MonsterParty.");
            Destroy(proceduralOpponentObject);
            proceduralOpponentObject = null;
            EndProceduralEncounter();
            return;
        }

        trainerParty.InitParty();
        proceduralOpponentObject.SetActive(false);

        StartProceduralBattle(trainerController, trainerParty);
    }

    private void StartProceduralBattle(TrainerController trainerController, MonsterParty trainerParty)
    {
        isProceduralBattle = true;
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        var playerParty = playerController.GetComponent<MonsterParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    private void HealPlayerParty()
    {
        var playerParty = playerController.GetComponent<MonsterParty>();
        playerParty.HealAllMonsters();
    }

    private TrainerController GetProceduralOpponentPrefabForRound()
    {
        if (proceduralEncounterOpponents == null || proceduralEncounterOpponents.Count == 0)
            return null;

        int index = Mathf.Clamp(currentProceduralRound - 1, 0, proceduralEncounterOpponents.Count - 1);
        return proceduralEncounterOpponents[index];
    }

    
    private void EndProceduralEncounter()
    {
        isProceduralEncounterActive = false;
        isProceduralBattle = false;

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        if (proceduralOpponentObject != null)
        {
            Destroy(proceduralOpponentObject);
            proceduralOpponentObject = null;
        }
    }

    private void Update()
    {
        if (state == GameState.FreeRoam)
        {
            playerController.HandleUpdate();

            if (Energy <= 0)
            {
                StartProceduralEncounter();
            }
        }
        else if (state == GameState.Battle)
        {
            battleSystem.HandleUpdate();
        }
    }

}
