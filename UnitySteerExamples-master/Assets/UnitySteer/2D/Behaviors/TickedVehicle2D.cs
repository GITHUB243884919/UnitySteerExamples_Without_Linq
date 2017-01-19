//#define TRACE_ADJUSTMENTS

using System.Diagnostics;
using TickedPriorityQueue;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace UnitySteer2D.Behaviors
{
    /// <summary>
    /// Vehicle2D subclass oriented towards autonomous bipeds and vehicles, which 
    /// will be ticked automatically to calculate their direction.
    /// 
    /// 1���ۺ���TickedObject��������UnityTickedQueue�������Լ�����UnityTickedQueue
    /// 2���Ѹ���steering�ϵ�����Ȩ�ۼӣ����������Ȼ�����������Ĭ��1����������µļ��ٶȡ�
    ///    ��steering�ϵ�������������ʵ�֣���ͬ��ʵ�־����˲�ͬ�Ĳٿأ�AI��.
    ///    ��֪��Ϊʲô�����õ��ٶȵĵ�������ʾ���ٶȡ��������ռ�����ٶȵĺ���
    ///    ApplySteeringForce()����ȷ�ġ�
    /// 3������Update�������ﴦ�����ʵ�λ�Ʊ仯�ͷ���仯��Ҳ�����ƶ����ʵ㣬��֪�����Ƿ���ʡ�
    /// 
    /// 
    /// </summary>
    public abstract class TickedVehicle2D : Vehicle2D
    {
        #region Internal state values
        // modified by fanzhengyong begin
        // ȥ������ı���
        // ������һ�ж�����һ��TickedObject���ͱ���TickedObject
        // ����Ҳ����get set��ʵ�ʴ����е��õ�Ҳ�� TickedObject
        // ��������_tickedObject�Ƕ����
        //private TickedObject _tickedObject;
        // modified by fanzhengyong end
        private UnityTickedQueue _steeringQueue;

        /// <summary>
        /// The name of the steering queue for this ticked vehicle.
        /// </summary>
        [SerializeField] private string _queueName = "Steering2D";

        /// <summary>
        /// How often will this Vehicle's steering calculations be ticked.
        /// </summary>
        [SerializeField] private float _tickLength = 0.1f;

        /// <summary>
        /// The maximum number of radar update calls processed on the queue per update
        /// </summary>
        /// <remarks>
        /// Notice that this is a limit shared across queue items of the same name, at
        /// least until we have some queue settings, so whatever value is set last for 
        /// the queue will win.  Make sure your settings are consistent for objects of
        /// the same queue.
        /// </remarks>
        [SerializeField] private int _maxQueueProcessedPerUpdate = 20;

        [SerializeField] private bool _traceAdjustments = false;

        #endregion

        /// <summary>
        /// Last time the vehicle's tick was completed.
        /// </summary>
        /// <value>The last tick time.</value>
        public float PreviousTickTime { get; private set; }


        /// <summary>
        /// Current time that the tick was called.
        /// </summary>
        /// <value>The current tick time.</value>
        public float CurrentTickTime { get; private set; }

        /// <summary>
        /// The time delta between now and when the vehicle's previous tick time and the current one.
        /// </summary>
        /// <value>The delta time.</value>
        public override float DeltaTime
        {
            get { return CurrentTickTime - PreviousTickTime; }
        }

        /// <summary>
        /// Velocity vector used to orient the agent.
        /// </summary>
        /// <remarks>
        /// This is expected to be set by the subclasses.
        /// </remarks>
        public Vector2 OrientationVelocity { get; protected set; }

        public string QueueName
        {
            get { return _queueName; }
            set { _queueName = value; }
        }

        /// <summary>
        /// Priority queue for this vehicle's updates
        /// </summary>
        public UnityTickedQueue SteeringQueue
        {
            get { return _steeringQueue; }
        }


        /// <summary>
        /// Ticked object for the vehicle, so that its owner can configure
        /// the priority as desired.
        /// </summary>
        public TickedObject TickedObject { get; private set; }

#region Unity events

        private void Start()
        {
            PreviousTickTime = Time.time;
        }


        protected override void OnEnable()
        {
            base.OnEnable();
            TickedObject = new TickedObject(OnUpdateSteering);
            TickedObject.TickLength = _tickLength;
            _steeringQueue = UnityTickedQueue.GetInstance(QueueName);
            _steeringQueue.Add(TickedObject);
            _steeringQueue.MaxProcessedPerUpdate = _maxQueueProcessedPerUpdate;
        }

        protected override void OnDisable()
        {
            DeQueue();
            base.OnDisable();
        }

#endregion

#region Velocity / Speed methods

        private void DeQueue()
        {
            if (_steeringQueue != null)
            {
                _steeringQueue.Remove(TickedObject);
            }
        }

        protected void OnUpdateSteering(object obj)
        {
            if (enabled)
            {
                // We just calculate the forces, and expect the radar updates itself.
                CalculateForces();
            }
            else
            {
                /*
			 * This is an interesting edge case.
			 * 
			 * Because of the way TickedQueue iterates through its items, we may have
			 * a case where:
			 * - The vehicle's OnUpdateSteering is enqueued into the work queue
			 * - An event previous to it being called causes it to be disabled, and de-queued
			 * - When the ticked queue gets to it, it executes and re-enqueues it
			 * 
			 * Therefore we double check that we're not trying to tick it while disabled, and 
			 * if so we de-queue it.  Must review TickedQueue to see if there's a way we can 
			 * easily handle these sort of issues without a performance hit.
			 */
                DeQueue();
                // Debug.LogError(string.Format("{0} HOLD YOUR HORSES. Disabled {1} being ticked", Time.time, this));
            }
        }

        /// <summary>
        /// ���㵱ǰ�ʵ������еĶ��Steering�������Ȩ�ؼ��������
        /// �������ǰ�ʵ���ͬʱ���ڶ���ٿ�AI�Ļ����Ϳ����Ƕ��Steering�����
        /// </summary>
        protected void CalculateForces()
        {
            PreviousTickTime = CurrentTickTime;
            CurrentTickTime = Time.time;

            if (!CanMove || Mathf.Approximately(MaxForce, 0) || Mathf.Approximately(MaxSpeed, 0))
            {
                return;
            }
            Profiler.BeginSample("Calculating vehicle forces");

            var force = Vector2.zero;

            Profiler.BeginSample("Adding up basic steerings");
            for (var i = 0; i < Steerings.Length; i++)
            {
                var s = Steerings[i];
                if (s.enabled)
                {
                    //Cast to make sure everything fits nicely
                    force += s.WeighedForce;
                }
            }
            Profiler.EndSample();
            LastRawForce = force;

            // Enforce speed limit.  Steering behaviors are expected to return a
            // final desired velocity, not a acceleration, so we apply them directly.
            var newVelocity = Vector2.ClampMagnitude(force / Mass, MaxForce);

            if (newVelocity.sqrMagnitude == 0)
            {
                ZeroVelocity();
                DesiredVelocity = Vector2.zero;
            }
            else
            {
                DesiredVelocity = newVelocity;
            }

            // Adjusts the velocity by applying the post-processing behaviors.
            //
            // This currently is not also considering the maximum force, nor 
            // blending the new velocity into an accumulator. We *could* do that,
            // but things are working just fine for now, and it seems like
            // overkill. 
            var adjustedVelocity = Vector2.zero;
            Profiler.BeginSample("Adding up post-processing steerings");
            for (var i = 0; i < SteeringPostprocessors.Length; i++)
            {
                var s = SteeringPostprocessors[i];
                if (s.enabled)
                {
                    //Cast to make sure everyone works together
                    adjustedVelocity += s.WeighedForce;
                }
            }
            Profiler.EndSample();


            if (adjustedVelocity != Vector2.zero)
            {
                adjustedVelocity = Vector2.ClampMagnitude(adjustedVelocity, MaxSpeed);
                TraceDisplacement(adjustedVelocity, Color.cyan);
                TraceDisplacement(newVelocity, Color.white);
                newVelocity = adjustedVelocity;
            }

            // Update vehicle velocity
            SetCalculatedVelocity(newVelocity);
            Profiler.EndSample();
        }


        /// <summary>
        /// Applies a steering force to this vehicle
        /// </summary>
        /// <param name="elapsedTime">
        /// How long has elapsed since the last update<see cref="System.Single"/>
        /// </param>
        private void ApplySteeringForce(float elapsedTime)
        {
            // Euler integrate (per frame) velocity into position
            var acceleration = CalculatePositionDelta(elapsedTime);
            acceleration = Vector2.Scale(acceleration, AllowedMovementAxes);

            if (Rigidbody == null || Rigidbody.isKinematic)
            {
                Transform.position += (Vector3)acceleration;
            }
            else
            {
                //Cast to make sure the rigidbody doesn't die on switch
                Rigidbody.MovePosition(Rigidbody.position + acceleration);
            }
        }


        /// <summary>
        /// Turns the vehicle towards his velocity vector. Previously called
        /// LookTowardsVelocity.
        /// </summary>
        /// <param name="deltaTime">Time delta to use for turn calculations</param>
        protected void AdjustOrientation(float deltaTime)
        {
            /* 
		 * Avoid adjusting if we aren't applying any velocity. We also
		 * disregard very small velocities, to avoid jittery movement on
		 * rounding errors.
		 */
            if (TargetSpeed > MinSpeedForTurning && Velocity != Vector2.zero)
            {
                var newForward = Vector2.Scale(OrientationVelocity, AllowedMovementAxes).normalized; //TODO need to check how this does in 2d, may need to change to quaternion
                if (TurnTime > 0)
                {
                    newForward = Vector3.Slerp(Forward, newForward, deltaTime / TurnTime);
                }

                Forward = newForward;
            }
        }

        /// <summary>
        /// Records the velocity that was just calculated by CalculateForces in a
        /// manner that is specific to each subclass. 
        /// </summary>
        /// <param name="velocity">Newly calculated velocity</param>
        protected abstract void SetCalculatedVelocity(Vector2 velocity);

        /// <summary>
        /// Calculates how much the agent's position should change in a manner that
        /// is specific to the vehicle's implementation.
        /// </summary>
        /// <param name="deltaTime">Time delta to use in position calculations</param>
        protected abstract Vector2 CalculatePositionDelta(float deltaTime);

        /// <summary>
        /// Zeros this vehicle's velocity.
        /// </summary>
        /// <remarks>
        /// Implementation details are left up to the subclasses.
        /// </remarks>
        protected abstract void ZeroVelocity();

        #endregion

        //ΪʲôҪ��Update�ﴦ���ƶ���
        private void Update()
        {
            if (CanMove)
            {
                ApplySteeringForce(Time.deltaTime);
                AdjustOrientation(Time.deltaTime);
            }
        }

        [Conditional("TRACE_ADJUSTMENTS")]
        private void TraceDisplacement(Vector2 delta, Color color)
        {
            if (_traceAdjustments)
            {
                Debug.DrawLine(Transform.position, (Vector2)Transform.position + delta, color);
            }
        }

        public void Stop()
        {
            CanMove = false;
            ZeroVelocity();
        }
    }
}