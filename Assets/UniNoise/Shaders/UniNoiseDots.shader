Shader "Hidden/UniNoiseDots"
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
            float scaleMulti = 1;
            int type;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float dots(float2 uv) {

                return 1 - length((uv - .5)* scaleMulti);
            }

            float dotsOffset(float2 uv) {
                scale = floor(scale / 2) * 2;
                float offset = floor(uv.x * scale) % 2;

                uv += float2(0, offset > 0 ? (1./(scale*2)) : 0);

                float2 local = frac(uv * scale);

                return saturate(1 - length((local - .5) * scaleMulti));
            }

            float dots2Tone(float2 uv) {
                scale = floor(scale / 2) * 2;
                float offset = floor(uv.x * scale) % 2;

                uv += float2(0, offset > 0 ? (1. / (scale * 2)) : 0);

                float2 local = frac(uv * scale);

                float inRange = saturate(1 - length((local - .5) * scaleMulti)) > .05;

                float row = offset > 0 ? 0 : 1;
                float column = (floor(uv.y * scale) % 2) > .5;

                if (inRange > 0) {
                    return (column * .5) + .5;
                }
                else {
                    return 0;
                }

                
            }

            float dots3Tone(float2 uv) {
                scale = floor(scale / 2) * 2;
                float offset = floor(uv.x * scale) % 2;

                uv += float2(0, offset > 0 ? (1. / (scale * 2)) : 0);

                float2 local = frac(uv * scale);

                float inRange = saturate(1 - length((local - .5) * scaleMulti)) > .05;

                float row = offset > 0 ? 0 : 1;
                float column = (floor(uv.y * scale) % 2) > .5;

                if (inRange > 0) {
                    return .3 + ((column + row) / 3);
                }
                else {
                    return 0;
                }


            }

            fixed4 frag(v2f i) : SV_Target
            {
                float F = 0;
            switch (type) {
            case 0: F = dots(frac(i.uv * scale)); break;
            case 1: F = dotsOffset(i.uv); break;
            case 2: F = dots2Tone(i.uv); break;
            case 3: F = dots3Tone(i.uv); break;
            }
                fixed4 col = tex2D(_GradientTex, float2(F,0));
                return col;
            }
            ENDCG
        }
    }
}
