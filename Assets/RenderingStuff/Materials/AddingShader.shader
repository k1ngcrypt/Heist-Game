Shader "Custom/AddingShader"
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
                half4 color = half4(max(SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).r, SAMPLE_TEXTURE2D(_OtherTex, sampler_OtherTex, IN.uv).r),0,0,1);
                return color;
            }
            ENDHLSL
        }
    }
}
/**{
    Properties
    {
        _MainTex ("Base", 2D) = "black" {}
        _OtherTex ("Other", 2D) = "black" {}
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_img
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _OtherTex;

            fixed4 frag(v2f_img i) : SV_Target
            {
                return float4(max(tex2D(_MainTex, i.uv).r, tex2D(_OtherTex, i.uv).r), 0, 0, 1);
            }

            ENDCG
        }
    }
}**/
