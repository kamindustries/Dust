using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust
{
    public class DustPointRenderer : DustRenderer {

        void Start ()
        {
            if (mesh == null) mesh = new Mesh();
            mesh.Clear();

            Vector3 [] meshVerts = new Vector3[numVertices];
            int [] meshIndices = new int[numVertices];

            for (int i = 0; i < numVertices; i++) {
                meshVerts[i] = Random.insideUnitSphere * 50f;
                meshIndices[i] = i;
            }

            // Dummy mesh geometry for points
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = meshVerts;
            mesh.SetIndices(meshIndices, MeshTopology.Points, 0);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);

            meshFilter.mesh = mesh;

            propertyBlock.Clear();
        }

        public override void Draw() 
        {
            Graphics.DrawMesh(mesh, transform.localToWorldMatrix, m_material, 0, null, 0, propertyBlock, true, true);
        }


        public override void UpdatePropertyBlock()
        {
            propertyBlock.SetBuffer("dataBuffer", particles.ParticlesBuffer);
            propertyBlock.SetFloat("numParticles", (float)particles.Emission);
        }


    }
}
