using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour, Interactable
{
    [Tooltip("ScriptableObject item to give")]
    public Item item;

    [Tooltip("How many of this item to give (1 for a potion)")]
    public int amount = 1;

    [Header("World Sprite")]
    [Tooltip("Optional: Override the sprite shown in the world. If empty, item.icon will be used.")]
    public Sprite worldSprite;

    [Header("Optional feedback")]
    public GameObject pickupEffect;
    public AudioClip pickupSfx;

    [Header("Secondary Effects (random chance on pickup)")]
    public List<SecondaryEffect> secondaryEffects = new List<SecondaryEffect>();

    private void OnValidate()
    {
        // Update the world sprite whenever values change in Inspector
        var sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            if (worldSprite != null)
                sr.sprite = worldSprite;
            else if (item != null && item.icon != null)
                sr.sprite = item.icon;
        }
    }

    public void Interact(Transform initiator)
    {
        if (item == null)
        {
            Debug.LogWarning("ItemPickup has no Item assigned!", this);
            return;
        }

        var inv = initiator.GetComponent<Inventory>();
        if (inv == null)
        {
            Debug.LogWarning("No Inventory component found on " + initiator.name);
            return;
        }

        // Always add the item
        for (int i = 0; i < amount; i++)
            inv.AddItem(item);

        // Optional: update inventory UI if it's currently open
        var invUI = FindObjectOfType<InventoryUI>();
        if (invUI != null && invUI.gameObject.activeInHierarchy)
            invUI.RefreshUI();

        // Play effects before destroy
        if (pickupEffect != null)
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        if (pickupSfx != null)
            AudioSource.PlayClipAtPoint(pickupSfx, Camera.main.transform.position);

        // Destroy immediately so it always disappears
        Destroy(gameObject);

        // Run secondary effects *after* destroying item
        foreach (var effect in secondaryEffects)
        {
            float roll = Random.Range(0f, 100f);
            if (roll <= effect.chancePercent)
            {
                Debug.Log($"Secondary effect triggered: {effect.effectType}");

                if (effect.effectType == SecondaryEffectType.MonsterEncounter)
                {
                    var monster = effect.GetEncounterRandomMonster();
                    if (monster != null)
                        GameController.Instance.StartItemEncounterBattle(monster);
                }

                // TODO: HealPlayer, BuffStat, etc.
            }
        }
    }

    public enum SecondaryEffectType
    {
        None,
        MonsterEncounter,
        HealPlayer,
        BuffStat,
        CustomEvent
    }

    [System.Serializable]
    public class SecondaryEffect
    {
        public SecondaryEffectType effectType;
        [Range(0, 100)] public float chancePercent = 100;

        [Header("For MonsterEncounter")]
        [SerializeField] List<Monster> monsters;

        public Monster GetEncounterRandomMonster()
        {
            if (monsters == null || monsters.Count == 0)
            {
                Debug.LogWarning("No monsters set for this SecondaryEffect");
                return null;
            }

            var chosen = monsters[Random.Range(0, monsters.Count)];
            chosen.Init(); // make sure stats, moves, HP, etc. are rolled
            return chosen;
        }
    }
    [System.Serializable]
    public class EncounterMonsterData
    {
        public MonsterBase monsterBase;
        public int level;
    }
}

