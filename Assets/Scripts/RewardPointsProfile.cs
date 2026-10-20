using System;
using UnityEngine;

/// <summary>
/// Device-local profile currency. This is deliberately independent of scene objects
/// so the balance survives scene changes and new procedural runs.
/// </summary>
public static class RewardPointsProfile
{
    const string SaveKey = "DreamLand.RewardPoints.Profile";
    const int CurrentVersion = 1;

    [Serializable]
    sealed class SaveData
    {
        public int version = CurrentVersion;
        public int balance;
    }

    static SaveData data;

    public static int Balance
    {
        get
        {
            EnsureLoaded();
            return data.balance;
        }
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
            if (loadedData == null || loadedData.version != CurrentVersion)
            {
                Debug.LogWarning("Reward points profile is invalid or has an unsupported version; starting with zero points.");
                data = new SaveData();
                return;
            }

            loadedData.balance = Math.Max(0, loadedData.balance);
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
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
        PlayerPrefs.Save();
    }
}
