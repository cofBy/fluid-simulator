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
    RenderParams rp;

    [Header("computing")]
    public ComputeShader computeShader;
    ComputeBuffer particlesBuffer;

    int kernalID;
    int groupSizeX;

    private void Start()
    {
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

        computeShader.SetBuffer(kernalID, "particlesBuffer", particlesBuffer);
        mat.SetBuffer("particlesBuffer", particlesBuffer);

        rp = new RenderParams();
        rp.worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000);
    }

    private void Update()
    {
        if (particlesBuffer == null) return;
        Vector2 worldCursor = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        computeShader.SetFloat("dt", Time.deltaTime);
        computeShader.SetFloats("mousePos", new float[2]{ worldCursor.x, worldCursor.y});
        computeShader.SetFloats("spawnerPos", new float[2]{ spawner.position.x, spawner.position.y});
        computeShader.Dispatch(kernalID, groupSizeX, 1, 1);

        Graphics.RenderPrimitives(rp, MeshTopology.Points, 1, particleAmount);
    }

    private void OnDestroy()
    {
        if (particlesBuffer == null) return;
        particlesBuffer.Release();
        particlesBuffer = null;
    }
}
