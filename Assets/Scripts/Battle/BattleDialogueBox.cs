using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class BattleDialogueBox : MonoBehaviour
{
    [SerializeField] int lettersPerSecond;
    [SerializeField] Color highlightedColor;

    [SerializeField] Text dialogueText;
    [SerializeField] GameObject actionSelector;
    [SerializeField] GameObject moveSelector;
    [SerializeField] GameObject moveDetails;

    [SerializeField] List<Text> actionTexts;
    [SerializeField] List<Text> moveTexts;

    [SerializeField] Text apText;
    [SerializeField] Text typeText;

    // Keep track of the current typing coroutine
    private Coroutine typingCoroutine;

    public void SetDialogue(string dialogue)
    {
        dialogueText.text = dialogue;
    }

    public IEnumerator TypeDialogue(string dialogue)
    {
        // Stop the previous typing if it exists
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        typingCoroutine = StartCoroutine(TypeDialogueCoroutine(dialogue));
        yield return typingCoroutine;
        typingCoroutine = null;
    }

    private IEnumerator TypeDialogueCoroutine(string dialogue)
    {
        dialogueText.text = "";
        foreach (var letter in dialogue.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        yield return new WaitForSeconds(0.5f);
    }

    public void EnableDialogueText(bool enabled)
    {
        dialogueText.enabled = enabled;
    }

    public void EnableActionSelector(bool enabled)
    {
        actionSelector.SetActive(enabled);
    }

    public void EnableMoveSelector(bool enabled)
    {
        moveSelector.SetActive(enabled);
        moveDetails.SetActive(enabled);
    }

    public void UpdateActionSelection(int selectedAction)
    {
        for (int i = 0; i < actionTexts.Count; ++i)
        {
            actionTexts[i].color = (i == selectedAction) ? highlightedColor : Color.black;
        }
    }

    public void UpdateMoveSelection(int selectedMove, Move move)
    {
        for (int i = 0; i < moveTexts.Count; ++i)
        {
            moveTexts[i].color = (i == selectedMove) ? highlightedColor : Color.black;
        }

        apText.text = $"AP {move.AP}/{move.Base.AP}";
        typeText.text = move.Base.Type.ToString();
        apText.color = (move.AP == 0) ? Color.red : Color.black;
    }

    public void SetMoveNames(List<Move> moves)
    {
        for (int i = 0; i < moveTexts.Count; ++i)
        {
            moveTexts[i].text = (i < moves.Count) ? moves[i].Base.Name : "-";
        }
    }
}
