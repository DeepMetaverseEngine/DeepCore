// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MFUGUI/TextGray"
{
	Properties
	{
		[PerRendererData] _MainTex("Sprite Texture", 2D) = "white" {}
		_Color("Main Color", Color) = (1,1,1,1)
		_Gray("Gray", Float) = 0

		_DetailTex("Detail (RGB)", 2D) = "white" {}
		_Strength("Detail Strength", Range(0.0, 1.0)) = 0.2

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		LOD 100

		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
		}

		Stencil
		{
			Ref[_Stencil]
			Comp[_StencilComp]
			Pass[_StencilOp]
			ReadMask[_StencilReadMask]
			WriteMask[_StencilWriteMask]
		}

		Cull Off
		Lighting Off
		ZWrite Off
		ZTest[unity_GUIZTestMode]
		Blend SrcAlpha OneMinusSrcAlpha
		ColorMask[_ColorMask]

		Pass
		{
		CGPROGRAM
		#pragma vertex vert
		#pragma fragment frag
		#include "UnityCG.cginc"
		#include "UnityUI.cginc"

			struct appdata_t
			{
				float4 vertex : POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoord2 : TEXCOORD1;
				fixed4 color : COLOR;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 texcoord : TEXCOORD0;
				float2 texcoord2 : TEXCOORD1;
				float4 worldPosition : TEXCOORD2;
				fixed4 color : COLOR;
			};

			sampler2D	_MainTex;
			sampler2D	_DetailTex;
			float4		_MainTex_ST;
			float4		_DetailTex_ST;
			float4		_DetailTex_TexelSize;
			fixed4		_Color;
			fixed		_Strength;
			fixed4		_TextureSampleAdd;
			float		_Gray;

			bool		_UseClipRect;
			float4		_ClipRect;
			bool		_UseAlphaClip;

			v2f vert(appdata_t v)
			{
				v2f o;
				o.worldPosition = v.vertex;
				o.vertex = UnityObjectToClipPos(o.worldPosition);

				o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
				o.texcoord2 = TRANSFORM_TEX(v.texcoord2 * _DetailTex_TexelSize.xy, _DetailTex);
				o.color = v.color * _Color;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				o.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1,1);
				#endif
				return o;
			}

			fixed4 frag(v2f i) : COLOR
			{
				//fixed4 color = (pow(tex2D(_MainTex, i.texcoord), 1.9) + _TextureSampleAdd) * i.color;
				//fixed4 detail = pow(tex2D(_DetailTex, i.texcoord2), 1.9);
				fixed4 color = (tex2D(_MainTex, i.texcoord) + _TextureSampleAdd) * i.color;
				//color.rgb = pow(color.rgb + 0.055, 2.4) / 1.13711896582;
				fixed4 detail = tex2D(_DetailTex, i.texcoord2);
				//detail.rgb = pow(detail.rgb + 0.055, 2.4) / 1.13711896582;

				color.rgb = lerp(color.rgb, color.rgb * detail.rgb, detail.a * _Strength);
				color = color * _Color;
				
				if (_Gray != 0) {
					float grey = dot(color.rgb, float3(0.299, 0.587, 0.114));
					color.rgb = float3(grey, grey, grey);
				}

				if (_UseClipRect)
					color *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

				if (_UseAlphaClip)
					clip(color.a - 0.001);

				return color;//pow(color, 2.2)
			}
		ENDCG
		}
	}
}

