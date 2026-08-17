using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LumaLooks
{
    // Token: 0x0200003A RID: 58
    internal sealed class MirrorLetterboxPass : ScriptableRenderPass
    {
        // Token: 0x06000217 RID: 535 RVA: 0x0001FC27 File Offset: 0x0001DE27
        public MirrorLetterboxPass()
        {
            base.renderPassEvent = (RenderPassEvent)1000;
        }

        // Token: 0x06000218 RID: 536 RVA: 0x0001FC48 File Offset: 0x0001DE48
        private static float BarPixels(int w, int h, float ratio)
        {
            if (ratio <= 0.01f || h <= 0 || w <= 0)
            {
                return 0f;
            }

            float num = (float)w / ratio;
            return Mathf.Max(0f, ((float)h - num) * 0.5f);
        }

        // Token: 0x06000219 RID: 537 RVA: 0x0001FC84 File Offset: 0x0001DE84
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            try
            {
                UniversalResourceData universalResourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData universalCameraData = frameData.Get<UniversalCameraData>();
                int num = 0;
                int num2 = 0;
                try
                {
                    RenderTargetInfo renderTargetInfo =
                        renderGraph.GetRenderTargetInfo(universalResourceData.activeColorTexture);
                    num = renderTargetInfo.width;
                    num2 = renderTargetInfo.height;
                }
                catch
                {
                }

                if (num <= 0 || num2 <= 0)
                {
                    num = universalCameraData.cameraTargetDescriptor.width;
                    num2 = universalCameraData.cameraTargetDescriptor.height;
                }

                float num3 = MirrorLetterboxPass.BarPixels(num, num2, this.Ratio);
                if (num3 >= 1f)
                {
                    MirrorLetterboxPass.PassData passData = null;
                    using (IRasterRenderGraphBuilder rasterRenderGraphBuilder =
                           renderGraph.AddRasterRenderPass<MirrorLetterboxPass.PassData>("LumaMirrorLetterbox",
                               out passData,
                               "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs",
                               5468))
                    {
                        passData.BarPx = num3;
                        passData.W = num;
                        passData.H = num2;
                        rasterRenderGraphBuilder.SetRenderAttachment(universalResourceData.activeColorTexture, 0,
                            (AccessFlags)2);
                        rasterRenderGraphBuilder.AllowPassCulling(false);
                        rasterRenderGraphBuilder.SetRenderFunc<MirrorLetterboxPass.PassData>(
                            delegate(MirrorLetterboxPass.PassData pd, RasterGraphContext ctx)
                            {
                                ctx.cmd.SetViewport(new Rect(0f, 0f, (float)pd.W, pd.BarPx));
                                ctx.cmd.ClearRenderTarget((RTClearFlags)1, Color.black, 1f, 0U);
                                ctx.cmd.SetViewport(new Rect(0f, (float)pd.H - pd.BarPx, (float)pd.W, pd.BarPx));
                                ctx.cmd.ClearRenderTarget((RTClearFlags)1, Color.black, 1f, 0U);
                                ctx.cmd.SetViewport(new Rect(0f, 0f, (float)pd.W, (float)pd.H));
                            });
                    }
                }
            }
            catch
            {
            }
        }

        // Token: 0x0600021A RID: 538 RVA: 0x0001FDA8 File Offset: 0x0001DFA8
        public unsafe override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            RTHandle cameraColorTargetHandle;
            int num;
            int num2;
            try
            {
                cameraColorTargetHandle = renderingData.cameraData.renderer.cameraColorTargetHandle;

                RenderTexture renderTexture = cameraColorTargetHandle != null
                    ? cameraColorTargetHandle.rt
                    : null;

                if (renderTexture != null)
                {
                    num = renderTexture.width;
                    num2 = renderTexture.height;
                }
                else
                {
                    num = renderingData.cameraData.camera.pixelWidth;
                    num2 = renderingData.cameraData.camera.pixelHeight;
                }

                if (num <= 0 || num2 <= 0)
                {
                    num = renderingData.cameraData.cameraTargetDescriptor.width;
                    num2 = renderingData.cameraData.cameraTargetDescriptor.height;
                }
            }
            catch
            {
                return;
            }

            if (cameraColorTargetHandle == null)
            {
                return;
            }

            float num3 = MirrorLetterboxPass.BarPixels(num, num2, this.Ratio);
            if (num3 < 1f)
            {
                return;
            }

            CommandBuffer commandBuffer = CommandBufferPool.Get("LumaMirrorLetterbox");
            commandBuffer.SetRenderTarget(cameraColorTargetHandle);
            commandBuffer.SetViewport(new Rect(0f, 0f, (float)num, num3));
            commandBuffer.ClearRenderTarget(false, true, Color.black);
            commandBuffer.SetViewport(new Rect(0f, (float)num2 - num3, (float)num, num3));
            commandBuffer.ClearRenderTarget(false, true, Color.black);
            commandBuffer.SetViewport(new Rect(0f, 0f, (float)num, (float)num2));
            context.ExecuteCommandBuffer(commandBuffer);
            CommandBufferPool.Release(commandBuffer);
        }

        // Token: 0x04000498 RID: 1176
        public float Ratio = 2.35f;

        // Token: 0x0200003B RID: 59
        private class PassData
        {
            // Token: 0x04000499 RID: 1177
            public float BarPx;

            // Token: 0x0400049A RID: 1178
            public int W;

            // Token: 0x0400049B RID: 1179
            public int H;
        }
    }
}