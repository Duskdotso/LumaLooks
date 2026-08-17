using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200005D RID: 93
	internal sealed class WorldLight
	{
		// Token: 0x17000072 RID: 114
		// (get) Token: 0x06000372 RID: 882 RVA: 0x00034061 File Offset: 0x00032261
		// (set) Token: 0x06000373 RID: 883 RVA: 0x00034068 File Offset: 0x00032268
		public static Light ActiveSun { get; private set; }

		// Token: 0x17000073 RID: 115
		// (get) Token: 0x06000374 RID: 884 RVA: 0x00034070 File Offset: 0x00032270
		// (set) Token: 0x06000375 RID: 885 RVA: 0x00034077 File Offset: 0x00032277
		internal static WorldLight Instance { get; private set; }

		// Token: 0x06000376 RID: 886 RVA: 0x00034080 File Offset: 0x00032280
		internal static void ReapplyGtAmbient()
		{
			WorldLight instance = WorldLight.Instance;
			if (instance == null)
			{
				return;
			}
			try
			{
				instance.MaintainGtSurfaceAmbient();
			}
			catch
			{
			}
		}

		// Token: 0x17000074 RID: 116
		// (get) Token: 0x06000377 RID: 887 RVA: 0x000340B4 File Offset: 0x000322B4
		// (set) Token: 0x06000378 RID: 888 RVA: 0x000340BB File Offset: 0x000322BB
		public static bool SourceIsMoon { get; private set; }

		// Token: 0x17000075 RID: 117
		// (get) Token: 0x06000379 RID: 889 RVA: 0x000340C3 File Offset: 0x000322C3
		// (set) Token: 0x0600037A RID: 890 RVA: 0x000340CA File Offset: 0x000322CA
		public static float DayFactor { get; private set; }

		// Token: 0x17000076 RID: 118
		// (get) Token: 0x0600037B RID: 891 RVA: 0x000340D2 File Offset: 0x000322D2
		// (set) Token: 0x0600037C RID: 892 RVA: 0x000340D9 File Offset: 0x000322D9
		public static float SkyDayFactor { get; private set; }

		// Token: 0x17000077 RID: 119
		// (get) Token: 0x0600037D RID: 893 RVA: 0x000340E1 File Offset: 0x000322E1
		// (set) Token: 0x0600037E RID: 894 RVA: 0x000340E8 File Offset: 0x000322E8
		public static Vector3 ResolvedSunDir { get; private set; } = Vector3.up;

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x0600037F RID: 895 RVA: 0x000340F0 File Offset: 0x000322F0
		// (set) Token: 0x06000380 RID: 896 RVA: 0x000340F7 File Offset: 0x000322F7
		public static Vector3 RayDir { get; private set; } = Vector3.up;

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x06000381 RID: 897 RVA: 0x000340FF File Offset: 0x000322FF
		// (set) Token: 0x06000382 RID: 898 RVA: 0x00034106 File Offset: 0x00032306
		public static float SunElevation { get; private set; } = 1f;

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x06000383 RID: 899 RVA: 0x0003410E File Offset: 0x0003230E
		// (set) Token: 0x06000384 RID: 900 RVA: 0x00034115 File Offset: 0x00032315
		public static bool SunArcActive { get; private set; }

		// Token: 0x1700007B RID: 123
		// (get) Token: 0x06000385 RID: 901 RVA: 0x0003411D File Offset: 0x0003231D
		// (set) Token: 0x06000386 RID: 902 RVA: 0x00034124 File Offset: 0x00032324
		public static float ResolvedHour { get; private set; } = 10f;

		// Token: 0x1700007C RID: 124
		// (get) Token: 0x06000387 RID: 903 RVA: 0x0003412C File Offset: 0x0003232C
		// (set) Token: 0x06000388 RID: 904 RVA: 0x00034133 File Offset: 0x00032333
		public static int ResolvedHourSource { get; private set; } = 0;

		// Token: 0x06000389 RID: 905 RVA: 0x0003413C File Offset: 0x0003233C
		public WorldLight(ManualLogSource log)
		{
			WorldLight.Instance = this;
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x0600038A RID: 906 RVA: 0x00034206 File Offset: 0x00032406
		public void Configure(bool on, float sunMul, float sunWarmth, float ambientMul, float shadow)
		{
			this._wantOn = on;
			this._sunMul = sunMul;
			this._sunWarmth = sunWarmth;
			this._ambientMul = Mathf.Max(0f, ambientMul);
			this._shadow = shadow;
			this._dirty = true;
		}

		// Token: 0x0600038B RID: 907 RVA: 0x00034240 File Offset: 0x00032440
		public void ConfigureSunPosition(bool skyOn, bool vrAllowed, bool desktopAllowed, int mode, float timeOfDay)
		{
			this._sunPosOn = skyOn || mode != 0;
			this._sunPosVrAllowed = vrAllowed;
			this._sunPosDesktopAllowed = desktopAllowed;
			this._sunPosMode = Mathf.Clamp(mode, 0, 2);
			this._timeOfDay = Mathf.Repeat(timeOfDay, 24f);
		}

		// Token: 0x0600038C RID: 908 RVA: 0x0003428D File Offset: 0x0003248D
		public void ConfigureSunPosition(bool skyOn, int mode, float timeOfDay)
		{
			this.ConfigureSunPosition(skyOn, true, true, mode, timeOfDay);
		}

		// Token: 0x0600038D RID: 909 RVA: 0x0003429C File Offset: 0x0003249C
		public void Tick()
		{
			try
			{
				this.EnsureOwnSun();
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WorldLight own-sun skipped: " + ex.Message);
			}
			try
			{
				this.MaintainSunCache();
			}
			catch (Exception ex2)
			{
				this._log.LogWarning("WorldLight sun cache skipped: " + ex2.Message);
			}
			try
			{
				GtClock.Tick(this._log);
			}
			catch (Exception ex3)
			{
				this._log.LogWarning("WorldLight game clock skipped: " + ex3.Message);
			}
			try
			{
				this.ResolveSunDirection();
			}
			catch (Exception ex4)
			{
				this._log.LogWarning("WorldLight sun position skipped: " + ex4.Message);
			}
			try
			{
				this.ApplyArcMoonVerdict();
			}
			catch (Exception ex5)
			{
				this._log.LogWarning("WorldLight moon verdict skipped: " + ex5.Message);
			}
			try
			{
				this.UpdateDayFactor();
			}
			catch (Exception ex6)
			{
				this._log.LogWarning("WorldLight day factor skipped: " + ex6.Message);
			}
			try
			{
				this.MaybeLogSceneDiagnostic();
			}
			catch (Exception ex7)
			{
				this._log.LogWarning("WorldLight diagnostic skipped: " + ex7.Message);
			}
			if (this._applied && this._sun != null && WorldLight.ActiveSun != null && this._sun != WorldLight.ActiveSun)
			{
				try
				{
					this.Restore();
					this._sun = null;
					this._captured = false;
					this._dirty = true;
				}
				catch (Exception ex8)
				{
					this._log.LogWarning("WorldLight retarget skipped: " + ex8.Message);
				}
			}
			try
			{
				this.MaintainGtSurfaceAmbient();
			}
			catch (Exception ex9)
			{
				this._log.LogWarning("WorldLight GT-ambient skipped: " + ex9.Message);
			}
			try
			{
				this.MaintainGameClockDrive();
			}
			catch (Exception ex10)
			{
				this._log.LogWarning("WorldLight game-clock drive skipped: " + ex10.Message);
			}
			if (!this._dirty)
			{
				return;
			}
			this._dirty = false;
			try
			{
				this.Reapply();
			}
			catch (Exception ex11)
			{
				this._log.LogWarning("WorldLight apply skipped: " + ex11.Message);
			}
		}

		// Token: 0x0600038E RID: 910 RVA: 0x00034544 File Offset: 0x00032744
		private void MaintainGameClockDrive()
		{
			if (!this._sunPosOn || (this._sunPosMode != 1 && this._sunPosMode != 2))
			{
				if (this._droveGameClock)
				{
					GtClock.TryReleaseGameTime();
					this._droveGameClock = false;
					this._lastDrivenHour = float.NaN;
					this._log.LogInfo("WorldLight: game clock handed back to GT (Follow Game / off).");
				}
				return;
			}
			float resolvedHour = this._resolvedHour;
			if (!float.IsNaN(this._lastDrivenHour) && Mathf.Abs(resolvedHour - this._lastDrivenHour) < 0.005f)
			{
				return;
			}
			if (GtClock.TrySetGameTime(resolvedHour))
			{
				bool flag = !this._droveGameClock;
				this._droveGameClock = true;
				this._lastDrivenHour = resolvedHour;
				if (flag)
				{
					this._log.LogInfo(string.Format("WorldLight: driving GT's day-night clock (hour {0:0.00}).", resolvedHour));
				}
			}
		}

		// Token: 0x0600038F RID: 911 RVA: 0x00034608 File Offset: 0x00032808
		private void MaintainGtSurfaceAmbient()
		{
			bool flag = this._wantOn && Mathf.Abs(this._ambientMul - 1f) > 0.001f;
			float globalFloat = Shader.GetGlobalFloat(WorldLight.GtBright1);
			float globalFloat2 = Shader.GetGlobalFloat(WorldLight.GtBright2);
			if (!flag)
			{
				if (!float.IsNaN(this._gtWritten1))
				{
					if (Mathf.Approximately(globalFloat, this._gtWritten1))
					{
						Shader.SetGlobalFloat(WorldLight.GtBright1, this._gtBase1);
					}
					if (Mathf.Approximately(globalFloat2, this._gtWritten2))
					{
						Shader.SetGlobalFloat(WorldLight.GtBright2, this._gtBase2);
					}
					this._gtWritten1 = (this._gtWritten2 = float.NaN);
				}
				return;
			}
			if (float.IsNaN(this._gtWritten1) || !Mathf.Approximately(globalFloat, this._gtWritten1))
			{
				this._gtBase1 = globalFloat;
			}
			if (float.IsNaN(this._gtWritten2) || !Mathf.Approximately(globalFloat2, this._gtWritten2))
			{
				this._gtBase2 = globalFloat2;
			}
			float num = Mathf.Clamp(this._gtBase1 * this._ambientMul, 0f, 4f);
			float num2 = Mathf.Clamp(this._gtBase2 * this._ambientMul, 0f, 4f);
			if (!Mathf.Approximately(globalFloat, num))
			{
				Shader.SetGlobalFloat(WorldLight.GtBright1, num);
			}
			if (!Mathf.Approximately(globalFloat2, num2))
			{
				Shader.SetGlobalFloat(WorldLight.GtBright2, num2);
			}
			this._gtWritten1 = num;
			this._gtWritten2 = num2;
		}

		// Token: 0x06000390 RID: 912 RVA: 0x00034764 File Offset: 0x00032964
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._nextForeignProbe = 0f;
			this._foreignDirectional = false;
			this.DestroyOwnSun();
			if (m == LoadSceneMode.Single)
			{
				return;
			}
			this._sun = null;
			this._shaderSun = null;
			WorldLight.ActiveSun = null;
			this._nextSunScanAt = 0f;
			this._captured = false;
			this._applied = false;
			this._dirty = true;
			this._rotatedLight = null;
			this._lightRotApplied = false;
			WorldLight.SunArcActive = false;
			WorldLight.ResolvedSunDir = Vector3.up;
			this._loggedArc = false;
			this._loggedArcMode = -1;
			this._loggedArcHour = -1f;
			this._loggedClockFallback = false;
			this._diagDone = false;
			this._diagAt = -1f;
		}

		// Token: 0x06000391 RID: 913 RVA: 0x00034812 File Offset: 0x00032A12
		public void SetFlareDiagContext(bool flareEnabled, float flareMode)
		{
			this._diagFlareEnabled = flareEnabled;
			this._diagFlareMode = flareMode;
		}

		// Token: 0x06000392 RID: 914 RVA: 0x00034824 File Offset: 0x00032A24
		private void MaybeLogSceneDiagnostic()
		{
			if (this._diagDone)
			{
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (this._diagAt < 0f)
			{
				this._diagAt = realtimeSinceStartup + 4f;
				return;
			}
			if (realtimeSinceStartup < this._diagAt)
			{
				return;
			}
			this._diagDone = true;
			Light activeSun = WorldLight.ActiveSun;
			if (activeSun == null)
			{
				this._log.LogInfo("SCENE DIAG: ActiveSun=NULL (no directional light) isMoon=False DayFactor=0 sunLum=n/a flareGate=closed(no source)");
				return;
			}
			float sunElevation = WorldLight.SunElevation;
			Color color = activeSun.color.linear * activeSun.intensity;
			float num = 0.2126f * color.r + 0.7152f * color.g + 0.0722f * color.b;
			bool flag = this._diagFlareEnabled && (this._diagFlareMode >= 2f || this._diagFlareMode >= 1f == WorldLight.SourceIsMoon);
			this._log.LogInfo(string.Format("SCENE DIAG: ActiveSun='{0}' intensity={1:0.###} elevY={2:0.###} ", activeSun.name, activeSun.intensity, sunElevation) + string.Format("isMoon={0} DayFactor={1:0.###} sunLum={2:0.###} ", WorldLight.SourceIsMoon, WorldLight.DayFactor, num) + string.Format("flare(enabled={0} mode={1:0}) gate={2}", this._diagFlareEnabled, this._diagFlareMode, flag ? "OPEN" : "closed"));
		}

		// Token: 0x06000393 RID: 915 RVA: 0x00034990 File Offset: 0x00032B90
		private void EnsureOwnSun()
		{
			if (this._ownSun == null && Time.unscaledTime >= this._nextForeignProbe)
			{
				this._nextForeignProbe = Time.unscaledTime + 5f;
				this._foreignDirectional = this.SceneHasForeignDirectional();
			}
			if (this._foreignDirectional)
			{
				this.DestroyOwnSun();
				return;
			}
			if (this._ownSun == null)
			{
				this._ownSunGo = new GameObject("LumaLooks Sun");
				this._ownSunGo.hideFlags = (HideFlags)52;
				UnityEngine.Object.DontDestroyOnLoad(this._ownSunGo);
				this._ownSun = this._ownSunGo.AddComponent<Light>();
				this._ownSun.type = (LightType)1;
				this._ownSun.shadows = (LightShadows)2;
				this._ownSun.shadowStrength = 0.75f;
				this._ownSun.shadowResolution = (LightShadowResolution)3;
				this._ownSun.shadowNormalBias = 0.15f;
				this._ownSun.shadowBias = 0.03f;
				this._ownSun.shadowNearPlane = 0.1f;
				if (!this._loggedOwnSun)
				{
					this._loggedOwnSun = true;
					this._log.LogInfo("OWNSUN: this map has no directional light — created 'LumaLooks Sun' and driving it from the time-of-day arc (rays, shadows, ambient and highlights all read it). Removed again on disable/scene change.");
				}
			}
			WorldLight.RayDir = WorldLight.ResolvedSunDir;
			float num = Mathf.Clamp01(WorldLight.RayAngleDeg / 90f);
			if (num > 0.0001f && WorldLight.ResolvedSunDir.sqrMagnitude > 1E-08f)
			{
				WorldLight.RayDir = Vector3.Slerp(WorldLight.ResolvedSunDir.normalized, Vector3.up, num);
			}
			Vector3 rayDir = WorldLight.RayDir;
			if (rayDir.sqrMagnitude > 1E-08f)
			{
				this._ownSunGo.transform.rotation = Quaternion.LookRotation(-rayDir.normalized, Vector3.up);
			}
			bool flag = WorldLight.SunElevation < 0f;
			this._ownSun.color = (flag ? WorldLight.OwnSunNightColor : WorldLight.OwnSunDayColor);
			float num2 = Mathf.Clamp01(Mathf.Abs(WorldLight.SunElevation) * 4f);
			this._ownSun.intensity = (flag ? Mathf.Lerp(0.02f, 0.08f, num2) : Mathf.Lerp(0.08f, 0.22f, num2));
		}

		// Token: 0x06000394 RID: 916 RVA: 0x00034BB0 File Offset: 0x00032DB0
		private bool SceneHasForeignDirectional()
		{
			Light sun = RenderSettings.sun;
			if (sun != null && sun.enabled && sun != this._ownSun)
			{
				return true;
			}
			foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(0))
			{
				if (!(light == null) && !(light == this._ownSun) && light.type == LightType.Directional && light.enabled && light.gameObject.activeInHierarchy)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000395 RID: 917 RVA: 0x00034C38 File Offset: 0x00032E38
		private void DestroyOwnSun()
		{
			if (this._ownSunGo == null)
			{
				this._ownSun = null;
				return;
			}
			if (WorldLight.ActiveSun == this._ownSun)
			{
				WorldLight.ActiveSun = null;
			}
			UnityEngine.Object.Destroy(this._ownSunGo);
			this._ownSunGo = null;
			this._ownSun = null;
		}

		// Token: 0x06000396 RID: 918 RVA: 0x00034C88 File Offset: 0x00032E88
		private void MaintainSunCache()
		{
			Light sun = RenderSettings.sun;
			if (sun != null && sun.enabled && sun.intensity >= 0.05f)
			{
				this._shaderSun = sun;
				WorldLight.ActiveSun = sun;
				WorldLight.SourceIsMoon = false;
				Shader.SetGlobalFloat(ShaderIds.SourceIsMoonGlobal, 0f);
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup >= this._nextSunScanAt)
			{
				this._nextSunScanAt = realtimeSinceStartup + 5f;
				Light light = WorldLight.BrightestDirectional();
				this._shaderSun = ((light != null) ? light : sun);
			}
			WorldLight.ActiveSun = this._shaderSun;
			WorldLight.SourceIsMoon = WorldLight.ActiveSun != null && WorldLight.IsMoonVerdict(WorldLight.ActiveSun);
			Shader.SetGlobalFloat(ShaderIds.SourceIsMoonGlobal, WorldLight.SourceIsMoon ? 1f : 0f);
		}

		// Token: 0x06000397 RID: 919 RVA: 0x00034D58 File Offset: 0x00032F58
		private static bool IsMoonVerdict(Light l)
		{
			if (WorldLight.NameLooksNocturnal(l.name))
			{
				return true;
			}
			if ((-l.transform.forward).y < -0.21f)
			{
				return true;
			}
			if (l.intensity < 0.15f)
			{
				Color color = l.color;
				if (color.b > color.r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000398 RID: 920 RVA: 0x00034DB8 File Offset: 0x00032FB8
		private static bool NameLooksNocturnal(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			if (WorldLight.HasTokenStart(name, "day"))
			{
				return false;
			}
			for (int i = 0; i < WorldLight.NocturnalTokens.Length; i++)
			{
				if (WorldLight.HasTokenStart(name, WorldLight.NocturnalTokens[i]))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000399 RID: 921 RVA: 0x00034E04 File Offset: 0x00033004
		private static bool HasTokenStart(string name, string kw)
		{
			int num = 0;
			int num2;
			while ((num2 = name.IndexOf(kw, num, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				if (WorldLight.IsTokenStart(name, num2))
				{
					return true;
				}
				num = num2 + 1;
			}
			return false;
		}

		// Token: 0x0600039A RID: 922 RVA: 0x00034E34 File Offset: 0x00033034
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x0600039B RID: 923 RVA: 0x00034E70 File Offset: 0x00033070
		private void ResolveSunDirection()
		{
			bool flag = false;
			try
			{
				flag = XRSettings.isDeviceActive;
			}
			catch
			{
			}
			bool flag2 = (flag ? this._sunPosVrAllowed : this._sunPosDesktopAllowed);
			bool flag3 = this._sunPosMode == 0 && GtClock.Available;
			bool flag4 = flag2 && ((this._sunPosOn && (this._sunPosMode == 1 || this._sunPosMode == 2)) || flag3);
			if (this._ownSun != null)
			{
				flag4 = true;
			}
			if (!flag4)
			{
				if (this._sunPosMode == 0 && !GtClock.Available && !this._loggedClockFallback)
				{
					this._loggedClockFallback = true;
					this._log.LogInfo(GtClock.ManagerSeen ? "SUNPOS: FollowGame — game clock not ready yet (manager found, slot table pending); following the scene light until it resolves." : "SUNPOS: FollowGame — this map has no game day/night clock (BetterDayNightManager not present); following the scene light as before.");
				}
				this.RestoreLightRotation();
				WorldLight.SunArcActive = false;
				Light activeSun = WorldLight.ActiveSun;
				WorldLight.ResolvedSunDir = ((activeSun != null) ? (-activeSun.transform.forward) : Vector3.up);
				WorldLight.SunElevation = WorldLight.ResolvedSunDir.y;
				WorldLight.ResolvedHourSource = 0;
				this.MaybeLogArc(false);
				return;
			}
			float num;
			int num2;
			if (this._sunPosMode == 2)
			{
				DateTime now = DateTime.Now;
				num = (float)now.Hour + (float)now.Minute / 60f;
				num2 = 2;
			}
			else if (this._sunPosMode == 0 && GtClock.Available)
			{
				num = GtClock.GameHour;
				num2 = 3;
			}
			else
			{
				num = this._timeOfDay;
				num2 = 1;
			}
			this._resolvedHour = num;
			WorldLight.ResolvedHour = num;
			WorldLight.ResolvedHourSource = num2;
			Vector3 vector = WorldLight.SunDirFromTimeOfDay(num);
			WorldLight.SunElevation = vector.y;
			Vector3 vector2 = ((vector.y < -0.21f) ? (-vector) : vector);
			WorldLight.ResolvedSunDir = vector2;
			this.ApplyLightRotation(vector2);
			WorldLight.SunArcActive = true;
			this.MaybeLogArc(true);
		}

		// Token: 0x0600039C RID: 924 RVA: 0x00035040 File Offset: 0x00033240
		private void ApplyArcMoonVerdict()
		{
			if (!WorldLight.SunArcActive)
			{
				return;
			}
			bool flag = WorldLight.SunElevation < -0.21f;
			if (flag == WorldLight.SourceIsMoon)
			{
				return;
			}
			WorldLight.SourceIsMoon = flag;
			Shader.SetGlobalFloat(ShaderIds.SourceIsMoonGlobal, flag ? 1f : 0f);
		}

		// Token: 0x0600039D RID: 925 RVA: 0x0003508C File Offset: 0x0003328C
		public static Vector3 SunDirFromTimeOfDay(float hours)
		{
			float num = 0.6981317f;
			float num2 = 0.2617994f;
			float num3 = (Mathf.Repeat(hours, 24f) - 12f) * 3.1415927f / 12f;
			float num4 = Mathf.Asin(Mathf.Clamp(Mathf.Sin(num) * Mathf.Sin(num2) + Mathf.Cos(num) * Mathf.Cos(num2) * Mathf.Cos(num3), -1f, 1f));
			float num5 = Mathf.Atan2(Mathf.Sin(num3), Mathf.Cos(num3) * Mathf.Sin(num) - Mathf.Tan(num2) * Mathf.Cos(num));
			float num6 = Mathf.Cos(num4);
			Vector3 vector = new Vector3(-num6 * Mathf.Sin(num5), Mathf.Sin(num4), -num6 * Mathf.Cos(num5));
			if (vector.sqrMagnitude <= 1E-08f)
			{
				return Vector3.up;
			}
			return vector.normalized;
		}

		// Token: 0x0600039E RID: 926 RVA: 0x00035168 File Offset: 0x00033368
		private void ApplyLightRotation(Vector3 dir)
		{
			Light activeSun = WorldLight.ActiveSun;
			if (activeSun == null)
			{
				this.RestoreLightRotation();
				return;
			}
			if (this._rotatedLight != activeSun)
			{
				this.RestoreLightRotation();
				this._rotatedLight = activeSun;
				this._origLightRot = activeSun.transform.rotation;
				this._lightRotApplied = false;
			}
			Vector3 vector = ((Mathf.Abs(dir.y) > 0.999f) ? Vector3.forward : Vector3.up);
			Quaternion quaternion = Quaternion.LookRotation(-dir, vector);
			Transform transform = activeSun.transform;
			if (!this._lightRotApplied || Quaternion.Angle(transform.rotation, quaternion) > 0.01f)
			{
				transform.rotation = quaternion;
				this._lightRotApplied = true;
			}
		}

		// Token: 0x0600039F RID: 927 RVA: 0x00035218 File Offset: 0x00033418
		private void RestoreLightRotation()
		{
			if (this._lightRotApplied && this._rotatedLight != null)
			{
				try
				{
					this._rotatedLight.transform.rotation = this._origLightRot;
				}
				catch
				{
				}
			}
			this._lightRotApplied = false;
			this._rotatedLight = null;
			WorldLight.SunArcActive = false;
		}

		// Token: 0x060003A0 RID: 928 RVA: 0x0003527C File Offset: 0x0003347C
		private void MaybeLogArc(bool tod)
		{
			int num = ((!tod) ? 0 : ((WorldLight.ResolvedHourSource == 3) ? 3 : this._sunPosMode));
			float num2 = (tod ? Mathf.Floor(this._resolvedHour) : (-1f));
			if (this._loggedArc && this._loggedArcMode == num && Mathf.Approximately(this._loggedArcHour, num2))
			{
				return;
			}
			this._loggedArc = true;
			this._loggedArcMode = num;
			this._loggedArcHour = num2;
			Vector3 resolvedSunDir = WorldLight.ResolvedSunDir;
			float num3 = Mathf.Asin(Mathf.Clamp(resolvedSunDir.y, -1f, 1f)) * 57.29578f;
			this._log.LogInfo(string.Concat(new string[]
			{
				"SUNPOS: mode=",
				(!tod) ? "FollowGame" : ((num == 3) ? "FollowGame(game)" : ((this._sunPosMode == 2) ? "RealTime" : "TimeOfDay")),
				" ",
				string.Format("t={0:0.00}h ", this._resolvedHour),
				string.Format("dir=({0:0.###},{1:0.###},{2:0.###}) elev={3:0.#}° ", new object[] { resolvedSunDir.x, resolvedSunDir.y, resolvedSunDir.z, num3 }),
				"(",
				(resolvedSunDir.x > 0f) ? "east" : "west",
				"/",
				(resolvedSunDir.z < 0f) ? "south" : "north",
				") light=",
				(WorldLight.ActiveSun != null) ? WorldLight.ActiveSun.name : "(none)",
				" ",
				string.Format("reoriented={0} isMoon={1}", this._lightRotApplied ? 1 : 0, WorldLight.SourceIsMoon)
			}));
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x00035468 File Offset: 0x00033668
		private void UpdateDayFactor()
		{
			float num = 0f;
			Light activeSun = WorldLight.ActiveSun;
			float num2 = Mathf.Clamp01(Mathf.InverseLerp(-0.21f, 0.3f, WorldLight.SunElevation));
			if (WorldLight.SunArcActive)
			{
				float num3 = ((activeSun != null && activeSun != this._ownSun) ? Mathf.Clamp01(activeSun.intensity) : 1f);
				num = num2 * num3;
			}
			else if (activeSun != null && !WorldLight.SourceIsMoon)
			{
				float num4 = Mathf.Clamp01(activeSun.intensity);
				num = num2 * num4;
			}
			this._dayFactor = Mathf.MoveTowards(this._dayFactor, num, Time.unscaledDeltaTime / 5f);
			WorldLight.DayFactor = this._dayFactor;
			float num5 = ((WorldLight.SunArcActive || (activeSun != null && !WorldLight.SourceIsMoon)) ? num2 : 0f);
			this._skyDayFactor = Mathf.MoveTowards(this._skyDayFactor, num5, Time.unscaledDeltaTime / 5f);
			WorldLight.SkyDayFactor = this._skyDayFactor;
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x00035564 File Offset: 0x00033764
		private static Light BrightestDirectional()
		{
			Light light = null;
			float num = -1f;
			foreach (Light light2 in UnityEngine.Object.FindObjectsByType<Light>(0))
			{
				if (!(light2 == null) && light2.enabled && light2.type == LightType.Directional && light2.intensity > num)
				{
					num = light2.intensity;
					light = light2;
				}
			}
			return light;
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x000355C8 File Offset: 0x000337C8
		private Light FindSun()
		{
			if (this._sun != null)
			{
				return this._sun;
			}
			Light activeSun = WorldLight.ActiveSun;
			if (activeSun != null)
			{
				this._sun = activeSun;
				return this._sun;
			}
			Light sun = RenderSettings.sun;
			if (sun != null && sun.enabled && sun.intensity >= 0.05f)
			{
				this._sun = sun;
				return this._sun;
			}
			Light light = WorldLight.BrightestDirectional();
			this._sun = ((light != null) ? light : sun);
			return this._sun;
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x00035658 File Offset: 0x00033858
		private void Capture(Light sun)
		{
			if (this._captured)
			{
				return;
			}
			this._capturedSunId = sun.GetInstanceID();
			this._origSunIntensity = sun.intensity;
			this._origSunColor = sun.color;
			this._origSunShadowStrength = sun.shadowStrength;
			this._origAmbientIntensity = RenderSettings.ambientIntensity;
			this._captured = true;
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x000356B0 File Offset: 0x000338B0
		private static Color ApplyWarmth(Color c, float warmth)
		{
			float num = Mathf.Clamp(warmth, -1f, 1f) * 0.15f;
			return new Color(Mathf.Clamp01(c.r * (1f + num)), Mathf.Clamp01(c.g * (1f + num * 0.4f)), Mathf.Clamp01(c.b * (1f - num)), c.a);
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x00035720 File Offset: 0x00033920
		public void Reapply()
		{
			Light light = this.FindSun();
			if (light == null)
			{
				return;
			}
			if (this._captured && light.GetInstanceID() != this._capturedSunId)
			{
				this._captured = false;
				this._applied = false;
			}
			this.Capture(light);
			if (this._wantOn)
			{
				light.intensity = this._origSunIntensity * this._sunMul;
				light.color = WorldLight.ApplyWarmth(this._origSunColor, this._sunWarmth);
				light.shadowStrength = Mathf.Clamp01(this._shadow);
				RenderSettings.ambientIntensity = this._origAmbientIntensity * this._ambientMul;
				this._applied = true;
				return;
			}
			if (this._applied)
			{
				this.Restore();
			}
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x000357D4 File Offset: 0x000339D4
		public void Restore()
		{
			if (!this._captured || this._sun == null)
			{
				this._applied = false;
				return;
			}
			this._sun.intensity = this._origSunIntensity;
			this._sun.color = this._origSunColor;
			this._sun.shadowStrength = this._origSunShadowStrength;
			RenderSettings.ambientIntensity = this._origAmbientIntensity;
			this._applied = false;
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x00035844 File Offset: 0x00033A44
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.DestroyOwnSun();
			try
			{
				this.RestoreLightRotation();
			}
			catch
			{
			}
			this.Restore();
			this._shaderSun = null;
			WorldLight.ActiveSun = null;
			WorldLight.SourceIsMoon = false;
			WorldLight.DayFactor = 0f;
			WorldLight.SkyDayFactor = 0f;
			WorldLight.ResolvedSunDir = Vector3.up;
			WorldLight.SunArcActive = false;
			GtClock.Reset();
			try
			{
				Shader.SetGlobalFloat(ShaderIds.SourceIsMoonGlobal, 0f);
			}
			catch
			{
			}
		}

		// Token: 0x04000800 RID: 2048
		private const float DimSunIntensity = 0.05f;

		// Token: 0x04000801 RID: 2049
		private const float SunRescanSeconds = 5f;

		// Token: 0x04000802 RID: 2050
		private const float MoonDimIntensity = 0.15f;

		// Token: 0x04000803 RID: 2051
		private const float DiagDelaySeconds = 4f;

		// Token: 0x04000807 RID: 2055
		public const float TwilightEndElev = -0.21f;

		// Token: 0x04000808 RID: 2056
		public const float FullDayElev = 0.3f;

		// Token: 0x0400080B RID: 2059
		public const int SunPosFollowGame = 0;

		// Token: 0x0400080C RID: 2060
		public const int SunPosTimeOfDay = 1;

		// Token: 0x0400080D RID: 2061
		public const int SunPosRealTime = 2;

		// Token: 0x0400080E RID: 2062
		public const int SunPosGameClock = 3;

		// Token: 0x0400080F RID: 2063
		private const float ArcLatitudeDeg = 40f;

		// Token: 0x04000810 RID: 2064
		private const float ArcDeclinationDeg = 15f;

		// Token: 0x04000813 RID: 2067
		public static float RayAngleDeg;

		// Token: 0x04000818 RID: 2072
		private const float DaySmoothSeconds = 5f;

		// Token: 0x04000819 RID: 2073
		private float _dayFactor;

		// Token: 0x0400081A RID: 2074
		private float _skyDayFactor;

		// Token: 0x0400081B RID: 2075
		private readonly ManualLogSource _log;

		// Token: 0x0400081C RID: 2076
		private Light _sun;

		// Token: 0x0400081D RID: 2077
		private Light _shaderSun;

		// Token: 0x0400081E RID: 2078
		private float _nextSunScanAt;

		// Token: 0x0400081F RID: 2079
		private bool _captured;

		// Token: 0x04000820 RID: 2080
		private int _capturedSunId;

		// Token: 0x04000821 RID: 2081
		private float _origSunIntensity;

		// Token: 0x04000822 RID: 2082
		private Color _origSunColor;

		// Token: 0x04000823 RID: 2083
		private float _origSunShadowStrength;

		// Token: 0x04000824 RID: 2084
		private float _origAmbientIntensity;

		// Token: 0x04000825 RID: 2085
		private bool _applied;

		// Token: 0x04000826 RID: 2086
		private bool _wantOn;

		// Token: 0x04000827 RID: 2087
		private float _sunMul = 1f;

		// Token: 0x04000828 RID: 2088
		private float _sunWarmth;

		// Token: 0x04000829 RID: 2089
		private float _ambientMul = 1f;

		// Token: 0x0400082A RID: 2090
		private float _shadow = 1f;

		// Token: 0x0400082B RID: 2091
		private bool _sunPosOn;

		// Token: 0x0400082C RID: 2092
		private bool _sunPosVrAllowed = true;

		// Token: 0x0400082D RID: 2093
		private bool _sunPosDesktopAllowed = true;

		// Token: 0x0400082E RID: 2094
		private int _sunPosMode;

		// Token: 0x0400082F RID: 2095
		private float _timeOfDay = 10f;

		// Token: 0x04000830 RID: 2096
		private float _resolvedHour = 10f;

		// Token: 0x04000831 RID: 2097
		private Light _rotatedLight;

		// Token: 0x04000832 RID: 2098
		private Quaternion _origLightRot = Quaternion.identity;

		// Token: 0x04000833 RID: 2099
		private bool _lightRotApplied;

		// Token: 0x04000834 RID: 2100
		private bool _loggedArc;

		// Token: 0x04000835 RID: 2101
		private int _loggedArcMode = -1;

		// Token: 0x04000836 RID: 2102
		private float _loggedArcHour = -1f;

		// Token: 0x04000837 RID: 2103
		private bool _loggedClockFallback;

		// Token: 0x04000838 RID: 2104
		private bool _diagDone;

		// Token: 0x04000839 RID: 2105
		private float _diagAt = -1f;

		// Token: 0x0400083A RID: 2106
		private bool _diagFlareEnabled;

		// Token: 0x0400083B RID: 2107
		private float _diagFlareMode = -1f;

		// Token: 0x0400083C RID: 2108
		private bool _dirty;

		// Token: 0x0400083D RID: 2109
		private float _lastDrivenHour = float.NaN;

		// Token: 0x0400083E RID: 2110
		private bool _droveGameClock;

		// Token: 0x0400083F RID: 2111
		private static readonly int GtBright1 = Shader.PropertyToID("_GT_DayCycleBrightnessOption1");

		// Token: 0x04000840 RID: 2112
		private static readonly int GtBright2 = Shader.PropertyToID("_GT_DayCycleBrightnessOption2");

		// Token: 0x04000841 RID: 2113
		private float _gtBase1;

		// Token: 0x04000842 RID: 2114
		private float _gtBase2;

		// Token: 0x04000843 RID: 2115
		private float _gtWritten1 = float.NaN;

		// Token: 0x04000844 RID: 2116
		private float _gtWritten2 = float.NaN;

		// Token: 0x04000845 RID: 2117
		private GameObject _ownSunGo;

		// Token: 0x04000846 RID: 2118
		private float _nextForeignProbe;

		// Token: 0x04000847 RID: 2119
		private bool _foreignDirectional;

		// Token: 0x04000848 RID: 2120
		private Light _ownSun;

		// Token: 0x04000849 RID: 2121
		private bool _loggedOwnSun;

		// Token: 0x0400084A RID: 2122
		private static readonly Color OwnSunDayColor = new Color(1f, 0.957f, 0.878f);

		// Token: 0x0400084B RID: 2123
		private static readonly Color OwnSunNightColor = new Color(0.62f, 0.72f, 1f);

		// Token: 0x0400084C RID: 2124
		private static readonly string[] NocturnalTokens = new string[] { "moon", "luna", "night" };
	}
}
