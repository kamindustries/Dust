using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust
{
    public class DustPointRenderer : DustRenderer {

        void Start ()
        {
            if (DefaultMesh == null) DefaultMesh = new Mesh();
            DefaultMesh.Clear();
            Vector3 [] meshVerts = new Vector3[NumVertices];
            int [] meshIndices = new int[NumVertices];

            for (int i = 0; i < NumVertices; i++) {
                meshVerts[i] = Random.insideUnitSphere * 50f;
                meshIndices[i] = i;
            }

            // Dummy mesh geometry for points
            DefaultMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            DefaultMesh.vertices = meshVerts;
            DefaultMesh.SetIndices(meshIndices, MeshTopology.Points, 0);
            DefaultMesh.RecalculateBounds();
            DefaultMesh.UploadMeshData(true);

            MeshFilter mf = gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            mf.hideFlags = HideFlags.HideInInspector;
            mf.mesh = DefaultMesh;

            PropertyBlock.Clear();
        }

        public override void Draw() 
        {
            Graphics.DrawMesh(DefaultMesh, transform.localToWorldMatrix, m_material, 0, null, 0, PropertyBlock, true, true);
        }


        public override void UpdatePropertyBlock()
        {
            PropertyBlock.SetBuffer("dataBuffer", Particles.ParticlesBuffer);
            PropertyBlock.SetFloat("numParticles", (float)Particles.Emission);
        }


    }
}
