Shader "Instanced/DebugTextShader"
{
    Properties
    {
        _FontTex ("Font (RGB)", 2D) = "white" {}
	}
    CGINCLUDE

        uint GetUint(Texture2D tex, float4 size, uint x, uint y)
        {
            uint w,h;
            tex.GetDimensions(w,h);
            y = h - 1 - y;
            float2 uv = (float2(x,y)+0.5) * size.xy;
            uint4 bytes = tex.Load(uint3(x,y,0)) * 255;
            return (bytes.z&0xff) | ((bytes.y&0xff)<<8) | ((bytes.x&0xff)<<16) | ((bytes.w&0xff)<<24);
        }
        
        uint GetUshort(Texture2D tex, float4 size, uint x, uint y)
        {
            int bits = (int)(x&1) * 16;
            return (GetUint(tex, size, x/2,y) >> bits) & 0xFFFF;
        }

    ENDCG
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

        Texture2D _CellTex;
        Texture2D _FontTex;
        uniform float4 _CellTex_TexelSize;
        uniform float4 _FontTex_TexelSize;

		// TODO: molokai or tango

		float4 scales; // glyph scale in world (x,y) and on texture (z,w)

		StructuredBuffer<float4> colorBuffer;
		float4 Palette(uint index)
		{
			return colorBuffer[index];
		}

		uint textBufferWidth;
		StructuredBuffer<uint> textBuffer;
		struct Cell
		{
			uint code;
			uint color;
			uint side;
		};

		Cell GetCell(uint x, uint y)
		{
			uint u = textBuffer[y * (3840/8) + x];
			Cell cell;
			cell.code = (u & 0x7f) | ((u >> 16) << 7);
			cell.color = (u >> 8) & 0x7f;
			cell.side = (u >> 15) & 1;
			return cell;
		}


		struct instanceData
		{
			float3 worldPosition;
			float2 firstCell;
			float2 cellSize;
			uint   useWorldMatrix;
		};
		StructuredBuffer<instanceData> positionBuffer;

		struct v2f
		{
			float4 pos       : SV_POSITION;
			float2 screenPos : TEXCOORD0;
			float  instanceVal  : TEXCOORD1;
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
			float2 screenPosition;
			if (positionBuffer[instID].useWorldMatrix != 0)
			{
				float4 projectionPosition = mul(UNITY_MATRIX_VP, float4(positionBuffer[instID].worldPosition, 1));
				screenPosition.x = (projectionPosition.x / projectionPosition.w) * 0.5 + 0.5;
				screenPosition.y = -(projectionPosition.y / projectionPosition.w) * 0.5 + 0.5;
				screenPosition.x /= scales.z;
				screenPosition.y /= scales.w;
			}
			else
				screenPosition = positionBuffer[instID].worldPosition.xy;
			float2 pos = screenPosition * scales.zw;
			float2 scale = positionBuffer[instID].cellSize * scales.xy;

			// Generate position
			float2 p = pos + v_pos * scale;
			p.y = 1.0 - p.y;
			p = float2(-1, -1) + p * 2.0;

			v2f o;
			o.pos = float4(p.xy, 1, 1);
			o.screenPos = screenPosition;
			o.instanceVal = instID;
			return o;
		}

        fixed4 frag (v2f i) : SV_Target
        {
			int instID = i.instanceVal;
                
            int2 pixel_xy = i.pos.xy - i.screenPos;
            pixel_xy.y = _ScreenParams.y-1 - pixel_xy.y;
//			pixel_xy /= 2;
			int2 cell_xy = int2(pixel_xy.x >> 3, pixel_xy.y >> 4);

			int2 firstCell = positionBuffer[i.instanceVal].firstCell;

            Cell cell = GetCell(firstCell.x + cell_xy.x, firstCell.y + cell_xy.y);

            if((cell.color & 0xf) == (cell.color >> 4)&0x7)
              discard;

			uint coord_x = pixel_xy.x & 7;
            uint coord_y = pixel_xy.y & 15;
            uint coord_col = cell.code & 255;
            uint coord_row = cell.code >> 8;
            uint coord_side = cell.side;

            uint bitInCell = coord_y * 8 + coord_x;
            uint pixelInQuad  = bitInCell >> 5; 
            uint pixelInQuadX = pixelInQuad & 1; 
            uint pixelInQuadY = pixelInQuad >> 1; 
            // each 32x32 pixels is a UNICODE row of 8x16 pixel cells (256 code points with the same high byte)
            // inside each 32x32, each 2x2 is one 8x16 pixel cell of one code point. so it's 16x16 cells total
            // code points wider than 8x16 (e.g. chinese) have the right half in another 32x32 pixels
            uint encodedX = pixelInQuadX + 2 * (coord_col & 15) + 32 * (coord_row & 15);
            uint encodedY = pixelInQuadY + 2 * (coord_col >> 4) + 32 * (coord_row >> 4) + coord_side * 512;

            int2 encoded_xy = int2(encodedX, encodedY);

            uint dwordVal = GetUint(_FontTex, _FontTex_TexelSize, encoded_xy.x, encoded_xy.y);
            
            uint bitInDword = bitInCell & 31;
            uint color = (dwordVal >> bitInDword) & 1;
			if(color)
				return Palette((cell.color >> 4) & 0x07);
			else
				return Palette((cell.color >> 0) & 0x0F);
		}
ENDCG
      }
    }
}
