using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000044 RID: 68
	internal sealed class Settings
	{
		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600022E RID: 558 RVA: 0x0002238B File Offset: 0x0002058B
		// (set) Token: 0x0600022F RID: 559 RVA: 0x0002239C File Offset: 0x0002059C
		public bool Master
		{
			get
			{
				return this._master && LaunchGate.Allowed;
			}
			set
			{
				this._master = value;
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x06000230 RID: 560 RVA: 0x000223A5 File Offset: 0x000205A5
		public bool MasterPreference
		{
			get
			{
				return this._master;
			}
		}

		// Token: 0x06000231 RID: 561 RVA: 0x000223AD File Offset: 0x000205AD
		public int QualityFor(bool vrTarget)
		{
			if (!vrTarget)
			{
				return this.QualityDesktop;
			}
			return this.QualityVr;
		}

		// Token: 0x06000232 RID: 562 RVA: 0x000223C0 File Offset: 0x000205C0
		public EffectSettings Effect(string id)
		{
			EffectSettings effectSettings;
			if (!this.Effects.TryGetValue(id, out effectSettings) || effectSettings == null)
			{
				return null;
			}
			for (int i = 0; i < Settings.WithheldEffects.Length; i++)
			{
				if (string.Equals(id, Settings.WithheldEffects[i], StringComparison.Ordinal))
				{
					effectSettings.Enabled = false;
					return effectSettings;
				}
			}
			return effectSettings;
		}

		// Token: 0x06000233 RID: 563 RVA: 0x00022410 File Offset: 0x00020610
		public bool IsOn(string id, bool vrTarget)
		{
			if (!this.Master)
			{
				return false;
			}
			if (vrTarget && !this.VrAll)
			{
				return false;
			}
			EffectSettings effectSettings = this.Effect(id);
			if (effectSettings == null || !effectSettings.Enabled)
			{
				return false;
			}
			if (!vrTarget)
			{
				return effectSettings.Desktop;
			}
			return effectSettings.Vr;
		}

		// Token: 0x06000234 RID: 564 RVA: 0x0002245C File Offset: 0x0002065C
		public static Settings BuildDefaults()
		{
			Settings settings = new Settings
			{
				Master = true
			};
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				EffectSettings effectSettings = new EffectSettings
				{
					Enabled = effectDef.Enabled,
					Vr = effectDef.Vr,
					Desktop = effectDef.Desktop
				};
				foreach (ParamDef paramDef in effectDef.Params)
				{
					switch (paramDef.Type)
					{
					case ParamType.Float:
						effectSettings.Pars[paramDef.Id] = paramDef.Def;
						break;
					case ParamType.Enum:
						effectSettings.Pars[paramDef.Id] = paramDef.DefStr;
						break;
					case ParamType.Color:
						effectSettings.Pars[paramDef.Id] = paramDef.DefStr;
						break;
					case ParamType.Bool:
						effectSettings.Pars[paramDef.Id] = false;
						break;
					}
				}
				settings.Effects[effectDef.Id] = effectSettings;
			}
			return settings;
		}

		// Token: 0x06000235 RID: 565 RVA: 0x0002258F File Offset: 0x0002078F
		private static int ReadLevel(JToken tok, int fallback)
		{
			if (tok == null)
			{
				return QualityTiers.Clamp(fallback);
			}
			if (tok.Type == JTokenType.String)
			{
				return QualityTiers.Parse(Extensions.Value<string>(tok), fallback);
			}
			if (tok.Type == JTokenType.Integer)
			{
				return QualityTiers.Clamp(Extensions.Value<int>(tok));
			}
			return QualityTiers.Clamp(fallback);
		}

		// Token: 0x06000236 RID: 566 RVA: 0x000225CC File Offset: 0x000207CC
		public static Settings Parse(string json, bool migrateDefaults = false)
		{
			Settings settings = Settings.BuildDefaults();
			if (string.IsNullOrEmpty(json))
			{
				return settings;
			}
			JObject jobject;
			try
			{
				jobject = JObject.Parse(json);
			}
			catch
			{
				return settings;
			}
			JToken jtoken = jobject["master"];
			if (jtoken != null && jtoken.Type == JTokenType.Boolean)
			{
				settings.Master = Extensions.Value<bool>(jtoken);
			}
			JToken jtoken2 = jobject["vrAll"];
			if (jtoken2 != null && jtoken2.Type == JTokenType.Boolean)
			{
				settings.VrAll = Extensions.Value<bool>(jtoken2);
			}
			settings.QualityVr = Settings.ReadLevel(jobject["qualityVr"], 1);
			settings.QualityDesktop = Settings.ReadLevel(jobject["qualityDesktop"], 3);
			JObject jobject2 = jobject["effects"] as JObject;
			if (jobject2 == null)
			{
				return settings;
			}
			int num = 0;
			try
			{
				JToken jtoken3 = jobject["configVersion"];
				if (jtoken3 != null && jtoken3.Type == JTokenType.Integer)
				{
					num = Extensions.Value<int>(jtoken3);
				}
			}
			catch
			{
				num = 0;
			}
			if (migrateDefaults && num < 10)
			{
				for (int i = num + 1; i <= 10; i++)
				{
					string[] array;
					if (Settings.DefaultOnMigrations.TryGetValue(i, out array))
					{
						foreach (string text in array)
						{
							try
							{
								EffectDef effectDef = Manifest.Get(text);
								if (effectDef != null && effectDef.Enabled)
								{
									JObject jobject3 = jobject2[text] as JObject;
									if (jobject3 != null)
									{
										JToken jtoken4 = jobject3["enabled"];
										if (jtoken4 != null && jtoken4.Type == JTokenType.Boolean && !Extensions.Value<bool>(jtoken4))
										{
											jobject3["enabled"] = true;
										}
									}
								}
							}
							catch
							{
							}
						}
					}
				}
				if (num < 9 && jobject["qualityVr"] == null)
				{
					try
					{
						JObject jobject4 = jobject2["performance"] as JObject;
						if (jobject4 != null)
						{
							JToken jtoken5 = jobject4["enabled"];
							bool flag = jtoken5 == null || jtoken5.Type != JTokenType.Boolean || Extensions.Value<bool>(jtoken5);
							JObject jobject5 = jobject4["params"] as JObject;
							string text2;
							if (jobject5 == null)
							{
								text2 = null;
							}
							else
							{
								JToken jtoken6 = jobject5["vrMode"];
								text2 = ((jtoken6 != null) ? Extensions.Value<string>(jtoken6) : null);
							}
							bool flag2 = !string.Equals(text2, "Quality", StringComparison.OrdinalIgnoreCase);
							settings.QualityVr = ((flag && flag2) ? 1 : 3);
						}
					}
					catch
					{
					}
				}
				if (num < 10)
				{
					try
					{
						JObject jobject6 = jobject2["clouds"] as JObject;
						if (jobject6 != null)
						{
							JToken jtoken7 = jobject6["enabled"];
							if (jtoken7 != null && jtoken7.Type == JTokenType.Boolean)
							{
								JObject jobject7 = jobject2["nightSky"] as JObject;
								if (jobject7 == null)
								{
									jobject7 = new JObject();
									jobject2["nightSky"] = jobject7;
								}
								JObject jobject8 = jobject7["params"] as JObject;
								if (jobject8 == null)
								{
									jobject8 = new JObject();
									jobject7["params"] = jobject8;
								}
								if (jobject8["clouds"] == null)
								{
									jobject8["clouds"] = (Extensions.Value<bool>(jtoken7) ? "On" : "Off");
								}
							}
						}
					}
					catch
					{
					}
				}
				if (num < 5)
				{
					try
					{
						JObject jobject9 = jobject2["sunMoon"] as JObject;
						if (jobject9 != null)
						{
							JObject jobject10 = jobject9["params"] as JObject;
							if (jobject10 != null)
							{
								JToken jtoken8 = jobject10["position"];
								if (jtoken8 != null && jtoken8.Type == JTokenType.String && string.Equals(Extensions.Value<string>(jtoken8), "Real Time", StringComparison.Ordinal))
								{
									jobject10["position"] = "Follow Game";
								}
							}
						}
					}
					catch
					{
					}
				}
				if (num < 7)
				{
					try
					{
						JObject jobject11 = jobject2["sunMoon"] as JObject;
						if (jobject11 != null)
						{
							JObject jobject12 = jobject11["params"] as JObject;
							if (jobject12 != null && jobject12["shadowStrength"] != null && Math.Abs(Extensions.Value<float>(jobject12["shadowStrength"]) - 1f) < 0.0001f)
							{
								jobject12["shadowStrength"] = 0.8f;
							}
						}
					}
					catch
					{
					}
				}
				if (num < 8)
				{
					try
					{
						JObject jobject13 = jobject2["sunlight"] as JObject;
						if (jobject13 != null)
						{
							JObject jobject14 = jobject13["params"] as JObject;
							if (jobject14 != null)
							{
								JObject jobject15 = jobject2["sunMoon"] as JObject;
								if (jobject15 == null)
								{
									jobject15 = new JObject();
									jobject2["sunMoon"] = jobject15;
								}
								JObject jobject16 = jobject15["params"] as JObject;
								if (jobject16 == null)
								{
									jobject16 = new JObject();
									jobject15["params"] = jobject16;
								}
								foreach (string text3 in new string[] { "surfaceLight", "playerShade" })
								{
									if (jobject14[text3] != null && jobject16[text3] == null)
									{
										jobject16[text3] = jobject14[text3];
									}
								}
							}
						}
					}
					catch
					{
					}
				}
				List<string[][]> list = new List<string[][]>();
				if (num < 2)
				{
					list.Add(Settings.SkySplitCarry);
				}
				if (num < 3)
				{
					list.Add(Settings.WeatherSplitCarry);
				}
				if (num < 6)
				{
					list.Add(Settings.WorldLightMergeCarry);
				}
				if (num < 10)
				{
					list.Add(Settings.CloudFoldCarry);
				}
				foreach (string[][] array3 in list)
				{
					foreach (string[] array4 in array3)
					{
						try
						{
							JObject jobject17 = jobject2[array4[0]] as JObject;
							JToken jtoken9;
							if (jobject17 == null)
							{
								jtoken9 = null;
							}
							else
							{
								JToken jtoken10 = jobject17["params"];
								jtoken9 = ((jtoken10 != null) ? jtoken10[array4[1]] : null);
							}
							JToken jtoken11 = jtoken9;
							if (jtoken11 != null && jtoken11.Type != JTokenType.Undefined)
							{
								JObject jobject18 = jobject2[array4[2]] as JObject;
								if (jobject18 == null)
								{
									jobject18 = new JObject();
									jobject2[array4[2]] = jobject18;
								}
								JObject jobject19 = jobject18["params"] as JObject;
								if (jobject19 == null)
								{
									jobject19 = new JObject();
									jobject18["params"] = jobject19;
								}
								if (jobject19[array4[3]] == null)
								{
									jobject19[array4[3]] = jtoken11.DeepClone();
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			try
			{
				JToken jtoken12 = jobject2["tonemap"];
				JToken jtoken13;
				if (jtoken12 == null)
				{
					jtoken13 = null;
				}
				else
				{
					JToken jtoken14 = jtoken12["params"];
					jtoken13 = ((jtoken14 != null) ? jtoken14["exposure"] : null);
				}
				JToken jtoken15 = jtoken13;
				JToken jtoken16 = jobject2["colorAdjust"];
				JToken jtoken17;
				if (jtoken16 == null)
				{
					jtoken17 = null;
				}
				else
				{
					JToken jtoken18 = jtoken16["params"];
					jtoken17 = ((jtoken18 != null) ? jtoken18["exposure"] : null);
				}
				JToken jtoken19 = jtoken17;
				if (jtoken15 != null && jtoken19 == null && (jtoken15.Type == JTokenType.Float || jtoken15.Type == JTokenType.Integer))
				{
					JObject jobject20 = jobject2["colorAdjust"] as JObject;
					if (jobject20 == null)
					{
						jobject20 = new JObject();
						jobject2["colorAdjust"] = jobject20;
					}
					JObject jobject21 = jobject20["params"] as JObject;
					if (jobject21 == null)
					{
						jobject21 = new JObject();
						jobject20["params"] = jobject21;
					}
					jobject21["exposure"] = Extensions.Value<float>(jtoken15);
				}
			}
			catch
			{
			}
			foreach (KeyValuePair<string, JToken> keyValuePair in jobject2)
			{
				EffectDef effectDef2 = Manifest.Get(keyValuePair.Key);
				EffectSettings effectSettings;
				if (effectDef2 != null && settings.Effects.TryGetValue(keyValuePair.Key, out effectSettings))
				{
					JObject jobject22 = keyValuePair.Value as JObject;
					if (jobject22 != null)
					{
						JToken jtoken20 = jobject22["enabled"];
						if (jtoken20 != null && jtoken20.Type == JTokenType.Boolean)
						{
							effectSettings.Enabled = Extensions.Value<bool>(jtoken20);
						}
						JToken jtoken21 = jobject22["vr"];
						if (jtoken21 != null && jtoken21.Type == JTokenType.Boolean)
						{
							effectSettings.Vr = Extensions.Value<bool>(jtoken21);
						}
						JToken jtoken22 = jobject22["desktop"];
						if (jtoken22 != null && jtoken22.Type == JTokenType.Boolean)
						{
							effectSettings.Desktop = Extensions.Value<bool>(jtoken22);
						}
						JObject jobject23 = jobject22["params"] as JObject;
						if (jobject23 != null)
						{
							foreach (ParamDef paramDef in effectDef2.Params)
							{
								JToken jtoken23 = jobject23[paramDef.Id];
								if (jtoken23 != null)
								{
									try
									{
										switch (paramDef.Type)
										{
										case ParamType.Float:
											effectSettings.Pars[paramDef.Id] = Mathf.Clamp((float)Extensions.Value<double>(jtoken23), paramDef.Min, paramDef.Max);
											break;
										case ParamType.Enum:
										{
											string text4 = Extensions.Value<string>(jtoken23);
											bool flag3 = false;
											if (text4 != null)
											{
												foreach (string text5 in paramDef.Options)
												{
													if (string.Equals(text5, text4, StringComparison.OrdinalIgnoreCase))
													{
														effectSettings.Pars[paramDef.Id] = text5;
														flag3 = true;
														break;
													}
												}
											}
											if (!flag3)
											{
												effectSettings.Pars[paramDef.Id] = paramDef.DefStr;
											}
											break;
										}
										case ParamType.Color:
										{
											string text6 = Extensions.Value<string>(jtoken23);
											Color color = default;
											effectSettings.Pars[paramDef.Id] = ((text6 != null && ColorUtility.TryParseHtmlString(text6, out color)) ? text6 : paramDef.DefStr);
											break;
										}
										case ParamType.Bool:
											effectSettings.Pars[paramDef.Id] = jtoken23.Type == JTokenType.Boolean && Extensions.Value<bool>(jtoken23);
											break;
										}
									}
									catch
									{
									}
								}
							}
						}
					}
				}
			}
			return settings;
		}

		// Token: 0x06000237 RID: 567 RVA: 0x00023120 File Offset: 0x00021320
		public string ToJson()
		{
			JObject jobject = new JObject();
			jobject["configVersion"] = 10;
			jobject["master"] = this.MasterPreference;
			jobject["vrAll"] = this.VrAll;
			jobject["qualityVr"] = QualityTiers.Name(this.QualityVr);
			jobject["qualityDesktop"] = QualityTiers.Name(this.QualityDesktop);
			JObject jobject2 = jobject;
			JObject jobject3 = new JObject();
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				EffectSettings effectSettings;
				if (this.Effects.TryGetValue(effectDef.Id, out effectSettings))
				{
					JObject jobject4 = new JObject();
					foreach (ParamDef paramDef in effectDef.Params)
					{
						object obj;
						if (effectSettings.Pars.TryGetValue(paramDef.Id, out obj) && obj != null)
						{
							if (paramDef.Type == ParamType.Float)
							{
								jobject4[paramDef.Id] = Convert.ToSingle(obj, CultureInfo.InvariantCulture);
							}
							else
							{
								jobject4[paramDef.Id] = obj.ToString();
							}
						}
					}
					JObject jobject5 = jobject3;
					string id = effectDef.Id;
					JObject jobject6 = new JObject();
					jobject6["enabled"] = effectSettings.Enabled;
					jobject6["vr"] = effectSettings.Vr;
					jobject6["desktop"] = effectSettings.Desktop;
					jobject6["params"] = jobject4;
					jobject5[id] = jobject6;
				}
			}
			jobject2["effects"] = jobject3;
			return jobject2.ToString(0, Array.Empty<JsonConverter>());
		}

		// Token: 0x040004CC RID: 1228
		private bool _master = true;

		// Token: 0x040004CD RID: 1229
		public const int CurrentConfigVersion = 10;

		// Token: 0x040004CE RID: 1230
		private static readonly Dictionary<int, string[]> DefaultOnMigrations = new Dictionary<int, string[]>
		{
			{
				1,
				new string[] { "sky", "sunlight", "dynamicLights" }
			},
			{
				2,
				new string[] { "sunMoon", "clouds", "nightSky" }
			},
			{
				3,
				new string[] { "rain", "rainSplash", "wetSurfaces", "stormMood" }
			}
		};

		// Token: 0x040004CF RID: 1231
		private static readonly string[][] WorldLightMergeCarry = new string[][]
		{
			new string[] { "worldLight", "sunIntensity", "sunMoon", "sunIntensity" },
			new string[] { "worldLight", "sunWarmth", "sunMoon", "sunWarmth" },
			new string[] { "worldLight", "ambientIntensity", "sunMoon", "ambientIntensity" },
			new string[] { "worldLight", "shadowStrength", "sunMoon", "shadowStrength" }
		};

		// Token: 0x040004D0 RID: 1232
		private static readonly string[][] WeatherSplitCarry = new string[][]
		{
			new string[] { "weather", "rainVisibility", "rain", "amount" },
			new string[] { "weather", "wind", "rain", "wind" },
			new string[] { "weather", "splashes", "rainSplash", "amount" },
			new string[] { "weather", "storminess", "stormMood", "strength" }
		};

		// Token: 0x040004D1 RID: 1233
		private static readonly string[][] CloudFoldCarry = new string[][]
		{
			new string[] { "clouds", "coverage", "nightSky", "cloudCoverage" },
			new string[] { "clouds", "density", "nightSky", "cloudDensity" },
			new string[] { "clouds", "speed", "nightSky", "cloudSpeed" },
			new string[] { "clouds", "height", "nightSky", "cloudHeight" },
			new string[] { "clouds", "thickness", "nightSky", "cloudThickness" },
			new string[] { "clouds", "softness", "nightSky", "cloudSoftness" },
			new string[] { "clouds", "tint", "nightSky", "cloudTint" },
			new string[] { "clouds", "sunGlow", "nightSky", "cloudSunGlow" },
			new string[] { "clouds", "resolution", "nightSky", "cloudResolution" },
			new string[] { "clouds", "shadowStrength", "nightSky", "cloudShadowStrength" },
			new string[] { "clouds", "shadowSoftness", "nightSky", "cloudShadowSoftness" }
		};

		// Token: 0x040004D2 RID: 1234
		private static readonly string[][] SkySplitCarry = new string[][]
		{
			new string[] { "sky", "sunBrightness", "sunMoon", "sunBrightness" },
			new string[] { "sky", "sunDiscSize", "sunMoon", "sunSize" },
			new string[] { "sky", "sunPosition", "sunMoon", "position" },
			new string[] { "sky", "timeOfDay", "sunMoon", "timeOfDay" },
			new string[] { "sky", "starDensity", "nightSky", "starDensity" },
			new string[] { "sky", "starBrightness", "nightSky", "starBrightness" },
			new string[] { "sky", "starSize", "nightSky", "starSize" },
			new string[] { "sky", "auroraMode", "nightSky", "auroraMode" },
			new string[] { "sky", "auroraIntensity", "nightSky", "auroraIntensity" },
			new string[] { "sky", "auroraSpeed", "nightSky", "auroraSpeed" },
			new string[] { "sky", "auroraColorA", "nightSky", "auroraColorA" },
			new string[] { "sky", "auroraColorB", "nightSky", "auroraColorB" },
			new string[] { "sky", "horizonWarmth", "nightSky", "horizonWarmth" },
			new string[] { "sky", "mode", "nightSky", "method" },
			new string[] { "sky", "strength", "nightSky", "strength" },
			new string[] { "sky", "backdropDistance", "nightSky", "backdropDistance" }
		};

		// Token: 0x040004D3 RID: 1235
		public bool VrAll = true;

		// Token: 0x040004D4 RID: 1236
		public int QualityVr = 1;

		// Token: 0x040004D5 RID: 1237
		public int QualityDesktop = 3;

		// Token: 0x040004D6 RID: 1238
		public readonly Dictionary<string, EffectSettings> Effects = new Dictionary<string, EffectSettings>();

		// Token: 0x040004D7 RID: 1239
		private static readonly string[] WithheldEffects = new string[] { "playerShadow", "waves", "dynamicLights", "deband" };
	}
}
