using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnitySteer.Behaviors;
/// <summary>
/// author : fanzhengyong
/// data : 2017-01-17
/// TagPlayer按一个设定的AutonomousVehicle，与其的sqrMagnitude排序的Comparer
/// 首次用于去除TagPlayer.cs中的Linq
/// </summary>
public class TagPlayerCompareByTarget : IComparer<TagPlayer>
{
    public AutonomousVehicle Vehicle { get; set; }
    public int Compare(TagPlayer a, TagPlayer b)
    {
        float sqrMagnitude_a = (a.Vehicle.Position - Vehicle.Position).sqrMagnitude;
        float sqrMagnitude_b = (b.Vehicle.Position - Vehicle.Position).sqrMagnitude;

        return sqrMagnitude_a.CompareTo(sqrMagnitude_b);
        
    }
}