using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ItemType { Consumable, Key, Equipment, BattleOnly }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public string description;

    public virtual void Use(GameObject user)
    {
        Debug.Log("Using " + itemName);
    }
}