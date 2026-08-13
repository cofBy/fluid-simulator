using UnityEngine;

public class particleFluid : MonoBehaviour
{
    [Header("spawning particles")]
    public Vector2Int rowsCols;
    int particleAmount;
    public float spacing;

    particle[] particles;
    struct particle
    {
        public Vector2 pos;
        public Vector2 nextPos;
        public Vector2 vel;
        public float density;
        public particle(Vector2 newPos, Vector2 newNextPos, Vector2 newVel, float newDensity)
        {
            pos = newPos;
            nextPos = newNextPos;
            vel = newVel;
            density = newDensity;
        }
    }

    [Header("fluid simulating")]
    public float pressureRadius;
    [Range(0, 1)] public float mass;
    public float targetDensity;
    public float pressureMultiplier;
    [Range(0, 1)] public float viscosityStrength;

    public float gravity;
    [Range(0, 1)] public float elasticity;

    public Vector2 bounds;

    [Header("interacting")]
    public float interactionForceRadius;
    public float interactionForce;

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

    private void Awake()
    {
        cam = Camera.main;
        particleAmount = rowsCols.x * rowsCols.y;
        particles = new particle[particleAmount];
        for (int i = 0; i < particleAmount; i++)
        {
            float x = (i % rowsCols.x - rowsCols.x * 0.5f + 0.5f) * spacing;
            float y = (i / rowsCols.x - rowsCols.y * 0.5f + 0.5f) * spacing;
            particles[i] = new particle(new Vector2(x, y), new Vector2(x, y), Vector2.zero, targetDensity > 0 ? targetDensity : 1f);
        }

        particlesBuffer = new ComputeBuffer(particleAmount, 7 * sizeof(float));
        particlesBuffer.SetData(particles);

        mat.SetBuffer("particlesBuffer", particlesBuffer);
        mat.SetFloat("aspectRatio", (float)Screen.width / Screen.height);

        densityMap = new RenderTexture(res.x, res.y, 0, RenderTextureFormat.RFloat);
        densityMap.enableRandomWrite = true;
        densityMap.filterMode = FilterMode.Point;
        densityMap.Create();
        mat.SetTexture("_BaseMap", densityMap);

        initShader();
    }
    void initShader()
    {
        kernalID = computeShader.FindKernel("CSParticle");
        clearKernalID = computeShader.FindKernel("CSClear");
        uint threadx;
        uint clearThreadSize;
        computeShader.GetKernelThreadGroupSizes(kernalID, out threadx, out _, out _);
        computeShader.GetKernelThreadGroupSizes(clearKernalID, out clearThreadSize, out _, out _);
        groupSizeX = Mathf.CeilToInt(particleAmount / (float)threadx);
        clearGroupSizeX = Mathf.CeilToInt(res.x / (float)clearThreadSize);
        clearGroupSizeY = Mathf.CeilToInt(res.y / (float)clearThreadSize);

        computeShader.SetTexture(kernalID, "densityMap", densityMap);
        computeShader.SetTexture(clearKernalID, "densityMap", densityMap);

        computeShader.SetBuffer(kernalID, "particlesBuffer", particlesBuffer);

        computeShader.SetFloats("screenSize", new float[2] { cam.orthographicSize * ((float)Screen.width / Screen.height), cam.orthographicSize });
        computeShader.SetFloats("bounds", new float[2] { bounds.x, bounds.y});
        computeShader.SetFloats("res", new float[2] { res.x, res.y });
        computeShader.SetFloat("thickness", thickness);
        computeShader.SetInt("particleCount", particleAmount);

        computeShader.SetFloat("interactionForceRadius", interactionForceRadius);
        computeShader.SetFloat("interactionForce", interactionForce);
        computeShader.SetFloat("pressureRadius", pressureRadius);
        computeShader.SetFloat("mass", mass);
        computeShader.SetFloat("targetDensity", targetDensity);
        computeShader.SetFloat("pressureMultiplier", pressureMultiplier);
        computeShader.SetFloat("viscosityStrength", viscosityStrength);
        computeShader.SetFloat("gravity", gravity);
        computeShader.SetFloat("elasticity", elasticity);
    }

    private void Update()
    {
        if (particlesBuffer == null) return;
        Vector2 worldCursor = cam.ScreenToWorldPoint(Input.mousePosition);

        computeShader.SetFloat("dt", Time.smoothDeltaTime);
        computeShader.SetInt("mouseInput", input(KeyCode.Mouse0, KeyCode.Mouse1));
        computeShader.SetFloats("mousePos", new float[2]{ worldCursor.x, worldCursor.y});
        computeShader.Dispatch(clearKernalID, clearGroupSizeX, clearGroupSizeY, 1);
        computeShader.Dispatch(kernalID, groupSizeX, 1, 1);
    }
    int input(KeyCode posKey, KeyCode negKey)
    {
        int x = 0;
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

    private void OnDestroy()
    {
        if (particlesBuffer == null) return;
        particlesBuffer.Release();
        particlesBuffer = null;
    }
}
