Shader "Hidden/UniNoiseCheckers"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _GradientTex("Gradient Texture", 2D) = "white" {}
        _Octaves("Octaves", int) = 4
        _Wobble("Wobble", float) = 0
        _Wobble2("Wobble2", float) = 0
        _WobbleSize("Wobble Size", int) = 1
        _Teardrop("Teardrop", float) = 0
        _Sphere("Sphere", float) = 0
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
            int _Octaves;
            float _Wobble;
            float _Wobble2;
            float _Teardrop;
            int _WobbleSize;
            float _Sphere;
            int _Type;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float getValue(float2 uv) {
                uv += 1;
                float holdY = uv.y;

                float MulX = uv.x * _Octaves;
                float MulY = uv.y * _Octaves;

                uv.y += sin(MulX * ((6.28/_Octaves)* _WobbleSize)) * _Wobble;
                uv.x += sin(MulY * ((6.28 / _Octaves) * _WobbleSize)) * _Wobble2;

                float sValue = (sin(((uv.x - .5) * (6.28 * _Octaves))) + cos(((uv.y - .5) * (6.28 * _Octaves)))) * _Teardrop; // Teardrop
                uv += float2(sin(sValue), cos(sValue));

                //float2 local = float2(((MulX) % 1) + .5, ((MulY) % 1) + .5) * _Sphere; // funk

                float2 cUV = abs(uv * _Octaves) % 1;
                float sUV = length((abs((uv * _Octaves) % 1)) - .5);
                float v = _Type == 0 ? (cUV.x > .5) : cUV.x;
                v = cUV.y > .5 ? v : 1 - v;
                return lerp(v, sUV, _Sphere);
            }


            fixed4 frag (v2f i) : SV_Target
            {
                // sample the texture
                fixed4 col = tex2D(_GradientTex, float2(getValue(i.uv),0));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
