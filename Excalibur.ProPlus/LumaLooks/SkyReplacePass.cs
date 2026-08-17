using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LumaLooks
{
    // Token: 0x0200003D RID: 61
    internal sealed class SkyReplacePass : ScriptableRenderPass
    {
        // Token: 0x0600021F RID: 543 RVA: 0x0001FFCA File Offset: 0x0001E1CA
        public SkyReplacePass(RenderEngine engine)
        {
            this._engine = engine;
            base.renderPassEvent = (RenderPassEvent)450;
            base.ConfigureInput((ScriptableRenderPassInput)1);
            base.requiresIntermediateTexture = true;
        }

        // Token: 0x06000220 RID: 544 RVA: 0x0001FFF4 File Offset: 0x0001E1F4
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            long num = Telemetry.Begin();
            Telemetry.PassesRanThisFrame = true;
            try
            {
                this._engine.RecordSkyReplace(renderGraph, frameData, this.IsVr);
                this._engine.NoteSuccess();
            }
            catch (Exception ex)
            {
                this._engine.ReportException(ex);
            }
            finally
            {
                Telemetry.End(num);
            }
        }

        // Token: 0x06000221 RID: 545 RVA: 0x00020060 File Offset: 0x0001E260
        public unsafe override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RTHandle cameraColorTargetHandle;
            RenderTextureDescriptor renderTextureDescriptor;
            try
            {
                cameraColorTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;
                renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            }
            catch (Exception ex)
            {
                this._engine.ReportException(ex);
                return;
            }

            if (cameraColorTargetHandle == null)
            {
                return;
            }

            long num = Telemetry.Begin();
            Telemetry.PassesRanThisFrame = true;
            CommandBuffer commandBuffer = CommandBufferPool.Get("LumaSkyReplace");
            try
            {
                this._engine.ExecuteSkyReplace(this.IsVr, commandBuffer, cameraColorTargetHandle,
                    renderTextureDescriptor);
                this._engine.NoteSuccess();
            }
            catch (Exception ex2)
            {
                this._engine.ReportException(ex2);
            }

            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
            Telemetry.End(num);
        }

        // Token: 0x0400049E RID: 1182
        private readonly RenderEngine _engine;

        // Token: 0x0400049F RID: 1183
        public bool IsVr;
    }
}