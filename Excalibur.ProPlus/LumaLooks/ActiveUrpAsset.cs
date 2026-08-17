using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LumaLooks
{
	// Token: 0x0200002C RID: 44
	internal static class ActiveUrpAsset
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x060001A6 RID: 422 RVA: 0x00018270 File Offset: 0x00016470
		public static UniversalRenderPipelineAsset Current
		{
			get
			{
				int qualityLevel = QualitySettings.GetQualityLevel();
				float unscaledTime = Time.unscaledTime;
				if (qualityLevel != ActiveUrpAsset._cachedQuality || unscaledTime >= ActiveUrpAsset._nextRevalidateAt || ActiveUrpAsset._cached == null)
				{
					ActiveUrpAsset._cachedQuality = qualityLevel;
					ActiveUrpAsset._nextRevalidateAt = unscaledTime + 1f;
					ActiveUrpAsset._cached = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
				}
				return ActiveUrpAsset._cached;
			}
		}

		// Token: 0x040003A4 RID: 932
		private const float RevalidateSeconds = 1f;

		// Token: 0x040003A5 RID: 933
		private static UniversalRenderPipelineAsset _cached;

		// Token: 0x040003A6 RID: 934
		private static int _cachedQuality = int.MinValue;

		// Token: 0x040003A7 RID: 935
		private static float _nextRevalidateAt;
	}
}
