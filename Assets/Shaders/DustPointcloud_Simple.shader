Shader "Dust/Pointcloud_Simple"
{
	SubShader
	{
		Pass
	{

		CGPROGRAM
		#pragma target 5.0

		#pragma vertex vert
		#pragma fragment frag

		#include "UnityCG.cginc"
		#include "DustParticleSystemCommon.cginc"
		
		StructuredBuffer<DustParticle> dataBuffer;

		struct ps_input 
		{
			float4 pos : SV_POSITION;
			float4 cd : COLOR;
		};

		ps_input vert(uint id : SV_VertexID)
		{
			ps_input o;
			float3 worldPos = dataBuffer[id].pos;
			o.pos = mul(UNITY_MATRIX_VP, float4(worldPos,1.0f));
			o.cd = dataBuffer[id].cd;

			return o;
		}

		//Pixel function returns a solid color for each point.
		float4 frag(ps_input i) : COLOR
		{
			return i.cd;
		}

			ENDCG

		}
		}

		Fallback Off
}