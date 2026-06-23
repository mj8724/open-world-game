Shader "UI/GlassmorphismBlur"
{
    Properties
    {
        _BlurSize ("Blur Size", Range(0, 10)) = 2.0
        _OverlayColor ("Overlay Color", Color) = (0,0,0,0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        GrabPass { "_BackgroundTexture" }
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 grabPos : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            sampler2D _BackgroundTexture;
            float _BlurSize;
            float4 _OverlayColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.grabPos = ComputeGrabScreenPos(o.pos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Simple blur sampling logic mock
                float4 col = tex2Dproj(_BackgroundTexture, i.grabPos);
                // Blend with overlay color for frosted glass look
                col.rgb = lerp(col.rgb, _OverlayColor.rgb, _OverlayColor.a);
                return col;
            }
            ENDCG
        }
    }
}
