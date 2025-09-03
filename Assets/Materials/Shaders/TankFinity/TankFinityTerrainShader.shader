Shader "Unlit/TankFinityTerrainShader"
{
    // these are variables you can pass to the shader from the inspector or from a script
    Properties
    {
        // we have removed support for texture tiling/offset,
        // so make them not be displayed in material inspector
        [NoScaleOffset] _MainTex ("Base RGB", 2D) = "white" {}
        _ForegroundColor ("Foreground Color", Color) = (1,0,0)
        _BackgroundColor ("Background Color", Color) = (0,1,0)
    }

    // actual shader code begins here
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}
        LOD 100

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha 

        Pass
        {
            CGPROGRAM
            // Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct appdata members uv2_MainTex2)
            //#pragma exclude_renderers d3d11
            // use "vert" function as the vertex shader
            #pragma vertex vert
            // use "frag" function as the pixel (fragment) shader
            #pragma fragment frag

             #include "UnityCG.cginc"

             // vertex shader inputs
            struct appdata
            {
                float4 vertex : POSITION; // vertex position
                float2 uv_MainTex : TEXCOORD0;
//                float2 uv2_PatternTex : TEXCOORD1;
            };

            // vertex shader outputs ("vertex to fragment")
            struct v2f
            {
                float4 vertex : SV_POSITION; // clip space position
                half2 texCoord : TEXCOORD0; // texture coordinate
//                half2 texCoord1: TEXCOORD1;
            };

            // texture we will sample
            sampler2D _MainTex;
            fixed3 _ForegroundColor;
            fixed3 _BackgroundColor;

            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;

           // vertex shader
            v2f vert (appdata v)
            {
                 v2f o;
                 o.vertex = UnityObjectToClipPos(v.vertex);
                 o.texCoord = TRANSFORM_TEX(v.uv_MainTex, _MainTex);
                 return o;
            }

            // pixel shader; returns low precision ("fixed4" type)
            // color ("SV_Target" semantic)
            fixed4 frag (v2f pix) : SV_Target
            {
                // we are only scaling the pattern
                // sample texture with new scaled coordinates
                fixed4 col = tex2D(_MainTex, pix.texCoord);
                fixed4 newColor = col;
                // newColor.a = 1.0f;
                // newColor.rgb = (col.a == 0.0f) ? _BackgroundColor : _ForegroundColor;

                return newColor;
            }
            ENDCG
        }

    }
}