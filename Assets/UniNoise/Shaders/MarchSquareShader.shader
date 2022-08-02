Shader "Hidden/MarchSquareShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
            float tiles = 15;
            float seed = 0;
            int octaves = 1;
            float detailWeight = .95;
            int detail = 1;
            float contrast = 1;
            float secondaryScale;
            int type = 0;
            float percent = 1;
            float gain = 0;

            static const float pi = 3.14159265359;
            static const float doublePi = 6.28318530718;
            static const float4 bigVec = float4(43743.4, 3284.432, 889432.3, 24824.65);

            float randbool(float2 p, float scale) {
                float2 uv = p % (scale);
                int xPos = floor(uv.x * tiles);
                int yPos = floor(uv.y * tiles);
                //p /= scale;
                return frac(sin(round(xPos * 542.3 + seed) + round(yPos * 2345.7)) * (99999. * (scale * .1))) * percent > .5 ? 1. : 0.;
            }

            float2 randDir(float2 uv) {
                float x = sin(343.32 * uv.x * bigVec.x);
                float y = sin(98323.542 * uv.y * bigVec.y);
                return float2(x, y);
            }

            float c0(float2 local) {
                return 1.;
            }

            float c1(float2 local) {
                return 1. - (local.x + local.y < .5);
            }

            float c2(float2 local) {
                return 1. - (local.x - (local.y) > .5);
            }

            float c13(float2 local) {
                return 1. - c2(local);
            }

            float c14(float2 local) {
                return 1. - c1(local);
            }

            float c3(float2 local) {
                return ((local.y) > .5);
            }

            float c4(float2 local) {
                return 1. - (local.x + (local.y - 1.) > .5);
            }

            float c7(float2 local) {
                return ((local.x - local.y) < -.5);
            }

            float c5(float2 local) {
                return c13(local) + c7(local);
            }

            float c6(float2 local) {
                return ((local.x) < .5);
            }

            float c8(float2 local) {
                return 1. - c7(local);
            }

            float c9(float2 local) {
                return ((local.x) > .5);
            }

            float c11(float2 local) {
                return 1. - c4(local);
            }

            float c10(float2 local) {
                return (c11(local) + c14(local));
            }

            float c12(float2 local) {
                return ((local.y) < .5);
            }

            float c15(float2 local) {
                return 0.;
            }












            float cs7(float2 local) {
                return local.y * (1 - local.x);
            }

            float cs11(float2 local) {
                return local.y * (local.x);;
            }

            float cs13(float2 local) {
                return (1 - local.y) * (local.x);
            }

            float cs14(float2 local) {
                return (1 - local.y) * (1 - local.x);;
            }

            float cs0(float2 local) {
                return 1.;
            }

            float cs1(float2 local) {
                return 1 - cs14(local);
            }

            float cs2(float2 local) {
                return 1 - cs13(local);
            }

            float cs3(float2 local) {
                return local.y;
            }

            float cs4(float2 local) {
                return  1 - cs11(local);
            }

            float cs8(float2 local) {
                return 1 - cs7(local);
            }

            float cs5(float2 local) {
                return cs13(local) + cs7(local);
            }

            float cs6(float2 local) {
                return 1 - local.x;
            }

            float cs9(float2 local) {
                return local.x;
            }

            float cs10(float2 local) {
                return cs14(local) + cs11(local);
            }

            float cs12(float2 local) {
                return 1 - local.y;
            }

            float cs15(float2 local) {
                return 0.;
            }









            float2 local(float2 pos) {
                return frac(pos * tiles);
            }

            float cswitch(float t, float2 p) {

                switch (t) {
                case 0: return c0(p);
                case 1: return c1(p);
                case 2: return c2(p);
                case 3: return c3(p);
                case 4: return c4(p);
                case 5: return c5(p);
                case 6: return c6(p);
                case 7: return c7(p);
                case 8: return c8(p);
                case 9: return c9(p);
                case 10: return c10(p);
                case 11: return c11(p);
                case 12: return c12(p);
                case 13: return c13(p);
                case 14: return c14(p);
                case 15: return c15(p);
                }

                return c0(p);
            }
            float cSmoothswitch(float t, float2 p) {

                switch (t) {
                case 0: return cs0(p);
                case 1: return cs1(p);
                case 2: return cs2(p);
                case 3: return cs3(p);
                case 4: return cs4(p);
                case 5: return cs5(p);
                case 6: return cs6(p);
                case 7: return cs7(p);
                case 8: return cs8(p);
                case 9: return cs9(p);
                case 10: return cs10(p);
                case 11: return cs11(p);
                case 12: return cs12(p);
                case 13: return cs13(p);
                case 14: return cs14(p);
                case 15: return cs15(p);
                }

                return c0(p);
            }

            float cint(float2 p, float scale) {

                float offset = 1. / tiles;

                float bl = randbool(p, scale);
                float br = randbool((p + float2(offset, 0.)), scale) * 2.;
                float tl = randbool((p + float2(0., offset)), scale) * 8.;
                float tr = randbool((p + float2(offset, offset)), scale) * 4.;

                return bl + br + tl + tr;
            }

            float getmarchSmooth(float2 p, float scale) {

                float2 lpos = frac(p * tiles);
                //lpos.x = 1.-lpos.x;
                float2 roundedPos = floor(p * scale);



                float i = cint(p, scale);

                return cSmoothswitch(i, lpos);
            }


            float getmarch(float2 p, float scale) {

                float2 lpos = frac(p * tiles);
                //lpos.x = 1.-lpos.x;
                float2 roundedPos = floor(p * scale);



                float i = cint(p, scale);

                return clamp(cswitch(i, lpos), 0, 1);
            }

            float octivizedSmooth(float2 uv) {

                float value = 0.;
                float maxamp = 0.;
                float current = 1.;
                float scale;
                for (int i = 0; i < octaves; i++) {
                    scale = ((i * detail) + 1);
                    value += getmarchSmooth(uv * scale, scale) * current;
                    maxamp += current;
                    current *= detailWeight;
                }

                return value / maxamp;
            }

            float octivized(float2 uv) {

                float value = 0.;
                float maxamp = 0.;
                float current = 1.;
                float scale;
                for (int i = 0; i < octaves; i++) {
                    scale = ((i * detail) + 1);
                    value += getmarch(uv * scale, scale) * current;
                    maxamp += current;
                    current *= detailWeight;
                }

                return value / maxamp;
            }

            float lines(float2 uv) {
                float v = ((octivized(uv) - .5) * contrast);
                float2 lPos = uv;
                lPos.x += (v - .5);
                float value = (sin(lPos.x * doublePi * secondaryScale) + 1.) / 2.;
                return value;
            }
            float linesSmooth(float2 uv) {
                float v = ((octivizedSmooth(uv) - .5) * contrast);
                float2 lPos = uv;
                lPos.x += (v - .5);
                float value = (sin(lPos.x * doublePi * secondaryScale) + 1.) / 2.;
                return value;
            }

            float warp(float2 uv) {

                float c1 = (octivized(uv+234.)-.5) * contrast;
                float c2 = (octivized(uv+94.)-.5) * contrast;

                return octivized(uv + (float2(c1,c2)*.2));
            }
            float warpSmooth(float2 uv) {

                float c1 = (octivizedSmooth(uv + 234.) - .5) * contrast;
                float c2 = (octivizedSmooth(uv + 94.) - .5) * contrast;

                return octivizedSmooth(uv + (float2(c1, c2) * .2) );
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float c = 0;
                //return tex2D(_GradientTex, float2(cSmoothswitch(octaves, (i.uv*10)%1), 0));
                switch (type) {
                    case 0:c = ((octivized(i.uv) - .5) * contrast) + .5; break;
                    case 1:c = lines(i.uv); break;
                    case 2:c = warp(i.uv); break;
                    case 3:c = ((octivizedSmooth(i.uv) - .5) * contrast) + .5; break;
                    case 4:c = linesSmooth(i.uv); break;
                    case 5:c = warpSmooth(i.uv); break;
                }

                return tex2D(_GradientTex, float2(c+gain, 0));
            }
            ENDCG
        }
    }
}
