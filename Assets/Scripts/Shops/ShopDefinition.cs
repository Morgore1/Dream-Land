using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shops/Shop Definition", fileName = "New Shop")]
public class ShopDefinition : ScriptableObject
{
    public string shopName;
    public List<ShopOffer> offers = new List<ShopOffer>();
}

[Serializable]
public class ShopOffer
{
    public string displayName;

    [TextArea]
    public string description;

    [Min(0)]
    public int rewardPointCost;
}