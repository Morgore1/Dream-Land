using UnityEngine;

[CreateAssetMenu(menuName = "Inventory/Capture Item")]
public class CaptureItem : Item
{
    public override void Use(GameObject user)
    {
        var battle = FindObjectOfType<BattleSystem>();
        if (battle != null)
        {
            battle.StartCoroutine(battle.UseDreamCatcher(this));
        }
    }
}