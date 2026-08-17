using System;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000026 RID: 38
	internal static class QualityTiers
	{
		// Token: 0x0600016C RID: 364 RVA: 0x00015CA6 File Offset: 0x00013EA6
		public static int Clamp(int level)
		{
			return Mathf.Clamp(level, 0, 4);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00015CB0 File Offset: 0x00013EB0
		public static int TierOffset(int level)
		{
			return QualityTiers.TierOffsets[QualityTiers.Clamp(level)];
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00015CBE File Offset: 0x00013EBE
		public static int HalfDiv(int level)
		{
			return QualityTiers.HalfDivs[QualityTiers.Clamp(level)];
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00015CCC File Offset: 0x00013ECC
		public static bool CheapTail(int level)
		{
			return QualityTiers.CheapTails[QualityTiers.Clamp(level)];
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00015CDA File Offset: 0x00013EDA
		public static bool LowCpu(int level)
		{
			return QualityTiers.LowCpus[QualityTiers.Clamp(level)];
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00015CE8 File Offset: 0x00013EE8
		public static int Shift(int qualityIdx, int level)
		{
			return Mathf.Clamp(qualityIdx + QualityTiers.TierOffset(level), 0, 2);
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00015CF9 File Offset: 0x00013EF9
		public static int ShiftInverted(int qualityIdx, int level)
		{
			return Mathf.Clamp(qualityIdx - QualityTiers.TierOffset(level), 0, 2);
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00015D0C File Offset: 0x00013F0C
		public static int Parse(string name, int fallback)
		{
			if (!string.IsNullOrEmpty(name))
			{
				for (int i = 0; i < QualityTiers.Options.Length; i++)
				{
					if (string.Equals(QualityTiers.Options[i], name, StringComparison.OrdinalIgnoreCase))
					{
						return i;
					}
				}
			}
			return QualityTiers.Clamp(fallback);
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00015D4B File Offset: 0x00013F4B
		public static string Name(int level)
		{
			return QualityTiers.Options[QualityTiers.Clamp(level)];
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00015D5C File Offset: 0x00013F5C
		// Note: this type is marked as 'beforefieldinit'.
		static QualityTiers()
		{
			bool[] array = new bool[5];
			array[0] = true;
			array[1] = true;
			QualityTiers.LowCpus = array;
		}

		// Token: 0x04000322 RID: 802
		public const int Lowest = 0;

		// Token: 0x04000323 RID: 803
		public const int Low = 1;

		// Token: 0x04000324 RID: 804
		public const int Balanced = 2;

		// Token: 0x04000325 RID: 805
		public const int High = 3;

		// Token: 0x04000326 RID: 806
		public const int Ultra = 4;

		// Token: 0x04000327 RID: 807
		public const int Count = 5;

		// Token: 0x04000328 RID: 808
		public static readonly string[] Options = new string[] { "Lowest", "Low", "Balanced", "High", "Ultra" };

		// Token: 0x04000329 RID: 809
		public const int DefaultVr = 1;

		// Token: 0x0400032A RID: 810
		public const int DefaultDesktop = 3;

		// Token: 0x0400032B RID: 811
		private static readonly int[] TierOffsets = new int[] { -2, -1, -1, 0, 1 };

		// Token: 0x0400032C RID: 812
		private static readonly int[] HalfDivs = new int[] { 4, 4, 2, 2, 2 };

		// Token: 0x0400032D RID: 813
		private static readonly bool[] CheapTails = new bool[] { true, true, true, false, false };

		// Token: 0x0400032E RID: 814
		private static readonly bool[] LowCpus;
	}
}
