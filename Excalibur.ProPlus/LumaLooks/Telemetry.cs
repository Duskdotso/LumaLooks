using System;
using System.Diagnostics;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000051 RID: 81
	internal static class Telemetry
	{
		// Token: 0x1700006C RID: 108
		// (get) Token: 0x060002F8 RID: 760 RVA: 0x0002A9BA File Offset: 0x00028BBA
		public static float CpuPercent
		{
			get
			{
				return Telemetry._cpuPct;
			}
		}

		// Token: 0x1700006D RID: 109
		// (get) Token: 0x060002F9 RID: 761 RVA: 0x0002A9C1 File Offset: 0x00028BC1
		public static float GpuPercent
		{
			get
			{
				return Telemetry._gpuPct;
			}
		}

		// Token: 0x060002FA RID: 762 RVA: 0x0002A9C8 File Offset: 0x00028BC8
		public static long Begin()
		{
			if (!Telemetry.Active)
			{
				return 0L;
			}
			return Stopwatch.GetTimestamp();
		}

		// Token: 0x060002FB RID: 763 RVA: 0x0002A9DC File Offset: 0x00028BDC
		public static void End(long start)
		{
			if (start == 0L)
			{
				return;
			}
			long num = Stopwatch.GetTimestamp() - start;
			if (num > 0L)
			{
				Telemetry._frameTicks += num;
			}
		}

		// Token: 0x060002FC RID: 764 RVA: 0x0002AA08 File Offset: 0x00028C08
		public static void Tick(float unscaledDeltaTime)
		{
			if (!Telemetry.Active)
			{
				Telemetry._frameTicks = 0L;
				Telemetry.PassesRanThisFrame = false;
				return;
			}
			double num = (double)Telemetry._frameTicks * Telemetry.TicksToMs;
			Telemetry._frameTicks = 0L;
			float num2 = unscaledDeltaTime * 1000f;
			if (num2 > 0.01f)
			{
				float num3 = (float)(num / (double)num2) * 100f;
				if (num3 < 0f)
				{
					num3 = 0f;
				}
				if (num3 > 100f)
				{
					num3 = 100f;
				}
				Telemetry._cpuPct += (num3 - Telemetry._cpuPct) * 0.1f;
			}
			try
			{
				FrameTimingManager.CaptureFrameTimings();
				FrameTiming[] timings = Telemetry._timings;
				if (FrameTimingManager.GetLatestTimings(1U, timings) > 0U)
				{
					double gpuFrameTime = timings[0].gpuFrameTime;
					if (gpuFrameTime > 0.0)
					{
						Telemetry._gpuMs = ((Telemetry._gpuMs > 0.0) ? (Telemetry._gpuMs + (gpuFrameTime - Telemetry._gpuMs) * 0.1) : gpuFrameTime);
						if (!Telemetry.PassesRanThisFrame)
						{
							if (Telemetry._baselineMs < 0.0 || gpuFrameTime < Telemetry._baselineMs)
							{
								Telemetry._baselineMs = gpuFrameTime;
							}
						}
						else if (Telemetry._baselineMs > 0.0 && Telemetry._gpuMs > 0.0)
						{
							float num4 = (float)((Telemetry._gpuMs - Telemetry._baselineMs) / Telemetry._gpuMs) * 100f;
							if (num4 < 0f)
							{
								num4 = 0f;
							}
							if (num4 > 100f)
							{
								num4 = 100f;
							}
							Telemetry._gpuPct = ((Telemetry._gpuPct < 0f) ? num4 : (Telemetry._gpuPct + (num4 - Telemetry._gpuPct) * 0.1f));
						}
					}
				}
			}
			catch
			{
			}
			Telemetry.PassesRanThisFrame = false;
		}

		// Token: 0x040006F9 RID: 1785
		private static readonly double TicksToMs = 1000.0 / (double)Stopwatch.Frequency;

		// Token: 0x040006FA RID: 1786
		private static long _frameTicks;

		// Token: 0x040006FB RID: 1787
		private static float _cpuPct = 0f;

		// Token: 0x040006FC RID: 1788
		private static double _gpuMs;

		// Token: 0x040006FD RID: 1789
		private static double _baselineMs = -1.0;

		// Token: 0x040006FE RID: 1790
		private static float _gpuPct = -1f;

		// Token: 0x040006FF RID: 1791
		public static bool PassesRanThisFrame;

		// Token: 0x04000700 RID: 1792
		public static bool Active;

		// Token: 0x04000701 RID: 1793
		private static readonly FrameTiming[] _timings = new FrameTiming[1];
	}
}
