Shader "UI/IrisOverlay"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Radius ("Radius", Float) = 0
        _Center ("Center", Vector) = (0.5, 0.5, 0, 0)
        _Aspect ("Aspect", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _Radius;
            float2 _Center;
            float _Aspect;

            v2f vert(appdata_t input)
            {
                v2f output;

                output.vertex = UnityObjectToClipPos(input.vertex);
                output.texcoord = input.texcoord;
                output.color = input.color * _Color;

                return output;
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 offset = input.texcoord - _Center;
                offset.x *= _Aspect;

                float distanceFromCenter = length(offset);

                float outsideHole =
                    step(_Radius, distanceFromCenter);

                fixed textureAlpha =
                    tex2D(_MainTex, input.texcoord).a;

                fixed alpha =
                    textureAlpha *
                    input.color.a *
                    outsideHole;

                return fixed4(0, 0, 0, alpha);
            }

            ENDCG
        }
    }
}