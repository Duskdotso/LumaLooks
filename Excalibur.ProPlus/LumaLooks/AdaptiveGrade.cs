using System;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000004 RID: 4
	internal static class AdaptiveGrade
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002067 File Offset: 0x00000267
		// (set) Token: 0x06000004 RID: 4 RVA: 0x0000206E File Offset: 0x0000026E
		public static float Night { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002076 File Offset: 0x00000276
		// (set) Token: 0x06000006 RID: 6 RVA: 0x0000207D File Offset: 0x0000027D
		public static float ExposureOffset { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000007 RID: 7 RVA: 0x00002085 File Offset: 0x00000285
		// (set) Token: 0x06000008 RID: 8 RVA: 0x0000208C File Offset: 0x0000028C
		public static float WarmthOffset { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000009 RID: 9 RVA: 0x00002094 File Offset: 0x00000294
		// (set) Token: 0x0600000A RID: 10 RVA: 0x0000209B File Offset: 0x0000029B
		public static float SaturationOffset { get; private set; }

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000B RID: 11 RVA: 0x000020A3 File Offset: 0x000002A3
		// (set) Token: 0x0600000C RID: 12 RVA: 0x000020AA File Offset: 0x000002AA
		public static float ContrastOffset { get; private set; }

		// Token: 0x0600000D RID: 13 RVA: 0x000020B2 File Offset: 0x000002B2
		public static void Configure(bool on, float strength, float seconds)
		{
			AdaptiveGrade._on = on;
			AdaptiveGrade._strength = Mathf.Clamp01(strength);
			AdaptiveGrade._seconds = Mathf.Max(0f, seconds);
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000020D8 File Offset: 0x000002D8
		public static void Tick(float dt)
		{
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			float num5 = 0f;
			if (AdaptiveGrade._on)
			{
				float num6 = 0f;
				try
				{
					num6 = Mathf.Clamp01(1f - WorldLight.DayFactor);
				}
				catch
				{
				}
				num = num6 * AdaptiveGrade._strength;
				float num7 = 0f;
				try
				{
					num7 = Mathf.Clamp01(RainSensor.RainFactor);
				}
				catch
				{
				}
				num7 *= AdaptiveGrade._strength;
				bool flag = false;
				bool flag2 = false;
				bool flag3 = false;
				try
				{
					flag = MapSense.IsBasement;
					flag2 = MapSense.IsNightUrban;
					flag3 = MapSense.IsForest;
				}
				catch
				{
				}
				num2 = -0.3f * num + -0.1f * num7 + (flag ? (-0.1f * AdaptiveGrade._strength) : 0f);
				num3 = -0.15f * num + -0.1f * num7 + (flag ? (-0.05f * AdaptiveGrade._strength) : 0f) + (flag2 ? (0.05f * AdaptiveGrade._strength) : 0f) + (flag3 ? (0.04f * AdaptiveGrade._strength) : 0f);
				num4 = -0.12f * num + -0.15f * num7 + (flag2 ? (-0.05f * AdaptiveGrade._strength) : 0f) + (flag3 ? (0.03f * AdaptiveGrade._strength) : 0f);
				num5 = -0.05f * num + -0.08f * num7;
			}
			float num8;
			if (!AdaptiveGrade._seeded || AdaptiveGrade._seconds <= 0.001f || dt <= 0f)
			{
				num8 = 1f;
				AdaptiveGrade._seeded = true;
			}
			else
			{
				float num9 = Mathf.Max(AdaptiveGrade._seconds * 0.2f, 0.001f);
				num8 = 1f - Mathf.Exp(-dt / num9);
			}
			AdaptiveGrade.Night = Mathf.Lerp(AdaptiveGrade.Night, num, num8);
			AdaptiveGrade.ExposureOffset = Mathf.Lerp(AdaptiveGrade.ExposureOffset, num2, num8);
			AdaptiveGrade.WarmthOffset = Mathf.Lerp(AdaptiveGrade.WarmthOffset, num3, num8);
			AdaptiveGrade.SaturationOffset = Mathf.Lerp(AdaptiveGrade.SaturationOffset, num4, num8);
			AdaptiveGrade.ContrastOffset = Mathf.Lerp(AdaptiveGrade.ContrastOffset, num5, num8);
		}

		// Token: 0x0600000F RID: 15 RVA: 0x00002320 File Offset: 0x00000520
		public static void ResetSmoothing()
		{
			AdaptiveGrade._seeded = false;
		}

		// Token: 0x04000002 RID: 2
		private static bool _on;

		// Token: 0x04000003 RID: 3
		private static float _strength = 0.7f;

		// Token: 0x04000004 RID: 4
		private static float _seconds = 4f;

		// Token: 0x0400000A RID: 10
		private static bool _seeded;

		// Token: 0x0400000B RID: 11
		private const float NightExposure = -0.3f;

		// Token: 0x0400000C RID: 12
		private const float NightWarmth = -0.15f;

		// Token: 0x0400000D RID: 13
		private const float NightSaturation = -0.12f;

		// Token: 0x0400000E RID: 14
		private const float NightContrast = -0.05f;

		// Token: 0x0400000F RID: 15
		private const float RainExposure = -0.1f;

		// Token: 0x04000010 RID: 16
		private const float RainWarmth = -0.1f;

		// Token: 0x04000011 RID: 17
		private const float RainSaturation = -0.15f;

		// Token: 0x04000012 RID: 18
		private const float RainContrast = -0.08f;

		// Token: 0x04000013 RID: 19
		private const float BasementExposure = -0.1f;

		// Token: 0x04000014 RID: 20
		private const float BasementWarmth = -0.05f;

		// Token: 0x04000015 RID: 21
		private const float UrbanWarmth = 0.05f;

		// Token: 0x04000016 RID: 22
		private const float UrbanSaturation = -0.05f;

		// Token: 0x04000017 RID: 23
		private const float ForestWarmth = 0.04f;

		// Token: 0x04000018 RID: 24
		private const float ForestSaturation = 0.03f;
	}
}
