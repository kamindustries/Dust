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
        public int Shape = 0;
        [Range(0,VertCount)]
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
        
        #region Private Variables
        private Mesh particlesMesh;
        private ComputeBuffer particlesBuffer;
        private ComputeBuffer emissionMeshBuffer;
        private ComputeBuffer emissionMeshTrisBuffer;
        private int _kernel;
        private Vector3 _origin;
        private Vector3 _initialVelocityDir;
        private Vector3 _prevPos;
        private Mesh _emissionMesh;
        private int emissionMeshTrisCount;
        
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
            // Update internal variables
            _prevPos = transform.position;

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
                            _initialVelocityDir = transform.parent.gameObject.GetComponent<Rigidbody>().velocity;
                        }
                    }
                    else {
                        _initialVelocityDir = Vector3.zero;
                    }
                    break;
                case 1:
                    _initialVelocityDir = transform.position-_prevPos;
                    break;
            }

            _origin = transform.position;

            ParticleSystemKernel.SetFloat("dt", Time.fixedDeltaTime);
            ParticleSystemKernel.SetFloat("fixedTime", Time.fixedTime);
            ParticleSystemKernel.SetVector("origin", _origin);
            ParticleSystemKernel.SetVector("massNew", Mass);
            ParticleSystemKernel.SetVector("momentumNew", Momentum);
            ParticleSystemKernel.SetVector("lifespanNew", Lifespan);
            ParticleSystemKernel.SetFloat("inheritVelocityMult", InheritVelocity);
            ParticleSystemKernel.SetVector("initialVelocityDir", _initialVelocityDir);
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
                ParticleSystemKernel.SetMatrix("emissionMeshMatrix", EmissionMeshRenderer.localToWorldMatrix);
                ParticleSystemKernel.SetInt("emissionMeshVertCount", _emissionMesh.vertexCount);
                ParticleSystemKernel.SetInt("emissionMeshTrisCount", emissionMeshTrisCount);
                
            }
        }


        // Create and initialize compute shader buffers
        private void CreateBuffers()
        {
            // Allocate
            particlesBuffer = new ComputeBuffer(VertCount, sizeof(float) * NumElements); //float3 pos, vel, cd; float age
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

            // Dummy mesh geometry for points
            particlesMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            particlesMesh.vertices = meshVerts;
            particlesMesh.SetIndices(meshIndices, MeshTopology.Points, 0);
            particlesMesh.RecalculateBounds();
            MeshFilter mf = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            mf.hideFlags = HideFlags.HideInInspector;
            mf.mesh = particlesMesh;

            // Create color ramp textures
            ColorByLife.Setup();
            ColorByVelocity.Setup();
            ParticleSystemKernel.SetTexture(_kernel, "_colorByLife", (Texture)ColorByLife.Texture);
            ParticleSystemKernel.SetTexture(_kernel, "_colorByVelocity", (Texture)ColorByVelocity.Texture);

            if (EmissionMeshRenderer) {
                UpdateMeshSource();
            }

        }

        private void UpdateMeshSource() 
        {
            _emissionMesh = EmissionMeshRenderer.GetComponent<MeshFilter>().sharedMesh;
            int numAttribs = 3; //pos, normal, cd
            emissionMeshTrisCount = _emissionMesh.triangles.Length;
            emissionMeshBuffer = new ComputeBuffer(_emissionMesh.vertexCount, sizeof(float) * 3 * numAttribs); 
            emissionMeshTrisBuffer = new ComputeBuffer(emissionMeshTrisCount, sizeof(int)); 

            bool hasNormals = _emissionMesh.normals.Length == _emissionMesh.vertexCount ? true : false;
            bool hasColors = _emissionMesh.colors.Length == _emissionMesh.vertexCount ? true : false;
            
            Vector3[] emissionMeshTemp = new Vector3[_emissionMesh.vertexCount * numAttribs];
            int[] emissionMeshTrisTemp = new int[emissionMeshTrisCount];
            
            for (int i = 0; i < _emissionMesh.vertexCount; i++) {
                emissionMeshTemp[(i*numAttribs)+0] = _emissionMesh.vertices[i];
                emissionMeshTemp[(i*numAttribs)+1] = new Vector3(0f,1f,0f); //normals
                emissionMeshTemp[(i*numAttribs)+2] = Vector3.one; //colors

                if (hasNormals) {
                    emissionMeshTemp[(i*numAttribs)+1] = _emissionMesh.normals[i];
                }

                if (hasColors) {
                    emissionMeshTemp[(i*numAttribs)+2].x = _emissionMesh.colors[i].r;
                    emissionMeshTemp[(i*numAttribs)+2].y = _emissionMesh.colors[i].g;
                    emissionMeshTemp[(i*numAttribs)+2].z = _emissionMesh.colors[i].b;
                }
            }

            for (int i = 0; i < emissionMeshTrisCount; i++) {
                emissionMeshTrisTemp[i] = _emissionMesh.triangles[i];
                print(_emissionMesh.triangles[i]);
            }
            
            emissionMeshBuffer.SetData(emissionMeshTemp);
            emissionMeshTrisBuffer.SetData(emissionMeshTrisTemp);
            ParticleSystemKernel.SetBuffer(_kernel, "emissionMesh", emissionMeshBuffer);
            ParticleSystemKernel.SetBuffer(_kernel, "emissionMeshTris", emissionMeshTrisBuffer);
        }

        //Remember to release buffers and destroy the material when play has been stopped.
        private void ReleaseBuffers()
        {
            particlesBuffer.Release();
            if (EmissionMeshRenderer) {
                emissionMeshBuffer.Release();
                emissionMeshTrisBuffer.Release();
            }
        }

    }
}