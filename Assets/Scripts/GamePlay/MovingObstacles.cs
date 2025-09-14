using UnityEngine;
using Mirror;
using UnityEngine.UIElements;

public class MovingObstacles : NetworkBehaviour
{
    public enum MovementType { horizontal, vertical }
    public MovementType movementType;
    public float speed = 2f;
    public float range = 3f;
    private Vector3 startPos;
    private bool axisMove = true;
    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        if (!isServer)
        {
            return;
        }
        Vector3 pos = transform.position;
        switch (movementType)
        {
            case MovementType.horizontal:
                MoveAxis(ref pos.x, startPos.x);
                break;
            case MovementType.vertical:
                MoveAxis(ref pos.y, startPos.y);
                break;
        }
        transform.position = pos;
    }

    void MoveAxis(ref float axis, float origin)
    {
        float max = origin + range;
        float min = origin - range;
        if (axisMove)
        {
            axis += speed * Time.deltaTime;
            if (axis >= max)
            {
                axisMove = false;
            }
        }
        else
        {
            axis -= speed * Time.deltaTime;
            if (axis <= min)
            {
                axisMove = true;
            }
        }
    }
}