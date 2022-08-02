Shader "Hidden/UniNoiseParticleize"
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
            sampler2D _InputTex;
            float power = 1;
            float threshold = 1;
            float4 _MainTex_ST;
            float size = 1;
            int preserveColor = 1;
            int hardCut = 0;
            int doCheckers = 1;
            float contrast = 1;
            int square;
            int singleColor = 0;
            fixed4 singleColorColor;
            fixed4 backgroundColor;

            static float gridSize = 10;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float grid(float2 uv) {
                float2 G = frac(uv * gridSize);
                float V = (G.y < .5 ? (G.x > .5) : (G.x < .5));
                return V ? 1 : .9;
            }

            fixed4 frag (v2f i) : SV_Target
            { 
                fixed4 col = tex2D(_InputTex, i.uv);
                float P = 1-pow((length(i.uv - .5))*((11-size)*2), power); // Circle
                float PSquare = 1 - pow(max(abs(i.uv - .5).x, abs(i.uv - .5).y) * ((11 - size) * 2), power);
                P = square ? PSquare : P;
                float flat = 1-length(col.rgb);
                float cut = saturate(flat - (1-threshold));

                float G = grid(i.uv);
                
                float alpha = hardCut ? (P > threshold*flat) : saturate(P - cut);
                float finalAlpha = lerp(saturate(((alpha - .5) * contrast) + .5), alpha, 1-P);
                col.a = saturate(finalAlpha);
                col.rgb = preserveColor ? col.rgb : 1;

                col = singleColor ? fixed4(singleColorColor.r, singleColorColor.g, singleColorColor.b, col.a) : col;
                col = lerp(backgroundColor * G, col, doCheckers ? col.a : 1);
                
                return col;
            }
            ENDCG
        }
    }
}
