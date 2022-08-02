Shader "Hidden/UniNoiseOutlineShader"
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
            float4 _MainTex_ST;

            float power;
            float blend;
            float dist;
            float textureSize;
            float threshold;
            

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 getOutline(float2 uv) {

                float4 base = tex2D(_InputTex, uv);
                float perDistance = (dist*.2)/ textureSize;

                float Max = 0;

                float total = 0;
                float count = 0;
                for (int x = -10; x <= 10; x++) {
                    for (int y = -10; y <= 10; y++) {
                        if (x == 0 && y == 0) {
                            continue;
                        }
                        float2 offset = float2(perDistance * x, perDistance * y);
                        float2 uvPos = (uv + (offset));
                        uvPos = uvPos;
                        float4 current = tex2D(_InputTex, uvPos);

                        float D = saturate(distance(base.rgb, current.rgb)-threshold);
                        total += D;
                        count++;

                        Max = max(D, Max);
                    }
                }

                
                return lerp(base, 0, ((total / count) * (power * 20 * power))*blend);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = getOutline(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
