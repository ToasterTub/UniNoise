Shader "Hidden/UniNoiseHoundstooth"
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

            sampler2D _MainTex;
            sampler2D _GradientTex;
            float4 _MainTex_ST;
            float scale = 2;
            int type = 0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            

            float ht(float2 uv, float2 local, float2 grid) {

                float isBlank = (grid.x+grid.y) % 2 == 0;
                float blankFlip = grid.x % 2 == 0 && grid.y % 2 == 0;

                if (blankFlip) {
                    return 0;
                }
                else if (isBlank) {
                    return 1;
                }
                else {
                    float stripe = (grid.x+1)% 2 == 0 && (grid.x + 1) % 2 == 0;
                    float2 uv2 = local;
                    float s1 = frac((uv2.x + (1 - uv2.y))) < .5;
                    return stripe ? 1-s1 : s1;
                }

            }

            float smoothHT(float2 uv, float2 local, float2 grid) {
                float v = ht(uv, local, grid);
                float stripe = (sin((uv.x+ uv.y+(v*.03))*3.14*(scale/2))+1)/2;

                float T = abs(stripe - v);
                return T;
            }

            float smoothHT2(float2 uv, float2 local, float2 grid) {
                float v = ht(uv, local, grid);
                float stripe = (sin(((uv.x-(.5/scale)) + (1 - uv.y)) * 3.14 * scale) + 1) / 2;

                float T = abs(stripe - (v+.1));
                return T;
            }

            float htFlip(float2 uv, float2 local, float2 grid) {
                float v = ht(uv, local, grid);
                float distance = length(local - .5);
                return abs(v-distance);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float F = smoothHT(i.uv, frac(i.uv * scale), floor(i.uv * scale));
                switch (type) {
                case 0: F = ht(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                case 1: F = smoothHT(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                case 2: F = smoothHT2(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                case 3: F = htFlip(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                }

                fixed4 col = tex2D(_GradientTex, float2(F,0));
                return col;
            }
            ENDCG
        }
    }
}
