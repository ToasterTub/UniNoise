Shader "Hidden/UniNoiseBlendTextures"
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
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _InputTex;
            sampler2D blendTex;
            sampler2D blendTex2;
            float blend = .5;
            float threshold = 1;

            sampler2D _MainTex;
            float4 _MainTex_ST;
            int type = 0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 basicMix(float2 uv) {
                fixed4 original = tex2D(_InputTex,uv);
                fixed4 blendSample = tex2D(blendTex, uv);

                float blendValue = (blend - (length(original - blendSample) * ((1 - threshold)*2)))*2;

                return lerp(original, blendSample, saturate(blendValue));
            }

            fixed4 blend2ByInput(float2 uv) {
                float mixSample = clamp((((tex2D(_InputTex, uv)-.5) * ((threshold-.5)*20))+.5) + ((blend-.5)*3),0,1);
                fixed4 blendSample1 = tex2D(blendTex, uv);
                fixed4 blendSample2 = tex2D(blendTex2, uv);

                return lerp(blendSample1, blendSample2, mixSample);
            }

            fixed4 frag (v2f i) : SV_Target
            {

                return type ? blend2ByInput(i.uv) : basicMix(i.uv);
            }
            ENDCG
        }
    }
}
