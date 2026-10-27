using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DirectionMapRule
{
    public List<DistanceMapRule> leftRules = new List<DistanceMapRule>();
    public List<DistanceMapRule> rightRules = new List<DistanceMapRule>();
}