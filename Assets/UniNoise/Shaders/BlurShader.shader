Shader "Hidden/TexBlurShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _InputTex("Input", 2D) = "white"{}
        _Blur("Blur Count", float) = 3
        _BlurSpread("Blur Spread", float) = 1
        _TextureSize("Texture Size", float) = 256
        _Threshold("Threshold", Range(0,1)) = 1
        _Quality("Quality", int) = 4
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

            float4 _MainTex_ST;
            float _Blur;
            float _BlurSpread;
            int _Quality;
            float _Threshold;

            float _TextureSize;

            //float4 _MainTex_TexelSize;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            float4 getBlur(float2 uv) {

                float4 base = tex2D(_InputTex, uv);
                float4 c = (0, 0, 0, 0);
                float perDistance = ((1.0 / _TextureSize) * _BlurSpread)/_Quality;
                int added = 0;
                for (int x = -_Quality; x <= _Quality; x++) {
                    for (int y = -_Quality; y <= _Quality; y++) {
                        float4 current = tex2D(_InputTex, uv + (float2(perDistance * x, perDistance * y) * _BlurSpread));
                        if (length(saturate(current.rgb) - saturate(base.rgb)) <= _Threshold*2) {
                            added++;
                            c += current;
                        }
                        
                    }
                }

                c /= added;
                c = lerp(base, c, _Blur);

                return c;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                //float z = 1.0/ _TextureSize;
                //float w = 1.0/ _TextureSize;

                //float4 color = tex2D(_InputTex, i.uv);
                /*for (int x = -_Blur; x < _Blur; x++) {
                    for (int y = -_Blur; y < _Blur; y++) {
                        color += tex2D(_InputTex, i.uv + (float2(x*z, y*w)* _BlurSpread));
                    }
                }*/

                /*float per = _Blur / 3.1415;
                float count = 1;
                for (int I = 0; I < _Blur; I++) {
                    float2 coords = float2(sin(per * I) * z, cos(per * I) * w);
                    color += tex2D(_InputTex, i.uv + (coords * _BlurSpread));
                    count++;
                }*/


                //color /= count;
                
                //color = tex2D(_InputTex, i.uv);

                return getBlur(i.uv);
            }
            ENDCG
        }
    }
}
