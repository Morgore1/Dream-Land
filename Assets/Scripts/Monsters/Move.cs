using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public MoveBase Base { get; set; }
    public int AP { get; set;}

    public Move(MoveBase pBase)
    {
        Base = pBase;
        AP = pBase.AP;
    }
}
