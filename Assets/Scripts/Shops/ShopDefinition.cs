using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shops/Shop Definition", fileName = "New Shop")]
public class ShopDefinition : ScriptableObject
{
    public string shopName;
    public List<ShopOffer> offers = new List<ShopOffer>();

    private void OnValidate()
    {
        if (offers == null)
            return;

        var unlockIds = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < offers.Count; i++)
        {
            ShopOffer offer = offers[i];
            if (offer == null)
                continue;

            if (string.IsNullOrWhiteSpace(offer.unlockId))
                Debug.LogWarning($"Shop '{name}' offer at index {i} needs a stable unlock ID.", this);
            else if (!unlockIds.Add(offer.unlockId.Trim()))
                Debug.LogWarning($"Shop '{name}' contains duplicate unlock ID '{offer.unlockId}'.", this);

            if (offer.rewardPointCost <= 0)
                Debug.LogWarning($"Shop '{name}' offer '{offer.displayName}' must cost at least 1 reward point.", this);
        }
    }
}

[Serializable]
public class ShopOffer
{
    [Tooltip("Stable ID for the permanent unlock, e.g. unlock.extra_palette. Do not change after release.")]
    public string unlockId;

    public string displayName;

    [TextArea]
    public string description;

    public Sprite image;

    [Min(1)]
    public int rewardPointCost;

    public bool IsValid
    {
        get
        {
            return !string.IsNullOrWhiteSpace(unlockId)
                && rewardPointCost > 0;
        }
    }
}