using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine;

public class UnsharpRenderFeature : ScriptableRendererFeature
{
    class UnsharpPass : ScriptableRenderPass
    {
        const string m_PassName = "UnsharpRenderPass";
        private Material mat;
        float threshold, factor;
        private RenderTextureDescriptor blurTextureDescriptor;
        #region shader properties 
        private static readonly int thresholdId = Shader.PropertyToID("_Threshold");
        private static readonly int factorId = Shader.PropertyToID("_Factor");
        private static readonly int blurTexId = Shader.PropertyToID("_blurTex");
        #endregion
        public UnsharpPass(RenderPassEvent injectionPoint, float threshold, float factor)
        {
            this.threshold = threshold;
            this.factor = factor;
            this.mat = new Material(Shader.Find("Hidden/Unsharp"));
            this.renderPassEvent = injectionPoint;
            this.blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
            requiresIntermediateTexture = true;
        }

        class PassData
        {
            public TextureHandle source;
            public TextureHandle bluredTex;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

            var resourceData = frameData.Get<UniversalResourceData>();
            var blurData = frameData.Get<BlurPass.CustomData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;
            TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, "_destTexture", false);

            builder.UseTexture(source);
            builder.UseTexture(blurData.bluredTex);

            passData.source = source;
            passData.bluredTex = blurData.bluredTex;

            builder.AllowPassCulling(false);
            builder.SetRenderAttachment(destination, 0);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            resourceData.cameraColor = destination;
        }

        void ExecutePass(PassData data, RasterGraphContext context)
        {
            mat.SetTexture(blurTexId, data.bluredTex);
            mat.SetFloat(thresholdId, threshold);
            mat.SetFloat(factorId, factor);
            Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0), mat, 0);
        }
    }

    [Range(0, 20)] public float strength = 2;
    [Range(3, 50)] public int pixelRadius = 10;
    [Range(0, 1f)] public float threshold = .1f;
    [Range(0, 4f)] public float factor = 1;
    public BlurKind blurKind = BlurKind.GAUSSIAN;
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    UnsharpPass unsharpPass;
    BlurPass blurPass;

    /// <inheritdoc/>
    public override void Create()
    {
        blurPass = new(injectionPoint, pixelRadius, strength, blurKind);
        unsharpPass = new(injectionPoint, threshold, factor);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (unsharpPass != null && renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(blurPass);
            renderer.EnqueuePass(unsharpPass);
        }
    }
    protected override void Dispose(bool disposing) { }
}
