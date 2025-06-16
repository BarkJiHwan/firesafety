using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MakeCurvedMesh : MonoBehaviour
{
    public float radius;
    public float height { get; private set; }
    [SerializeField] float heightOffset;
    public float angle;
    public int segement;
    [SerializeField] Canvas canvas;
    [SerializeField] float offset;
    [SerializeField] GameObject xrRig;
    [SerializeField] Camera renderTextureCam;
    [SerializeField] Vector3 xrRigCameraOffSet = new Vector3(0, 5, 0.2f);
    [SerializeField] bool isFullScreenButton;
    [Range(0f, 1f)]
    [SerializeField] float baseOrthoSize;

    RectTransform canvasTransform;
    MeshCollider meshCol;
    CanvasMeshRootCtrl canvasMeshCtrl;

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
        height = canvasTransform.rect.height * canvas.transform.localScale.y * 2 - heightOffset;
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

    void PositionCurvedUIInFront()
    {
        // 기준 방향 : Canvas의 정면
        Vector3 canForward = canvas.transform.forward;
        // 기준 위치 : Canvas의 중심
        Vector3 canvasPos = canvas.transform.position;
        Vector3 meshPos = canvasPos - canForward * (radius / 2 + offset) + new Vector3(0, heightOffset, 0);
        if(isFullScreenButton == true)
        {
            float canvasHeight = canvasTransform.rect.height * canvasTransform.lossyScale.y / 2;
            meshPos += new Vector3(0, canvasHeight / 2 - 0.2f, 0);
        }
        //Transform xrOriginTrans = Camera.main.transform.parent;
        //xrOriginTrans.position = meshPos - new Vector3(0, 0, 1f);
        transform.position = meshPos;
    }

    void PositionFixedUIInfront()
    {

    }

    void MoveCameraCenter(GameObject xrRigObject)
    {
        if (xrRigObject == null)
            return;
        //Vector3 meshCenter = transform.position;
        Vector3 meshCenter = transform.position;

        Vector3 meshNormal = transform.forward;

        xrRigObject.transform.position = meshCenter + meshNormal - new Vector3(0, height / xrRigCameraOffSet.y, xrRigCameraOffSet.z);
        xrRigObject.transform.rotation = Quaternion.LookRotation(meshNormal, Vector3.up);

        xrRigCurvedMeshDist = transform.position - xrRigObject.transform.position;
    }

}
