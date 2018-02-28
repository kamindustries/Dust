using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Dust {

	[CustomEditor(typeof(DustParticleSystem))]
	[CanEditMultipleObjects]
	public class DustParticleSystemEditor : Editor {

        SerializedProperty Compute;
        SerializedProperty Material;

		// Particles
        SerializedProperty Mass;
        SerializedProperty Momentum;
        SerializedProperty Lifespan;
        SerializedProperty PreWarmFrames;

		// Velocity
        SerializedProperty InheritVelocity;
		string[] _emitterVelocity = new string[] {"Rigid body", "Transform"};
		int _emitterVelocityIdx = 0;
        SerializedProperty GravityModifier;

		// Shape
		string[] _shape = new string[] {"Sphere", "Mesh Renderer"};
		int _shapeIdx = 0;
        SerializedProperty Emission;
        SerializedProperty InitialSpeed;
        SerializedProperty EmissionSize;
		SerializedProperty ScatterVolume;
        SerializedProperty EmissionMeshRenderer;

		// Color
        SerializedProperty StartColor;
        SerializedProperty ColorByLife;
        SerializedProperty ColorByVelocity;

		// Noise
		string[] _noiseType = new string[] {"2D", "3D", "4D"};
		int _noiseTypeIdx = 1;
        Vector3 _noiseAmplitude = new Vector4(0f,0f,0f);
        Vector3 _noiseScale = new Vector4(1f,1f,1f);
        Vector4 _noiseOffset = new Vector4(0f,0f,0f,0f);
        Vector4 _noiseOffsetSpeed = new Vector4(0f,0f,0f,0f);
		GUIStyle headerStyle;

		void OnEnable() 
		{
			Compute = serializedObject.FindProperty("ParticleSystemKernel");
			Material = serializedObject.FindProperty("ParticleMaterial");
			
			// Particles
			Mass = serializedObject.FindProperty("Mass");
			Momentum = serializedObject.FindProperty("Momentum");
			Lifespan = serializedObject.FindProperty("Lifespan");
			PreWarmFrames = serializedObject.FindProperty("PreWarmFrames");

			// Velocity
			InheritVelocity = serializedObject.FindProperty("InheritVelocity");
			GravityModifier = serializedObject.FindProperty("GravityModifier");

			// Shape
			Emission = serializedObject.FindProperty("Emission");
			InitialSpeed = serializedObject.FindProperty("InitialSpeed");
			EmissionSize = serializedObject.FindProperty("EmissionSize");
			ScatterVolume = serializedObject.FindProperty("ScatterVolume");
			EmissionMeshRenderer = serializedObject.FindProperty("EmissionMeshRenderer");

			// Color
			StartColor = serializedObject.FindProperty("StartColor");
			ColorByLife = serializedObject.FindProperty("ColorByLife");
			ColorByVelocity = serializedObject.FindProperty("ColorByVelocity");
			
			// Noise
			

		}

		public override void OnInspectorGUI()
		{
			var particles = target as DustParticleSystem;

			EditorGUILayout.PropertyField(Compute);
			EditorGUILayout.PropertyField(Material);

			// Particles
			EditorGUILayout.PropertyField(Mass);
			EditorGUILayout.PropertyField(Momentum);
			EditorGUILayout.PropertyField(Lifespan);
			EditorGUILayout.PropertyField(PreWarmFrames);


			// Velocity
			EditorGUILayout.PropertyField(InheritVelocity);
			_emitterVelocityIdx = particles.EmitterVelocity;
			_emitterVelocityIdx = EditorGUILayout.Popup("Emitter Velocity", _emitterVelocityIdx, _emitterVelocity);
			particles.EmitterVelocity = _emitterVelocityIdx;
			EditorGUILayout.PropertyField(GravityModifier);

			// Shape
			EditorGUILayout.Space();
			headerStyle = GUI.skin.label;
			headerStyle.fontStyle = FontStyle.Bold;
			EditorGUILayout.LabelField("Shape", headerStyle);

			_shapeIdx = particles.Shape;
			_shapeIdx = EditorGUILayout.Popup("Shape", _shapeIdx, _shape);
			particles.Shape = _shapeIdx;
			EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(Emission);
			if (EditorGUI.EndChangeCheck()) {
				particles.UpdateKernelArgs();
			}			
			EditorGUILayout.PropertyField(InitialSpeed);
			switch(_shapeIdx){
				case 0:
					EditorGUILayout.PropertyField(EmissionSize);
					EditorGUILayout.PropertyField(ScatterVolume);
					break;
				case 1:
					EditorGUILayout.PropertyField(EmissionMeshRenderer, new GUIContent("Mesh Renderer"));
					break;
			}

			// Color
			EditorGUILayout.PropertyField(StartColor);
			// Interactive gradient editor
			EditorGUI.BeginChangeCheck();
				EditorGUILayout.PropertyField(ColorByLife, true);
				EditorGUILayout.PropertyField(ColorByVelocity, true);
			if (EditorGUI.EndChangeCheck()) {
				particles.ColorByLife.Update();
				particles.ColorByVelocity.Update();
			}

			// Noise
			// Using Vector4Field because for some reason PropertyField renders an array
			// Also have to manually draw the header because it doesn't like Popup or Vector4Fields
			EditorGUILayout.Space();
			headerStyle = GUI.skin.label;
			headerStyle.fontStyle = FontStyle.Bold;
			EditorGUILayout.LabelField("Noise", headerStyle);

			_noiseTypeIdx = particles.NoiseType;
			_noiseAmplitude = particles.NoiseAmplitude;
        	_noiseScale = particles.NoiseScale;
        	_noiseOffset = particles.NoiseOffset;
        	_noiseOffsetSpeed = particles.NoiseOffsetSpeed;

			_noiseTypeIdx = EditorGUILayout.Popup("Noise Type", _noiseTypeIdx, _noiseType);
			_noiseAmplitude = EditorGUILayout.Vector3Field("Noise Amplitude", _noiseAmplitude);
			_noiseScale = EditorGUILayout.Vector3Field("Noise Scale", _noiseScale);
			_noiseOffset = EditorGUILayout.Vector4Field("Noise Offset", _noiseOffset);
			_noiseOffsetSpeed = EditorGUILayout.Vector4Field("Noise Offset Speed", _noiseOffsetSpeed);
			
			particles.NoiseType = _noiseTypeIdx;
			particles.NoiseAmplitude = _noiseAmplitude;
			particles.NoiseScale = _noiseScale;
			particles.NoiseOffset = _noiseOffset;
			particles.NoiseOffsetSpeed = _noiseOffsetSpeed;

			EditorUtility.SetDirty(particles);

			serializedObject.ApplyModifiedProperties();

		}

	}
}

