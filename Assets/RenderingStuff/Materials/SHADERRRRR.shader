Shader "Custom/SHADERRRRR"
{
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
}
