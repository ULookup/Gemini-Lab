Shader "GeminiLab/UI/SpriteAlphaOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (1,0.82,0.22,1)
        _OutlineThickness ("Outline Thickness", Range(0.5,4)) = 1.5
        [MaterialToggle] PixelSnap ("Pixel Snap", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
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
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineThickness;
            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed centerAlpha = tex2D(_MainTex, i.texcoord).a;
                float2 texel = _MainTex_TexelSize.xy * _OutlineThickness;
                fixed neighbourAlpha = 0;
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord + float2(texel.x, 0)).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord - float2(texel.x, 0)).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord + float2(0, texel.y)).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord - float2(0, texel.y)).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord + texel).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord - texel).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord + float2(texel.x, -texel.y)).a);
                neighbourAlpha = max(neighbourAlpha, tex2D(_MainTex, i.texcoord + float2(-texel.x, texel.y)).a);

                // 只保留透明区域紧邻花瓶 Alpha 的边缘，中心区域不输出颜色。
                fixed edgeAlpha = saturate(neighbourAlpha - centerAlpha);
                fixed4 result = _OutlineColor;
                result.a *= edgeAlpha * i.color.a;

                #ifdef UNITY_UI_CLIP_RECT
                result.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif

                return result;
            }
            ENDCG
        }
    }
}
