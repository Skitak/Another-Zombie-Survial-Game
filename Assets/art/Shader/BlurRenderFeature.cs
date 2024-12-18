using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine;

public class BlurRenderFeature : ScriptableRendererFeature
{
    class BlurApplyPass : ScriptableRenderPass
    {
        const string m_PassName = "BlurApplyRenderPass";
        private RenderTextureDescriptor blurTextureDescriptor;
        public BlurApplyPass(RenderPassEvent injectionPoint)
        {
            this.renderPassEvent = injectionPoint;
            this.blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);
            // requiresIntermediateTexture = true;
        }

        class PassData { public TextureHandle bluredTex; }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);

            var resourceData = frameData.Get<UniversalResourceData>();
            var blurData = frameData.Get<BlurPass.CustomData>();
            if (resourceData.isActiveTargetBackBuffer)
                return;
            // TextureHandle source = resourceData.activeColorTexture;
            TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, "_destTexture", false);

            builder.UseTexture(blurData.bluredTex);
            passData.bluredTex = blurData.bluredTex;

            builder.AllowPassCulling(false);
            builder.SetRenderAttachment(destination, 0);
            builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            resourceData.cameraColor = destination;
        }

        void ExecutePass(PassData data, RasterGraphContext context)
        {
            Blitter.BlitTexture(context.cmd, data.bluredTex, new Vector4(1, 1, 0, 0), 0, false);
        }
    }

    [Range(0, 50)] public float strength;
    [Range(0, 50)] public int gridSize;
    public BlurKind blurKind = BlurKind.GAUSSIAN;
    public RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingPostProcessing;
    BlurPass blurPass;
    BlurApplyPass blurApplyPass;

    /// <inheritdoc/>
    public override void Create()
    {
        blurPass = new(injectionPoint, gridSize, strength, blurKind);
        blurApplyPass = new(injectionPoint);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (blurPass != null && renderingData.cameraData.cameraType == CameraType.Game)
        {
            renderer.EnqueuePass(blurPass);
            renderer.EnqueuePass(blurApplyPass);
        }
    }
    protected override void Dispose(bool disposing) { }
}
