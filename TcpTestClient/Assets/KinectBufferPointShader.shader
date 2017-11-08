Shader "Unlit/KinectBufferPointShader"
{
	Properties
	{
        _NearPointSize("Point Size", Range(0.01, 0.001)) = .003
        _FarPointSize("Point Size", Range(0.01, 0.001)) = .0045
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

			Cull Off
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			
			#include "UnityCG.cginc" 
            
            StructuredBuffer<float2> _DepthTable;

			struct v2g
			{
				float4 pos : SV_Position;
                float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
                float cardSize : TEXCOORD2;
                float baseDepth : TEXCOORD3;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
                float2 cardUv : TEXCOORD1;
			};

            sampler2D _DepthTex;
            float _NearPointSize;
            float _FarPointSize;
			float _MaxDistance;
			float _MinDistance;
			float _MaxAng;
			float _MinAng;
			float _CardUvScale;

            float4x4 _MasterTransform;

            float2 GetUvFromId(uint id)
            {
                float x = (float)(id % 512) / 424;
                float y = (float)(id / 424) / 512;
                return float2(x, y);
            }

            v2g vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
            {
                float2 uvsFromId = GetUvFromId(instanceId);
                float depthVal = tex2Dlod(_DepthTex, float4(uvsFromId, 0, 0));
                float2 xyVal = _DepthTable[instanceId] * depthVal;

                float3 basePos = float3(xyVal, depthVal);
				v2g o;
				o.pos = mul(_MasterTransform, float4(basePos, 1));
                o.baseDepth = depthVal;
				o.viewDir = normalize(WorldSpaceViewDir(o.pos));
				return o;
			}

			[maxvertexcount(4)]
			void geo(point v2g p[1], inout TriangleStream<g2f> triStream)
			{
				float4 vertBase = p[0].pos;
				float4 vertBaseClip = UnityObjectToClipPos(vertBase);
                float depth = (p[0].baseDepth - _MinDistance) / (_MaxDistance - _MinDistance);
				float size = lerp(_NearPointSize, _FarPointSize, depth);

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
				o.vertex = topVertB;
                o.cardUv = float2(0, 0);
				triStream.Append(o);

				o.vertex = topVertA;
                o.cardUv = float2(1, 0); 
				triStream.Append(o);

				o.vertex = bottomVertB;
                o.cardUv = float2(0, 1);
				triStream.Append(o);
                 
				o.vertex = bottomVertA;
                o.cardUv = float2(1, 1);
				triStream.Append(o);
			}
			
			float4 frag (g2f i) : SV_Target
			{
                return 1;
			}
			ENDCG
		}
	}
}
