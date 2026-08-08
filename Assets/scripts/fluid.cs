using UnityEngine;

public class fluid : MonoBehaviour
{
    [Header("drawing")]
    public float radius;
    public int segments;
    public Material waterMat;

    private void Update()
    {
        circle(Vector2.zero);
    }
    void circle(Vector2 position)
    {
        Mesh mesh = new Mesh();
        Vector3[] verts = new Vector3[segments + 1];
        int[] tris = new int[segments * 3];

        verts[0] = Vector2.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            verts[i + 1] = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * radius;

            int t = i * 3;
            tris[t + 0] = 0;
            tris[t + 1] = (i + 1);
            tris[t + 2] = (i + 1) % segments + 1;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;

        Graphics.DrawMesh(mesh, position, Quaternion.identity, waterMat, 0, Camera.main);
    }
}
