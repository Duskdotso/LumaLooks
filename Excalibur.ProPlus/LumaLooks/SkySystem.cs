using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200004B RID: 75
	internal sealed class SkySystem
	{
		// Token: 0x0600028A RID: 650 RVA: 0x000272E8 File Offset: 0x000254E8
		private static Vector4 StormGrey()
		{
			Color linear = new Color(0.54901963f, 0.54901963f, 0.5764706f).linear;
			return new Vector4(linear.r, linear.g, linear.b, 0f);
		}

		// Token: 0x17000049 RID: 73
		// (get) Token: 0x0600028B RID: 651 RVA: 0x0002732E File Offset: 0x0002552E
		// (set) Token: 0x0600028C RID: 652 RVA: 0x00027335 File Offset: 0x00025535
		public static bool ScreenSpaceOn { get; private set; }

		// Token: 0x1700004A RID: 74
		// (get) Token: 0x0600028D RID: 653 RVA: 0x0002733D File Offset: 0x0002553D
		// (set) Token: 0x0600028E RID: 654 RVA: 0x00027344 File Offset: 0x00025544
		public static bool NightPassOn { get; private set; }

		// Token: 0x1700004B RID: 75
		// (get) Token: 0x0600028F RID: 655 RVA: 0x0002734C File Offset: 0x0002554C
		// (set) Token: 0x06000290 RID: 656 RVA: 0x00027353 File Offset: 0x00025553
		public static bool SunPassOn { get; private set; }

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000291 RID: 657 RVA: 0x0002735B File Offset: 0x0002555B
		// (set) Token: 0x06000292 RID: 658 RVA: 0x00027362 File Offset: 0x00025562
		public static bool CloudPassOn { get; private set; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000293 RID: 659 RVA: 0x0002736A File Offset: 0x0002556A
		// (set) Token: 0x06000294 RID: 660 RVA: 0x00027371 File Offset: 0x00025571
		public static int Mode { get; private set; } = 0;

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000295 RID: 661 RVA: 0x00027379 File Offset: 0x00025579
		// (set) Token: 0x06000296 RID: 662 RVA: 0x00027380 File Offset: 0x00025580
		public static float ReplaceStrength { get; private set; } = 1f;

		// Token: 0x1700004F RID: 79
		// (get) Token: 0x06000297 RID: 663 RVA: 0x00027388 File Offset: 0x00025588
		// (set) Token: 0x06000298 RID: 664 RVA: 0x0002738F File Offset: 0x0002558F
		public static float BackdropDistance { get; private set; } = 300f;

		// Token: 0x17000050 RID: 80
		// (get) Token: 0x06000299 RID: 665 RVA: 0x00027397 File Offset: 0x00025597
		// (set) Token: 0x0600029A RID: 666 RVA: 0x0002739E File Offset: 0x0002559E
		public static Vector4 UniSunDir { get; private set; } = new Vector4(0f, 1f, 0f, 0f);

		// Token: 0x17000051 RID: 81
		// (get) Token: 0x0600029B RID: 667 RVA: 0x000273A6 File Offset: 0x000255A6
		// (set) Token: 0x0600029C RID: 668 RVA: 0x000273AD File Offset: 0x000255AD
		public static Vector4 UniParams { get; private set; } = new Vector4(1.2f, 0.5f, 1f, 0.5f);

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x0600029D RID: 669 RVA: 0x000273B5 File Offset: 0x000255B5
		// (set) Token: 0x0600029E RID: 670 RVA: 0x000273BC File Offset: 0x000255BC
		public static Vector4 UniParams2 { get; private set; } = new Vector4(0.6f, 1f, 0.5f, 0.35f);

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x0600029F RID: 671 RVA: 0x000273C4 File Offset: 0x000255C4
		// (set) Token: 0x060002A0 RID: 672 RVA: 0x000273CB File Offset: 0x000255CB
		public static Vector4 UniParams3 { get; private set; } = new Vector4(4f, 0.5f, 0f, 0f);

		// Token: 0x17000054 RID: 84
		// (get) Token: 0x060002A1 RID: 673 RVA: 0x000273D3 File Offset: 0x000255D3
		// (set) Token: 0x060002A2 RID: 674 RVA: 0x000273DA File Offset: 0x000255DA
		public static float RaysSunBrightness { get; private set; } = 4f;

		// Token: 0x17000055 RID: 85
		// (get) Token: 0x060002A3 RID: 675 RVA: 0x000273E2 File Offset: 0x000255E2
		// (set) Token: 0x060002A4 RID: 676 RVA: 0x000273E9 File Offset: 0x000255E9
		public static float RaysBodyBrightness { get; private set; } = 4f;

		// Token: 0x17000056 RID: 86
		// (get) Token: 0x060002A5 RID: 677 RVA: 0x000273F1 File Offset: 0x000255F1
		// (set) Token: 0x060002A6 RID: 678 RVA: 0x000273F8 File Offset: 0x000255F8
		public static float RaysBodySize { get; private set; } = 1.2f;

		// Token: 0x17000057 RID: 87
		// (get) Token: 0x060002A7 RID: 679 RVA: 0x00027400 File Offset: 0x00025600
		// (set) Token: 0x060002A8 RID: 680 RVA: 0x00027407 File Offset: 0x00025607
		public static Vector4 UniBodyParams { get; private set; } = new Vector4(1.5f, 0.9f, 0.5f, 1f);

		// Token: 0x17000058 RID: 88
		// (get) Token: 0x060002A9 RID: 681 RVA: 0x0002740F File Offset: 0x0002560F
		// (set) Token: 0x060002AA RID: 682 RVA: 0x00027416 File Offset: 0x00025616
		public static Vector4 UniSunTint { get; private set; } = new Vector4(1f, 1f, 1f, 0f);

		// Token: 0x17000059 RID: 89
		// (get) Token: 0x060002AB RID: 683 RVA: 0x0002741E File Offset: 0x0002561E
		// (set) Token: 0x060002AC RID: 684 RVA: 0x00027425 File Offset: 0x00025625
		public static Vector4 UniMoonTint { get; private set; } = new Vector4(1f, 1f, 1f, 0f);

		// Token: 0x1700005A RID: 90
		// (get) Token: 0x060002AD RID: 685 RVA: 0x0002742D File Offset: 0x0002562D
		// (set) Token: 0x060002AE RID: 686 RVA: 0x00027434 File Offset: 0x00025634
		public static Vector4 UniCloudParams { get; private set; } = new Vector4(0.4f, 0.5f, 0.3f, 0.5f);

		// Token: 0x1700005B RID: 91
		// (get) Token: 0x060002AF RID: 687 RVA: 0x0002743C File Offset: 0x0002563C
		// (set) Token: 0x060002B0 RID: 688 RVA: 0x00027443 File Offset: 0x00025643
		public static Vector4 UniCloudParams2 { get; private set; } = new Vector4(0.5f, 0.5f, 0f, 1f);

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060002B1 RID: 689 RVA: 0x0002744B File Offset: 0x0002564B
		// (set) Token: 0x060002B2 RID: 690 RVA: 0x00027452 File Offset: 0x00025652
		public static Vector4 UniCloudParams3 { get; private set; } = new Vector4(24f, 0.5f, 0f, 0f);

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060002B3 RID: 691 RVA: 0x0002745A File Offset: 0x0002565A
		// (set) Token: 0x060002B4 RID: 692 RVA: 0x00027461 File Offset: 0x00025661
		public static Vector4 UniCloudTint { get; private set; } = new Vector4(1f, 1f, 1f, 0f);

		// Token: 0x1700005E RID: 94
		// (get) Token: 0x060002B5 RID: 693 RVA: 0x00027469 File Offset: 0x00025669
		// (set) Token: 0x060002B6 RID: 694 RVA: 0x00027470 File Offset: 0x00025670
		public static Vector4 UniSkyDayZenith { get; private set; } = new Vector4(0.0295f, 0.2232f, 0.7305f, 1f);

		// Token: 0x1700005F RID: 95
		// (get) Token: 0x060002B7 RID: 695 RVA: 0x00027478 File Offset: 0x00025678
		// (set) Token: 0x060002B8 RID: 696 RVA: 0x0002747F File Offset: 0x0002567F
		public static Vector4 UniSkyDayHorizon { get; private set; } = new Vector4(0.3231f, 0.5776f, 0.8469f, 1f);

		// Token: 0x17000060 RID: 96
		// (get) Token: 0x060002B9 RID: 697 RVA: 0x00027487 File Offset: 0x00025687
		// (set) Token: 0x060002BA RID: 698 RVA: 0x0002748E File Offset: 0x0002568E
		public static float UniSkyDaySat { get; private set; } = 1f;

		// Token: 0x17000061 RID: 97
		// (get) Token: 0x060002BB RID: 699 RVA: 0x00027496 File Offset: 0x00025696
		// (set) Token: 0x060002BC RID: 700 RVA: 0x0002749D File Offset: 0x0002569D
		public static float UniSkyDayHue { get; private set; } = 0.5f;

		// Token: 0x17000062 RID: 98
		// (get) Token: 0x060002BD RID: 701 RVA: 0x000274A5 File Offset: 0x000256A5
		// (set) Token: 0x060002BE RID: 702 RVA: 0x000274AC File Offset: 0x000256AC
		public static Vector4 UniAuroraA { get; private set; } = new Vector4(0.05f, 0.87f, 0.26f, 1f);

		// Token: 0x17000063 RID: 99
		// (get) Token: 0x060002BF RID: 703 RVA: 0x000274B4 File Offset: 0x000256B4
		// (set) Token: 0x060002C0 RID: 704 RVA: 0x000274BB File Offset: 0x000256BB
		public static Vector4 UniAuroraB { get; private set; } = new Vector4(0.05f, 0.33f, 0.87f, 1f);

		// Token: 0x17000064 RID: 100
		// (get) Token: 0x060002C1 RID: 705 RVA: 0x000274C3 File Offset: 0x000256C3
		// (set) Token: 0x060002C2 RID: 706 RVA: 0x000274CA File Offset: 0x000256CA
		public static Vector4 UniReplaceParams2 { get; private set; }

		// Token: 0x17000065 RID: 101
		// (get) Token: 0x060002C3 RID: 707 RVA: 0x000274D2 File Offset: 0x000256D2
		// (set) Token: 0x060002C4 RID: 708 RVA: 0x000274D9 File Offset: 0x000256D9
		public static float DomeDistance { get; private set; }

		// Token: 0x17000066 RID: 102
		// (get) Token: 0x060002C5 RID: 709 RVA: 0x000274E1 File Offset: 0x000256E1
		// (set) Token: 0x060002C6 RID: 710 RVA: 0x000274E8 File Offset: 0x000256E8
		public static bool DomeValid { get; private set; }

		// Token: 0x17000067 RID: 103
		// (get) Token: 0x060002C7 RID: 711 RVA: 0x000274F0 File Offset: 0x000256F0
		// (set) Token: 0x060002C8 RID: 712 RVA: 0x000274F7 File Offset: 0x000256F7
		public static float BackdropEstimate { get; private set; }

		// Token: 0x17000068 RID: 104
		// (get) Token: 0x060002C9 RID: 713 RVA: 0x000274FF File Offset: 0x000256FF
		internal SkyDome Dome
		{
			get
			{
				return this._dome;
			}
		}

		// Token: 0x17000069 RID: 105
		// (get) Token: 0x060002CA RID: 714 RVA: 0x00027507 File Offset: 0x00025707
		public static string ModeName
		{
			get
			{
				if (SkySystem.Mode == 1)
				{
					return "ScreenSpace";
				}
				if (SkySystem.Mode != 2)
				{
					return "Auto";
				}
				return "Skybox";
			}
		}

		// Token: 0x1700006A RID: 106
		// (get) Token: 0x060002CB RID: 715 RVA: 0x0002752A File Offset: 0x0002572A
		private string SunPosName
		{
			get
			{
				if (this._sunPosition == 2)
				{
					return "RealTime";
				}
				if (this._sunPosition != 1)
				{
					return "FollowGame";
				}
				return "TimeOfDay";
			}
		}

		// Token: 0x1700006B RID: 107
		// (get) Token: 0x060002CC RID: 716 RVA: 0x00027550 File Offset: 0x00025750
		private string HourText
		{
			get
			{
				if (WorldLight.ResolvedHourSource != 0)
				{
					return WorldLight.ResolvedHour.ToString("0.00") + "h(" + ((WorldLight.ResolvedHourSource == 2) ? "clock" : ((WorldLight.ResolvedHourSource == 3) ? "game" : "slider")) + ")";
				}
				return "n/a(light)";
			}
		}

		// Token: 0x060002CD RID: 717 RVA: 0x000275B0 File Offset: 0x000257B0
		public SkySystem(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			this._dome = new SkyDome(log);
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x060002CE RID: 718 RVA: 0x00027840 File Offset: 0x00025A40
		public void Configure(bool sunMoonOn, bool sunMoonVr, bool sunMoonDesktop, float sunBrightness, float sunSize, Color sunTint, float moonBrightness, float moonSize, Color moonTint, float glowFalloff, int sunPosition, float timeOfDay, bool cloudsOn, bool cloudsVr, bool cloudsDesktop, bool vrPerfBalanced, float cloudCoverage, float cloudDensity, float cloudSpeed, float cloudHeight, float cloudThickness, float cloudSoftness, Color cloudTint, float cloudSunGlow, bool nightSkyOn, bool nightSkyVr, bool nightSkyDesktop, int method, int replaceWhen, float strength, float backdropDistance, float horizonWarmth, float starDensity, float starBrightness, float starSize, int auroraMode, float auroraIntensity, float auroraSpeed, Color dayZenith, Color dayHorizon, float daySat, float dayHue, Color auroraA, Color auroraB, bool lensFlareOn, bool lensFlareVr, bool lensFlareDesktop)
		{
			this._wantSun = sunMoonOn;
			this._sunVr = sunMoonVr;
			this._sunDesktop = sunMoonDesktop;
			this._sunBrightness = Mathf.Clamp(sunBrightness, 0f, 8f);
			this._sunSize = Mathf.Clamp(sunSize, 0f, 4f);
			this._sunTint = sunTint;
			this._moonBrightness = Mathf.Clamp(moonBrightness, 0f, 8f);
			this._moonSize = Mathf.Clamp(moonSize, 0f, 4f);
			this._moonTint = moonTint;
			this._glowFalloff = Mathf.Clamp01(glowFalloff);
			this._sunPosition = Mathf.Clamp(sunPosition, 0, 2);
			this._timeOfDay = Mathf.Repeat(timeOfDay, 24f);
			this._wantClouds = cloudsOn;
			this._cloudVr = cloudsVr;
			this._cloudDesktop = cloudsDesktop;
			this._vrPerfBalanced = vrPerfBalanced;
			this._cloudCoverage = Mathf.Clamp01(cloudCoverage);
			this._cloudDensity = Mathf.Clamp01(cloudDensity);
			this._cloudSpeed = Mathf.Clamp01(cloudSpeed);
			this._cloudHeight = Mathf.Clamp01(cloudHeight);
			this._cloudThickness = Mathf.Clamp01(cloudThickness);
			this._cloudSoftness = Mathf.Clamp01(cloudSoftness);
			this._cloudTint = cloudTint;
			this._cloudSunGlow = Mathf.Clamp01(cloudSunGlow);
			this._wantNight = nightSkyOn;
			this._nightVr = nightSkyVr;
			this._nightDesktop = nightSkyDesktop;
			this._mode = Mathf.Clamp(method, 0, 2);
			this._replaceAlways = replaceWhen == 0;
			this._strength = Mathf.Clamp01(strength);
			this._backdrop = Mathf.Clamp(backdropDistance, 0f, 20000f);
			this._horizonWarmth = Mathf.Clamp01(horizonWarmth);
			this._starDensity = Mathf.Clamp01(starDensity);
			this._starBrightness = Mathf.Clamp01(starBrightness);
			this._starSize = Mathf.Clamp01(starSize);
			this._auroraMode = Mathf.Clamp(auroraMode, 0, 2);
			this._auroraIntensity = Mathf.Clamp01(auroraIntensity);
			this._auroraSpeed = Mathf.Clamp01(auroraSpeed);
			this._dayZenith = dayZenith;
			this._dayHorizon = dayHorizon;
			this._daySat = Mathf.Clamp(daySat, 0f, 2f);
			this._dayHue = Mathf.Clamp01(dayHue);
			this._auroraA = auroraA;
			this._auroraB = auroraB;
			this._wantFlare = lensFlareOn;
			this._flareVr = lensFlareVr;
			this._flareDesktop = lensFlareDesktop;
			this.ResolveAuthoredUniforms();
			this._paramsDirty = true;
			this._matStaticDirty = true;
		}

		// Token: 0x060002CF RID: 719 RVA: 0x00027AA2 File Offset: 0x00025CA2
		public void NotifyEngineDisabled()
		{
			this._engineDisabled = true;
		}

		// Token: 0x060002D0 RID: 720 RVA: 0x00027AAC File Offset: 0x00025CAC
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == (LoadSceneMode)1)
			{
				return;
			}
			this._applied = false;
			this._origSkybox = null;
			this._envDay = -1f;
			this._driftReApplied = 0;
			this._loggedSkyState = false;
			this._loggedZone = null;
			this._loggedMode = -1;
			this._loggedGates = -1;
			this._matStaticDirty = true;
			SkyDome dome = this._dome;
			if (dome != null)
			{
				dome.NotifySceneChanged();
			}
			this._cam = null;
			this._nextCamAt = 0f;
			this._nightLatched = false;
			this._nightWeight = 0f;
			this._nightArmAt = -1f;
		}

		// Token: 0x060002D1 RID: 721 RVA: 0x00027B44 File Offset: 0x00025D44
		public void Tick()
		{
			try
			{
				bool flag = false;
				try
				{
					flag = XRSettings.isDeviceActive;
				}
				catch
				{
				}
				bool flag2 = (flag ? this._sunVr : this._sunDesktop);
				bool flag3 = (flag ? this._cloudVr : this._cloudDesktop);
				bool flag4 = (flag ? this._nightVr : this._nightDesktop);
				bool flag5 = this._wantSun && flag2;
				bool flag6 = this._wantClouds && flag3;
				bool flag7 = this._wantNight && flag4;
				bool flag8 = flag5 || flag6 || flag7;
				bool flag9 = (flag ? this._flareVr : this._flareDesktop);
				bool flag10 = this._wantFlare && flag9;
				bool flag11 = flag8 || flag10;
				this.UpdateNightWeight(flag7);
				SkySystem.Mode = this._mode;
				SkySystem.ReplaceStrength = this._strength;
				bool obj = this._engine != null && this._engine.Enabled && !this._engineDisabled;
				float num = (this._replaceAlways ? 1f : this._nightWeight);
				bool obj2 = obj;
				SkySystem.NightPassOn = obj2 != null && this._mode != 2 && flag7 && this._strength > 0.001f && num > 0.0001f;
				bool flag12 = obj2 != null && this._engine.GetMaterial("Hidden/LumaLooks/SkyReplace") != null;
				this.RefreshCamClearFlags();
				bool flag13 = flag7 && this._mode != 1 && this._mat != null && this._camClearsToSkybox;
				bool flag14 = (SkySystem.NightPassOn && flag12) || flag13;
				float num2 = Mathf.Clamp01(Mathf.Max((SkySystem.NightPassOn && flag12) ? (num * this._strength) : 0f, (flag13 && num > 0.001f) ? num : 0f));
				float num3 = Mathf.Clamp01(1f - num2);
				float num4 = Mathf.Clamp01(WorldLight.SunElevation * 33f);
				float num5 = ((flag5 && this._sunBrightness > 0.0001f) ? (num3 * num4) : 0f);
				bool flag15 = flag5 && this._moonBrightness > 0.0001f && WorldLight.SunElevation < 0f && num3 > 0.0001f;
				float num6 = Mathf.Max(num5, flag15 ? num3 : 0f);
				bool active = SkyShell.Active;
				SkySystem.SunPassOn = (obj2 & flag5) != null && num6 > 0.0001f && !active;
				SkySystem.CloudPassOn = (obj2 & flag6) != null && num3 > 0.0001f && !active;
				SkySystem.ScreenSpaceOn = SkySystem.NightPassOn || SkySystem.SunPassOn || SkySystem.CloudPassOn;
				this.ResolveUniforms(flag5, flag6, num3, flag15, flag);
				this._dome.Tick(flag11, flag14 ? num : 0f, this._nightLatched, flag14);
				SkySystem.DomeValid = flag11 && this._dome.DomeValid;
				SkySystem.DomeDistance = (SkySystem.DomeValid ? this._dome.DomeDistance : 0f);
				SkySystem.BackdropEstimate = this._dome.BackdropEstimate;
				SkySystem.BackdropDistance = (SkySystem.DomeValid ? Mathf.Min(this._backdrop, 0.85f * SkySystem.DomeDistance) : this._backdrop);
				SkySystem.UniReplaceParams2 = (flag11 ? new Vector4(flag8 ? num : 0f, flag8 ? num6 : 0f, SkySystem.DomeDistance, SkySystem.DomeValid ? 1f : 0f) : Vector4.zero);
				if (!flag8)
				{
					this.RestoreIfApplied();
					if (this._wantSun || this._wantClouds || this._wantNight)
					{
						this.MaybeLogSkyState(false, false, flag, flag5, flag6, flag7);
					}
				}
				else if (!flag7 || this._mode == 1 || num <= 0.001f)
				{
					this.RestoreIfApplied();
					this.MaybeLogSkyState(true, false, flag, flag5, flag6, flag7);
				}
				else if (!this.EnsureMaterial())
				{
					this.MaybeLogSkyState(true, false, flag, flag5, flag6, flag7);
				}
				else
				{
					if (!this._applied)
					{
						if (RenderSettings.skybox == this._mat)
						{
							this._applied = true;
						}
						else
						{
							this._origSkybox = RenderSettings.skybox;
							RenderSettings.skybox = this._mat;
							this._applied = true;
							this.ForceEnvRefresh();
						}
					}
					else if (RenderSettings.skybox != this._mat)
					{
						RenderSettings.skybox = this._mat;
						this._driftReApplied++;
					}
					this.PushToSkyboxMaterial();
					this.MaybeRefreshEnvironment();
					this.MaybeLogSkyState(true, true, flag, flag5, flag6, flag7);
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("SkySystem tick skipped: " + ex.Message);
			}
		}

		// Token: 0x060002D2 RID: 722 RVA: 0x00028020 File Offset: 0x00026220
		private void ResolveUniforms(bool sunOn, bool cloudOn, float dayWeight, bool moonAllowed, bool vrActive)
		{
			Vector3 resolvedSunDir = WorldLight.ResolvedSunDir;
			SkySystem.UniSunDir = new Vector4(resolvedSunDir.x, resolvedSunDir.y, resolvedSunDir.z, 0f);
			SkySystem.UniParams = new Vector4(this._sunSize, this._horizonWarmth, WorldLight.SkyDayFactor, this._starDensity);
			float num = Mathf.Max(Mathf.Clamp01(RainSensor.RainFactor), RainSensor.StormFactor);
			float num2 = 1f - num;
			float num3 = (sunOn ? this._sunBrightness : 0f) * num2;
			float num4 = (sunOn ? this._moonBrightness : 0f) * num2;
			SkySystem.RaysSunBrightness = (sunOn ? this._sunBrightness : 4f);
			bool flag = false;
			try
			{
				flag = WorldLight.SourceIsMoon;
			}
			catch
			{
			}
			SkySystem.RaysBodyBrightness = ((!sunOn) ? 4f : (flag ? (Mathf.Max(this._moonBrightness, 0f) / 1.5f * 4f) : this._sunBrightness));
			SkySystem.RaysBodySize = ((!sunOn) ? 1.2f : (flag ? (Mathf.Max(this._moonSize, 0f) / 0.9f * 1.2f) : this._sunSize));
			SkySystem.UniParams3 = new Vector4(num3, this._auroraSpeed, moonAllowed ? 1f : 0f, SkyShell.Active ? 1f : 0f);
			SkySystem.UniBodyParams = new Vector4(num4, this._moonSize, this._glowFalloff, 1f);
			SkySystem.UniCloudParams2 = new Vector4(this._cloudSoftness, this._cloudSunGlow * (1f - num * 0.8f), cloudOn ? 1f : 0f, Mathf.Clamp01(dayWeight));
			SkySystem.UniCloudParams3 = new Vector4(vrActive ? (this._vrPerfBalanced ? 10f : 13f) : 20f, this._cloudThickness, Mathf.Clamp01(num), 0f);
			this.ComposeCloudRainLanes(num);
			if (!this._paramsDirty)
			{
				return;
			}
			this._paramsDirty = false;
			this.ResolveAuthoredUniforms();
		}

		// Token: 0x060002D3 RID: 723 RVA: 0x0002823C File Offset: 0x0002643C
		private void ComposeCloudRainLanes(float rain)
		{
			float num = Mathf.Lerp(1f, 1.35f, rain);
			SkySystem.UniCloudParams = new Vector4(Mathf.Clamp01(this._cloudCoverage * num), Mathf.Clamp01(this._cloudDensity * num), this._cloudSpeed, this._cloudHeight);
			SkySystem.UniCloudTint = Vector4.Lerp(this._cloudTintLinear, SkySystem.CloudStormGreyLinear, rain);
		}

		// Token: 0x060002D4 RID: 724 RVA: 0x000282A0 File Offset: 0x000264A0
		private void ResolveAuthoredUniforms()
		{
			SkySystem.UniParams2 = new Vector4(this._starBrightness, (float)this._auroraMode, this._auroraIntensity, this._starSize);
			Color linear = this._auroraA.linear;
			Color linear2 = this._auroraB.linear;
			SkySystem.UniSkyDayZenith = new Vector4(this._dayZenith.r, this._dayZenith.g, this._dayZenith.b, 1f);
			SkySystem.UniSkyDayHorizon = new Vector4(this._dayHorizon.r, this._dayHorizon.g, this._dayHorizon.b, 1f);
			SkySystem.UniSkyDaySat = this._daySat;
			SkySystem.UniSkyDayHue = this._dayHue;
			SkySystem.UniAuroraA = new Vector4(linear.r, linear.g, linear.b, 1f);
			SkySystem.UniAuroraB = new Vector4(linear2.r, linear2.g, linear2.b, 1f);
			Color linear3 = this._sunTint.linear;
			Color linear4 = this._moonTint.linear;
			Color linear5 = this._cloudTint.linear;
			SkySystem.UniSunTint = new Vector4(linear3.r, linear3.g, linear3.b, 0f);
			SkySystem.UniMoonTint = new Vector4(linear4.r, linear4.g, linear4.b, 0f);
			this._cloudTintLinear = new Vector4(linear5.r, linear5.g, linear5.b, 0f);
			this.ComposeCloudRainLanes(Mathf.Max(Mathf.Clamp01(RainSensor.RainFactor), RainSensor.StormFactor));
		}

		// Token: 0x060002D5 RID: 725 RVA: 0x00028448 File Offset: 0x00026648
		private void RefreshCamClearFlags()
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup < this._nextCamAt && this._cam != null)
			{
				return;
			}
			this._nextCamAt = realtimeSinceStartup + 1f;
			try
			{
				if (this._cam == null)
				{
					this._cam = Camera.main;
				}
				if (this._cam != null)
				{
					this._camClearsToSkybox = this._cam.clearFlags == CameraClearFlags.SolidColor;
				}
			}
			catch
			{
			}
		}

		// Token: 0x060002D6 RID: 726 RVA: 0x000284D0 File Offset: 0x000266D0
		private void UpdateNightWeight(bool on)
		{
			if (!on)
			{
				this._nightLatched = false;
				this._nightWeight = 0f;
				this._nightArmAt = -1f;
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (this._nightArmAt < 0f)
			{
				this._nightArmAt = realtimeSinceStartup + 6f;
				return;
			}
			float skyDayFactor = WorldLight.SkyDayFactor;
			bool sourceIsMoon = WorldLight.SourceIsMoon;
			if (realtimeSinceStartup >= this._nightArmAt)
			{
				if (this._nightLatched)
				{
					if (skyDayFactor >= 0.3f && !sourceIsMoon)
					{
						this._nightLatched = false;
					}
				}
				else if (skyDayFactor <= 0.12f || sourceIsMoon)
				{
					this._nightLatched = true;
				}
			}
			float num = (this._nightLatched ? 1f : 0f);
			this._nightWeight = Mathf.MoveTowards(this._nightWeight, num, Time.unscaledDeltaTime / 3f);
		}

		// Token: 0x060002D7 RID: 727 RVA: 0x00028598 File Offset: 0x00026798
		private void PushToSkyboxMaterial()
		{
			if (this._mat == null)
			{
				return;
			}
			this._mat.SetVector(ShaderIds.SkySunDir, SkySystem.UniSunDir);
			this._mat.SetVector(ShaderIds.SkyParams, SkySystem.UniParams);
			this._mat.SetVector(ShaderIds.SkyParams3, SkySystem.UniParams3);
			this._mat.SetVector(ShaderIds.SkyBodyParams, SkySystem.UniBodyParams);
			this._mat.SetVector(ShaderIds.CloudParams2, SkySystem.UniCloudParams2);
			this._mat.SetVector(ShaderIds.CloudParams3, SkySystem.UniCloudParams3);
			bool flag = RainSensor.RainFactor > 0f;
			if (flag || this._cloudRainPushed)
			{
				this._cloudRainPushed = flag;
				this._mat.SetVector(ShaderIds.CloudParams, SkySystem.UniCloudParams);
				this._mat.SetVector(ShaderIds.CloudTint, SkySystem.UniCloudTint);
			}
			if (!this._matStaticDirty)
			{
				return;
			}
			this._matStaticDirty = false;
			this._mat.SetVector(ShaderIds.SkyParams2, SkySystem.UniParams2);
			this._mat.SetVector(ShaderIds.SkyDayZenith, SkySystem.UniSkyDayZenith);
			this._mat.SetVector(ShaderIds.SkyDayHorizon, SkySystem.UniSkyDayHorizon);
			this._mat.SetFloat(ShaderIds.SkyDaySat, SkySystem.UniSkyDaySat);
			this._mat.SetFloat(ShaderIds.SkyDayHue, SkySystem.UniSkyDayHue);
			this._mat.SetVector(ShaderIds.SkyAuroraA, SkySystem.UniAuroraA);
			this._mat.SetVector(ShaderIds.SkyAuroraB, SkySystem.UniAuroraB);
			this._mat.SetVector(ShaderIds.SkySunTint, SkySystem.UniSunTint);
			this._mat.SetVector(ShaderIds.SkyMoonTint, SkySystem.UniMoonTint);
			this._mat.SetVector(ShaderIds.CloudParams, SkySystem.UniCloudParams);
			this._mat.SetVector(ShaderIds.CloudTint, SkySystem.UniCloudTint);
			Texture2D texture2D = ((this._engine != null) ? this._engine.GetTexture("MoonAlbedo") : null);
			if (texture2D != null)
			{
				this._mat.SetTexture(ShaderIds.MoonTex, texture2D);
			}
		}

		// Token: 0x060002D8 RID: 728 RVA: 0x000287A8 File Offset: 0x000269A8
		private void MaybeLogSkyState(bool on, bool skyboxPath, bool vrActive, bool sunOn, bool cloudOn, bool nightOn)
		{
			string zoneName = MapSense.ZoneName;
			bool flag = this._engine != null && this._engine.Enabled && !this._engineDisabled;
			int num = (sunOn ? 1 : 0) | (cloudOn ? 2 : 0) | (nightOn ? 4 : 0);
			if (this._loggedSkyState && this._loggedOn == on && this._loggedMode == this._mode && this._loggedGates == num && this._loggedNight == this._nightLatched && this._loggedEngineLive == flag && string.Equals(this._loggedZone, zoneName, StringComparison.Ordinal))
			{
				return;
			}
			this._loggedGates = num;
			this._loggedSkyState = true;
			this._loggedOn = on;
			this._loggedMode = this._mode;
			this._loggedNight = this._nightLatched;
			this._loggedEngineLive = flag;
			this._loggedZone = zoneName;
			bool flag2 = this._applied && RenderSettings.skybox == this._mat;
			Material material = ((this._engine != null) ? this._engine.GetMaterial("Hidden/LumaLooks/SkyReplace") : null);
			string text = "(none)";
			string text2 = "?";
			try
			{
				Camera main = Camera.main;
				if (main != null)
				{
					text = main.name;
					text2 = main.clearFlags.ToString();
				}
			}
			catch
			{
			}
			this._log.LogInfo(string.Concat(new string[]
			{
				string.Format("SKY: sunMoon(want={0} vr={1} dk={2} ", this._wantSun ? 1 : 0, this._sunVr ? 1 : 0, this._sunDesktop ? 1 : 0),
				string.Format("on={0}) clouds(want={1} vr={2} ", sunOn ? 1 : 0, this._wantClouds ? 1 : 0, this._cloudVr ? 1 : 0),
				string.Format("dk={0} on={1}) nightSky(want={2} ", this._cloudDesktop ? 1 : 0, cloudOn ? 1 : 0, this._wantNight ? 1 : 0),
				string.Format("vr={0} dk={1} on={2}) ", this._nightVr ? 1 : 0, this._nightDesktop ? 1 : 0, nightOn ? 1 : 0),
				string.Format("vrActive={0} | ", vrActive ? 1 : 0),
				string.Format("method={0} anyOn={1} | skyboxPath={2} swapped={3} ", new object[]
				{
					SkySystem.ModeName,
					on ? 1 : 0,
					skyboxPath ? 1 : 0,
					flag2 ? 1 : 0
				}),
				string.Format("drift={0} origWasNull={1} | ", this._driftReApplied, (this._origSkybox == null) ? 1 : 0),
				"matSkybox=",
				SkySystem.MatState(this._mat),
				" matSkyReplace=",
				SkySystem.MatState(material),
				" ",
				string.Format("engineLive={0} camClearsSkybox={1} | ", flag ? 1 : 0, this._camClearsToSkybox ? 1 : 0),
				string.Format("screenSpace={0} nightPass={1} ", SkySystem.ScreenSpaceOn ? 1 : 0, SkySystem.NightPassOn ? 1 : 0),
				string.Format("sunPass={0} cloudPass={1} ", SkySystem.SunPassOn ? 1 : 0, SkySystem.CloudPassOn ? 1 : 0),
				string.Format("replaceWeight={0:0.###} ", SkySystem.UniReplaceParams2.x),
				string.Format("bodyWeight={0:0.###} moonAllowed={1:0} ", SkySystem.UniReplaceParams2.y, SkySystem.UniParams3.z),
				string.Format("dayWeight={0:0.###} ", SkySystem.UniCloudParams2.w),
				string.Format("cloudsOn={0:0} (dayFactor={1:0.###} ", SkySystem.UniCloudParams2.z, WorldLight.DayFactor),
				string.Format("skyDay={0:0.###} ", WorldLight.SkyDayFactor),
				string.Format("isMoon={0} latch={1}) | ", WorldLight.SourceIsMoon, this._nightLatched ? "NIGHT" : "DAY"),
				string.Format("dome: valid={0} distance={1:0}m radius={2:0}m ", SkySystem.DomeValid ? 1 : 0, SkySystem.DomeDistance, this._dome.DomeRadius),
				string.Format("backdropEst={0:0}m ", SkySystem.BackdropEstimate),
				string.Format("found={0} hidden={1} | ", this._dome.FoundCount, this._dome.HiddenCount),
				string.Format("sunPos={0} hour={1} slider={2:0.00}h | ", this.SunPosName, this.HourText, this._timeOfDay),
				string.Format("sun(bright={0:0.##} size={1:0.##}) ", SkySystem.UniParams3.x, this._sunSize),
				string.Format("moon(bright={0:0.##} size={1:0.##}) glowFalloff={2:0.##} ", SkySystem.UniBodyParams.x, this._moonSize, this._glowFalloff),
				string.Format("rain={0:0.00} | ", RainSensor.RainFactor),
				string.Format("cloud(cover={0:0.##} dens={1:0.##} speed={2:0.##} ", this._cloudCoverage, this._cloudDensity, this._cloudSpeed),
				string.Format("height={0:0.##} thick={1:0.##} soft={2:0.##} ", this._cloudHeight, this._cloudThickness, this._cloudSoftness),
				string.Format("sunGlow={0:0.##}) cloudSteps={1:0} | ", this._cloudSunGlow, SkySystem.UniCloudParams3.x),
				string.Format("starSize={0:0.##} auroraSpeed={1:0.##} ", this._starSize, this._auroraSpeed),
				string.Format("strength={0:0.##} backdrop={1:0}m→{2:0}m | ", this._strength, this._backdrop, SkySystem.BackdropDistance),
				string.Format("cam='{0}' clearFlags={1} | zone={2} outdoor={3} ", new object[]
				{
					text,
					text2,
					zoneName,
					MapSense.IsOutdoor
				}),
				string.Format("hasSky={0} (zone terms are DIAGNOSTIC ONLY since realism4 §I — they gate nothing)", MapSense.HasSky)
			}));
		}

		// Token: 0x060002D9 RID: 729 RVA: 0x00028E20 File Offset: 0x00027020
		private static string MatState(Material m)
		{
			if (m == null)
			{
				return "NULL";
			}
			Shader shader = m.shader;
			if (shader == null)
			{
				return "SHADERNULL";
			}
			if (!shader.isSupported)
			{
				return "UNSUPPORTED(supported=0)";
			}
			return "ok(supported=1)";
		}

		// Token: 0x060002DA RID: 730 RVA: 0x00028E68 File Offset: 0x00027068
		private bool EnsureMaterial()
		{
			if (this._mat != null)
			{
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/Skybox") : null);
			if (shader == null)
			{
				if (!this._shaderMissingLogged)
				{
					this._shaderMissingLogged = true;
					this._log.LogWarning("SkySystem: shader 'LumaLooks/Skybox' not in the bundle — the skybox-swap path is disabled (the screen-space SkyReplace pass is unaffected).");
				}
				return false;
			}
			this._mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this._matStaticDirty = true;
			this._log.LogInfo(string.Format("SkySystem: procedural sky material ready ({0}, isSupported={1}).", "LumaLooks/Skybox", shader.isSupported));
			return true;
		}

		// Token: 0x060002DB RID: 731 RVA: 0x00028F08 File Offset: 0x00027108
		private void MaybeRefreshEnvironment()
		{
			float dayFactor = WorldLight.DayFactor;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (Mathf.Abs(dayFactor - this._envDay) <= 0.05f || realtimeSinceStartup < this._envNextAt)
			{
				return;
			}
			this._envDay = dayFactor;
			this._envNextAt = realtimeSinceStartup + 2f;
			DynamicGI.UpdateEnvironment();
		}

		// Token: 0x060002DC RID: 732 RVA: 0x00028F58 File Offset: 0x00027158
		private void ForceEnvRefresh()
		{
			this._envDay = WorldLight.DayFactor;
			this._envNextAt = Time.realtimeSinceStartup + 2f;
			DynamicGI.UpdateEnvironment();
		}

		// Token: 0x060002DD RID: 733 RVA: 0x00028F7B File Offset: 0x0002717B
		private void RestoreIfApplied()
		{
			if (!this._applied)
			{
				return;
			}
			if (RenderSettings.skybox == this._mat)
			{
				RenderSettings.skybox = this._origSkybox;
				DynamicGI.UpdateEnvironment();
			}
			this._origSkybox = null;
			this._applied = false;
		}

		// Token: 0x060002DE RID: 734 RVA: 0x00028FB4 File Offset: 0x000271B4
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			SkySystem.ScreenSpaceOn = false;
			SkySystem.NightPassOn = false;
			SkySystem.SunPassOn = false;
			SkySystem.CloudPassOn = false;
			SkySystem.UniReplaceParams2 = Vector4.zero;
			SkySystem.UniCloudParams2 = new Vector4(SkySystem.UniCloudParams2.x, SkySystem.UniCloudParams2.y, 0f, 0f);
			SkySystem.UniParams3 = new Vector4(0f, SkySystem.UniParams3.y, 0f, 0f);
			SkySystem.UniBodyParams = new Vector4(0f, SkySystem.UniBodyParams.y, SkySystem.UniBodyParams.z, 1f);
			SkySystem.RaysSunBrightness = 4f;
			SkySystem.DomeValid = false;
			SkySystem.DomeDistance = 0f;
			try
			{
				SkyDome dome = this._dome;
				if (dome != null)
				{
					dome.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				this.RestoreIfApplied();
			}
			catch
			{
			}
			if (this._mat != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._mat);
				}
				catch
				{
				}
				this._mat = null;
			}
		}

		// Token: 0x04000642 RID: 1602
		private const string SkyShaderName = "LumaLooks/Skybox";

		// Token: 0x04000643 RID: 1603
		private const string SkyReplaceShaderName = "Hidden/LumaLooks/SkyReplace";

		// Token: 0x04000644 RID: 1604
		public const int ModeAuto = 0;

		// Token: 0x04000645 RID: 1605
		public const int ModeScreenSpace = 1;

		// Token: 0x04000646 RID: 1606
		public const int ModeSkybox = 2;

		// Token: 0x04000647 RID: 1607
		private const float EnvThrottleSeconds = 2f;

		// Token: 0x04000648 RID: 1608
		private const float EnvDayDelta = 0.05f;

		// Token: 0x04000649 RID: 1609
		private const float NightLatchOn = 0.12f;

		// Token: 0x0400064A RID: 1610
		private const float NightLatchOff = 0.3f;

		// Token: 0x0400064B RID: 1611
		private const float NightFadeSeconds = 3f;

		// Token: 0x0400064C RID: 1612
		private const float NightWarmupSeconds = 6f;

		// Token: 0x0400064D RID: 1613
		private const float SunUpRamp = 33f;

		// Token: 0x0400064E RID: 1614
		private const float DomeBackdropFraction = 0.85f;

		// Token: 0x0400064F RID: 1615
		private readonly ManualLogSource _log;

		// Token: 0x04000650 RID: 1616
		private readonly RenderEngine _engine;

		// Token: 0x04000651 RID: 1617
		private readonly SkyDome _dome;

		// Token: 0x04000652 RID: 1618
		private Material _mat;

		// Token: 0x04000653 RID: 1619
		private Material _origSkybox;

		// Token: 0x04000654 RID: 1620
		private bool _applied;

		// Token: 0x04000655 RID: 1621
		private bool _shaderMissingLogged;

		// Token: 0x04000656 RID: 1622
		private int _driftReApplied;

		// Token: 0x04000657 RID: 1623
		private bool _loggedSkyState;

		// Token: 0x04000658 RID: 1624
		private string _loggedZone;

		// Token: 0x04000659 RID: 1625
		private bool _loggedOn;

		// Token: 0x0400065A RID: 1626
		private int _loggedMode = -1;

		// Token: 0x0400065B RID: 1627
		private int _loggedGates = -1;

		// Token: 0x0400065C RID: 1628
		private bool _loggedNight;

		// Token: 0x0400065D RID: 1629
		private bool _loggedEngineLive = true;

		// Token: 0x0400065E RID: 1630
		private bool _wantSun;

		// Token: 0x0400065F RID: 1631
		private bool _sunVr = true;

		// Token: 0x04000660 RID: 1632
		private bool _sunDesktop = true;

		// Token: 0x04000661 RID: 1633
		private float _sunBrightness = 4f;

		// Token: 0x04000662 RID: 1634
		private float _sunSize = 1.2f;

		// Token: 0x04000663 RID: 1635
		private Color _sunTint = new Color(1f, 0.9098f, 0.7529f);

		// Token: 0x04000664 RID: 1636
		private float _moonBrightness = 0.3f;

		// Token: 0x04000665 RID: 1637
		private float _moonSize = 0.9f;

		// Token: 0x04000666 RID: 1638
		private Color _moonTint = new Color(0.749f, 0.8314f, 1f);

		// Token: 0x04000667 RID: 1639
		private float _glowFalloff = 0.5f;

		// Token: 0x04000668 RID: 1640
		private int _sunPosition = 2;

		// Token: 0x04000669 RID: 1641
		private float _timeOfDay = 10f;

		// Token: 0x0400066A RID: 1642
		private bool _wantClouds;

		// Token: 0x0400066B RID: 1643
		private bool _cloudVr = true;

		// Token: 0x0400066C RID: 1644
		private bool _cloudDesktop = true;

		// Token: 0x0400066D RID: 1645
		private float _cloudCoverage = 0.4f;

		// Token: 0x0400066E RID: 1646
		private float _cloudDensity = 0.5f;

		// Token: 0x0400066F RID: 1647
		private float _cloudSpeed = 0.3f;

		// Token: 0x04000670 RID: 1648
		private float _cloudHeight = 0.5f;

		// Token: 0x04000671 RID: 1649
		private float _cloudSoftness = 0.5f;

		// Token: 0x04000672 RID: 1650
		private float _cloudSunGlow = 0.5f;

		// Token: 0x04000673 RID: 1651
		private float _cloudThickness = 0.5f;

		// Token: 0x04000674 RID: 1652
		private Color _cloudTint = Color.white;

		// Token: 0x04000675 RID: 1653
		private bool _vrPerfBalanced;

		// Token: 0x04000676 RID: 1654
		private Vector4 _cloudTintLinear = new Vector4(1f, 1f, 1f, 0f);

		// Token: 0x04000677 RID: 1655
		private static readonly Vector4 CloudStormGreyLinear = SkySystem.StormGrey();

		// Token: 0x04000678 RID: 1656
		private bool _cloudRainPushed;

		// Token: 0x04000679 RID: 1657
		private bool _wantNight;

		// Token: 0x0400067A RID: 1658
		private bool _nightVr = true;

		// Token: 0x0400067B RID: 1659
		private bool _nightDesktop = true;

		// Token: 0x0400067C RID: 1660
		private int _mode;

		// Token: 0x0400067D RID: 1661
		private bool _replaceAlways = true;

		// Token: 0x0400067E RID: 1662
		private float _strength = 1f;

		// Token: 0x0400067F RID: 1663
		private float _backdrop = 300f;

		// Token: 0x04000680 RID: 1664
		private float _horizonWarmth = 0.5f;

		// Token: 0x04000681 RID: 1665
		private float _starDensity = 0.5f;

		// Token: 0x04000682 RID: 1666
		private float _starBrightness = 0.6f;

		// Token: 0x04000683 RID: 1667
		private float _starSize = 0.35f;

		// Token: 0x04000684 RID: 1668
		private int _auroraMode = 1;

		// Token: 0x04000685 RID: 1669
		private float _auroraIntensity = 0.5f;

		// Token: 0x04000686 RID: 1670
		private float _auroraSpeed = 0.5f;

		// Token: 0x04000687 RID: 1671
		private Color _dayZenith = new Color(0.18f, 0.498f, 0.859f);

		// Token: 0x04000688 RID: 1672
		private Color _dayHorizon = new Color(0.612f, 0.784f, 0.933f);

		// Token: 0x04000689 RID: 1673
		private float _daySat = 1f;

		// Token: 0x0400068A RID: 1674
		private float _dayHue = 0.5f;

		// Token: 0x0400068B RID: 1675
		private Color _auroraA = new Color(0.235f, 0.941f, 0.549f);

		// Token: 0x0400068C RID: 1676
		private Color _auroraB = new Color(0.235f, 0.612f, 0.941f);

		// Token: 0x0400068D RID: 1677
		private bool _wantFlare;

		// Token: 0x0400068E RID: 1678
		private bool _flareVr = true;

		// Token: 0x0400068F RID: 1679
		private bool _flareDesktop = true;

		// Token: 0x04000690 RID: 1680
		private bool _paramsDirty = true;

		// Token: 0x04000691 RID: 1681
		private bool _matStaticDirty = true;

		// Token: 0x04000692 RID: 1682
		private float _envDay = -1f;

		// Token: 0x04000693 RID: 1683
		private float _envNextAt;

		// Token: 0x04000694 RID: 1684
		private bool _nightLatched;

		// Token: 0x04000695 RID: 1685
		private float _nightWeight;

		// Token: 0x04000696 RID: 1686
		private float _nightArmAt = -1f;

		// Token: 0x04000697 RID: 1687
		private bool _engineDisabled;

		// Token: 0x04000698 RID: 1688
		private Camera _cam;

		// Token: 0x04000699 RID: 1689
		private float _nextCamAt;

		// Token: 0x0400069A RID: 1690
		private bool _camClearsToSkybox = true;

		// Token: 0x0400069B RID: 1691
		private const float CamPollSeconds = 1f;
	}
}
