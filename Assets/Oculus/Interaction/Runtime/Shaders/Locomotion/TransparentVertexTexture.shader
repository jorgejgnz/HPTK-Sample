Shader "Unlit/TransparentVertexTexture"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Fade("Fade",Range(0,1)) = 1
        _Highlight("Highlight Strength",Range(0,1)) = 0
        _HighlightColor("Highlight Color", COLOR) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 100
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            
            #include "UnityCG.cginc"

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct VertexOutput
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 color : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            half _Fade;
            half _Highlight;
            half4 _HighlightColor;

            VertexOutput vert (VertexInput v)
            {
                VertexOutput o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = v.color;
                o.color.a *= _Fade;
                return o;
            }

            half4 frag(VertexOutput i) : SV_Target
            {
                half4 col = tex2D(_MainTex, i.uv) * i.color;
                col.rgb = lerp(col.rgb, _HighlightColor.rgb, _Highlight * _HighlightColor.a);
                return col;
            }
            ENDCG
        }
    }
}
