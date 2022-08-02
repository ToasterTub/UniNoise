Shader "Hidden/UniNoiseWaves"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Waves("Wave Count", int) = 1
        _Waves2("Wave Count", int) = 1
        _Power("Power", float) = .15
        _TexSize("Texture Size", float) = 256
        _GradientTex("Gradient Texture", 2D) = "white" {}
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

            sampler2D _MainTex;
            sampler2D _GradientTex;
            float4 _MainTex_ST;
            int _Waves;
            int _Waves2;
            float _Power;
            float _TexSize;
            int type;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 warpUVClamp(float2 uv) {
                uv.y += clamp(sin((uv.x * _Waves2) * 6.28) * _Power, 0, 1);
                return uv;
            }

            float2 warpUV(float2 uv) {
                uv.y += sin((uv.x* _Waves2)*6.28) * _Power;
                return uv;
            }

            float2 warpUVSharp(float2 uv) {
                float add = abs(sin(uv.x * _Waves2 * 6.28)) * _Power;
                uv.y += add;
                return uv;
            }

            float2 warpUVFlat(float2 uv) {
                float localX = frac(uv.x * _Waves2);
                localX = abs(localX-.5) * _Power;
                uv.y += localX;
                return uv;
            }

            float2 warpUVX(float2 uv) {
                uv.y = abs(((uv.y) + 1) * _Waves) % 1;
                return uv;
            }

            

            float getValue(float2 uv) {
                switch (type) {
                case 0:uv = warpUV(uv); break;
                case 1:uv = warpUVSharp(uv); break;
                case 2:uv = warpUVClamp(uv); break;
                case 3:uv = warpUVFlat(uv); break;
                }
                uv = warpUVX(uv);
                
                return uv.y;
            }
            

            fixed4 frag(v2f i) : SV_Target
            {

                float value = getValue(i.uv);

                fixed4 C = tex2D(_GradientTex, float2(value, 0));
                return C;
            }
            ENDCG
        }
    }
}
