using UnityEngine;

public class fluid : MonoBehaviour
{
    [Header("drawing")]
    public Material baseUnlit;

    public float particleRadius;
    public int segments;
    public Color fluidColor;
    Mesh fluidMesh;
    Material fluidMat;

    public float containerThickness;
    public Color containerColor;
    Mesh containerMesh;
    Material containerMat;

    [Header("fluid")]
    public int particlesAmount;
    public float spacing;
    Vector2[] positions;
    Vector2[] velocitys;
    float[] densities;

    public float dampingRadius;
    public float mass;
    public float targetDensity;
    public float pressureMultiplier;

    public float gravity;
    public float elasticity;

    [Header("container")]
    public Vector2 boxSize;

    private void Awake()
    {
        positions = new Vector2[particlesAmount];
        velocitys = new Vector2[particlesAmount];
        densities = new float[particlesAmount];
        
        int rows = (int)Mathf.Sqrt(particlesAmount);
        int cols = (particlesAmount + rows - 1) / rows;
        float space = particlesAmount * 2 + spacing;
        for (int i = 0; i < particlesAmount; i++)
        {
            float x = (i % rows - rows / 2f + 0.5f) * spacing;
            float y = (i / rows - cols / 2f + 0.5f) * spacing;
            positions[i] = new Vector2(x, y);
        }

        constructCircle();
        constructContainer();
    }
    private void Update()
    {
        for (int i = 0; i < particlesAmount; i++)
        {
            velocitys[i] += new Vector2(0, -gravity) * Time.deltaTime;
            densities[i] = calculateDensity(positions[i]);
        }
        for (int i = 0; i < particlesAmount; i++)   
        {
            Vector2 pressureAcc = pressureGradiant(i) / densities[i];
            velocitys[i] += pressureAcc * Time.deltaTime;
        }
        for (int i = 0; i < particlesAmount; i++)
        {
            positions[i] += velocitys[i] * Time.deltaTime;
            Debug.DrawRay(positions[i], velocitys[i]);
            collision(i);
            Graphics.DrawMesh(fluidMesh, positions[i], Quaternion.identity, fluidMat, 0, Camera.main);
        }
        Graphics.DrawMesh(containerMesh, Vector2.zero, Quaternion.identity, containerMat, 0, Camera.main);
    }

    float calculateDensity(Vector2 pos)
    {
        float density = 0;
        foreach (Vector2 otherPos in positions)
        {
            float distance = Vector2.Distance(otherPos, pos);
            density += mass * smoothing(distance, dampingRadius);
        }
        return density;
    }
    void constructCircle()
    {
        fluidMesh = new Mesh();
        Vector3[] verts = new Vector3[segments + 1];
        int[] tris = new int[segments * 3];
        verts[0] = Vector2.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = (float)i / segments * Mathf.PI * 2;
            verts[i + 1] = new Vector3(Mathf.Sin(angle), Mathf.Cos(angle)) * particleRadius;
            int t = i * 3;
            tris[t + 0] = 0;
            tris[t + 1] = (i + 1);
            tris[t + 2] = (i + 1) % segments + 1;
        }
        fluidMesh.vertices = verts;
        fluidMesh.triangles = tris;
        fluidMat = new Material(baseUnlit);
        fluidMat.color = fluidColor;
    }
    void constructContainer()
    {
        containerMesh = new Mesh();
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

        containerMesh.vertices = verts;
        containerMesh.triangles = tris;

        containerMat = new Material(baseUnlit);
        containerMat.color = containerColor;
    }

    void collision(int index)
    {
        Vector2 half = boxSize / 2;

        if (Mathf.Abs(positions[index].x) >= half.x - particleRadius)
        {
            positions[index].x = (half.x - particleRadius) * Mathf.Sign(positions[index].x);
            velocitys[index].y *= -1 * elasticity;
        }
        if (Mathf.Abs(positions[index].y) >= half.y - particleRadius)
        {
            positions[index].y = (half.y - particleRadius) * Mathf.Sign(positions[index].y);
            velocitys[index].y *= -1 * elasticity;
        }
    }
    float smoothing(float distance, float radius)
    {
        if (distance >= radius) return 0;

        float volume = Mathf.PI * Mathf.Pow(radius, 4) / 6;
        return (radius - distance) * (radius - distance) / volume;
    }
    float smoothingDer(float distance, float radius)
    {
        if (distance >= radius) return 0;

        float scale = 12 / (Mathf.Pow(radius, 4) * Mathf.PI);
        return (distance - radius) * scale;
    }
    float calculatePressure(float density)
    {
        float densityError = density - targetDensity;
        return densityError * pressureMultiplier;
    }

    Vector2 pressureGradiant(int index)
    {
        Vector2 gradiant = Vector2.zero;

        for (int i = 0; i < particlesAmount; i++)
        {
            if (i == index) continue;
            Vector2 dir = positions[i] == positions[index] ? Random.insideUnitCircle.normalized : positions[i] - positions[index];
            float avgPressure = (calculatePressure(densities[i]) + calculatePressure(densities[index])) / 2;
            gradiant += avgPressure * dir.normalized * smoothingDer(dir.magnitude, dampingRadius) * mass / densities[i];
        }
        return gradiant;
    }
}
