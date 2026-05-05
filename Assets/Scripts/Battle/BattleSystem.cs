using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using JetBrains.Annotations;
using Unity.VisualScripting;
#if UNITY_EDITOR
using UnityEditor.Experimental.GraphView;
#endif
using UnityEngine;
using UnityEngine.UI;

public enum BattleState { Start, ActionSelection, MoveSelection, RunningTurn, Busy, PartyScreen, InventoryScreen, BattleOver }
public enum BattleAction { Move, SwitchMonster, UseItem, Run}

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] public BattleDialogueBox dialogueBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject dreamCatcherPrefab;
    [SerializeField] GameObject moveAnimationPrefab;
    [SerializeField] Transform battleCanvasTransform;
    [SerializeField] InventoryUI inventoryUI;

    [SerializeField] List<Sprite> dreamCatcherSuccessfulFrames;
    [SerializeField] List<Sprite> dreamCatcherUnsuccessful1Frames;
    [SerializeField] List<Sprite> dreamCatcherUnsuccessful2Frames;
    [SerializeField] List<Sprite> dreamCatcherUnsuccessful3Frames;
    [SerializeField] float dreamCatcherFrameRate = 12f;
    public event Action<HealingItem> OnHealingFinished;
    public bool isUsingHealingItem = false;
    public HealingItem currentHealingItem = null;

    public event Action<bool> OnBattleOver;

    public BattleState state;
    public BattleState? prevState;
    int currentAction;
    int currentMove;
    int currentMember;

    int numberOfEscapeAttempts;

    public MonsterParty playerParty;
    MonsterParty trainerParty;
    Monster wildMonster;

    public bool isTrainerBattle = false;
    PlayerController player;
    TrainerController trainer;

    public void StartBattle(MonsterParty playerParty, Monster wildMonster)
    {
        this.playerParty = playerParty;
        this.wildMonster = wildMonster;
        player = playerParty.GetComponent<PlayerController>();
        isTrainerBattle = false;

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(MonsterParty playerParty, MonsterParty trainerParty)
    {
        this.playerParty = playerParty;
        this.trainerParty = trainerParty;

        isTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        trainer = trainerParty.GetComponent<TrainerController>();

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        playerUnit.Clear();
        enemyUnit.Clear();

        if (isTrainerBattle == false)
        {
            // Wild Monster Battle
            playerUnit.Setup(playerParty.GetHealthyMonster());
            enemyUnit.Setup(wildMonster);

            dialogueBox.SetMoveNames(playerUnit.Monster.Moves);
            yield return dialogueBox.TypeDialogue($"A roaming {enemyUnit.Monster.Base.Name} appeared.");
        }
        else
        {
            // Trainer battle

            // Show trainer and player sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = trainer.Sprite;

            yield return dialogueBox.TypeDialogue($"{trainer.Name} wants to battle");

            // Send out first monster of the trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyMonster = trainerParty.GetHealthyMonster();
            enemyUnit.Setup(enemyMonster);
            yield return dialogueBox.TypeDialogue($"{trainer.Name} sent out {enemyMonster.Base.Name}");

            // Send out first monster of the player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerMonster = playerParty.GetHealthyMonster();
            playerUnit.Setup(playerMonster);
            yield return dialogueBox.TypeDialogue($"Go {playerMonster.Base.Name}!");
            dialogueBox.SetMoveNames(playerUnit.Monster.Moves);
        }

        numberOfEscapeAttempts = 0;
        partyScreen.init();
        ActionSelection();
    }

    void BattleOver(bool won)
    {
        state = BattleState.BattleOver;
        playerParty.Monsters.ForEach(p => p.OnBattleOver());
        OnBattleOver(won);
    }

    void MoveSelection()
    {
        state = BattleState.MoveSelection;
        dialogueBox.EnableActionSelector(false);
        dialogueBox.EnableDialogueText(false);
        dialogueBox.EnableMoveSelector(true);
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {
        state = BattleState.RunningTurn;

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Monster.CurrentMove = playerUnit.Monster.Moves[currentMove];
            enemyUnit.Monster.CurrentMove = enemyUnit.Monster.GetRandomMove();

            int playerMovePriority = playerUnit.Monster.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Monster.CurrentMove.Base.Priority;

            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if (enemyMovePriority == playerMovePriority)
                playerGoesFirst = playerUnit.Monster.Speed >= enemyUnit.Monster.Speed;

            var firstUnit = playerGoesFirst ? playerUnit : enemyUnit;
            var secondUnit = playerGoesFirst ? enemyUnit : playerUnit;
            var secondMonster = secondUnit.Monster;

            // First turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Monster.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (state == BattleState.BattleOver) yield break;

            // Second turn if the monster is still alive
            if (secondMonster.HP > 0)
            {
                yield return RunMove(secondUnit, firstUnit, secondUnit.Monster.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchMonster)
            {
                var selectedMonster = playerParty.Monsters[currentMember];
                state = BattleState.Busy;
                yield return SwitchMonster(selectedMonster);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                state = BattleState.InventoryScreen;
                inventoryUI.gameObject.SetActive(true);
                inventoryUI.RefreshUI();

                bool itemUsed = false;

                void OnUsed(Item item)
                {
                    if (item is HealingItem healing)
                    {
                        currentHealingItem = healing;
                        isUsingHealingItem = true;
                    }
                    itemUsed = true;
                }

                inventoryUI.OnItemUsed += OnUsed;

                // Wait until the player selects an item
                yield return new WaitUntil(() => itemUsed);

                inventoryUI.OnItemUsed -= OnUsed;
                inventoryUI.gameObject.SetActive(false);

                if (!isUsingHealingItem)
                {
                    // Non-healing item used, enemy takes turn
                    state = BattleState.RunningTurn;
                    var enemyMoveAfterItem = enemyUnit.Monster.GetRandomMove();
                    yield return RunMove(enemyUnit, playerUnit, enemyMoveAfterItem);
                    yield return RunAfterTurn(enemyUnit);
                }
                else
                {
                    // Healing item flow handled in HandlePartySelection
                    yield break;
                }
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToRunAway();
            }

            // Only run enemy move if not using a healing item
            if (playerAction != BattleAction.UseItem || !isUsingHealingItem)
            {
                var enemyMove = enemyUnit.Monster.GetRandomMove();
                yield return RunMove(enemyUnit, playerUnit, enemyMove);
                yield return RunAfterTurn(enemyUnit);
                if (state == BattleState.BattleOver) yield break;
            }
        }

        if (state != BattleState.BattleOver)
            ActionSelection();
    }


    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Monster.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Monster);
            yield return sourceUnit.Hud.UpdateHP();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Monster);

        move.AP--;
        yield return dialogueBox.TypeDialogue($"{sourceUnit.Monster.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Monster, targetUnit.Monster))
        {
            sourceUnit.PlayAttackAnimation();
            yield return sourceUnit.PlayMoveEffect(move.Base, battleCanvasTransform);
            targetUnit.PlayHitAnimation();

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Monster, targetUnit.Monster, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Monster.TakeDamage(move, sourceUnit.Monster);
                yield return targetUnit.Hud.UpdateHP();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Monster.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Monster, targetUnit.Monster, secondary.Target);
                }
            }

            if (targetUnit.Monster.HP <= 0)
            {
                yield return dialogueBox.TypeDialogue($"{targetUnit.Monster.Base.Name} Fainted");
                targetUnit.PlayFaintAnimation();
                yield return new WaitForSeconds(2f);

                CheckForBattleOver(targetUnit);
            }
        }
        else
        {
            yield return dialogueBox.TypeDialogue($"{sourceUnit.Monster.Base.Name}'s attack missed");
        }


    }

        IEnumerator RunMoveEffects(MoveEffects effects, Monster source, Monster target, MoveTarget moveTarget)
        {
            // Stat Boosting
            if (effects.Boosts != null)
            {
                if (moveTarget == MoveTarget.Self)
                    source.ApplyBoosts(effects.Boosts);
                else
                    target.ApplyBoosts(effects.Boosts);
            }


            // Status Condition
            if (effects.Status != ConditionID.none)
            {
                target.SetStatus(effects.Status);
            }
        
            // Volatile Status Condition
            if (effects.VolatileStatus != ConditionID.none)
            {
                target.SetVolatileStatus(effects.VolatileStatus);
            }

            yield return ShowStatusChanges(source);
            yield return ShowStatusChanges(target);
        }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (state == BattleState.BattleOver) yield break;
        yield return new WaitUntil(() => state == BattleState.RunningTurn);

        // statuses like nightmare
        sourceUnit.Monster.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Monster);
        yield return sourceUnit.Hud.UpdateHP();
        if (sourceUnit.Monster.HP <= 0)
        {
            yield return dialogueBox.TypeDialogue($"{sourceUnit.Monster.Base.Name} Fainted");
            sourceUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);

            CheckForBattleOver(sourceUnit);
            yield return new WaitUntil(() => state == BattleState.RunningTurn);
        }
    }

    bool CheckIfMoveHits(Move move, Monster source, Monster target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }
    IEnumerator ShowStatusChanges(Monster monster)
        {
            while (monster.StatusChanges.Count > 0)
            {
                var message = monster.StatusChanges.Dequeue();
                yield return dialogueBox.TypeDialogue(message);
            }
        }

        void CheckForBattleOver(BattleUnit faintedUnit)
        {
            if (faintedUnit.IsPlayerUnit)
            {
                var nextMonster = playerParty.GetHealthyMonster();
                if (nextMonster != null)
                    OpenPartyScreen();
                else
                    BattleOver(false);
            }
            else
            {   
                if (!isTrainerBattle) 
                { 
                    BattleOver(true); 
                }
                else
                {
                    var nextMonster = trainerParty.GetHealthyMonster();
                if (nextMonster != null)
                    StartCoroutine(SendNextTrainerMonster(nextMonster));
                else
                    BattleOver(true);
                }
            }
        }

        IEnumerator ShowDamageDetails(DamageDetails damageDetails)
        {
            if (damageDetails.Critical > 1f)
                yield return dialogueBox.TypeDialogue("A critical hit!");

            if (damageDetails.TypeEffectiveness > 1f)
                yield return dialogueBox.TypeDialogue("It's super effective!");
            else if (damageDetails.TypeEffectiveness < 1f)
                yield return dialogueBox.TypeDialogue("It's not very effective!");
        }
        public void ActionSelection()
        {
            state = BattleState.ActionSelection;
            dialogueBox.SetDialogue("Choose an action");
            dialogueBox.EnableActionSelector(true);
        }
    void OpenInventory()
    {
        state = BattleState.InventoryScreen;
        inventoryUI.gameObject.SetActive(true);
        inventoryUI.RefreshUI();
    }
    private Action onHealingFinished;
    public void OpenPartyScreenForHealing(HealingItem item, Action onHealFinished)
    {
        isUsingHealingItem = true;
        currentHealingItem = item;

        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Monsters);
        partyScreen.gameObject.SetActive(true);

        currentMember = 0;
        partyScreen.UpdateMemberSelection(currentMember);
        partyScreen.SetMessageText("Select a monster to heal!");

        // Store callback
        onHealingFinished = onHealFinished;
    }
    public void OpenPartyScreen()
    {
        state = BattleState.PartyScreen;
        partyScreen.SetPartyData(playerParty.Monsters);
        partyScreen.gameObject.SetActive(true);
    }
    
    public void HandleUpdate()
   {
        if (state == BattleState.ActionSelection)
        {
            HandleActionSelection();
        }
        else if (state == BattleState.MoveSelection)
        {
            HandleMoveSelection();
        }
        else if (state == BattleState.PartyScreen)
        {
            HandlePartySelection();
        }
        else if (state == BattleState.InventoryScreen)
        {
            // Let InventoryUI handle input itself
        }
    }

        void HandleActionSelection()
        {
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                currentAction += 2;
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                currentAction -= 2;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                ++currentAction;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                --currentAction;

            currentAction = Mathf.Clamp(currentAction, 0, 3);

            dialogueBox.UpdateActionSelection(currentAction);

            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (currentAction == 0)
                {
                    // Fight
                    MoveSelection();
                }
                else if (currentAction == 1)
                {
                    // Run
                    StartCoroutine(RunTurns(BattleAction.Run));
                }
                else if (currentAction == 2)
                {
                    // Items
                    prevState = state;        // Save state
                    OpenInventory();          // Change state and show UI
                }
                else if (currentAction == 3)
                {
                    //Switch
                    prevState = state;
                    OpenPartyScreen();
                }
            }
        }

        void HandleMoveSelection()
        {

            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
                currentMove += 2;
            else if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow))
                currentMove -= 2;
            else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
                ++currentMove;
            else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
                --currentMove;

            currentMove = Mathf.Clamp(currentMove, 0, playerUnit.Monster.Moves.Count - 1);

            dialogueBox.UpdateMoveSelection(currentMove, playerUnit.Monster.Moves[currentMove]);

            if (Input.GetKeyDown(KeyCode.Z))
            {
                var move = playerUnit.Monster.Moves[currentMove];
                if (move.AP == 0) return;

                dialogueBox.EnableMoveSelector(false);
                dialogueBox.EnableDialogueText(true);
                StartCoroutine(RunTurns(BattleAction.Move));
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                dialogueBox.EnableMoveSelector(false);
                dialogueBox.EnableDialogueText(true);
                ActionSelection();
            }

        }

    void HandlePartySelection()
    {
        // Navigate the party
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) currentMember -= 2;
        else if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) currentMember += 2;
        else if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) currentMember--;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) currentMember++;

        currentMember = Mathf.Clamp(currentMember, 0, playerParty.Monsters.Count - 1);
        partyScreen.UpdateMemberSelection(currentMember);

        var selectedMonster = playerParty.Monsters[currentMember];

        if (isUsingHealingItem)
        {
            if (Input.GetKeyDown(KeyCode.Z))
            {
                if (selectedMonster.HP == selectedMonster.MaxHp)
                {
                    StartCoroutine(dialogueBox.TypeDialogue($"{selectedMonster.Base.Name} already has full health!"));
                    return;
                }

                partyScreen.gameObject.SetActive(false);

                // Heal and continue battle
                StartCoroutine(HealAndContinueBattle(selectedMonster));
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                isUsingHealingItem = false;
                currentHealingItem = null;

                partyScreen.gameObject.SetActive(false);
                state = BattleState.InventoryScreen;
            }

            return; // prevent normal switch logic while healing
        }

        // Normal switch logic
        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (selectedMonster.HP <= 0)
            {
                partyScreen.SetMessageText("This creature has already fainted");
                return;
            }

            if (selectedMonster == playerUnit.Monster)
            {
                partyScreen.SetMessageText("This creature is already in battle");
                return;
            }

            partyScreen.gameObject.SetActive(false);

            if (prevState == BattleState.ActionSelection)
            {
                prevState = null;
                StartCoroutine(RunTurns(BattleAction.SwitchMonster));
            }
            else
            {
                state = BattleState.Busy;
                StartCoroutine(SwitchMonster(selectedMonster));
            }
        }

        // Cancel switching
        if (Input.GetKeyDown(KeyCode.X))
        {
            partyScreen.gameObject.SetActive(false);
            ActionSelection();
        }
    }
    IEnumerator HealAndContinueBattle(Monster selectedMonster)
    {
        // Heal the monster
        currentHealingItem.HealMonster(selectedMonster);

        // Update HUD if this is the active monster
        if (selectedMonster == playerUnit.Monster)
        {
            playerUnit.Hud.SetData(playerUnit.Monster);
            yield return playerUnit.Hud.UpdateHP();
        }

        yield return dialogueBox.TypeDialogue($"{selectedMonster.Base.Name} was healed!");

        // Reset healing flags
        isUsingHealingItem = false;
        currentHealingItem = null;

        // Call the continuation callback if one exists
        onHealingFinished?.Invoke();
        onHealingFinished = null;

        // Return to normal battling mechanics
        yield return AfterItem();
    }
    IEnumerator AfterItem()
    {
        state = BattleState.RunningTurn;

        // Enemy takes a turn
        var enemyMove = enemyUnit.Monster.GetRandomMove();
        yield return RunMove(enemyUnit, playerUnit, enemyMove);
        yield return RunAfterTurn(enemyUnit);

        // Return to player's action selection if battle is not over
        if (state != BattleState.BattleOver)
            ActionSelection();
    }

    IEnumerator SwitchMonster(Monster newMonster)
        {
            if (playerUnit.Monster.HP > 0)
            {
                yield return dialogueBox.TypeDialogue($"Come back {playerUnit.Monster.Base.Name}");
                playerUnit.PlayFaintAnimation();
                yield return new WaitForSeconds(0.5f);
            }

            playerUnit.Setup(newMonster);
            dialogueBox.SetMoveNames(newMonster.Moves);
            yield return dialogueBox.TypeDialogue($"Go {newMonster.Base.Name}!");

            state = BattleState.RunningTurn;
        }

    IEnumerator SendNextTrainerMonster(Monster nextMonster)
    {
        state = BattleState.Busy;

        enemyUnit.Setup(nextMonster);
        yield return dialogueBox.TypeDialogue($"{trainer.Name} sent out {nextMonster.Base.Name}!");

        state = BattleState.RunningTurn;
    }

    public IEnumerator UseDreamCatcher(Item item)
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue($"Hey! No stealing!");
            ActionSelection();
            yield break;
        }

        yield return dialogueBox.TypeDialogue($"{player.Name} used {item.itemName}!");


        // Optional: manually flip or rotate the image if needed
        // catcherGO.transform.localScale = new Vector3(isPlayerUnit ? 1 : -1, 1, 1);



        int shakeCount = CalculateCaptureResult(enemyUnit.Monster);
        yield return new WaitForSeconds(0.5f);

        yield return enemyUnit.PlayCaptureAnimation();

        var catcherGO = Instantiate(dreamCatcherPrefab, battleCanvasTransform); // Assign this prefab!

        var catcherAnim = catcherGO.GetComponent<DreamCatcherAnimation>();

        if (shakeCount == 4)
        {
            yield return catcherAnim.Play(dreamCatcherSuccessfulFrames, dreamCatcherFrameRate);
            yield return dialogueBox.TypeDialogue($"{enemyUnit.Monster.Base.Name} was successfully caught!");
            yield return HandleCapturedMonster(enemyUnit.Monster);
            BattleOver(true);
        }
        else
        {

            if (shakeCount == 1)
            {
                yield return catcherAnim.Play(dreamCatcherUnsuccessful1Frames, dreamCatcherFrameRate);
                yield return enemyUnit.PlayBreakOutAnimation();
                yield return dialogueBox.TypeDialogue($"{enemyUnit.Monster.Base.Name} easily escaped.");
            }
            else if (shakeCount == 2)
            {
                yield return catcherAnim.Play(dreamCatcherUnsuccessful2Frames, dreamCatcherFrameRate);
                yield return enemyUnit.PlayBreakOutAnimation();
                yield return dialogueBox.TypeDialogue($"{enemyUnit.Monster.Base.Name} escaped.");
            }
            else
            {
                yield return catcherAnim.Play(dreamCatcherUnsuccessful3Frames, dreamCatcherFrameRate);
                yield return enemyUnit.PlayBreakOutAnimation();
                yield return dialogueBox.TypeDialogue($"{enemyUnit.Monster.Base.Name} was almost trapped within the dream catcher.");
            }
            // Return to normal battling mechanics
            yield return AfterItem();
        }
    }

    public static int CalculateCaptureResult(Monster monster)
    {
        float maxHP = monster.MaxHp;
        float currentHP = monster.HP;
        float baseRate = monster.Base.CatchRate;
        float statusBonus = ConditionsDB.GetStatusBonus(monster.Status);

        float a = ((3f * maxHP - 2f * currentHP) * baseRate * statusBonus) / (3f * maxHP);

        if (a >= 255f)
            return 4;

        float b = 1048560f / Mathf.Sqrt(Mathf.Sqrt(16711680f / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            int roll = UnityEngine.Random.Range(0, 65535); // range is 0–65534
            if (roll >= b)
                break;

            ++shakeCount;
        }

        return shakeCount;
    }

    IEnumerator TryToRunAway()
    {
        state = BattleState.Busy;

        if (isTrainerBattle)
        {
            yield return dialogueBox.TypeDialogue($"Don't run from me!");
            state = BattleState.RunningTurn;
            yield break;
        }

        ++numberOfEscapeAttempts;

        int playerSpeed = playerUnit.Monster.Speed;
        int enemySpeed = enemySpeed = enemyUnit.Monster.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogueBox.TypeDialogue($"You easily escaped.");
            BattleOver(true);
        }
        else
        {
            float d = (playerSpeed * 128) / enemySpeed + 30 * numberOfEscapeAttempts;
            d = d % 256;

            if (UnityEngine.Random.Range(0, 256) < d)
            {
                yield return dialogueBox.TypeDialogue($"You somehow managed to succesfully escape!");
                BattleOver(true);
            }
            else
            {
                yield return dialogueBox.TypeDialogue($"You couldn't escape!");
                state = BattleState.RunningTurn;
            }
        }
    }
    IEnumerator HandleCapturedMonster(Monster newMonster)
    {
        var compatible = playerParty.Monsters
            .Where(m =>
                m.Base.FamilyID == newMonster.Base.FamilyID &&
                m.Base.Evolution != null &&
                m.Base.Evolution.FamilyID == newMonster.Base.FamilyID
            )
            .ToList();

        if (compatible.Count > 0)
        {
            // TODO: replace with real UI later
            Monster target = compatible[0];

            yield return dialogueBox.TypeDialogue(
                $"Combine {newMonster.Base.Name} with {target.Base.Name}?"
            );


            StartCoroutine(playerParty.CombineMonster(target));

            yield return dialogueBox.TypeDialogue(
                $"{target.Base.Name} gained evolution progress!"
            );
        }
        else
        {
            playerParty.AddMonsterToParty(newMonster);

            yield return dialogueBox.TypeDialogue(
                $"{newMonster.Base.Name} has taken a spot in your party."
            );
        }
    }
    
    
}
