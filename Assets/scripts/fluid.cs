using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class fluid : MonoBehaviour
{
    [Header("drawing")]
    public Material baseUnlit;

    public float particleRadius;
    public int segments;
    public Gradient fluidColor;
    Mesh fluidMesh;
    Material fluidMat;
    MaterialPropertyBlock mpb;

    public float containerThickness;
    public Color containerColor;
    Mesh containerMesh;
    Material containerMat;

    [Header("fluid")]
    public Vector2Int rowsCols;
    int particlesAmount;
    public float spacing;

    Vector2[] nextPositions;
    Vector2[] positions;
    Vector2[] velocitys;
    float[] densities;

    public float pressureRadius;
    public float mass;
    public float targetDensity;
    public float pressureMultiplier;
    public float damping;
    public float viscosityStrength;

    public float gravity;
    public float elasticity;

    [Header("spatial partitioning")]
    entry[] cells;
    int[] firstIndices;
    List<int>[] neighborBuffers;

    struct entry : IComparable<entry>
    {
        public int index;
        public uint key;

        public entry(int index, uint key)
        {
            this.index = index;
            this.key = key;
        }
        public int CompareTo(entry other)
        {
            return key.CompareTo(other.key);
        }
    }

    [Header("container")]
    public Vector2 boxSize;

    [Header("interacting")]
    public float interactionForceRadius;
    public float interactionForce;

    private void Awake()
    {
        particlesAmount = rowsCols.x * rowsCols.y;
        positions = new Vector2[particlesAmount];
        nextPositions = new Vector2[particlesAmount];
        velocitys = new Vector2[particlesAmount];
        densities = new float[particlesAmount];

        cells = new entry[particlesAmount];
        firstIndices = new int[particlesAmount];

        neighborBuffers = new List<int>[particlesAmount];
        for (int i = 0; i < particlesAmount; i++)
        {
            neighborBuffers[i] = new List<int>();
        }

        float space = particlesAmount * 2 + spacing;
        for (int i = 0; i < particlesAmount; i++)
        {
            float x = (i % rowsCols.x - rowsCols.x * 0.5f + 0.5f) * spacing;
            float y = (i / rowsCols.x - rowsCols.y * 0.5f + 0.5f) * spacing;
            positions[i] = new Vector2(x, y);
        }

        mpb = new MaterialPropertyBlock();
        constructCircle();
        constructContainer();
    }
    private void Update()
    {
        simulate(Time.deltaTime);

        float maxVel = 0;
        for (int i = 0; i < particlesAmount; i++)
        {
            float vel = velocitys[i].magnitude;
            if (vel > maxVel) maxVel = Mathf.Max(vel, 0.01f);
        }

        for (int i = 0; i < particlesAmount; i++)
        {
            mpb.SetColor("_BaseColor", fluidColor.Evaluate(Mathf.Clamp01(velocitys[i].magnitude / maxVel)));
            Graphics.DrawMesh(fluidMesh, positions[i], Quaternion.identity, fluidMat, 0, Camera.main, 0, mpb);
        }
        Graphics.DrawMesh(containerMesh, Vector2.zero, Quaternion.identity, containerMat, 0, Camera.main);
    }
    void simulate(float dt)
    {
        float mouseInput = input(KeyCode.Mouse0, KeyCode.Mouse1);
        Vector2 mousePos = mouseInput != 0 ? (Vector2)Camera.main.ScreenToWorldPoint(Input.mousePosition) : Vector2.zero;

        Parallel.For(0, particlesAmount, i =>
        {
            velocitys[i] += new Vector2(0, -gravity) * dt;
            nextPositions[i] = positions[i] + velocitys[i] / 120f;

            if (mouseInput != 0 && Vector2.Distance(mousePos, nextPositions[i]) < interactionForceRadius)
            {
                Vector2 dir = (nextPositions[i] - mousePos).normalized;
                velocitys[i] += dir * mouseInput * interactionForce;
            }
        });

        updateCells(nextPositions);

        Parallel.For(0, particlesAmount, i =>
        {
            cellsInRadius(nextPositions[i], neighborBuffers[i]);
            densities[i] = calculateDensity(i);
        });

        Parallel.For(0, particlesAmount, i =>
        {
            Vector2 pressureAcc = pressureGradiant(i) / densities[i];
            velocitys[i] += pressureAcc * dt;
        });

        Parallel.For(0, particlesAmount, i =>
        {
            Vector2 viscosityAcc = viscosityGradiant(i) / densities[i];
            velocitys[i] += viscosityAcc * dt;
        });

        Parallel.For(0, particlesAmount, i =>
        {
            positions[i] += velocitys[i] * dt;
            collision(i);

            Vector2 v = velocitys[i];
            float dampX = Mathf.Min(Mathf.Abs(v.x), damping * dt) * Mathf.Sign(v.x);
            float dampY = Mathf.Min(Mathf.Abs(v.y), damping * dt) * Mathf.Sign(v.y);
            velocitys[i] -= new Vector2(dampX, dampY);
        });
    }
    float input(KeyCode posKey, KeyCode negKey)
    {
        float x = 0;
        if (Input.GetKey(posKey))
        {
            x += 1;
        }
        if (Input.GetKey(negKey))
        {
            x -= 1;
        }
        return x;
    }

    float calculateDensity(int index)
    {
        float density = 0;
        Vector2 pos = nextPositions[index];
        List<int> neighbors = neighborBuffers[index];

        for (int n = 0; n < neighbors.Count; n++)
        {
            Vector2 otherPos = nextPositions[neighbors[n]];
            float distance = Vector2.Distance(otherPos, pos);
            density += mass * smoothing(distance, pressureRadius);
        }
        return Mathf.Max(density, 0.001f);
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
        fluidMat.color = fluidColor.Evaluate(0);
    }
    void constructContainer()
    {
        containerMesh = new Mesh();
        Vector2 outerHalf = (boxSize + (Vector2.one * containerThickness)) * 0.5f;
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
            velocitys[index].x *= -1 * elasticity;
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
        List<int> neighbors = neighborBuffers[index];

        for (int n = 0; n < neighbors.Count; n++)
        {
            int i = neighbors[n];
            if (i == index) continue;
            Vector2 dir = positions[i] - positions[index];
            float avgPressure = (calculatePressure(densities[i]) + calculatePressure(densities[index])) / 2;
            gradiant += avgPressure * dir.normalized * smoothingDer(dir.magnitude, pressureRadius) * mass / densities[i];
        }
        return gradiant;
    }
    Vector2 viscosityGradiant(int index)
    {
        Vector2 gradiant = Vector2.zero;
        List<int> neighbors = neighborBuffers[index];

        for (int n = 0; n < neighbors.Count; n++)
        {
            int i = neighbors[n];
            if (i == index) continue;
            Vector2 dir = positions[i] - positions[index];
            gradiant += dir * smoothing(dir.magnitude, pressureRadius);
        }
        return gradiant * viscosityStrength;
    }

    void updateCells(Vector2[] points)
    {
        Parallel.For(0, points.Length, i =>
        {
            (int x, int y) = cellCoord(points[i]);
            uint cellKey = keyCell(hashCell(x, y));
            cells[i] = new entry(i, cellKey);
            firstIndices[i] = int.MaxValue;
        });

        Array.Sort(cells);

        Parallel.For(0, points.Length, i =>
        {
            uint key = cells[i].key;
            uint keyPrev = i == 0 ? uint.MaxValue : cells[i - 1].key;
            if (key != keyPrev) firstIndices[key] = i;
        });
    }

    void cellsInRadius(Vector2 point, List<int> result)
    {
        result.Clear();

        (int, int)[] offsetCells =
        {
            cellCoord(point),
            cellCoord(point + new Vector2(+pressureRadius, 0)),
            cellCoord(point + new Vector2(-pressureRadius, 0)),
            cellCoord(point + new Vector2(0, +pressureRadius)),
            cellCoord(point + new Vector2(0, -pressureRadius)),
            cellCoord(point + new Vector2(+pressureRadius, +pressureRadius)),
            cellCoord(point + new Vector2(+pressureRadius, -pressureRadius)),
            cellCoord(point + new Vector2(-pressureRadius, +pressureRadius)),
            cellCoord(point + new Vector2(-pressureRadius, -pressureRadius))
        };

        foreach ((int x, int y) in offsetCells)
        {
            uint key = keyCell(hashCell(x, y));
            int cellFirstIndex = firstIndices[key];

            if (cellFirstIndex == int.MaxValue) continue;

            for (int i = cellFirstIndex; i < cells.Length; i++)
            {
                if (cells[i].key != key) break;

                int particleIndex = cells[i].index;

                if (Vector2.Distance(nextPositions[particleIndex], point) < pressureRadius)
                {
                    result.Add(particleIndex);
                }
            }
        }
    }
    (int x, int y) cellCoord(Vector2 point)
    {
        Vector2 cell = (point / pressureRadius);
        return (Mathf.FloorToInt(cell.x), Mathf.FloorToInt(cell.y));
    }
    uint hashCell(int cellx, int celly)
    {
        uint a = (uint)cellx * 15823;
        uint b = (uint)celly * 9737333;
        return a + b;
    }
    uint keyCell(uint hash)
    {
        return hash % (uint)cells.Length;
    }
}