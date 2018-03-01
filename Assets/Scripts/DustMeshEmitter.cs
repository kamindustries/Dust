using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Dust 
{
    public class DustMeshEmitter
    {
        #region Public Properties
        public MeshRenderer MeshRenderer
        {
            get
            {
                return m_meshRenderer;
            }
        }        

        public ComputeBuffer MeshBuffer
        {
            get
            {
                return m_meshBuffer;
            }
        }

        public ComputeBuffer MeshTrisBuffer
        {
            get
            {
                return m_meshTrisBuffer;
            }
        }
        public Mesh Mesh
        {
            get
            {
                return m_mesh;
            }
        }
        public int VertexCount
        {
            get
            {
                return m_mesh.vertexCount;
            }
        }
        public int TriangleCount
        {
            get
            {
                return m_meshTrisCount;
            }
        }

        public DustMeshEmitter(MeshRenderer meshRenderer)
        {
            m_meshRenderer = meshRenderer;
        }
        #endregion

        #region Private Properties
        private MeshRenderer m_meshRenderer;
        public ComputeBuffer m_meshBuffer;
        public ComputeBuffer m_meshTrisBuffer;

        private Mesh m_mesh;
        private int m_meshTrisCount;
        #endregion

        public void Update()
        {
            m_mesh = m_meshRenderer.GetComponent<MeshFilter>().sharedMesh;
            int numAttribs = 3; //pos, normal, cd
            m_meshTrisCount = m_mesh.triangles.Length;
            m_meshBuffer = new ComputeBuffer(m_mesh.vertexCount, sizeof(float) * 3 * numAttribs); 
            m_meshTrisBuffer = new ComputeBuffer(m_meshTrisCount, sizeof(int)); 

            bool hasNormals = m_mesh.normals.Length == m_mesh.vertexCount ? true : false;
            bool hasColors = m_mesh.colors.Length == m_mesh.vertexCount ? true : false;
            
            Vector3[] emissionMeshTemp = new Vector3[m_mesh.vertexCount * numAttribs];
            int[] emissionMeshTrisTemp = new int[m_meshTrisCount];
            
            for (int i = 0; i < m_mesh.vertexCount; i++) {
                emissionMeshTemp[(i*numAttribs)+0] = m_mesh.vertices[i];
                emissionMeshTemp[(i*numAttribs)+1] = new Vector3(0f,1f,0f); //normals
                emissionMeshTemp[(i*numAttribs)+2] = Vector3.one; //colors

                if (hasNormals) {
                    emissionMeshTemp[(i*numAttribs)+1] = m_mesh.normals[i];
                }

                if (hasColors) {
                    emissionMeshTemp[(i*numAttribs)+2].x = m_mesh.colors[i].r;
                    emissionMeshTemp[(i*numAttribs)+2].y = m_mesh.colors[i].g;
                    emissionMeshTemp[(i*numAttribs)+2].z = m_mesh.colors[i].b;
                }
            }

            for (int i = 0; i < m_meshTrisCount; i++) {
                emissionMeshTrisTemp[i] = m_mesh.triangles[i];
            }
            
            m_meshBuffer.SetData(emissionMeshTemp);
            m_meshTrisBuffer.SetData(emissionMeshTrisTemp);
        }

        // Called from DustParticleSystem... GC doesn't seem to like this and throws a warning
        public void ReleaseBuffers() 
        {
            m_meshBuffer.Release();
            m_meshTrisBuffer.Release();
        }


    }
}