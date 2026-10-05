using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class ProceduralTrainerRoundSettings
{
    [SerializeField] List<MonsterBase> monsterPool = new List<MonsterBase>();
    [SerializeField, Range(0f, 1f)] float replacementChance;

    public List<MonsterBase> MonsterPool => monsterPool;
    public float ReplacementChance => replacementChance;
}

public enum GameState { FreeRoam, Battle, Dialogue, Cutscene }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    [Header("Procedural Encounter")]
    [SerializeField] TrainerController proceduralOpponent;
    [SerializeField] List<ProceduralTrainerRoundSettings> proceduralRounds = new List<ProceduralTrainerRoundSettings>();
    // Retained to migrate scenes/prefabs serialized with the previous multi-opponent setup.
    [SerializeField, HideInInspector] List<TrainerController> proceduralEncounterOpponents = new List<TrainerController>();
    [SerializeField] int Energy = 10;
    [SerializeField] int proceduralPlayerLives = 1;
    [SerializeField] TMP_Text energyText;
    [SerializeField] TMP_Text fightText;
    [SerializeField] TMP_Text livesText;

    GameState state;

    bool isProceduralEncounterActive;
    bool isProceduralBattle;
    bool proceduralRosterInitialized;
    bool proceduralConfigurationWarningLogged;
    int remainingPlayerLives;
    int currentFight = 1;
    int maximumEnergy;
    int roundProgressAwarded;
    GameObject proceduralOpponentObject;
    MonsterParty proceduralTrainerParty;
    readonly List<ProceduralTrainerRosterMember> proceduralTrainerRoster = new List<ProceduralTrainerRosterMember>();

    const int MaxTrainerPartySize = 6;
    const int MaxRoundEvolutionProgress = 6;

    [System.Serializable]
    class ProceduralTrainerRosterMember
    {
        public MonsterBase Base;
        public int EvolutionProgress;

        public ProceduralTrainerRosterMember(MonsterBase monsterBase, int evolutionProgress)
        {
            Base = monsterBase;
            EvolutionProgress = Mathf.Max(0, evolutionProgress);
        }
    }

    public static GameController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        maximumEnergy = Energy;
        remainingPlayerLives = proceduralPlayerLives;
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

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayBattleMusic();
        }

        var playerParty = playerController.GetComponent<MonsterParty>();
        var wildMonster = FindObjectOfType<MapArea>().GetComponent<MapArea>().GetRandomWildMonster();

        var wildMonsterCopy = new Monster(wildMonster.Base);

        battleSystem.StartBattle(playerParty, wildMonsterCopy);
    }
    public void StartItemEncounterBattle(Monster encounterMonster)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayBattleMusic();
        }

        var playerParty = playerController.GetComponent<MonsterParty>();

        var encounterMonsterCopy = new Monster(encounterMonster.Base);
        battleSystem.StartBattle(playerParty, encounterMonsterCopy);
    }

    TrainerController trainer;
    public void StartTrainerBattle(TrainerController trainer)
    {
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        if (MusicManager.Instance != null)
        {
            MusicManager.Instance.PlayBattleMusic();
        }

        this.trainer = trainer;
        var playerParty = playerController.GetComponent<MonsterParty>();
        var trainerParty = trainer.GetComponent<MonsterParty>();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    void EndBattle(bool won)
    {
        HealPlayerParty();

        if (isProceduralBattle)
        {
            CaptureProceduralRoster();
            AwardProceduralBattleProgress();

            if (proceduralOpponentObject != null)
            {
                Destroy(proceduralOpponentObject);
                proceduralOpponentObject = null;
            }
            proceduralTrainerParty = null;

            if (!won)
            {
                remainingPlayerLives--;
                UpdateLivesText();
            }

            currentFight++;
            UpdateFightText();

            isProceduralBattle = false;

            if (remainingPlayerLives <= 0)
            {
                EndProceduralEncounter();
                ReturnToMainMenu();
                return;
            }

            Energy = maximumEnergy;
            UpdateEnergyText();
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

        if (MusicManager.Instance != null)
        {
            var mapManager = FindObjectOfType<MapManager>();
            if (mapManager != null && mapManager.previousMapType == MapManager.MapType.Procedural)
            {
                MusicManager.Instance.PlayRouteMapMusic();
            }
            else
            {
                MusicManager.Instance.PlayMainMapMusic();
            }
        }
    }

    private void EnableEnergyOverlay()
    {
        UpdateEnergyText();
        UpdateFightText();
        UpdateLivesText();

        if (energyText != null)
        {
            energyText.gameObject.SetActive(true);
        }

        if (fightText != null)
        {
            fightText.gameObject.SetActive(true);
        }

        if (livesText != null)
        {
            livesText.gameObject.SetActive(true);
        }
    }

    private void DisableEnergyOverlay()
    {
        if (energyText != null)
        {
            energyText.gameObject.SetActive(false);
        }

        if (fightText != null)
        {
            fightText.gameObject.SetActive(false);
        }

        if (livesText != null)
        {
            livesText.gameObject.SetActive(false);
        }
    }

    private void UpdateEnergyText()
    {
        if (energyText != null)
        {
            energyText.text = $"Energy ({Energy})";
        }
    }

    private void UpdateFightText()
    {
        if (fightText != null)
        {
            fightText.text = $"Fight ({currentFight})";
        }
    }

    private void UpdateLivesText()
    {
        if (livesText != null)
        {
            livesText.text = $"Lives ({remainingPlayerLives})";
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
        var configuredOpponent = GetProceduralOpponent();
        if (configuredOpponent == null || configuredOpponent.GetComponent<MonsterParty>() == null)
        {
            if (!proceduralConfigurationWarningLogged)
            {
                Debug.LogWarning("Procedural encounter requires a trainer prefab with TrainerController and MonsterParty on the same GameObject. Assign the new opponent field or retain an entry in the legacy opponent list.", this);
                proceduralConfigurationWarningLogged = true;
            }
            RestoreEnergyAfterSkippedRound();
            return;
        }

        if (isProceduralEncounterActive)
            return;

        isProceduralEncounterActive = true;
        if (remainingPlayerLives <= 0)
        {
            remainingPlayerLives = proceduralPlayerLives;
            currentFight = 1;
            proceduralTrainerRoster.Clear();
            proceduralRosterInitialized = false;
        }
        UpdateFightText();
        UpdateLivesText();

        HealPlayerParty();
        SpawnProceduralOpponentAndStartFight();
    }

    private void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        PauseMenu.GameIsPaused = false;
        SceneManager.LoadScene("MainMenu");
    }

    private void SpawnProceduralOpponentAndStartFight()
    {
        var opponentPrefab = GetProceduralOpponent();
        if (opponentPrefab == null)
        {
            RestoreEnergyAfterSkippedRound();
            return;
        }

        var roundSettings = GetProceduralRoundSettingsForFight();
        if (!InitializeProceduralRoster())
        {
            RestoreEnergyAfterSkippedRound();
            return;
        }

        if (roundSettings != null)
        {
            PrepareProceduralRoundRoster(roundSettings);
        }

        // An empty authored team can still start if the configured pool supplies a monster.
        if (proceduralTrainerRoster.Count == 0 && roundSettings != null)
        {
            var validPool = GetValidMonsterPool(roundSettings);
            if (validPool.Count > 0)
                proceduralTrainerRoster.Add(new ProceduralTrainerRosterMember(validPool[Random.Range(0, validPool.Count)], 0));
        }

        if (proceduralTrainerRoster.Count == 0)
        {
            Debug.LogError("Procedural trainer has no monsters. Add an initial monster to its MonsterParty or configure a non-empty round pool.", this);
            RestoreEnergyAfterSkippedRound();
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
            RestoreEnergyAfterSkippedRound();
            return;
        }

        var trainerParty = proceduralOpponentObject.GetComponent<MonsterParty>();
        if (trainerParty == null)
        {
            Debug.LogWarning("Procedural opponent must have TrainerController and MonsterParty on the same GameObject so BattleSystem can resolve the trainer.", this);
            Destroy(proceduralOpponentObject);
            proceduralOpponentObject = null;
            RestoreEnergyAfterSkippedRound();
            return;
        }

        var battleMonsters = new List<Monster>();
        foreach (var member in proceduralTrainerRoster)
        {
            var monster = new Monster(member.Base);
            monster.EvolutionProgress = member.EvolutionProgress;
            battleMonsters.Add(monster);
        }
        trainerParty.SetParty(battleMonsters);
        proceduralTrainerParty = trainerParty;
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

    private TrainerController GetProceduralOpponent()
    {
        if (proceduralOpponent != null)
            return proceduralOpponent;

        if (proceduralEncounterOpponents != null)
        {
            foreach (var legacyOpponent in proceduralEncounterOpponents)
            {
                if (legacyOpponent != null)
                    return legacyOpponent;
            }
        }

        return null;
    }

    private ProceduralTrainerRoundSettings GetProceduralRoundSettingsForFight()
    {
        if (proceduralRounds == null || proceduralRounds.Count == 0)
            return null;

        int index = Mathf.Clamp(currentFight - 1, 0, proceduralRounds.Count - 1);
        return proceduralRounds[index];
    }

    private bool InitializeProceduralRoster()
    {
        if (proceduralRosterInitialized)
            return true;

        var opponentPrefab = GetProceduralOpponent();
        var initialParty = opponentPrefab != null ? opponentPrefab.GetComponent<MonsterParty>() : null;
        if (initialParty == null)
            return false;

        proceduralTrainerRoster.Clear();
        foreach (var monster in initialParty.Monsters)
        {
            if (monster == null || monster.Base == null)
                continue;

            if (proceduralTrainerRoster.Count >= MaxTrainerPartySize)
            {
                Debug.LogWarning("Procedural trainer's authored party has more than six monsters; extra members are ignored.", this);
                break;
            }

            var member = new ProceduralTrainerRosterMember(monster.Base, monster.EvolutionProgress);
            NormalizeRosterMember(member);
            proceduralTrainerRoster.Add(member);
        }

        proceduralRosterInitialized = true;
        return true;
    }

    private void PrepareProceduralRoundRoster(ProceduralTrainerRoundSettings roundSettings)
    {
        roundProgressAwarded = 0;

        var pool = GetValidMonsterPool(roundSettings);
        int numberOfPicks = Random.Range(0, 3);
        for (int i = 0; i < numberOfPicks && pool.Count > 0; i++)
        {
            var selectedBase = pool[Random.Range(0, pool.Count)];
            var existingMember = proceduralTrainerRoster.Find(member => member.Base == selectedBase);

            if (existingMember != null)
            {
                if (AwardEvolutionProgress(existingMember))
                    roundProgressAwarded++;
            }
            else if (proceduralTrainerRoster.Count < MaxTrainerPartySize)
            {
                proceduralTrainerRoster.Add(new ProceduralTrainerRosterMember(selectedBase, 0));
            }
        }

        if (proceduralTrainerRoster.Count == MaxTrainerPartySize
            && (roundSettings.ReplacementChance >= 1f
                || Random.value < Mathf.Clamp01(roundSettings.ReplacementChance)))
        {
            TryReplaceLowestProgressMember(pool);
        }
    }

    private List<MonsterBase> GetValidMonsterPool(ProceduralTrainerRoundSettings roundSettings)
    {
        var pool = new List<MonsterBase>();
        if (roundSettings == null || roundSettings.MonsterPool == null)
            return pool;

        foreach (var monsterBase in roundSettings.MonsterPool)
        {
            if (monsterBase != null)
                pool.Add(monsterBase);
        }

        return pool;
    }

    private void TryReplaceLowestProgressMember(List<MonsterBase> pool)
    {
        var replacementCandidates = pool.FindAll(monsterBase =>
            !proceduralTrainerRoster.Exists(member => member.Base == monsterBase));

        if (replacementCandidates.Count == 0 || proceduralTrainerRoster.Count == 0)
            return;

        int lowestProgress = int.MaxValue;
        var replacementIndices = new List<int>();
        for (int i = 0; i < proceduralTrainerRoster.Count; i++)
        {
            int progress = proceduralTrainerRoster[i].EvolutionProgress;
            if (progress < lowestProgress)
            {
                lowestProgress = progress;
                replacementIndices.Clear();
                replacementIndices.Add(i);
            }
            else if (progress == lowestProgress)
            {
                replacementIndices.Add(i);
            }
        }

        int indexToReplace = replacementIndices[Random.Range(0, replacementIndices.Count)];
        var replacement = replacementCandidates[Random.Range(0, replacementCandidates.Count)];
        proceduralTrainerRoster[indexToReplace] = new ProceduralTrainerRosterMember(replacement, 0);
    }

    private void CaptureProceduralRoster()
    {
        if (proceduralTrainerParty == null)
            return;

        proceduralTrainerRoster.Clear();
        foreach (var monster in proceduralTrainerParty.Monsters)
        {
            if (monster == null || monster.Base == null)
                continue;

            if (proceduralTrainerRoster.Count >= MaxTrainerPartySize)
                break;

            var member = new ProceduralTrainerRosterMember(monster.Base, monster.EvolutionProgress);
            NormalizeRosterMember(member);
            proceduralTrainerRoster.Add(member);
        }
    }

    private void AwardProceduralBattleProgress()
    {
        int pointsToAward = Random.Range(1, 4);
        int remainingBudget = Mathf.Max(0, MaxRoundEvolutionProgress - roundProgressAwarded);
        pointsToAward = Mathf.Min(pointsToAward, remainingBudget);

        for (int i = 0; i < pointsToAward; i++)
        {
            var eligibleMembers = proceduralTrainerRoster.FindAll(CanReceiveEvolutionProgress);
            if (eligibleMembers.Count == 0)
                break;

            var member = eligibleMembers[Random.Range(0, eligibleMembers.Count)];
            if (AwardEvolutionProgress(member))
                roundProgressAwarded++;
        }
    }

    private bool CanReceiveEvolutionProgress(ProceduralTrainerRosterMember member)
    {
        return member != null
            && member.Base != null
            && member.Base.EvolutionRequirement > 0
            && member.EvolutionProgress < member.Base.EvolutionRequirement;
    }

    private bool AwardEvolutionProgress(ProceduralTrainerRosterMember member)
    {
        if (!CanReceiveEvolutionProgress(member))
            return false;

        member.EvolutionProgress++;
        if (member.EvolutionProgress >= member.Base.EvolutionRequirement)
        {
            if (member.Base.Evolution != null)
            {
                member.Base = member.Base.Evolution;
                member.EvolutionProgress = 0;
            }
            else
            {
                member.EvolutionProgress = member.Base.EvolutionRequirement;
            }
        }

        return true;
    }

    private void NormalizeRosterMember(ProceduralTrainerRosterMember member)
    {
        if (member == null || member.Base == null)
            return;

        int requirement = member.Base.EvolutionRequirement;
        if (requirement <= 0)
        {
            member.EvolutionProgress = 0;
            Debug.LogWarning($"Monster '{member.Base.Name}' has a non-positive evolution requirement; trainer evolution progress was reset.", this);
            return;
        }

        member.EvolutionProgress = Mathf.Clamp(member.EvolutionProgress, 0, requirement);
        if (member.Base.Evolution != null && member.EvolutionProgress >= requirement)
        {
            member.Base = member.Base.Evolution;
            member.EvolutionProgress = 0;
        }
    }

    private void RestoreEnergyAfterSkippedRound()
    {
        Energy = maximumEnergy;
        UpdateEnergyText();
        EndProceduralEncounter();
    }

    private void EndProceduralEncounter()
    {
        isProceduralEncounterActive = false;
        isProceduralBattle = false;
        EnableEnergyOverlay();

        state = GameState.FreeRoam;
        battleSystem.gameObject.SetActive(false);
        worldCamera.gameObject.SetActive(true);

        if (proceduralOpponentObject != null)
        {
            Destroy(proceduralOpponentObject);
            proceduralOpponentObject = null;
        }
        proceduralTrainerParty = null;
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
