Shader "Hidden/UniNoiseCubes"
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
            int scale = 1;
            int type = 0;
            float topValue = 0;
            float leftValue = .5;
            float rightValue = 1.;
            float smoothPower = 1;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float d(float a, float b) {
                return abs(a - b);
            }

            float hatchedTop(float value, float2 uv, float hatchValue) {
                uv.y *= 2;
                uv.y += (sin(uv.x * (6.28*scale)) / (scale * 5))* (smoothPower/5);
                float S = floor(((4 * scale) * (hatchValue + .75)));
                float P = 6. / hatchValue;
                float hatch1 = pow(abs(sin(((uv.x + (uv.y * 1.5)) * 6.28) * S)),P);
                float hatch2 = pow(abs(sin(((uv.x - (uv.y * 1.5)) * 6.28) * S)),P);

                float hatch = min(1-(clamp((hatch2 * hatchValue), 0, 1) * 3), 1-(hatch1* hatchValue));

                return hatch;
            }

            float hatchedLeft(float value, float2 uv, float hatchValue) {
                uv.x += (sin(uv.y * (6.28 * scale)) / (scale * 5))* (smoothPower / 10);
                uv.y *= 2;
                float S = floor(((4 * scale) * (hatchValue + .75)));
                float P = 6. / hatchValue;
                float hatch1 = pow(abs(sin(((uv.x + (uv.y * 1.5)) * 6.28) *S)), P);
                float hatch2 = pow(abs(sin(((uv.x *2.) * 6.28) * S)), P);

                float hatch = min(1 - (clamp((hatch2 * hatchValue), 0, 1) * 3), 1 - (hatch1 * hatchValue));

                return hatch;
            }

            float hatchedRight(float value, float2 uv, float hatchValue) {
                uv.x += -(sin((uv.y+.1) * (6.28 * scale)) / (scale * 2))* (smoothPower / 10);
                uv.y *= 2;
                float S = floor(((4 * scale) * (hatchValue + .75)));
                float P = 6. / hatchValue;
                float hatch1 = pow(abs(sin(((uv.x - (uv.y * 1.5)) * 6.28) * S)), P);
                float hatch2 = pow(abs(sin(((uv.x * 2.) * 6.28) * S)), P);

                float hatch = min(1 - (clamp((hatch2 * hatchValue), 0, 1) * 3), 1 - (hatch1 * hatchValue));

                return hatch;
            }


            float getPart(float2 uv, out float shadeValue) {
                uv.y = (uv.y * .75) + .25;
                float offx = uv.x - .5;
                float splitx = abs(offx) * 2.;
                uv.y -= splitx * .25;
                shadeValue = 1;

                float yval = uv.y < .25 ? 1. : uv.y > .75 ? 1. : uv.y;

                if (uv.y < .25 || uv.y > .75) {
                    return topValue;
                }

                return sign(offx) < .5 ? leftValue : rightValue;

                return 1;
            }

            float getPartHatched(float2 uv, out float shadeValue, float2 baseUV) {
                uv.y = (uv.y * .75) + .25;
                float offx = uv.x - .5;
                float splitx = abs(offx) * 2.;
                uv.y -= splitx * .25;
                shadeValue = 1;

                float yval = uv.y < .25 ? 1. : uv.y > .75 ? 1. : uv.y;

                if (uv.y < .25 || uv.y > .75) {
                    return hatchedTop(1.,baseUV,topValue);
                }

                return sign(offx) < .5 ? hatchedLeft(.5, baseUV, leftValue) : hatchedRight(.5, baseUV, rightValue);

                return 1;
            }

            float getPartSmooth(float2 uv, out float shadeValue) {
                uv.y = (uv.y * .75) + .25;
                float offx = uv.x - .5;
                float splitx = abs(offx) * 2.;
                uv.y -= splitx * .25;
                shadeValue = topValue;

                float yval = uv.y < .25 ? 1. : uv.y > .75 ? 1. : uv.y;

                if (uv.y < .25 || uv.y > .75) {
                    return frac(d(abs(uv.y),.5)*1.5* smoothPower);
                }
                shadeValue = (sign(offx) > .5) ? rightValue : leftValue;
                return frac(1-(d(--uv.y, -.1) * 1.5)* smoothPower);
                
                return 1;
            }

            float getPartSmoothFlatTop(float2 uv, out float shadeValue) {
                uv.y = (uv.y * .75) + .25;
                float offx = uv.x - .5;
                float splitx = abs(offx) * 2.;
                uv.y -= splitx * .25;
                shadeValue = 1.;

                float yval = uv.y < .25 ? 1. : uv.y > .75 ? 1. : uv.y;

                if (uv.y < .25 || uv.y > .75) {
                    return topValue;
                }
                shadeValue = (sign(offx) > .5) ? rightValue : leftValue;
                return frac(1 - (d(--uv.y, -.1) * 1.5) * smoothPower);

                return 1;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                scale *= 2;
                float2 holduv = i.uv;
                i.uv.x += frac((i.uv.y / 2) * scale) < .5 ? (.5 / scale) : 0;
                float2 local = frac(i.uv * scale);
                float shadeValue = 0;
                float c = 0;

                switch (type) {
                case 0: c = getPart(local, shadeValue); break;
                case 1: c = getPartSmooth(local, shadeValue); break;
                case 2: c = getPartSmoothFlatTop(local, shadeValue); break;
                case 3: c = getPartHatched(local, shadeValue, holduv); break;
                }

                fixed4 col = tex2D(_GradientTex, float2(c,0));
                col.rgb *= shadeValue;
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
