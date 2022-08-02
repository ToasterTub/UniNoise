Shader "Hidden/UniNoiseGradientShader"
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
            float rotation = 0;
            int flip = 0;
            int snapAngle = 0;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 rotate(float2 uv) {
                rotation = snapAngle ? (round(rotation * 8) / 8) : rotation;
                float sinX = sin(rotation * 6.28);
                float cosX = cos(rotation * 6.28);
                float2x2 RM = float2x2(cosX, -sinX, sinX, cosX);
                uv = uv - .5;
                uv = mul(uv, RM);
                return uv + .5;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                i.uv = rotate(i.uv);
                float v = flip ? i.uv.x : 1 - i.uv.x;
                fixed4 col = tex2D(_GradientTex, float2(v,0));
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}
 