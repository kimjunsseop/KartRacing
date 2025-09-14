using Mirror;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SlidingZone : NetworkBehaviour
{
    [Header("Zone Overrides (optional)")]
    public bool  useOverrides       = true;
    public float maxForwardSpeed    = 33f;
    public float forwardAccel       = 28f;
    public float sideSpeed          = 12f;
    public float sideAccel          = 60f;
    public float yawLockDegPerSec   = 420f;
    public float dragMulOnGround    = 0.85f;

    private void OnTriggerEnter(Collider other)
    {
        if (!isServer) return;

        var kart = other.GetComponentInParent<KartController>();
        if (kart != null)
        {
            Vector3 zoneForward = transform.forward;
            if (useOverrides)
            {
                kart.EnterSlidingZone(zoneForward.normalized, true,
                                      maxForwardSpeed, forwardAccel, sideSpeed, sideAccel,
                                      yawLockDegPerSec, dragMulOnGround);
            }
            else
            {
                kart.EnterSlidingZone(zoneForward.normalized, false, 0,0,0,0, 0,1f);
            }
            return;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isServer) return;

        var kart = other.GetComponentInParent<KartController>();
        if (kart != null)
        {
            kart.ExitSlidingZone();
            return;
        }
    }
}