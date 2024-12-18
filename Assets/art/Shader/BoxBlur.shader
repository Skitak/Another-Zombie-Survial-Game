Shader "Hidden/BoxBlur"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _Radius;
    
        float4 BlurVertical (Varyings input) : SV_Target
        {
            const float BLUR_SAMPLES = 64;
            const float BLUR_SAMPLES_RANGE = BLUR_SAMPLES / 2;
            
            float3 color = 0;
            float blurPixels = _Radius;
            
            for(float i = -BLUR_SAMPLES_RANGE; i <= BLUR_SAMPLES_RANGE; i++)
            {
                float2 sampleOffset = float2 (0, (blurPixels / _BlitTexture_TexelSize.w) * (i / BLUR_SAMPLES_RANGE));
                color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord + sampleOffset).rgb;
            }
            return float4(color.rgb / (BLUR_SAMPLES + 1), 1);
        }

        float4 BlurHorizontal (Varyings input) : SV_Target
        {
            const float BLUR_SAMPLES = 64;
            const float BLUR_SAMPLES_RANGE = BLUR_SAMPLES / 2;
            
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float3 color = 0;
            float blurPixels = _Radius;
            for(float i = -BLUR_SAMPLES_RANGE; i <= BLUR_SAMPLES_RANGE; i++)
            {
                float2 sampleOffset =
                    float2 ((blurPixels / _BlitTexture_TexelSize.z) * (i / BLUR_SAMPLES_RANGE), 0);
                color += SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord + sampleOffset).rgb;
            }
            return float4(color / (BLUR_SAMPLES + 1), 1);
        }
        float4 Unsharp (Varyings input) : SV_Target
        {
            float4 baseColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);
            float4 blurColor = SAMPLE_TEXTURE2D(_blurTex, sampler_LinearClamp, input.texcoord);
            // return blurColor;

            // float4 originalColor = SAMPLE_TEXTURE2D(_BaseTex, sampler_LinearClamp, input.texcoord);

            float4 difference = baseColor - blurColor;
            difference *= step(_Threshold,length(difference));
            return baseColor + difference * _Factor;
            return difference;
            // return sharpenedColor;
            // return baseColor;
            // return blurColor;
        }
    
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