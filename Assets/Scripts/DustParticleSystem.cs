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
        #region Public Variables
        public Mesh ParticlesMesh 
        {
            get 
            {
                return particlesMesh;
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
        [Range(0,VertCount)]
        public int Emission = 65000;
        public Vector3 EmissionSize = new Vector3(1f,1f,1f);
        public float InitialSpeed = 0f;
        [Range(0,1)]
        public float ScatterSphereVolume = 0f;

        [Header("Color")]
        public Color StartColor = new Color(1f,1f,1f,1f);
        public ColorRamp ColorByLife;
        public ColorRampRange ColorByVelocity;

        [Header("Noise")]
        public Vector3 NoiseAmplitude = new Vector3(0f,0f,0f);
        public Vector3 NoiseScale = new Vector3(1f,1f,1f);
        public Vector3 NoiseOffset = new Vector3(0f,0f,0f);
        public Gradient TestGradient;
        #endregion
        
        #region Private Variables
        private Mesh particlesMesh;
        private ComputeBuffer particlesBuffer;
        private int _kernel;
        private Vector3 origin;
        private Vector3 initialVelocityDir;
        private Vector3 prevPos;
        
        private const int NumElements = 14; //float4 cd; float3 pos, vel; float age, lifespan, mass, momentum
        private const int VertCount = 1048576; //64*64*16*16 (Groups*ThreadsPerGroup)
        #endregion

        //We initialize the buffers and the material used to draw.
        void Start()
        {
            _kernel = ParticleSystemKernel.FindKernel("DustParticleSystemKernel");
            CreateBuffers();            
            UpdateUniforms();

            // Prewarm the system
            if (PreWarmFrames > 0) {
                for (int i = 0; i < PreWarmFrames; i++) {
                    Dispatch();
                }
            }
            
        }

        void FixedUpdate() 
        {
            UpdateUniforms();
            Dispatch();
        }

        void Update() 
        {
            ParticleMaterial.SetBuffer("dataBuffer", particlesBuffer);
            ParticleMaterial.SetVector("xform", transform.position);
            // Matrix4x4 m = GetComponent<Renderer>().transform.localToWorldMatrix;
            // ParticleMaterial.SetMatrix("iMatrix", m);
            ParticleMaterial.SetPass(0);
            Graphics.DrawMesh(particlesMesh, transform.localToWorldMatrix, ParticleMaterial, 0, null, 0, null, true, true);
        }

        void OnDisable()
        {
            ReleaseBuffers();
        }

        //We dispatch 32x32x1 groups of threads of our CSMain kernel.
        private void Dispatch()
        {
            ParticleSystemKernel.Dispatch(_kernel, 64, 64, 1);
        }

        private void UpdateUniforms() 
        {
            prevPos = transform.position;

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
                            initialVelocityDir = transform.parent.gameObject.GetComponent<Rigidbody>().velocity;
                        }
                    }
                    else {
                        initialVelocityDir = Vector3.zero;
                    }
                    break;
                case 1:
                    initialVelocityDir = transform.position-prevPos;
                    break;
            }

            origin = transform.position;

            ParticleSystemKernel.SetFloat("dt", Time.deltaTime);
            ParticleSystemKernel.SetVector("origin", origin);
            ParticleSystemKernel.SetVector("massNew", Mass);
            ParticleSystemKernel.SetVector("momentumNew", Momentum);
            ParticleSystemKernel.SetVector("lifespanNew", Lifespan);
            ParticleSystemKernel.SetFloat("inheritVelocityMult", InheritVelocity);
            ParticleSystemKernel.SetVector("initialVelocityDir", initialVelocityDir);
            ParticleSystemKernel.SetVector("gravityIn", Physics.gravity);
            ParticleSystemKernel.SetFloat("gravityModifier", GravityModifier);
            ParticleSystemKernel.SetInt("emission", Emission);
            ParticleSystemKernel.SetVector("emissionSize", EmissionSize);
            ParticleSystemKernel.SetFloat("initialSpeed", InitialSpeed);
            ParticleSystemKernel.SetFloat("scatterSphereVolume", ScatterSphereVolume);
            ParticleSystemKernel.SetVector("startColor", StartColor);
            ParticleSystemKernel.SetFloat("velocityColorRange", ColorByVelocity.Range);
            ParticleSystemKernel.SetVector("noiseAmplitude", NoiseAmplitude);
            ParticleSystemKernel.SetVector("noiseScale", NoiseScale);
            ParticleSystemKernel.SetVector("noiseOffset", NoiseOffset);
        }


        // Create and initialize compute shader buffers
        private void CreateBuffers()
        {
            // Allocate
            particlesBuffer = new ComputeBuffer(VertCount, 4 * NumElements); //float3 pos, vel, cd; float age
            particlesMesh = new Mesh();
            float[] particlesTemp = new float[VertCount * NumElements];
            Vector3 [] meshVerts = new Vector3[VertCount];
            int [] meshIndices = new int[VertCount];

            // Initialize
            for (int i = 0; i < VertCount; i++) {
                meshVerts[i] = Random.insideUnitSphere * 50f;
                meshIndices[i] = i;
                for (int j = 0; j < NumElements; j++) {
                    particlesTemp[(i*NumElements)+j] = 0f;
                }
            }

            particlesBuffer.SetData(particlesTemp);
            ParticleSystemKernel.SetBuffer(_kernel, "output", particlesBuffer);

            // Mesh
            particlesMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            particlesMesh.vertices = meshVerts;
            particlesMesh.SetIndices(meshIndices, MeshTopology.Points, 0);
            particlesMesh.RecalculateBounds();
            Debug.Log(particlesMesh.bounds);
            MeshFilter mf = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            mf.hideFlags = HideFlags.HideInInspector;
            mf.mesh = particlesMesh;

            // Create color ramp textures
            ColorByLife.Setup();
            ColorByVelocity.Setup();
            ParticleSystemKernel.SetTexture(_kernel, "_colorByLife", (Texture)ColorByLife.Texture);
            ParticleSystemKernel.SetTexture(_kernel, "_colorByVelocity", (Texture)ColorByVelocity.Texture);

        }

        //Remember to release buffers and destroy the material when play has been stopped.
        private void ReleaseBuffers()
        {
            particlesBuffer.Release();

        }

    }
}