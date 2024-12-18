Shader "Hidden/Unsharp"
{
    HLSLINCLUDE
    
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        // The Blit.hlsl file provides the vertex shader (Vert),
        // the input structure (Attributes), and the output structure (Varyings)
        #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

        float _Threshold;
        float _Factor;
        Texture2D _blurTex;
    
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
            Name "UnsharpPass"

            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment Unsharp
            
            ENDHLSL
        }
    }
}