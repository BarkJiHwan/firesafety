using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] float radius = 2f;
    [SerializeField] float speed = 5f;
    [SerializeField] float appearDuration = 1f;
    [SerializeField] TutorialDataMgr dataMgr;

    Transform playerPos;
    // TutorialDataMgr의 GetInteractObject를 받아야 하는데...
    Transform targetPos;
    float timeElasped;
    bool isGuiding = false;
    Vector3 appearStartPos;
    Vector3 appearEndPos;

    TutorialMgr turtorialMgr;

    CreateArrow createArrow;

    private void Awake()
    {
        createArrow = GetComponent<CreateArrow>();
    }

    void Start()
    {
        if(createArrow != null)
        {
            createArrow.MakeArrow();
        }
    }

    void Update()
    {
        if(turtorialMgr == null)
        {
            turtorialMgr = FindObjectOfType<TutorialMgr>();
        }
    }

    void AppearArrow(int playerIndex)
    {
        // 둘 다 TutorialMgr에 있음
        // playerPos : Player 받아와야 함
        playerPos = turtorialMgr.transform;
        // targetPos : 타겟 받아와야 함
        targetPos = dataMgr.GetInteractObject(playerIndex).transform;

        Vector3 forward = playerPos.forward;
        appearStartPos = playerPos.position + transform.forward * radius;

        Vector3 dirToTarget = (targetPos.position - playerPos.position).normalized;
        appearEndPos = playerPos.position + dirToTarget * radius;

        transform.position = appearStartPos;

        timeElasped += Time.deltaTime;
        float t = Mathf.Clamp01(timeElasped / appearDuration);
        transform.position = Vector3.Lerp(appearStartPos, appearEndPos, t);
        transform.LookAt(targetPos.position);

        if(t >= 1f)
        {
            isGuiding = true;
            FollowCircleToTarget();
        }
    }

    void FollowCircleToTarget()
    {
        Vector3 dirToTarget = (targetPos.position - playerPos.position).normalized;
        Vector3 targetPosOnCircle = playerPos.position + dirToTarget * radius;

        transform.position = Vector3.Lerp(transform.position, targetPosOnCircle, Time.deltaTime * speed);
        transform.LookAt(targetPos.position);
    }
}
