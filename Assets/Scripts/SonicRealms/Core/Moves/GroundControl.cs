﻿using SonicRealms.Core.Actors;
using SonicRealms.Core.Utils;
using UnityEngine;

namespace SonicRealms.Core.Moves
{
    public class GroundControl : Move
    {
        #region Animation
        /// <summary>
        /// Name of an Animator float set to magnitude of ground control input.
        /// </summary>
        [AnimationFoldout]
        [Tooltip("Name of an Animator float set to magnitude of ground control input.")]
        public string InputAxisFloat;
        protected int InputAxisFloatHash;

        /// <summary>
        /// Name of an Animator bool set to whether there is any input.
        /// </summary>
        [AnimationFoldout]
        [Tooltip("Name of an Animator bool set to whether there is any input.")]
        public string InputBool;
        protected int InputBoolHash;

        /// <summary>
        /// Name of an Animator bool set to whether the controller is accelerating.
        /// </summary>
        [AnimationFoldout]
        [Tooltip("Name of an Animator bool set to whether the controller is accelerating.")]
        public string AcceleratingBool;
        protected int AcceleratingBoolHash;

        /// <summary>
        /// Name of an Animator bool set to whether the controller is braking.
        /// </summary>
        [AnimationFoldout]
        [Tooltip("Name of an Animator bool set to whether the controller is braking.")]
        public string BrakingBool;
        protected int BrakingBoolHash;

        /// <summary>
        /// Name of an Animator float set to absolute ground speed divided by top speed.
        /// </summary>
        [AnimationFoldout]
        [Tooltip("Name of an Animator float set to absolute ground speed divided by top speed.")]
        public string TopSpeedPercentFloat;
        protected int TopSpeedPercentFloatHash;
        #endregion
        #region Control
        /// <summary>
        /// The name of the input axis.
        /// </summary>
        [ControlFoldout]
        [Tooltip("The name of the input axis.")]
        public string MovementAxis;

        /// <summary>
        /// Whether to invert the axis input.
        /// </summary>
        [ControlFoldout]
        [Tooltip("Whether to invert the axis input.")]
        public bool InvertAxis;
        #endregion
        #region Physics
        /// <summary>
        /// Ground acceleration in units per second squared.
        /// </summary>
        [PhysicsFoldout]
        [Tooltip("Ground acceleration in units per second squared.")]
        public float Acceleration;

        /// <summary>
        /// Whether acceleration is allowed.
        /// </summary>
        [DebugFoldout]
        public bool DisableAcceleration;

        /// <summary>
        /// Ground deceleration in units per second squared.
        /// </summary>
        [PhysicsFoldout]
        [Tooltip("Ground deceleration units per second squared.")]
        public float Deceleration;

        /// <summary>
        /// Whether deceleration is allowed.
        /// </summary>
        [DebugFoldout]
        public bool DisableDeceleration;

        /// <summary>
        /// Top running speed in unit per second.
        /// </summary>
        [PhysicsFoldout]
        [Tooltip("Top running speed in units per second.")]
        public float TopSpeed;

        /// <summary>
        /// Minimum ground speed at which slope gravity is applied, in units per second. Allows Sonic to stand still on
        /// steep slopes.
        /// </summary>
        [PhysicsFoldout]
        [Tooltip("Minimum ground speed at which slope gravity is applied, in units per second. Allows Sonic to stand still on " +
                 "steep slopes.")]
        public float MinSlopeGravitySpeed;
        #endregion

        #region Properties
        /// <summary>
        /// Whether the controller is accelerating.
        /// </summary>
        public bool Accelerating
        {
            get
            {
                return !ControlLockTimerOn &&
                       (Controller.GroundVelocity > 0.0f && _axis > 0.0f ||
                        Controller.GroundVelocity < 0.0f && _axis < 0.0f);
            }
        }

        /// <summary>
        /// Whether the controller is braking.
        /// </summary>
        public bool Braking
        {
            get
            {
                return !ControlLockTimerOn &&
                       (Controller.GroundVelocity > 0.0f && _axis < 0.0f ||
                        Controller.GroundVelocity < 0.0f && _axis > 0.0f);
            }
        }

        /// <summary>
        /// Whether the controller is standing still.
        /// </summary>
        public bool Standing
        {
            get { return DMath.Equalsf(Controller.GroundVelocity) && !Braking && !Accelerating; }
        }

        /// <summary>
        /// Whether the controller is at top speed.
        /// </summary>
        public bool AtTopSpeed
        {
            get { return Mathf.Abs(Controller.GroundVelocity) - TopSpeed > -0.1f; }
        }
        #endregion

        private float _axis;

        /// <summary>
        /// Whether control is disabled.
        /// </summary>
        public bool DisableControl;

        /// <summary>
        /// Whether the control lock is on.
        /// </summary>
        public bool ControlLockTimerOn;

        /// <summary>
        /// Time until the control lock is switched off, in seconds. Set to zero if the control is not locked.
        /// </summary>
        public float ControlLockTimer;

        protected ScoreCounter Score;

        //agregado por fer
        Nmove move = new Nmove();
        Markov auxmark = new Markov();


        public override MoveLayer Layer
        {
            get { return MoveLayer.Control; }
        }

        public override void Reset()
        {
            base.Reset();

            MovementAxis = "Horizontal";
            InvertAxis = false;

            InputAxisFloat = InputBool = AcceleratingBool =
                BrakingBool = TopSpeedPercentFloat = "";

            Acceleration = 1.6875f;
            DisableAcceleration = false;
            Deceleration = 18.0f;
            DisableDeceleration = false;
            TopSpeed = 3.6f;
            MinSlopeGravitySpeed = 0.1f;
        }

        public override void Awake()
        {
            base.Awake();
            _axis = 0.0f;

            DisableControl = false;
            ControlLockTimerOn = false;
            ControlLockTimer = 0.0f;

            InputAxisFloatHash = string.IsNullOrEmpty(InputAxisFloat) ? 0 : Animator.StringToHash(InputAxisFloat);
            InputBoolHash = string.IsNullOrEmpty(InputBool) ? 0 : Animator.StringToHash(InputBool);
            AcceleratingBoolHash = string.IsNullOrEmpty(AcceleratingBool) ? 0 : Animator.StringToHash(AcceleratingBool);
            BrakingBoolHash = string.IsNullOrEmpty(BrakingBool) ? 0 : Animator.StringToHash(BrakingBool);
            TopSpeedPercentFloatHash = string.IsNullOrEmpty(TopSpeedPercentFloat) ? 0 : Animator.StringToHash(TopSpeedPercentFloat);
        }

        public override void OnManagerAdd()
        {
            if (Controller.Grounded) Perform();
            Controller.OnAttach.AddListener(OnAttach);
            Score = Controller.GetComponent<ScoreCounter>();
        }

        public void OnAttach()
        {
            Perform();
            if (Score) Score.EndCombo();
        }

        public override void OnActiveEnter(State previousState)
        {
            float aux2move = move.getAle();
            Manager.End<AirControl>();
            Controller.OnSteepDetach.AddListener(OnSteepDetach);

            // _axis = InvertAxis ? -Input.GetAxis(MovementAxis) : Input.GetAxis(MovementAxis);
            _axis = aux2move;
        }

        public override void OnActiveUpdate()
        {
            float auxmove = move.getAle();
            if (ControlLockTimerOn || DisableControl) return;
            _axis = auxmove;
        }

        public override void OnActiveFixedUpdate()
        {
            UpdateControlLockTimer();

            // Accelerate as long as the control lock isn't on
            if (!ControlLockTimerOn) Accelerate(_axis);

            // If we're on a wall and aren't going quickly enough, start the control lock
            if (Mathf.Abs(Controller.GroundVelocity) < Controller.DetachSpeed &&
                DMath.AngleInRange_d(Controller.RelativeSurfaceAngle, 50.0f, 310.0f))
                Lock();

            // Disable slope gravity when we're not moving, so that Sonic can stand on slopes
            Controller.DisableSlopeGravity = !(Accelerating || ControlLockTimerOn ||
                                           Mathf.Abs(Controller.GroundVelocity) > MinSlopeGravitySpeed);

            // Disable ground friction while we have player input
            Controller.DisableGroundFriction =
                (!DisableAcceleration && Accelerating) ||
                (!DisableDeceleration && Braking);

            // Orient the player in the direction we're moving (not graphics-wise, just internally!)
            if (!ControlLockTimerOn && !DMath.Equalsf(_axis))
                Controller.FacingForward = Controller.GroundVelocity >= 0.0f;
        }

        public override void OnActiveExit()
        {
            // Set everything back to normal
            Controller.OnSteepDetach.RemoveListener(OnSteepDetach);
            Controller.DisableSlopeGravity = false;
            Controller.DisableGroundFriction = false;

            if (Animator == null) return;

            if (AcceleratingBoolHash != 0) Animator.SetBool(AcceleratingBoolHash, false);
            if (BrakingBoolHash != 0) Animator.SetBool(BrakingBoolHash, false);
        }

        public override void SetAnimatorParameters()
        {
            if (InputAxisFloatHash != 0)
                Animator.SetFloat(InputAxisFloatHash, _axis);

            if (InputBoolHash != 0)
                Animator.SetBool(InputBoolHash, !DMath.Equalsf(_axis));

            if (AcceleratingBoolHash != 0)
                Animator.SetBool(AcceleratingBoolHash, Accelerating);

            if (BrakingBoolHash != 0)
                Animator.SetBool(BrakingBoolHash, Braking);

            if (TopSpeedPercentFloatHash != 0)
                Animator.SetFloat(TopSpeedPercentFloatHash, Mathf.Abs(Controller.GroundVelocity) / TopSpeed);
        }

        public void OnSteepDetach()
        {
            Lock();
        }

        public void UpdateControlLockTimer()
        {
            UpdateControlLockTimer(Time.fixedDeltaTime);
        }

        public void UpdateControlLockTimer(float timestep)
        {
            if (!ControlLockTimerOn) return;

            ControlLockTimer -= timestep;
            if (ControlLockTimer < 0.0f) Unlock();
        }

        /// <summary>
        /// Locks ground control for the specified duration.
        /// </summary>
        /// <param name="time"></param>
        public void Lock(float time = 0.5f)
        {
            ControlLockTimer = time;
            ControlLockTimerOn = true;
        }

        /// <summary>
        /// Unlocks ground control.
        /// </summary>
        public void Unlock()
        {
            ControlLockTimer = 0.0f;
            ControlLockTimerOn = false;
        }

        /// <summary>
        /// Accelerates the controller forward.
        /// </summary>
        /// <param name="magnitude">A value between 0 and 1, 0 being no acceleration and 1 being the amount
        /// defined by Acceleration.</param>
        /// <returns>Whether any acceleration occurred.</returns>
        public bool AccelerateForward(float magnitude = 1.0f)
        {
            return Accelerate(magnitude);
        }

        /// <summary>
        /// Accelerates the controller backward.
        /// </summary>
        /// <param name="magnitude">A value between 0 and 1, 0 being no acceleration and 1 being the amount
        /// defined by Acceleration.</param>
        /// <returns>Whether any acceleration occurred.</returns>
        public bool AccelerateBackward(float magnitude = 1.0f)
        {
            return Accelerate(-magnitude);
        }

        /// <summary>
        /// Accelerates the controller using Time.fixedDeltaTime as the timestep.
        /// </summary>
        /// <param name="magnitude">A value between -1 and 1, positive moving it forward and negative moving
        /// it back.</param>
        /// <returns>Whether any acceleration occurred.</returns>
        public bool Accelerate(float magnitude)
        {
            return Accelerate(magnitude, Time.fixedDeltaTime);
        }

        /// <summary>
        /// Accelerates the controller.
        /// </summary>
        /// <param name="magnitude">A value between -1 and 1, positive moving it forward and negative moving
        /// it back.</param>
        /// <param name="timestep">The timestep, in seconds</param>
        /// <returns>Whether any acceleration occurred.</returns>
        public bool Accelerate(float magnitude, float timestep)
        {
            magnitude = Mathf.Clamp(magnitude, -1.0f, 1.0f);
            if (DMath.Equalsf(magnitude)) return false;

            if (magnitude < 0.0f)
            {
                if (!DisableDeceleration && Controller.GroundVelocity > 0.0f)
                {
                    Controller.GroundVelocity += Deceleration * magnitude * timestep;
                    return true;
                }
                else if (!DisableAcceleration && Controller.GroundVelocity > -TopSpeed)
                {
                    Controller.GroundVelocity += Acceleration * magnitude * timestep;
                    return true;
                }
            }
            else if (magnitude > 0.0f)
            {
                if (!DisableDeceleration && Controller.GroundVelocity < 0.0f)
                {
                    Controller.GroundVelocity += Deceleration * magnitude * timestep;
                    return true;
                }
                else if (!DisableAcceleration && Controller.GroundVelocity < TopSpeed)
                {
                    Controller.GroundVelocity += Acceleration * magnitude * timestep;
                    return true;
                }
            }

            return false;
        }
    }

    class Nmove
    {
        public float ale;
        public float getAle()
        {
            int n = UnityEngine.Random.Range(0, 1);
            int dec = UnityEngine.Random.Range(0, 9999);
            string cadena = n.ToString() + "." + dec.ToString();
            float Ale = System.Convert.ToSingle(cadena);
            float I1 = 2 * Ale;
            float I2 = I1 / 2;
            ale = I2;
            System.Console.Write(cadena);
            return ale;
        }
        /*private int ale;
        public int getAle()
        {
            int Ale = UnityEngine.Random.Range(0, 2); 
            int I1 = 2 * Ale;
            int I2 = I1 / 2;
            ale = I2;
            return ale;
        }*/

    }

    class Markov
    {
        int i,j;
        public int getMark()
        {
            for (i = 0; i <= 2; i++)
            {
                for(j = 0; j <=2; j++)
                {

                }

            }
            return 0;
        }
       
    }
}
