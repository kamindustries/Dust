Shader "Dust/Instanced"
{
	Properties
	{
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Scale("Metallic", Vector) = (1,1,1)
	}
		SubShader
		{
			//Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
			Tags{ "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model
			#pragma surface surf Standard addshadow fullforwardshadows
			#pragma multi_compile_instancing
			#pragma instancing_options procedural:setup
			#pragma target 5.0
			#include "DustParticleSystemCommon.cginc"

			struct Input 
			{
				float2 uv_MainTex;
			};

			fixed4 _Color;
			sampler2D _MainTex;
			half _Glossiness;
			half _Metallic;
			float3 _Scale;
			int _NumInstances;
			int _NumParticles;

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				StructuredBuffer<ParticleStruct> dataBuffer;

				uint GetID() 
				{
					return uint(rand(float2(unity_InstanceID, unity_InstanceID+.5)) * _NumInstances);
				}

				void setup()
				{
					// Get a random particle if there's more of them than instances
					uint id = unity_InstanceID;
					if (_NumParticles > _NumInstances) id = GetID();

					float3 position = dataBuffer[id].pos;
					float3 scale = _Scale;

					unity_ObjectToWorld._11_21_31_41 = float4(scale.x, 0, 0, 0);
					unity_ObjectToWorld._12_22_32_42 = float4(0, scale.y, 0, 0);
					unity_ObjectToWorld._13_23_33_43 = float4(0, 0, scale.z, 0);
					unity_ObjectToWorld._14_24_34_44 = float4(position.xyz, 1);
					unity_WorldToObject = unity_ObjectToWorld;
					unity_WorldToObject._14_24_34 *= -1;
					unity_WorldToObject._11_22_33 = 1.0f / unity_WorldToObject._11_22_33;
				}
			#endif

			void surf(Input IN, inout SurfaceOutputStandard o)
			{
				float4 col = float4(1.0, 1.0, 1.0, 1.0);
				float2 coord = IN.uv_MainTex;

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				uint id = unity_InstanceID;
				if (_NumParticles > _NumInstances) id = GetID();
				col = dataBuffer[id].cd;

			#else
				col = float4(0, 0, 1, 1);
			#endif
				fixed4 c = tex2D(_MainTex, coord) * _Color * col;
				o.Albedo = c.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			}
			ENDCG
		}
	FallBack "Diffuse"
}
