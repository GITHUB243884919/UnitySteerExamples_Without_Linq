using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// author : fanzhengyong
/// data : 2017-01-17
/// TagPlayer按Random.value字段排序的Comparer
/// 首次用于去除TagPlayerManager.cs中的Linq
/// </summary>
public class TagPlayerCompareByRandom : IComparer<TagPlayer>
{
    public int Compare(TagPlayer a, TagPlayer b)
    {
        float random_a = Random.value;
        float random_b = Random.value;
        return random_a.CompareTo(random_b);
    }
}
