using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities;
using System.Linq;
using Cinemachine;
using Unity.Netcode;
using System;
using Unity.Netcode.Components;

namespace H4R
{
    [System.Serializable]
    public class AxleInfo
    {
        public WheelCollider leftWheel;
        public WheelCollider rightWheel;
        public bool motor;
        public bool steering;
        public WheelFrictionCurve originalForwardFriction;
        public WheelFrictionCurve originalSidewayFriction;

    }

    public struct InputPayload : INetworkSerializable
    {
        public int Tick;
        public Vector3 InputVector;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref InputVector);
        }
    }

    public struct StatePayload : INetworkSerializable
    {
        public int Tick;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public Vector3 AngularVelocity;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Tick);
            serializer.SerializeValue(ref Position);
            serializer.SerializeValue(ref Rotation);   
            serializer.SerializeValue(ref Velocity);
            serializer.SerializeValue(ref AngularVelocity);
        }
    }

    public class CarController : NetworkBehaviour
    {
        [Header("Refs")]
        [SerializeField] private InputReader _inputReader;
        [SerializeField] private Rigidbody _rb;
        [SerializeField] private CinemachineVirtualCamera _playerCamera;
        [SerializeField] private NetworkTransform _networkTransform;

        [Space]
        [Header("Axle Info")]
        [SerializeField] private AxleInfo[] _axleInfos;

        [Header("Motor Attributes")]
        [SerializeField] private float _maxMotorTorque = 3000f;
        [SerializeField] float _maxSpeed;

        [Header("Braking Attr")]
        [SerializeField] private float _brakeTorque = 10000f;
        [SerializeField] private float _driftSteerMultiplier = 1.5f;


        [Header("Sterring Attr")]
        [SerializeField] private float _maxSteeringAngle = 30f;
        [SerializeField] private AnimationCurve _turnCurve;
        [SerializeField] private float _turnStrength = 1500f;

        [Header("Car Physics")]
        [SerializeField] private Transform _centerOfMass;
        [SerializeField] private float _downForce;
        [SerializeField] private float _gravity = Physics.gravity.y;
        [SerializeField] private float _lateralGScale = 10f;

        [Header("Banking")]
        [SerializeField] private float _maxBankAngle = 5f;
        [SerializeField] private float _bankSpeed = 2f;

        private float _brakeVelocity;
        private Vector3 _carVelocity;
        private float _driftVelocity;

        RaycastHit _hit;
        const float _thresholdSpeed = 10f;
        const float _centerOfMassOffset = -0.5f;
        Vector3 _originalCenterOfMass;

        public bool IsGrounded = true;
        public Vector3 Velocity => _carVelocity;
        public float MaxSpeed => _maxSpeed;

        //Netcode
        NetworkTimer _timer;
        const float k_serverTickRate = 60f;
        const int k_bufferSize = 1024;

        //netcode client
        CircularBuffer<StatePayload> _clientStateBuffer;
        CircularBuffer<InputPayload> _clientInputBuffer;
        InputPayload _lastInputPayload;
        StatePayload _lastServerState;
        StatePayload _lastProcessState;

        //netcode server
        CircularBuffer<StatePayload> _serverStateBuffer;
        Queue<InputPayload> _serverInputQueue;


        /// <summary>
        ///  đầu tiên em phải tìm hiểu về Circular buffer
        /// </summary>

        private void Awake()
        {
            _rb.centerOfMass = _centerOfMass.localPosition;
            _originalCenterOfMass = _centerOfMass.localPosition;

            foreach (AxleInfo axleInfo in _axleInfos)
            {
                axleInfo.originalForwardFriction = axleInfo.leftWheel.forwardFriction;
                axleInfo.originalSidewayFriction = axleInfo.leftWheel.sidewaysFriction;
            }

            _timer = new NetworkTimer(k_serverTickRate);
            _clientInputBuffer = new CircularBuffer<InputPayload>(k_bufferSize);
            _clientStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);

            _serverStateBuffer = new CircularBuffer<StatePayload>(k_bufferSize);
            _serverInputQueue = new Queue<InputPayload>();
        }


        public override void OnNetworkSpawn()
        {
            //if (!IsOwner)
            //{
            //    _playerCamera.Priority = 0;
            //    return;
            //}
            _playerCamera.Priority = 0;

             _rb.interpolation = RigidbodyInterpolation.Interpolate;
                       
        }

        //chỉ gọi ở server. và khi bắt đầu sẽ gửi dữ liệu về cho client để bắt đầu ở client
        public void StartRace() {
           
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            StartRaceClientRpc();
        }

        [ClientRpc]
        public void StartRaceClientRpc()
        {
            //nếu là owner thì bật input
            if(IsOwner)
            {
                if (UIController.Instance != null)
                    UIController.Instance.Show();

                _rb.interpolation = RigidbodyInterpolation.Interpolate;
                _playerCamera.Priority = 100;
            }
        }

        [ClientRpc]
        public void InvokeDrivingClientRpc()
        {
            if (!IsOwner) return;
            _inputReader.Enable();
        }
             
        private void Update()
        {
            _timer.Update(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            while (_timer.ShouldTick()) {
                HandleClientTick();
                HandleServerTick();
            }
        }
        private void HandleServerTick()
        {
            if (!IsServer) return;

            var bufferIndex = -1;
            InputPayload inputPayload = default;
            while (_serverInputQueue.Count > 0)
            {
                inputPayload = _serverInputQueue.Dequeue();
                bufferIndex = inputPayload.Tick % k_bufferSize;

                StatePayload statePayload = ProcessMovement(inputPayload);
                _serverStateBuffer.Add(statePayload, bufferIndex);
            }

            if (bufferIndex == -1) return;
            SendToClientRpc(_serverStateBuffer.Get(bufferIndex), inputPayload);
        }

        private void HandleClientTick()
        {
            if(IsOwner)
            {
                var currentTick = _timer.CurrentTick;
                var bufferIndex = currentTick % k_bufferSize;

                InputPayload inputPayload = new InputPayload()
                {
                    Tick = currentTick,
                    InputVector = _inputReader.Move
                };

                _clientInputBuffer.Add(inputPayload, bufferIndex);
                SendToServerRpc(inputPayload);
                StatePayload statePayload = ProcessMovement(inputPayload);
                _clientStateBuffer.Add(statePayload, bufferIndex);
            } else
            {
                ProcessMovement(_lastInputPayload);
            }

            //handleServerReconciliation();

        }

        [ClientRpc]
        private void SendToClientRpc(StatePayload statePayload, InputPayload inputPayload)
        {
            if (IsOwner) {
                _lastServerState = statePayload;
            } else
            {
                _lastInputPayload = inputPayload;
            }
        }

        [ServerRpc]
        void SendToServerRpc(InputPayload inputPayload)
        {
            _serverInputQueue.Enqueue(inputPayload);
        }

        private StatePayload ProcessMovement(InputPayload inputPayload) {
            Move(inputPayload.InputVector);

            return new StatePayload
            {
                Tick = inputPayload.Tick,
                Position = transform.position,
                Velocity = _rb.velocity,
                AngularVelocity = _rb.angularVelocity,
            };
        }

        private void Move(Vector2 inputVector)
        {
            float verticalInput = AdjustInput(inputVector.y);
            float horizontalInput = AdjustInput(inputVector.x);

            float motor = _maxMotorTorque * verticalInput;
            float steering = _maxSteeringAngle * horizontalInput;

            UpdateAxles(motor, steering);
            UpdateBanking(horizontalInput);

            _carVelocity = transform.InverseTransformDirection(_rb.velocity);


            if (IsGrounded)
            {
                HandleGroundedMovement(verticalInput, horizontalInput);
            }
            else
            {
                HandleAirborneMovement(verticalInput, horizontalInput);
            }
        }

      

        private void UpdateAxles(float motor, float steering)
        {
            foreach (AxleInfo axleInfo in _axleInfos)
            {
                HandleSteering(axleInfo, steering);
                HandleMotor(axleInfo, motor);
                HandleBrakeAndDrift(axleInfo);
                UpdateWheelVisuals(axleInfo.leftWheel);
                UpdateWheelVisuals(axleInfo.rightWheel);
            }
        }

        private void UpdateBanking(float horizontalInput)
        {
            float targetBankAngle = horizontalInput * -_maxBankAngle;
            Vector3 currentEuler = transform.localEulerAngles;
            currentEuler.z = Mathf.LerpAngle(currentEuler.z, targetBankAngle, Time.fixedDeltaTime * _bankSpeed);
            transform.localEulerAngles = currentEuler;
        }
        private void HandleGroundedMovement(float verticalInput, float horizontalInput)
        {
            // Turn logic
            if (Mathf.Abs(verticalInput) > 0.1f || Mathf.Abs(_carVelocity.z) > 1)
            {
                float turnMultiplier = Mathf.Clamp01(_turnCurve.Evaluate(_carVelocity.magnitude / _maxSpeed));
                _rb.AddTorque(Vector3.up * (horizontalInput * Mathf.Sign(_carVelocity.z) * _turnStrength * 100f * turnMultiplier));
            }

            // Acceleration Logic
            if (!_inputReader.IsBraking)
            {
                float targetSpeed = verticalInput * _maxSpeed;
                Vector3 forwardWithoutY = transform.forward.With(y: 0).normalized;
                float lerpFraction = _timer.MinTimeBetweenTicks;
                _rb.velocity = Vector3.Lerp(_rb.velocity, forwardWithoutY * targetSpeed, lerpFraction);
            }

            // Downforce - always push the cart down, using lateral Gs to scale the force if the Car is moving sideways fast
            float speedFactor = Mathf.Clamp01(_rb.velocity.magnitude / _maxSpeed);
            float lateralG = Mathf.Abs(Vector3.Dot(_rb.velocity, transform.right));
            float downForceFactor = Mathf.Max(speedFactor, lateralG / _lateralGScale);
            _rb.AddForce(-transform.up * (_downForce * _rb.mass * downForceFactor));

            // Shift Center of Mass
            float speed = _rb.velocity.magnitude;
            Vector3 centerOfMassAdjustment = (speed > _thresholdSpeed)
                ? new Vector3(0f, 0f, Mathf.Abs(verticalInput) > 0.1f ? Mathf.Sign(verticalInput) * _centerOfMassOffset : 0f)
                : Vector3.zero;
            _rb.centerOfMass = _originalCenterOfMass + centerOfMassAdjustment;

        }

        private void HandleAirborneMovement(float verticalInput, float horizontalInput)
        {
            _rb.velocity = Vector3.Lerp(_rb.velocity, _rb.velocity + Vector3.down * _gravity, Time.deltaTime * _gravity);
        }

        private void HandleSteering(AxleInfo axleInfo, float steering)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }
        }

        private void HandleMotor(AxleInfo axleInfo, float motor)
        {
            if (axleInfo.motor)
            {
                axleInfo.leftWheel.motorTorque = motor;
                axleInfo.rightWheel.motorTorque = motor;
            }
        }

        private void HandleBrakeAndDrift(AxleInfo axleInfo)
        {
            if (axleInfo.motor)
            {
                if (_inputReader.IsBraking)
                {
                    _rb.constraints = RigidbodyConstraints.FreezeRotationX;

                    float newZ = Mathf.SmoothDamp(_rb.velocity.z, 0, ref _brakeVelocity, 1f);
                    _rb.velocity = _rb.velocity.With(z: newZ);

                    axleInfo.leftWheel.brakeTorque = _brakeTorque;
                    axleInfo.rightWheel.brakeTorque = _brakeTorque;
                    ApplyDriftFriction(axleInfo.leftWheel);
                    ApplyDriftFriction(axleInfo.rightWheel);
                }
                else
                {
                    _rb.constraints = RigidbodyConstraints.None;

                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.brakeTorque = 0;
                    ResetDriftFriction(axleInfo.leftWheel);
                    ResetDriftFriction(axleInfo.rightWheel);
                }
            }
        }
        void ResetDriftFriction(WheelCollider wheel)
        {
            AxleInfo axleInfo = _axleInfos.FirstOrDefault(axle => axle.leftWheel == wheel || axle.rightWheel == wheel);
            if (axleInfo == null) return;

            wheel.forwardFriction = axleInfo.originalForwardFriction;
            wheel.sidewaysFriction = axleInfo.originalSidewayFriction;
        }

        void ApplyDriftFriction(WheelCollider wheel)
        {
            if (wheel.GetGroundHit(out var hit))
            {
                wheel.forwardFriction = UpdateFriction(wheel.forwardFriction);
                wheel.sidewaysFriction = UpdateFriction(wheel.sidewaysFriction);
                IsGrounded = true;
            }
        }

        WheelFrictionCurve UpdateFriction(WheelFrictionCurve friction)
        {
            friction.stiffness = _inputReader.IsBraking ? Mathf.SmoothDamp(friction.stiffness, .5f, ref _driftVelocity, Time.deltaTime * 2f) : 1f;
            return friction;
        }

        private void UpdateWheelVisuals(WheelCollider wheelCollider)
        {
            if (wheelCollider.transform.childCount == 0) return;
            Transform visualWheel = wheelCollider.transform.GetChild(0);
            Vector3 position;
            Quaternion rotation;

            wheelCollider.GetWorldPose(out position, out rotation);
            visualWheel.position = position;
            visualWheel.rotation = rotation;
        }


        float AdjustInput(float input)
        {
            return input switch
            {
                >= .7f => 1f,
                <= -.7f => -1f,
                _ => input
            };
        }



    }
}
