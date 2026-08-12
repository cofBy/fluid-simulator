using UnityEngine;

public class particleFun : MonoBehaviour
{
    [Header("storing particles")]
    public Transform spawner;
    public int particleAmount;
    particle[] particles;
    struct particle
    {
        public Vector2 pos;
        public Vector2 vel;
        public float life;

        public particle(Vector2 newPos, Vector2 newVel, float newLife)
        {
            pos = newPos;
            vel = newVel;
            life = newLife;
        }
    }

    [Header("rendering")]
    public Material mat;
    public Vector2Int res;
    [Range(0, 1)] public float thickness;
    RenderTexture densityMap;
    Camera cam;

    [Header("computing")]
    public ComputeShader computeShader;
    ComputeBuffer particlesBuffer;

    int kernalID;
    int groupSizeX;

    int clearKernalID;
    int clearGroupSizeX;
    int clearGroupSizeY;

    private void Start()
    {
        cam = Camera.main;
        particles = new particle[particleAmount];
        for (int i = 0; i < particleAmount; i++)
        {
            particles[i] = new particle(spawner.position, Vector2.zero, (float)i / particleAmount * 5f);
        }

        particlesBuffer = new ComputeBuffer(particleAmount, 5 * sizeof(float));
        particlesBuffer.SetData(particles);

        kernalID = computeShader.FindKernel("CSParticle");
        uint threadx;
        computeShader.GetKernelThreadGroupSizes(kernalID, out threadx, out _, out _);
        groupSizeX = Mathf.CeilToInt(particleAmount / (float)threadx);

        clearKernalID = computeShader.FindKernel("CSClear");
        uint clearThreadSize;
        computeShader.GetKernelThreadGroupSizes(clearKernalID, out clearThreadSize, out _, out _);
        clearGroupSizeX = Mathf.CeilToInt(res.x / (float)clearThreadSize);
        clearGroupSizeY = Mathf.CeilToInt(res.y / (float)clearThreadSize);

        computeShader.SetBuffer(kernalID, "particlesBuffer", particlesBuffer);
        mat.SetBuffer("particlesBuffer", particlesBuffer);

        computeShader.SetFloats("bounds", new float[2] {cam.orthographicSize * ((float)Screen.width / Screen.height), cam.orthographicSize});
        computeShader.SetFloats("res", new float[2] {res.x, res.y});
        computeShader.SetFloat("thickness", thickness);
        computeShader.SetInt("particleCount", particleAmount);
        mat.SetFloat("aspectRatio", (float)Screen.width / Screen.height);

        densityMap = new RenderTexture(res.x, res.y, 0, RenderTextureFormat.RFloat);
        densityMap.enableRandomWrite = true;
        densityMap.Create();
        computeShader.SetTexture(kernalID, "densityMap", densityMap);
        computeShader.SetTexture(clearKernalID, "densityMap", densityMap);
        mat.SetTexture("_BaseMap", densityMap);
    }

    private void Update()
    {
        if (particlesBuffer == null) return;
        Vector2 worldCursor = cam.ScreenToWorldPoint(Input.mousePosition);
        computeShader.SetFloat("dt", Time.deltaTime);
        computeShader.SetFloats("mousePos", new float[2]{ worldCursor.x, worldCursor.y});
        computeShader.SetFloats("spawnerPos", new float[2]{ spawner.position.x, spawner.position.y});

        computeShader.Dispatch(clearKernalID, clearGroupSizeX, clearGroupSizeY, 1);
        computeShader.Dispatch(kernalID, groupSizeX, 1, 1);
    }

    private void OnDestroy()
    {
        if (particlesBuffer == null) return;
        particlesBuffer.Release();
        particlesBuffer = null;
    }
}
