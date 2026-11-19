using System.Collections.Generic;
using PixelCrushers.DialogueSystem;
using UnityEngine;

public sealed class DialogueSpeedRuntimeApplier : MonoBehaviour
{
    private static DialogueSpeedRuntimeApplier instance;
    private readonly HashSet<StandardDialogueUI> appliedDialogueUIs = new HashSet<StandardDialogueUI>();
    private readonly HashSet<DialogueSystemController> configuredManagers = new HashSet<DialogueSystemController>();

    public static void Bootstrap()
    {
        if (instance != null)
            return;

        var applierObject = new GameObject("Dialogue Speed Runtime Applier");
        DontDestroyOnLoad(applierObject);
        instance = applierObject.AddComponent<DialogueSpeedRuntimeApplier>();
    }

    private void Update()
    {
        DialogueSystemController manager = DialogueManager.instance;
        if (manager == null || !manager.isInitialized)
            return;

        ApplyManagerTiming(manager);

        StandardDialogueUI dialogueUI = manager.standardDialogueUI;
        if (dialogueUI == null || appliedDialogueUIs.Contains(dialogueUI))
            return;

        float baselineSpeed = dialogueUI.GetTypewriterSpeed();
        if (baselineSpeed <= 0f)
            return;

        appliedDialogueUIs.Add(dialogueUI);
        dialogueUI.SetTypewriterSpeed(DialogueSpeedSettings.ScaleCharactersPerSecond(baselineSpeed));
    }

    private void ApplyManagerTiming(DialogueSystemController manager)
    {
        if (!configuredManagers.Add(manager) || manager.displaySettings == null)
            return;

        float waitMultiplier = DialogueSpeedSettings.ExtraWaitMultiplier;
        float safeWaitMultiplier = Mathf.Max(0.01f, waitMultiplier);
        var subtitleSettings = manager.displaySettings.subtitleSettings;
        if (subtitleSettings != null)
        {
            subtitleSettings.subtitleCharsPerSecond = Mathf.Max(1f, subtitleSettings.subtitleCharsPerSecond / safeWaitMultiplier);
            subtitleSettings.minSubtitleSeconds *= waitMultiplier;
        }

        var barkSettings = manager.displaySettings.barkSettings;
        if (barkSettings != null)
        {
            if (barkSettings.barkCharsPerSecond > 0f)
                barkSettings.barkCharsPerSecond /= safeWaitMultiplier;
            if (barkSettings.minBarkSeconds > 0f)
                barkSettings.minBarkSeconds *= waitMultiplier;
        }
    }
}
