using UnityEngine;


public class KartCamera : MonoBehaviour
{
    public Transform target;
    public Rigidbody targetRb;

    [Header("Rig")]
    public float distance = 6.0f;
    public float height = 2.0f;
    public float followDamping = 8f;
    public float rotateDamping = 12f;

    [Header("Aim (전진 전용)")]
    public float lookAhead = 4.0f;
    public float velocityInfluence = 0.6f;

    [Header("Reverse Tuning")]
    public float reverseExtraDistance = 1.2f;
    public float reverseRotateDampingMul = 1.2f;

    [Header("FOV")]
    public Camera cam;
    public float baseFov = 60f;
    public float fovAtTopSpeed = 78f;
    public float topSpeedKmh = 140f;

    void LateUpdate()
    {
        if (!target) return;
        if (!targetRb) targetRb = target.GetComponent<Rigidbody>();


        Vector3 fwdFlat = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (fwdFlat.sqrMagnitude < 1e-6f) fwdFlat = target.forward;
        fwdFlat.Normalize();


        float dot = 1f;
        Vector3 vel = Vector3.zero, velFlat = Vector3.zero;
        if (targetRb)
        {
            vel = targetRb.velocity;
            velFlat = Vector3.ProjectOnPlane(vel, Vector3.up);
            if (velFlat.sqrMagnitude > 1e-6f)
            {
                velFlat.Normalize();
                dot = Vector3.Dot(fwdFlat, velFlat);
            }
        }

        bool isReversing = dot < 0f && vel.sqrMagnitude > 0.05f;


        float dist = distance + (isReversing ? reverseExtraDistance : 0f);
        Vector3 desiredPos = target.position + (-fwdFlat) * dist + Vector3.up * height;


        float posLerp = 1f - Mathf.Exp(-followDamping * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, posLerp);


        Vector3 aimPoint = target.position + Vector3.up * (height * 0.4f);

        if (!isReversing)
        {
            if (velFlat.sqrMagnitude > 1e-6f)
            {
                Vector3 aimDir = Vector3.Slerp(fwdFlat, velFlat, Mathf.Clamp01(velocityInfluence));
                aimPoint += aimDir * lookAhead;
            }
            else
            {
                aimPoint += fwdFlat * (lookAhead * 0.5f);
            }
        }

        Quaternion desiredRot = Quaternion.LookRotation((aimPoint - transform.position).normalized, Vector3.up);
        float rotDamp = rotateDamping * (isReversing ? reverseRotateDampingMul : 1f);
        float rotLerp = 1f - Mathf.Exp(-rotDamp * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, rotLerp);

        if (cam)
        {
            float speedKmh = targetRb ? targetRb.velocity.magnitude * 3.6f : 0f;
            float t = Mathf.InverseLerp(0f, topSpeedKmh, speedKmh);
            float targetFov = Mathf.Lerp(baseFov, fovAtTopSpeed, t);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
    }
}