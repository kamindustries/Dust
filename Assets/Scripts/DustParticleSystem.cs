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
    [StructLayout(LayoutKind.Explicit)]
    public struct DustParticle
    {
        [FieldOffset(0)] Vector3 pos;
        
        [FieldOffset(12)] Vector3 vel;

        [FieldOffset(24)] Color cd;

        [FieldOffset(40)] Color startColor;
        
        [FieldOffset(56)] float age;
        
        [FieldOffset(60)] float lifespan;
        
        [FieldOffset(64)] float mass;
        
        [FieldOffset(68)] float momentum;
        
        [FieldOffset(72)] Vector3 scale;
        
        [FieldOffset(84)] Matrix4x4 rot;
        
        [FieldOffset(148)] int active;
    };

    public class DustParticleSystem : MonoBehaviour
    {
        #region Public Properties
        public ComputeShader Compute;

        // [Header("Particles")]
        public Vector2 Mass = new Vector2(0.5f, 0.5f);
        public Vector2 Momentum = new Vector2(0.95f, 0.95f);
        public Vector2 Lifespan = new Vector2(.5f, 1f);
        public int PreWarmFrames = 0;

        // [Header("Velocity")]
        public float InheritVelocity = 0f;
        public int EmitterVelocity = 0;
        public float GravityModifier = 0f;

        // [Header("Shape")]
        public int Shape = 0;
        [Range(0,m_maxVertCount)]
        public int Emission = 65000;
        public float InitialSpeed = 0f;
        // [Range(0,1)]
        public float Jitter = 0f;
        [Range(0,1)]
        public float RandomizeDirection = 0f;
        [Range(0,1)]
        public float RandomizeRotation = 0f;
        public bool AlignToInitialDirection = false;
        public Vector3 EmissionSize = new Vector3(1f,1f,1f);
        [Range(0,1)]
        public float ScatterVolume = 0f;
        public MeshRenderer EmissionMeshRenderer;

        // [Header("Size")]
        public CurveRamp SizeOverLife;

        // [Header("Rotation")]
        public bool AlignToDirection = false;
        public Vector3 RotationOverLifetime = new Vector3(0,0,0);

        // [Header("Color")]
        [ColorUsageAttribute(true,true,0,8,.125f,3)]
        public Color StartColor = new Color(1f,1f,1f,1f);
        public ColorRamp ColorOverLife;
        public ColorRampRange ColorOverVelocity;
        [Range(0,1)]
        public float RandomizeColor = 0f;
        public bool UseMeshEmitterColor = false;

        // [Header("Noise")]
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
        private ComputeShader m_compute;
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

            // Create a unique instance of the compute shader, needed for multiple particle systems
            // This incorrectly throws an assertion error in 2017.3.1
            m_compute = (ComputeShader)Instantiate(Compute);

            m_kernelSpawn = m_compute.FindKernel("Spawn");
            m_kernelUpdate = m_compute.FindKernel("Update");
            m_prevPos = transform.position;

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
            var initKernel = m_compute.FindKernel("Init");
            m_compute.SetBuffer(initKernel, "_particles", m_particlesBuffer);
            m_compute.SetBuffer(initKernel, "_kernelArgs", m_kernelArgs);
            m_compute.SetBuffer(initKernel, "_deadList", m_particlePoolBuffer);
            m_compute.DispatchIndirect(initKernel, m_kernelArgs);
        }

        private void Dispatch()
        {
            int groupSize = GetPoolSize();
            if (groupSize > 0) {
                m_compute.SetBuffer(m_kernelSpawn, "_particlePool", m_particlePoolBuffer);
                m_compute.Dispatch(m_kernelSpawn, groupSize, groupSize, 1);
            }

            m_compute.SetBuffer(m_kernelUpdate, "_deadList", m_particlePoolBuffer);
            m_compute.DispatchIndirect(m_kernelUpdate, m_kernelArgs);
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
            m_compute.SetBuffer(m_kernelSpawn, "_particles", m_particlesBuffer);
            m_compute.SetBuffer(m_kernelUpdate, "_particles", m_particlesBuffer);

            // Create ramp textures
            SizeOverLife.Setup();
            ColorOverLife.Setup();
            ColorOverVelocity.Setup();
            m_compute.SetTexture(m_kernelUpdate, "_sizeOverLife", (Texture)SizeOverLife.Texture);
            m_compute.SetTexture(m_kernelUpdate, "_colorOverLife", (Texture)ColorOverLife.Texture);
            m_compute.SetTexture(m_kernelUpdate, "_colorOverVelocity", (Texture)ColorOverVelocity.Texture);

            // Set up mesh emitter
            if (EmissionMeshRenderer != null) {
                if (m_meshEmitter == null) {
                    m_meshEmitter = new DustMeshEmitter(EmissionMeshRenderer);
                }
                m_meshEmitter.Update();
                m_compute.SetBuffer(m_kernelSpawn , "_emissionMesh", m_meshEmitter.MeshBuffer);
                m_compute.SetBuffer(m_kernelSpawn , "_emissionMeshTris", m_meshEmitter.MeshTrisBuffer);
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

            m_compute.SetBuffer(m_kernelSpawn, "_kernelArgs", m_kernelArgs);
            m_compute.SetBuffer(m_kernelUpdate, "_kernelArgs", m_kernelArgs);            
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

            // Update internal variables
            m_origin = transform.position;
            m_prevPos = m_origin;

            m_compute.SetFloat("dt", Time.fixedDeltaTime);
            m_compute.SetFloat("fixedTime", Time.time);
            // Particles
            m_compute.SetVector("origin", m_origin);
            m_compute.SetVector("massNew", Mass);
            m_compute.SetVector("momentumNew", Momentum);
            m_compute.SetVector("lifespanNew", Lifespan);
			// Velocity
            m_compute.SetFloat("inheritVelocityMult", InheritVelocity);
            m_compute.SetVector("initialVelocityDir", m_initialVelocityDir);
            m_compute.SetVector("gravityIn", Physics.gravity);
            m_compute.SetFloat("gravityModifier", GravityModifier);
			// Shape
            m_compute.SetInt("emissionShape", Shape);
            m_compute.SetInt("emission", Emission);
            m_compute.SetVector("emissionSize", EmissionSize);
            m_compute.SetFloat("scatterVolume", ScatterVolume);
            m_compute.SetFloat("initialSpeed", InitialSpeed);
            m_compute.SetFloat("jitter", Jitter);
            m_compute.SetFloat("randomizeDirection", RandomizeDirection);
            m_compute.SetFloat("randomizeRotation", RandomizeRotation);
            m_compute.SetBool("alignToInitialDirection", AlignToInitialDirection);
            // Size
            m_compute.SetBool("sizeOverLifeToggle", SizeOverLife.Enable);
			// Rotation
            m_compute.SetBool("alignToDirection", AlignToDirection);
            m_compute.SetVector("rotationOverLifetime", RotationOverLifetime);
			// Color
            m_compute.SetVector("startColor", StartColor);
            m_compute.SetBool("colorOverLifeToggle", ColorOverLife.Enable);
            m_compute.SetBool("colorOverVelocityToggle", ColorOverVelocity.Enable);
            m_compute.SetFloat("velocityColorRange", ColorOverVelocity.Range);
            m_compute.SetFloat("randomizeColor", RandomizeColor);
            m_compute.SetBool("useMeshEmitterColor", UseMeshEmitterColor);
			// Noise
            m_compute.SetBool("noiseToggle", NoiseToggle);
            m_compute.SetInt("noiseType", NoiseType);
            m_compute.SetVector("noiseAmplitude", NoiseAmplitude);
            m_compute.SetVector("noiseScale", NoiseScale);
            m_compute.SetVector("noiseOffset", NoiseOffset);
            m_compute.SetVector("noiseOffsetSpeed", NoiseOffsetSpeed);
            if (m_meshEmitter != null) {
                m_compute.SetMatrix("emissionMeshMatrix", m_meshEmitter.MeshRenderer.localToWorldMatrix);
                m_compute.SetMatrix("emissionMeshMatrixInvT", m_meshEmitter.MeshRenderer.localToWorldMatrix.inverse.transpose);
                m_compute.SetInt("emissionMeshVertCount", m_meshEmitter.VertexCount);
                m_compute.SetInt("emissionMeshTrisCount", m_meshEmitter.TriangleCount);
            }
        }

    }
}