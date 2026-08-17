using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LumaLooks
{
	// Token: 0x0200000B RID: 11
	internal static class GtClock
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000042 RID: 66 RVA: 0x000051F9 File Offset: 0x000033F9
		// (set) Token: 0x06000043 RID: 67 RVA: 0x00005200 File Offset: 0x00003400
		public static bool Available { get; private set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000044 RID: 68 RVA: 0x00005208 File Offset: 0x00003408
		// (set) Token: 0x06000045 RID: 69 RVA: 0x0000520F File Offset: 0x0000340F
		public static float GameHour { get; private set; } = 12f;

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000046 RID: 70 RVA: 0x00005217 File Offset: 0x00003417
		// (set) Token: 0x06000047 RID: 71 RVA: 0x0000521E File Offset: 0x0000341E
		public static bool ManagerSeen { get; private set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x06000048 RID: 72 RVA: 0x00005226 File Offset: 0x00003426
		// (set) Token: 0x06000049 RID: 73 RVA: 0x0000522D File Offset: 0x0000342D
		public static int SlotIndex { get; private set; } = -1;

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600004A RID: 74 RVA: 0x00005235 File Offset: 0x00003435
		public static int SlotCount
		{
			get
			{
				return GtClock._slotCount;
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600004B RID: 75 RVA: 0x0000523C File Offset: 0x0000343C
		// (set) Token: 0x0600004C RID: 76 RVA: 0x00005243 File Offset: 0x00003443
		public static float SlotLerp { get; private set; }

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x0600004D RID: 77 RVA: 0x0000524B File Offset: 0x0000344B
		// (set) Token: 0x0600004E RID: 78 RVA: 0x00005252 File Offset: 0x00003452
		public static string[] SlotNames { get; private set; } = new string[0];

		// Token: 0x0600004F RID: 79 RVA: 0x0000525C File Offset: 0x0000345C
		public static void Tick(ManualLogSource log)
		{
			if (!GtClock._resolveAttempted)
			{
				GtClock._resolveAttempted = true;
				try
				{
					GtClock.ResolveMembers();
				}
				catch
				{
					GtClock._reflectionOk = false;
				}
				if (!GtClock._reflectionOk)
				{
					log.LogWarning("GTCLOCK: BetterDayNightManager not resolvable (GT update renamed it?) — Follow Game keeps the old follow-the-scene-light behaviour.");
				}
			}
			if (!GtClock._reflectionOk)
			{
				GtClock.Available = false;
				GtClock.SlotIndex = -1;
				GtClock.ManagerSeen = false;
				return;
			}
			object obj = GtClock._getInstance();
			if (obj as Object == null)
			{
				GtClock.Available = false;
				GtClock.SlotIndex = -1;
				GtClock.ManagerSeen = false;
				GtClock._inst = null;
				return;
			}
			GtClock.ManagerSeen = true;
			if (obj != GtClock._inst)
			{
				GtClock.Available = false;
				if (Time.unscaledTime < GtClock._nextBuildAttempt)
				{
					return;
				}
				GtClock._nextBuildAttempt = Time.unscaledTime + 5f;
				bool flag = false;
				try
				{
					flag = GtClock.BuildTable(obj, log);
				}
				catch
				{
				}
				if (!flag)
				{
					if (GtClock._warnedBuildFor != obj)
					{
						GtClock._warnedBuildFor = obj;
						log.LogWarning("GTCLOCK: manager found but its slot table would not build (names/ranges missing or unrecognisable) — Follow Game falls back to the scene light. Retrying.");
					}
					return;
				}
				GtClock._inst = obj;
			}
			int num = GtClock._getIndex(obj);
			if (num < 0 || num >= GtClock._slotCount)
			{
				GtClock.Available = false;
				GtClock.SlotIndex = -1;
				return;
			}
			float num2 = GtClock._getLerp(obj);
			if (num2 < 0f)
			{
				num2 = 0f;
			}
			else if (num2 > 1f)
			{
				num2 = 1f;
			}
			GtClock.SlotIndex = num;
			GtClock.SlotLerp = num2;
			GtClock.GameHour = Mathf.Repeat(GtClock._slotHour[num] + (GtClock._slotHourNext[num] - GtClock._slotHour[num]) * num2, 24f);
			GtClock.Available = true;
		}

		// Token: 0x06000050 RID: 80 RVA: 0x000053EC File Offset: 0x000035EC
		public static void Reset()
		{
			GtClock.SlotIndex = -1;
			GtClock.SlotLerp = 0f;
			GtClock.Available = false;
			GtClock.ManagerSeen = false;
			GtClock._inst = null;
			GtClock._warnedBuildFor = null;
			GtClock._nextBuildAttempt = 0f;
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000051 RID: 81 RVA: 0x00005420 File Offset: 0x00003620
		public static bool DriveSupported
		{
			get
			{
				return GtClock._reflectionOk && GtClock._fiCurrentTime != null && GtClock._fiSetting != null && GtClock._inst != null && GtClock._slotHour != null && GtClock._slotCount > 0;
			}
		}

		// Token: 0x06000052 RID: 82 RVA: 0x0000545C File Offset: 0x0000365C
		public static bool TrySetGameTime(float hour24)
		{
			if (!GtClock.DriveSupported)
			{
				return false;
			}
			double[] array;
			try
			{
				array = GtClock._piRange.GetValue(GtClock._inst) as double[];
			}
			catch
			{
				return false;
			}
			if (array == null || array.Length < GtClock._slotCount)
			{
				return false;
			}
			float num = Mathf.Repeat(hour24, 24f);
			for (int i = 0; i < GtClock._slotCount; i++)
			{
				float num2 = GtClock._slotHour[i];
				float num3 = GtClock._slotHourNext[i];
				float num4 = num;
				if (num4 < num2)
				{
					num4 += 24f;
				}
				if (num4 >= num2 && num4 < num3)
				{
					double num5 = 0.0;
					for (int j = 0; j < i; j++)
					{
						num5 += array[j];
					}
					double num6 = ((num3 - num2 > 0.0001f) ? ((double)((num4 - num2) / (num3 - num2))) : 0.0);
					double num7 = (num5 + num6 * array[i]) * 3600.0;
					try
					{
						GtClock._fiCurrentTime.SetValue(GtClock._inst, num7);
						GtClock._fiSetting.SetValue(GtClock._inst, Enum.ToObject(GtClock._fiSetting.FieldType, 0));
						return true;
					}
					catch
					{
						return false;
					}
				}
			}
			return false;
		}

		// Token: 0x06000053 RID: 83 RVA: 0x000055B8 File Offset: 0x000037B8
		public static void TryReleaseGameTime()
		{
			if (GtClock._fiSetting == null || GtClock._inst == null)
			{
				return;
			}
			try
			{
				GtClock._fiSetting.SetValue(GtClock._inst, Enum.ToObject(GtClock._fiSetting.FieldType, 1));
			}
			catch
			{
			}
		}

		// Token: 0x06000054 RID: 84 RVA: 0x00005610 File Offset: 0x00003810
		private static void ResolveMembers()
		{
			Type type = GtClock.FindType("BetterDayNightManager");
			if (type == null)
			{
				return;
			}
			FieldInfo field = type.GetField("instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field2 = type.GetField("currentTimeIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			FieldInfo field3 = type.GetField("currentLerp", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			GtClock._fiNames = type.GetField("dayNightLightmapNames", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			GtClock._piRange = type.GetProperty("timeOfDayRange", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			GtClock._fiSeason = type.GetField("currentSeason", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			GtClock._fiCurrentTime = type.GetField("currentTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			GtClock._fiSetting = type.GetField("currentSetting", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null || field2 == null || field3 == null || GtClock._fiNames == null || GtClock._piRange == null)
			{
				return;
			}
			if (field2.FieldType != typeof(int) || field3.FieldType != typeof(float))
			{
				return;
			}
			ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "o");
			UnaryExpression unaryExpression = Expression.Convert(parameterExpression, type);
			GtClock._getIndex = Expression.Lambda<Func<object, int>>(Expression.Field(unaryExpression, field2), new ParameterExpression[] { parameterExpression }).Compile();
			GtClock._getLerp = Expression.Lambda<Func<object, float>>(Expression.Field(unaryExpression, field3), new ParameterExpression[] { parameterExpression }).Compile();
			GtClock._getInstance = Expression.Lambda<Func<object>>(Expression.Convert(Expression.Field(null, field), typeof(object)), Array.Empty<ParameterExpression>()).Compile();
			GtClock._reflectionOk = true;
		}

		// Token: 0x06000055 RID: 85 RVA: 0x000057B0 File Offset: 0x000039B0
		private static bool BuildTable(object inst, ManualLogSource log)
		{
			string[] array = GtClock._fiNames.GetValue(inst) as string[];
			double[] array2 = null;
			try
			{
				array2 = GtClock._piRange.GetValue(inst, null) as double[];
			}
			catch
			{
			}
			if (array == null || array2 == null)
			{
				return false;
			}
			int num = Mathf.Min(array.Length, array2.Length);
			if (num < 1)
			{
				return false;
			}
			float[] array3 = new float[num];
			for (int i = 0; i < num; i++)
			{
				array3[i] = Mathf.Max(0.0001f, (float)array2[i]);
			}
			float[] array4 = new float[num];
			float[] array5 = new float[num];
			for (int j = 0; j < num; j++)
			{
				array4[j] = GtClock.MapNameToHour(array[j]);
				array5[j] = array4[j];
			}
			for (int k = 1; k < num; k++)
			{
				if (!float.IsNaN(array5[k]) && array5[k] == array4[k - 1])
				{
					array5[k] = float.NaN;
				}
			}
			if (num > 1 && !float.IsNaN(array5[0]) && array4[0] == array4[num - 1])
			{
				array5[0] = float.NaN;
			}
			float[] array6 = new float[num];
			bool[] array7 = new bool[num];
			int num2 = -1;
			int num3 = -1;
			int num4 = 0;
			float num5 = 0f;
			for (int l = 0; l < num; l++)
			{
				if (!float.IsNaN(array5[l]))
				{
					float num6 = array5[l];
					if (num4 > 0)
					{
						while (num6 <= num5)
						{
							num6 += 24f;
						}
					}
					else
					{
						num2 = l;
					}
					array6[l] = num6;
					num5 = num6;
					num3 = l;
					num4++;
					array7[l] = true;
				}
			}
			if (num4 == 0)
			{
				return false;
			}
			int num7 = num2;
			for (int m = num2 + 1; m <= num3; m++)
			{
				if (array7[m])
				{
					GtClock.FillGap(array6, array3, num7, m);
					num7 = m;
				}
			}
			float num8;
			for (num8 = array6[num2]; num8 <= array6[num3]; num8 += 24f)
			{
			}
			if (num8 - array6[num2] > 24.001f)
			{
				return false;
			}
			int num9 = (num2 - num3 + num) % num;
			if (num9 == 0)
			{
				num9 = num;
			}
			if (num9 > 1)
			{
				float num10 = 0f;
				for (int n = 0; n < num9; n++)
				{
					num10 += array3[(num3 + n) % num];
				}
				float num11 = 0f;
				for (int num12 = 1; num12 < num9; num12++)
				{
					num11 += array3[(num3 + num12 - 1) % num];
					int num13 = (num3 + num12) % num;
					float num14 = array6[num3] + (num8 - array6[num3]) * (num11 / num10);
					array6[num13] = ((num13 < num2) ? (num14 - 24f) : num14);
				}
			}
			float[] array8 = new float[num];
			for (int num15 = 0; num15 < num - 1; num15++)
			{
				array8[num15] = array6[num15 + 1];
			}
			float num16;
			for (num16 = array6[0] + 24f; num16 <= array6[num - 1]; num16 += 24f)
			{
			}
			array8[num - 1] = num16;
			for (int num17 = 0; num17 < num; num17++)
			{
				if (array8[num17] <= array6[num17])
				{
					return false;
				}
			}
			GtClock._slotHour = array6;
			GtClock._slotHourNext = array8;
			GtClock._slotCount = num;
			GtClock.SlotNames = array;
			string text = "?";
			bool flag = false;
			try
			{
				object obj = ((GtClock._fiSeason != null) ? GtClock._fiSeason.GetValue(inst) : null);
				if (obj != null)
				{
					text = obj.ToString();
					flag = string.Equals(text, "Winter", StringComparison.OrdinalIgnoreCase);
				}
			}
			catch
			{
			}
			StringBuilder stringBuilder = new StringBuilder(224);
			stringBuilder.Append("GTCLOCK: slots=[");
			for (int num18 = 0; num18 < num; num18++)
			{
				if (num18 > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(array[num18]).Append('→').Append(array6[num18].ToString("0.0#"));
				if (!array7[num18])
				{
					stringBuilder.Append('*');
				}
			}
			stringBuilder.Append("] (*=interpolated, hours run past 24 through the night) season=").Append(flag ? "winter" : "summer").Append('(')
				.Append(text)
				.Append(')');
			log.LogInfo(stringBuilder.ToString());
			return true;
		}

		// Token: 0x06000056 RID: 86 RVA: 0x00005BE4 File Offset: 0x00003DE4
		private static void FillGap(float[] start, float[] dur, int i0, int i1)
		{
			if (i1 - i0 < 2)
			{
				return;
			}
			float num = start[i0];
			float num2 = start[i1];
			float num3 = 0f;
			for (int j = i0; j < i1; j++)
			{
				num3 += dur[j];
			}
			float num4 = 0f;
			for (int k = i0 + 1; k < i1; k++)
			{
				num4 += dur[k - 1];
				start[k] = num + (num2 - num) * (num4 / num3);
			}
		}

		// Token: 0x06000057 RID: 87 RVA: 0x00005C4C File Offset: 0x00003E4C
		private static float MapNameToHour(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return float.NaN;
			}
			for (int i = 0; i < name.Length; i++)
			{
				if (name[i] >= '0' && name[i] <= '9')
				{
					int num = 0;
					int num2 = i;
					while (num2 < name.Length && name[num2] >= '0' && name[num2] <= '9')
					{
						num = num * 10 + (int)(name[num2] - '0');
						num2++;
					}
					int num3 = num2;
					while (num3 < name.Length && name[num3] == ' ')
					{
						num3++;
					}
					bool flag = num3 + 1 < name.Length && (name[num3] == 'a' || name[num3] == 'A') && (name[num3 + 1] == 'm' || name[num3 + 1] == 'M');
					bool flag2 = num3 + 1 < name.Length && (name[num3] == 'p' || name[num3] == 'P') && (name[num3 + 1] == 'm' || name[num3 + 1] == 'M');
					if ((flag || flag2) && num >= 1 && num <= 12)
					{
						return (float)((num == 12) ? 0 : num) + (flag2 ? 12f : 0f);
					}
					i = num2 - 1;
				}
			}
			if (GtClock.Has(name, "sunrise"))
			{
				return 6.5f;
			}
			if (GtClock.Has(name, "afternoon"))
			{
				return 15f;
			}
			if (GtClock.Has(name, "morning"))
			{
				return 9f;
			}
			if (GtClock.Has(name, "noon"))
			{
				return 12f;
			}
			if (GtClock.Has(name, "sunset"))
			{
				return 19f;
			}
			if (GtClock.Has(name, "evening"))
			{
				return 20f;
			}
			if (GtClock.Has(name, "night"))
			{
				return 23f;
			}
			return float.NaN;
		}

		// Token: 0x06000058 RID: 88 RVA: 0x00005E2B File Offset: 0x0000402B
		private static bool Has(string s, string kw)
		{
			return s.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x06000059 RID: 89 RVA: 0x00005E3C File Offset: 0x0000403C
		private static Type FindType(string name)
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				try
				{
					Type type = assemblies[i].GetType(name, false);
					if (type != null)
					{
						return type;
					}
				}
				catch
				{
				}
			}
			int j = 0;
			while (j < assemblies.Length)
			{
				Type[] array;
				try
				{
					array = assemblies[j].GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					array = ex.Types;
				}
				catch
				{
					goto IL_0092;
				}
				goto IL_0056;
				IL_0092:
				j++;
				continue;
				IL_0056:
				if (array != null)
				{
					for (int k = 0; k < array.Length; k++)
					{
						if (array[k] != null && array[k].Name == name)
						{
							return array[k];
						}
					}
					goto IL_0092;
				}
				goto IL_0092;
			}
			return null;
		}

		// Token: 0x0400008C RID: 140
		private const float BuildRetrySeconds = 5f;

		// Token: 0x0400008D RID: 141
		private static bool _resolveAttempted;

		// Token: 0x0400008E RID: 142
		private static bool _reflectionOk;

		// Token: 0x0400008F RID: 143
		private static Func<object> _getInstance;

		// Token: 0x04000090 RID: 144
		private static Func<object, int> _getIndex;

		// Token: 0x04000091 RID: 145
		private static Func<object, float> _getLerp;

		// Token: 0x04000092 RID: 146
		private static FieldInfo _fiNames;

		// Token: 0x04000093 RID: 147
		private static PropertyInfo _piRange;

		// Token: 0x04000094 RID: 148
		private static FieldInfo _fiSeason;

		// Token: 0x04000095 RID: 149
		private static object _inst;

		// Token: 0x04000096 RID: 150
		private static float[] _slotHour;

		// Token: 0x04000097 RID: 151
		private static float[] _slotHourNext;

		// Token: 0x04000098 RID: 152
		private static int _slotCount;

		// Token: 0x04000099 RID: 153
		private static float _nextBuildAttempt;

		// Token: 0x0400009A RID: 154
		private static object _warnedBuildFor;

		// Token: 0x0400009B RID: 155
		private static FieldInfo _fiCurrentTime;

		// Token: 0x0400009C RID: 156
		private static FieldInfo _fiSetting;
	}
}
