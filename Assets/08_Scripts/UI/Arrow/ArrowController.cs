using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] TutorialDataMgr dataMgr;

    [Header("원 회전")]
    [SerializeField] float radius = 2f;
    [SerializeField] float speed = 5f;
    [SerializeField] float appearDuration = 1f;
    [SerializeField] float heightOffset = 0.7f;
    [SerializeField] Vector3 rotArrow;

    [Header("날아가기")]
    [SerializeField] float flyDuration = 1.0f;

    Transform playerPos;
    // TutorialDataMgr의 GetInteractObject를 받아야 하는데...
    Transform targetPos;
    float timeElasped;
    Vector3 appearStartPos;
    Vector3 appearEndPos;
    Vector3 previousPlayerPos;

    TutorialMgr turtorialMgr;

    CreateArrow createArrow;
    bool isAlreadyMade = false;

    private void Awake()
    {
        createArrow = GetComponent<CreateArrow>();
    }

    void Start()
    {
        //if(createArrow != null)
        //{
        //    createArrow.MakeArrow();
        //}
    }

    void Update()
    {
        if(turtorialMgr == null)
        {
            turtorialMgr = FindObjectOfType<TutorialMgr>();
        }
        if(turtorialMgr != null && isAlreadyMade == false)
        {
            turtorialMgr.arrowCtrl = this;
            turtorialMgr.OnStartArrow += AppearArrow;
            isAlreadyMade = true;
        }
    }

    IEnumerator GuideArrowToTarget(Transform targetPos)
    {
        timeElasped = 0;
        playerPos = turtorialMgr.transform;
        Vector3 forward = playerPos.forward;
        Vector3 dirToTarget = (targetPos.position - playerPos.position).normalized;

        Quaternion startRot = Quaternion.LookRotation(forward);
        Quaternion targetRot = Quaternion.LookRotation(dirToTarget);

        float angle = Vector3.Angle(forward, dirToTarget);
        float currentAngle = 0;
        while (currentAngle < angle)
        {
            float stepAngle = speed * Time.deltaTime;
            currentAngle += stepAngle;

            float t = Mathf.Clamp01(currentAngle / angle);

            Vector3 currentDir = Vector3.Slerp(forward, dirToTarget, t);
            Vector3 circlePos = playerPos.position + currentDir * radius;
            transform.position = new Vector3(circlePos.x, heightOffset, circlePos.z);

            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);
            Quaternion fixedRot = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
            transform.rotation = fixedRot;

            yield return null;
        }

        Vector3 startPos = transform.position;
        Vector3 goalPos = targetPos.position + Vector3.up * heightOffset;

        timeElasped = 0;
        while(timeElasped < flyDuration)
        {
            timeElasped += Time.deltaTime;
            float t = Mathf.Clamp01(timeElasped / flyDuration);

            transform.position = Vector3.Lerp(startPos, goalPos, t);

            Vector3 dir = (goalPos - transform.position).normalized;
            Quaternion rot = Quaternion.LookRotation(dir);
            transform.rotation = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);

            yield return null;
        }
        transform.rotation = Quaternion.Euler(0, 0, -90);
    }

    void AppearArrow(int playerIndex)
    {
        // 둘 다 TutorialMgr에 있음
        // playerPos : Player 받아와야 함
        playerPos = turtorialMgr.transform;
        // targetPos : 타겟 받아와야 함
        targetPos = dataMgr.GetInteractObject(playerIndex).transform;

        if (createArrow != null)
        {
            createArrow.MakeArrow();
        }

        //Quaternion rot = Quaternion.LookRotation(playerPos.forward);
        //Quaternion fixedRot = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
        //transform.rotation = fixedRot;

        StartCoroutine(GuideArrowToTarget(targetPos));
    }

    private void OnEnable()
    {
        if (turtorialMgr == null)
        {
            return;
        }
        turtorialMgr.OnStartArrow += AppearArrow;
        isAlreadyMade = true;
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        turtorialMgr.OnStartArrow -= AppearArrow;
    }
}
