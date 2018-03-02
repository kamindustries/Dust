using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Dust
{
    public class DustInstanceRenderer : DustRenderer {

        public Mesh InstancedMesh;
        public uint InstanceCount = 100;
        public Vector3 Scale = new Vector3(1f,1f,1f);
        public ShadowCastingMode CastShadows = ShadowCastingMode.On;
        public bool ReceiveShadows = true;

        private ComputeBuffer argsBuffer;

        // num indices/instance, num instances, start index, start vertex, start instance
        private uint[] args = new uint[5] { 0, 0, 0, 0, 0 }; 

        void Start ()
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();
            mesh = InstancedMesh;
            mesh.RecalculateBounds();

            propertyBlock.Clear();

            if (argsBuffer != null) argsBuffer.Release();
            argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
            uint numIndices = (mesh != null) ? (uint)mesh.GetIndexCount(0) : 0;
            args[0] = numIndices;
            args[1] = (uint)Mathf.Min(InstanceCount, particles.Emission);
            argsBuffer.SetData(args);
        }

        public override void Draw() 
        {
            Graphics.DrawMeshInstancedIndirect(mesh, 0, m_material, mesh.bounds, argsBuffer, 0, propertyBlock, CastShadows, ReceiveShadows);
        }

        public override void UpdatePropertyBlock()
        {
            propertyBlock.SetBuffer("dataBuffer", particles.ParticlesBuffer);
            propertyBlock.SetVector("_Scale", Scale);
            if (InstanceCount != args[1]) {
                args[1] = (uint)Mathf.Min(InstanceCount, particles.Emission);
                argsBuffer.SetData(args);
            }
            propertyBlock.SetFloat("_NumInstances", InstanceCount);
            propertyBlock.SetFloat("_NumParticles", particles.Emission);
        }

        public void OnDestroy() 
        {
            argsBuffer.Release();
        }

    }
}
