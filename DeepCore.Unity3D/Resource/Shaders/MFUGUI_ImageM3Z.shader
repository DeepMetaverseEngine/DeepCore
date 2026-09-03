// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "MFUGUI/ImageM3Z"
{
	Properties
	{
		[PerRendererData] _MainTex("MainTex", 2D) = "white" {}
	    _MaskTex("MaskTex", 2D) = "white" {}
		_Color("Tint", Color) = (1,1,1,1)
		_Gray("Gray", Float) = 0

		_StencilComp("Stencil Comparison", Float) = 8
		_Stencil("Stencil ID", Float) = 0
		_StencilOp("Stencil Operation", Float) = 0
		_StencilWriteMask("Stencil Write Mask", Float) = 255
		_StencilReadMask("Stencil Read Mask", Float) = 255

		_ColorMask("Color Mask", Float) = 15
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
			"IgnoreProjector" = "True"
			"RenderType" = "Transparent"
			"PreviewType" = "Plane"
			"CanUseSpriteAtlas" = "True"
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
		Fog{ Mode Off }
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
				float4 vertex   : POSITION;
				float4 color    : COLOR;
				float2 texcoord : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex   : SV_POSITION;
				fixed4 color	: COLOR;
				half2 texcoord  : TEXCOORD0;
				float4 worldPosition : TEXCOORD1;
			};

			fixed4		_Color;
			fixed4		_TextureSampleAdd;
			sampler2D	_MainTex;
			sampler2D	_MaskTex;
			float		_Gray;

			bool		_UseClipRect;
			float4		_ClipRect;
			bool		_UseAlphaClip;

			v2f vert(appdata_t IN)
			{
				v2f OUT;
				OUT.worldPosition = IN.vertex;
				OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);

				OUT.texcoord = IN.texcoord;

				#ifdef UNITY_HALF_TEXEL_OFFSET
				OUT.vertex.xy += (_ScreenParams.zw - 1.0)*float2(-1, 1);
				#endif

				OUT.color = IN.color * _Color;
				//OUT.color.rgb = GammaToLinearSpace(IN.color.rgb) * _Color;// GammaToLinearSpace(_Color);
				//OUT.color.a = IN.color.a;
				return OUT;
			}

			fixed4 frag(v2f IN) : SV_Target
			{
				//half4 c1 = pow(tex2D(_MainTex, IN.texcoord), 1.9);
				//half4 c2 = pow(tex2D(_MaskTex, IN.texcoord), 1.9);
				half4 c1 = tex2D(_MainTex, IN.texcoord);				
				//c1.rgb = pow(c1.rgb + 0.055, 2.4) / 1.13711896582;
				//c1.rgb = GammaToLinearSpace(c1.rgb);

				half4 c2 = tex2D(_MaskTex, IN.texcoord);	
				//c2.rgb = GammaToLinearSpace(c2.rgb);
				//c2.rgb = pow(c2.rgb + 0.055, 2.4) / 1.13711896582;

				c1.a = c2.r;
				c1 = (c1 + _TextureSampleAdd) * IN.color;

				if (_Gray != 0) {
					float grey = dot(c1.rgb, float3(0.299, 0.587, 0.114));
					c1.rgb = float3(grey, grey, grey);
				}

				if (_UseClipRect)
					c1 *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);

				if (_UseAlphaClip)
					clip(c1.a - 0.001);

				//c1.rgb = GammaToLinearSpace(c1.rgb);
				return c1;
			}
			ENDCG
		}
	}
	
}

