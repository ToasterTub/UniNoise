Shader "Hidden/NewPerlin"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _Octaves("Octaves", Range(1,10)) = 5
        _Scale("Scale", Range(1,30)) = 5
        _Type("Type", int) = 0
        _Power("Power", Range(1,10)) = 1
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
            sampler2D curveTex;
            float4 _MainTex_ST;
            int _Octaves;
            int _ScaleX = 20; // larger is more complex
            int _ScaleY = 20;
            int _Type;
            float seed = 0;
            float step = 0; // smooth curve || -1 diamond || 0 linear || 1 smooth || 2 square
            float contrast = 0;
            float lacunarity; // rate at which octave frequency increases
            float _Power;
            float weight = .8;
            int turbulent = 0;
            float gain = 0;
            
            static const float pi = 3.14159265359;
            static const float doublePi = 6.28318530718;

            float random(float2 p, float scale, float seedOffset = 0)
            {
                p.x += seed;
                p.y += seed * .99;
                float value = frac(cos(p.x * 10.12 + p.y * 212.92 + seedOffset) * (99999. * (scale*.1)));
                return value;
            }

            float2 pointToDir(float ang) { // ang is 0. through 1.
                //ang = ang * 45663.4;
                return float2(sin(ang* doublePi), cos(ang * doublePi));
            }

            float randomPoint(float2 position, float scale, float seedOffset = 0) {

                float2 uv = position % (scale);
                int xPos = floor(uv.x * _ScaleX);
                int yPos = floor(uv.y * _ScaleY);
                return (random(float2(xPos, yPos), scale, seedOffset));
            }

            float2 gridPosition(float2 uv) {
                return float2(floor(uv.x * _ScaleX), floor(uv.x * _ScaleY));
            }

            float2 fade(float2 input) {
                return input * input *(3.-2.*input);
            }

            float fade(float input) {
                return input * input * (3. - 2. * input);
            }

            float getPoint(float2 uv, float scale, float seedOffset = 0) {

                float2 lPos = frac(uv * float2(_ScaleX, _ScaleY));

                float2 cPos = lPos - .5;
                lPos = saturate(lerp(lPos, fade(lPos), step));

                float BL = randomPoint(uv, scale, seedOffset);
                float BR = randomPoint(uv + float2(1.0/ _ScaleX, 0), scale, seedOffset);

                float TL = randomPoint(uv + float2(0, 1.0 / _ScaleY), scale, seedOffset);
                float TR = randomPoint(uv + float2(1.0 / _ScaleX, 1.0 / _ScaleY), scale, seedOffset);

                float bottom = lerp(BL, BR, lPos.x);
                float top = lerp(TL, TR, lPos.x);
                float mix = lerp(bottom, top, lPos.y);
                return mix;
            }

            float getPointV2(float2 uv, float scale, float seedOffset = 0) {

                float2 lPos = frac(uv * float2(_ScaleX, _ScaleY));
                //lPos = saturate(lerp(lPos, fade(lPos), step));

                float2 cPos = lPos - .5;

                //float smoothX = (sin(cPos.x * 3.14) + 1) / 2.0;
                //float smoothY = (sin(cPos.y * 3.14) + 1) / 2.0;

                //lPos.x = smoothX;
                //lPos.y = smoothY;
                //lPos = saturate(lerp(lPos, fade(lPos), step));;

                float2 BL = pointToDir(randomPoint(uv, scale, seedOffset))*.5;
                float2 BR = pointToDir(randomPoint(uv + float2(1.0 / _ScaleX, 0), scale, seedOffset)) * .5;

                float2 TL = pointToDir(randomPoint(uv + float2(0, 1.0 / _ScaleY), scale, seedOffset)) * .5;
                float2 TR = pointToDir(randomPoint(uv + float2(1.0 / _ScaleX, 1.0 / _ScaleY), scale, seedOffset)) * .5;

                //lPos -= .5;
                float2 blp = float2(lPos.x, lPos.y);
                float2 brp = float2(1-lPos.x, lPos.y);
                float2 tlp = float2(lPos.x, 1-lPos.y);
                float2 trp = float2(1-lPos.x, 1-lPos.y);
                //lPos += .5;

                float bottom = abs(lerp(dot(BL, blp), dot(BR, brp), lPos.x));
                float top = abs(lerp(dot(TL, tlp), dot(TR, trp), lPos.x));
                float mix = abs(lerp(bottom, top, lPos.y));
                
                return mix*2;
            }

            float perlin(float2 uv, float seedOffset = 0) {
                float value = 0;

                float totalAmp = 0;
                float currentAmp = 1;

                if (!turbulent) {
                    for (int i = 0; i < _Octaves; i++) {
                        float scale = ((i * lacunarity) + 1.0);
                        value += getPoint(uv * scale, scale, .3452) * currentAmp;
                        totalAmp += currentAmp;
                        currentAmp *= weight;
                    }
                }
                else {
                    for (int i = 0; i < _Octaves; i++) {
                        float scale = ((i * lacunarity) + 1.0);
                        float C = getPoint(uv * scale, scale, .3452);
                        C = abs(sin((C*2) * 3.14)*.75);
                        value += C * currentAmp;
                        totalAmp += currentAmp;
                        currentAmp *= weight;
                    }
                }
                return value / totalAmp;
            }

            float lines(float2 uv) {
                float P = perlin(uv);

                float2 lPos = uv;
                lPos.x += (P - .5)* _Power;
                float value = (sin(lPos.x * doublePi * _ScaleX) + 1.)/2.;
                return value;
            }

            float dots(float2 uv) {
                float P = perlin(uv);
                float P2 = perlin(uv + .9, 4323.21);

                float2 lPos = uv;
                lPos.x += (P - .5) * _Power;
                lPos.y += (P2 - .5) * _Power;
                
                float valX = sin(lPos.x * doublePi * _ScaleX) + 1;
                float valY = sin(lPos.y * doublePi * _ScaleY) + 1;

                return (valX + valY)/4.0;
            }

            float grid(float2 uv) {
                float P = perlin(uv) * _Power;
                float P2 = perlin(uv + .9, 4323.21) * _Power;

                float2 lPos = uv;
                lPos.x += (P - .5);
                lPos.y += (P2 - .5);

                float valX = sin(lPos.x * doublePi * _ScaleX) + 1;
                float valY = sin(lPos.y * doublePi * _ScaleY) + 1;

                return min(valX , valY) / 2.0;
            }

            float squares(float2 uv) {
                float P = perlin(uv) * _Power;
                float P2 = perlin(uv + .9, 4323.21) * _Power;

                float2 lPos = uv;
                lPos.x += (P - .5);
                lPos.y += (P2 - .5);

                float valX = sin(lPos.x * doublePi * _ScaleX) + 1;
                float valY = sin(lPos.y * doublePi * _ScaleY) + 1;

                return max(valX, valY) / 2.0;
            }

            float center(float2 uv) {
                float P = ((perlin(uv) - .5) * pi) * _Power;
                float P2 = ((perlin(uv+.9, 4323.21) - .5) * pi)* _Power;
                float val = (cos(atan2(P,P2) + pi) + 1)/2;

                return val;
            }

            float sined(float2 uv) {
                float P = (perlin(uv) - .5) * pi;
                
                float val = (sin(P * _Power) + 1) / 2.0;

                return val;
            }

            float sawed(float2 uv) {
                float P = perlin(uv);

                float val = (P * _Power) % 1.0;

                return val;
            }

            float warped(float2 uv) {
                float P1 = perlin(uv, 1.321);
                float P2 = perlin(uv, 849.2131);

                float2 pos = frac(uv + (float2(P1 - .5, P2 - .5) * _Power));

                float val = perlin(pos, 35687.236);

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
                float value = 0;
                switch (_Type) {
                case 0: value = tex2D(_GradientTex, perlin(i.uv) + gain); break;
                case 1: value = tex2D(_GradientTex, lines(i.uv) + gain); break;
                case 2: value = tex2D(_GradientTex, dots(i.uv) + gain); break;
                case 3: value = tex2D(_GradientTex, grid(i.uv) + gain); break;
                case 4: value = tex2D(_GradientTex, squares(i.uv) + gain); break;
                case 5: value = tex2D(_GradientTex, center(i.uv) + gain); break;
                case 6: value = tex2D(_GradientTex, sined(i.uv) + gain); break;
                case 7: value = tex2D(_GradientTex, sawed(i.uv) + gain); break;
                case 8: value = tex2D(_GradientTex, warped(i.uv) + gain); break;
                }

            value = ((value - .5) * ((1+ contrast))*2) + .5;
            return tex2D(_GradientTex,float2(value,0));
            }
            ENDCG
        }
    }
}



































//Shader "Hidden/NewPerlin"
//{
//    Properties
//    {
//        _MainTex("Texture", 2D) = "white" {}
//        _GradientTex("Gradient Texture", 2D) = "white" {}
//        _Octaves("Octaves", Range(1,10)) = 5
//        _Scale("Scale", Range(1,30)) = 5
//        _Type("Type", int) = 0
//        _Power("Power", Range(1,10)) = 1
//    }
//        SubShader
//        {
//            Tags { "RenderType" = "Opaque" }
//            LOD 100
//
//            Pass
//            {
//                CGPROGRAM
//                #pragma vertex vert
//                #pragma fragment frag
//                // make fog work
//                #pragma multi_compile_fog
//
//                #include "UnityCG.cginc"
//
//                struct appdata
//                {
//                    float4 vertex : POSITION;
//                    float2 uv : TEXCOORD0;
//                };
//
//                struct v2f
//                {
//                    float2 uv : TEXCOORD0;
//                    UNITY_FOG_COORDS(1)
//                    float4 vertex : SV_POSITION;
//                };
//
//                sampler2D _MainTex;
//                sampler2D _GradientTex;
//                sampler2D curveTex;
//                float4 _MainTex_ST;
//                int _Octaves;
//                int _Scale = 20; // larger is more complex
//                int _Type;
//                float seed = 0;
//                float step = 0; // smooth curve || -1 diamond || 0 linear || 1 smooth || 2 square
//                float contrast = 0;
//                float lacunarity; // rate at which octave frequency increases
//                float _Power;
//                float weight = .8;
//                int turbulent = 0;
//                float gain = 0;
//
//                static const float pi = 3.14159265359;
//                static const float doublePi = 6.28318530718;
//
//                float random(float2 p, float scale, float seedOffset = 0)
//                {
//                    p.x += seed;
//                    p.y += seed * .99;
//                    float value = frac(cos(p.x * 10.12 + p.y * 212.92 + seedOffset) * (99999. * (scale * .1)));
//                    return value;
//                }
//
//                float2 pointToDir(float ang) { // ang is 0. through 1.
//                    //ang = ang * 45663.4;
//                    return float2(sin(ang * doublePi), cos(ang * doublePi));
//                }
//
//                float randomPoint(float2 position, float scale, float seedOffset = 0) {
//
//                    float2 uv = position % (scale);
//                    int xPos = floor(uv.x * _Scale);
//                    int yPos = floor(uv.y * _Scale);
//                    return (random(float2(xPos, yPos), scale, seedOffset));
//                }
//
//                float2 gridPosition(float2 uv) {
//                    return float2(floor(uv.x * _Scale), floor(uv.x * _Scale));
//                }
//
//                float2 fade(float2 input) {
//                    return input * input * (3. - 2. * input);
//                }
//
//                float fade(float input) {
//                    return input * input * (3. - 2. * input);
//                }
//
//                float getPoint(float2 uv, float scale, float seedOffset = 0) {
//
//                    float2 lPos = frac(uv * _Scale);
//
//                    float2 cPos = lPos - .5;
//                    lPos = saturate(lerp(lPos, fade(lPos), step));
//
//                    float BL = randomPoint(uv, scale, seedOffset);
//                    float BR = randomPoint(uv + float2(1.0 / _Scale, 0), scale, seedOffset);
//
//                    float TL = randomPoint(uv + float2(0, 1.0 / _Scale), scale, seedOffset);
//                    float TR = randomPoint(uv + float2(1.0 / _Scale, 1.0 / _Scale), scale, seedOffset);
//
//                    float bottom = lerp(BL, BR, lPos.x);
//                    float top = lerp(TL, TR, lPos.x);
//                    float mix = lerp(bottom, top, lPos.y);
//                    return mix;
//                }
//
//                float getPointV2(float2 uv, float scale, float seedOffset = 0) {
//
//                    float2 lPos = frac(uv * _Scale);
//                    //lPos = saturate(lerp(lPos, fade(lPos), step));
//
//                    float2 cPos = lPos - .5;
//
//                    //float smoothX = (sin(cPos.x * 3.14) + 1) / 2.0;
//                    //float smoothY = (sin(cPos.y * 3.14) + 1) / 2.0;
//
//                    //lPos.x = smoothX;
//                    //lPos.y = smoothY;
//                    //lPos = saturate(lerp(lPos, fade(lPos), step));;
//
//                    float2 BL = pointToDir(randomPoint(uv, scale, seedOffset)) * .5;
//                    float2 BR = pointToDir(randomPoint(uv + float2(1.0 / _Scale, 0), scale, seedOffset)) * .5;
//
//                    float2 TL = pointToDir(randomPoint(uv + float2(0, 1.0 / _Scale), scale, seedOffset)) * .5;
//                    float2 TR = pointToDir(randomPoint(uv + float2(1.0 / _Scale, 1.0 / _Scale), scale, seedOffset)) * .5;
//
//                    //lPos -= .5;
//                    float2 blp = float2(lPos.x, lPos.y);
//                    float2 brp = float2(1 - lPos.x, lPos.y);
//                    float2 tlp = float2(lPos.x, 1 - lPos.y);
//                    float2 trp = float2(1 - lPos.x, 1 - lPos.y);
//                    //lPos += .5;
//
//                    float bottom = abs(lerp(dot(BL, blp), dot(BR, brp), lPos.x));
//                    float top = abs(lerp(dot(TL, tlp), dot(TR, trp), lPos.x));
//                    float mix = abs(lerp(bottom, top, lPos.y));
//
//                    return mix * 2;
//                }
//
//                float perlin(float2 uv, float seedOffset = 0) {
//                    float value = 0;
//
//                    float totalAmp = 0;
//                    float currentAmp = 1;
//
//                    if (!turbulent) {
//                        for (int i = 0; i < _Octaves; i++) {
//                            float scale = ((i * lacunarity) + 1.0);
//                            value += getPoint(uv * scale, scale, .3452) * currentAmp;
//                            totalAmp += currentAmp;
//                            currentAmp *= weight;
//                        }
//                    }
//                    else {
//                        for (int i = 0; i < _Octaves; i++) {
//                            float scale = ((i * lacunarity) + 1.0);
//                            float C = getPoint(uv * scale, scale, .3452);
//                            C = abs(sin((C * 2) * 3.14) * .75);
//                            value += C * currentAmp;
//                            totalAmp += currentAmp;
//                            currentAmp *= weight;
//                        }
//                    }
//                    return value / totalAmp;
//                }
//
//                float lines(float2 uv) {
//                    float P = perlin(uv);
//
//                    float2 lPos = uv;
//                    lPos.x += (P - .5) * _Power;
//                    float value = (sin(lPos.x * doublePi * _Scale) + 1.) / 2.;
//                    return value;
//                }
//
//                float dots(float2 uv) {
//                    float P = perlin(uv);
//                    float P2 = perlin(uv + .9, 4323.21);
//
//                    float2 lPos = uv;
//                    lPos.x += (P - .5) * _Power;
//                    lPos.y += (P2 - .5) * _Power;
//
//                    float valX = sin(lPos.x * doublePi * _Scale) + 1;
//                    float valY = sin(lPos.y * doublePi * _Scale) + 1;
//
//                    return (valX + valY) / 4.0;
//                }
//
//                float grid(float2 uv) {
//                    float P = perlin(uv) * _Power;
//                    float P2 = perlin(uv + .9, 4323.21) * _Power;
//
//                    float2 lPos = uv;
//                    lPos.x += (P - .5);
//                    lPos.y += (P2 - .5);
//
//                    float valX = sin(lPos.x * doublePi * _Scale) + 1;
//                    float valY = sin(lPos.y * doublePi * _Scale) + 1;
//
//                    return min(valX , valY) / 2.0;
//                }
//
//                float squares(float2 uv) {
//                    float P = perlin(uv) * _Power;
//                    float P2 = perlin(uv + .9, 4323.21) * _Power;
//
//                    float2 lPos = uv;
//                    lPos.x += (P - .5);
//                    lPos.y += (P2 - .5);
//
//                    float valX = sin(lPos.x * doublePi * _Scale) + 1;
//                    float valY = sin(lPos.y * doublePi * _Scale) + 1;
//
//                    return max(valX, valY) / 2.0;
//                }
//
//                float center(float2 uv) {
//                    float P = ((perlin(uv) - .5) * pi) * _Power;
//                    float P2 = ((perlin(uv + .9, 4323.21) - .5) * pi) * _Power;
//                    float val = (cos(atan2(P,P2) + pi) + 1) / 2;
//
//                    return val;
//                }
//
//                float sined(float2 uv) {
//                    float P = (perlin(uv) - .5) * pi;
//
//                    float val = (sin(P * _Power) + 1) / 2.0;
//
//                    return val;
//                }
//
//                float sawed(float2 uv) {
//                    float P = perlin(uv);
//
//                    float val = (P * _Power) % 1.0;
//
//                    return val;
//                }
//
//                float warped(float2 uv) {
//                    float P1 = perlin(uv, 1.321);
//                    float P2 = perlin(uv, 849.2131);
//
//                    float2 pos = frac(uv + (float2(P1 - .5, P2 - .5) * _Power));
//
//                    float val = perlin(pos, 35687.236);
//
//                    return val;
//                }
//
//                v2f vert(appdata v)
//                {
//                    v2f o;
//                    o.vertex = UnityObjectToClipPos(v.vertex);
//                    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
//                    UNITY_TRANSFER_FOG(o,o.vertex);
//                    return o;
//                }
//
//                fixed4 frag(v2f i) : SV_Target
//                {
//                    float value;
//                    switch (_Type) {
//                    case 0: value = tex2D(_GradientTex, perlin(i.uv) + gain); break;
//                    case 1: value = tex2D(_GradientTex, lines(i.uv) + gain); break;
//                    case 2: value = tex2D(_GradientTex, dots(i.uv) + gain); break;
//                    case 3: value = tex2D(_GradientTex, grid(i.uv) + gain); break;
//                    case 4: value = tex2D(_GradientTex, squares(i.uv) + gain); break;
//                    case 5: value = tex2D(_GradientTex, center(i.uv) + gain); break;
//                    case 6: value = tex2D(_GradientTex, sined(i.uv) + gain); break;
//                    case 7: value = tex2D(_GradientTex, sawed(i.uv) + gain); break;
//                    case 8: value = tex2D(_GradientTex, warped(i.uv) + gain); break;
//                    }
//
//                value = ((value - .5) * ((1 + contrast)) * 2) + .5;
//                return tex2D(_GradientTex,float2(value,0));
//                }
//                ENDCG
//            }
//        }
//}
//
