Shader "Hidden/UniNoisePlaid"
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
            int type = 0;
            float4 plaids[20];
            int plaidCount = 2;

            float plad(float2 uv, float2 local, float2 grid) {
                float isBlank = (grid.x + grid.y) % 2 == 0;
                float blankFlip = grid.x % 2 == 0 && grid.y % 2 == 0;

                if (blankFlip) {
                    return 0;
                }
                else if (isBlank) {
                    return 1;
                }
                else {
                    return .5;
                }

                //return float(blankFlip, isBlank, 0);
            }

            float plad2(float2 uv, float2 local, float2 grid) {
                float isBlank = (grid.x + grid.y) % 2 == 0;
                float blankFlip = grid.x % 2 == 0 && grid.y % 2 == 0;

                if (blankFlip) {
                    return 0;
                }
                else if (isBlank) {
                    return 1;
                }
                else {
                    return frac((local.x+local.y)*4)<.5;
                }

                //return float(blankFlip, isBlank, 0);
            }

            float within(float p, float t, float MIN) {
                return abs(p - t) < MIN;
            }

            float plad3(float2 uv, float2 local, float2 grid) {
                
                float v = 0;
                for (float i = 0; i < plaidCount; i++) { // add up plaids
                    float Cx = within(uv.x, plaids[i].x, plaids[i].w);
                    float Cy = within(uv.y, plaids[i].y, plaids[i].w);
                    float C = max(Cx, Cy)*(plaids[i].z*((Cx+Cy)/2));

                    v = C > 0 ? C : v;
                }

                return v;
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
                float F = 0;
                
                switch (type) {
                case 0: F = plad(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                case 1: F = plad2(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                case 2: F = plad3(i.uv, frac(i.uv * scale), floor(i.uv * scale)); break;
                }

                fixed4 col = tex2D(_GradientTex, float2(F,0));
                return col;
            }
            ENDCG
        }
    }
}
