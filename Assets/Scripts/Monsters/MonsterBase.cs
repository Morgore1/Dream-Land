using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;


[CreateAssetMenu(fileName = "Monster", menuName = "Monster/Create new monster")]
public class MonsterBase : ScriptableObject
{
    [SerializeField] string name;
    [SerializeField] string familyID;
    [SerializeField] MonsterBase evolution;
    [SerializeField] int evolutionRequirement = 2;
    [SerializeField] int evolutionStage; 
    
    public int EvolutionStage => evolutionStage;

    public string FamilyID => familyID;
    public MonsterBase Evolution => evolution;
    public int EvolutionRequirement => evolutionRequirement;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] public Sprite FrontSprite;
    [SerializeField] public Sprite frontIdleSprite;


    // Attack Animation
    [SerializeField] public List<Sprite> attackAnimationFrames;
    [SerializeField] public float attackFrameRate = 8f;

    // Hit Animation
    [SerializeField] public List<Sprite> hitAnimationFrames;
    [SerializeField] public float hitFrameRate = 10f;

    [SerializeField] public List<Sprite> enterAnimationFrames;
    [SerializeField] public float enterAnimationRate = 10f;


    [SerializeField] public MonsterType Type1;
    [SerializeField] public MonsterType Type2;


    // Base Stats
    [SerializeField] int maxHp;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int magicAttack;
    [SerializeField] int magicDefense;
    [SerializeField] int speed;

    [SerializeField] int catchRate = 255;

    [SerializeField] List<LearnableMove> learnableMoves;

    public string Name {
        get { return name; }
        }

    public string Description{
        get { return description;}
    }

    public int MaxHp{
        get { return maxHp; }
    }

    public int Attack{
        get { return attack; }
    }

    public int Defense{
        get { return defense; }
    }

    public int MagicAttack{
        get { return magicAttack; }
    }

    public int MagicDefense{
        get { return magicDefense; }
    }

    public int Speed{
        get { return speed; }
    }

    public List<LearnableMove> LearnableMoves 
        { get { return learnableMoves; } }

    public int CatchRate => catchRate;
}

[System.Serializable]
public class LearnableMove
{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base
    {
        get { return moveBase; }
    }

    public int Level {  get { return level;} }
}

public enum MonsterType
{
    None,
    Water,
    Electric,
    Fire,
    Void,
    Normal,
    Fractal,
    Cursed,
    Blessed,
    Sound,
    Shadow,
    Light,
    Earth,
    Air,
    Ice,
    Crystallin,
    Nature
}

public enum Stat
{
    Attack,
    Defense,
    MagicAttack,
    MagicDefense,
    Speed,

    //these two aren't actual stats, rather used to boost or decrease moveaccuracy
    Accuracy,
    Evasion
}

public class TypeChart
{
    static float[][] chart =
    {
        //                   WAT ELEC FIR VOID NOR FRAC CURSE BLESS SOUND SHAD LIGH EARTH AIR ICE CRYST NAT
        /*WAT*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*ELEC*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*FIR*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*VOID*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*NOR*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*FRAC*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*CURSE*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*BLESS*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*SOUND*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*SHAD*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*LIGH*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*EARTH*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*AIR*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*ICE*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*CRYST*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*NAT*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
    };

    public static float GetEffectiveness(MonsterType attackType, MonsterType defenseType)
    {
        if (attackType == MonsterType.None || defenseType == MonsterType.None)
                return 1;
        int row = (int)attackType - 1;
        int col = (int)defenseType - 1;

        return chart[row][col];
    }
}