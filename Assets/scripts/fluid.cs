using UnityEngine;
using UnityEngine.Rendering;

public class fluid : MonoBehaviour
{
    [Header("drawing")]
    public Material baseUnlit;

    public float radius;
    public int segments;
    public Color fluidColor;

    public float containerThickness;
    public Color containerColor;

    [Header("fluid")]
    public float gravity;
    Vector2 velocity;
    Vector2 position;

    [Header("container")]
    public Vector2 boxSize;

    private void Update()
    {
        velocity += new Vector2(0, -gravity) * Time.deltaTime;
        position += velocity * Time.deltaTime;
        circle(position);
        drawContainer();
    }
    void circle(Vector2 pos)
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

        Material waterMat = new Material(baseUnlit);
        waterMat.color = fluidColor;
        Graphics.DrawMesh(mesh, pos, Quaternion.identity, waterMat, 0, Camera.main);
    }
    void drawContainer()
    {
        Mesh mesh = new Mesh();

        Vector2 outerHalf = (boxSize + (Vector2.one *containerThickness)) * 0.5f;
        Vector2 innerHalf = boxSize * 0.5f;
        Vector3[] verts = new Vector3[]
        {
            // Outer ring
            new Vector3(-outerHalf.x, -outerHalf.y, 0),
            new Vector3( outerHalf.x, -outerHalf.y, 0),
            new Vector3( outerHalf.x,  outerHalf.y, 0),
            new Vector3(-outerHalf.x,  outerHalf.y, 0),

            // Inner ring
            new Vector3(-innerHalf.x, -innerHalf.y, 0),
            new Vector3( innerHalf.x, -innerHalf.y, 0),
            new Vector3( innerHalf.x,  innerHalf.y, 0),
            new Vector3(-innerHalf.x,  innerHalf.y, 0),
        };
        int[] tris = new int[]
        {
            0, 5, 1,  0, 4, 5, // Bottom side
            1, 6, 2,  1, 5, 6, // Right side
            2, 7, 3,  2, 6, 7, // Top side
            3, 4, 0,  3, 7, 4, // Left side
        };

        mesh.vertices = verts;
        mesh.triangles = tris;

        Material containerMat = new Material(baseUnlit);
        containerMat.color = containerColor;
        Graphics.DrawMesh(mesh, Vector2.zero, Quaternion.identity, containerMat, 0, Camera.main);
    }

    bool collision(Vector2 pos)
    {
        Vector2 half = boxSize / 2;

        return Mathf.Abs(pos.x) <= half.x || Mathf.Abs(pos.y) <= half.y;
    }
}
