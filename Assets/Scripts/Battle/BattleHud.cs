using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [FormerlySerializedAs("levelText")]
    [SerializeField] Text stageText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;

    [SerializeField] Color nmrColor;
    [SerializeField] Color insColor;
    [SerializeField] Color parColor;
    [SerializeField] Color slpColor;

    Monster _monster;
    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Monster monster)
    {
        _monster = monster;

        nameText.text = monster.Base.Name;
        stageText.text = "Stage " + monster.Base.EvolutionStage;
        hpBar.SetHP((float) monster.HP / monster.MaxHp);

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.nmr, nmrColor },
            {ConditionID.ins, insColor },
            {ConditionID.par, parColor },
            {ConditionID.slp, slpColor },
        };

        SetStatusText();
        _monster.OnStatusChanged += SetStatusText;
    }

    void SetStatusText()
    {
        if (_monster.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _monster.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_monster.Status.Id];
        }
    }


    public IEnumerator UpdateHP()
    {
        if (_monster.HpChanged)
        { 
            yield return hpBar.SetHPSmooth((float)_monster.HP / _monster.MaxHp);
            _monster.HpChanged = false;
        }
    }
}
