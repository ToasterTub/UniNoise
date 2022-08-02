Shader "Hidden/Truchet"
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
            float tiles;
            int type = 0;

            float randomization;
            float points = 1;
            float octaves = 1;
            int overrideTile = -1;

            float4 testPoint;

            int test;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float rand(float2 p) {
                p += 1;
                p = p * .32;
                return frac(sin(p.x * 9323.3292 + p.y * 37473.34) * 759302.348);
            }

            float inrange(float d, float mi, float ma) {
                return d > mi&& d < ma ? 0. : 1.;
            }

            float tile1(float2 uv) {
                float f1 = inrange(distance(uv, float2(0, 0)), .45, .55);

                return f1;
            }

            float tile2(float2 uv) {
                float f1 = inrange(distance(uv, float2(0, 0)), .45, .55);
                float f2 = inrange(distance(uv, float2(1, 0)), .45, .55);
                float f3 = inrange(distance(uv, float2(0, 1)), .45, .55);

                return min(min(f2, f1), f3);
            }

            float tile3(float2 uv, float2 fuv) {
                float f1 = inrange(distance(fuv, float2(0, 0)), .45, .55);
                float f2 = inrange(distance(fuv, float2(.5, 1)), 0., .05);
                float f3 = inrange(distance(fuv, float2(1, .5)), 0., .05);
                //float f3 = inrange(distance(uv,vec2(.75,.25)),0.,.05);

                return min(min(f1, f2), f3);
            }

            float tile4(float2 uv, float2 fuv) {
                float f1 = inrange(fuv.y, .45, .55);
                float f2 = inrange(distance(fuv, float2(.5, 0.)), 0., .05);
                float f3 = inrange(distance(fuv, float2(.5, 1)), 0., .05);
                //float f3 = inrange(distance(uv,vec2(.75,.25)),0.,.05);

                return min(min(f1, f2), f3);
            }

            float tile5(float2 uv, float2 fuv) {
                float f1 = inrange(distance(fuv, float2(1, 1)), .45, .55);
                float f2 = inrange(distance(fuv, float2(1, 0)), .45, .55);
                float f3 = inrange(distance(uv,  float2(0, .5)), 0., .05);

                return min(min(f1, f2), f3);
            }

            float tile6(float2 uv, float2 fuv) {
                float f1 = inrange(fuv.x, .45, .55);
                float f2 = inrange(distance(fuv, float2(0, 0.5)), 0., .05);
                float f3 = inrange(distance(fuv, float2(1, .5)), 0., .05);
                //float f3 = inrange(distance(uv,vec2(.75,.25)),0.,.05);

                return min(min(f1, f2), f3);
            }

            float tile7(float2 uv, float2 fuv) {
                float f1 = inrange(distance(fuv, float2(1, 1)), .45, .55);
                float f2 = inrange(distance(fuv, float2(1, 0)), .45, .55);
                float f3 = inrange(distance(fuv, float2(0, 0)), .45, .55);

                return min(min(f1, f2), f3);
            }

            float getTiles(float2 uv, float2 totaluv) {
                totaluv += 5;
                float2 id = floor(totaluv * tiles);
                uv.x = uv.x * sign(sin(id.x * 9825.76 + id.y * 5827.5+randomization));
                uv.y = uv.y * sign(sin(id.x * 3897.76 + id.y * 922.5)-randomization);
                
                uv = frac(uv);
                float2 fuv = uv;
                int flip = uv.x + uv.y < 1.;
                uv = flip ? uv : 1. - uv;

                float2 tid = floor(totaluv * tiles);
                float tv = floor(((rand(tid) * 7.99) + (randomization*tid.x)) % 8);
                
                if (overrideTile >= 0) {
                    tv = overrideTile;
                }

                float v = tv == 0. ?
                    tile2(uv) :
                    tv == 2. ?
                    tile1(uv) :
                    tv == 3. ?
                    tile3(uv, fuv) :
                    tv == 4. ?
                    tile4(uv, fuv) :
                    tv == 5. ?
                    tile5(uv, fuv) :
                    tv == 6. ?
                    tile6(uv, fuv) :
                    tile7(uv, fuv)
                    ;
                v = v > .5;
                return v;//flip ? v : 1-v;

            }
            //float f1 = inrange(distance(uv, testPoint.xy), testPoint.z, testPoint.w);
            //float Ttile1(float2 uv) {
            //    
            //    float f1 = inrange(distance(uv, float2(.25, .933)), .45, .55);
            //    float f2 = inrange(distance(uv, float2(.933, .25)), .45, .55);
            //    float v = min(f1, f2);
            //    return v;//uv.x+uv.y < 1 ? v : 1-v;
            //}

            //float Ttile2(float2 uv) {

            //    float f1 = inrange(distance(uv, float2(.25, .933)), .45, .55);
            //    float f2 = inrange(distance(uv, float2(.933, .25)), .45, .55);
            //    float f3 = inrange(distance(uv, float2(0, 0)), .45, .55);
            //    float v = min(min(f1, f2),f3);
            //    return v;
            //}

            //float Ttile3(float2 uv) {

            //    float f1 = inrange(distance(uv, float2(0, 0)), .45, .55);
            //    float f2 = inrange(distance(uv, float2(.5, .5)), 0,.05);
            //    float v = min(f1, f2);
            //    return v;
            //}

            //float Ttile4(float2 uv) {

            //    float f1 = inrange(distance(uv, float2(.5, 0)), 0, .05);
            //    float f2 = inrange(distance(uv, float2(.5, .5)), 0, .05);
            //    float f3 = inrange(distance(uv, float2(0, .5)), 0, .05);
            //    float v = min(min(f1, f2),f3);
            //    return v;
            //}


            //float getTilesTriangular(float2 uv, float2 totaluv) {
            //    totaluv += 5;
            //    float2 id = floor(totaluv * tiles);
            //    uv.x = uv.x * sign(sin(id.x * 9825.76 + id.y * 5827.5 + randomization));
            //    uv.y = uv.y * sign(sin(id.x * 3897.76 + id.y * 922.5) - randomization);

            //    uv = frac(uv);
            //    float2 fuv = uv;
            //    uv = uv.x + uv.y < 1. ? uv : 1. - uv;

            //    //return Ttile3(uv);

            //    float2 tid = floor(totaluv * tiles);
            //    float tv = floor(((rand(tid) * 7.99) + (randomization * tid.x)) % 5);

            //    if (overrideTile >= 0) {
            //        tv = overrideTile;
            //    }

            //    return tv == 0. ?
            //        Ttile1(uv) :
            //        tv == 2. ?
            //        Ttile2(uv) :
            //        tv == 3. ?
            //        Ttile3(uv) :
            //        tv == 4. ?
            //        Ttile4(uv) :
            //        tv == 5. ?
            //        tile5(uv, fuv) :
            //        tv == 6. ?
            //        tile6(uv, fuv) :
            //        tile7(uv, fuv)
            //        ;

            //}











            
            // 6.3 // 12.7 // 18.9 // 25.2
            float baseTruchet(float2 uv) {
                uv -= 100;
                uv.x = uv.x * sign(cos(length(floor(uv * points)) * (randomization)));
                uv = frac(uv);
                
                float multi;
                
                return (cos(min(length(uv), length(--uv)) * (octaves*(6.2831))+6.28) + 1.) / 2.;
            }

            float triangles(float2 uv) {
                float2 local = uv%1;
                float2 id = floor(uv);
                local.x *= sign(cos(length(id)*9526.7 * (randomization)));
                local.y *= sign(cos(length(id) * 34326.7 * (randomization)));
                local = frac(local);
                return local.x+local.y < 1;
            }

            float triangles2(float2 uv) {
                uv += 5;
                float2 local = uv % 1;
                float2 id = floor(uv);
                local.x *= sign(cos(length(id) * 96.7 * (randomization)));
                local.y *= sign(cos(length(id) * 343.7 * (randomization)));
                local = frac(local);
                return abs(local.x - local.y) < .2;
            }

            float testt(float2 uv) {
                uv += 5;
                float2 local = uv % 1;
                float2 id = floor(uv);
                local.x *= sign(cos(length(id) * 96.7 * (randomization)));
                local.y *= sign(cos(length(id) * 343.7 * (randomization)));
                local = frac(local);
                float val = sin(length(local)*6.28);
                return val;
            }


            fixed4 frag(v2f i) : SV_Target
            {


            //float C = octivized(i.uv);
            float C = 0;
            switch (type) {
            case 0: C = baseTruchet(i.uv * tiles); break;
            case 1: C = triangles(i.uv * tiles); break;
            case 2: C = triangles2(i.uv * tiles); break;
            case 3: C = getTiles(frac(i.uv * tiles), i.uv); break;
            }
            // sample the texture
            fixed4 col = tex2D(_GradientTex, float2(C,0));
            return col;
            }
            ENDCG
        }
    }
}
