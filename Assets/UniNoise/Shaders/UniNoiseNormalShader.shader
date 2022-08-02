Shader "Hidden/UniNoiseNormalShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InputTex ("Texture", 2D) = "white" {}
        _Distance("Distance", Range(.1,10)) = 1
        _Power("Power",Range(0,1.5)) = 1
        _TexSize("Texture Size", float) = 256
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
            float4 _MainTex_ST;

            float _Distance;
            float _Power;
            float _TexSize;
            int quality;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }


            float4 getNormal(float2 uv) {

                float base = tex2D(_InputTex, uv);
                float perDistance = ((1.0 / _TexSize) * _Distance) / quality;
                float added = 1;
                float totalX = 0;
                float totalY = 0;
                for (int x = -quality; x <= quality; x++) {
                    for (int y = -quality; y <= quality; y++) {
                        if (x == 0 && y == 0) {
                            continue;
                        }
                        float current = tex2D(_InputTex, uv + (float2(perDistance * x, perDistance * y) * _Distance));


                        added += 1 * _Distance;
                        totalX += ((base - current) * (x * perDistance * _Power * (3000. / _Distance)));
                        totalY += ((base - current) * (y * perDistance * _Power * (3000. / _Distance)));


                    }
                }

                totalX /= added;
                totalY /= added;

                float xNorm = .5 + totalX;
                float yNorm = .5 + totalY;
                return float4(xNorm,yNorm, 1, 1);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return getNormal(i.uv);
            }
            ENDCG
        }
    }
}
