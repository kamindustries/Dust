using UnityEditor;
using UnityEngine;
using System.Collections;

namespace Dust {

	[CustomEditor(typeof(DustParticleSystem))]
	[CanEditMultipleObjects]
	public class DustParticleSystemEditor : Editor {

        SerializedProperty Compute;

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
        SerializedProperty InitialSpeed;
        SerializedProperty Jitter;
        SerializedProperty RandomizeDirection;
        SerializedProperty RandomizeRotation;
        SerializedProperty AlignToInitialDirection;
        SerializedProperty EmissionSize;
		SerializedProperty ScatterVolume;
        SerializedProperty EmissionMeshRenderer;

		// Size
		SerializedProperty SizeOverLife;

		// Rotation
        SerializedProperty AlignToDirection;
        SerializedProperty RotationOverLifetime;
		
		// Color
        SerializedProperty StartColor;
        SerializedProperty ColorOverLife;
        SerializedProperty ColorOverVelocity;
        SerializedProperty RandomizeColor;
        SerializedProperty UseMeshEmitterColor;

		// Noise
        bool _noiseToggle = false;
		string[] _noiseType = new string[] {"2D", "3D", "4D"};
		int _noiseTypeIdx = 1;
        Vector3 _noiseAmplitude = new Vector4(0f,0f,0f);
        Vector3 _noiseScale = new Vector4(1f,1f,1f);
        Vector4 _noiseOffset = new Vector4(0f,0f,0f,0f);
        Vector4 _noiseOffsetSpeed = new Vector4(0f,0f,0f,0f);

		// Foldouts
		bool showParticles = true;
		bool showVelocity = true;
		bool showShape = true;
		bool showSize = true;
		bool showRotation = true;
		bool showColor = true;
		bool showNoise = true;


		GUIStyle foldStyle;

		void OnEnable() 
		{
			Compute = serializedObject.FindProperty("Compute");
			
			// Particles
			Mass = serializedObject.FindProperty("Mass");
			Momentum = serializedObject.FindProperty("Momentum");
			Lifespan = serializedObject.FindProperty("Lifespan");
			PreWarmFrames = serializedObject.FindProperty("PreWarmFrames");

			// Velocity
			InheritVelocity = serializedObject.FindProperty("InheritVelocity");
			GravityModifier = serializedObject.FindProperty("GravityModifier");

			// Shape
			InitialSpeed = serializedObject.FindProperty("InitialSpeed");
			Jitter = serializedObject.FindProperty("Jitter");
			RandomizeDirection = serializedObject.FindProperty("RandomizeDirection");
			RandomizeRotation = serializedObject.FindProperty("RandomizeRotation");
			AlignToInitialDirection = serializedObject.FindProperty("AlignToInitialDirection");
			EmissionSize = serializedObject.FindProperty("EmissionSize");
			ScatterVolume = serializedObject.FindProperty("ScatterVolume");
			EmissionMeshRenderer = serializedObject.FindProperty("EmissionMeshRenderer");

			// Size
			SizeOverLife = serializedObject.FindProperty("SizeOverLife");

			// Rotation
			AlignToDirection = serializedObject.FindProperty("AlignToDirection");
			RotationOverLifetime = serializedObject.FindProperty("RotationOverLifetime");

			// Color
			StartColor = serializedObject.FindProperty("StartColor");
			ColorOverLife = serializedObject.FindProperty("ColorOverLife");
			ColorOverVelocity = serializedObject.FindProperty("ColorOverVelocity");
			RandomizeColor = serializedObject.FindProperty("RandomizeColor");
			UseMeshEmitterColor = serializedObject.FindProperty("UseMeshEmitterColor");
			
			// Noise
		}



		public override void OnInspectorGUI()
		{
			// Style
			foldStyle = new GUIStyle(EditorStyles.foldout);
			foldStyle.fontStyle = FontStyle.Bold;

			var src = target as DustParticleSystem;

			EditorGUILayout.PropertyField(Compute);

			// Particles
			showParticles = EditorGUILayout.Foldout(showParticles, "Particles", foldStyle);
			if (showParticles) {
				EditorGUILayout.PropertyField(Mass);
				EditorGUILayout.PropertyField(Momentum);
				EditorGUILayout.PropertyField(Lifespan);
				EditorGUILayout.PropertyField(PreWarmFrames);
			}


			// Velocity
			EditorGUILayout.Space();
			showVelocity = EditorGUILayout.Foldout(showVelocity, "Velocity", foldStyle);
			if (showVelocity) {
				EditorGUILayout.PropertyField(InheritVelocity);
				_emitterVelocityIdx = src.EmitterVelocity;
				_emitterVelocityIdx = EditorGUILayout.Popup("Emitter Velocity", _emitterVelocityIdx, _emitterVelocity);
				src.EmitterVelocity = _emitterVelocityIdx;
				EditorGUILayout.PropertyField(GravityModifier);
			}

			// Shape
			EditorGUILayout.Space();
			showShape = EditorGUILayout.Foldout(showShape, "Shape", foldStyle);
			if (showShape) {
				_shapeIdx = src.Shape;
				_shapeIdx = EditorGUILayout.Popup("Shape", _shapeIdx, _shape);
				src.Shape = _shapeIdx;
				switch(_shapeIdx) {
					case 0:
						EditorGUILayout.PropertyField(EmissionSize);
						EditorGUILayout.PropertyField(ScatterVolume);
						break;
					case 1:
						EditorGUILayout.PropertyField(EmissionMeshRenderer, new GUIContent("Mesh Renderer"));
						break;
				}
				EditorGUI.BeginChangeCheck();
					src.Emission = EditorGUILayout.IntSlider("Emission", src.Emission, 0, src.MaxVerts);
				if (EditorGUI.EndChangeCheck()) { src.UpdateKernelArgs(); }			
				EditorGUILayout.PropertyField(InitialSpeed);
				EditorGUILayout.PropertyField(Jitter);
				EditorGUILayout.PropertyField(RandomizeDirection);
				EditorGUILayout.PropertyField(RandomizeRotation);
				EditorGUILayout.PropertyField(AlignToInitialDirection);
			}

			// Size
			EditorGUILayout.Space();
			showSize = EditorGUILayout.Foldout(showSize, "Size", foldStyle);
			if (showSize) {			
				EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(SizeOverLife, true);
				if (EditorGUI.EndChangeCheck()) {
					src.SizeOverLife.Update();
				}
			}

			// Rotation
			EditorGUILayout.Space();
			showRotation = EditorGUILayout.Foldout(showRotation, "Rotation", foldStyle);
			if (showRotation) {						
				EditorGUILayout.PropertyField(AlignToDirection);
				EditorGUILayout.PropertyField(RotationOverLifetime);
			}

			// Color
			EditorGUILayout.Space();
			showColor = EditorGUILayout.Foldout(showColor, "Color", foldStyle);
			if (showColor) {					
				EditorGUILayout.PropertyField(StartColor);
				// Interactive gradient editor
				EditorGUI.BeginChangeCheck();
					EditorGUILayout.PropertyField(ColorOverLife, true);
					EditorGUILayout.PropertyField(ColorOverVelocity, true);
				if (EditorGUI.EndChangeCheck()) {
					src.ColorOverLife.Update();
					src.ColorOverVelocity.Update();
				}
				EditorGUILayout.PropertyField(RandomizeColor);
				EditorGUILayout.PropertyField(UseMeshEmitterColor);
			}

			// Noise
			// Using Vector4Field because for some reason PropertyField renders an array
			// Also have to manually draw the header because it doesn't like Popup or Vector4Fields
			EditorGUILayout.Space();
			showNoise = EditorGUILayout.Foldout(showNoise, "Noise", foldStyle);
			if (showNoise) {					
				_noiseToggle = src.NoiseToggle;
				_noiseTypeIdx = src.NoiseType;
				_noiseAmplitude = src.NoiseAmplitude;
				_noiseScale = src.NoiseScale;
				_noiseOffset = src.NoiseOffset;
				_noiseOffsetSpeed = src.NoiseOffsetSpeed;

				_noiseToggle = EditorGUILayout.Toggle("Enable Noise", _noiseToggle);
				_noiseTypeIdx = EditorGUILayout.Popup("Noise Type", _noiseTypeIdx, _noiseType);
				_noiseAmplitude = EditorGUILayout.Vector3Field("Noise Amplitude", _noiseAmplitude);
				_noiseScale = EditorGUILayout.Vector3Field("Noise Scale", _noiseScale);
				_noiseOffset = EditorGUILayout.Vector4Field("Noise Offset", _noiseOffset);
				_noiseOffsetSpeed = EditorGUILayout.Vector4Field("Noise Offset Speed", _noiseOffsetSpeed);
				
				src.NoiseToggle = _noiseToggle;
				src.NoiseType = _noiseTypeIdx;
				src.NoiseAmplitude = _noiseAmplitude;
				src.NoiseScale = _noiseScale;
				src.NoiseOffset = _noiseOffset;
				src.NoiseOffsetSpeed = _noiseOffsetSpeed;
			}

			EditorUtility.SetDirty(src);

			serializedObject.ApplyModifiedProperties();

		}

	}
}

