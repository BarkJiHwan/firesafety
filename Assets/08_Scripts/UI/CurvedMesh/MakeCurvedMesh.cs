using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MakeCurvedMesh : MonoBehaviour
{
    // 곡면 UI 반지름
    public float radius;
    // 곡면 UI 높이
    public float height { get; private set; }
    [SerializeField] float heightOffset;
    // 곡면 UI의 커브 각도
    public float angle;
    // Mesh 세그먼트 수 (곡률 해상도)
    public int segement;
    // RenderTexture로 넣을 canvase
    [SerializeField] Canvas canvas;
    // 캔버스와 Mesh 간의 거리
    [SerializeField] float offset;
    [SerializeField] GameObject xrRig;
    // UI 렌더링용 카메라
    [SerializeField] Camera renderTextureCam;
    // XR Rig MainCamera 위치 오프셋
    [SerializeField] Vector3 xrRigCameraOffSet = new Vector3(0, 5, 0.2f);
    // 전체 화면이 버튼인지에 대한 여부
    [SerializeField] bool isFullScreenButton;
    // 기본 직교 카메라 크기 비율
    [Range(0f, 1f)]
    [SerializeField] float baseOrthoSize;

    // 캔버스의 RectTransform
    RectTransform canvasTransform;
    MeshCollider meshCol;
    CanvasMeshRootCtrl canvasMeshCtrl;

    // XR Rig와 메시 간의 거리
    public Vector3 xrRigCurvedMeshDist
    {
        get; private set;
    }

    void Start()
    {
        canvasMeshCtrl = GetComponentInParent<CanvasMeshRootCtrl>();
        meshCol = GetComponent<MeshCollider>();
        if(canvas != null)
        {
            canvasTransform = canvas.GetComponent<RectTransform>();
        }
        // 캔버스의 실제 높이를 기반으로 메시 높이 계산 (스케일 고려)
        height = canvasTransform.rect.height * canvas.transform.localScale.y * 2 - heightOffset;
        // 메시 생성
        MakeCurvedUIMesh();
    }

    void MakeCurvedUIMesh()
    {
        Mesh mesh = new Mesh();

        // 꼭지점 개수 = 세그먼트 + 1 만큼의 세로 줄이 2개(상/하)
        int vertCount = (segement + 1) * 2;
        Vector3[] vertices = new Vector3[vertCount]; // 꼭지점 위치 배열
        Vector2[] uvs = new Vector2[vertCount]; // UV 텍스처 좌표 배열
        int[] triangles = new int[segement * 6];

        // 라디안 각도를 세그먼트 수로 나눔
        float angleStep = Mathf.Deg2Rad * angle / segement;
        float halfHeight = height / 2f; // 중심 기준으로 위아래 분리

        // 각 세그먼트의 좌표 계산
        for(int i=0; i<=segement; i++)
        {
            float currentAngle = -angle * 0.5f * Mathf.Deg2Rad + i * angleStep;

            // XZ 평면에서 원형 좌표 계산
            float x = Mathf.Sin(currentAngle) * radius;
            float z = Mathf.Cos(currentAngle) * radius;

            // 하단 꼭지점
            vertices[i * 2] = new Vector3(x, -halfHeight, z);
            // 상단 꼭지점
            vertices[i * 2 + 1] = new Vector3(x, halfHeight, z);

            // 좌우반전된 U
            float uvX = ((float)i / segement);
            // UV 맵핑 (왼쪽에서 오른쪽으로)
            uvs[i * 2] = new Vector2(uvX, 0); // 아래쪽
            uvs[i * 2 + 1] = new Vector2(uvX, 1); // 위쪽
        }

        int triIndex = 0;
        // 삼각형 인덱스 생성
        for(int i=0; i<segement; i++)
        {
            int baseIndex = i * 2;

            // 첫번째 삼각형
            triangles[triIndex++] = baseIndex + 1; // 위
            triangles[triIndex++] = baseIndex; // 위 + 1
            triangles[triIndex++] = baseIndex + 3; // 아래

            // 두번째 삼각형
            triangles[triIndex++] = baseIndex + 3; // 위 + 1
            triangles[triIndex++] = baseIndex; // 아래 + 1
            triangles[triIndex++] = baseIndex + 2; // 아래

        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // 법선 벡터 재계산 -> 원통형 밖에 렌더링됨
        //mesh.RecalculateNormals();
        Vector3[] normals = new Vector3[vertCount];
        for(int i=0; i<=segement; i++)
        {
            float currentAngle = -angle * 0.5f * Mathf.Deg2Rad + i * angleStep;

            Vector3 normal = new Vector3(Mathf.Sin(currentAngle), 0, Mathf.Cos(currentAngle));
            normals[i * 2] = normal;
            normals[i * 2 + 1] = normal;
        }
        mesh.normals = normals;

        if(isFullScreenButton == true)
        {
            // 픽셀 단위 크기
            float pixelWidth = canvasTransform.rect.width;
            float pixelHeight = canvasTransform.rect.height;

            // 실제 월드 유닛 기준 크기
            float worldWidth = pixelWidth * canvasTransform.lossyScale.x;
            float worldHeight = pixelHeight * canvasTransform.lossyScale.y;

            // 곡률 반지름과 각도로 전체 호 길이 계산
            float arcLength = Mathf.Deg2Rad * angle * radius;

            float othoSize = renderTextureCam.orthographicSize;
            float scaleFactor = arcLength / pixelWidth;
            canvas.transform.localScale = Vector3.one * scaleFactor;

            renderTextureCam.orthographicSize = othoSize * worldHeight * baseOrthoSize;
            //Debug.Log(renderTextureCam.orthographicSize);
        }

        //transform.position += new Vector3(0, 0.9f, 0);
        if(canvasMeshCtrl.isPlayerMove == false)
        {
            PositionCurvedUIInFront();
            MoveCameraCenter(xrRig);
        }
        // Ingame 내에서 캐릭터가 생성되기 때문에 xrRig 필요
        else
        {
            transform.position += new Vector3(0, xrRigCameraOffSet.y, xrRigCameraOffSet.z);
        }
        // 카메라를 원통형의 중간으로 오게 함

        // MeshFilter에 메쉬 할당
        meshCol.sharedMesh = mesh;
        GetComponent<MeshFilter>().mesh = mesh;
    }

    // 곡면 UI 메시를 캔버스 앞에 위치 시키는 함수
    void PositionCurvedUIInFront()
    {
        // 기준 방향 : Canvas의 정면
        Vector3 canForward = canvas.transform.forward;
        // 기준 위치 : Canvas의 중심
        Vector3 canvasPos = canvas.transform.position;
        // 메시 위치 계산 : 캔버스보다 일정 거리 앞쪽에 배치
        Vector3 meshPos = canvasPos - canForward * (radius / 2 + offset) + new Vector3(0, heightOffset, 0);
        // 전체 화면일 경우
        if(isFullScreenButton == true)
        {
            // 약간 더 위로 위치 보정
            float canvasHeight = canvasTransform.rect.height * canvasTransform.lossyScale.y / 2;
            meshPos += new Vector3(0, canvasHeight / 2 - 0.2f, 0);
        }
        //Transform xrOriginTrans = Camera.main.transform.parent;
        //xrOriginTrans.position = meshPos - new Vector3(0, 0, 1f);
        // 메시 위치 설정
        transform.position = meshPos;
    }

    // XR Rig을 메시 중심 기준으로 회전/위치 이동시키는 함수
    void MoveCameraCenter(GameObject xrRigObject)
    {
        if (xrRigObject == null)
            return;
        //Vector3 meshCenter = transform.position;
        // 곡면 메시 중심 위치
        Vector3 meshCenter = transform.position;
        // 메시의 전방 방향
        Vector3 meshNormal = transform.forward;

        // XR Rig 위치 이동 : 메시 앞에 배치
        xrRigObject.transform.position = meshCenter + meshNormal - new Vector3(0, height / xrRigCameraOffSet.y, xrRigCameraOffSet.z);
        // XR Rig이 메시를 바라보도록 회전
        xrRigObject.transform.rotation = Quaternion.LookRotation(meshNormal, Vector3.up);
        // 거리 저장
        xrRigCurvedMeshDist = transform.position - xrRigObject.transform.position;
    }

}
