using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace LumaLooks
{
	// Token: 0x0200001D RID: 29
	internal sealed class DepthRequestPass : ScriptableRenderPass
	{
		// Token: 0x06000114 RID: 276 RVA: 0x0000E698 File Offset: 0x0000C898
		public DepthRequestPass()
		{
			base.renderPassEvent = (RenderPassEvent)450;
			base.ConfigureInput((ScriptableRenderPassInput)5);
		}

		// Token: 0x06000115 RID: 277 RVA: 0x0000E6B2 File Offset: 0x0000C8B2
		public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
		{
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000E6B2 File Offset: 0x0000C8B2
		public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
		{
		}
	}
}
