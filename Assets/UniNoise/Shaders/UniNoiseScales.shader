Shader "Hidden/UniNoiseScales"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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

            float xScale = 2;
            float yScale = 2;
            float power = 1;

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
            int type;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float flatScale(float2 uv) {
                uv = frac(float2(uv.x * xScale, uv.y * yScale));
                float F = distance(uv, float2(.5, 0)) < .5;
                F = uv.y > .5 ? min(distance(uv + float2(.5, 0), float2(.5, .5)), distance(uv + float2(-.5, 0), float2(.5, .5))) > .5 : F;
                return F;
            }

            float smoothScale(float2 uv) {
                uv = frac(float2(uv.x * xScale, uv.y * yScale));
                float F = distance(uv, float2(.5, 0)) < .5;
                F = uv.y > .5 ? min(distance(uv + float2(.5, 0), float2(.5, .5)), distance(uv + float2(-.5, 0), float2(.5, .5))) > .5 : F;
                
                if (F > .5) {
                    F = frac(uv.y+.5);
                }
                else {
                    F = frac(uv.y);
                }
                
                return F;
            }

            float sined(float2 uv) {
                uv = frac(float2(uv.x * xScale, uv.y * yScale));
                float F = distance(uv, float2(.5, 0)) < .5;
                F = uv.y > .5 ? min(distance(uv + float2(.5, 0), float2(.5, .5)), distance(uv + float2(-.5, 0), float2(.5, .5))) > .5 : F;

                if (F > .5) {
                    F = uv.y < .5 ?
                        abs(sin(distance(uv, float2(.5, -.5))* power)):
                        abs(sin(distance(uv, float2(.5, .5))* power));
                }
                else {
                    F = uv.x < .5 ?
                        abs(sin(distance(uv, float2(0, 0)) * power)) :
                        abs(sin(distance(uv, float2(1, 0)) * power));
                }

                return F;
            }

            float sinedFlipped(float2 uv) {
                uv = frac(float2(uv.x * xScale, uv.y * yScale));
                float F = distance(uv, float2(.5, 0)) < .5;
                F = uv.y > .5 ? min(distance(uv + float2(.5, 0), float2(.5, .5)), distance(uv + float2(-.5, 0), float2(.5, .5))) > .5 : F;

                if (F > .5) {
                    F = uv.y < .5 ?
                        abs(sin(distance(uv, float2(.5, -.5)) * power)) :
                        abs(sin(distance(uv, float2(.5, .5)) * power));
                }
                else {
                    F = 1 - (uv.x < .5 ?
                        abs(sin(distance(uv, float2(0, 0)) * power)) :
                        abs(sin(distance(uv, float2(1, 0)) * power)));
                }

                return F;
            }

            fixed4 frag(v2f i) : SV_Target
            {


                float F = 0;
            switch (type) {
            case 0: F = flatScale(i.uv); break;
            case 1: F = smoothScale(i.uv); break;
            case 2: F = sined(i.uv); break;
            case 3: F = sinedFlipped(i.uv); break;
                }

                fixed4 col = tex2D(_GradientTex, float2(F,0));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
