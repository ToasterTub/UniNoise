Shader "Hidden/VornoiShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _VectorCount("Vector Count", int) = 1
        _GradientTex("Gradient", 2D) = "white"{}
        _Type("Type", int) = 0
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
            int _VectorCount;
            float4 _points[1000];
            float yPosition;
            float yMultiplier;
            float warpX;
            float warpY;
            float contrast = 1;
            float gain = 0;
            float octaveWeight;

            int _Type;
            int octaves;

            static const float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float3 hsvTOrgb(float3 input) {
                float3 p = abs(frac(input.xxx + K.xyz) * 6.0 - K.www);

                return input.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), input.y);
            }

            float nearestSeamless(float3 position, float3 inPoint, out float x, out float y, out float z) {
                position.y *= yMultiplier;
                float3 seamPoints[27];
                seamPoints[0] = inPoint;

                seamPoints[1] = inPoint + float3(1, 0, 0);
                seamPoints[2] = inPoint + float3(-1, 0, 0);
                seamPoints[3] = inPoint + float3(0, 0, 1);
                seamPoints[4] = inPoint + float3(0, 0, -1);

                seamPoints[5] = inPoint + float3(1, 0, 1);
                seamPoints[6] = inPoint + float3(-1, 0, -1);
                seamPoints[7] = inPoint + float3(-1, 0, 1);
                seamPoints[8] = inPoint + float3(1, 0, -1);

                seamPoints[9] = inPoint + float3(1, yMultiplier, 0);
                seamPoints[10] = inPoint + float3(-1, yMultiplier, 0);
                seamPoints[11] = inPoint + float3(0, yMultiplier, 1);
                seamPoints[12] = inPoint + float3(0, yMultiplier, -1);

                seamPoints[13] = inPoint + float3(1, yMultiplier, 1);
                seamPoints[14] = inPoint + float3(-1, yMultiplier, -1);
                seamPoints[15] = inPoint + float3(-1, yMultiplier, 1);
                seamPoints[16] = inPoint + float3(1, yMultiplier, -1);

                seamPoints[17] = inPoint + float3(1, -yMultiplier, 0);
                seamPoints[18] = inPoint + float3(-1, -yMultiplier, 0);
                seamPoints[19] = inPoint + float3(0, -yMultiplier, 1);
                seamPoints[20] = inPoint + float3(0, -yMultiplier, -1);

                seamPoints[21] = inPoint + float3(1, -yMultiplier, 1);
                seamPoints[22] = inPoint + float3(-1, -yMultiplier, -1);
                seamPoints[23] = inPoint + float3(-1, -yMultiplier, 1);
                seamPoints[24] = inPoint + float3(1, -yMultiplier, -1);

                seamPoints[25] = inPoint + float3(0, -yMultiplier, 0);
                seamPoints[26] = inPoint + float3(0, yMultiplier, 0);

                float nearest = 99999;
                int selected = 0;

                for (int i = 0; i < 27; i++) {

                    float dist = length(seamPoints[i]- position);

                    selected = dist < nearest ? i : selected;
                    nearest = dist < nearest ? dist : nearest;
                }

                x = seamPoints[selected].x;
                y = seamPoints[selected].y;
                z = seamPoints[selected].z;

                return nearest;
            }

            float4 distanceToNearest(float3 position, int offset) {
                int arrayOffset = (offset) * _VectorCount;
                float nearest = 9999;
                int selected = 0;

                float H = 0;

                for (int i = 0; i < _VectorCount; i++) {
                    float xPos = _points[i+arrayOffset].x;
                    float yPos = _points[i+arrayOffset].y;
                    float zPos = _points[i+arrayOffset].z;

                    float cx, cy, cz;

                    float dist = nearestSeamless(float3(xPos, yPos, zPos), position, cx, cy, cz);

                    if (dist < nearest) {
                        selected = i;
                        nearest = dist;
                        H = _points[i].w;
                    }

                }
                float secondNearest = 9999;

                if (_Type > 2) {
                    

                    for (int i = 0; i < _VectorCount; i++) {
                        float xPos = _points[i+arrayOffset].x;
                        float yPos = _points[i+arrayOffset].y;
                        float zPos = _points[i+arrayOffset].z;

                        float cx, cy, cz, H;

                        float dist = nearestSeamless(float3(xPos, yPos, zPos), position, cx, cy, cz);

                        if (dist < secondNearest && dist > nearest) {
                            selected = i;
                            secondNearest = dist;
                            H = _points[i].w;
                        }

                    }
                    float val = secondNearest * ((.5 + (_VectorCount / 100.0)) * 3);
                    val = (((val - .5) * contrast) + .5);

                    float4 col = float4(0, 0, 0, 0);
                    switch (_Type) {
                        case 4: return float4(hsvTOrgb(float3(H, 1, secondNearest * ((.5 + (_VectorCount / 100.0))*4))), 1); 
                        case 2: return tex2D(_GradientTex, float2(val + gain, 0)); 
                        case 3: return tex2D(_GradientTex, float2(val + gain, 0)); 
                    }
                    
                    //return col;
                    

                } // this warning is annoying, I need to clean this shader up.

                
                switch (_Type) {
                    case 1: return float4(hsvTOrgb(float3(H, 1, nearest * ((.5 + (_VectorCount / 100.0)) * 4))), 1);
                    case 2: return float4(hsvTOrgb(float3(H, 1, 1)), 1);
                }

                float val = nearest * ((.5 + (_VectorCount / 100.0)) * 3);
                

                float val2 = secondNearest * ((.5 + (_VectorCount / 100.0)) * 3);

                

                switch (_Type) {
                case 5: return tex2D(_GradientTex, float2(saturate(((((val2/val) - .5) * contrast) + .5) + gain), 0));
                }


                val = (((val - .5) * contrast) + .5);
                
                return tex2D(_GradientTex, float2(val + gain,0));
            }


            float4 getOctaves(float3 pos) {
                float total = 0;
                float current = 1;
                float4 value = 0;

                for (float i = 0; i < octaves; i++) {
                    value+= distanceToNearest((pos*(i+1))%1, i%5) * current;
                    total += current;
                    current *= octaveWeight;
                }

                return value / total;
            }


            fixed4 frag(v2f i) : SV_Target
            {
                i.uv.xy += float2(sin(i.uv.y * 6.28) * warpX, cos(i.uv.x * 6.28) * warpY);
                i.uv.xy = frac(i.uv.xy);
                //float4 F = (distanceToNearest(float3(i.uv.x, yPosition * yMultiplier,i.uv.y)));
                float4 F = (getOctaves(float3(i.uv.x, yPosition * yMultiplier, i.uv.y)));
                return F;
            }
            ENDCG
        }
    }
}
