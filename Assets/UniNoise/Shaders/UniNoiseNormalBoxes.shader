Shader "Hidden/UniNoiseNormalBoxes"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _Octaves("Octaves", int) = 3
        _TextureSize("Tex Size", int) = 256
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
            float2 _points[1000];
            int _TextureSize;
            int _Octaves;

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
                int boxX = floor(i.uv.x * _Octaves);
                int boxY = floor(i.uv.y * _Octaves);
                int arrayIndex = (boxX * _Octaves) + boxY;
                int nextIndex = (boxX * _Octaves) + boxY + 1;
                nextIndex = nextIndex > _Octaves* _Octaves ? 0 : nextIndex;

                float localX = frac(i.uv.x * _Octaves);
                float localY = frac(i.uv.y * _Octaves);

                float2 box = _points[arrayIndex];
                float2 nextBox = _points[nextIndex];

                float vx = lerp(box.x, nextBox.x, localX);
                float vy = lerp(box.y, nextBox.y, localY);

                float V = (vx + vy)/2.0;//length(box - float2(localX * localY, localX * localY));

                fixed4 col = tex2D(_GradientTex, float2(V,0));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
