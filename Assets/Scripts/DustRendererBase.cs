using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Dust
{
    [RequireComponent(typeof(DustParticleSystem))]
    public class DustRenderer : MonoBehaviour {

        public Material m_material;

        public Mesh mesh { get { return m_mesh; } set { m_mesh = value; } }
        public GameObject child { get { return m_gameObject; } }
        public MaterialPropertyBlock propertyBlock { get { return m_propertyBlock; } }
        public MeshFilter meshFilter { get { return m_meshFilter; } }
        public DustParticleSystem particles { get { return m_particleSystem; } }
        public int numVertices { get { return m_maxVertCount; } }


        private Mesh m_mesh;
        private GameObject m_gameObject;
        private MeshFilter m_meshFilter;
        private MaterialPropertyBlock m_propertyBlock;
        private DustParticleSystem m_particleSystem;
        private const int m_maxVertCount = 1048576; 

        void OnEnable () 
        {
            m_particleSystem = GetComponent<DustParticleSystem>();
            m_propertyBlock = new MaterialPropertyBlock();

            m_gameObject = new GameObject();
            m_gameObject.transform.parent = transform;
            m_gameObject.hideFlags = HideFlags.HideInHierarchy;

            m_meshFilter = m_gameObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
            meshFilter.hideFlags = HideFlags.HideInInspector;
        }
        
        void OnDisable() 
        {
            Destroy(m_gameObject);
        }

        // Update is called once per frame
        void Update() 
        {
            UpdatePropertyBlock();
            Draw();
        }

        public virtual void Draw() {}

        public virtual void UpdatePropertyBlock() {}

    }
}
