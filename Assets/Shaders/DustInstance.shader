Shader "Dust/Instanced"
{
	Properties
	{
		_Color("Color", Color) = (1.0, 1.0, 1.0, 1.0)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
		_Glossiness("Smoothness", Range(0,1)) = 0.5
		_Metallic("Metallic", Range(0,1)) = 0.0
		_Scale("Scale", Vector) = (1,1,1)
	}
		SubShader
		{
			//Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
			Tags{ "RenderType" = "Opaque" }
			LOD 200

			CGPROGRAM
			// Physically based Standard lighting model
			#pragma surface surf Standard vertex:vert addshadow fullforwardshadows
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
				StructuredBuffer<DustParticle> dataBuffer;

				uint GetID() 
				{
					return unity_InstanceID;
				}
			#endif
				
			void vert (inout appdata_full v)
			{
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				uint id = GetID();
				float4 pos = float4(v.vertex.xyz * dataBuffer[id].scale, 1.);
				v.vertex.xyz = mul(dataBuffer[id].rot, pos ).xyz;
				v.normal = mul(dataBuffer[id].rot, float4(v.normal,1.)).xyz;
			#endif
			}

			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				void setup()
				{
					// Get a random particle if there's more of them than instances
					uint id = GetID();
					float3 position = dataBuffer[id].pos;
					// float3 scale = _Scale;
					float3 scale = float3(1,1,1);

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
			#ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
				uint id = GetID();
				float4 col = dataBuffer[id].cd;
				float2 coord = IN.uv_MainTex;


				fixed4 c = tex2D(_MainTex, coord) * _Color * col;
				o.Albedo = c.rgb;
				o.Metallic = _Metallic;
				o.Smoothness = _Glossiness;
				o.Alpha = c.a;
			#endif
			}
			ENDCG
		}
	FallBack "Diffuse"
}
