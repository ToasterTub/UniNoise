Shader "Hidden/UniNoiseWarp"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        warpX("Warp X", 2D) = "black" {}
        warpY("Warp Y", 2D) = "black" {}
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
            sampler2D _InputTex;
            sampler2D warpX;
            sampler2D warpY;
            float4 _MainTex_ST;

            float powerX = 0;
            float powerY = 0;
            float overallMultiplier = 1;

            int wobbleSizeX = 1;
            int wobbleSizeY = 1;
            float wobblex;
            float wobbley;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float2 wobble(float2 uv) {
                uv += 5;
                uv.y += sin(uv.x  * (6.28 * wobbleSizeX)) * wobblex;
                uv.x += sin(uv.y * (6.28 * wobbleSizeY)) * wobbley;
                return uv;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float WX = ((tex2D(warpX, i.uv) - .5) * powerX)*.5;
                float WY = ((tex2D(warpY, i.uv) - .5) * powerY) * .5;
                // sample the texture
                return tex2D(_InputTex, (wobble(i.uv) + (float2(WX, WY)* overallMultiplier))%1);
            }
            ENDCG
        }
    }
}
