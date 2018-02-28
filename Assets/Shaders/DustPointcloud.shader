// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Dust/Pointcloud"
{
	SubShader
	{
		Pass
		{
			Tags {"LightMode"="ForwardBase"}
			LOD 100
			CGPROGRAM
			#pragma target 5.0
			#pragma vertex vert
			#pragma fragment frag
            // compile shader into multiple variants, with and without shadows
            // (we don't care about any lightmaps yet, so skip these variants)
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            // shadow helper functions and macros
			#include "UnityCG.cginc"
            #include "AutoLight.cginc"
			#include "UnityLightingCommon.cginc" // for _LightColor0

			#include "DustParticleSystemCommon.cginc"
			
			StructuredBuffer<ParticleStruct> dataBuffer;
			

			struct v2f 
			{
				float2 uv : TEXCOORD0;
                SHADOW_COORDS(1) // put shadows data into TEXCOORD1
				fixed4 baseCd : COLOR0;
                fixed3 diff : COLOR1;
                fixed3 ambient : COLOR2;
                float4 pos : SV_POSITION;
			};

			v2f vert(uint id : SV_VertexID, appdata_base v)
			{
				v2f o;
				float3 worldPos = dataBuffer[id].pos;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));

				// lighting
				o.uv = v.texcoord;
                // half3 worldNormal = UnityObjectToWorldNormal(half3(0,1,0)); //arbitrary normal for points
                half3 worldNormal = half3(0,1,0); //arbitrary normal for points
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl * _LightColor0.rgb;
                o.ambient = ShadeSH9(half4(worldNormal,1));
				TRANSFER_SHADOW(o);

				o.baseCd = dataBuffer[id].cd;

				return o;
			}

			//Pixel function returns a solid color for each point.
			float4 frag(v2f i) : COLOR
			{
				fixed4 col = i.baseCd;
				fixed shadow = SHADOW_ATTENUATION(i);
				fixed3 lighting = i.diff * shadow + i.ambient;
                col.rgb *= lighting;
                return col;
			}
			ENDCG
		}

        // Shadow caster pass
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #include "UnityCG.cginc"
			#include "DustParticleSystemCommon.cginc"

			StructuredBuffer<ParticleStruct> dataBuffer;

			struct v2f_shdw
			{
				float4 pos : SV_POSITION;
			};

			v2f_shdw vert(uint id : SV_VertexID)
			{
				v2f_shdw o;
				float3 worldPos = dataBuffer[id].pos;
				o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));
				return o;
			}

            float4 frag(v2f_shdw i) : SV_Target
            {
                SHADOW_CASTER_FRAGMENT(i)
            }
            ENDCG
        }
	}	
	Fallback Off
}