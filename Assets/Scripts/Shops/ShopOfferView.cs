using TMPro;
using UnityEngine;

public class ShopOfferView : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;

    public void SetOffer(ShopOffer offer)
    {
        if (offer == null)
            return;

        if (nameText != null)
            nameText.text = offer.displayName;

        if (descriptionText != null)
            descriptionText.text = offer.description;

        if (costText != null)
            costText.text = $"{offer.rewardPointCost} reward points";
    }
}