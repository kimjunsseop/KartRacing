using UnityEngine;
using Mirror;
using System.Collections;
public class SuddenObstacle : NetworkBehaviour
{
    [Header("돌출 설정")]
    public Vector3 hiddenOffset = new Vector3(0, -4f, 0);
    public float popUpSpeed = 10f;
    public float stayTime = 1.5f;
    public float interval = 4f;
    public float bounceForce = 10f;

    private Vector3 startPos;
    private Vector3 hiddenPos;
    private Vector3 targetPos;

    private bool isPoppedUp = false;
    private bool isMoving = false;
    private float nextActionTime;

    void Start()
    {
        startPos = transform.position;
        hiddenPos = startPos + hiddenOffset;
        if (isServer)
        {
            transform.position = hiddenPos;
            targetPos = hiddenPos;
            nextActionTime = Time.time + Random.Range(0f, 2f);
        }
    }

    void Update()
    {
        if (!isServer) return;

        if (!isMoving && Time.time >= nextActionTime)
        {
            isPoppedUp = !isPoppedUp;
            targetPos = isPoppedUp ? startPos : hiddenPos;
            StartCoroutine(MoveToTarget());
        }
    }

    IEnumerator MoveToTarget()
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, popUpSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;

        if (isPoppedUp)
        {
            // 유지 시간 후 자동 복귀
            nextActionTime = Time.time + stayTime;
        }
        else
        {
            nextActionTime = Time.time + interval;
        }
    }
    void OnCollisionEnter(Collision collision)
    {
        if (!isServer || !isPoppedUp) return;

        Rigidbody playerRb = collision.rigidbody;
        if (playerRb != null)
        {
            Vector3 forceDir = (collision.transform.position - transform.position).normalized;
            playerRb.AddForce(forceDir * bounceForce, ForceMode.Impulse);
        }
        
    }
}