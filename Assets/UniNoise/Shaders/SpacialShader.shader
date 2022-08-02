Shader "Hidden/SpacialShader"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _VectorCount("Vector Count", int) = 1
        _GradientTex("Gradient", 2D) = "white"{}
        _Scale ("Scale", Range(1,100)) = 10
        _Type("Type", int) = 0
    }
        SubShader
    {
        Tags { "RenderType" = "Opaque" }
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
            int _VectorCount;
            float _points[1000];
            float _Scale;
            float power = 1;
            int _Type;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float addAll(float3 position) 
            {
                float total = 0;

                for (int i = 0; i < _VectorCount; i++) {
                    float xPos = _points[i * 3];
                    float yPos = _points[(i * 3) + 1];
                    float zPos = _points[(i * 3) + 2];

                    float3 P = float3(xPos, yPos, .5);

                    //total += addSeamless(float3(xPos, yPos, .5), position);
                    total += abs(sin(distance(P, position) * _Scale))* power;
                }

                return total;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float F = addAll(float3(i.uv.x, i.uv.y, .5));

                F += addAll(float3(1-i.uv.x, i.uv.y, .5));
                F += addAll(float3(i.uv.x, 1 - i.uv.y, .5));
                F += addAll(float3(1 - i.uv.x, 1 - i.uv.y, .5));

                F = _Type == 1 ? (sin(F) + 1)/2 : F % 1;

                fixed4 col = fixed4(1, 1, 1, 1);
                col.rgb = tex2D(_GradientTex, float2(F, 1)).rgb;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
