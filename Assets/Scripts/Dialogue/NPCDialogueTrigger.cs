using UnityEngine;
using PixelCrushers.DialogueSystem;
using System;

public class NPCDialogueTrigger : MonoBehaviour
{
    [SerializeField] private DialogueSystemTrigger defaultTrigger;
    [SerializeField] private DialogueSystemTrigger alternateTrigger;

    private bool useAlternate = false;

    public void SetUseAlternate(bool value)
    {
        useAlternate = value;
    }

    public void Interact()
    {
        if (useAlternate)
            alternateTrigger.OnUse();
        else
            defaultTrigger.OnUse();
    }

    public void InteractWithCallback(Action onDialogueComplete)
    {
        if (useAlternate)
        {
            alternateTrigger.OnUse();
        }
        else
        {
            defaultTrigger.OnUse();
        }

        // Wait for dialogue to finish, then call the callback
        StartCoroutine(WaitForDialogue(onDialogueComplete));
    }

    private System.Collections.IEnumerator WaitForDialogue(Action callback)
    {
        yield return new WaitUntil(() => !DialogueManager.IsConversationActive);
        callback?.Invoke();
    }
}