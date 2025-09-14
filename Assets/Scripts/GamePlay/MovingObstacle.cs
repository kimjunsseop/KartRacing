using UnityEngine;
using Mirror;

public class MovingObstacle : NetworkBehaviour
{
    [Header("Direction")]
    public bool goRight = true;
    public bool goLeft  = false;

    [Header("Motion")]
    public float range = 3f;
    public float moveSpeed = 2f;

    private Vector3 startPos;
    private bool goingOut = true;

    void Start()
    {
        startPos = transform.position;


        if (goRight == goLeft)
        {
            goRight = true;
            goLeft  = false;
        }

        range = Mathf.Abs(range);
    }

    void Update()
    {
        if (!isServer) return;

        float targetOutX = startPos.x + (goRight ? +range : -range);
        float targetHomeX = startPos.x;

        Vector3 pos = transform.position;
        float step = moveSpeed * Time.deltaTime;

        if (goingOut)
        {
            pos.x = Mathf.MoveTowards(pos.x, targetOutX, step);
            if (Mathf.Abs(pos.x - targetOutX) < 0.0005f)
                goingOut = false;
        }
        else
        {
            pos.x = Mathf.MoveTowards(pos.x, targetHomeX, step);
            if (Mathf.Abs(pos.x - targetHomeX) < 0.0005f)
                goingOut = true;
        }

        transform.position = pos;
    }
}