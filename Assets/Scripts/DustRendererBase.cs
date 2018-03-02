using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust
{
    [RequireComponent(typeof(DustParticleSystem))]
    public class DustRenderer : MonoBehaviour {

        public Material m_material;

        public MaterialPropertyBlock PropertyBlock { get { return m_propertyBlock; } }
        public Mesh DefaultMesh { get { return m_mesh; } set { m_mesh = value; } }
        public DustParticleSystem Particles { get { return m_particleSystem; } }
        public int NumVertices { get { return m_maxVertCount; } }

        private Mesh m_mesh;
        private MaterialPropertyBlock m_propertyBlock;
        private DustParticleSystem m_particleSystem;
        private const int m_maxVertCount = 1048576; 

        void OnEnable () 
        {
            m_particleSystem = GetComponent<DustParticleSystem>();
            m_propertyBlock = new MaterialPropertyBlock();
        }
        
        // Update is called once per frame
        void Update () 
        {
            UpdatePropertyBlock();
            Draw();
        }

        public virtual void Draw() {}

        public virtual void UpdatePropertyBlock() {}

    }
}
