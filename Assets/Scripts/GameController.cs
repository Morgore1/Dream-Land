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

[System.Serializable]
public class ProceduralTrainerPreset
{
    [SerializeField] TrainerController trainerPrefab;
    [SerializeField, Range(1, 6)] int startingPartySize = 3;
    [SerializeField] List<ProceduralTrainerRoundSettings> rounds = new List<ProceduralTrainerRoundSettings>();

    public TrainerController TrainerPrefab => trainerPrefab;
    public int StartingPartySize => Mathf.Clamp(startingPartySize, 1, 6);
    public List<ProceduralTrainerRoundSettings> Rounds => rounds;
}

public enum GameState { FreeRoam, Battle, Dialogue, Cutscene }

public class GameController : MonoBehaviour
{
    [SerializeField] PlayerController playerController;
    [SerializeField] BattleSystem battleSystem;
    [SerializeField] Camera worldCamera;

    [Header("Procedural Encounter")]
    [SerializeField] List<WeightedItem<ProceduralTrainerPreset>> proceduralTrainerPresets = new List<WeightedItem<ProceduralTrainerPreset>>();
    [SerializeField] int Energy = 10;
    [SerializeField] int proceduralPlayerLives = 1;
    [SerializeField] TMP_Text energyText;
    [SerializeField] TMP_Text fightText;
    [SerializeField] TMP_Text livesText;

    GameState state;

    bool isProceduralEncounterActive;
    bool isProceduralBattle;
    bool battleCompletionHandled;
    bool proceduralRosterInitialized;
    bool proceduralConfigurationWarningLogged;
    ProceduralTrainerPreset selectedProceduralTrainerPreset;
    int remainingPlayerLives;
    int currentFight = 1;
    int proceduralRewardFightNumber;
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
        BeginNonProceduralBattle();
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
        BeginNonProceduralBattle();
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
        BeginNonProceduralBattle();
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
        if (battleCompletionHandled)
            return;

        battleCompletionHandled = true;
        bool completedProceduralBattle = isProceduralBattle;
        int completedFightNumber = proceduralRewardFightNumber;
        isProceduralBattle = false;
        proceduralRewardFightNumber = 0;

        if (completedProceduralBattle && won && completedFightNumber > 0)
        {
            int reward = completedFightNumber > int.MaxValue / 10
                ? int.MaxValue
                : completedFightNumber * 10;
            int newBalance = RewardPointsProfile.AddPoints(reward);
            Debug.Log($"Procedural fight {completedFightNumber} victory: earned {reward} reward points. Balance: {newBalance}.");
        }

        Debug.Log($"Reward points balance after battle: {RewardPointsProfile.Balance}.");

        HealPlayerParty();

        if (completedProceduralBattle)
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

            if (remainingPlayerLives <= 0)
            {
                EndProceduralEncounter();
                ResetProceduralRun();
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
            if (mapManager != null)
                mapManager.PlayCurrentAreaMusic();
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
        if (isProceduralEncounterActive)
            return;

        if (selectedProceduralTrainerPreset == null && !TrySelectProceduralTrainer())
        {
            if (!proceduralConfigurationWarningLogged)
            {
                Debug.LogError("No valid procedural trainer presets are configured. Add weighted trainer prefabs with TrainerController and MonsterParty on the same GameObject.", this);
                proceduralConfigurationWarningLogged = true;
            }
            RestoreEnergyAfterSkippedRound();
            return;
        }

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

    private void ResetProceduralRun()
    {
        selectedProceduralTrainerPreset = null;
        proceduralTrainerRoster.Clear();
        proceduralRosterInitialized = false;
        proceduralTrainerParty = null;
        isProceduralEncounterActive = false;
        isProceduralBattle = false;
        battleCompletionHandled = false;
        proceduralRewardFightNumber = 0;
        roundProgressAwarded = 0;
        currentFight = 1;
        remainingPlayerLives = proceduralPlayerLives;
    }

    private void SpawnProceduralOpponentAndStartFight()
    {
        var opponentPrefab = selectedProceduralTrainerPreset != null ? selectedProceduralTrainerPreset.TrainerPrefab : null;
        if (opponentPrefab == null)
        {
            Debug.LogError("No trainer has been selected for this procedural run.", this);
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

        if (proceduralTrainerRoster.Count == 0 || !proceduralTrainerRoster.Exists(member => member != null && member.Base != null))
        {
            Debug.LogError("Procedural trainer has no monsters. Add an initial monster to its MonsterParty or configure a non-empty round pool.", this);
            isProceduralEncounterActive = false;
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

        StartProceduralBattle(trainerParty);
    }

    private void StartProceduralBattle(MonsterParty trainerParty)
    {
        MonsterParty playerParty = playerController != null ? playerController.GetComponent<MonsterParty>() : null;
        if (playerParty == null || playerParty.GetHealthyMonster() == null || trainerParty == null || trainerParty.GetHealthyMonster() == null)
        {
            Debug.LogError("Cannot start procedural battle: both parties need at least one healthy monster.", this);
            if (proceduralOpponentObject != null)
            {
                Destroy(proceduralOpponentObject);
                proceduralOpponentObject = null;
            }
            proceduralTrainerParty = null;
            isProceduralEncounterActive = false;
            RestoreEnergyAfterSkippedRound();
            return;
        }

        isProceduralBattle = true;
        proceduralRewardFightNumber = currentFight;
        battleCompletionHandled = false;
        state = GameState.Battle;
        battleSystem.gameObject.SetActive(true);
        worldCamera.gameObject.SetActive(false);

        if (MusicManager.Instance != null)
            MusicManager.Instance.PlayBattleMusic();

        battleSystem.StartTrainerBattle(playerParty, trainerParty);
    }

    private void BeginNonProceduralBattle()
    {
        isProceduralBattle = false;
        proceduralRewardFightNumber = 0;
        battleCompletionHandled = false;
    }

    private void HealPlayerParty()
    {
        var playerParty = playerController.GetComponent<MonsterParty>();
        playerParty.HealAllMonsters();
    }

    private bool TrySelectProceduralTrainer()
    {
        var validPresets = new List<WeightedItem<ProceduralTrainerPreset>>();
        if (proceduralTrainerPresets != null)
        {
            foreach (WeightedItem<ProceduralTrainerPreset> entry in proceduralTrainerPresets)
            {
                if (entry == null || entry.item == null || entry.item.TrainerPrefab == null || entry.weight <= 0)
                    continue;

                TrainerController trainerPrefab = entry.item.TrainerPrefab;
                if (trainerPrefab.GetComponent<MonsterParty>() == null)
                {
                    Debug.LogWarning($"Procedural trainer preset '{trainerPrefab.name}' has no MonsterParty on its root and will be ignored.", trainerPrefab);
                    continue;
                }

                List<ProceduralTrainerRoundSettings> rounds = entry.item.Rounds;
                List<MonsterBase> startingPool = rounds != null && rounds.Count > 0
                    ? GetValidMonsterPool(rounds[0])
                    : new List<MonsterBase>();

                if (startingPool.Count == 0)
                {
                    Debug.LogWarning($"Procedural trainer preset '{trainerPrefab.name}' needs at least one valid monster in its first round's monster pool.", trainerPrefab);
                    continue;
                }

                validPresets.Add(entry);
            }
        }

        long totalWeight = 0;
        foreach (WeightedItem<ProceduralTrainerPreset> preset in validPresets)
            totalWeight += preset.weight;

        if (totalWeight <= 0)
            return false;

        long roll = (long)(Random.value * totalWeight);
        foreach (WeightedItem<ProceduralTrainerPreset> preset in validPresets)
        {
            roll -= preset.weight;
            if (roll < 0)
            {
                selectedProceduralTrainerPreset = preset.item;
                proceduralTrainerRoster.Clear();
                proceduralRosterInitialized = false;
                Debug.Log($"Selected procedural trainer '{selectedProceduralTrainerPreset.TrainerPrefab.name}' for this run.", this);
                return true;
            }
        }

        selectedProceduralTrainerPreset = validPresets[validPresets.Count - 1].item;
        proceduralTrainerRoster.Clear();
        proceduralRosterInitialized = false;
        return true;
    }

    private ProceduralTrainerRoundSettings GetProceduralRoundSettingsForFight()
    {
        List<ProceduralTrainerRoundSettings> rounds = selectedProceduralTrainerPreset != null
            ? selectedProceduralTrainerPreset.Rounds
            : null;

        if (rounds == null || rounds.Count == 0)
            return null;

        int index = Mathf.Clamp(currentFight - 1, 0, rounds.Count - 1);
        return rounds[index];
    }

    private bool InitializeProceduralRoster()
    {
        if (proceduralRosterInitialized)
            return true;

        if (selectedProceduralTrainerPreset == null)
            return false;

        proceduralTrainerRoster.Clear();
        List<ProceduralTrainerRoundSettings> rounds = selectedProceduralTrainerPreset.Rounds;
        ProceduralTrainerRoundSettings startingRound = rounds != null && rounds.Count > 0 ? rounds[0] : null;
        List<MonsterBase> startingPool = GetValidMonsterPool(startingRound);
        if (startingPool.Count == 0)
        {
            Debug.LogError($"Selected trainer preset '{selectedProceduralTrainerPreset.TrainerPrefab.name}' has no valid monsters in its first round pool.", this);
            return false;
        }

        int partySize = Mathf.Min(selectedProceduralTrainerPreset.StartingPartySize, MaxTrainerPartySize, startingPool.Count);
        for (int i = 0; i < partySize; i++)
        {
            int selectedIndex = Random.Range(0, startingPool.Count);
            MonsterBase selectedBase = startingPool[selectedIndex];
            startingPool.RemoveAt(selectedIndex);
            proceduralTrainerRoster.Add(new ProceduralTrainerRosterMember(selectedBase, 0));
        }

        proceduralRosterInitialized = true;
        return proceduralTrainerRoster.Count > 0;
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

        var mapManager = FindObjectOfType<MapManager>();
        if (mapManager != null)
            mapManager.PlayCurrentAreaMusic();
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
