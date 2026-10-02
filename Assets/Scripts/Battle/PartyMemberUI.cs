using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [FormerlySerializedAs("levelText")]
    [SerializeField] Text stageText;
    [SerializeField] HPBar hpBar;

    [SerializeField] Color highlightedColor;

    Monster _monster;

    public void SetData(Monster monster)
    {
        _monster = monster;

        nameText.text = monster.Base.Name;
        stageText.text = "Stage " + monster.Base.EvolutionStage;
        hpBar.SetHP((float)monster.HP / monster.MaxHp);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            nameText.color = highlightedColor;
        else
            nameText.color = Color.black;
    }
}
