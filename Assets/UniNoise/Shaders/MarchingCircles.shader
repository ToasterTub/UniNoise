Shader "Hidden/MarchingCircles"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
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
            

            float randbool(float2 p, float scale) {
                float2 uv = p % (scale);
                int xPos = floor(uv.x * tiles);
                int yPos = floor(uv.y * tiles);
                //p /= scale;
                return frac(sin(round(xPos * 542.3 + seed) + round(yPos * 2345.7)) * (99999. * (scale * .1)))* percent > .5 ? 1. : 0.;
            }

            /*float2 randDir(float2 uv) {
                float x = sin(343.32 * uv.x * bigVec.x);
                float y = sin(98323.542 * uv.y * bigVec.y);
                return float2(x, y);
            }*/

            float c0(float2 local) {
                return 1.;
            }

            float c1(float2 local) {
                return length(local);
            }

            float c2(float2 local) {
                return length(float2(1- local.x, local.y));
            }

            float c13(float2 local) {
                return 1. - c2(local);
            }

            float c14(float2 local) {
                return 1. - c1(local);
            }

            float c3(float2 local) {
                return local.y;
            }

            float c4(float2 local) {
                return length(float2(1 - local.x, 1-local.y));
            }

            float c8(float2 local) {
                return length(float2(local.x, 1 - local.y));
            }

            float c7(float2 local) {
                return 1-c8(local);
            }

            float c5(float2 local) {
                return (clamp(c13(local),0,1) + clamp(c7(local),0,1));
            }

            float c6(float2 local) {
                return 1 - local.x;
            }

            

            float c9(float2 local) {
                return  local.x;
            }

            float c11(float2 local) {
                return 1. - c4(local);
            }

            float c10(float2 local) {
                return (clamp(c11(local),0,1) + clamp(c14(local),0,1));
            }

            float c12(float2 local) {
                return 1 - local.y;
            }

            float c15(float2 local) {
                return 0.;
            }




            float2 local(float2 pos) {
                return frac(pos * tiles);
            }

            float cswitch(float t, float2 p) {

                float V = 0;

                switch (t) {
                case 0: V = c0(p); break;
                case 1: V = c1(p); break;
                case 2: V = c2(p); break;
                case 3: V = c3(p); break;
                case 4: V = c4(p); break;
                case 5: V = c5(p); break;
                case 6: V = c6(p); break;
                case 7: V = c7(p); break;
                case 8: V = c8(p); break;
                case 9: V = c9(p); break;
                case 10: V = c10(p); break;
                case 11: V = c11(p); break;
                case 12: V = c12(p); break;
                case 13: V = c13(p); break;
                case 14: V = c14(p); break;
                case 15: V = c15(p); break;
                }

                return V;
            }

            float cint(float2 p, float scale) {

                float offset = 1. / tiles;

                float bl = randbool(p, scale);
                float br = randbool((p + float2(offset, 0.)), scale) * 2.;
                float tl = randbool((p + float2(0., offset)), scale) * 8.;
                float tr = randbool((p + float2(offset, offset)), scale) * 4.;

                return bl + br + tl + tr;
            }


            float getmarch(float2 p, float scale) {

                float2 lpos = frac(p * tiles);
                //lpos.x = 1.-lpos.x;
                float2 roundedPos = floor(p * scale);



                float i = cint(p, scale);

                return clamp(cswitch(i, lpos),0,1);
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

            v2f vert(appdata v)
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
            switch (type) {
                case 0:c = ((octivized(i.uv) - .5) * contrast) + .5; break;
                case 1:c = lines(i.uv); break;
            }

            return tex2D(_GradientTex, float2(c + gain, 0));
        }
        ENDCG
    }
    }
}