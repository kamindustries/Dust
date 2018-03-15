/* 
Description:
    GPU Particles using compute shaders in Unity.
*/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Dust 
{
    // Particle system buffer
    struct DustParticle
    {
        Vector3 pos;
        Vector3 vel;
        Color cd;
        Color startColor;
        float age;
        float lifespan;
        float mass;
        float momentum;
        Vector3 scale;
        Matrix4x4 rot;
        bool active;
    };

    public class DustParticleSystem : MonoBehaviour
    {
        #region Public Properties
        public ComputeShader Compute;

        [Header("Particles")]
        public Vector2 Mass = new Vector2(0.5f, 0.5f);
        public Vector2 Momentum = new Vector2(0.95f, 0.95f);
        public Vector2 Lifespan = new Vector2(.5f, 1f);
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
        // [Range(0,1)]
        public float Jitter = 0f;
        [Range(0,1)]
        public float RandomizeDirection = 0f;
        public Vector3 EmissionSize = new Vector3(1f,1f,1f);
        [Range(0,1)]
        public float ScatterVolume = 0f;
        public MeshRenderer EmissionMeshRenderer;

        [Header("Rotation")]
        public bool AlignToDirection = false;
        public float RotationOverLifetime = 0f;

        [Header("Color")]
        [ColorUsageAttribute(true,true,0,8,.125f,3)]
        public Color StartColor = new Color(1f,1f,1f,1f);
        public ColorRamp ColorByLife;
        public ColorRampRange ColorByVelocity;
        [Range(0,1)]
        public float RandomizeColor = 0f;
        public bool UseMeshEmitterColor = false;

        [Header("Noise")]
        public bool NoiseToggle = false;
        public int NoiseType = 1;
        public Vector3 NoiseAmplitude = new Vector3(0f,0f,0f);
        public Vector3 NoiseScale = new Vector3(1f,1f,1f);
        public Vector4 NoiseOffset = new Vector4(0f,0f,0f,0f);
        public Vector4 NoiseOffsetSpeed = new Vector4(0f,0f,0f,0f);
        #endregion

        #region Getters
        public ComputeBuffer ParticlesBuffer { get { return m_particlesBuffer; } }
        public int MaxVerts { get { return m_maxVertCount; } }
        #endregion
        
        #region Private Properties
        private int m_kernelSpawn;
        private int m_kernelUpdate;
        private ComputeBuffer m_particlesBuffer;
        private ComputeBuffer m_particlePoolBuffer;
        private ComputeBuffer m_particlePoolArgsBuffer;
        
        private ComputeBuffer m_kernelArgs;
        private int[] m_kernelArgsLocal = new int[3];
        private int[] m_poolArgsLocal = new int[3];
    
        private Vector3 m_origin;
        private Vector3 m_initialVelocityDir;
        private Vector3 m_prevPos;

        private DustMeshEmitter m_meshEmitter;
        
        private const int m_maxVertCount = 1048576; //64*64*16*16 (Groups*ThreadsPerGroup)
        #endregion

        //We initialize the buffers and the material used to draw.
        void Start()
        {
            if (Compute == null) {
                Debug.LogError("DustParticleSystem: No compute shader attached!");
                Debug.Break();
            }

            m_kernelSpawn = Compute.FindKernel("Spawn");
            m_kernelUpdate = Compute.FindKernel("Update");

            CreateBuffers();
            UpdateComputeUniforms();
            DispatchInit();

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

        void OnDisable()
        {
            ReleaseBuffers();
        }

        private void DispatchInit()
        {
            var initKernel = Compute.FindKernel("Init");
            Compute.SetBuffer(initKernel, "_particles", m_particlesBuffer);
            Compute.SetBuffer(initKernel, "_kernelArgs", m_kernelArgs);
            Compute.SetBuffer(initKernel, "_deadList", m_particlePoolBuffer);
            Compute.DispatchIndirect(initKernel, m_kernelArgs);
        }

        private void Dispatch()
        {
            int groupSize = GetPoolSize();
            if (groupSize > 0) {
                Compute.SetBuffer(m_kernelSpawn, "_particlePool", m_particlePoolBuffer);
                Compute.Dispatch(m_kernelSpawn, groupSize, groupSize, 1);
            }

            Compute.SetBuffer(m_kernelUpdate, "_deadList", m_particlePoolBuffer);
            Compute.DispatchIndirect(m_kernelUpdate, m_kernelArgs);
        }

        private int GetPoolSize()
        {
            m_particlePoolArgsBuffer.SetData(m_poolArgsLocal);
            ComputeBuffer.CopyCount(m_particlePoolBuffer, m_particlePoolArgsBuffer, 0);
            m_particlePoolArgsBuffer.GetData(m_poolArgsLocal);
            int groupSize = (int)Mathf.Floor(Mathf.Sqrt(m_poolArgsLocal[0])/16f);
            return groupSize;
        }

        // Create and initialize compute shader buffers
        private void CreateBuffers()
        {
            CreatePoolArgs();
            UpdateKernelArgs();

            DustParticle[] particlesTemp = new DustParticle[m_maxVertCount];
            for (int i = 0; i < m_maxVertCount; i++) {
                particlesTemp[i] = new DustParticle();
            }

            m_particlesBuffer = new ComputeBuffer(m_maxVertCount, Marshal.SizeOf(typeof(DustParticle)));
            m_particlesBuffer.SetData(particlesTemp);
            Compute.SetBuffer(m_kernelSpawn, "_particles", m_particlesBuffer);
            Compute.SetBuffer(m_kernelUpdate, "_particles", m_particlesBuffer);

            // Create color ramp textures
            ColorByLife.Setup();
            ColorByVelocity.Setup();
            Compute.SetTexture(m_kernelUpdate, "_colorByLife", (Texture)ColorByLife.Texture);
            Compute.SetTexture(m_kernelUpdate, "_colorByVelocity", (Texture)ColorByVelocity.Texture);

            // Set up mesh emitter
            if (EmissionMeshRenderer != null) {
                if (m_meshEmitter == null) {
                    m_meshEmitter = new DustMeshEmitter(EmissionMeshRenderer);
                }
                m_meshEmitter.Update();
                Compute.SetBuffer(m_kernelSpawn , "_emissionMesh", m_meshEmitter.MeshBuffer);
                Compute.SetBuffer(m_kernelSpawn , "_emissionMeshTris", m_meshEmitter.MeshTrisBuffer);
            }

        }

        public void UpdateKernelArgs()
        {
            if (m_kernelArgs == null) {
                m_kernelArgs = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
            }
            if (m_kernelArgsLocal == null) {
                m_kernelArgsLocal = new int[3];
            }

            int groupSize = (int)Mathf.Ceil(Mathf.Sqrt(Emission)/16f);
            m_kernelArgsLocal[0] = groupSize;
            m_kernelArgsLocal[1] = groupSize;
            m_kernelArgsLocal[2] = 1;
            m_kernelArgs.SetData(m_kernelArgsLocal);

            Compute.SetBuffer(m_kernelSpawn, "_kernelArgs", m_kernelArgs);
            Compute.SetBuffer(m_kernelUpdate, "_kernelArgs", m_kernelArgs);            
        }

        private void CreatePoolArgs()
        {
            if (m_particlePoolBuffer != null) m_particlePoolBuffer.Release();
            m_particlePoolBuffer = new ComputeBuffer(m_maxVertCount, sizeof(int), ComputeBufferType.Append);
            m_particlePoolBuffer.SetCounterValue(0);

            if (m_particlePoolArgsBuffer != null) m_particlePoolArgsBuffer.Release();
            m_particlePoolArgsBuffer = new ComputeBuffer(3, sizeof(int), ComputeBufferType.IndirectArguments);
            m_poolArgsLocal = new int[] {0, 1, 0};
        }

        private void ReleaseBuffers()
        {
            m_particlesBuffer.Release();
            m_particlePoolBuffer.Release();
            m_particlePoolArgsBuffer.Release();
            m_kernelArgs.Release();
            if (m_meshEmitter != null) {
                m_meshEmitter.ReleaseBuffers();
            }
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

            Compute.SetFloat("dt", Time.fixedDeltaTime);
            Compute.SetFloat("fixedTime", Time.time);
            // Particles
            Compute.SetVector("origin", m_origin);
            Compute.SetVector("massNew", Mass);
            Compute.SetVector("momentumNew", Momentum);
            Compute.SetVector("lifespanNew", Lifespan);
			// Velocity
            Compute.SetFloat("inheritVelocityMult", InheritVelocity);
            Compute.SetVector("initialVelocityDir", m_initialVelocityDir);
            Compute.SetVector("gravityIn", Physics.gravity);
            Compute.SetFloat("gravityModifier", GravityModifier);
            Compute.SetFloat("jitter", Jitter);
			// Shape
            Compute.SetFloat("randomizeDirection", RandomizeDirection);
            Compute.SetInt("emissionShape", Shape);
            Compute.SetInt("emission", Emission);
            Compute.SetVector("emissionSize", EmissionSize);
            Compute.SetFloat("initialSpeed", InitialSpeed);
            Compute.SetFloat("scatterVolume", ScatterVolume);
			// Rotation
            Compute.SetBool("alignToDirection", AlignToDirection);
            Compute.SetFloat("rotationOverLifetime", RotationOverLifetime);
			// Color
            Compute.SetVector("startColor", StartColor);
            Compute.SetFloat("velocityColorRange", ColorByVelocity.Range);
            Compute.SetFloat("randomizeColor", RandomizeColor);
            Compute.SetBool("useMeshEmitterColor", UseMeshEmitterColor);
			// Noise
            Compute.SetBool("noiseToggle", NoiseToggle);
            Compute.SetInt("noiseType", NoiseType);
            Compute.SetVector("noiseAmplitude", NoiseAmplitude);
            Compute.SetVector("noiseScale", NoiseScale);
            Compute.SetVector("noiseOffset", NoiseOffset);
            Compute.SetVector("noiseOffsetSpeed", NoiseOffsetSpeed);
            if (m_meshEmitter != null) {
                Compute.SetMatrix("emissionMeshMatrix", m_meshEmitter.MeshRenderer.localToWorldMatrix);
                Compute.SetMatrix("emissionMeshMatrixInvT", m_meshEmitter.MeshRenderer.localToWorldMatrix.inverse.transpose);
                Compute.SetInt("emissionMeshVertCount", m_meshEmitter.VertexCount);
                Compute.SetInt("emissionMeshTrisCount", m_meshEmitter.TriangleCount);
            }
        }

    }
}