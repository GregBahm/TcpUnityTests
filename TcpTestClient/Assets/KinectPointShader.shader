Shader "Unlit/KinectPointShader"
{
	Properties
	{
		_PointSize("Point Size", Range(0.05, 0.001)) = .1
		_MaxDistance("MaxDistance", Float) = 1.5
		_MinDistance("MinDistance", Float) = 1
		_MaxAng("MaxAng", Float) = 0.55
		_MinAng("MinAng", Float) = 0.4
		_CardUvScale("Card Uv Scale", Float) = .01
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			Cull Front
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			
			#include "UnityCG.cginc"

            StructuredBuffer<float2> _SomePointsBuffer;

			struct v2g
			{
				float4 rawVert : SV_Position;
				float3 viewDir : TEXCOORD1;
				float2 uv : TEXCOORD2;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float2 cardUv : TEXCOORD2;
				float depthKey : TEXCOORD1;
			};

            sampler2D _MainTex;
            sampler2D _DepthTex;
			float _PointSize;
			float _MaxDistance;
			float _MinDistance;
			float _MaxAng;
			float _MinAng;
			float _CardUvScale;

            v2g vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
            {
                float2 meshPos = _SomePointsBuffer[instanceId];
                float depthVal = tex2Dlod(_DepthTex, float4(meshPos.x, meshPos.y, 0, 0)).x;
				v2g o;
				o.uv = meshPos;
                meshPos.y *= -1;
				o.rawVert = float4(meshPos.x, meshPos.y, depthVal, 1);
				o.viewDir = normalize(WorldSpaceViewDir(o.rawVert));
				return o;
			}

            fixed3 YuvToRgb(fixed Y, fixed U, fixed V)
            {
                fixed R = 1.164 * (Y - 0.0625) + 1.596 * (V - .5);
                fixed G = 1.164 * (Y - 0.0625) - 0.813 * (V - .5) - 0.391 * (U - .5);
                fixed B = 1.164 * (Y - 0.0625) + 2.018 * (U - .5);
                return fixed3(R, G, B);
            }

			[maxvertexcount(4)]
			void geo(point v2g p[1], inout TriangleStream<g2f> triStream)
			{
				float4 vertBase = p[0].rawVert;
				float4 vertBaseClip = UnityObjectToClipPos(vertBase);
				float size = _PointSize;

				// Calc vert points
				float4 leftScreenOffset = float4(size, 0, 0, 0);
				float4 rightScreenOffset = float4(-size, 0, 0, 0);
				float4 topScreenOffset = float4(0, -size, 0, 0);
				float4 bottomScreenOffset = float4(0, size, 0, 0);

				float4 topVertA = leftScreenOffset + topScreenOffset + vertBaseClip;
				float4 topVertB = rightScreenOffset + topScreenOffset + vertBaseClip;
				float4 bottomVertA = leftScreenOffset + bottomScreenOffset + vertBaseClip;
				float4 bottomVertB = rightScreenOffset + bottomScreenOffset + vertBaseClip;

				g2f o;
				o.uv = p[0].uv;
				o.depthKey = vertBase.z;
				o.vertex = topVertB;
				o.cardUv = float2(0, 0);
				triStream.Append(o);
				o.cardUv = float2(1, 0);
				o.vertex = topVertA;
				triStream.Append(o);
				o.cardUv = float2(0, 1);
				o.vertex = bottomVertB;
				triStream.Append(o);
				o.cardUv = float2(1, 1);
				o.vertex = bottomVertA;
				triStream.Append(o);
			}
			
			fixed4 frag (g2f i) : SV_Target
			{
                clip(i.depthKey - .05);
				float centerDist = length(abs(i.cardUv - .5)) * 2;
				clip(-centerDist + 1);

                fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 rgbCol = YuvToRgb(col.g, col.r, col.b);
                return fixed4(rgbCol, 1);
			}
			ENDCG
		}
	}
}
