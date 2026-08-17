using System;

namespace LumaLooks
{
	// Token: 0x02000020 RID: 32
	internal static class PerfMode
	{
		// Token: 0x17000022 RID: 34
		// (get) Token: 0x0600011D RID: 285 RVA: 0x00010153 File Offset: 0x0000E353
		public static float ScanMul
		{
			get
			{
				if (!PerfMode.LowCpu)
				{
					return 1f;
				}
				return 3f;
			}
		}

		// Token: 0x04000295 RID: 661
		public static bool LowCpu;
	}
}
