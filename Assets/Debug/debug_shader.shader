Shader "Unlit/test"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
        _Range ("Scale", Range(0, 20)) = 1

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
			#pragma target 5.0
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
                float3 w_pos : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
            sampler2D _Density;
			float _Range;
            float2 _Index;
            float2 _GridResolution;
			v2f vert (appdata v)
			{
				v2f o;
                float4 pos = v.vertex;
                pos.y += _Range * tex2Dlod(_MainTex, float4(_Index/_GridResolution,0,0)).r;
				o.vertex = UnityObjectToClipPos(pos);
                o.w_pos = mul(unity_ObjectToWorld, pos).xyz;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
                return fixed4(_Index/_GridResolution,0,1);
                return tex2D(_MainTex, float4(_Index/_GridResolution,0,0)).r;
			}
			ENDCG
		}
	}
}
