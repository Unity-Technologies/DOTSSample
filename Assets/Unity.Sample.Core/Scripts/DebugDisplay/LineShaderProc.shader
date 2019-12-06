Shader "Instanced/LineShaderProc" {
	Properties
	{
	}
	SubShader
	{
		Pass
		{
			Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }


			ZWrite off
			ZTest Always
			Cull off
			//Blend One One
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM

			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
			#pragma target 4.5

			#include "UnityCG.cginc"

			StructuredBuffer<float4> colorBuffer;
			float4 Palette(uint index)
			{
				return colorBuffer[index];
			}

			float4 color; // glyph scale in world (x,y) and on texture (z,w)
			float4 scales;

			struct instanceData
			{
				float4 begin;
				float4 end;
			};

			StructuredBuffer<instanceData> positionBuffer;

			struct v2f
			{
				float4 pos : SV_POSITION;
				float4 color : TEXCOORD0;
			};

			v2f vert(uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
			{
				float4 pos = (vid & 1) ? positionBuffer[vid >> 1].end : positionBuffer[vid >> 1].begin;
				float4 worldPos = float4(pos.xyz, 1);
				float4 projectionPos = mul(UNITY_MATRIX_VP, worldPos);
				v2f o;
				o.pos = projectionPos;
				o.color = Palette(pos.w);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				return i.color;
			}

			ENDCG
		}
	}
}
