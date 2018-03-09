using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dust
{
    public class DustInstanceRenderer : DustRenderer {

        public Mesh InstancedMesh;
        public uint InstanceCount = 100;
        public Vector3 Size = new Vector3(1f,1f,1f);
        public Vector3 Rotation = new Vector3(0f,0f,0f);
        public ShadowCastingMode CastShadows = ShadowCastingMode.On;
        public bool ReceiveShadows = true;

        private ComputeBuffer argsBuffer;
        private uint m_instanceCount
        {
            get
            {
                return (uint)Mathf.Min(InstanceCount, particles.Emission);
            }
        }

        // num indices/instance, num instances, start index, start vertex, start instance
        private uint[] m_args = new uint[5] { 0, 0, 0, 0, 0 }; 
        private Vector3 m_size;
        private Vector3 m_rotation;

        void Start ()
        {
            UpdateMesh();

            if (argsBuffer != null) argsBuffer.Release();
            argsBuffer = new ComputeBuffer(1, m_args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            UpdateArgs();        
        
            propertyBlock.Clear();
            propertyBlock.SetBuffer("dataBuffer", particles.ParticlesBuffer);
        }

        void UpdateMesh()
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();
            mesh = Instantiate(InstancedMesh);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 10000);

            m_size = Size;
            m_rotation = Rotation;
            
            Vector3[] verts = mesh.vertices;
            for (int i = 0; i < verts.Length; i++) {
                Vector3 pos = Vector3.Scale(verts[i], m_size);
                pos = Quaternion.Euler(Rotation) * pos;
                verts[i] = pos;
            }
            mesh.vertices = verts;

            if (mesh.normals.Length > 0) {
                Vector3[] normals = mesh.normals;
                Quaternion rotation = Quaternion.Euler(m_rotation);
                for (int i = 0; i < normals.Length; i++) {
                    normals[i] =  rotation * normals[i];
                }
                mesh.normals = normals;
            }

        }

        void UpdateArgs()
        {
            m_args[0] = (mesh != null) ? (uint)mesh.GetIndexCount(0) : 0;
            m_args[1] = m_instanceCount;
            argsBuffer.SetData(m_args);
        }

        public override void Draw() 
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, m_material, mesh.bounds, argsBuffer, 0, propertyBlock, CastShadows, ReceiveShadows);
        }

        public override void UpdatePropertyBlock()
        {
            if (m_args[1] != m_instanceCount) {
                UpdateArgs();
            }
            if (m_size != Size || m_rotation != Rotation) {
                UpdateMesh();
            }
            propertyBlock.SetFloat("_NumInstances", InstanceCount);
            propertyBlock.SetFloat("_NumParticles", particles.Emission);

        }

        void OnDestroy() 
        {
            if (argsBuffer != null) argsBuffer.Release();
        }

    }
}
