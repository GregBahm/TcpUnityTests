Shader "Unlit/BlissGroundplaneShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_DisplacementMap ("Displacement", 2D) = "black" {}
		_DisplacementAmount ("Displacement Amount", Float) = 1
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 worldPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			sampler2D _DisplacementMap;
			float _DisplacementAmount;

			float4 _LowColor;
			float4 _DistanceColor;
			float _TransitionParam;
			
			v2f vert (appdata v)
			{
				float displacementSample = tex2Dlod(_DisplacementMap, float4(v.uv, 0, 0)).x;
				v.vertex.y += displacementSample * _DisplacementAmount;

				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				return o;
			}

			float GetTransParam(float3 worldPos)
			{
				float theArc = atan2(worldPos.x, worldPos.z);
				return theArc - (_TransitionParam * 3.141) * 2 + 3.141;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float transParam = GetTransParam(i.worldPos);
				clip(transParam);

				fixed4 col = tex2D(_MainTex, i.uv);
				col *= _LowColor;
				float distToCenter = length(i.uv - .5) * 2;
				col.rgb = lerp(col.rgb, _DistanceColor.xyz, pow(saturate(distToCenter), 2) * _DistanceColor.a);
				return col;
			}
			ENDCG
		}
	}
}
