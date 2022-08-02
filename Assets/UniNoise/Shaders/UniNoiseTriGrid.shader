Shader "Hidden/UniNoiseTriGrid"
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

            sampler2D _GradientTex;
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float scaleX;
            float scaleY;
            int wobbleSize = 1;
            float wobble = 0;
            float wobble2 = 0;
            int flipHalfY;
            int type;


            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }


            float triGrid(float2 uv) { // uv local 0-1
                float v = 0;

                uv.x = abs(uv.x - .5);
                
                return (uv.x + (uv.y/2)) > .5;
            }

            float triGridHex(float2 uv) { // uv local 0-1

                float d1 = length(uv - float2(0, 1));
                float d2 = length(uv - float2(1, 1));
                float d3 = length(uv - float2(.5, 0));

                return abs((min(min(d1, d2), d3)))*1.5;
            }

            float triGridPoint(float2 uv) { // uv local 0-1
                uv.x = abs(uv.x-.5)*2;
                float d1 = length(uv - float2(0, 0));
                float d2 = length(uv - float2(1, 1));

                return abs((max(d1, d2)-1))*2;
            }

            float DistToLine(float2 pt1, float2 pt2, float2 uv)
            {
                float2 lineDir = pt2 - pt1;
                float2 perpDir = float2(lineDir.y, -lineDir.x);
                float2 dirToPt1 = pt1 - uv;
                return abs(dot(normalize(perpDir), dirToPt1));
            }

            float triTri(float2 uv) {
                float F1 = DistToLine(float2(0, 0), float2(.5, 1), uv);
                float F2 = DistToLine(float2(1, 0), float2(.5, 1), uv);
                float F3 = DistToLine(float2(0, 0), float2(1, 0), uv);
                float F4 = DistToLine(float2(0, 1), float2(1, 1), uv);
                return min(min(F1, F2), min(F3, F4))*3;
            }

            

            float2 applyWarp(float2 uv) {
                float2 F = float2(0,0);
                F.y = sin(uv.x * ((6.28) * wobbleSize)) * wobble;
                F.x = sin(uv.y * ((6.28) * wobbleSize)) * wobble2;

                return F;
            }


            float getTris(float2 uv) {

                if (flipHalfY) {
                    //scaleX = round(scaleX * 2) / 2;
                    scaleY = round(scaleY * .5) * 2;
                }

                uv += 10;
                float2 holdUV = uv;
                uv += applyWarp(holdUV);
                int needsFlip = flipHalfY ? ((uv.y * round(scaleY)) % 2 < 1 ? 0 : 1) : 0;

                float totalX = uv.x * round(scaleX);
                float totalY = uv.y * round(scaleY);
                uv = float2(totalX, totalY);

                uv = uv % 1;

                uv.y = needsFlip ? 1 - uv.y : uv.y;

                float F = 0;

                switch (type) {
                    case 0: F = triGrid(uv); break;
                    case 1: F = triTri(uv); break;
                    case 2: F = triGridHex(uv); break;
                    case 3: F = triGridPoint(uv); break;
                }

                return F;

                //return triGrid(float2(uv.x, uv.y));
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float v = getTris(i.uv);
                fixed4 col = tex2D(_GradientTex, v);
                return col;
            }
            ENDCG
        }
    }
}
