// Source 'https://gamedev.stackexchange.com/a/212908'.
Shader "Unlit/ShrinkableRing"
{
    Properties
    {
        _MainTex("Sprite Texture", 2D) = "white" {}
        _Thickness("Thickness", Range(0, 1)) = 0.1
    }
        SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                fixed4 color : Color0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                fixed4 color : Color0;
                float4 vertex : SV_POSITION;
            };

            float _Thickness;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                // Map texture coordinates from (0 to 1) range to (-1 to +1) 
                // with (0,0) in the middle, for ease of measuring radius.
                o.uv = v.uv * 2.0f - 1.0f;

                // Pass through vertex colour (Image.color)
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Use image component's Alpha to control ring radius.
                // 0 = dot with radius equal to the ring thickness.
                // Max = ring just touching outer edge of the image quad.
                float radius = (1 - _Thickness) * i.color.a + _Thickness / 2;

            // Get radius of "this" fragment.
            float r = length(i.uv);
            // Compute its UV-space distance outside the ring's thickness.
            float outside = max(0, abs(r - radius) - _Thickness / 2);

            // Convert to screen pixel distance.
            float pixelSpeed = length(float2(ddx(r), ddy(r)));
            float pixelDist = outside / pixelSpeed;

            fixed4 col = i.color;
            // Make area outside the ring transparent, with slight
            // anti-aliasing on pixels just past the edge.
            col.a = 1 - saturate(pixelDist);
            return col;
        }
        ENDCG
    }
    }
}