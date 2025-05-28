using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class MakeCurvedMesh : MonoBehaviour
{
    [SerializeField] float radius;
    [SerializeField] float height;
    [SerializeField] float angle;
    [SerializeField] int segement;

    void Start()
    {
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

            // UV 맵핑 (왼쪽에서 오른쪽으로)
            uvs[i * 2] = new Vector2((float)i / segement, 0); // 아래쪽
            uvs[i * 2 + 1] = new Vector2((float)i / segement, 1); // 위쪽
        }

        int triIndex = 0;
        // 삼각형 인덱스 생성
        for(int i=0; i<segement; i++)
        {
            int baseIndex = i * 2;

            // 첫번째 삼각형
            triangles[triIndex++] = baseIndex; // 아래
            triangles[triIndex++] = baseIndex + 3; // 위
            triangles[triIndex++] = baseIndex + 1; // 위 + 1

            // 두번째 삼각형
            triangles[triIndex++] = baseIndex; // 아래
            triangles[triIndex++] = baseIndex + 2; // 아래 + 1
            triangles[triIndex++] = baseIndex + 3; // 위 + 1

        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        // 법선 벡터 재계산
        mesh.RecalculateNormals();

        // MeshFilter에 메쉬 할당
        GetComponent<MeshFilter>().mesh = mesh;
    }
}
