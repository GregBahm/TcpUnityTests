Shader "Unlit/RgbKinnectImageShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
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
			};

			sampler2D _MainTex;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
            fixed3 YuvToRgb(fixed Y, fixed U, fixed V)
            {
                fixed R = 1.164 * (Y - 0.0625) + 1.596 * (V - .5);
                fixed G = 1.164 * (Y - 0.0625) - 0.813 * (V - .5) - 0.391 * (U - .5);
                fixed B = 1.164 * (Y - 0.0625) + 2.018 * (U - .5);
                return fixed3(R, G, B);
            }

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_MainTex, i.uv);
                fixed3 rgbCol = YuvToRgb(col.g, col.r, col.b);
				return fixed4(rgbCol, 1); 
			}
			ENDCG
		}
	}
}
