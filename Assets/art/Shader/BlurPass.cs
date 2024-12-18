using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

public enum BlurKind { BOX, GAUSSIAN }
public class BlurPass : ScriptableRenderPass
{
    const string m_PassName = "BlurRenderPass";
    Material mat;
    float spread;
    int gridSize;
    RenderTextureDescriptor blurTextureDescriptor;
    static readonly int gridSizeId = Shader.PropertyToID("_GridSize");
    static readonly int spreadId = Shader.PropertyToID("_Spread");
    static readonly int blurTexId = Shader.PropertyToID("_BlurTex");
    public BlurPass(RenderPassEvent injectionPoint, int gridSize, float spread, BlurKind kind)
    {
        this.gridSize = gridSize % 2 == 0 ? gridSize + 1 : gridSize;
        this.spread = spread;
        this.renderPassEvent = injectionPoint;
        this.blurTextureDescriptor = new RenderTextureDescriptor(Screen.width, Screen.height, RenderTextureFormat.Default, 0);

        requiresIntermediateTexture = true;

        if (kind == BlurKind.BOX)
            this.mat = new Material(Shader.Find("Hidden/BoxBlur"));
        else if (kind == BlurKind.GAUSSIAN)
            this.mat = new Material(Shader.Find("Hidden/GaussianBlur"));
    }
    class PassData
    {
        internal TextureHandle sourceTex;
        internal TextureHandle tmpTex;
        internal Material mat;
    }
    public class CustomData : ContextItem
    {
        public TextureHandle bluredTex;

        public override void Reset()
        {
            bluredTex = TextureHandle.nullHandle;
        }
    }
    void UpdateMaterial()
    {
        mat?.SetInt(gridSizeId, gridSize);
        mat?.SetFloat(spreadId, spread);
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // using var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData);
        var resourceData = frameData.Get<UniversalResourceData>();

        if (resourceData.isActiveTargetBackBuffer)
            return;

        UpdateMaterial();

        TextureHandle tmp = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, "_blurTextureV", false);
        TextureHandle dest = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blurTextureDescriptor, "_blurTexture", false);
        var source = resourceData.activeColorTexture;

        if (!source.IsValid() || !dest.IsValid())
            return;
        CustomData customData = frameData.Create<CustomData>();
        customData.bluredTex = dest;

        // builder.UseTexture(passData.sourceTex, AccessFlags.Read);
        // builder.SetRenderAttachment(dest, 0, AccessFlags.Write);
        // builder.SetRenderAttachment(dest, 1, AccessFlags.WriteAll);
        // builder.SetGlobalTextureAfterPass(dest, blurTexId);

        // passData.sourceTex = resourceData.activeColorTexture;
        // passData.tmpTex = tmp;
        // passData.mat = mat;

        RenderGraphUtils.BlitMaterialParameters passVerticalBlur = new(source, tmp, mat, 0);
        RenderGraphUtils.BlitMaterialParameters passHorizontalBlur = new(tmp, dest, mat, 1);
        renderGraph.AddBlitPass(passVerticalBlur, passName: $"{m_PassName} blur V");
        renderGraph.AddBlitPass(passHorizontalBlur, passName: $"{m_PassName} blur H");
        // resourceData.cameraColor = dest;
        // builder.AllowPassCulling(false);
        // builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
    }

    static void ExecutePass(PassData data, RasterGraphContext context)
    {
        // context.cmd.SetGlobalTexture(data.)
        // Records a rendering command to copy, or blit, the contents of the source texture
        // to the color render target of the render pass.
        // The RecordRenderGraph method sets the destination texture as the render target
        // with the UseTextureFragment method.
        Blitter.BlitTexture(context.cmd, data.sourceTex, Vector4.one, data.mat, 0);
        // Blitter.BlitTexture(context.cmd, data.tmpTex, Vector4.one, data.mat, 1);
    }
}
