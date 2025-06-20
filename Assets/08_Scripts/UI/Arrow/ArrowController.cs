using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] float radius = 2f;
    [SerializeField] float speed = 5f;
    [SerializeField] float appearDuration = 1f;
    [SerializeField] float heightOffset = 0.7f;
    [SerializeField] Vector3 rotArrow;
    [SerializeField] TutorialDataMgr dataMgr;

    Transform playerPos;
    // TutorialDataMgr의 GetInteractObject를 받아야 하는데...
    Transform targetPos;
    float timeElasped;
    bool isGuiding = false;
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
        if(isGuiding == false)
        {
            return;
        }
        Vector3 currentPlayerPos = playerPos.position;
        float movement = Vector3.Distance(previousPlayerPos, currentPlayerPos);

        if(movement > 0.001f)
        {
            FollowCircleToTarget(currentPlayerPos);
            previousPlayerPos = currentPlayerPos;
        }
    }

    IEnumerator GuideArrowToTarget(Transform targetPos)
    {
        timeElasped = 0;
        while(timeElasped < appearDuration)
        {
            timeElasped += Time.deltaTime;

            float t = Mathf.Clamp01(timeElasped / appearDuration);
            playerPos = turtorialMgr.transform;

            Vector3 forward = playerPos.forward;
            Vector3 dirToTarget = (targetPos.position - playerPos.position).normalized;
            Vector3 currentDir = Vector3.Slerp(forward, dirToTarget, t);

            Vector3 circlePos = playerPos.position + currentDir * radius;
            transform.position = new Vector3(circlePos.x, heightOffset, circlePos.z);

            //Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
            //Quaternion fixedRot = lookRot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
            //transform.rotation = fixedRot;

            Quaternion startRot = Quaternion.LookRotation(forward);
            Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
            Quaternion rot = Quaternion.Slerp(startRot, targetRot, t);
            Quaternion fixedRot = rot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
            transform.rotation = fixedRot;

            yield return null;
        }
        isGuiding = true;
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

    void FollowCircleToTarget(Vector3 pos)
    {
        Vector3 dirToTarget = (targetPos.position - pos).normalized;
        Vector3 targetPosOnCircle = pos + dirToTarget * radius;

        transform.position = Vector3.Lerp(transform.position, targetPosOnCircle, Time.deltaTime * speed);
        transform.position = new Vector3(transform.position.x, heightOffset, transform.position.z);

        Quaternion lookRot = Quaternion.LookRotation(dirToTarget);
        Quaternion fixedRot = lookRot * Quaternion.Euler(rotArrow.x, rotArrow.y, rotArrow.z);
        transform.rotation = fixedRot;
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
        Debug.Log("꺼짐");
        turtorialMgr.OnStartArrow -= AppearArrow;
    }
}
