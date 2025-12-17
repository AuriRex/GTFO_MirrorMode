Shader "Hidden/MirrorMode"
{
	Properties
	{
		_MainTex("Base (RGB)", 2D) = "white" {}
	}

	SubShader
	{
		Pass
		{
			CGPROGRAM

			#pragma vertex vert_img
			#pragma fragment frag

			#include "UnityCG.cginc"

			uniform sampler2D _MainTex;

			fixed4 frag(v2f_img i) : COLOR
			{
				i.uv.x = i.uv.x * -1 + 1;
				
				fixed4 c = tex2D(_MainTex, i.uv);

				return c;
			}
			ENDCG
		}
	}
}