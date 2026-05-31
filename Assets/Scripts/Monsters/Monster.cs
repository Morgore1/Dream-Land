using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Monster
{
    [SerializeField] MonsterBase _base;
    [SerializeField] int level;

    public Monster(MonsterBase mBase, int mLevel)
    {
        if (mBase == null)
        {
            Debug.LogError("MonsterBase is NULL when creating Monster!");
            return;
        }

        _base = mBase;
        level = mLevel;

        Init();
    }

    public MonsterBase Base {
        get {
            return _base;
        }
    }
    public int Level {
        get {
            return level;
        }
    }
    [SerializeField] int evolutionProgress = 0;

    public int EvolutionProgress
    {
        get => evolutionProgress;
        set => evolutionProgress = value;
    }

    public int HP { get; set; }
    public List<Move> Moves { get; set; }
    public Move CurrentMove { get; set; }
    public Dictionary<Stat, int> Stats { get; private set; }

    public Dictionary<Stat, int> StatBoosts { get; private set; }
    public Condition Status { get; private set; }
    public int StatusTime { get; set; }
    public Condition VolatileStatus { get; set; }
    public int VolatileStatusTime { get; set; }

    public Queue<string> StatusChanges { get; private set; }
    public bool HpChanged { get; set; }
    public event System.Action OnStatusChanged;

    public void Init()
    {
        // Generate Moves
        Moves = new List<Move>();
        foreach (var move in Base.LearnableMoves)
        {
            if (move.Level <= Level)
                Moves.Add(new Move(move.Base));

            if (Moves.Count >= 4)
                break;
        }

        CalculateStats();
        HP = MaxHp;

        StatusChanges = new Queue<string>();
        ResetStatboost();
        Status = null;
        VolatileStatus = null;
    }

    void CalculateStats()
    {
        Stats = new Dictionary<Stat, int>
    {
        { Stat.Attack, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5 },
        { Stat.Defense, Mathf.FloorToInt((Base.Defense * Level) / 100f) + 5 },
        { Stat.MagicAttack, Mathf.FloorToInt((Base.MagicAttack * Level) / 100f) + 5 },
        { Stat.MagicDefense, Mathf.FloorToInt((Base.MagicDefense * Level) / 100f) + 5 },
        { Stat.Speed, Mathf.FloorToInt((Base.Speed * Level) / 100f) + 5 }
         };


        MaxHp = Mathf.FloorToInt((Base.MaxHp * Level) / 100f) + 10 + Level;
    }

    void ResetStatboost()
    {
        StatBoosts = new Dictionary<Stat, int>()
        {
            {Stat.Attack, 0},
            {Stat.Defense, 0},
            {Stat.MagicAttack, 0},
            {Stat.MagicDefense, 0},
            {Stat.Speed, 0},
            {Stat.Accuracy, 0 },
            {Stat.Evasion, 0 },
        };
    }

    int GetStat(Stat stat)
    {
        int statVal = Stats[stat];

        //Apply stat boost
        int boost = StatBoosts[stat];
        var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

        if (boost >= 0)
            statVal = Mathf.FloorToInt(statVal * boostValues[boost]);
        else
            statVal = Mathf.FloorToInt(statVal / boostValues[-boost]);

        return statVal;


    }

    public void ApplyBoosts(List<StatBoost> statBoosts)
    {
         foreach (var statBoost in statBoosts)
        {
            var stat = statBoost.stat;
            var boost = statBoost.boost;

            StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost, -6, 6);

            if (boost > 0)
                StatusChanges.Enqueue($"{Base.Name}'s {stat} increased!");
            else
                StatusChanges.Enqueue($"{Base.Name}'s {stat} decreased!");

            Debug.Log($"{stat} has been boosted to {StatBoosts[stat]}");

        }
    }


    public int Attack
    {
        get { return GetStat(Stat.Attack); }
    }

    public int Defense
    {
        get { return GetStat(Stat.Defense); }
    }

    public int MagicAttack
    {
        get { return GetStat(Stat.MagicAttack); }
    }

    public int MagicDefense
    {
        get { return GetStat(Stat.MagicDefense); }
    }


    public int MaxHp { get; private set; }

    public int Speed
    {
        get { return GetStat(Stat.Speed); }
    }

    public DamageDetails TakeDamage(Move move, Monster attacker)
    {
        float critical = 1f;
        if (UnityEngine.Random.value * 100f <= 5f)
            critical = 2f;

        float type = TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type2);

        var damageDetails = new DamageDetails()
        {
            TypeEffectiveness = type,
            Critical = critical,
            Fainted = false,
        };

        float attack = (move.Base.Category == MoveCategory.Magical) ? attacker.MagicAttack : attacker.Attack;
        float defense = (move.Base.Category == MoveCategory.Magical) ? MagicDefense : Defense;

        float modifiers = (UnityEngine.Random.Range(0.9f, 1f) * type * critical);
        float a = (2 * attacker.Level + 10) / 250f;
        float d = a * move.Base.Power * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        UpdateHp(damage);

        return damageDetails;
    }
    
    public void UpdateHp(int damage)
    {
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        HpChanged = true;
    }
    public void Heal(int amount)
    {
        HP = Mathf.Clamp(HP + amount, 0, MaxHp);
        HpChanged = true;  // so the UI knows HP was updated
    }

    public void RestoreFullHealth()
    {
        HP = MaxHp;
        Status = null;
        VolatileStatus = null;
        StatusTime = 0;
        VolatileStatusTime = 0;
        ResetStatboost();
        StatusChanges.Clear();
        HpChanged = true;
        OnStatusChanged?.Invoke();
    }

    public void SetStatus(ConditionID conditionID)
    {
        if (Status != null) return;

        Status = ConditionsDB.Conditions[conditionID];
        Status?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }

    public void SetVolatileStatus(ConditionID conditionID)
    {
        if (VolatileStatus != null) return;

        VolatileStatus = ConditionsDB.Conditions[conditionID];
        VolatileStatus?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.name} {VolatileStatus.StartMessage}");
    }

    public void CureStatus()
    {
        Status = null;
        OnStatusChanged?.Invoke();
    }

    public void CureVolatileStatus()
    {
        VolatileStatus = null;
        OnStatusChanged?.Invoke();
    }
    public Move GetRandomMove()
    {
        var movesWithAP = Moves.Where(x => x.AP > 0).ToList();

        int r = UnityEngine.Random.Range(0, Moves.Count);
        return movesWithAP[r];
    }

    public bool OnBeforeMove() 
    {
        bool canPerformMove = true;
        if (Status?.OnBeforeMove != null)
        {
            if (!Status.OnBeforeMove(this))
                canPerformMove = false;
        }

        if (VolatileStatus?.OnBeforeMove != null)
        {
            if (!VolatileStatus.OnBeforeMove(this))
                canPerformMove = false;
        }

        return canPerformMove; 
    }

    public void OnAfterTurn()
    {
        Status?.OnAfterTurn?.Invoke(this);
        VolatileStatus?.OnAfterTurn?.Invoke(this);
    }

    public void OnBattleOver()
    {
        VolatileStatus = null;
        ResetStatboost();
    }
}

public class DamageDetails
{
    public bool Fainted { get; set; }
    public float Critical { get; set; }
    public float TypeEffectiveness { get; set; }
}

