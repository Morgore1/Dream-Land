using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopMenu : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text shopTitle;
    [SerializeField] TMP_Text rewardPointBalanceText;
    [SerializeField] Transform offersContainer;
    [SerializeField] ShopOfferView offerRowPrefab;
    [Header("Purchase Confirmation")]
    [SerializeField] GameObject confirmationPanel;
    [SerializeField] TMP_Text confirmationNameText;
    [SerializeField] TMP_Text confirmationDescriptionText;
    [SerializeField] TMP_Text confirmationCostText;
    [SerializeField] Image confirmationImage;
    [SerializeField] TMP_Text confirmationFeedbackText;
    [SerializeField] Button purchaseButton;
    [SerializeField] Button cancelButton;

    readonly List<ShopOfferView> offerRows = new List<ShopOfferView>();
    ShopOffer selectedOffer;

    private void Awake()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.AddListener(ConfirmPurchase);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelPurchase);
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(ConfirmPurchase);
        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(CancelPurchase);
    }

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

        selectedOffer = null;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);

        foreach (Transform child in offersContainer)
            Destroy(child.gameObject);
        offerRows.Clear();

        var seenUnlockIds = new HashSet<string>();

        if (shop.offers != null)
        {
            foreach (ShopOffer offer in shop.offers)
            {
                if (offer == null)
                    continue;

                if (offer.IsValid && !seenUnlockIds.Add(offer.unlockId.Trim()))
                    Debug.LogWarning($"Shop '{shop.name}' lists unlock '{offer.unlockId}' more than once.", shop);

                ShopOfferView row = Instantiate(offerRowPrefab, offersContainer);
                offerRows.Add(row);
                row.SetOffer(offer, ShowConfirmation, RewardPointsProfile.IsUnlockOwned(offer.unlockId));
            }
        }

        RefreshBalance();
        panel.SetActive(true);
    }

    public void ShowConfirmation(ShopOffer offer)
    {
        if (offer == null || !offer.IsValid || confirmationPanel == null)
        {
            Debug.LogWarning("Cannot show purchase confirmation: the offer is invalid or the confirmation panel is not assigned.", this);
            return;
        }

        selectedOffer = offer;
        if (confirmationNameText != null)
            confirmationNameText.text = offer.displayName;
        if (confirmationDescriptionText != null)
            confirmationDescriptionText.text = offer.description;
        if (confirmationCostText != null)
            confirmationCostText.text = $"{offer.rewardPointCost} reward points";
        if (confirmationFeedbackText != null)
            confirmationFeedbackText.text = string.Empty;
        if (confirmationImage != null)
        {
            confirmationImage.sprite = offer.image;
            confirmationImage.gameObject.SetActive(offer.image != null);
        }

        confirmationPanel.SetActive(true);
    }

    public void ConfirmPurchase()
    {
        if (selectedOffer == null)
            return;

        RewardPointsProfile.PurchaseResult result = RewardPointsProfile.TryPurchaseUnlock(
            selectedOffer.unlockId,
            selectedOffer.rewardPointCost);

        if (result == RewardPointsProfile.PurchaseResult.Success)
        {
            selectedOffer = null;
            if (confirmationPanel != null)
                confirmationPanel.SetActive(false);
            RefreshOfferRows();
            RefreshBalance();
            return;
        }

        if (confirmationFeedbackText != null)
            confirmationFeedbackText.text = GetPurchaseFailureMessage(result);

        RefreshOfferRows();
        RefreshBalance();
    }

    public void CancelPurchase()
    {
        selectedOffer = null;
        if (confirmationPanel != null)
            confirmationPanel.SetActive(false);
    }

    public void Close()
    {
        CancelPurchase();
        if (panel != null)
            panel.SetActive(false);
    }

    void RefreshOfferRows()
    {
        foreach (ShopOfferView row in offerRows)
        {
            if (row == null)
                continue;

            // The instantiated row stores its currently displayed offer.
            row.RefreshOwnership(RewardPointsProfile.IsUnlockOwned(row.UnlockId));
        }
    }

    void RefreshBalance()
    {
        if (rewardPointBalanceText != null)
            rewardPointBalanceText.text = $"Reward points: {RewardPointsProfile.Balance}";
    }

    static string GetPurchaseFailureMessage(RewardPointsProfile.PurchaseResult result)
    {
        switch (result)
        {
            case RewardPointsProfile.PurchaseResult.AlreadyOwned:
                return "You already own this unlock.";
            case RewardPointsProfile.PurchaseResult.InsufficientPoints:
                return "You don't have enough reward points.";
            case RewardPointsProfile.PurchaseResult.SaveFailed:
                return "Purchase could not be saved. No points were spent.";
            default:
                return "This offer is unavailable.";
        }
    }
}