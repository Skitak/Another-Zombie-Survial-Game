Shader "Hidden/GaussianBlur"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #define E 2.71828f
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        int _GridSize;
        float _Spread;

        float gaussian(int x)
        {
            float sigmaSqu = _Spread * _Spread;
            return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
        }

        float4 BlurVertical (Varyings input) : SV_Target
        {
            const int BLUR_SAMPLES_RANGE = (_GridSize - 1) / 2;

            float3 color = 0;
            float gridSum = 0;
            float blurPixels = _GridSize;
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            
            float totalWeight = 0;
            for(int i = -BLUR_SAMPLES_RANGE; i <= BLUR_SAMPLES_RANGE; ++i)
            {
                float gauss = gaussian(i);
                gridSum += gauss;
                float2 sampleOffset = float2 (0, (_BlitTexture_TexelSize.y * i));
                color += gauss * SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord + sampleOffset).rgb;
            }
            color /= gridSum;
            return float4(color.rgb, 1);
        }

        float4 BlurHorizontal (Varyings input) : SV_Target
        {
            const int BLUR_SAMPLES_RANGE = (_GridSize - 1) / 2;

            float3 color = 0;
            float gridSum = 0;
            float blurPixels = _GridSize;
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            
            float totalWeight = 0;
            for(int i = -BLUR_SAMPLES_RANGE; i <= BLUR_SAMPLES_RANGE; ++i)
            {
                float gauss = gaussian(i);
                gridSum += gauss;

                float2 sampleOffset = float2 ((_BlitTexture_TexelSize.x * i), 0);
                color += gauss * SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord + sampleOffset).rgb;
            }
            color /= gridSum;
            return float4(color.rgb, 1);
        }

        // float4 BlurHorizontal (Varyings input) : SV_Target
        // {
        //     const float BLUR_SAMPLES = 64;
        //     const float BLUR_SAMPLES_RANGE = BLUR_SAMPLES / 2;
            
        //     UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
        //     float3 color = 0;
        //     float blurPixels = _GridSize;
        //     for(float i = -BLUR_SAMPLES_RANGE; i <= BLUR_SAMPLES_RANGE; i++)
        //     {
        //         float2 sampleOffset =
        //             float2 ((blurPixels / _BlitTexture_TexelSize.z) * (i / BLUR_SAMPLES_RANGE), 0);
        //         color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord + sampleOffset).rgb;
        //     }
        //     return float4(color / (BLUR_SAMPLES + 1), 1);
        // }

    ENDHLSL
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100
        ZWrite Off Cull Off
        Pass
        {
            Name "BlurPassVertical"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment BlurVertical
            
            ENDHLSL
        }
        
        Pass
        {
            Name "BlurPassHorizontal"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment BlurHorizontal
            
            ENDHLSL
        }
    }
}