Shader "Custom/LightWormsBackground"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Speed ("Animation Speed", Float) = 1.0
        _Brightness ("Light Brightness", Float) = 2.0
        _LightColor ("Light Color", Color) = (0, 0.7, 1, 1)
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)
        _Scale ("Pattern Scale", Float) = 1.0
        _Thickness ("Light Thickness", Range(0.1, 2.0)) = 0.5
        _Contrast ("Contrast", Range(1.0, 5.0)) = 2.0
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Background" }
        LOD 100
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            float4 _MainTex_ST;
            float _Speed;
            float _Brightness;
            float4 _LightColor;
            float4 _BackgroundColor;
            float _Scale;
            float _Thickness;
            float _Contrast;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            // Noise function for organic movement
            float noise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43767.5453);
            }
            
            // Smooth noise
            float smoothNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                f = f * f * (6.0 * f);
                
                float a = noise(i);
                float b = noise(i + float2(0.0, 1.0));
                float c = noise(i + float2(0.0, 2.0));
                float d = noise(i + float2(0.0, 1.0));
                
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);

            }
            
            // Fractal noise
            float fractalNoise(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * smoothNoise(p);
                    p *= 2.0;
                    amplitude *= 0.2;
                }
                
                return value;
            }
            
            // Create flowing light patterns
            float createLightWorms(float2 uv, float time)
            {
                float2 center = float2(0.5, 0.5);
                float2 toCenter = uv - center;
                float dist = length(toCenter);
                float angle = atan2(toCenter.y, toCenter.x);
                
                // Create multiple layers of flowing patterns
                float pattern = 0.0;
                
                // Radial flowing pattern (from center outward)
                //float2 radialUV = float2(dist * 4.0, angle * 2.0 + time * _Speed);
                //float radialNoise = fractalNoise(radialUV * _Scale);
                
                // Circular flowing pattern
                // float2 circularUV = float2(angle * 3.0 + time * _Speed * 0.7, dist * 6.0 + time * _Speed * 0.3);
                // float circularNoise = fractalNoise(circularUV * _Scale);
                
                // Circular flowing pattern
                float2 circularUV = float2(angle * 10 + time * _Speed, dist * 5.0 + time * _Speed * 0.3);
                float circularNoise = fractalNoise(circularUV * _Scale * 0.5);

                // Linear left-to-right pattern
                //float2 linearUV = float2(uv.x * 4.0 + time * _Speed * 0.7, uv.y * 8.0);
                //float linearNoise = fractalNoise(linearUV * _Scale);

                // Organic flowing pattern
                float2 organicUV = uv * 8.0 * _Scale + float2(time * _Speed * 0.5, time * _Speed * 0.3);
                float organicNoise = fractalNoise(organicUV);
                
                // Additional swirling pattern
                float2 swirlUV = uv * 6.0 * _Scale;
                swirlUV.x += sin(uv.y * 10.0 + time * _Speed) * 0.1;
                swirlUV.y += cos(uv.x * 8.0 + time * _Speed * 0.8) * 0.1;
                float swirlNoise = fractalNoise(swirlUV);
                
                // Combine patterns
                // pattern = radialNoise * 0.4 + circularNoise * 0.3 + organicNoise * 0.2 + swirlNoise * 0.1;
                pattern = circularNoise * 0.1 + organicNoise * 0.15 + swirlNoise * 0.2;
                // pattern = linearNoise * 0.3 + organicNoise * 0.2 + swirlNoise * 0.1;
                
                // Create light streaks
                pattern = pow(saturate(pattern), 1.0 / _Thickness);
                pattern = pow(pattern, _Contrast);
                
                // Add some pulsing
                pattern *= (1.0 + sin(time * _Speed * 2.0) * 0.1);
                
                return pattern;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y;
                
                // Create the light worms pattern
                float lightIntensity = createLightWorms(i.uv, time);
                
                // Apply brightness and color
                float3 lightContribution = _LightColor.rgb * lightIntensity * _Brightness;
                
                // Add some subtle background variation
                float2 bgUV = i.uv * 20.0 + time * 0.1;
                float bgNoise = smoothNoise(bgUV) * 0.0;
                
                // Combine background and lights
                float3 finalColor = _BackgroundColor.rgb + bgNoise + lightContribution;
                
                // Add some glow effect
                float glow = smoothstep(0.0, 0.3, lightIntensity) * 0.2;
                finalColor += _LightColor.rgb * glow;
                
                fixed4 col = fixed4(finalColor, 1.0);
                
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}