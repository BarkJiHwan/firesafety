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
        Mesh mesh = new Mesh();

        Vector3[] front = new Vector3[]
        {
            new Vector3(-1f, 0.5f, depth * 0.5f),  
            new Vector3(0.3f, 0.5f, depth * 0.5f),
            new Vector3(0.3f, 1f, depth * 0.5f), 
            new Vector3(1f, 0f, depth * 0.5f),    
            new Vector3(0.3f, -1f, depth * 0.5f), 
            new Vector3(0.3f, -0.5f, depth * 0.5f),
            new Vector3(-1f, -0.5f, depth * 0.5f)
        };

        Vector3[] back = new Vector3[front.Length];

        for (int i = 0; i < front.Length; i++)
        {
            Vector3 v = front[i];
            back[i] = new Vector3(v.x, v.y, -depth * 0.5f);
        }

        Vector3[] vertices = new Vector3[front.Length + back.Length];

        for(int i=0; i<front.Length; i++)
        {
            vertices[i] = front[i];
            vertices[i + front.Length] = back[i];
        }

        List<int> triangles = new List<int>();

        // 앞면
        triangles.Add(0); triangles.Add(1); triangles.Add(6);
        triangles.Add(1); triangles.Add(5); triangles.Add(6);
        triangles.Add(1); triangles.Add(2); triangles.Add(5);
        triangles.Add(2); triangles.Add(4); triangles.Add(5);
        triangles.Add(2); triangles.Add(3); triangles.Add(4);

        int offset = front.Length;
        // 뒷면
        triangles.Add(6 + offset); triangles.Add(1 + offset); triangles.Add(0 + offset);
        triangles.Add(6 + offset); triangles.Add(5 + offset); triangles.Add(1 + offset);
        triangles.Add(5 + offset); triangles.Add(2 + offset); triangles.Add(1 + offset);
        triangles.Add(5 + offset); triangles.Add(4 + offset); triangles.Add(2 + offset);
        triangles.Add(4 + offset); triangles.Add(3 + offset); triangles.Add(2 + offset);

        for(int i=0;i<front.Length; i++)
        {
            int next = (i + 1) % 7;
            if (next >= front.Length)
                continue;
            //AddQuad(triangles, i, next, next + front.Length, i + front.Length);
            int f0 = i;
            int f1 = next;
            int b0 = i + offset;
            int b1 = next + offset;

            triangles.Add(f0); triangles.Add(f1); triangles.Add(b1);
            triangles.Add(f0); triangles.Add(b1); triangles.Add(b0);
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles.ToArray();
        mesh.RecalculateNormals();

        arrowMesh.mesh = mesh;
    }
}
