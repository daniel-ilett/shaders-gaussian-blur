using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class BlurRenderPass : ScriptableRenderPass
{
    private Material material;
    private BlurSettings blurSettings;

    private RenderTargetIdentifier source;
    private RenderTargetHandle blurTex;
    private int blurTexID;

    public bool Setup(ScriptableRenderer renderer)
    {
        source = renderer.cameraColorTarget;
        blurSettings = VolumeManager.instance.stack.GetComponent<BlurSettings>();
        renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        if (blurSettings != null && blurSettings.IsActive())
        {
            material = new Material(Shader.Find("PostProcessing/Blur"));
            return true;
        }

        return false;
    }

    public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
    {
        if (blurSettings == null || !blurSettings.IsActive())
        {
            return;
        }

        blurTexID = Shader.PropertyToID("_BlurTex");
        blurTex = new RenderTargetHandle();
        blurTex.id = blurTexID;
        cmd.GetTemporaryRT(blurTex.id, cameraTextureDescriptor);

        base.Configure(cmd, cameraTextureDescriptor);
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (blurSettings == null || !blurSettings.IsActive())
        {
            return;
        }

        CommandBuffer cmd = CommandBufferPool.Get("Blur Post Process");

        // Set Blur effect properties.
        int gridSize = Mathf.CeilToInt(blurSettings.strength.value * 3.0f);

        if(gridSize % 2 == 0)
        {
            gridSize++;
        }

        material.SetInteger("_GridSize", gridSize);
        material.SetFloat("_Spread", blurSettings.strength.value);

        // Execute effect using effect material with two passes.
        cmd.Blit(source, blurTex.id, material, 0);
        cmd.Blit(blurTex.id, source, material, 1);

        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        CommandBufferPool.Release(cmd);
    }

    public override void FrameCleanup(CommandBuffer cmd)
    {
        cmd.ReleaseTemporaryRT(blurTexID);
        base.FrameCleanup(cmd);
    }
}
