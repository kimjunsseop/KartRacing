using UnityEngine;
using Mirror;
using System.Collections;

public class StartGateController : NetworkBehaviour
{
    public Transform leftDoor;
    public Transform rightDoor;

    public float openAngle = 90f;
    public float openDuration = 2f;

    private Quaternion leftInitialRot;
    private Quaternion rightInitialRot;

    void Start()
    {
        // 초기 회전값 저장
        leftInitialRot = leftDoor.localRotation;
        rightInitialRot = rightDoor.localRotation;
    }

    [Server]
    public void OpenGate()
    {
        RpcOpenGate();
    }

    [ClientRpc]
    void RpcOpenGate()
    {
        StopAllCoroutines();
        StartCoroutine(OpenDoors());
    }

    IEnumerator OpenDoors()
    {
        float t = 0;
        Quaternion leftTarget = leftInitialRot * Quaternion.Euler(0, openAngle, 0);
        Quaternion rightTarget = rightInitialRot * Quaternion.Euler(0, -openAngle, 0);

        while (t < openDuration)
        {
            t += Time.deltaTime;
            float ratio = Mathf.Clamp01(t / openDuration);
            leftDoor.localRotation = Quaternion.Slerp(leftInitialRot, leftTarget, ratio);
            rightDoor.localRotation = Quaternion.Slerp(rightInitialRot, rightTarget, ratio);
            yield return null;
        }
    }
}