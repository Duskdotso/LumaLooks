using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LumaLooks
{
	// Token: 0x0200001C RID: 28
	internal sealed class LumaPass : ScriptableRenderPass
	{
		// Token: 0x06000111 RID: 273 RVA: 0x0000E514 File Offset: 0x0000C714
		public LumaPass(RenderEngine engine, Stage stage, bool needsDepth)
		{
			this._engine = engine;
			this._stage = stage;
			base.renderPassEvent = (RenderPassEvent)(501 + stage);
			ScriptableRenderPassInput scriptableRenderPassInput = (ScriptableRenderPassInput)4;
			if (needsDepth)
			{
				scriptableRenderPassInput |= (ScriptableRenderPassInput)1;
			}
			base.ConfigureInput(scriptableRenderPassInput);
			base.requiresIntermediateTexture = true;
		}

		// Token: 0x06000112 RID: 274 RVA: 0x0000E55C File Offset: 0x0000C75C
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
			long num = Telemetry.Begin();
			Telemetry.PassesRanThisFrame = true;
			try
			{
				this._engine.RecordStage(this._stage, this.IsVr, renderGraph, frameData);
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

		// Token: 0x06000113 RID: 275 RVA: 0x0000E5D0 File Offset: 0x0000C7D0
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
			CommandBuffer commandBuffer = CommandBufferPool.Get("LumaLooks");
			try
			{
				this._engine.ExecuteStage(this._stage, this.IsVr, commandBuffer, cameraColorTargetHandle, renderTextureDescriptor);
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

		// Token: 0x040001F0 RID: 496
		private readonly RenderEngine _engine;

		// Token: 0x040001F1 RID: 497
		private readonly Stage _stage;

		// Token: 0x040001F2 RID: 498
		public bool IsVr;
	}
}
