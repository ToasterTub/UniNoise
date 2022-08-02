Shader "Hidden/UniNoiseStrokes"
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
            float layerWeight = .9;
            float power = .5;

            float scale = 2;
            float detailSize = 2;
            float scratchCount = 3;
            float brightness = 1;
            float contrast = 1;
            float squiggles = 1;
            int octaves = 1;
            float seed = 1;
            int type = 0;
            float sides = 0;

            float testFloat = 1;

            float rand(float2 p) {
                return frac(sin(p.x * 347.489 + p.y * 43.246) * (57306.54+ seed));
            }

            float2 toLocal(float2 uv, float size) {
                return frac(uv * scale * size);
            }

            float2 toGrid(float2 uv, float size) {
                float2 grid = floor((uv * scale * size));
                return grid;
            }

            float3 gridValues(float2 gridPos, float size) {
                float gridAngle = (rand(gridPos + size))*639.28;
                float angleScale = (rand(gridPos * .83 + size)* 3);
                float gridOffset = (rand(gridPos*.743 + size)) * 639.28;
                return float3(gridAngle, angleScale, gridOffset);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 rotateUV(float2 uv, float a)
            {
                float X = cos(a) * (uv.x - .5) + sin(a) * (uv.y - .5) + .5;
                float Y = cos(a) * (uv.y - .5) - sin(a) * (uv.x - .5) + .5;
                return float2(X,Y);
            }

            float onePos(float2 grid, float2 local, float size) {
                float3 strokeValues = gridValues(grid, size);

                float2 angled = float2(sin(strokeValues.x), cos(strokeValues.x));


                float2 linePos = local-1;
                linePos = rotateUV(linePos, strokeValues.x);

                float2 offset = float2(sin(linePos.y * strokeValues.y), cos(linePos.x* strokeValues.y))* squiggles;
                linePos += offset;

                float Y = (linePos.y) * scratchCount * 3.14;
                float lines = pow(abs(sin(Y)), 1/power);

                float S1 = ((sin(linePos.y * lines) + 1) / 2);
                float S2 = ((sin(linePos.y * lines *3) + 1) / 2);
                //float S3 = ((sin(linePos.x * 15.7) + 1) / 2) * ((sin(linePos.x * 9.5) + 1) / 2) * ((sin(linePos.x * 12.8) + 1) / 2);

                float YCuts = abs(Y-3.14) * ((S1+S2));
                lines = lerp(lines, lines * YCuts, sides);


                return lines * brightness;



                /////////////////////////////////// old stuff below

                //float2 angled = float2(sin(strokeValues.x), cos(strokeValues.x));
                //float2 lOff = float2(sin(strokeValues.z), cos(strokeValues.z)); 
                //local += lOff*2.; /////////////////////////// adds waves
                //float angDiff = dot(normalize(local - .5), normalize(angled));

                //float2 offs = float2(sin(angDiff), cos(angDiff));

                //float2 offs2 = float2((local.y) + sin(angDiff* 1 +(offs.x*(3.))), cos(angDiff * squiggles + (offs.y * 3.)));
                //float2 finalOffs = float2((local.x * (offs2.x)) , (local.y * (offs2.y)));
                //float weightAngle = (finalOffs.x + finalOffs.y); 


                //float weight = (sin((weightAngle *(1+(angled.y*.5))) * scratchCount));

                //float finalShape = saturate(abs(sin(angDiff) * weight));

                //float value = ((1 - pow(sin(finalShape), power * 2))) * brightness;


                //return pow(value, 1-(power/2)); 
            }

            float2 clampToGrid(float2 grid, float size) {
                grid.x = grid.x >= scale*size ? 0 : grid.x;
                grid.y = grid.y >= scale*size ? 0 : grid.y;

                return grid;
            }

            float2 stroke(float2 uv, float size) {
                float2 local = toLocal(uv, size);
                float2 grid = toGrid(uv, size);

                float bl = onePos(grid, local, size);
                float br = onePos(clampToGrid(grid + float2(1, 0), size), local + float2(-1, 0), size);
                float tl = onePos(clampToGrid(grid + float2(0, 1), size), local + float2(0, -1), size);
                float tr = onePos(clampToGrid(grid + float2(1, 1), size), local + float2(-1, -1), size);
                
                float B = lerp(bl, br, local.x);
                float T = lerp(tl, tr, local.x);
                float M = lerp(B, T, local.y);
                return saturate(M);
            }

            float getOctavesAdd(float2 uv) {
                float total = 0;
                float current = 1;
                float v = 0;

                for (int i = 0; i < octaves; i++) {
                    v += stroke(uv, (1 + (i* detailSize))) * current;
                    total += current;
                    current *= layerWeight;
                }

                //((value - .5)* contrast) + .5
                v = ((v - .5) * contrast) + .5;
                return clamp(v-.1,0,1);
            }

            float getOctavesBlend(float2 uv) {
                float total = 0;
                float current = 1;
                float v = 0;

                for (int i = 0; i < octaves; i++) {
                    v += stroke(uv, (1 + (i * detailSize))) * current;
                    total += current;
                    current *= layerWeight;
                }

                //((value - .5)* contrast) + .5
                v = (((v/total) - .5) * contrast) + .5;
                return clamp(v - .1, 0, 1);
            }

            float getOctavesMax(float2 uv) {
                float v = 0;
                float current = 1;
                for (int i = 0; i < octaves; i++) {
                    float C = stroke(uv, (1 + (i * detailSize))) * current;
                    current *= layerWeight;
                    v = max(v, C);
                }

                //((value - .5)* contrast) + .5
                v = ((v - .5) * contrast) + .5;
                return clamp(v - .1, 0, 1);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float S;

                switch (type) {
                case 0: S = getOctavesAdd(i.uv); break;
                case 1: S = getOctavesBlend(i.uv); break;
                case 2: S = getOctavesMax(i.uv); break;
                }

                fixed4 col = tex2D(_GradientTex, float2(S,0));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
