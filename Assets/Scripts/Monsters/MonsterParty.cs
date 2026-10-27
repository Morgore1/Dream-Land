using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MonsterParty : MonoBehaviour
{
    [SerializeField] List<Monster> monsters;

    public List<Monster> Monsters
    {
        get
        {
            if (monsters == null)
                monsters = new List<Monster>();

            return monsters;
        }
    }

    private void Start()
    {
        InitParty();
    }

    public void InitParty()
    {
        foreach (var monster in Monsters)
        {
            if (monster != null)
                monster.Init();
        }
    }

    public void SetParty(List<Monster> newParty)
    {
        Monsters.Clear();
        if (newParty == null)
            return;

        foreach (var monster in newParty)
        {
            if (monster == null)
                continue;

            if (Monsters.Count >= 6)
            {
                Debug.LogWarning("Monster party cannot contain more than six monsters.", this);
                break;
            }

            Monsters.Add(monster);
        }
    }

    public Monster GetHealthyMonster()
    {
        return Monsters.FirstOrDefault(monster => monster != null && monster.Base != null && monster.HP > 0);
    }

    public bool AddMonsterToParty(Monster newMonster)
    {
        if (monsters.Count < 6)
        {
            monsters.Add(newMonster);
            return true;
        }
        else
        {
            // TODO: transfer to storage
            return false;
        }
    }

    public void HealAllMonsters()
    {
        foreach (var monster in Monsters)
        {
            if (monster != null)
                monster.RestoreFullHealth();
        }
    }
    public IEnumerator CombineMonster(Monster target)
    {
        if (target == null || target.Base == null || target.Base.EvolutionRequirement <= 0)
        {
            Debug.LogWarning("Cannot award evolution progress to a monster with an invalid evolution requirement.", this);
            yield break;
        }

        target.EvolutionProgress += 1;

        if (target.Base.Evolution != null
            && target.EvolutionProgress >= target.Base.EvolutionRequirement)
        {
            yield return EvolveMonster(target);
        }
    }
    public IEnumerator EvolveMonster(Monster monster)
    {
        if (monster == null || monster.Base == null || monster.Base.Evolution == null)
            yield break;

        float hpRatio = monster.MaxHp > 0 ? (float)monster.HP / monster.MaxHp : 1f;
        bool wasFainted = monster.HP <= 0;
        var newMonster = new Monster(monster.Base.Evolution, false);
        newMonster.InheritMovesAndUnlockSlot(monster);
        newMonster.HP = wasFainted
            ? 0
            : Mathf.Clamp(Mathf.RoundToInt(hpRatio * newMonster.MaxHp), 1, newMonster.MaxHp);
        newMonster.HpChanged = true;

        ReplaceMonster(monster, newMonster);
    }
    public void ReplaceMonster(Monster oldMonster, Monster newMonster)
    {
        int index = Monsters.IndexOf(oldMonster);

        if (index == -1)
        {
            Debug.LogWarning("Monster not found in list!");
            return;
        }

        Monsters[index] = newMonster;
    }
}
