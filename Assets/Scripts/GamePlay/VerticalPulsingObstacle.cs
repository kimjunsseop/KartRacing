using UnityEngine;
using Mirror;

public class VerticalPulsingObstacle : NetworkBehaviour
{
    public float moveSpeed = 1.5f;
    public float moveHeight = 2f;
    public float stayAtTopMin = 0.3f;
    public float stayAtTopMax = 1.2f;
    public float stayAtBottomMin = 0.3f;
    public float stayAtBottomMax = 1.2f;

    private Vector3 startPos;
    private bool movingUp = true;
    private float waitTimer = 0f;
    private enum State { Waiting, Moving }
    private State state = State.Waiting;

    public override void OnStartServer()
    {
        base.OnStartServer();

        startPos = transform.position;
        waitTimer = Time.time + Random.Range(stayAtBottomMin, stayAtBottomMax);
        state = State.Waiting;
        movingUp = true;
    }

    void Update()
    {
        if (!isServer) return; // 서버에서만 제어

        if (state == State.Waiting)
        {
            if (Time.time >= waitTimer)
            {
                state = State.Moving;
            }
            return;
        }

        // 이동 중
        float step = moveSpeed * Time.deltaTime;
        Vector3 targetPos = movingUp ? startPos + Vector3.up * moveHeight : startPos;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, step);

        if (Vector3.Distance(transform.position, targetPos) < 0.001f)
        {
            state = State.Waiting;
            movingUp = !movingUp;

            if (movingUp)
                waitTimer = Time.time + Random.Range(stayAtBottomMin, stayAtBottomMax); // 아래 → 위
            else
                waitTimer = Time.time + Random.Range(stayAtTopMin, stayAtTopMax);       // 위 → 아래
        }
    }
}