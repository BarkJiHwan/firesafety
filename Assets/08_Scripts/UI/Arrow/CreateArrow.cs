using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreateArrow : MonoBehaviour
{
    [SerializeField] float depth = 0.2f;
    MeshFilter arrowMesh;
    // Start is called before the first frame update
    void Awake()
    {
        arrowMesh = GetComponent<MeshFilter>();
    }

    private void Start()
    {
        //MakeArrow();
    }

    public void MakeArrow()
    {
        // 새로운 메시 생성
        Mesh mesh = new Mesh();

        // 화살표의 앞면 꼭지점 정의
        // Z+ 방향을 향한 평면상의 2D 화살표 형태 (depth는 앞으로 튀어나온 깊이값)
        Vector3[] front = new Vector3[]
        {
            new Vector3(-1f, 0.5f, depth * 0.5f),   // 0: 왼쪽 위
            new Vector3(0.3f, 0.5f, depth * 0.5f),  // 1: 몸통 상단
            new Vector3(0.3f, 1f, depth * 0.5f),    // 2: 머리 위 꼭지점
            new Vector3(1f, 0f, depth * 0.5f),      // 3: 화살 끝
            new Vector3(0.3f, -1f, depth * 0.5f),   // 4: 머리 아래 꼭짓점
            new Vector3(0.3f, -0.5f, depth * 0.5f), // 5: 몸통 하단
            new Vector3(-1f, -0.5f, depth * 0.5f)   // 6: 왼쪽 아래
        };

        // 뒷면 꼭지점 정의 (앞면과 같은 x,y 좌표지만 z방향 반대)
        Vector3[] back = new Vector3[front.Length];

        for (int i = 0; i < front.Length; i++)
        {
            Vector3 v = front[i];
            // Z- 방향
            back[i] = new Vector3(v.x, v.y, -depth * 0.5f);
        }

        // 앞면 + 뒷면 정점들을 하나의 배열로 합치기
        Vector3[] vertices = new Vector3[front.Length + back.Length];

        for(int i=0; i<front.Length; i++)
        {
            vertices[i] = front[i]; // 앞면
            vertices[i + front.Length] = back[i]; // 뒷면
        }

        // 삼각형 인덱스 생성
        List<int> triangles = new List<int>();

        // 앞면 그리기 (시계 방향)
        triangles.Add(0); triangles.Add(1); triangles.Add(6);
        triangles.Add(1); triangles.Add(5); triangles.Add(6);
        triangles.Add(1); triangles.Add(2); triangles.Add(5);
        triangles.Add(2); triangles.Add(4); triangles.Add(5);
        triangles.Add(2); triangles.Add(3); triangles.Add(4);

        int offset = front.Length;  // 뒷면 정점 인덱스 보정값

        // 뒷면 그리기 (반시계 방향 - 뒤집힌 방향)
        triangles.Add(6 + offset); triangles.Add(1 + offset); triangles.Add(0 + offset);
        triangles.Add(6 + offset); triangles.Add(5 + offset); triangles.Add(1 + offset);
        triangles.Add(5 + offset); triangles.Add(2 + offset); triangles.Add(1 + offset);
        triangles.Add(5 + offset); triangles.Add(4 + offset); triangles.Add(2 + offset);
        triangles.Add(4 + offset); triangles.Add(3 + offset); triangles.Add(2 + offset);

        // 측면 구성
        // 앞면과 뒷면을 연결하여 입체(두께감)를 구성
        for(int i=0;i<front.Length; i++)
        {
            int next = (i + 1) % 7;
            if (next >= front.Length)
                continue;
            //AddQuad(triangles, i, next, next + front.Length, i + front.Length);

            // f0, f1 : 앞면 정점 인덱스
            // b0, b1 : 뒷면 정점 인덱스
            int f0 = i;
            int f1 = next;
            int b0 = i + offset;
            int b1 = next + offset;

            // 사이드 면 두개의 삼각형으로 구성 (앞면 -> 뒷면 연결)
            triangles.Add(f0); triangles.Add(f1); triangles.Add(b1);
            triangles.Add(f0); triangles.Add(b1); triangles.Add(b0);
        }

        // 메시 구성 및 적용
        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        // 조명 처리를 위한 법선 계산
        mesh.RecalculateNormals();

        // 메쉬 필드에 적용
        arrowMesh.mesh = mesh;
    }
}
