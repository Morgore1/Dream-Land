using System;
using TMPro;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine;

public class ShopOfferView : MonoBehaviour
{
    [SerializeField] TMP_Text nameText;
    [SerializeField] TMP_Text descriptionText;
    [SerializeField] TMP_Text costText;
    [SerializeField] Button costButton;

    UnityAction clickHandler;
    ShopOffer displayedOffer;

    public string UnlockId => displayedOffer != null ? displayedOffer.unlockId : null;

    public void SetOffer(ShopOffer offer, Action<ShopOffer> onSelected, bool isOwned)
    {
        if (offer == null)
            return;

        displayedOffer = offer;

        if (nameText != null)
            nameText.text = offer.displayName;

        if (descriptionText != null)
            descriptionText.text = offer.description;

        RefreshOwnership(isOwned);

        if (costButton == null && costText != null)
            costButton = costText.GetComponent<Button>();

        // Keep the existing text-based row prefab clickable even if a Button
        // component hasn't been added to its cost label in the Inspector yet.
        if (costButton == null && costText != null)
            costButton = costText.gameObject.AddComponent<Button>();

        if (costButton != null)
        {
            if (costText != null && costButton.targetGraphic == null)
                costButton.targetGraphic = costText;

            if (clickHandler != null)
                costButton.onClick.RemoveListener(clickHandler);

            clickHandler = () => onSelected?.Invoke(offer);
            costButton.onClick.AddListener(clickHandler);
            costButton.interactable = offer.IsValid && !isOwned && onSelected != null;
        }
    }

    public void RefreshOwnership(bool isOwned)
    {
        if (displayedOffer == null)
            return;

        bool isValid = displayedOffer.IsValid;
        if (costText != null)
            costText.text = isOwned ? "Owned" : isValid ? $"{displayedOffer.rewardPointCost} reward points" : "Unavailable";

        if (costButton == null && costText != null)
            costButton = costText.GetComponent<Button>();

        if (costButton != null)
            costButton.interactable = isValid && !isOwned && clickHandler != null;
    }
}