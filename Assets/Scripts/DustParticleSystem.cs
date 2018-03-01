/* 
Description:
    GPU Particles using compute shaders in Unity.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dust 
{
    public class DustParticleSystem : MonoBehaviour
    {
        #region Public Properties
        public Mesh ParticlesMesh 
        {
            get 
            {
                return m_particlesMesh;
            }
        }
        public ComputeShader ParticleSystemKernel;
        public Material ParticleMaterial;
        
        [Space(10)]
        [Header("Particles")]
        public Vector2 Mass = new Vector2(0.5f, 0.5f);
        public Vector2 Momentum = new Vector2(0.95f, 0.95f);
        public Vector2 Lifespan = new Vector2(5f, 5f);
        public int PreWarmFrames = 0;

        [Header("Velocity")]
        public float InheritVelocity = 0f;
        public int EmitterVelocity = 0;
        public float GravityModifier = 0f;

        [Header("Shape")]
        public int Shape = 0;
        [Range(0,m_maxVertCount)]
        public int Emission = 65000;
        public float InitialSpeed = 0f;
        public Vector3 EmissionSize = new Vector3(1f,1f,1f);
        [Range(0,1)]
        public float ScatterVolume = 0f;
        public MeshRenderer EmissionMeshRenderer;

        [Header("Color")]
        [ColorUsageAttribute(true,true,0,8,.125f,3)]
        public Color StartColor = new Color(1f,1f,1f,1f);
        public ColorRamp ColorByLife;
        public ColorRampRange ColorByVelocity;

        [Header("Noise")]
        public int NoiseType = 1;
        public Vector3 NoiseAmplitude = new Vector3(0f,0f,0f);
        public Vector3 NoiseScale = new Vector3(1f,1f,1f);
        public Vector4 NoiseOffset = new Vector4(0f,0f,0f,0f);
        public Vector4 NoiseOffsetSpeed = new Vector4(0f,0f,0f,0f);
        #endregion
        
        #region Private Properties
        private int m_kernel;
        private Mesh m_particlesMesh;
        private ComputeBuffer m_particlesBuffer;
        private ComputeBuffer m_kernelArgs;
        private int[] m_kernelArgsLocal;
    
        private Vector3 m_origin;
        private Vector3 m_initialVelocityDir;
        private Vector3 m_prevPos;

        private DustMeshEmitter m_meshEmitter;
        
        private const int m_maxVertCount = 1048576; //64*64*16*16 (Groups*ThreadsPerGroup)
        #endregion

        //We initialize the buffers and the material used to draw.
        void Start()
        {
            m_kernel = ParticleSystemKernel.FindKernel("DustParticleSystemKernel");
            CreateBuffers();            
            UpdateComputeUniforms();

            // Prewarm the system
            if (PreWarmFrames > 0) {
                for (int i = 0; i < PreWarmFrames; i++) {
                    Dispatch();
                }
            }
        }

        void FixedUpdate() 
        {
            UpdateComputeUniforms();
            Dispatch();
        }

        void Update() 
        {
            ParticleMaterial.SetBuffer("dataBuffer", m_particlesBuffer);
            ParticleMaterial.SetInt("numParticles", Emission);
            // ParticleMaterial.SetPass(0);
            Graphics.DrawMesh(m_particlesMesh, transform.localToWorldMatrix, ParticleMaterial, 0, null, 0, null, true, true);
        }

        void OnDisable()
        {
            ReleaseBuffers();
        }

        private void Dispatch()
        {
            ParticleSystemKernel.DispatchIndirect(m_kernel, m_kernelArgs);
        }

        private void UpdateComputeUniforms() 
        {
            // Update internal variables
            m_prevPos = transform.position;

            // Follow mouse cursor
            if (Input.GetMouseButton(0)){
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                Vector3 o = ray.origin + (ray.direction * 20f);
                transform.position = o;
            }

            // Handle where to take the initial velocity direction from
            // 0 = Rigidbody, 1 = Transform
            switch(EmitterVelocity) {
                case 0:
                    if (transform.parent != null) {
                        if (transform.parent.gameObject.GetComponent<Rigidbody>() != null) {
                            m_initialVelocityDir = transform.parent.gameObject.GetComponent<Rigidbody>().velocity;
                        }
                    }
                    else {
                        m_initialVelocityDir = Vector3.zero;
                    }
                    break;
                case 1:
                    m_initialVelocityDir = transform.position-m_prevPos;
                    break;
            }

            m_origin = transform.position;

            ParticleSystemKernel.SetFloat("dt", Time.fixedDeltaTime);
            ParticleSystemKernel.SetFloat("fixedTime", Time.fixedTime);
            ParticleSystemKernel.SetVector("origin", m_origin);
            ParticleSystemKernel.SetVector("massNew", Mass);
            ParticleSystemKernel.SetVector("momentumNew", Momentum);
            ParticleSystemKernel.SetVector("lifespanNew", Lifespan);
            ParticleSystemKernel.SetFloat("inheritVelocityMult", InheritVelocity);
            ParticleSystemKernel.SetVector("initialVelocityDir", m_initialVelocityDir);
            ParticleSystemKernel.SetVector("gravityIn", Physics.gravity);
            ParticleSystemKernel.SetFloat("gravityModifier", GravityModifier);
            ParticleSystemKernel.SetInt("emissionShape", Shape);
            ParticleSystemKernel.SetInt("emission", Emission);
            ParticleSystemKernel.SetVector("emissionSize", EmissionSize);
            ParticleSystemKernel.SetFloat("initialSpeed", InitialSpeed);
            ParticleSystemKernel.SetFloat("scatterVolume", ScatterVolume);
            ParticleSystemKernel.SetVector("startColor", StartColor);
            ParticleSystemKernel.SetFloat("velocityColorRange", ColorByVelocity.Range);
            ParticleSystemKernel.SetInt("noiseType", NoiseType);
            ParticleSystemKernel.SetVector("noiseAmplitude", NoiseAmplitude);
            ParticleSystemKernel.SetVector("noiseScale", NoiseScale);
            ParticleSystemKernel.SetVector("noiseOffset", NoiseOffset);
            ParticleSystemKernel.SetVector("noiseOffsetSpeed", NoiseOffsetSpeed);
            if (EmissionMeshRenderer) {
                ParticleSystemKernel.SetMatrix("emissionMeshMatrix", m_meshEmitter.MeshRenderer.localToWorldMatrix);
                ParticleSystemKernel.SetInt("emissionMeshVertCount", m_meshEmitter.VertexCount);
                ParticleSystemKernel.SetInt("emissionMeshTrisCount", m_meshEmitter.TriangleCount);
            }
        }


        // Create and initialize compute shader buffers
        private void CreateBuffers()
        {
            // Allocate
            int numElements = 14; //float4 cd; float3 pos, vel; float age, lifespan, mass, momentum
            m_kernelArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
            m_particlesBuffer = new ComputeBuffer(m_maxVertCount, sizeof(float) * numElements); //float3 pos, vel, cd; float age

            m_particlesMesh = new Mesh();
            m_kernelArgsLocal = new int[3];
            float[] particlesTemp = new float[m_maxVertCount * numElements];
            Vector3 [] meshVerts = new Vector3[m_maxVertCount];
            int [] meshIndices = new int[m_maxVertCount];

            UpdateKernelArgs();

            ParticleSystemKernel.SetBuffer(m_kernel, "kernelArgs", m_kernelArgs);

            for (int i = 0; i < m_maxVertCount; i++) {
                meshVerts[i] = Random.insideUnitSphere * 50f;
                meshIndices[i] = i;
                for (int j = 0; j < numElements; j++) {
                    particlesTemp[(i*numElements)+j] = 0f;
                }
            }

            m_particlesBuffer.SetData(particlesTemp);
            ParticleSystemKernel.SetBuffer(m_kernel, "output", m_particlesBuffer);

            // Dummy mesh geometry for points
            m_particlesMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            m_particlesMesh.vertices = meshVerts;
            m_particlesMesh.SetIndices(meshIndices, MeshTopology.Points, 0);
            m_particlesMesh.RecalculateBounds();
            MeshFilter mf = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            mf.hideFlags = HideFlags.HideInInspector;
            mf.mesh = m_particlesMesh;

            // Create color ramp textures
            ColorByLife.Setup();
            ColorByVelocity.Setup();
            ParticleSystemKernel.SetTexture(m_kernel, "_colorByLife", (Texture)ColorByLife.Texture);
            ParticleSystemKernel.SetTexture(m_kernel, "_colorByVelocity", (Texture)ColorByVelocity.Texture);

            // Set up mesh emitter
            if (EmissionMeshRenderer) {
                m_meshEmitter = new DustMeshEmitter(EmissionMeshRenderer);
                m_meshEmitter.Update();
                
                ParticleSystemKernel.SetBuffer(m_kernel, "emissionMesh", m_meshEmitter.MeshBuffer);
                ParticleSystemKernel.SetBuffer(m_kernel, "emissionMeshTris", m_meshEmitter.MeshTrisBuffer);
            }

        }

        public void UpdateKernelArgs()
        {
            int groupSize = (int)Mathf.Ceil(Mathf.Sqrt(Emission)/16f);

            m_kernelArgsLocal[0] = groupSize;
            m_kernelArgsLocal[1] = groupSize;
            m_kernelArgsLocal[2] = 1;

            m_kernelArgs.SetData(m_kernelArgsLocal);
        }

        private void ReleaseBuffers()
        {
            m_particlesBuffer.Release();
            m_kernelArgs.Release();
            if (EmissionMeshRenderer) {
                m_meshEmitter.ReleaseBuffers();
            }
        }

    }
}