//#define ANNOTATE_AVOIDOBSTACLES

//using System.Linq;
using UnityEngine;

namespace UnitySteer2D.Behaviors
{
    /// <summary>
    /// Steers a vehicle to be repulsed by stationary obstacles
    /// </summary>
    /// <remarks>
    /// For every obstacle detected, this will:
    /// 1) Add up a repulsion vector that is the distance between the vehicle 
    /// and the obstacle, divided by the squared magnitude of the distance 
    /// between the obstacle and the vehicle's future intended position. This is
    /// done because the further an obstacle is from our desired position, the
    /// least we care about it (which could have side effects when dealing with
    /// very large obstacles which we don't happen to intersect, need to review).
    /// 2) If we would intersect this obstacle on our current path, then we 
    /// multiply this repulsion vector by a factor of the number of detected
    /// (since the others might just be to the side and we want to give it
    /// higher weight).
    /// 3) Divide the total by the number of obstacles.
    /// The final correction vector is the old desired velocity reflected 
    /// along the calculated avoidance vector.
    /// </remarks>
    [AddComponentMenu("UnitySteer2D/Steer/... for Spherical Obstacles (Post-Process)")]
    public class SteerForSphericalObstacles2D : Steering2D
    {
#region Structs

        /// <summary>
        /// Struct used to store the next likely intersection with an obstacle
        /// for a vehicle's current direction.
        /// </summary>
        public struct PathIntersection
        {
            public bool Intersect;
            public float Distance;
            public DetectableObject2D Obstacle;

            public PathIntersection(DetectableObject2D obstacle)
            {
                Obstacle = obstacle;
                Intersect = false;
                Distance = float.MaxValue;
            }
        };

#endregion

#region Private fields

        [SerializeField] private float _estimationTime = 2;

#endregion

        public override bool IsPostProcess
        {
            get { return true; }
        }

#region Public properties

        /// <summary>
        /// How far in the future to estimate the vehicle position
        /// </summary>
        public float EstimationTime
        {
            get { return _estimationTime; }
            set { _estimationTime = value; }
        }

#endregion

        /// <summary>
        /// Calculates the force necessary to avoid the detected spherical obstacles
        /// </summary>
        /// <returns>
        /// Force necessary to avoid detected obstacles, or Vector2.zero
        /// </returns>
        /// <remarks>
        /// This method will iterate through all detected spherical obstacles that 
        /// are within MinTimeToCollision, and calculate a repulsion vector based
        /// on them.
        /// </remarks>
        protected override Vector2 CalculateForce()
        {
            var avoidance = Vector2.zero;
            // modified by fanzhengyong begin
            // �Ƴ�Linq
            //if (Vehicle.Radar.Obstacles == null || !Vehicle.Radar.Obstacles.Any())
            if (Vehicle.Radar.Obstacles == null || Vehicle.Radar.Obstacles.Count == 0)
            {
                return avoidance;
            }
            // modified by fanzhengyong end
            /*
		     * While we could just calculate movement as (Velocity * predictionTime) 
		     * and save ourselves the substraction, this allows other vehicles to
		     * override PredictFuturePosition for their own ends.
		     */
            var futurePosition = Vehicle.PredictFutureDesiredPosition(_estimationTime);

#if ANNOTATE_AVOIDOBSTACLES
            Debug.DrawLine(Vehicle.Position, futurePosition, Color.cyan);
#endif

            /*
             * Test all obstacles for intersection with the vehicle's future position.
             * If we find that we are going to intersect them, use their position
             * and distance to affect the avoidance - the further away the intersection
             * is, the less weight they'll carry.
             */
            Profiler.BeginSample("Accumulate spherical obstacle influences");
            for (var i = 0; i < Vehicle.Radar.Obstacles.Count; i++)
            {
                var sphere = Vehicle.Radar.Obstacles[i];
                if (sphere == null || sphere.Equals(null))
                    continue; // In case the object was destroyed since we cached it
                var next = FindNextIntersectionWithSphere(Vehicle, futurePosition, sphere);
                var avoidanceMultiplier = 0.1f;
                if (next.Intersect)
                {
#if ANNOTATE_AVOIDOBSTACLES
                    Debug.DrawRay(Vehicle.Position, Vehicle.DesiredVelocity.normalized * next.Distance, Color.yellow);
#endif
                    var timeToObstacle = next.Distance / Vehicle.Speed;
                    avoidanceMultiplier = 2 * (_estimationTime / timeToObstacle);
                }

                var oppositeDirection = Vehicle.Position - sphere.Position;
                avoidance += avoidanceMultiplier * oppositeDirection;
            }
            Profiler.EndSample();

            avoidance /= Vehicle.Radar.Obstacles.Count;

            var newDesired = Vector2.Reflect(Vehicle.DesiredVelocity, avoidance);

#if ANNOTATE_AVOIDOBSTACLES
            Debug.DrawLine(Vehicle.Position, (Vector3)Vehicle.Position + (Vector3)avoidance, Color.green);
            Debug.DrawLine(Vehicle.Position, futurePosition, Color.blue);
            Debug.DrawLine(Vehicle.Position, (Vector3)Vehicle.Position + newDesired, Color.white);
#endif

            return newDesired;
        }

        /// <summary>
        /// Finds a vehicle's next intersection with a spherical obstacle
        /// </summary>
        /// <param name="vehicle">
        /// The vehicle to evaluate.
        /// </param>
        /// <param name="futureVehiclePosition">
        /// The position where we expect the vehicle to be soon
        /// </param>
        /// <param name="obstacle">
        /// A spherical obstacle to check against <see cref="DetectableObject"/>
        /// </param>
        /// <returns>
        /// A PathIntersection with the intersection details <see cref="PathIntersection"/>
        /// </returns>
        /// <remarks>We could probably spin out this function to an independent tool class</remarks>
        public static PathIntersection FindNextIntersectionWithSphere(Vehicle2D vehicle, Vector2 futureVehiclePosition,
            DetectableObject2D obstacle)
        {
            // this mainly follows http://www.lighthouse3d.com/tutorials/maths/ray-sphere-intersection/

            var intersection = new PathIntersection(obstacle);

            var combinedRadius = vehicle.Radius + obstacle.Radius;
            var movement = futureVehiclePosition - vehicle.Position;
            var direction = movement.normalized;

            var vehicleToObstacle = obstacle.Position - vehicle.Position;

            // this is the length of vehicleToObstacle projected onto direction
            var projectionLength = Vector2.Dot(direction, vehicleToObstacle);

            // if the projected obstacle center lies further away than our movement + both radius, we're not going to collide
            if (projectionLength > movement.magnitude + combinedRadius)
            {
                //print("no collision - 1");
                return intersection;
            }

            // the foot of the perpendicular
            var projectedObstacleCenter = vehicle.Position + projectionLength * direction;

            // distance of the obstacle to the pathe the vehicle is going to take
            var obstacleDistanceToPath = (obstacle.Position - projectedObstacleCenter).magnitude;
            //print("obstacleDistanceToPath: " + obstacleDistanceToPath);

            // if the obstacle is further away from the movement, than both radius, there's no collision
            if (obstacleDistanceToPath > combinedRadius)
            {
                //print("no collision - 2");
                return intersection;
            }

            // use pythagorean theorem to calculate distance out of the sphere (if you do it 2D, the line through the circle would be a chord and we need half of its length)
            var halfChord = Mathf.Sqrt(combinedRadius * combinedRadius + obstacleDistanceToPath * obstacleDistanceToPath);

            // if the projected obstacle center lies opposite to the movement direction (aka "behind")
            if (projectionLength < 0)
            {
                // behind and further away than both radius -> no collision (we already passed)
                if (vehicleToObstacle.magnitude > combinedRadius)
                    return intersection;

                var intersectionPoint = projectedObstacleCenter - direction * halfChord;
                intersection.Intersect = true;
                intersection.Distance = (intersectionPoint - vehicle.Position).magnitude;
                return intersection;
            }

            // calculate both intersection points
            var intersectionPoint1 = projectedObstacleCenter - direction * halfChord;
            var intersectionPoint2 = projectedObstacleCenter + direction * halfChord;

            // pick the closest one
            var intersectionPoint1Distance = (intersectionPoint1 - vehicle.Position).magnitude;
            var intersectionPoint2Distance = (intersectionPoint2 - vehicle.Position).magnitude;

            intersection.Intersect = true;
            intersection.Distance = Mathf.Min(intersectionPoint1Distance, intersectionPoint2Distance);

            return intersection;
        }

#if ANNOTATE_AVOIDOBSTACLES
        private void OnDrawGizmos()
        {
            if (Vehicle == null) return;
            foreach (var o in Vehicle.Radar.Obstacles.Where(x => x != null))
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(o.Position, o.Radius);
            }
        }
#endif
    }
}