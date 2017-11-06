Shader "Unlit/BlissGrassShader"
{
	Properties
	{
		_Texture("Texture", 2D) = "white" {}
		_DisplacementMap("Displacement Map", 2D) = "black" {}
		_DisplacementAmount("Displacement Amount", Float) = 1
	}
	SubShader
	{
		Cull Off
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geo
			#pragma fragment frag
			#pragma target 5.0 
			
			#include "UnityCG.cginc"

			struct FixedWheatData
			{
				float2 PlanePos;
				float2 PlaneTangent;
			};

			struct VariableWheatData
			{
				float3 StalkNormal;
				float2 PlanarVelocity;
			};

			struct v2g
			{
				float4 BasePoint : TEXCOORD0;
				float2 PlaneTangent : TEXCOORD1;
				float4 StalkNormal : TEXCOORD2;
				float PlaneDistToCenter: TEXCOORD4;
				float2 worldUv : TEXCOORD5;
				float3 planeNormal : TEXCOORD6;
			};

			struct g2f
			{
				float4 vertex : SV_POSITION;
				float2 uvs : TEXCOORD0;
				float planeDistToCenter : TEXCOOR1;
				float3 worldPos: TEXCOORD3;
				float3 viewDir : TEXCOORD4;
				float2 worldUv : TEXCOORD5;
				float shadeRand : TEXCOORD6;
				float3 planeNormal : TEXCOORD7;
			};

			StructuredBuffer<FixedWheatData> _FixedDataBuffer;
			StructuredBuffer<VariableWheatData> _VariableDataBuffer; 

			sampler2D _Texture;
			sampler2D _DisplacementMap;
			float _DisplacementAmount;

			float _CardWidth;
			float2 _PlayspaceScale;
			float _CardHeight;
			float3 _HighColor;
			float3 _LowColor;
			float _HeightOffset;

			float4 _DistanceColor;
			float _TransitionParam;

			float2 GetWorldUv(float2 planePos)
			{
				return 1 - planePos;// / _PlayspaceScale + .5;
			}

			float3 GetNormal(float4 baseTexcoord)
			{
				float4 xOffset = float4(0.05, 0, 0, 0);
				float4 yOffset = float4(0, 0.05, 0, 0);
				float heightA = tex2Dlod(_DisplacementMap, baseTexcoord + xOffset).x;
				float heightB = tex2Dlod(_DisplacementMap, baseTexcoord - xOffset).x;
				float heightC = tex2Dlod(_DisplacementMap, baseTexcoord + yOffset).x;

				float3 pointA = float3(.2, heightA, 0);
				float3 pointB = float3(-.2, heightB, 0);
				float3 pointC = float3(0, heightC, .2);

				float3 diffA = pointC - pointA;
				float3 diffB = pointC - pointB;
				float3 ret = cross(diffA, diffB);
				return normalize(ret);
			}

			v2g vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
			{
				FixedWheatData fixedData = _FixedDataBuffer[instanceId];
				VariableWheatData variableData = _VariableDataBuffer[instanceId];

				float2 worldUv = GetWorldUv(fixedData.PlanePos);
				float displacementSample = tex2Dlod(_DisplacementMap, float4(worldUv, 0, 0)).x;
				float baseY = displacementSample * _DisplacementAmount + _HeightOffset;

				float4 basePoint = float4(fixedData.PlanePos.x, baseY, fixedData.PlanePos.y, 1);
				basePoint.xz = (basePoint.xz - .5) * 2;
				basePoint.xz *= _PlayspaceScale;

				float noisyCardHeight = _CardHeight + abs(_CardHeight * fixedData.PlaneTangent.x);
				float4 stalkNormal = float4(variableData.StalkNormal * noisyCardHeight, 1);

				v2g o;
				o.worldUv = worldUv;
				o.BasePoint = basePoint;
				o.PlaneTangent = fixedData.PlaneTangent;
				o.StalkNormal = stalkNormal;
				o.PlaneDistToCenter = length(fixedData.PlanePos - .5) * 2;
				o.planeNormal = GetNormal(float4(worldUv, 0, 0));
				return o;
			}

			[maxvertexcount(4)]
			void geo(point v2g p[1], inout TriangleStream<g2f> triStream)
			{

				float2 card = p[0].PlaneTangent * _CardWidth;
				float4 topPointA = float4(-card.x, 0, -card.y, 0) + p[0].StalkNormal;
				float4 topPointB = float4(card.x, 0, card.y, 0) + p[0].StalkNormal;
				float4 bottomPointA = float4(-card.x, 0, -card.y, 0);
				float4 bottomPointB = float4(card.x, 0, card.y, 0);

				g2f o;
				o.shadeRand = p[0].PlaneTangent.y;
				o.worldUv = p[0].worldUv;
				o.planeDistToCenter = p[0].PlaneDistToCenter;
				o.planeNormal = p[0].planeNormal;
				float4 objPos = topPointA + p[0].BasePoint;
				o.vertex = UnityObjectToClipPos(objPos);
				o.viewDir = ObjSpaceViewDir(objPos);
				o.worldPos = mul(unity_ObjectToWorld, objPos);
				o.uvs = float2(0, 1);
				triStream.Append(o);
				
				objPos = topPointB + p[0].BasePoint;
				o.vertex = UnityObjectToClipPos(objPos);
				o.viewDir = ObjSpaceViewDir(objPos);
				o.worldPos = mul(unity_ObjectToWorld, objPos);
				o.uvs = float2(1, 1);
				triStream.Append(o);

				objPos = bottomPointA + p[0].BasePoint;
				o.vertex = UnityObjectToClipPos(objPos);
				o.viewDir = ObjSpaceViewDir(objPos);
				o.worldPos = mul(unity_ObjectToWorld, objPos);
				o.uvs = float2(0, 0);
				triStream.Append(o);

				objPos = bottomPointB + p[0].BasePoint;
				o.vertex = UnityObjectToClipPos(objPos);
				o.viewDir = ObjSpaceViewDir(objPos);
				o.worldPos = mul(unity_ObjectToWorld, objPos);
				o.uvs = float2(1, 0);
				triStream.Append(o);
			}

			float GetTransParam(float3 worldPos)
			{
				float theArc = atan2(worldPos.x, worldPos.z);
				return theArc - (_TransitionParam * 3.141) * 2 + 3.141;
			}

			fixed4 frag (g2f i) : SV_Target
			{
				//return fresnel;
				//return float4(i.planeNormal, 1);
				
				float transParam = GetTransParam(i.worldPos);
				clip(transParam);

				float4 textureSample = tex2D(_Texture, i.worldUv);

				float quadDistToCenter = 1 - length(i.uvs - .5) * 2;
				float cardAlpha = 1 - textureSample.a;
				clip(quadDistToCenter - .01 - cardAlpha);

				float3 highColor = _HighColor * textureSample.xyz;
				float3 shadeRand = lerp(float3(1, .9, .5), 1, abs(i.shadeRand));
				highColor = pow(highColor, 2) * 5 * shadeRand;
				float fresnel = dot(normalize(i.viewDir), normalize(i.planeNormal)) / 2 + .5;

				//highColor += highColor * fresnel;
				float3 lowColor = _LowColor * textureSample.xyz;
				float3 color = lerp(lowColor, highColor, i.uvs.y);
				color = lerp(color, _DistanceColor.xyz, pow(saturate(i.planeDistToCenter), 2) * _DistanceColor.a);
				return float4(color, 1);
			}
			ENDCG
		}
	}
}
