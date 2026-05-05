using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Healing Item", menuName = "Inventory/Healing Item")]
public class HealingItem : Item
{
    public int healAmount = 20;

    private void OnEnable()
    {
        itemType = ItemType.Consumable; // Ensure it's treated as consumable
    }

    public override void Use(GameObject user)
    {
        var pauseMenu = FindObjectOfType<PauseMenu>();
        var inventory = user.GetComponent<Inventory>();

        if (pauseMenu != null && inventory != null)
        {
            // Open party screen to select which monster to heal
            pauseMenu.OpenPartyForHealing(this, inventory);
        }
        else
        {
            Debug.LogWarning("HealingItem: PauseMenu or Inventory not found!");
        }
    }

    /// <summary>
    /// Tries to heal a monster. Returns true if healing happened.
    /// </summary>
    public bool HealMonster(Monster monster)
    {
        if (monster.HP >= monster.MaxHp)
        {
            Debug.Log($"{monster.Base.Name} is already at full HP!");
            return false; // Don’t consume item
        }

        int oldHp = monster.HP;
        monster.HP = Mathf.Clamp(monster.HP + healAmount, 0, monster.MaxHp);

        int healedAmount = monster.HP - oldHp;
        Debug.Log($"{monster.Base.Name} healed {healedAmount} HP! Now {monster.HP}/{monster.MaxHp}.");

        return true; // Item was successfully used
    }
}