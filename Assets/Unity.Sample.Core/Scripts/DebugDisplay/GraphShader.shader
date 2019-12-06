Shader "Instanced/GraphShader" {
	Properties{
	}
		SubShader{

			Pass{

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

		// TODO: molokai or tango

		float4 scales; // glyph scale in world (x,y) and on texture (z,w)

		StructuredBuffer<float> sampleBuffer;

		struct GraphInstanceDataSample
		{
			uint color;       // color of the sample
			uint firstIndex;  // first sample index in range to display
			uint indexMask;   // AND sample index with this to make it wrap around
			float indexMul;   // multiply the pixel.x by this,
			float indexAdd;   // and then by this to get the sample index.
			float sampleMul;  // multiply the sample by this,
			float sampleAdd;  // and then add this to get the pixel.y
		};

		struct GraphInstanceData
		{
			float2 screenPosition;
			float2 cellSize;
			uint   frameColor;
			uint   samples;
			GraphInstanceDataSample data[2];
		};
		StructuredBuffer<GraphInstanceData> instanceBuffer;

		StructuredBuffer<float4> colorBuffer;
		float4 Palette(uint index)
		{
			return colorBuffer[index];
		}

		struct v2f
		{
			float4 pos      : SV_POSITION;
			float  instanceVal : TEXCOORD0;
		};

		v2f vert(uint vid : SV_VertexID, uint instanceID : SV_InstanceID)
		{
			// We just draw a bunch of vertices but want to pretend to
			// be drawing two-triangle quads. Build inst/vert id for this:
			int instID = vid / 6.0;
			int vertID = vid - instID * 6;

			// Generates (0,0) (1,0) (1,1) (1,1) (1,0) (0,0) from vertID
			float4 v_pos = saturate(float4(2 - abs(vertID - 2), 2 - abs(vertID - 3), 0, 0));

			// Read instance data
			float2 pos   = instanceBuffer[instID].screenPosition * scales.zw;
			float2 scale = instanceBuffer[instID].cellSize * scales.xy;

			// Generate position
			float2 p = pos + v_pos * scale;
			p.y = 1.0 - p.y;
			p = float2(-1, -1) + p * 2.0;

			v2f o;
			o.pos = float4(p.xy, 1, 1);
			o.instanceVal = instID;
			return o;
		}

		fixed4 frag(v2f i) : SV_Target
		{
			int instID = i.instanceVal;

		    int2 xy = i.pos.xy - instanceBuffer[instID].screenPosition;

			if(xy.x == 0)
				return Palette(instanceBuffer[instID].frameColor);
			if(xy.y == 0)
				return Palette(instanceBuffer[instID].frameColor);
			if(xy.x == instanceBuffer[instID].cellSize.x * 8 - 1)
				return Palette(instanceBuffer[instID].frameColor);
			if(xy.y == instanceBuffer[instID].cellSize.y * 16 - 1)
				return Palette(instanceBuffer[instID].frameColor);

			int2 subsamples = int2(5,5);
			int totalSubsamples = subsamples.x * subsamples.y;
			float2 step = float2(2.0 / subsamples.x, 1.0 / subsamples.y);

			uint ybits = 0xb33f86bf;

			fixed4 color = (((xy.x&7)==0)||((xy.y&15)==0)) ? fixed4(0.125,0.125,0.125,0.5) : fixed4(0, 0, 0, 0.5);
			for (uint i = 0; i < instanceBuffer[instID].samples; ++i)
			{
				int count = 0;
				for (int sx = 0; sx < subsamples.x; ++sx)
				{
					int xsub = (sx - subsamples.x / 2);
					float x = xy.x + xsub * step.x;
					
					x = x * instanceBuffer[instID].data[i].indexMul
					      + instanceBuffer[instID].data[i].indexAdd;

					// where to read from sample buffer?

					int f = instanceBuffer[instID].data[i].firstIndex;
					int s = (int)floor(x);
					int m = instanceBuffer[instID].data[i].indexMask;

					int s0 = f + ((s - 1) & m);
					int s1 = f + ((s)& m);
					int s2 = f + ((s + 1) & m);
					int s3 = f + ((s + 2) & m);

					// read four values from sample buffer

					float p0 = sampleBuffer[s0];
					float p1 = sampleBuffer[s1];
					float p2 = sampleBuffer[s2];
					float p3 = sampleBuffer[s3];

					float t = x - floor(x);
					float t2 = t * t;
					float t3 = t * t * t;

					float y = p1 + (p2 - p1) * t;
					//				 	  (-1 / 2.0 * p0 + 3 / 2.0 * p1 - 3 / 2.0 * p2 + 1 / 2.0 * p3) * x3
					//					+ (           p0 - 5 / 2.0 * p1 +     2.0 * p2 - 1 / 2.0 * p3) * x2
					//					+ (-1 / 2.0 * p0 +                1 / 2.0 * p2               ) * x
					//					+ (                          p1                              );

					y = y * instanceBuffer[instID].data[i].sampleMul + instanceBuffer[instID].data[i].sampleAdd;

					for (int sy = 0; sy < subsamples.y; ++sy)
					{
						int ysub = (sy - subsamples.y / 2) * 2 + (ybits & 1);
						float side = y - (xy.y + ysub * step.y);
						count += side > 0 ? 1 : -1;

						ybits >>= 1;
					}
				}
				float alpha = 1 - abs(count) / (float)totalSubsamples;
//				alpha = 1 - alpha * alpha;
  			    color = color * (1-alpha) + Palette(instanceBuffer[instID].data[i].color) * alpha;
			}
			return color;
		}

	ENDCG
}
	}
}
