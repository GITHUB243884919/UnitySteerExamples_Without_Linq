using UnityEngine;
using System.Collections;
using System.Collections.Generic;
//using System.Linq;
using UnitySteer;
using UnitySteer.Behaviors;

[RequireComponent(typeof(SteerForPathSimplified))]
public class PathFollowingController : MonoBehaviour 
{

	SteerForPathSimplified _steering;

	[SerializeField] Transform _pathRoot;

	[SerializeField] bool _followAsSpline;

	// Use this for initialization
	void Start() 
	{
		_steering = GetComponent<SteerForPathSimplified>();
		AssignPath();
	}


	void AssignPath()
	{
		// Get a list of points to follow;
		var pathPoints = PathFromRoot(_pathRoot);
		_steering.Path = _followAsSpline ? new SplinePathway(pathPoints, 1) : new Vector3Pathway(pathPoints, 1);
	}

	static List<Vector3> PathFromRoot(Transform root)
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
		//return children.OrderBy(t => t.gameObject.name).Select(t => t.position).ToList();
        children.Sort(new TransformCompareByName());
        List<Vector3> result = new List<Vector3>();
        foreach (Transform t in children)
        {
            if (t != null)
            {
                result.Add(t.position);
            }
        }
        return result;
        // modified by fanzhengyong end
    }


}
