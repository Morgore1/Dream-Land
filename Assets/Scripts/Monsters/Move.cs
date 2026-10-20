using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Move
{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int ap;

    public MoveBase Base
    {
        get => moveBase;
        set => moveBase = value;
    }

    public int AP
    {
        get => ap;
        set => ap = value;
    }

    public Move(MoveBase pBase)
    {
        moveBase = pBase;
        ap = pBase.AP;
    }
}
