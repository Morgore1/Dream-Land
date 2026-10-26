using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    [SerializeField, HideInInspector] List<LearnableMove> learnableMoves;
    [SerializeField] List<MonsterMoveSlot> moveSlots = new List<MonsterMoveSlot>();
    [SerializeField, Range(0, 4), Tooltip("Starting unlocked move slots for evolving forms. Forms with no evolution automatically use all four slots.")]
    int initialUnlockedMoveSlots = 3;

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

    public int InitialUnlockedMoveSlots => evolution == null ? 4 : Mathf.Clamp(initialUnlockedMoveSlots, 0, 4);

    public List<MoveBase> GetMoveCandidates(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4)
            return new List<MoveBase>();

        if (moveSlots != null && moveSlots.Count > 0)
        {
            if (slotIndex >= moveSlots.Count || moveSlots[slotIndex] == null)
                return new List<MoveBase>();

            if (moveSlots[slotIndex].Candidates == null)
                return new List<MoveBase>();

            return moveSlots[slotIndex].Candidates
                .Where(candidate => candidate != null)
                .ToList();
        }

        // Existing assets use a shared learnset. Keep them playable until their
        // four slot-specific candidate pools are authored in the Inspector.
        if (learnableMoves == null)
            return new List<MoveBase>();

        return learnableMoves
            .Where(learnableMove => learnableMove != null && learnableMove.Base != null)
            .Select(learnableMove => learnableMove.Base)
            .ToList();
    }

    private void OnValidate()
    {
        initialUnlockedMoveSlots = Mathf.Clamp(initialUnlockedMoveSlots, 0, 4);

        int slotCount = InitialUnlockedMoveSlots;
        for (int i = 0; i < slotCount; i++)
        {
            if (GetMoveCandidates(i).Count == 0)
                Debug.LogWarning($"{name} has no valid move candidates configured for unlocked slot {i + 1}.", this);
        }
    }

    public int CatchRate => catchRate;
}

[System.Serializable]
public class MonsterMoveSlot
{
    [SerializeField] List<MoveBase> candidates = new List<MoveBase>();

    public List<MoveBase> Candidates => candidates;
}

[System.Serializable]
public class LearnableMove
{
    [SerializeField] MoveBase moveBase;

    public MoveBase Base
    {
        get { return moveBase; }
    }
}

public enum MonsterType
{
    None = 0,
    Aquatic = 1,
    Flame = 2,
    Stone = 3,
    Natural = 4,
    Chaos = 5,
    Order = 6,
    Shadow = 7,
    Light = 8,
    Magical = 9
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
        // All matchups are neutral until type effectiveness is designed.
        /*AQUATIC*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*FLAME*/  new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*STONE*/  new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*NATURAL*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*CHAOS*/  new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*ORDER*/  new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*SHADOW*/ new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*LIGHT*/  new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
        /*MAGICAL*/new float[] { 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f, 1f },
    };

    public static float GetEffectiveness(MonsterType attackType, MonsterType defenseType)
    {
        if (attackType <= MonsterType.None || defenseType <= MonsterType.None)
            return 1f;

        int row = (int)attackType - 1;
        int col = (int)defenseType - 1;

        if (row < 0 || row >= chart.Length || col < 0 || col >= chart[row].Length)
            return 1f;

        return chart[row][col];
    }
}