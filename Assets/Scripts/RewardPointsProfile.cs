using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Device-local profile currency. This is deliberately independent of scene objects
/// so the balance survives scene changes and new procedural runs.
/// </summary>
public static class RewardPointsProfile
{
    const string SaveKey = "DreamLand.RewardPoints.Profile";
    const int CurrentVersion = 2;

    [Serializable]
    sealed class SaveData
    {
        public int version = CurrentVersion;
        public int balance;
        public List<string> ownedUnlockIds = new List<string>();
    }

    public enum PurchaseResult
    {
        Success,
        InvalidOffer,
        AlreadyOwned,
        InsufficientPoints,
        SaveFailed
    }

    public static event Action<string> UnlockOwnershipChanged;

    static SaveData data;

    public static int Balance
    {
        get
        {
            EnsureLoaded();
            return data.balance;
        }
    }

    public static bool IsUnlockOwned(string unlockId)
    {
        EnsureLoaded();
        unlockId = NormalizeUnlockId(unlockId);
        return !string.IsNullOrEmpty(unlockId) && data.ownedUnlockIds.Contains(unlockId);
    }

    /// <summary>
    /// Atomically deducts the cost and records ownership in one profile save.
    /// Unlock IDs are global, so the same unlock can be listed in multiple shops.
    /// </summary>
    public static PurchaseResult TryPurchaseUnlock(string unlockId, int cost)
    {
        EnsureLoaded();
        unlockId = NormalizeUnlockId(unlockId);

        if (string.IsNullOrEmpty(unlockId) || cost <= 0)
            return PurchaseResult.InvalidOffer;

        if (data.ownedUnlockIds.Contains(unlockId))
            return PurchaseResult.AlreadyOwned;

        if (data.balance < cost)
            return PurchaseResult.InsufficientPoints;

        var updatedData = new SaveData
        {
            version = CurrentVersion,
            balance = data.balance - cost,
            ownedUnlockIds = new List<string>(data.ownedUnlockIds) { unlockId }
        };

        if (!TrySave(updatedData))
            return PurchaseResult.SaveFailed;

        data = updatedData;
        UnlockOwnershipChanged?.Invoke(unlockId);
        return PurchaseResult.Success;
    }

    /// <summary>Adds positive points and immediately writes the profile.</summary>
    public static int AddPoints(int amount)
    {
        if (amount <= 0)
            return Balance;

        EnsureLoaded();
        data.balance = (int)Math.Min((long)data.balance + amount, int.MaxValue);
        Save();
        return data.balance;
    }

    /// <summary>Spends points if the amount is positive and the balance is sufficient.</summary>
    public static bool TrySpendPoints(int amount)
    {
        EnsureLoaded();
        if (amount <= 0 || amount > data.balance)
            return false;

        data.balance -= amount;
        Save();
        return true;
    }

    static void EnsureLoaded()
    {
        if (data != null)
            return;

        if (!PlayerPrefs.HasKey(SaveKey))
        {
            data = new SaveData();
            return;
        }

        try
        {
            var loadedData = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(SaveKey));
            if (loadedData == null || (loadedData.version != 1 && loadedData.version != CurrentVersion))
            {
                Debug.LogWarning("Reward points profile is invalid or has an unsupported version; starting with zero points.");
                data = new SaveData();
                return;
            }

            loadedData.balance = Math.Max(0, loadedData.balance);
            if (loadedData.ownedUnlockIds == null)
                loadedData.ownedUnlockIds = new List<string>();

            loadedData.version = CurrentVersion;
            data = loadedData;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not load reward points profile; starting with zero points. {exception.Message}");
            data = new SaveData();
        }
    }

    static void Save()
    {
        TrySave(data);
    }

    static bool TrySave(SaveData saveData)
    {
        try
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(saveData));
            PlayerPrefs.Save();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogError($"Could not save reward points profile. {exception.Message}");
            return false;
        }
    }

    static string NormalizeUnlockId(string unlockId)
    {
        return string.IsNullOrWhiteSpace(unlockId) ? null : unlockId.Trim();
    }
}
