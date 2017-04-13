﻿Shader "Unlit/test"
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
			// make fog work
			#pragma multi_compile_fog
			
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
                float2 screen : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
            sampler2D _Density;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.screen = UnityObjectToClipPos(v.vertex).xy;
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
//				float2 screen_coord = i.screen * 0.5 + 0.5;
//                float density = 1 - distance(i.uv,float2(0.5, 0.5))/0.1;
//
//                return fixed4(density, density, density,1);
//				return fixed4(screen_coord.x,screen_coord.y,0,1);
                float density = tex2D(_Density, i.uv).r;
                return fixed4(density, density, density,1);
			}
			ENDCG
		}
	}
}
