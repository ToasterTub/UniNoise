Shader "Hidden/DGN"
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
            float4 _MainTex_ST;
            sampler2D _GradientTex;

            float octaves = 20;
            int tiles;
            int lacunarity;
            float detailWeight;
            float contrast = 1;
            float seed = 0;
            float gain = 1;
            int turbulent;
            int type = 0;
            float power = 1;


            float2 pointToDir(float ang, float scale, float secondSeed = 0) { // ang is 0. to 1.
                // doesn't need to be 2d, might use it for something in the future
                return float2(sin((ang * (58.80 + scale) + secondSeed) + (seed * (.618))), cos((ang * 534.642 + scale) + (seed * (.618))));
            }

            // get a pseudo-random number between 0 and 1, with some offsets for randomness
            float random(float2 p, float scale, float seedOffset = 0)
            {
                float value = frac(cos(p.x * 10.12 + p.y * 212.92) * (99999. * (scale * .1)));
                return value;
            }

            // get grid positions
            float getPoint(float2 pos, float scale) {
                float2 uv = pos % (scale);
                int xPos = floor(uv.x * tiles);
                int yPos = floor(uv.y * tiles);

                return random(float2(xPos, yPos), scale, 1);
            }
            
            float fade(float input) {
                return input * input * (3. - 2. * input);
            }

            // get one octave
            float grid(float2 uv, int scale, float secondSeed = 0) {
                uv *= scale;
                // grid based uv coords
                float2 grid = floor(uv * scale); 

                // coords within int grid
                float2 local = frac(uv * tiles);

                // use to get neighbor tiles
                float2 xAdd = float2(1.0, 0); 
                float2 yAdd = float2(0, 1.0);

                // get neighbors with directional offset
                float bl = pointToDir(getPoint(uv, scale), scale, secondSeed).x;
                float br = pointToDir(getPoint(uv + float2(1.0 / tiles, 0), scale), scale, secondSeed).x;
                float tl = pointToDir(getPoint(uv + float2(0, 1.0 / tiles), scale), scale, secondSeed).x;
                float tr = pointToDir(getPoint(uv + float2(1.0 / tiles, 1.0 / tiles), scale), scale, secondSeed).x;

                // gradient between neighbors
                float t = lerp(tl, tr, local.x);
                float b = lerp(bl, br, local.x);
                float mix = lerp(b, t, local.y);

                return smoothstep(.2,.9,mix + .5);
            }

            float doOctaves(float2 uv, float secondSeed = 0) {

                float value, totalAmp;
                float currentAmp = 1;

                // Blend the octaves
                if (!turbulent) {
                    for (int i = 0; i < octaves; i++) {
                        float scale = ((i * lacunarity) + 1.0);
                        value += grid(uv, scale, secondSeed) * currentAmp;
                        totalAmp += currentAmp;
                        currentAmp *= detailWeight;
                    }
                }
                else {
                    for (int i = 0; i < octaves; i++) {
                        float scale = ((i * lacunarity) + 1.0);
                        float C = grid(uv, scale, secondSeed);
                        // add turbulence
                        C = abs(sin((C-.25) * 3.14));
                        value += C * currentAmp;
                        totalAmp += currentAmp;
                        currentAmp *= detailWeight;
                    }
                }

                // return with contrast and gain
                return ((((value / totalAmp) - .5)*contrast)+.5)+ gain;
            }


            float warped(float2 uv) {
                // get two offsets
                float P1 = doOctaves(uv, 1.321);
                float P2 = doOctaves(uv, 849.2131);

                // warp third sample with offsets
                float2 pos = frac(uv + (float2(P1 - .5, P2 - .5)* (power/2)));

                // get final result
                float val = doOctaves(pos, 35687.236);

                return val;
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
                switch (type) {
                    case 0: return tex2D(_GradientTex, float2(doOctaves(i.uv), 0));
                    case 1: return tex2D(_GradientTex, float2(warped(i.uv), 0));
                }

                return tex2D(_GradientTex, float2(doOctaves(i.uv),0));
            }
            ENDCG
        }
    }
}
