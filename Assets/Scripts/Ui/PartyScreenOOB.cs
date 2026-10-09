using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreenOOB : MonoBehaviour
{
    [SerializeField] Text messageText;

    [Header("Monster Details")]
    [SerializeField] GameObject detailsPanel;
    [SerializeField] Image monsterSprite;
    [SerializeField] TMP_Text monsterNameText;
    [SerializeField] TMP_Text statsText;
    [SerializeField] TMP_Text stageText;
    [SerializeField] TMP_Text evolutionProgressText;
    [SerializeField] TMP_Text descriptionText;

    PartyMemberUI[] memberSlots;
    List<Monster> monsters;

    public bool IsDetailsOpen => detailsPanel != null && detailsPanel.activeSelf;

    public void init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
        HideMonsterDetails();
    }

    public void SetPartyData(List<Monster> monsters)
    {
        this.monsters = monsters ?? new List<Monster>();

        if (memberSlots == null)
            memberSlots = GetComponentsInChildren<PartyMemberUI>(true);

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < this.monsters.Count && this.monsters[i] != null)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].SetData(this.monsters[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        if (messageText != null)
            messageText.text = "Select a monster";
    }
 
    public void UpdateMemberSelection(int selectedMember)
    {
        if (memberSlots == null || monsters == null)
            return;

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (memberSlots[i] != null)
                memberSlots[i].SetSelected(i == selectedMember && i < monsters.Count);
        }

    }

    public void SetMessageText(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }

    public void ShowMonsterDetails(Monster monster)
    {
        if (detailsPanel == null)
        {
            Debug.LogWarning("PartyScreenOOB: Monster details panel is not assigned.", this);
            return;
        }

        if (monster == null || monster.Base == null)
        {
            Debug.LogWarning("PartyScreenOOB: Cannot show details for a missing monster or MonsterBase.", this);
            return;
        }

        var monsterBase = monster.Base;
        if (monsterSprite != null)
        {
            monsterSprite.sprite = monsterBase.FrontSprite;
            monsterSprite.enabled = monsterBase.FrontSprite != null;
        }

        if (monsterNameText != null)
            monsterNameText.text = monsterBase.Name;

        if (statsText != null)
        {
            statsText.text =
                $"Health\n{monster.HP}/{monster.MaxHp}\n" +
                $"Attk\n{monster.Attack}\n" +
                $"Def\n{monster.Defense}\n" +
                $"Mgk.Attk\n{monster.MagicAttack}\n" +
                $"Mgk.Def\n{monster.MagicDefense}\n" +
                $"Speed\n{monster.Speed}";
        }
        else
        {
            Debug.LogWarning("PartyScreenOOB: Stats text is not assigned; monster stats cannot be displayed.", this);
        }

        if (stageText != null)
            stageText.text = monsterBase.Evolution != null
                ? $"Stage {monsterBase.EvolutionStage}"
                : "Stage Max";

        if (evolutionProgressText != null)
        {
            string progressLabel = monsterBase.Evolution != null
                ? $"Evolution Progress: {monster.EvolutionProgress}/{monsterBase.EvolutionRequirement}"
                : $"Evolution Progress: {monster.EvolutionProgress}/{monsterBase.EvolutionRequirement}";
            evolutionProgressText.text = progressLabel;
        }

        if (descriptionText != null)
            descriptionText.text = monsterBase.Description;

        detailsPanel.SetActive(true);
    }

    public void HideMonsterDetails()
    {
        if (detailsPanel != null)
            detailsPanel.SetActive(false);
    }
}

