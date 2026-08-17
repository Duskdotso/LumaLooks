using System;
using System.Collections.Generic;

namespace LumaLooks
{
	// Token: 0x02000042 RID: 66
	internal static class Manifest
	{
		// Token: 0x06000224 RID: 548 RVA: 0x00020120 File Offset: 0x0001E320
		private static ParamDef F(string id, float min, float max, float def)
		{
			return new ParamDef
			{
				Id = id,
				Type = ParamType.Float,
				Min = min,
				Max = max,
				Def = def
			};
		}

		// Token: 0x06000225 RID: 549 RVA: 0x0002014A File Offset: 0x0001E34A
		private static ParamDef E(string id, string def, params string[] opts)
		{
			return new ParamDef
			{
				Id = id,
				Type = ParamType.Enum,
				Options = opts,
				DefStr = def
			};
		}

		// Token: 0x06000226 RID: 550 RVA: 0x0002016D File Offset: 0x0001E36D
		private static ParamDef C(string id, string def)
		{
			return new ParamDef
			{
				Id = id,
				Type = ParamType.Color,
				DefStr = def
			};
		}

		// Token: 0x06000227 RID: 551 RVA: 0x0002018C File Offset: 0x0001E38C
		public static EffectDef Get(string id)
		{
			if (Manifest._byId == null)
			{
				Manifest._byId = new Dictionary<string, EffectDef>();
				foreach (EffectDef effectDef in Manifest.Effects)
				{
					Manifest._byId[effectDef.Id] = effectDef;
				}
			}
			EffectDef effectDef2;
			if (!Manifest._byId.TryGetValue(id, out effectDef2))
			{
				return null;
			}
			return effectDef2;
		}

		// Token: 0x040004C5 RID: 1221
		public const int Port = 47800;

		// Token: 0x040004C6 RID: 1222
		public static readonly EffectDef[] Effects = new EffectDef[]
		{
			new EffectDef
			{
				Id = "fxaa",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("quality", 0f, 1f, 1f) }
			},
			new EffectDef
			{
				Id = "sharpen",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("amount", 0f, 1f, 0.25f) }
			},
			new EffectDef
			{
				Id = "dof",
				Enabled = false,
				Vr = false,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("focusDistance", 0.5f, 50f, 6f),
					Manifest.F("strength", 0f, 1f, 0.5f),
					Manifest.F("bokehRadius", 4f, 32f, 14f),
					Manifest.F("autoFocus", 0f, 1f, 1f),
					Manifest.F("focusSpeed", 0.2f, 3f, 1f),
					Manifest.F("nearStrength", 0f, 1f, 0.35f),
					Manifest.F("bokehGamma", 1f, 4f, 2.2f)
				}
			},
			new EffectDef
			{
				Id = "distanceBlur",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("startDistance", 0f, 100f, 15f),
					Manifest.F("endDistance", 5f, 200f, 60f),
					Manifest.F("maxBlur", 4f, 32f, 10f)
				}
			},
			new EffectDef
			{
				Id = "motionBlur",
				Enabled = true,
				Vr = false,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("amount", 0f, 1f, 0.2f) }
			},
			new EffectDef
			{
				Id = "sunlight",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 8f, 0.5f),
					Manifest.F("rayRelief", 0f, 0.85f, 0.7f),
					Manifest.F("reach", 5f, 500f, 150f),
					Manifest.F("density", 0f, 1f, 0.15f),
					Manifest.F("warmth", 0f, 1f, 0.65f),
					Manifest.F("clarity", 0f, 1f, 0.35f),
					Manifest.F("rayThickness", 0f, 1f, 0.5f),
					Manifest.F("rayShimmer", 0f, 1f, 0f),
					Manifest.F("rayAngle", 0f, 90f, 0f),
					Manifest.F("sideGlow", 0f, 1f, 0.5f),
					Manifest.E("quality", "Medium", new string[] { "Low", "Medium", "High" })
				}
			},
			new EffectDef
			{
				Id = "ssgi",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 1f, 0.35f),
					Manifest.F("radius", 0.5f, 8f, 3f),
					Manifest.F("colorBleed", 0f, 1f, 0.7f),
					Manifest.F("sharpness", 0f, 1f, 0.6f),
					Manifest.F("emitters", 0f, 4f, 1f),
					Manifest.E("quality", "Medium", new string[] { "Low", "Medium", "High" })
				}
			},
			new EffectDef
			{
				Id = "trueDarkness",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 1f, 0.6f),
					Manifest.F("reach", 4f, 20f, 12f),
					Manifest.F("floor", 0f, 0.5f, 0.12f),
					Manifest.F("enclosure", 0f, 1f, 0.55f)
				}
			},
			new EffectDef
			{
				Id = "playerShadow",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 1f, 0.7f),
					Manifest.F("softness", 0f, 1f, 0.4f),
					Manifest.E("mode", "Sun", new string[] { "Sun", "Contact", "Both" })
				}
			},
			new EffectDef
			{
				Id = "lumaRain",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 5f, 1f),
					Manifest.F("fallSpeed", 0.25f, 2f, 1f),
					Manifest.F("dropSize", 0.25f, 2f, 1f),
					Manifest.F("wind", 0f, 1f, 0.2f),
					Manifest.E("storm", "Off", new string[] { "Off", "On" }),
					Manifest.F("lightning", 0f, 2f, 1f),
					Manifest.F("lightningSpeed", 0.1f, 3f, 1f),
					Manifest.F("lightningRandomness", 0f, 1f, 0.5f)
				}
			},
			new EffectDef
			{
				Id = "halftone",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("amount", 0f, 1f, 0.85f),
					Manifest.F("scale", 20f, 300f, 90f),
					Manifest.F("colour", 0f, 1f, 0f)
				}
			},
			new EffectDef
			{
				Id = "cartoon",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("amount", 0f, 1f, 0.9f),
					Manifest.F("steps", 2f, 16f, 5f),
					Manifest.F("outline", 0f, 1f, 0.5f)
				}
			},
			new EffectDef
			{
				Id = "scanlines",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("amount", 0f, 1f, 0.5f),
					Manifest.F("count", 40f, 1200f, 400f),
					Manifest.F("grille", 0f, 1f, 0f)
				}
			},
			new EffectDef
			{
				Id = "pixelate",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("amount", 0f, 1f, 1f),
					Manifest.F("size", 8f, 400f, 140f),
					Manifest.F("levels", 1f, 32f, 1f)
				}
			},
			new EffectDef
			{
				Id = "birds",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 2f, 1f),
					Manifest.F("size", 0.2f, 3f, 1f),
					Manifest.F("altitude", 0.2f, 3f, 1f),
					Manifest.F("speed", 0.2f, 3f, 1f)
				}
			},
			new EffectDef
			{
				Id = "butterflies",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 2f, 1f),
					Manifest.F("size", 0.2f, 3f, 1f),
					Manifest.F("height", 0.2f, 3f, 1f),
					Manifest.F("speed", 0.2f, 3f, 1f)
				}
			},
			new EffectDef
			{
				Id = "bees",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 2f, 1f),
					Manifest.F("size", 0.2f, 3f, 1f),
					Manifest.F("height", 0.2f, 3f, 1f),
					Manifest.F("speed", 0.2f, 3f, 1f)
				}
			},
			new EffectDef
			{
				Id = "haze",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 1f, 0.15f),
					Manifest.F("startDistance", 0f, 100f, 25f),
					Manifest.F("heightFalloff", 0f, 1f, 0.25f),
					Manifest.F("sunScatter", 0f, 1f, 0.5f),
					Manifest.F("wispiness", 0f, 1f, 0.3f),
					Manifest.F("skyVeil", 0f, 1f, 0.25f),
					Manifest.F("maxBrightness", 0.05f, 1f, 0.55f),
					Manifest.C("tint", "#FFFFFF")
				}
			},
			new EffectDef
			{
				Id = "lensFlare",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.E("mode", "Both", new string[] { "Sun", "Moon", "Both" }),
					Manifest.F("intensity", 0f, 1f, 0.6f),
					Manifest.F("streakLength", 0f, 1f, 0.5f),
					Manifest.F("streakCount", 4f, 24f, 12f),
					Manifest.F("dispersion", 0f, 1f, 0.35f),
					Manifest.F("ghostStrength", 0f, 1f, 0.35f),
					Manifest.F("shimmer", 0f, 1f, 0.3f),
					Manifest.F("coverEase", 0.5f, 12f, 3f)
				}
			},
			new EffectDef
			{
				Id = "adaptive",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("strength", 0f, 1f, 0.7f),
					Manifest.F("speed", 0f, 20f, 4f)
				}
			},
			new EffectDef
			{
				Id = "sunMoon",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("sunBrightness", 0f, 8f, 4f),
					Manifest.F("sunSize", 0f, 4f, 1.2f),
					Manifest.C("sunTint", "#FFE8C0"),
					Manifest.F("moonBrightness", 0f, 8f, 0.3f),
					Manifest.F("moonSize", 0f, 4f, 0.9f),
					Manifest.C("moonTint", "#BFD4FF"),
					Manifest.F("sunIntensity", 0f, 3f, 1.15f),
					Manifest.F("sunWarmth", 0f, 1f, 0.1f),
					Manifest.F("ambientIntensity", 0f, 3f, 1.1f),
					Manifest.F("shadowStrength", 0f, 1f, 0.8f),
					Manifest.F("surfaceLight", 0f, 1f, 0.35f),
					Manifest.F("playerShade", 0f, 1f, 0.5f),
					Manifest.F("glowFalloff", 0f, 1f, 0.5f),
					Manifest.E("position", "Follow Game", new string[] { "Follow Game", "Time of Day", "Real Time" }),
					Manifest.F("timeOfDay", 0f, 24f, 10f)
				}
			},
			new EffectDef
			{
				Id = "clouds",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("coverage", 0f, 1f, 0.4f),
					Manifest.F("density", 0f, 1f, 0.5f),
					Manifest.F("speed", 0f, 1f, 0.3f),
					Manifest.F("height", 0f, 1f, 0.5f),
					Manifest.F("thickness", 0f, 1f, 0.5f),
					Manifest.F("softness", 0f, 1f, 0.5f),
					Manifest.C("tint", "#FFFFFF"),
					Manifest.F("sunGlow", 0f, 1f, 0.5f),
					Manifest.E("resolution", "Full", new string[] { "Full", "Half", "Quarter" }),
					Manifest.F("shadowStrength", 0f, 1f, 0f),
					Manifest.F("shadowSoftness", 0f, 1f, 0.5f)
				}
			},
			new EffectDef
			{
				Id = "nightSky",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("starDensity", 0f, 1f, 0.5f),
					Manifest.F("starBrightness", 0f, 1f, 0.6f),
					Manifest.F("starSize", 0f, 1f, 0.35f),
					Manifest.E("customSkies", "On", new string[] { "Off", "On" }),
					Manifest.E("auroraMode", "Night", new string[] { "Off", "Night", "Always" }),
					Manifest.F("auroraIntensity", 0f, 1f, 0.5f),
					Manifest.F("auroraSpeed", 0f, 1f, 0.5f),
					Manifest.C("auroraColorA", "#3CF08C"),
					Manifest.C("auroraColorB", "#3C9CF0"),
					Manifest.F("horizonWarmth", 0f, 1f, 0.5f),
					Manifest.E("replaceWhen", "Always", new string[] { "Always", "Night Only" }),
					Manifest.E("method", "Auto", new string[] { "Auto", "ScreenSpace", "Skybox" }),
					Manifest.F("strength", 0f, 1f, 1f),
					Manifest.F("backdropDistance", 50f, 5000f, 300f),
					Manifest.C("dayZenith", "#165BFE"),
					Manifest.C("dayHorizon", "#9CC8EE"),
					Manifest.F("daySaturation", 0f, 2f, 1.15f),
					Manifest.F("dayHue", 0f, 1f, 0.5f),
					Manifest.F("cloudCoverage", 0f, 1f, 0.4f),
					Manifest.F("cloudDensity", 0f, 1f, 0.5f),
					Manifest.F("cloudSpeed", 0f, 1f, 0.3f),
					Manifest.F("cloudHeight", 0f, 1f, 0.5f),
					Manifest.F("cloudThickness", 0f, 1f, 0.5f),
					Manifest.F("cloudSoftness", 0f, 1f, 0.5f),
					Manifest.C("cloudTint", "#FFFFFF"),
					Manifest.F("cloudSunGlow", 0f, 1f, 0.5f),
					Manifest.E("cloudResolution", "Full", new string[] { "Full", "Half", "Quarter" }),
					Manifest.F("cloudShadowStrength", 0f, 1f, 0f),
					Manifest.F("cloudShadowSoftness", 0f, 1f, 0.5f)
				}
			},
			new EffectDef
			{
				Id = "ssr",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.E("mode", "RainOnly", new string[] { "Always", "RainOnly" }),
					Manifest.F("intensity", 0f, 1f, 0.5f),
					Manifest.F("maxDistance", 5f, 100f, 40f),
					Manifest.E("quality", "Medium", new string[] { "Low", "Medium", "High" }),
					Manifest.F("blur", 0f, 1f, 0.35f),
					Manifest.F("surfaceAwareness", 0f, 1f, 0.7f),
					Manifest.F("metalSharpness", 0f, 1f, 0.6f)
				}
			},
			new EffectDef
			{
				Id = "underwater",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("distortion", 0f, 1f, 0.5f),
					Manifest.F("blurAmount", 0f, 1f, 0.4f),
					Manifest.F("fogDensity", 0f, 1f, 0.5f),
					Manifest.F("caustics", 0f, 1f, 0.55f)
				}
			},
			new EffectDef
			{
				Id = "dustMotes",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 3f, 0.4f),
					Manifest.F("size", 0f, 3f, 0.35f),
					Manifest.F("driftSpeed", 0f, 1f, 0.3f),
					Manifest.F("brightness", 0f, 1f, 0.5f),
					Manifest.E("shape", "Dot", new string[] { "Dot", "Square" })
				}
			},
			new EffectDef
			{
				Id = "fireflies",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 1f, 0.4f),
					Manifest.F("brightness", 0f, 1f, 0.6f),
					Manifest.F("wanderSpeed", 0f, 1f, 0.35f),
					Manifest.E("shape", "Dot", new string[] { "Dot", "Square" })
				}
			},
			new EffectDef
			{
				Id = "embers",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 1f, 0.5f),
					Manifest.F("riseSpeed", 0f, 1f, 0.5f),
					Manifest.F("brightness", 0f, 1f, 0.7f),
					Manifest.F("glow", 0f, 1f, 0.5f),
					Manifest.E("shape", "Dot", new string[] { "Dot", "Square" })
				}
			},
			new EffectDef
			{
				Id = "fallingLeaves",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("density", 0f, 3f, 0.5f),
					Manifest.F("fallSpeed", 0f, 1f, 0.4f),
					Manifest.F("size", 0f, 3f, 0.5f),
					Manifest.E("leafType", "Mixed", new string[] { "Oak", "Maple", "Pine Needle", "Mixed" }),
					Manifest.E("shape", "Dot", new string[] { "Dot", "Square" })
				}
			},
			new EffectDef
			{
				Id = "ssao",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 2f, 0.6f),
					Manifest.F("radius", 0.05f, 1.5f, 0.4f),
					Manifest.F("power", 0.5f, 4f, 1.5f),
					Manifest.E("quality", "Medium", new string[] { "Low", "Medium", "High" })
				}
			},
			new EffectDef
			{
				Id = "bloom",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 2f, 0.7f),
					Manifest.F("threshold", 0f, 2f, 1f),
					Manifest.F("scatter", 0.2f, 1f, 0.55f),
					Manifest.F("highlights", 0f, 1f, 0.7f),
					Manifest.C("tint", "#FFFFFF")
				}
			},
			new EffectDef
			{
				Id = "filmLook",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.E("look", "Technicolor", new string[] { "DPX", "Technicolor" }),
					Manifest.F("strength", 0f, 1f, 0.6f)
				}
			},
			new EffectDef
			{
				Id = "drama",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("amount", 0f, 1f, 0.25f) }
			},
			new EffectDef
			{
				Id = "letterbox",
				Enabled = false,
				Vr = false,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("ratio", 1.78f, 2.76f, 2.35f) }
			},
			new EffectDef
			{
				Id = "vignette",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 1f, 0.25f),
					Manifest.F("smoothness", 0.05f, 1f, 0.6f)
				}
			},
			new EffectDef
			{
				Id = "puddles",
				Enabled = false,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("coverage", 0f, 1f, 0.5f),
					Manifest.F("ripples", 0f, 1f, 0.6f),
					Manifest.F("reflectivity", 0f, 1f, 0.75f),
					Manifest.F("rippleSize", 0f, 1f, 0.2f),
					Manifest.F("rippleSpeed", 0f, 1f, 0.7f),
					Manifest.F("rippleCount", 0f, 1f, 0.85f),
					Manifest.F("openArea", 0f, 1f, 0.3f),
					Manifest.F("fogginess", 0f, 1f, 0.3f)
				}
			},
			new EffectDef
			{
				Id = "grain",
				Enabled = true,
				Vr = false,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("intensity", 0f, 1f, 0.08f),
					Manifest.F("speed", 4f, 90f, 24f)
				}
			},
			new EffectDef
			{
				Id = "chromatic",
				Enabled = false,
				Vr = false,
				Desktop = true,
				Params = new ParamDef[] { Manifest.F("intensity", 0f, 1f, 0.3f) }
			},
			new EffectDef
			{
				Id = "tonemap",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[] { Manifest.E("mode", "ACES", new string[] { "Neutral", "ACES" }) }
			},
			new EffectDef
			{
				Id = "colorAdjust",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("exposure", -2f, 2f, 0.15f),
					Manifest.F("whites", -1f, 1f, 0f),
					Manifest.F("blacks", -1f, 1f, 0f),
					Manifest.F("contrast", -1f, 1f, 0.08f),
					Manifest.F("saturation", -1f, 1f, 0.05f),
					Manifest.F("vibrance", 0f, 1f, 0.15f)
				}
			},
			new EffectDef
			{
				Id = "whiteBalance",
				Enabled = true,
				Vr = true,
				Desktop = true,
				Params = new ParamDef[]
				{
					Manifest.F("warmth", -1f, 1f, 0.12f),
					Manifest.F("tint", -1f, 1f, 0f)
				}
			}
		};

		// Token: 0x040004C7 RID: 1223
		private static Dictionary<string, EffectDef> _byId;
	}
}
