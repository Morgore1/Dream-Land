using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum ItemType { Key, Equipment, BattleOnly }

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class Item : ScriptableObject
{
    public string itemName;
    public Sprite icon;
    public ItemType itemType;
    public bool isConsumable;
    public bool isUsable;
    public string description;

    public virtual void Use(GameObject user)
    {
        Debug.Log("Using " + itemName);
    }
}