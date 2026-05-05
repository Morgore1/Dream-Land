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
            return monsters;
        }
    }

    private void Start()
    {
        foreach (var monster in monsters)
        {
            monster.Init();
        }
    }

    public Monster GetHealthyMonster()
    {
        return monsters.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddMonsterToParty(Monster newMonster)
    {
        if (monsters.Count < 6)
        {
            monsters.Add(newMonster);
        }
        else
        {
            // TODO: transfer to storage
        }
    }
    public IEnumerator CombineMonster(Monster target)
    {
        target.EvolutionProgress += 1;

        if (target.EvolutionProgress >= target.Base.EvolutionRequirement)
        {
            yield return EvolveMonster(target);
        }
    }
    public IEnumerator EvolveMonster(Monster monster)
    {
        if (monster.Base.Evolution == null)
            yield break;

        var newMonster = new Monster(monster.Base.Evolution, monster.Level);

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
