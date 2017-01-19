using System.Collections;
using System.Collections.Generic;
//using System.Linq;

using UnityEngine;

using UnitySteer2D;
using UnitySteer2D.Behaviors;

[RequireComponent(typeof(SteerForPathSimplified2D))]
public class PathFollowingController2D : MonoBehaviour 
{

	SteerForPathSimplified2D _steering;

	[SerializeField] Transform _pathRoot;

	[SerializeField] bool _followAsSpline;

	// Use this for initialization
	void Start() 
	{
		_steering = GetComponent<SteerForPathSimplified2D>();
		AssignPath();
	}


	void AssignPath()
	{
		// Get a list of points to follow;
		var pathPoints = PathFromRoot(_pathRoot);
        _steering.Path = _followAsSpline ? new SplinePathway(pathPoints, 1) : new Vector2Pathway(pathPoints, 1);
	}

	static List<Vector2> PathFromRoot(Transform root)
	{
		var children = new List<Transform>();
		foreach (Transform t in root)
		{
			if (t != null)
			{
				children.Add(t);
			}
		}
        // modified by fanzhengyong begin
        // 移除Linq
        //return children.OrderBy(t => t.gameObject.name).Select(t => new Vector2(t.position.x, t.position.y)).ToList();
        children.Sort(new TransformCompareByName());
        List<Vector2> result = new List<Vector2>();
        foreach (Transform t in children)
        {
            if (t != null)
            {
                result.Add(new Vector2(t.position.x, t.position.y));
            }
        }
        return result;
        // modified by fanzhengyong end
    }


}
