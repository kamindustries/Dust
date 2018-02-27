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
        SerializedProperty GravityModifier;

		// Shape
        SerializedProperty Emission;
        SerializedProperty EmissionSize;
        SerializedProperty InitialSpeed;
		SerializedProperty ScatterSphereVolume;

		// Color
        SerializedProperty StartColor;
        SerializedProperty ColorByLife;
        SerializedProperty ColorByVelocity;

		// Noise
        SerializedProperty NoiseAmplitude;
        SerializedProperty NoiseScale;
        SerializedProperty NoiseOffset;

		string[] _emitterVelocity = new string[] {"Rigid body", "Transform"};
		int _emitterVelocityIdx = 0;

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
			EmissionSize = serializedObject.FindProperty("EmissionSize");
			InitialSpeed = serializedObject.FindProperty("InitialSpeed");
			ScatterSphereVolume = serializedObject.FindProperty("ScatterSphereVolume");

			// Color
			StartColor = serializedObject.FindProperty("StartColor");
			ColorByLife = serializedObject.FindProperty("ColorByLife");
			ColorByVelocity = serializedObject.FindProperty("ColorByVelocity");

			// Noise
			NoiseAmplitude = serializedObject.FindProperty("NoiseAmplitude");
			NoiseScale = serializedObject.FindProperty("NoiseScale");
			NoiseOffset = serializedObject.FindProperty("NoiseOffset");
			

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
			EditorGUILayout.PropertyField(Emission);
			EditorGUILayout.PropertyField(EmissionSize);
			EditorGUILayout.PropertyField(InitialSpeed);
			EditorGUILayout.PropertyField(ScatterSphereVolume);

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
			EditorGUILayout.PropertyField(NoiseAmplitude);
			EditorGUILayout.PropertyField(NoiseScale);
			EditorGUILayout.PropertyField(NoiseOffset);
			
			EditorUtility.SetDirty(particles);

			serializedObject.ApplyModifiedProperties();

		}

	}
}

