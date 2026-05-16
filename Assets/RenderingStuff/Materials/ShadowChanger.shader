Shader "Custom/ShadowChanger"
{
    Properties
    {
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        _OtherTex("FasterShadow", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_MainTex);
            TEXTURE2D(_OtherTex);
            SAMPLER(sampler_MainTex);
            SAMPLER(sampler_OtherTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                float4 _MainTex_ST;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = half4(0, 0, 0, _BaseColor.a-4*max(0.4f*SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r, SAMPLE_TEXTURE2D(_OtherTex, sampler_OtherTex, IN.uv).r)*_BaseColor.a) * _BaseColor;
                return color;
            }
            ENDHLSL
        }
    }
}
/**{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OtherTex ("FasterShadow", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 uv       : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            sampler2D _MainTex;
            sampler2D _OtherTex;
            fixed4 _Color;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Read the R8 texture value
                float mask = tex2D(_MainTex, i.uv).r;

                // White RGB, mask becomes alpha
                return fixed4(i.color.rgb, i.color.a - 4 * mask * i.color.a);
            }

            ENDCG
        }
    }
}**/

