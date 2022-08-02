Shader "Hidden/LinesShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex("Gradient", 2D) = "white"{}
        _LineCount("Line Count", int) = 1
        _LineAngle("Line Angle", int) = 0
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

            int _LineCount;
            float _LineAngle;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float getValue(float2 uv) {
                float F = 0;
                switch (_LineAngle) { // integerized line angles 0-3
                case 0: F = (abs(((uv.x) * _LineCount))) % 1; break;
                case 1: F = (abs(((uv.x + uv.y+1)) * _LineCount)) % 1; break;
                case 2: F = (abs(((uv.y) * _LineCount))) % 1; break;
                case 3: F = (abs(((uv.x - (uv.y + 1)) * _LineCount))) % 1; break;
                }

                return F;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 col = tex2D(_GradientTex, getValue(i.uv));
                return col;
            }
            ENDCG
        }
    }
}
