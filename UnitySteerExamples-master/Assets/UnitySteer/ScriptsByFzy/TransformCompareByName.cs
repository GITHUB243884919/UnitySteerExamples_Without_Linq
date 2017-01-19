using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// author : fanzhengyong
/// data : 2017-01-17
/// Transform按name字段排序的Comparer
/// 首次用于去除PathFollowingController2D.cs中的Linq
/// </summary>
public class TransformCompareByName : IComparer<Transform>
{
    public int Compare(Transform a, Transform b)
    {
        return a.name.CompareTo(b.name);
    }
}
