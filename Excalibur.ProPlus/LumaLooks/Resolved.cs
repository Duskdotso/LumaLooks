using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x0200001E RID: 30
	internal sealed class Resolved
	{
		// Token: 0x06000117 RID: 279 RVA: 0x0000E6B4 File Offset: 0x0000C8B4
		private static Vector4 Lin(Color c)
		{
			Color linear = c.linear;
			return new Vector4(linear.r, linear.g, linear.b, 1f);
		}

		private bool _vr;

		private static EffectSettings Effect(string id, Settings settings)
		{
			return settings.Effect(id);
		}

		private bool IsOn(string id, Settings settings)
		{
			return settings.IsOn(id, this._vr);
		}

		private int ShiftTier(int q)
		{
			return QualityTiers.Shift(q, this.QualityLevel);
		}

		// Token: 0x06000118 RID: 280 RVA: 0x0000E6E8 File Offset: 0x0000C8E8
		public void Compute(Settings s, bool vr)
		{
			this._vr = vr;
			this.QualityLevel = s.QualityFor(vr);
			this.PerfBalanced = QualityTiers.CheapTail(this.QualityLevel);
			this.HalfResDiv = QualityTiers.HalfDiv(this.QualityLevel);
			this.AoOn = IsOn("ssao", s);
			EffectSettings effectSettings = Effect("ssao", s);
			this.AoIntensity = ((effectSettings != null) ? effectSettings.GetFloat("intensity", 0.6f) : 0.6f);
			this.AoRadius = ((effectSettings != null) ? effectSettings.GetFloat("radius", 0.4f) : 0.4f);
			this.AoPower = ((effectSettings != null) ? effectSettings.GetFloat("power", 1.5f) : 1.5f);
			int num = ShiftTier((effectSettings != null) ? effectSettings.GetEnumIndex("quality", new string[] { "Low", "Medium", "High" }, 1) : 1);
			this.AoSamples = ((num == 0) ? 6f : ((num == 2) ? 16f : 10f));
			this.SsrOn = IsOn("ssr", s);
			EffectSettings effectSettings2 = Effect("ssr", s);
			this.SsrRainOnly = effectSettings2 == null || effectSettings2.GetEnumIndex("mode", new string[] { "Always", "RainOnly" }, 1) == 1;
			this.SsrIntensity = ((effectSettings2 != null) ? effectSettings2.GetFloat("intensity", 0.5f) : 0.5f);
			this.SsrMaxDist = ((effectSettings2 != null) ? effectSettings2.GetFloat("maxDistance", 40f) : 40f);
			this.SsrBlur = ((effectSettings2 != null) ? effectSettings2.GetFloat("blur", 0.35f) : 0.35f);
			this.SsrSurfaceAware = ((effectSettings2 != null) ? effectSettings2.GetFloat("surfaceAwareness", 0.7f) : 0.7f);
			this.SsrMetalSharp = ((effectSettings2 != null) ? effectSettings2.GetFloat("metalSharpness", 0.6f) : 0.6f);
			int num2 = ShiftTier((effectSettings2 != null) ? effectSettings2.GetEnumIndex("quality", new string[] { "Low", "Medium", "High" }, 1) : 1);
			this.SsrSteps = ((num2 == 0) ? 16f : ((num2 == 2) ? 64f : 32f));
			this.SunlightOn = IsOn("sunlight", s);
			this.SunMoonOn = IsOn("sunMoon", s);
			EffectSettings effectSettings3 = Effect("sunMoon", s);
			EffectSettings effectSettings4 = Effect("sunlight", s);
			this.SunlightIntensity = ((effectSettings4 != null) ? effectSettings4.GetFloat("intensity", 0.5f) : 0.5f);
			this.SunlightReach = ((effectSettings4 != null) ? effectSettings4.GetFloat("reach", 60f) : 60f);
			this.SunlightSigmaT = ((effectSettings4 != null) ? effectSettings4.GetFloat("density", 0.15f) : 0.15f) * 0.03f;
			int num3 = ShiftTier((effectSettings4 != null) ? effectSettings4.GetEnumIndex("quality", new string[] { "Low", "Medium", "High" }, 1) : 1);
			this.SunlightSteps = ((num3 == 0) ? 12f : ((num3 == 2) ? 32f : 20f));
			this.SunlightWarmth = ((effectSettings4 != null) ? effectSettings4.GetFloat("warmth", 0.65f) : 0.65f);
			this.SunlightClarity = ((effectSettings4 != null) ? effectSettings4.GetFloat("clarity", 0.35f) : 0.35f);
			this.SunlightSideGlow = ((effectSettings4 != null) ? effectSettings4.GetFloat("sideGlow", 0.5f) : 0.5f);
			EffectSettings effectSettings5 = Effect("nightSky", s);
			this.CloudShadowStrength = ((effectSettings5 != null) ? effectSettings5.GetFloat("cloudShadowStrength", 0f) : 0f);
			int num4 = ((effectSettings5 != null) ? effectSettings5.GetEnumIndex("cloudResolution", new string[] { "Full", "Half", "Quarter" }, 0) : 0);
			num4 = QualityTiers.ShiftInverted(num4, this.QualityLevel);
			this.CloudsResDiv = ((num4 == 2) ? 4 : ((num4 == 1) ? 2 : 1));
			this.CloudShadowSoftness = ((effectSettings5 != null) ? effectSettings5.GetFloat("cloudShadowSoftness", 0.5f) : 0.5f);
			this.SunlightRayThickness = ((effectSettings4 != null) ? effectSettings4.GetFloat("rayThickness", 0.5f) : 0.5f);
			this.SunlightShimmer = ((effectSettings4 != null) ? effectSettings4.GetFloat("rayShimmer", 0f) : 0f);
			this.SunlightRayAngle = ((effectSettings4 != null) ? effectSettings4.GetFloat("rayAngle", 0f) : 0f);
			WorldLight.RayAngleDeg = (IsOn("sunlight", s) ? this.SunlightRayAngle : 0f);
			if (this.SunlightRayThickness < 0.34f)
			{
				this.SunlightSteps = Mathf.Min(this.SunlightSteps + 8f, 32f);
			}
			this.SunlightSurfaceLight = ((effectSettings3 != null) ? effectSettings3.GetFloat("surfaceLight", 0.35f) : 0.35f);
			this.SunlightPlayerShade = 0f * ((effectSettings3 != null) ? effectSettings3.GetFloat("playerShade", 0.5f) : 0.5f);
			this.SunlightRayRelief = ((effectSettings4 != null) ? effectSettings4.GetFloat("rayRelief", 0.7f) : 0.7f);
			this.GiOn = IsOn("ssgi", s);
			EffectSettings effectSettings6 = Effect("ssgi", s);
			this.GiIntensity = ((effectSettings6 != null) ? effectSettings6.GetFloat("intensity", 0.35f) : 0.35f);
			this.GiRadius = ((effectSettings6 != null) ? effectSettings6.GetFloat("radius", 3f) : 3f);
			this.GiColorBleed = ((effectSettings6 != null) ? effectSettings6.GetFloat("colorBleed", 0.7f) : 0.7f);
			this.GiEmissive = ((effectSettings6 != null) ? effectSettings6.GetFloat("emitters", 1f) : 1f);
			int num5 = ShiftTier((effectSettings6 != null) ? effectSettings6.GetEnumIndex("quality", new string[] { "Low", "Medium", "High" }, 1) : 1);
			this.GiRays = ((num5 == 0) ? 8f : ((num5 == 2) ? 24f : 16f));
			this.GiSharpness = ((effectSettings6 != null) ? effectSettings6.GetFloat("sharpness", 0.6f) : 0.6f);
			this.GiQuality = num5;
			this.PShadowOn = IsOn("playerShadow", s);
			EffectSettings effectSettings7 = Effect("playerShadow", s);
			this.PShadowIntensity = ((effectSettings7 != null) ? effectSettings7.GetFloat("intensity", 0.7f) : 0.7f);
			this.PShadowSoftness = ((effectSettings7 != null) ? effectSettings7.GetFloat("softness", 0.4f) : 0.4f);
			this.PShadowMode = ((effectSettings7 != null) ? ((float)effectSettings7.GetEnumIndex("mode", new string[] { "Sun", "Contact", "Both" }, 0)) : 0f);
			this.PShadowContact = this.PShadowOn && this.PShadowMode >= 0.5f;
			this.TdOn = IsOn("trueDarkness", s);
			EffectSettings effectSettings8 = Effect("trueDarkness", s);
			this.TdIntensity = ((effectSettings8 != null) ? effectSettings8.GetFloat("intensity", 0.6f) : 0.6f);
			this.TdReach = ((effectSettings8 != null) ? effectSettings8.GetFloat("reach", 12f) : 12f);
			this.TdFloor = ((effectSettings8 != null) ? effectSettings8.GetFloat("floor", 0.12f) : 0.12f);
			this.TdEnclosure = ((effectSettings8 != null) ? effectSettings8.GetFloat("enclosure", 0.55f) : 0.55f);
			this.HazeOn = IsOn("haze", s);
			EffectSettings effectSettings9 = Effect("haze", s);
			float num6 = ((effectSettings9 != null) ? effectSettings9.GetFloat("density", 0.15f) : 0.15f);
			this.HazeDensity = num6 * num6 * 0.15f;
			this.HazeStart = ((effectSettings9 != null) ? effectSettings9.GetFloat("startDistance", 25f) : 25f);
			this.HazeSunScatter = ((effectSettings9 != null) ? effectSettings9.GetFloat("sunScatter", 0.5f) : 0.5f);
			float num7 = ((effectSettings9 != null) ? effectSettings9.GetFloat("heightFalloff", 0.25f) : 0.25f);
			this.HazeHeightFalloff = num7 * num7 * 0.4f;
			this.HazeWisps = ((effectSettings9 != null) ? effectSettings9.GetFloat("wispiness", 0.3f) : 0.3f);
			this.HazeSkyVeil = ((effectSettings9 != null) ? effectSettings9.GetFloat("skyVeil", 0.25f) : 0.25f);
			this.PuddlesOn = false;
			EffectSettings effectSettings10 = Effect("puddles", s);
			this.PuddleCoverage = ((effectSettings10 != null) ? effectSettings10.GetFloat("coverage", 0.5f) : 0.5f);
			this.PuddleRipples = ((effectSettings10 != null) ? effectSettings10.GetFloat("ripples", 0.6f) : 0.6f);
			this.PuddleMirror = ((effectSettings10 != null) ? effectSettings10.GetFloat("reflectivity", 0.75f) : 0.75f);
			this.PuddleRippleSize = ((effectSettings10 != null) ? effectSettings10.GetFloat("rippleSize", 0.2f) : 0.2f);
			this.PuddleRippleSpeed = ((effectSettings10 != null) ? effectSettings10.GetFloat("rippleSpeed", 0.7f) : 0.7f);
			this.PuddleRippleCount = ((effectSettings10 != null) ? effectSettings10.GetFloat("rippleCount", 0.85f) : 0.85f);
			this.PuddleOpenArea = ((effectSettings10 != null) ? effectSettings10.GetFloat("openArea", 0.3f) : 0.3f);
			this.PuddleFogginess = ((effectSettings10 != null) ? effectSettings10.GetFloat("fogginess", 0.3f) : 0.3f);
			this.HazeMaxBrightness = ((effectSettings9 != null) ? effectSettings9.GetFloat("maxBrightness", 0.55f) : 0.55f);
			this.HazeTint = ((effectSettings9 != null) ? Resolved.Lin(effectSettings9.GetColor("tint", Color.white)) : new Vector4(1f, 1f, 1f, 1f));
			this.FlareOn = IsOn("lensFlare", s);
			EffectSettings effectSettings11 = Effect("lensFlare", s);
			this.FlareIntensity = ((effectSettings11 != null) ? effectSettings11.GetFloat("intensity", 0.6f) : 0.6f);
			this.FlareStreakLen = ((effectSettings11 != null) ? effectSettings11.GetFloat("streakLength", 0.5f) : 0.5f);
			this.FlareEaseRate = ((effectSettings11 != null) ? effectSettings11.GetFloat("coverEase", 3f) : 3f);
			this.FlareMode = (float)((effectSettings11 != null) ? effectSettings11.GetEnumIndex("mode", new string[] { "Sun", "Moon", "Both" }, 2) : 2);
			this.FlareStreakCount = ((effectSettings11 != null) ? Mathf.Clamp(effectSettings11.GetFloat("streakCount", 12f), 4f, 24f) : 12f);
			this.FlareDispersion = ((effectSettings11 != null) ? effectSettings11.GetFloat("dispersion", 0.35f) : 0.35f);
			this.FlareGhost = ((effectSettings11 != null) ? effectSettings11.GetFloat("ghostStrength", 0.35f) : 0.35f);
			this.FlareShimmer = ((effectSettings11 != null) ? effectSettings11.GetFloat("shimmer", 0.3f) : 0.3f);
			this.WetOn = s.Master;
			this.WetStrength = 0.6f;
			this.StormOn = s.Master;
			this.StormStrength = 0.45f;
			this.RainVisibility = 0.25f;
			this.AdaptiveOn = IsOn("adaptive", s);
			EffectSettings effectSettings12 = Effect("adaptive", s);
			this.AdaptiveStrength = ((effectSettings12 != null) ? effectSettings12.GetFloat("strength", 0.7f) : 0.7f);
			this.BloomOn = IsOn("bloom", s);
			EffectSettings effectSettings13 = Effect("bloom", s);
			this.BloomThreshold = ((effectSettings13 != null) ? effectSettings13.GetFloat("threshold", 1f) : 1f);
			this.BloomScatter = ((effectSettings13 != null) ? effectSettings13.GetFloat("scatter", 0.55f) : 0.55f);
			this.BloomIntensity = (this.BloomOn ? (((effectSettings13 != null) ? effectSettings13.GetFloat("intensity", 0.7f) : 0.7f) * 0.5f) : 0f);
			this.BloomTint = ((effectSettings13 != null) ? Resolved.Lin(effectSettings13.GetColor("tint", Color.white)) : new Vector4(1f, 1f, 1f, 1f));
			this.BloomHighlights = (this.BloomOn ? ((effectSettings13 != null) ? effectSettings13.GetFloat("highlights", 0.7f) : 0.7f) : 0f);
			this.DofOn = IsOn("dof", s);
			EffectSettings effectSettings14 = Effect("dof", s);
			this.DofFocus = ((effectSettings14 != null) ? effectSettings14.GetFloat("focusDistance", 6f) : 6f);
			this.DofStrength = ((effectSettings14 != null) ? effectSettings14.GetFloat("strength", 0.5f) : 0.5f);
			this.DofMaxRadius = ((effectSettings14 != null) ? effectSettings14.GetFloat("bokehRadius", 14f) : 14f);
			this.DofFocusSpeed = ((effectSettings14 != null) ? effectSettings14.GetFloat("focusSpeed", 1f) : 1f);
			this.DofAutoFocus = ((effectSettings14 != null) ? effectSettings14.GetFloat("autoFocus", 1f) : 1f);
			if (vr)
			{
				this.DofAutoFocus = 0f;
			}
			this.DofNearStrength = ((effectSettings14 != null) ? effectSettings14.GetFloat("nearStrength", 0.35f) : 0.35f);
			this.DofBokehGamma = ((effectSettings14 != null) ? effectSettings14.GetFloat("bokehGamma", 2.2f) : 2.2f);
			this.DistBlurOn = IsOn("distanceBlur", s);
			EffectSettings effectSettings15 = Effect("distanceBlur", s);
			this.DistBlurStart = ((effectSettings15 != null) ? effectSettings15.GetFloat("startDistance", 15f) : 15f);
			this.DistBlurEnd = ((effectSettings15 != null) ? effectSettings15.GetFloat("endDistance", 60f) : 60f);
			this.DistBlurMax = ((effectSettings15 != null) ? effectSettings15.GetFloat("maxBlur", 10f) : 10f);
			this.BlurMode = (this.DistBlurOn ? 1f : 0f);
			this.BlurStageOn = this.DofOn || this.DistBlurOn;
			this.MbOn = IsOn("motionBlur", s);
			EffectSettings effectSettings16 = Effect("motionBlur", s);
			this.MbAmount = ((effectSettings16 != null) ? effectSettings16.GetFloat("amount", 0.2f) : 0.2f);
			EffectSettings effectSettings17 = Effect("tonemap", s);
			bool flag = IsOn("tonemap", s);
			int num8 = ((effectSettings17 != null) ? effectSettings17.GetEnumIndex("mode", new string[] { "Neutral", "ACES" }, 1) : 1);
			this.Tonemap = (flag ? ((num8 == 0) ? 1f : 2f) : 0f);
			EffectSettings effectSettings18 = Effect("colorAdjust", s);
			bool flag2 = IsOn("colorAdjust", s);
			this.Exposure = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("exposure", 0.15f) : 0.15f) : 0f);
			this.Whites = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("whites", 0f) : 0f) : 0f);
			this.Blacks = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("blacks", 0f) : 0f) : 0f);
			this.Contrast = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("contrast", 0.08f) : 0f) : 0f);
			this.Saturation = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("saturation", 0.05f) : 0f) : 0f);
			this.Vibrance = (flag2 ? ((effectSettings18 != null) ? effectSettings18.GetFloat("vibrance", 0.15f) : 0f) : 0f);
			EffectSettings effectSettings19 = Effect("whiteBalance", s);
			bool flag3 = IsOn("whiteBalance", s);
			this.WbWarmth = (flag3 ? ((effectSettings19 != null) ? effectSettings19.GetFloat("warmth", 0.12f) : 0f) : 0f);
			this.WbTint = (flag3 ? ((effectSettings19 != null) ? effectSettings19.GetFloat("tint", 0f) : 0f) : 0f);
			EffectSettings effectSettings20 = Effect("filmLook", s);
			bool flag4 = IsOn("filmLook", s);
			int num9 = ((effectSettings20 != null) ? effectSettings20.GetEnumIndex("look", new string[] { "DPX", "Technicolor" }, 1) : 1);
			this.FilmLook = (flag4 ? ((float)num9 + 1f) : 0f);
			this.FilmStrength = (flag4 ? ((effectSettings20 != null) ? effectSettings20.GetFloat("strength", 0.6f) : 0f) : 0f);
			EffectSettings effectSettings21 = Effect("drama", s);
			this.Drama = (IsOn("drama", s) ? ((effectSettings21 != null) ? effectSettings21.GetFloat("amount", 0.25f) : 0f) : 0f);
			EffectSettings effectSettings22 = Effect("vignette", s);
			bool flag5 = IsOn("vignette", s);
			this.VignetteI = (flag5 ? ((effectSettings22 != null) ? effectSettings22.GetFloat("intensity", 0.25f) : 0f) : 0f);
			this.VignetteS = ((effectSettings22 != null) ? effectSettings22.GetFloat("smoothness", 0.6f) : 0.6f);
			EffectSettings effectSettings23 = Effect("grain", s);
			this.Grain = (IsOn("grain", s) ? ((effectSettings23 != null) ? effectSettings23.GetFloat("intensity", 0.08f) : 0f) : 0f);
			this.GrainSpeed = ((effectSettings23 != null) ? effectSettings23.GetFloat("speed", 24f) : 24f);
			EffectSettings effectSettings24 = Effect("chromatic", s);
			this.Chromatic = (IsOn("chromatic", s) ? ((effectSettings24 != null) ? effectSettings24.GetFloat("intensity", 0.3f) : 0f) : 0f);
			EffectSettings effectSettings25 = Effect("deband", s);
			this.Deband = (IsOn("deband", s) ? ((effectSettings25 != null) ? effectSettings25.GetFloat("strength", 0.5f) : 0f) : 0f);
			EffectSettings effectSettings26 = Effect("letterbox", s);
			this.Letterbox = (IsOn("letterbox", s) ? ((effectSettings26 != null) ? effectSettings26.GetFloat("ratio", 2.35f) : 0f) : 0f);
			this.UnderwaterOn = IsOn("underwater", s);
			EffectSettings effectSettings27 = Effect("underwater", s);
			this.UwDistort = ((effectSettings27 != null) ? effectSettings27.GetFloat("distortion", 0.5f) : 0.5f);
			this.UwBlur = ((effectSettings27 != null) ? effectSettings27.GetFloat("blurAmount", 0.4f) : 0.4f);
			this.UwFogDensity = ((effectSettings27 != null) ? effectSettings27.GetFloat("fogDensity", 0.5f) : 0.5f);
			this.UwCaustics = ((effectSettings27 != null) ? effectSettings27.GetFloat("caustics", 0.55f) : 0.55f);
			this.FxaaOn = IsOn("fxaa", s);
			EffectSettings effectSettings28 = Effect("fxaa", s);
			this.FxaaQuality = ((effectSettings28 != null) ? effectSettings28.GetFloat("quality", 1f) : 1f);
			EffectSettings effectSettings29 = Effect("halftone", s);
			EffectSettings effectSettings30 = Effect("cartoon", s);
			EffectSettings effectSettings31 = Effect("scanlines", s);
			EffectSettings effectSettings32 = Effect("pixelate", s);
			this.FxHalftoneAmount = ((effectSettings29 != null) ? effectSettings29.GetFloat("amount", 0.85f) : 0f);
			this.FxHalftoneScale = ((effectSettings29 != null) ? effectSettings29.GetFloat("scale", 90f) : 90f);
			this.FxHalftoneColour = ((effectSettings29 != null) ? effectSettings29.GetFloat("colour", 0f) : 0f);
			this.FxCartoonAmount = ((effectSettings30 != null) ? effectSettings30.GetFloat("amount", 0.9f) : 0f);
			this.FxCartoonSteps = ((effectSettings30 != null) ? effectSettings30.GetFloat("steps", 5f) : 5f);
			this.FxCartoonOutline = ((effectSettings30 != null) ? effectSettings30.GetFloat("outline", 0.5f) : 0.5f);
			this.FxScanAmount = ((effectSettings31 != null) ? effectSettings31.GetFloat("amount", 0.5f) : 0f);
			this.FxScanCount = ((effectSettings31 != null) ? effectSettings31.GetFloat("count", 400f) : 400f);
			this.FxScanGrille = ((effectSettings31 != null) ? effectSettings31.GetFloat("grille", 0f) : 0f);
			this.FxPixelAmount = ((effectSettings32 != null) ? effectSettings32.GetFloat("amount", 1f) : 0f);
			this.FxPixelSize = ((effectSettings32 != null) ? effectSettings32.GetFloat("size", 140f) : 140f);
			this.FxPixelLevels = ((effectSettings32 != null) ? effectSettings32.GetFloat("levels", 1f) : 1f);
			this.FxHalftoneOn = IsOn("halftone", s) && this.FxHalftoneAmount > 0.0001f;
			this.FxCartoonOn = IsOn("cartoon", s) && this.FxCartoonAmount > 0.0001f;
			this.FxScanOn = IsOn("scanlines", s) && this.FxScanAmount > 0.0001f;
			this.FxPixelOn = IsOn("pixelate", s) && this.FxPixelAmount > 0.0001f;
			this.VideoFxOn = this.FxHalftoneOn || this.FxCartoonOn || this.FxScanOn || this.FxPixelOn;
			this.CasOn = IsOn("sharpen", s);
			EffectSettings effectSettings33 = Effect("sharpen", s);
			this.CasAmount = ((effectSettings33 != null) ? effectSettings33.GetFloat("amount", 0.25f) : 0.25f);
			bool flag6 = flag || flag2 || flag3 || flag4 || IsOn("drama", s);
			bool flag7 = flag5 || IsOn("grain", s) || IsOn("chromatic", s) || IsOn("deband", s) || IsOn("letterbox", s);
			this.CompositeOn = this.AoOn || this.SsrOn || this.GiOn || this.TdOn || this.HazeOn || this.WetOn || this.StormOn || this.SunlightOn || this.PuddlesOn;
			this.UberOn = this.BloomOn || flag6 || flag7 || this.SunlightOn;
			this.CompositeFlags = new Vector4(this.AoOn ? 1f : 0f, this.SsrOn ? 1f : 0f, 0f, this.HazeOn ? 1f : 0f);
			this.CompositeFlags2 = new Vector4(this.GiOn ? 1f : 0f, this.TdOn ? 1f : 0f, 0f, this.WetOn ? 1f : 0f);
			this.UberFlags = new Vector4(this.BloomOn ? 1f : 0f, flag6 ? 1f : 0f, flag7 ? 1f : 0f, this.UnderwaterOn ? 1f : 0f);
			this.AnyEnabled = this.VideoFxOn || this.AoOn || this.SsrOn || this.GiOn || this.TdOn || this.CompositeOn || this.BloomOn || this.BlurStageOn || this.MbOn || this.UberOn || this.FxaaOn || this.CasOn || this.UnderwaterOn || this.FlareOn || this.WetOn || this.StormOn || this.PShadowContact || this.SunlightOn;
		}

		// Token: 0x040001F3 RID: 499
		public bool AoOn;

		// Token: 0x040001F4 RID: 500
		public bool SsrOn;

		// Token: 0x040001F5 RID: 501
		public bool GiOn;

		// Token: 0x040001F6 RID: 502
		public bool TdOn;

		// Token: 0x040001F7 RID: 503
		public bool HazeOn;

		// Token: 0x040001F8 RID: 504
		public bool CompositeOn;

		// Token: 0x040001F9 RID: 505
		public bool BloomOn;

		// Token: 0x040001FA RID: 506
		public bool DofOn;

		// Token: 0x040001FB RID: 507
		public bool MbOn;

		// Token: 0x040001FC RID: 508
		public bool UberOn;

		// Token: 0x040001FD RID: 509
		public bool FxaaOn;

		// Token: 0x040001FE RID: 510
		public bool CasOn;

		// Token: 0x040001FF RID: 511
		public float GiEmissive;

		// Token: 0x04000200 RID: 512
		public bool FxHalftoneOn;

		// Token: 0x04000201 RID: 513
		public bool FxCartoonOn;

		// Token: 0x04000202 RID: 514
		public bool FxScanOn;

		// Token: 0x04000203 RID: 515
		public bool FxPixelOn;

		// Token: 0x04000204 RID: 516
		public bool VideoFxOn;

		// Token: 0x04000205 RID: 517
		public float FxHalftoneAmount;

		// Token: 0x04000206 RID: 518
		public float FxHalftoneScale;

		// Token: 0x04000207 RID: 519
		public float FxHalftoneColour;

		// Token: 0x04000208 RID: 520
		public float FxCartoonAmount;

		// Token: 0x04000209 RID: 521
		public float FxCartoonSteps;

		// Token: 0x0400020A RID: 522
		public float FxCartoonOutline;

		// Token: 0x0400020B RID: 523
		public float FxScanAmount;

		// Token: 0x0400020C RID: 524
		public float FxScanCount;

		// Token: 0x0400020D RID: 525
		public float FxScanGrille;

		// Token: 0x0400020E RID: 526
		public float FxPixelAmount;

		// Token: 0x0400020F RID: 527
		public float FxPixelSize;

		// Token: 0x04000210 RID: 528
		public float FxPixelLevels;

		// Token: 0x04000211 RID: 529
		public bool UnderwaterOn;

		// Token: 0x04000212 RID: 530
		public bool AnyEnabled;

		// Token: 0x04000213 RID: 531
		public int QualityLevel;

		// Token: 0x04000214 RID: 532
		public bool PerfBalanced;

		// Token: 0x04000215 RID: 533
		public int HalfResDiv;

		// Token: 0x04000216 RID: 534
		public float AoIntensity;

		// Token: 0x04000217 RID: 535
		public float AoRadius;

		// Token: 0x04000218 RID: 536
		public float AoPower;

		// Token: 0x04000219 RID: 537
		public float AoSamples;

		// Token: 0x0400021A RID: 538
		public float SsrIntensity;

		// Token: 0x0400021B RID: 539
		public float SsrMaxDist;

		// Token: 0x0400021C RID: 540
		public float SsrSteps;

		// Token: 0x0400021D RID: 541
		public float SsrBlur;

		// Token: 0x0400021E RID: 542
		public float SsrSurfaceAware;

		// Token: 0x0400021F RID: 543
		public float SsrMetalSharp;

		// Token: 0x04000220 RID: 544
		public bool SsrRainOnly;

		// Token: 0x04000221 RID: 545
		public bool SunlightOn;

		// Token: 0x04000222 RID: 546
		public bool SunMoonOn;

		// Token: 0x04000223 RID: 547
		public float SunlightIntensity;

		// Token: 0x04000224 RID: 548
		public float SunlightReach;

		// Token: 0x04000225 RID: 549
		public float SunlightSigmaT;

		// Token: 0x04000226 RID: 550
		public float SunlightSteps;

		// Token: 0x04000227 RID: 551
		public float SunlightWarmth;

		// Token: 0x04000228 RID: 552
		public float SunlightClarity;

		// Token: 0x04000229 RID: 553
		public float SunlightRayThickness;

		// Token: 0x0400022A RID: 554
		public float SunlightSurfaceLight;

		// Token: 0x0400022B RID: 555
		public float SunlightSideGlow;

		// Token: 0x0400022C RID: 556
		public float SunlightShimmer;

		// Token: 0x0400022D RID: 557
		public float SunlightRayAngle;

		// Token: 0x0400022E RID: 558
		public float SunlightPlayerShade;

		// Token: 0x0400022F RID: 559
		public float SunlightRayRelief;

		// Token: 0x04000230 RID: 560
		public float FlareEaseRate;

		// Token: 0x04000231 RID: 561
		public float CloudShadowStrength;

		// Token: 0x04000232 RID: 562
		public float CloudShadowSoftness;

		// Token: 0x04000233 RID: 563
		public int CloudsResDiv;

		// Token: 0x04000234 RID: 564
		public float GiIntensity;

		// Token: 0x04000235 RID: 565
		public float GiRadius;

		// Token: 0x04000236 RID: 566
		public float GiRays;

		// Token: 0x04000237 RID: 567
		public float GiColorBleed;

		// Token: 0x04000238 RID: 568
		public float GiSharpness;

		// Token: 0x04000239 RID: 569
		public int GiQuality;

		// Token: 0x0400023A RID: 570
		public bool PShadowOn;

		// Token: 0x0400023B RID: 571
		public bool PShadowContact;

		// Token: 0x0400023C RID: 572
		public float PShadowIntensity;

		// Token: 0x0400023D RID: 573
		public float PShadowSoftness;

		// Token: 0x0400023E RID: 574
		public float PShadowMode;

		// Token: 0x0400023F RID: 575
		public float TdIntensity;

		// Token: 0x04000240 RID: 576
		public float TdReach;

		// Token: 0x04000241 RID: 577
		public float TdFloor;

		// Token: 0x04000242 RID: 578
		public float TdEnclosure;

		// Token: 0x04000243 RID: 579
		public float HazeDensity;

		// Token: 0x04000244 RID: 580
		public float HazeStart;

		// Token: 0x04000245 RID: 581
		public float HazeSunScatter;

		// Token: 0x04000246 RID: 582
		public float HazeHeightFalloff;

		// Token: 0x04000247 RID: 583
		public float HazeWisps;

		// Token: 0x04000248 RID: 584
		public float HazeSkyVeil;

		// Token: 0x04000249 RID: 585
		public bool PuddlesOn;

		// Token: 0x0400024A RID: 586
		public float PuddleCoverage;

		// Token: 0x0400024B RID: 587
		public float PuddleRipples;

		// Token: 0x0400024C RID: 588
		public float PuddleMirror;

		// Token: 0x0400024D RID: 589
		public float PuddleRippleSize;

		// Token: 0x0400024E RID: 590
		public float PuddleRippleSpeed;

		// Token: 0x0400024F RID: 591
		public float PuddleRippleCount;

		// Token: 0x04000250 RID: 592
		public float PuddleOpenArea;

		// Token: 0x04000251 RID: 593
		public float PuddleFogginess;

		// Token: 0x04000252 RID: 594
		public float HazeMaxBrightness;

		// Token: 0x04000253 RID: 595
		public Vector4 HazeTint;

		// Token: 0x04000254 RID: 596
		public bool FlareOn;

		// Token: 0x04000255 RID: 597
		public float FlareIntensity;

		// Token: 0x04000256 RID: 598
		public float FlareStreakLen;

		// Token: 0x04000257 RID: 599
		public float FlareMode;

		// Token: 0x04000258 RID: 600
		public float FlareStreakCount;

		// Token: 0x04000259 RID: 601
		public float FlareDispersion;

		// Token: 0x0400025A RID: 602
		public float FlareGhost;

		// Token: 0x0400025B RID: 603
		public float FlareShimmer;

		// Token: 0x0400025C RID: 604
		public bool WetOn;

		// Token: 0x0400025D RID: 605
		public bool StormOn;

		// Token: 0x0400025E RID: 606
		public float WetStrength;

		// Token: 0x0400025F RID: 607
		public float StormStrength;

		// Token: 0x04000260 RID: 608
		public float RainVisibility;

		// Token: 0x04000261 RID: 609
		public bool AdaptiveOn;

		// Token: 0x04000262 RID: 610
		public float AdaptiveStrength;

		// Token: 0x04000263 RID: 611
		public float BloomThreshold;

		// Token: 0x04000264 RID: 612
		public float BloomScatter;

		// Token: 0x04000265 RID: 613
		public float BloomIntensity;

		// Token: 0x04000266 RID: 614
		public float BloomHighlights;

		// Token: 0x04000267 RID: 615
		public Vector4 BloomTint;

		// Token: 0x04000268 RID: 616
		public float DofFocus;

		// Token: 0x04000269 RID: 617
		public float DofStrength;

		// Token: 0x0400026A RID: 618
		public float DofMaxRadius;

		// Token: 0x0400026B RID: 619
		public float DofAutoFocus;

		// Token: 0x0400026C RID: 620
		public float DofNearStrength;

		// Token: 0x0400026D RID: 621
		public float DofFocusSpeed;

		// Token: 0x0400026E RID: 622
		public float DofBokehGamma;

		// Token: 0x0400026F RID: 623
		public bool DistBlurOn;

		// Token: 0x04000270 RID: 624
		public bool BlurStageOn;

		// Token: 0x04000271 RID: 625
		public float DistBlurStart;

		// Token: 0x04000272 RID: 626
		public float DistBlurEnd;

		// Token: 0x04000273 RID: 627
		public float DistBlurMax;

		// Token: 0x04000274 RID: 628
		public float BlurMode;

		// Token: 0x04000275 RID: 629
		public float MbAmount;

		// Token: 0x04000276 RID: 630
		public float Whites;

		// Token: 0x04000277 RID: 631
		public float Blacks;

		// Token: 0x04000278 RID: 632
		public float Exposure;

		// Token: 0x04000279 RID: 633
		public float WbWarmth;

		// Token: 0x0400027A RID: 634
		public float WbTint;

		// Token: 0x0400027B RID: 635
		public float Contrast;

		// Token: 0x0400027C RID: 636
		public float Saturation;

		// Token: 0x0400027D RID: 637
		public float Vibrance;

		// Token: 0x0400027E RID: 638
		public float FilmLook;

		// Token: 0x0400027F RID: 639
		public float FilmStrength;

		// Token: 0x04000280 RID: 640
		public float Drama;

		// Token: 0x04000281 RID: 641
		public float Tonemap;

		// Token: 0x04000282 RID: 642
		public float VignetteI;

		// Token: 0x04000283 RID: 643
		public float VignetteS;

		// Token: 0x04000284 RID: 644
		public float Grain;

		// Token: 0x04000285 RID: 645
		public float GrainSpeed;

		// Token: 0x04000286 RID: 646
		public float Chromatic;

		// Token: 0x04000287 RID: 647
		public float Deband;

		// Token: 0x04000288 RID: 648
		public float Letterbox;

		// Token: 0x04000289 RID: 649
		public float UwDistort;

		// Token: 0x0400028A RID: 650
		public float UwBlur;

		// Token: 0x0400028B RID: 651
		public float UwFogDensity;

		// Token: 0x0400028C RID: 652
		public float UwCaustics;

		// Token: 0x0400028D RID: 653
		public float FxaaQuality;

		// Token: 0x0400028E RID: 654
		public float CasAmount;

		// Token: 0x0400028F RID: 655
		public Vector4 CompositeFlags;

		// Token: 0x04000290 RID: 656
		public Vector4 CompositeFlags2;

		// Token: 0x04000291 RID: 657
		public Vector4 UberFlags;
	}
}
