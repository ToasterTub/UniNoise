Shader "Hidden/UniNoiseGrid"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _Octaves("Octaves", int) = 4
        _Type("Type", int) = 0
        _Width("Width", float) = .1
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
            int _Type;
            float4 _MainTex_ST;
            int _Octaves;
            float _Width;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 basicGrid(float2 uv) {
                float localX = frac(uv.x * _Octaves);
                float localY = frac(uv.y * _Octaves);

                float g = min(localX, localY) < _Width;

                fixed4 col = tex2D(_GradientTex, float2(g, 0));

                return col;
            }

            fixed4 halfGrid(float2 uv) {
                float localX = frac(uv.x * _Octaves);
                float localY = frac(uv.y * _Octaves);

                float localDoubleX = frac((uv.x * _Octaves * 2) - (_Width) +(_Width / 4.0));
                float localDoubleY = frac((uv.y * _Octaves * 2) - (_Width) + (_Width/4.0));

                float g = min(localX, localY) < _Width;
                float g2 = min(localDoubleX, localDoubleY) < _Width/2.0 ? .5 : g;
                g = g > 0 ? g : g2;

                fixed4 col = tex2D(_GradientTex, float2(g, 0));

                return col;
            }

            fixed4 smoothedGrid(float2 uv) {
                float localX = frac(uv.x * _Octaves);
                float localY = frac(uv.y * _Octaves);

                float minl = min(localX, localY);
                float g = minl < _Width;

                if (g > 0 && ((uv.x + uv.y) * _Octaves) % 2 > 1) {
                    g = localX < _Width ? localX / _Width : localY / _Width ;
                }
                else {
                    g = localY < _Width ? localY / _Width : localX / _Width ;
                }
                

                fixed4 col = tex2D(_GradientTex, float2(g, 0));

                return col;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                switch (_Type) {
                case 0: return basicGrid(i.uv);
                case 1: return halfGrid(i.uv);
                case 2: return smoothedGrid(i.uv);
                }
                fixed4 col = basicGrid(i.uv);
                return col;
            }
            ENDCG
        }
    }
}
