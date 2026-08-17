using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace LumaLooks
{
	// Token: 0x02000015 RID: 21
	internal sealed class MapSense
	{
		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600009C RID: 156 RVA: 0x00009515 File Offset: 0x00007715
		// (set) Token: 0x0600009D RID: 157 RVA: 0x0000951C File Offset: 0x0000771C
		public static bool IsOutdoor { get; private set; } = true;

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600009E RID: 158 RVA: 0x00009524 File Offset: 0x00007724
		// (set) Token: 0x0600009F RID: 159 RVA: 0x0000952B File Offset: 0x0000772B
		public static bool IsBasement { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00009533 File Offset: 0x00007733
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x0000953A File Offset: 0x0000773A
		public static bool HasSky { get; private set; } = true;

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000A2 RID: 162 RVA: 0x00009542 File Offset: 0x00007742
		// (set) Token: 0x060000A3 RID: 163 RVA: 0x00009549 File Offset: 0x00007749
		public static bool IsNightUrban { get; private set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000A4 RID: 164 RVA: 0x00009551 File Offset: 0x00007751
		// (set) Token: 0x060000A5 RID: 165 RVA: 0x00009558 File Offset: 0x00007758
		public static bool IsForest { get; private set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000A6 RID: 166 RVA: 0x00009560 File Offset: 0x00007760
		// (set) Token: 0x060000A7 RID: 167 RVA: 0x00009567 File Offset: 0x00007767
		public static bool SunUp { get; private set; } = true;

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x060000A8 RID: 168 RVA: 0x0000956F File Offset: 0x0000776F
		// (set) Token: 0x060000A9 RID: 169 RVA: 0x00009576 File Offset: 0x00007776
		public static float SunElevationY { get; private set; } = 1f;

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x060000AA RID: 170 RVA: 0x0000957E File Offset: 0x0000777E
		// (set) Token: 0x060000AB RID: 171 RVA: 0x00009585 File Offset: 0x00007785
		public static string ZoneName { get; private set; } = "unknown";

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x060000AC RID: 172 RVA: 0x0000958D File Offset: 0x0000778D
		public static string ZoneClass
		{
			get
			{
				if (!MapSense.IsOutdoor)
				{
					return "INDOOR";
				}
				return "OUTDOOR";
			}
		}

		// Token: 0x060000AD RID: 173 RVA: 0x000095A4 File Offset: 0x000077A4
		public MapSense(ManualLogSource log)
		{
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x060000AE RID: 174 RVA: 0x000095F0 File Offset: 0x000077F0
		public void Configure(bool want)
		{
			this._want = want;
		}

		// Token: 0x060000AF RID: 175 RVA: 0x000095FC File Offset: 0x000077FC
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == LoadSceneMode.Single)
			{
				return;
			}
			this._instanceCache = null;
			this._nextPollAt = 0f;
			MapSense.IsOutdoor = true;
			MapSense.IsBasement = false;
			MapSense.IsNightUrban = false;
			MapSense.HasSky = true;
			MapSense.IsForest = false;
			MapSense.SunUp = true;
			MapSense.SunElevationY = 1f;
			MapSense.ZoneName = "unknown";
			this._loggedZone = null;
		}

		// Token: 0x060000B0 RID: 176 RVA: 0x00009660 File Offset: 0x00007860
		public void Tick()
		{
			try
			{
				if (this._want)
				{
					this.ResolveSunGate();
					float realtimeSinceStartup = Time.realtimeSinceStartup;
					if (realtimeSinceStartup >= this._nextPollAt)
					{
						this._nextPollAt = realtimeSinceStartup + 0.5f;
						this.EnsureReflection();
						this.Resolve();
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("MapSense tick skipped: " + ex.Message);
			}
		}

		// Token: 0x060000B1 RID: 177 RVA: 0x000096D8 File Offset: 0x000078D8
		private void ResolveSunGate()
		{
			float sunElevation = WorldLight.SunElevation;
			MapSense.SunElevationY = sunElevation;
			if (WorldLight.SourceIsMoon)
			{
				MapSense.SunUp = false;
				return;
			}
			MapSense.SunUp = (MapSense.SunUp ? (sunElevation > -0.015f) : (sunElevation > 0.035f));
		}

		// Token: 0x060000B2 RID: 178 RVA: 0x00009720 File Offset: 0x00007920
		private void EnsureReflection()
		{
			if (this._reflectResolved)
			{
				return;
			}
			this._reflectResolved = true;
			try
			{
				this._tZoneMgmt = MapSense.FindType("ZoneManagement");
				this._tGTZone = MapSense.FindType("GTZone");
				if (this._tZoneMgmt != null)
				{
					this._miGetActiveZones = this._tZoneMgmt.GetMethod("GetActiveZones", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
					if (this._miGetActiveZones != null)
					{
						this._getActiveZonesStatic = true;
					}
					else
					{
						this._miGetActiveZones = this._tZoneMgmt.GetMethod("GetActiveZones", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
					}
					this._fiActiveZones = this._tZoneMgmt.GetField("activeZones", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					this._piCurrentZone = this._tZoneMgmt.GetProperty("currentZone", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (FieldInfo fieldInfo in this._tZoneMgmt.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
					{
						if (this._tZoneMgmt.IsAssignableFrom(fieldInfo.FieldType))
						{
							this._singletonMember = fieldInfo;
							break;
						}
					}
					if (this._singletonMember == null)
					{
						foreach (PropertyInfo propertyInfo in this._tZoneMgmt.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
						{
							if (this._tZoneMgmt.IsAssignableFrom(propertyInfo.PropertyType) && propertyInfo.GetIndexParameters().Length == 0)
							{
								this._singletonMember = propertyInfo;
								break;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("MapSense reflection failed: " + ex.Message);
			}
			if (!this._loggedReflection)
			{
				this._loggedReflection = true;
				this._log.LogInfo(string.Concat(new string[]
				{
					string.Format("MapSense reflection: ZoneManagement={0} GTZone={1} ", this._tZoneMgmt != null, this._tGTZone != null),
					"GetActiveZones=",
					(this._miGetActiveZones != null) ? (this._getActiveZonesStatic ? "static" : "instance") : "none",
					" ",
					string.Format("activeZones.field={0} currentZone.prop={1} ", this._fiActiveZones != null, this._piCurrentZone != null),
					"singleton=",
					(this._singletonMember != null) ? this._singletonMember.Name : "none"
				}));
			}
		}

		// Token: 0x060000B3 RID: 179 RVA: 0x000099B4 File Offset: 0x00007BB4
		private void Resolve()
		{
			string text = this.ReadZoneNames();
			if (string.IsNullOrEmpty(text))
			{
				text = this.FallbackScanZoneName();
			}
			bool flag;
			bool flag2;
			bool flag3;
			bool flag4;
			bool flag5;
			if (string.IsNullOrEmpty(text))
			{
				flag = true;
				flag2 = false;
				flag3 = false;
				flag4 = true;
				flag5 = false;
				text = "unknown";
			}
			else
			{
				flag2 = MapSense.NameMatchesAny(text, MapSense.BasementTokens);
				flag = !MapSense.NameMatchesAny(text, MapSense.IndoorTokens);
				bool flag6 = MapSense.NameMatchesAny(text, MapSense.NaturalTokens);
				flag3 = !flag || (MapSense.NameMatchesAny(text, MapSense.UrbanTokens) && !flag6);
				flag4 = !MapSense.NameMatchesAny(text, MapSense.EnclosedTokens);
				flag5 = MapSense.NameMatchesAny(text, MapSense.ForestTokens);
			}
			MapSense.IsOutdoor = flag;
			MapSense.IsBasement = flag2;
			MapSense.IsNightUrban = flag3;
			MapSense.HasSky = flag4;
			MapSense.IsForest = flag5;
			MapSense.ZoneName = text;
			if (!string.Equals(this._loggedZone, text, StringComparison.Ordinal) || this._loggedOutdoor != flag || this._loggedBasement != flag2 || this._loggedNightUrban != flag3 || this._loggedSunUp != MapSense.SunUp)
			{
				this._loggedZone = text;
				this._loggedOutdoor = flag;
				this._loggedBasement = flag2;
				this._loggedNightUrban = flag3;
				this._loggedSunUp = MapSense.SunUp;
				this._log.LogInfo(string.Concat(new string[]
				{
					"MapSense: zone='",
					text,
					"' class=",
					flag ? "OUTDOOR" : "INDOOR",
					" ",
					string.Format("basement={0} nightUrban={1} forest={2} ", flag2, flag3, flag5),
					string.Format("sunUp={0} sunElevY={1:0.###}", MapSense.SunUp, MapSense.SunElevationY)
				}));
			}
		}

		// Token: 0x060000B4 RID: 180 RVA: 0x00009B6C File Offset: 0x00007D6C
		private string ReadZoneNames()
		{
			if (this._tZoneMgmt == null)
			{
				return null;
			}
			string text;
			try
			{
				object obj = null;
				if (this._miGetActiveZones != null)
				{
					object obj2 = (this._getActiveZonesStatic ? null : this.Instance());
					if (this._getActiveZonesStatic || obj2 != null)
					{
						obj = this._miGetActiveZones.Invoke(obj2, null);
					}
				}
				if (obj == null)
				{
					object obj3 = this.Instance();
					if (obj3 != null)
					{
						if (this._fiActiveZones != null)
						{
							obj = this._fiActiveZones.GetValue(obj3);
						}
						if (obj == null && this._piCurrentZone != null)
						{
							obj = this._piCurrentZone.GetValue(obj3);
						}
					}
				}
				text = this.ZonesToString(obj);
			}
			catch (Exception ex)
			{
				if (!this._loggedReflection)
				{
					this._log.LogWarning("MapSense read failed: " + ex.Message);
				}
				text = null;
			}
			return text;
		}

		// Token: 0x060000B5 RID: 181 RVA: 0x00009C50 File Offset: 0x00007E50
		private string ZonesToString(object zones)
		{
			if (zones == null)
			{
				return null;
			}
			this._sb.Length = 0;
			string text = zones as string;
			if (text != null)
			{
				return text;
			}
			IEnumerable enumerable = zones as IEnumerable;
			if (enumerable != null && !(zones is string))
			{
				IEnumerator enumerator = enumerable.GetEnumerator();
				while (enumerator.MoveNext())
				{
					object obj = enumerator.Current;
					if (obj != null)
					{
						if (this._sb.Length > 0)
						{
							this._sb.Append(',');
						}
						this._sb.Append(obj.ToString());
					}
				}
			}
			else
			{
				this._sb.Append(zones.ToString());
			}
			if (this._sb.Length <= 0)
			{
				return null;
			}
			return this._sb.ToString();
		}

		// Token: 0x060000B6 RID: 182 RVA: 0x00009D28 File Offset: 0x00007F28
		private object Instance()
		{
			if (this._instanceCache != null)
			{
				return this._instanceCache;
			}
			try
			{
				FieldInfo fieldInfo = this._singletonMember as FieldInfo;
				if (fieldInfo != null)
				{
					this._instanceCache = fieldInfo.GetValue(null) as UnityEngine.Object;
				}
				else
				{
					PropertyInfo propertyInfo = this._singletonMember as PropertyInfo;
					if (propertyInfo != null)
					{
						this._instanceCache = propertyInfo.GetValue(null) as UnityEngine.Object;
					}
				}
				if (this._instanceCache == null && this._tZoneMgmt != null)
				{
					this._instanceCache = UnityEngine.Object.FindFirstObjectByType(this._tZoneMgmt);
				}
			}
			catch
			{
				this._instanceCache = null;
			}
			return this._instanceCache;
		}

		// Token: 0x060000B7 RID: 183 RVA: 0x00009DE0 File Offset: 0x00007FE0
		private string FallbackScanZoneName()
		{
			try
			{
				for (int i = 0; i < SceneManager.sceneCount; i++)
				{
					Scene sceneAt = SceneManager.GetSceneAt(i);
					if (sceneAt.isLoaded)
					{
						sceneAt.GetRootGameObjects(this._rootsScratch);
						for (int j = 0; j < this._rootsScratch.Count; j++)
						{
							GameObject gameObject = this._rootsScratch[j];
							if (!(gameObject == null) && gameObject.activeInHierarchy)
							{
								string name = gameObject.name;
								if (MapSense.NameMatchesAny(name, MapSense.FallbackIndoorTokens))
								{
									this._rootsScratch.Clear();
									return name;
								}
							}
						}
					}
				}
				this._rootsScratch.Clear();
			}
			catch
			{
			}
			return null;
		}

		// Token: 0x060000B8 RID: 184 RVA: 0x00009E9C File Offset: 0x0000809C
		private static bool NameMatchesAny(string name, string[] keywords)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			foreach (string text in keywords)
			{
				int num = 0;
				int num2;
				while ((num2 = name.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)) >= 0)
				{
					if (MapSense.IsTokenStart(name, num2) && MapSense.IsTokenEnd(name, num2 + text.Length))
					{
						return true;
					}
					num = num2 + 1;
				}
			}
			return false;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00009EF8 File Offset: 0x000080F8
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x060000BA RID: 186 RVA: 0x00009F34 File Offset: 0x00008134
		private static bool IsTokenEnd(string s, int end)
		{
			if (end >= s.Length)
			{
				return true;
			}
			char c = s[end];
			return !char.IsLetter(c) || char.IsUpper(c);
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00009F64 File Offset: 0x00008164
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

		// Token: 0x060000BC RID: 188 RVA: 0x0000A03C File Offset: 0x0000823C
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this._instanceCache = null;
			MapSense.IsOutdoor = true;
			MapSense.IsBasement = false;
			MapSense.IsNightUrban = false;
			MapSense.HasSky = true;
			MapSense.IsForest = false;
			MapSense.SunUp = true;
			MapSense.SunElevationY = 1f;
			MapSense.ZoneName = "unknown";
		}

		// Token: 0x0400010F RID: 271
		private const float PollSeconds = 0.5f;

		// Token: 0x04000110 RID: 272
		private const float SunUpEnterY = 0.035f;

		// Token: 0x04000111 RID: 273
		private const float SunUpExitY = -0.015f;

		// Token: 0x04000112 RID: 274
		private static readonly string[] IndoorTokens = new string[] { "basement", "arcade", "cave", "monkeblock", "mall", "metropolis", "hoverboard", "rotating", "stump", "attic" };

		// Token: 0x04000113 RID: 275
		private static readonly string[] BasementTokens = new string[] { "basement" };

		// Token: 0x04000114 RID: 276
		private static readonly string[] EnclosedTokens = new string[] { "basement", "arcade", "cave", "monkeblock", "rotating", "stump", "attic" };

		// Token: 0x04000115 RID: 277
		private static readonly string[] UrbanTokens = new string[] { "city", "metropolis", "mall" };

		// Token: 0x04000116 RID: 278
		private static readonly string[] NaturalTokens = new string[] { "forest", "jungle", "canyon", "mountain", "beach", "cloud", "sky" };

		// Token: 0x04000117 RID: 279
		private static readonly string[] ForestTokens = new string[] { "forest" };

		// Token: 0x04000118 RID: 280
		private static readonly string[] FallbackIndoorTokens = new string[] { "basement", "arcade", "monkeblock", "metropolis", "hoverboard", "attic" };

		// Token: 0x04000121 RID: 289
		private readonly ManualLogSource _log;

		// Token: 0x04000122 RID: 290
		private readonly List<GameObject> _rootsScratch = new List<GameObject>(64);

		// Token: 0x04000123 RID: 291
		private readonly StringBuilder _sb = new StringBuilder(64);

		// Token: 0x04000124 RID: 292
		private bool _want;

		// Token: 0x04000125 RID: 293
		private float _nextPollAt;

		// Token: 0x04000126 RID: 294
		private string _loggedZone;

		// Token: 0x04000127 RID: 295
		private bool _loggedOutdoor;

		// Token: 0x04000128 RID: 296
		private bool _loggedBasement;

		// Token: 0x04000129 RID: 297
		private bool _loggedNightUrban;

		// Token: 0x0400012A RID: 298
		private bool _loggedSunUp = true;

		// Token: 0x0400012B RID: 299
		private bool _reflectResolved;

		// Token: 0x0400012C RID: 300
		private Type _tZoneMgmt;

		// Token: 0x0400012D RID: 301
		private Type _tGTZone;

		// Token: 0x0400012E RID: 302
		private MethodInfo _miGetActiveZones;

		// Token: 0x0400012F RID: 303
		private bool _getActiveZonesStatic;

		// Token: 0x04000130 RID: 304
		private FieldInfo _fiActiveZones;

		// Token: 0x04000131 RID: 305
		private PropertyInfo _piCurrentZone;

		// Token: 0x04000132 RID: 306
		private MemberInfo _singletonMember;

		// Token: 0x04000133 RID: 307
		private UnityEngine.Object _instanceCache;

		// Token: 0x04000134 RID: 308
		private bool _loggedReflection;
	}
}
