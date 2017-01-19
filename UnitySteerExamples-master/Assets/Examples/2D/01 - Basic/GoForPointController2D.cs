using System.Collections;

using UnityEngine;

using UnitySteer2D;
using UnitySteer2D.Behaviors;

[RequireComponent(typeof(AutonomousVehicle2D)), RequireComponent(typeof(SteerForPoint2D))]
public class GoForPointController2D : MonoBehaviour 
{
    SteerForPoint2D _steering;

	[SerializeField]
    Vector2 _pointRange = Vector2.one * 5f;

	void Start() 
	{
        _steering = GetComponent<SteerForPoint2D>();
        //_steering.OnArrival += (_) => FindNewTarget();
        //_steering.OnArrival += new System.Action<Steering2D>(
        //    delegate(Steering2D s) 
        //    {
        //        FindNewTarget();
        //    });
        _steering.OnArrival += (Steering2D s) => FindNewTarget();
        //_steering.OnStartMoving += (_) => { Debug.Log("ff"); }; 
		FindNewTarget();
	}

    public int index = 0;
	void FindNewTarget()
	{
		_steering.TargetPoint = Vector2.Scale(Random.onUnitSphere, _pointRange);
        
        Debug.Log("TargetPoint " + _steering.TargetPoint + "index " + index++);
		_steering.enabled = true;
	}
}
