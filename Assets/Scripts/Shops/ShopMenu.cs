using TMPro;
using UnityEngine;

public class ShopMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text shopTitle;
    [SerializeField] Transform offersContainer;
    [SerializeField] ShopOfferView offerRowPrefab;

    public void Open(ShopDefinition shop)
    {
        if (shop == null)
            return;

        if (panel == null || offersContainer == null || offerRowPrefab == null)
        {
            Debug.LogError("ShopMenu is missing a panel, offers container, or offer row prefab.", this);
            return;
        }

        if (shopTitle != null)
            shopTitle.text = string.IsNullOrWhiteSpace(shop.shopName) ? shop.name : shop.shopName;

        foreach (Transform child in offersContainer)
            Destroy(child.gameObject);

        foreach (ShopOffer offer in shop.offers)
        {
            if (offer == null)
                continue;

            ShopOfferView row = Instantiate(offerRowPrefab, offersContainer);
            row.SetOffer(offer);
        }

        panel.SetActive(true);
    }

    public void Close()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}