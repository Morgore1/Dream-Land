using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.nmr,
            new Condition()
            {
                Name = "Nightmare",
                StartMessage = "is having a nightmare!",
                OnAfterTurn = (Monster monster) =>
                {
                    monster.UpdateHp(monster.MaxHp / 7);
                    monster.StatusChanges.Enqueue($"{monster.Base.Name} damaged itself due to the nightmare");
                }
            }
        },
        { ConditionID.par,
            new Condition()
            {
                Name = "Sleep Paralyze",
                StartMessage = "is stuck in sleep paralysis!",
                OnBeforeMove = (Monster monster) =>
                {
                    if (Random.Range(1, 5) == 1)
                    {
                        monster.CureStatus();
                        monster.StatusChanges.Enqueue($"{monster.Base.Name} broke through the sleep paralysis!");
                        return true;
                    }
                    return false;
                }
            }
        },
        { ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep!",
                OnStart = (Monster monster) =>
                {
                    // sleep for 1-3 turns
                    monster.StatusTime = Random.Range(1,4);
                    Debug.Log($"Will be asleep for {monster.StatusTime} moves");
                },
                OnBeforeMove = (Monster monster) =>
                {
                    if (monster.StatusTime <= 0)
                    {
                        monster.CureStatus();
                         monster.StatusChanges.Enqueue($"{monster.Base.Name} woke up!");
                        return true;
                    }

                    monster.StatusTime--;
                    monster.StatusChanges.Enqueue($"{monster.Base.Name} is sleeping");
                    return false;
                }
            }
        },

        //Volatile Status Conditions 
        {
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                StartMessage = "has been confused",
                OnStart = (Monster monster) =>
                {
                    // Confused for 1-4 turns
                    monster.StatusTime = Random.Range(1,5);
                    Debug.Log($"Will be confused for {monster.VolatileStatusTime} moves");
                },
                OnBeforeMove = (Monster monster) =>
                {
                    if (monster.VolatileStatusTime <= 0)
                    {
                        monster.CureVolatileStatus();
                         monster.StatusChanges.Enqueue($"{monster.Base.Name} broke itself out of confusion!");
                        return true;
                    }

                    monster.VolatileStatusTime--;

                    //50% chance to do a move
                    if (Random.Range(1, 3) == 1)
                        return true;
                    
                    //hurt by confusion
                    monster.StatusChanges.Enqueue($"{monster.Base.Name} is confused");
                    monster.UpdateHp(monster.MaxHp / 10);
                    monster.StatusChanges.Enqueue($"It got confused and damaged itself");
                    return false;
                }
            }
        }
        
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
        else if (condition.Id == ConditionID.slp)
            return 1.75f;
        else if (condition.Id == ConditionID.nmr)
            return 2f;
        else if (condition.Id == ConditionID.par)
            return 1.25f;
        else if (condition.Id == ConditionID.ins)
            return 1.5f;
        else
            return 1f;
    }
}

public enum ConditionID
{
    none, nmr, ins, par, slp, 
    confusion
}
