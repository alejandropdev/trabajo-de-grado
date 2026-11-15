// Direccion de arte (2026-11-15): UI que SUMA luz en vez de taparla. Lo cargan LuzUI (Resources/Shaders/UIAditivo) y
// las laminas de muestra para los halos, haces y motas. Mismo contrato que UI/Default
// (color de vertice, textura, recorte por RectMask2D), con mezcla aditiva.
Shader "Nexus/UI Aditivo" {
    Properties {
        [PerRendererData] _MainTex ("Textura", 2D) = "white" {}
        _Color ("Tinte", Color) = (1,1,1,1)
    }
    SubShader {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha One
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            struct appdata { float4 vertex : POSITION; float4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f { float4 pos : SV_POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; float4 mundo : TEXCOORD1; };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _ClipRect;

            v2f vert (appdata v) {
                v2f o;
                o.mundo = v.vertex;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                fixed4 c = tex2D(_MainTex, i.uv) * i.color;
                #ifdef UNITY_UI_CLIP_RECT
                c.a *= UnityGet2DClipping(i.mundo.xy, _ClipRect);
                #endif
                return c;
            }
            ENDCG
        }
    }
}
