using UnityEngine;
using Mirror;
using System.Collections;

[RequireComponent(typeof(Rigidbody))]
public class KartController : NetworkBehaviour
{
    [Header("Base")]
    public float topSpeed = 24f;
    public float accel = 60f;
    public float brakeAccel = 80f;
    public float steerAngleMax = 35f;
    public float lateralGrip = 25f;

    [Header("Drift (Handbrake style)")]
    public KeyCode driftKey = KeyCode.LeftShift;
    public float driftMinSpeed = 6f;
    public float driftGripMul = 0.36f;
    public float driftYawMul = 1.0f;
    public float driftAlignMul = 0.33f;
    public float driftSteerBoost = 0.28f;
    public float driftEntrySideKick = 2.8f;
    public float slipEntryAngle = 8f;
    public float slipExitAngle = 3.5f;
    public float speedDecayPerSec = 0.55f;
    public float driftSpeedDecayMul = 0.02f;
    public float counterSteerAssist = 0.22f;

    [Header("Desired Drift Style")]
    public float steerTurnBrake = 0.6f;
    public float driftMoveSideMax = 3f;
    public float driftDirLerpDegPerSec = 150f;
    public float driftAlignMulOverride = 1.2f;
    public float bigSlipReduceCounter = 0.20f;

    [Header("Drift Slide Preference")]
    public bool driftPreferSlide = true;
    public float driftSteerBleed = 1.0f;

    public float driftYawScaleLow = 0.85f;
    public float driftYawScaleHigh = 0.55f;

    public float driftLateralPush = 28f;
    public float driftGripWhileSteerMul = 0.38f;

    [Header("Steer Power Controls (NEW)")]
    public float driftSteerExpo = 0.75f;
    public float driftYawGain = 3.5f;
    public float counterAssistMaxShare = 0.5f;

    [Header("Ground")]
    public Transform groundRay;
    public float groundRayLen = 0.6f;
    public LayerMask groundMask = ~0;

    [Header("Tuning")]
    public float steerSpeedFactor = 0.85f;
    public float airDrag = 0.02f;
    public float groundDrag = 0.05f;

    [Header("Steer Rules")]
    public float minSpeedToAllowSteer = 0.6f;

    [Header("Drift Speed Hold")]
    public float driftBleedPerSec = 0.05f;

    [Header("Arcade Drift Response")]
    public float driftSteerAuthority = 1.5f;
    public float driftSteerSpeedAtten = 0.35f;
    public float driftVelRotateDegPerSec = 220f;
    public float driftGripBaseMul = 0.45f;
    public float driftGripPow = 0.65f;
    public float driftAlignMulDrift = 0.25f;


    [Header("Sliding Zone (Forced Slide)")]
    [SyncVar] public bool inSlidingZone = false;
    [SyncVar] public Vector3 zoneForwardWorld = Vector3.forward;


    public float zoneMaxForwardSpeed = 33f;
    public float zoneForwardAccel = 28f;
    public float zoneSideSpeed = 12f;
    public float zoneSideAccel = 60f;
    public float zoneYawLockDegPerSec = 420f;
    public float zoneDragMulOnGround = 0.85f;


    bool _zoneHasOverride;
    float _ovrMaxForwardSpeed, _ovrForwardAccel, _ovrSideSpeed, _ovrSideAccel, _ovrYawLock, _ovrDragMul;


    [Header("Coast / Engine Braking")]
    public float engineBrakePerSec = 2.0f;
    public float coastTurnBrakeMul = 0.35f;

    [SyncVar] public bool isDrifting;
    [SyncVar] public float speedKmh;
    [SyncVar] public float slipAngleDeg;

    Rigidbody rb;

    [Header("Gameing")]
    private Vector3 lastCheckPointPosition;
    [SyncVar] public int currentLap = 0;
    [SyncVar] public int nextCheckpointIndex = 0;
    [SyncVar] public int requiredLaps = 2;
    double lastCheckpointTick = 0;
    double lastFinishTick = 0;
    public float fallThresholdY = -20f;
    private bool isSpeedBoosted = false;
    private bool isSpeedDown = false;

    [Header("Visual Chassis & Ground Stick (Added)")]
    public Transform chassis;
    public float chassisTiltLerp = 12f;
    public float rideHeight = 0.25f;
    public float groundStickAccel = 25f;
    public float maxAlignSlopeDeg = 40f;

    struct InputState
    {
        public float throttle;
        public float steer;
        public bool drift;
        public double remoteTime;
    }
    InputState lastInput;

    float localThrottle, localSteer; bool localDrift;
    int driftDirSign = 0;

    float _desiredSlipDeg;
    float _slideHoldStrength = 1f;
    float _lastSpeed;
    float _driftStickiness = 0.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
    void Start()
    {
        if (isServer) lastCheckPointPosition = transform.position;
        GameUI.Instance?.SetLapInfo(currentLap, requiredLaps);
    }

    public override void OnStartServer()
    {
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        rb.isKinematic = false;
        Debug.Log($"[Client] {name} local={isLocalPlayer} isServer={isServer} netId={netId}");
    }

    public override void OnStartClient()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (!isServer) rb.isKinematic = true;
        if (isLocalPlayer)
        {
            var cam = Camera.main ? Camera.main.GetComponent<KartCamera>() : null;
            if (cam != null)
            {
                cam.target = transform;
                cam.targetRb = GetComponent<Rigidbody>();
                if (!cam.cam) cam.cam = cam.GetComponent<Camera>();
            }
        }
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        float v = Input.GetAxisRaw("Vertical");
        float h = Input.GetAxisRaw("Horizontal");
        bool driftHeld = Input.GetKey(driftKey);

        localThrottle = Mathf.Clamp(v, -1f, 1f);
        localSteer = Mathf.Clamp(h, -1f, 1f);
        localDrift = driftHeld;

        CmdSetInput(localThrottle, localSteer, localDrift, NetworkTime.time);
    }

    [Command(channel = Channels.Unreliable)]
    void CmdSetInput(float throttle, float steer, bool drift, double sentAt)
    {
        lastInput.throttle = Mathf.Clamp(throttle, -1f, 1f);
        lastInput.steer = Mathf.Clamp(steer, -1f, 1f);
        lastInput.drift = drift;
        lastInput.remoteTime = sentAt;
    }

    void FixedUpdate()
    {
        if (!isServer) return;

        bool grounded = IsGrounded();


        float dragNow = grounded ? groundDrag : airDrag;
        if (grounded && inSlidingZone) dragNow *= (_zoneHasOverride ? _ovrDragMul : zoneDragMulOnGround);
        rb.drag = dragNow;

        Vector3 v = rb.velocity;

        if (inSlidingZone)
        {

            Vector3 up = Vector3.up;
            Vector3 f = Vector3.ProjectOnPlane(zoneForwardWorld, up);
            if (f.sqrMagnitude < 1e-6f) f = zoneForwardWorld; 
            f.Normalize();
            Vector3 r = Vector3.Cross(up, f).normalized;


            float vF = Vector3.Dot(v, f);
            float vR = Vector3.Dot(v, r);
            float vU = Vector3.Dot(v, up);


            float maxF = _zoneHasOverride ? _ovrMaxForwardSpeed : zoneMaxForwardSpeed;
            float aF = _zoneHasOverride ? _ovrForwardAccel : zoneForwardAccel;
            float maxR = _zoneHasOverride ? _ovrSideSpeed : zoneSideSpeed;
            float aR = _zoneHasOverride ? _ovrSideAccel : zoneSideAccel;
            float yawS = _zoneHasOverride ? _ovrYawLock : zoneYawLockDegPerSec;


            float targetF = maxF;
            vF = Mathf.MoveTowards(vF, targetF, aF * Time.fixedDeltaTime);
            if (vF < 0f) vF = Mathf.MoveTowards(vF, 0f, aF * Time.fixedDeltaTime); 


            float targetR = Mathf.Clamp(lastInput.steer, -1f, 1f) * maxR;
            vR = Mathf.MoveTowards(vR, targetR, aR * Time.fixedDeltaTime);

            Quaternion targetRot = Quaternion.LookRotation(f, up);
            rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, yawS * Time.fixedDeltaTime));


            Vector3 vNew = f * vF + r * vR + up * vU;
            rb.velocity = vNew;


            speedKmh = rb.velocity.magnitude * 3.6f;
            slipAngleDeg = 0f; 
            isDrifting = false; 

            if (transform.position.y < fallThresholdY) CmdRequestRespawn();
            return; 
        }



        Vector3 lv = transform.InverseTransformDirection(v);

        // 전/후진 속도 수렴 (코스팅 개선)
        const float reverseFactor = 0.45f;
        float throttle = lastInput.throttle;

        if (throttle > 0f)
        {
            float targetZ = topSpeed * throttle;
            lv.z = Mathf.MoveTowards(lv.z, targetZ, accel * Time.fixedDeltaTime);
        }
        else if (throttle < 0f)
        {
            float targetZ = topSpeed * reverseFactor * throttle; // 음수
            lv.z = Mathf.MoveTowards(lv.z, targetZ, accel * Time.fixedDeltaTime);
        }
        else
        {
            lv.z = Mathf.MoveTowards(lv.z, 0f, engineBrakePerSec * Time.fixedDeltaTime);
        }

        // 일반 코너링 감속
        if (!isDrifting)
        {
            float steerAbs0 = Mathf.Abs(lastInput.steer);
            if (steerAbs0 > 0.01f && lv.z > 0f)
            {
                float turnBrake = steerTurnBrake * steerAbs0 * accel * Time.fixedDeltaTime;
                if (Mathf.Abs(lastInput.throttle) < 0.01f) turnBrake *= coastTurnBrakeMul;
                lv.z = Mathf.MoveTowards(lv.z, 0f, turnBrake);
            }
        }

        float forwardSign = 0f;
        if (Mathf.Abs(lv.z) > 1e-4f) forwardSign = Mathf.Sign(lv.z);
        else if (Mathf.Abs(throttle) > 1e-4f) forwardSign = Mathf.Sign(throttle);
        else forwardSign = 1f;

        float speed = transform.TransformDirection(v).magnitude;

        float slip = Mathf.Atan2(lv.x, Mathf.Abs(lv.z)) * Mathf.Rad2Deg;
        slipAngleDeg = slip;

        bool canDrift = grounded && speed > driftMinSpeed;
        bool driftIntent = canDrift && (Mathf.Abs(lastInput.steer) > 0.1f) && lastInput.drift;
        bool driftHoldBySlip = canDrift && (Mathf.Abs(slip) > slipEntryAngle);

        if (!isDrifting && driftIntent)
        {
            isDrifting = true;
            driftDirSign = Mathf.Sign(lastInput.steer) >= 0f ? 1 : -1;
            lv.x += driftDirSign * (driftEntrySideKick + 0.12f * Mathf.Abs(lv.z));
            float baseTarget = Mathf.Lerp(10f, 28f, Mathf.Clamp01(Mathf.Abs(lastInput.steer)));
            _desiredSlipDeg = Mathf.Sign(driftDirSign) * Mathf.Max(Mathf.Abs(slip), baseTarget);
            _driftStickiness = 0.25f;
        }
        else if (isDrifting && !(driftIntent || driftHoldBySlip))
        {
            if (Mathf.Abs(slip) <= slipExitAngle || !grounded || Mathf.Abs(lv.z) < minSpeedToAllowSteer)
            {
                isDrifting = false;
                driftDirSign = 0;
            }
        }

        // 조향 팩터
        float baseAtten = Mathf.InverseLerp(0f, topSpeed, Mathf.Abs(lv.z));
        float steerFactor = Mathf.Lerp(1f, steerSpeedFactor, isDrifting ? baseAtten * driftSteerSpeedAtten : baseAtten);

        // 입력 가공
        float steerInput;
        float steerInputExp = lastInput.steer;
        if (isDrifting)
        {
            steerInputExp = Mathf.Sign(lastInput.steer) * Mathf.Pow(Mathf.Abs(lastInput.steer), driftSteerExpo);
            steerInput = Mathf.Clamp(steerInputExp * (1f + driftSteerBoost), -1f, 1f);
        }
        else
        {
            steerInput = lastInput.steer;
        }

        bool throttleActive = Mathf.Abs(lastInput.throttle) > 0.01f;
        bool speedAllowsSteer = Mathf.Abs(lv.z) >= minSpeedToAllowSteer;
        if (!throttleActive && !speedAllowsSteer && !isDrifting) steerInput = 0f;

        // 카운터스티어 보정
        if (isDrifting)
        {
            float slip01 = Mathf.InverseLerp(0f, 35f, Mathf.Abs(slip));
            float counterScale = counterSteerAssist * Mathf.Lerp(1f, bigSlipReduceCounter, slip01);
            float counter = Mathf.Clamp(-slip / 30f, -0.8f, 0.8f) * counterScale;
            float maxCounter = Mathf.Abs(steerInput) * counterAssistMaxShare;
            counter = Mathf.Clamp(counter, -maxCounter, maxCounter);
            steerInput = Mathf.Clamp(steerInput + counter, -1f, 1f);
        }

        // 드리프트 입력 이득
        if (isDrifting)
        {
            steerInput *= driftSteerAuthority;
            steerInput = Mathf.Clamp(steerInput, -1f, 1f);
        }

        // 차체 Yaw
        float yawDegPerSec = steerInput * steerAngleMax * steerFactor * forwardSign;
        if (isDrifting)
        {
            float speed01 = Mathf.InverseLerp(0f, topSpeed, Mathf.Abs(lv.z));
            float yawScale = Mathf.Lerp(driftYawScaleLow, driftYawScaleHigh, speed01);
            if (driftPreferSlide) yawDegPerSec *= yawScale * driftYawGain;
            else yawDegPerSec *= driftYawMul * driftYawGain;
        }
        Quaternion dRot = Quaternion.Euler(0f, yawDegPerSec * Time.fixedDeltaTime, 0f);
        rb.MoveRotation(rb.rotation * dRot);

        // 속도벡터 카빙
        if (isDrifting)
        {
            float speed01 = Mathf.InverseLerp(0f, topSpeed, Mathf.Abs(lv.z));
            float steerMag = Mathf.Pow(Mathf.Abs(steerInput), 0.9f);
            if (steerMag > 0.001f)
            {
                float rotDeg = Mathf.Sign(steerInput) * driftVelRotateDegPerSec
                             * (0.25f + 0.75f * speed01) * steerMag * Time.fixedDeltaTime;

                float rad = rotDeg * Mathf.Deg2Rad;
                float cos = Mathf.Cos(rad), sin = Mathf.Sin(rad);
                float x = lv.x, z = lv.z;
                lv.x = x * cos + z * sin;
                lv.z = z * cos - x * sin;
            }
        }

        // 드리프트 lv.x 가속
        if (isDrifting)
        {
            float speed01 = Mathf.InverseLerp(0f, topSpeed, Mathf.Abs(lv.z));
            float slideAbs = Mathf.Abs(steerInputExp);
            if (slideAbs > 0.001f)
            {
                float slideStrength = driftLateralPush * (0.25f + 0.75f * speed01) * slideAbs;
                lv.x += Mathf.Sign(steerInputExp) * slideStrength * Time.fixedDeltaTime;
            }
        }

 
        if (isDrifting)
        {
            float steerAbs = Mathf.Abs(lastInput.steer);
            float steerSign = Mathf.Sign(lastInput.steer == 0f ? driftDirSign : lastInput.steer);

            float targetSide = steerSign * Mathf.Lerp(0.35f, driftMoveSideMax, steerAbs);
            Vector2 desiredDir = new Vector2(targetSide, 1f).normalized;
            float targetDeg = Mathf.Atan2(desiredDir.x, desiredDir.y) * Mathf.Rad2Deg;

            if (_driftStickiness > 0f)
            {
                targetDeg = Mathf.Lerp(targetDeg, steerSign * 28f, 0.5f);
                _driftStickiness -= Time.fixedDeltaTime;
            }

            float currDeg = Mathf.Atan2(lv.x, Mathf.Abs(lv.z)) * Mathf.Rad2Deg;

            float slideWeight = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(steerAbs + 0.2f));
            float speedWeight = Mathf.InverseLerp(0f, topSpeed, Mathf.Abs(lv.z));
            float approach = driftDirLerpDegPerSec * slideWeight * (0.4f + 0.6f * speedWeight);

            float newDeg = Mathf.MoveTowardsAngle(currDeg, targetDeg, approach * Time.fixedDeltaTime);

            float speedMag = new Vector2(lv.x, lv.z).magnitude;
            float radMove = newDeg * Mathf.Deg2Rad;
            lv.x = Mathf.Sin(radMove) * speedMag;
            lv.z = Mathf.Cos(radMove) * speedMag * Mathf.Sign(lv.z == 0f ? 1f : lv.z);

            _desiredSlipDeg = newDeg;
        }

        // 그립/정렬/감속
        float baseGrip = lateralGrip * (isDrifting ? driftGripBaseMul : 1f);
        bool reverseIntent = lastInput.throttle < -0.1f;
        bool noSteer = Mathf.Abs(lastInput.steer) < 0.05f;
        bool veryLowSpeedLocal = Mathf.Abs(lv.z) < 1.5f && Mathf.Abs(lv.x) < 1.5f;
        bool reverseStraightLock = grounded && reverseIntent && noSteer && !isDrifting && veryLowSpeedLocal;

        if (reverseStraightLock)
        {
            lv.x = 0f;
            slip = 0f;
        }
        else
        {
            float steerAbs = Mathf.Abs(lastInput.steer);
            float gripUse = baseGrip;
            if (isDrifting && steerAbs > 0.001f)
            {
                float steerCurve = Mathf.Pow(steerAbs, driftGripPow);
                gripUse *= Mathf.Lerp(1f, driftGripWhileSteerMul, steerCurve);
            }
            lv.x = Mathf.MoveTowards(lv.x, 0f, gripUse * Time.fixedDeltaTime);
        }

        float baseAlign = 85f;
        float alignRateDegPerSec = isDrifting ? baseAlign * driftAlignMulDrift : baseAlign;
        if (reverseStraightLock) alignRateDegPerSec = Mathf.Max(alignRateDegPerSec, 360f);

        float newSlip = Mathf.MoveTowardsAngle(slip, 0f, alignRateDegPerSec * Time.fixedDeltaTime);

        float preDecayMag = new Vector2(lv.x, lv.z).magnitude;
        float decayMul = isDrifting ? driftSpeedDecayMul : 1f;
        float sBase = Mathf.Max(0f, preDecayMag - speedDecayPerSec * decayMul * Time.fixedDeltaTime);

        float s;
        if (isDrifting)
        {
            float minAllowed = Mathf.Max(0f, preDecayMag - driftBleedPerSec * Time.fixedDeltaTime);
            s = Mathf.Max(sBase, minAllowed);
        }
        else s = sBase;

        float rad2 = newSlip * Mathf.Deg2Rad;
        lv.x = Mathf.Sin(rad2) * s;
        lv.z = forwardSign * Mathf.Cos(rad2) * s;

        rb.velocity = transform.TransformDirection(lv);

        speedKmh = rb.velocity.magnitude * 3.6f;
        _lastSpeed = speed;

        if (transform.position.y < fallThresholdY)
            CmdRequestRespawn();

        if (Physics.Raycast(groundRay ? groundRay.position : transform.position,
                    Vector3.down, out RaycastHit hit, groundRayLen, groundMask,
                    QueryTriggerInteraction.Ignore))
        {
            // 차량 모델 기울이기
            if (chassis)
            {
                // 절벽에선 패스
                float slope = Vector3.Angle(hit.normal, Vector3.up);
                if (slope <= maxAlignSlopeDeg)
                {
                    Quaternion target = Quaternion.FromToRotation(chassis.up, hit.normal) * chassis.rotation;
                    chassis.rotation = Quaternion.Slerp(chassis.rotation, target, chassisTiltLerp * Time.fixedDeltaTime);
                }
            }

            rb.AddForce(-hit.normal * groundStickAccel, ForceMode.Acceleration);

            Vector3 targetPos = hit.point + hit.normal * rideHeight;
            Vector3 posError = targetPos - rb.position;
            float springK = 60f;  
            float damper = 8f;
            Vector3 velAt = rb.GetPointVelocity(transform.position);
            float alongN = Vector3.Dot(velAt, hit.normal);
            Vector3 springForce = (posError * springK - hit.normal * alongN * damper);
            rb.AddForce(springForce, ForceMode.Acceleration);
        }
    }
    [ClientCallback] void LateUpdate()
    {
        VisualAlignChassis();
    }
    void VisualAlignChassis()
    {
        if (!chassis) return;

        
        if (Physics.Raycast(groundRay ? groundRay.position : transform.position,
                            Vector3.down, out RaycastHit hit, groundRayLen, groundMask,
                            QueryTriggerInteraction.Ignore))
        {
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            if (slope <= maxAlignSlopeDeg)
            {
                Quaternion target = Quaternion.FromToRotation(chassis.up, hit.normal) * chassis.rotation;
                chassis.rotation  = Quaternion.Slerp(chassis.rotation, target, chassisTiltLerp * Time.deltaTime);
                return;
            }
        }

        Quaternion upright = Quaternion.FromToRotation(chassis.up, Vector3.up) * chassis.rotation;
        chassis.rotation   = Quaternion.Slerp(chassis.rotation, upright, (chassisTiltLerp * 0.6f) * Time.deltaTime);
    }


    bool IsGrounded()
    {
        Transform origin = groundRay ? groundRay : transform;
        return Physics.Raycast(origin.position, Vector3.down, groundRayLen, groundMask, QueryTriggerInteraction.Ignore);
    }

    [Server]
    public void EnterSlidingZone(Vector3 zoneForward, bool useOverride,
                                 float maxF, float aF, float sideSpd, float aR,
                                 float yawLock, float dragMul)
    {
        inSlidingZone = true;
        zoneForwardWorld = zoneForward.normalized;

        _zoneHasOverride = useOverride;
        _ovrMaxForwardSpeed = maxF;
        _ovrForwardAccel = aF;
        _ovrSideSpeed = sideSpd;
        _ovrSideAccel = aR;
        _ovrYawLock = yawLock;
        _ovrDragMul = Mathf.Max(0.05f, dragMul);
    }

    [Server]
    public void ExitSlidingZone()
    {
        inSlidingZone = false;
        _zoneHasOverride = false;
    }

    [Server]
    public void UpdateCheckpoint(Vector3 checkpoint) { lastCheckPointPosition = checkpoint; }

    [Server]
    public void OnCheckpointpassed(int index, Vector3 checkpointPos)
    {
        if (index == nextCheckpointIndex)
        {
            nextCheckpointIndex++;
            lastCheckPointPosition = checkpointPos;
            lastCheckpointTick = NetworkTime.time;
        }
    }
    [TargetRpc]
    public void TargetUpdateLap(NetworkConnection conn, int curLap, int totalLap)
    {
        GameUI.Instance?.SetLapInfo(curLap, totalLap);
    }

    [Server]
    public bool IsEligibleForFinish(int totalCheckpoints)
    {
        return nextCheckpointIndex == totalCheckpoints;
    }
    [Command]
    void CmdRequestRespawn()
    {
        transform.position = lastCheckPointPosition;
        rb.velocity = Vector3.zero;
    }
    [Server]
    public void ApplyItemEffect(ItemPickup.ItemType itemType, float duration)
    {
        switch (itemType)
        {
            case ItemPickup.ItemType.SpeedBoost:
                if (!isSpeedBoosted)
                {
                    StartCoroutine(SpeedBoostRoutine(duration));
                    RpcShowBuff(connectionToClient, duration);
                }
                break;

            case ItemPickup.ItemType.SpeedDown:
                if (!isSpeedDown)
                {
                    StartCoroutine(SpeedDownRoutine(duration));
                    RpcShowBuff(connectionToClient, duration);
                }
                break;
        }
    }
    IEnumerator SpeedBoostRoutine(float duration)
    {
        isSpeedBoosted = true;
        topSpeed *= 1.5f;
        yield return new WaitForSeconds(duration);
        topSpeed /= 1.5f;
        isSpeedBoosted = false;
    }
    IEnumerator SpeedDownRoutine(float duration)
    {
        isSpeedDown = true;
        topSpeed /= 1.5f;
        yield return new WaitForSeconds(duration);
        topSpeed *= 1.5f;
        isSpeedDown = false;
    }   [TargetRpc]
    void RpcShowBuff(NetworkConnection conn, float duration)
    {
        GameUI.Instance?.ShowBuffBar(duration);
    }
}