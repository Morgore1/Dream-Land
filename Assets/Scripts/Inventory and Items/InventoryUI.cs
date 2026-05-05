using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public Inventory playerInventory;

    [Header("UI")]
    public Transform itemListParent;     // Parent that holds item entries
    public GameObject itemEntryPrefab;   // Prefab for displaying each item (just a Text)
    public Image itemIcon;
    public TextMeshProUGUI itemNameText;
    public TextMeshProUGUI itemDescriptionText;
    public TextMeshProUGUI useOptionText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color highlightColor = Color.yellow;

    public event Action<Item> OnItemUsed;
    private List<GameObject> itemEntries = new List<GameObject>();
    private int selectedIndex = 0;
    private bool onUseOption = false;

    void Start()
    {
        RefreshUI();
    }

    public void CloseInventory()
    {
        onUseOption = false;
        selectedIndex = 0;

        foreach (var entry in itemEntries)
            entry.GetComponentInChildren<TextMeshProUGUI>().color = normalColor;

        var battleSystem = FindObjectOfType<BattleSystem>();
        if (battleSystem != null)
        {
            gameObject.SetActive(false);

            if (battleSystem.state == BattleState.InventoryScreen)
            {
                battleSystem.state = battleSystem.prevState ?? BattleState.ActionSelection;
                battleSystem.prevState = null;
            }
        }
        else
        {
            var pauseMenu = FindObjectOfType<PauseMenu>();
            if (pauseMenu != null)
                pauseMenu.ReturnFromInventory();
            else
                gameObject.SetActive(false); // fallback
        }
    }

    // Update method
    void Update()
    {
        HandleInput();
    }

    void HandleInput()
    {
        if (!onUseOption)
        {
            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) MoveSelection(-1);
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) MoveSelection(1);

            // Only allow going right if the selected item can be used
            var selectedSlot = playerInventory.slots[selectedIndex];
            var selectedItem = selectedSlot.item;

            bool canUse = selectedItem.itemType == ItemType.Consumable || selectedItem.itemType == ItemType.BattleOnly;

            if ((Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) && canUse)
                onUseOption = true;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) onUseOption = false;
            if (Input.GetKeyDown(KeyCode.Z)) UseSelectedItem();
        }

        if (Input.GetKeyDown(KeyCode.X))
            CloseInventory();

        UpdateDetailsPanel();
    }

    void MoveSelection(int direction)
    {
        if (playerInventory.slots.Count == 0) return;

        selectedIndex += direction;
        if (selectedIndex < 0) selectedIndex = playerInventory.slots.Count - 1;
        if (selectedIndex >= playerInventory.slots.Count) selectedIndex = 0;
    }

    public void RefreshUI()
    {
        foreach (Transform child in itemListParent)
            Destroy(child.gameObject);

        itemEntries.Clear();

        foreach (var slot in playerInventory.slots)
        {
            GameObject entry = Instantiate(itemEntryPrefab, itemListParent);
            string displayName = slot.count > 1 ? $"{slot.item.itemName} x{slot.count}" : slot.item.itemName;
            entry.GetComponentInChildren<TextMeshProUGUI>().text = displayName;
            entry.GetComponentInChildren<TextMeshProUGUI>().color = normalColor;
            itemEntries.Add(entry);
        }

        selectedIndex = 0;
        onUseOption = false;
        UpdateDetailsPanel();
    }

    void UpdateDetailsPanel()
    {
        if (playerInventory.slots.Count == 0)
        {
            itemIcon.enabled = false;
            itemNameText.text = "";
            itemDescriptionText.text = "";
            useOptionText.text = "";
            return;
        }

        var selectedSlot = playerInventory.slots[selectedIndex];
        Item selectedItem = selectedSlot.item;

        // Highlight color already set in inspector
        for (int i = 0; i < itemEntries.Count; i++)
        {
            var text = itemEntries[i].GetComponentInChildren<TextMeshProUGUI>();
            text.color = (i == selectedIndex && !onUseOption) ? highlightColor : normalColor;
        }

        itemIcon.enabled = selectedItem.icon != null;
        if (itemIcon.enabled) itemIcon.sprite = selectedItem.icon;
        itemNameText.text = selectedItem.itemName;
        itemDescriptionText.text = selectedItem.description;

        // Only show "Use" if item can be used
        bool canUse = selectedItem.itemType == ItemType.Consumable || selectedItem.itemType == ItemType.BattleOnly;
        if (canUse)
        {
            useOptionText.text = onUseOption ? "> Use <" : "Use";
            useOptionText.color = onUseOption ? highlightColor : normalColor;
        }
        else
        {
            useOptionText.text = "";
        }
    }

    void UseSelectedItem()
    {
        if (playerInventory.slots.Count == 0) return;

        var selectedSlot = playerInventory.slots[selectedIndex];
        Item selectedItem = selectedSlot.item;

        var battle = FindObjectOfType<BattleSystem>();
        bool inBattle = battle != null && battle.state == BattleState.InventoryScreen;

        // Prevent battle-only items from being used outside battle
        if (selectedItem.itemType == ItemType.BattleOnly && !inBattle)
        {
            StartCoroutine(ShowMessage("This item can only be used in battle!"));
            return;
        }

        if (inBattle)
        {
            if (selectedItem is HealingItem healingItem)
            {
                var monsterToHeal = battle.playerParty.GetHealthyMonster();
                if (monsterToHeal == null || monsterToHeal.HP == monsterToHeal.MaxHp)
                {
                    gameObject.SetActive(false);
                    battle.ActionSelection();
                    battle.StartCoroutine(battle.dialogueBox.TypeDialogue("No monsters need healing!"));
                    return;
                }

                // Hide inventory
                gameObject.SetActive(false);

                // Open party screen for healing
                battle.OpenPartyScreenForHealing(healingItem, () =>
                {
                    // HealAndContinueBattle handles enemy turn
                });

                return;
            }

            // Non-healing battle item
            if (selectedItem is CaptureItem captureItem)
            {
                CloseInventory();
                battle.StartCoroutine(battle.UseDreamCatcher(selectedItem));
                return;
            }
        }

        // Normal items that can be used outside battle
        playerInventory.UseItem(selectedIndex, playerInventory.gameObject, inBattle);
        RefreshUI();

        if (playerInventory.slots.Count == 0)
            CloseInventory();

        OnItemUsed?.Invoke(selectedItem);
    }
    private IEnumerator ShowMessage(string message)
    {
        // Optionally, show this on a UI panel or dialogue box
        Debug.Log(message);
        yield return null;
    }

}