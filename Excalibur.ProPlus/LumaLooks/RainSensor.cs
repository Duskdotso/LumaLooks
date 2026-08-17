using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000029 RID: 41
	internal sealed class RainSensor
	{
		// Token: 0x17000029 RID: 41
		// (get) Token: 0x0600018C RID: 396 RVA: 0x00016FB5 File Offset: 0x000151B5
		// (set) Token: 0x0600018D RID: 397 RVA: 0x00016FBC File Offset: 0x000151BC
		public static float RainFactor { get; private set; }

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x0600018E RID: 398 RVA: 0x00016FC4 File Offset: 0x000151C4
		// (set) Token: 0x0600018F RID: 399 RVA: 0x00016FCB File Offset: 0x000151CB
		public static float StormFactor { get; private set; }

		// Token: 0x06000190 RID: 400 RVA: 0x00016FD3 File Offset: 0x000151D3
		public static void SetStorm(float v)
		{
			RainSensor.StormFactor = Mathf.Clamp01(v);
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000191 RID: 401 RVA: 0x00016FE0 File Offset: 0x000151E0
		// (set) Token: 0x06000192 RID: 402 RVA: 0x00016FE7 File Offset: 0x000151E7
		public static float WetBuildup { get; private set; }

		// Token: 0x06000193 RID: 403 RVA: 0x00016FF0 File Offset: 0x000151F0
		public RainSensor(ManualLogSource log)
		{
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00017080 File Offset: 0x00015280
		public void Configure(bool enabled, bool dimEnabled, bool dimVrAllowed, bool dimDesktopAllowed, float gameRainOpacity)
		{
			if (enabled && !this._enabled)
			{
				this._nextScanAt = 0f;
				this._emptyScans = 0;
			}
			this._enabled = enabled;
			gameRainOpacity = Mathf.Clamp01(gameRainOpacity);
			if (dimEnabled != this._dimWanted || dimVrAllowed != this._dimVrAllowed || dimDesktopAllowed != this._dimDesktopAllowed || !Mathf.Approximately(gameRainOpacity, this._gameRainOpacity))
			{
				this._dimsDirty = true;
			}
			this._dimWanted = dimEnabled;
			this._dimVrAllowed = dimVrAllowed;
			this._dimDesktopAllowed = dimDesktopAllowed;
			this._gameRainOpacity = gameRainOpacity;
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0001710C File Offset: 0x0001530C
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._scanning = false;
			this._stack.Clear();
			this._sceneJustLoaded = true;
			this._emptyScans = 0;
			this._factor = 0f;
			this._buildup = 0f;
			this._dimsDirty = true;
			this._sceneGen++;
		}

		// Token: 0x06000196 RID: 406 RVA: 0x00017164 File Offset: 0x00015364
		public void Tick()
		{
			try
			{
				float unscaledDeltaTime = Time.unscaledDeltaTime;
				bool flag = false;
				try
				{
					flag = XRSettings.isDeviceActive;
				}
				catch
				{
				}
				bool flag2 = this._dimWanted && (flag ? this._dimVrAllowed : this._dimDesktopAllowed);
				if (flag2 != this._dimOn)
				{
					this._dimOn = flag2;
					this._dimsDirty = true;
				}
				bool flag3 = false;
				if (this._enabled)
				{
					float realtimeSinceStartup = Time.realtimeSinceStartup;
					if (this._sceneJustLoaded)
					{
						this._sceneJustLoaded = false;
						this._nextScanAt = realtimeSinceStartup + 2f;
					}
					if (this._scanning)
					{
						this.StepScan();
					}
					else if (realtimeSinceStartup >= this._nextScanAt)
					{
						this.BeginScan();
					}
					flag3 = this.AnyActiveRain();
				}
				if (this._dimsDirty)
				{
					this._dimsDirty = false;
					this.ApplyDims();
				}
				this._factor = Mathf.MoveTowards(this._factor, flag3 ? 1f : 0f, unscaledDeltaTime / 3f);
				if (this._factor > 0.001f)
				{
					this._buildup = Mathf.Min(1f, this._buildup + unscaledDeltaTime * this._factor / 35f);
				}
				else
				{
					this._buildup = Mathf.Max(0f, this._buildup - unscaledDeltaTime / 60f);
				}
				RainSensor.RainFactor = this._factor;
				RainSensor.WetBuildup = this._buildup;
				Shader.SetGlobalFloat(ShaderIds.RainFactorGlobal, this._factor);
				Shader.SetGlobalFloat(ShaderIds.WetBuildupGlobal, this._buildup);
				Shader.SetGlobalFloat(ShaderIds.CamCoveredGlobal, 0f);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("RainSensor tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000197 RID: 407 RVA: 0x00017338 File Offset: 0x00015538
		private bool AnyActiveRain()
		{
			bool flag = false;
			for (int i = this._tracked.Count - 1; i >= 0; i--)
			{
				ParticleSystem particleSystem = this._tracked[i];
				if (particleSystem == null)
				{
					this._tracked.RemoveAt(i);
				}
				else if (!flag)
				{
					try
					{
						flag = particleSystem.emission.enabled && particleSystem.isEmitting;
					}
					catch
					{
					}
				}
			}
			return flag;
		}

		// Token: 0x06000198 RID: 408 RVA: 0x000173B8 File Offset: 0x000155B8
		private void BeginScan()
		{
			this._stack.Clear();
			this._examined = 0;
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene sceneAt = SceneManager.GetSceneAt(i);
				if (sceneAt.isLoaded)
				{
					sceneAt.GetRootGameObjects(this._rootsScratch);
					for (int j = 0; j < this._rootsScratch.Count; j++)
					{
						GameObject gameObject = this._rootsScratch[j];
						if (gameObject != null && gameObject.activeInHierarchy)
						{
							this._stack.Push(gameObject.transform);
						}
					}
				}
			}
			this._rootsScratch.Clear();
			this._scanning = true;
			if (this._stack.Count == 0)
			{
				this.FinishScan();
			}
		}

		// Token: 0x06000199 RID: 409 RVA: 0x00017470 File Offset: 0x00015670
		private void StepScan()
		{
			int num = 40;
			while (num-- > 0 && this._stack.Count > 0)
			{
				Transform transform = this._stack.Pop();
				if (!(transform == null))
				{
					this._examined++;
					this.Examine(transform);
					int childCount = transform.childCount;
					for (int i = 0; i < childCount; i++)
					{
						Transform child = transform.GetChild(i);
						if (child.gameObject.activeSelf)
						{
							this._stack.Push(child);
						}
					}
				}
			}
			if (this._stack.Count == 0 || this._examined >= 100000)
			{
				this.FinishScan();
			}
		}

		// Token: 0x0600019A RID: 410 RVA: 0x0001751C File Offset: 0x0001571C
		private void Examine(Transform t)
		{
			if (this._tracked.Count >= 32)
			{
				return;
			}
			if (t.name.StartsWith("LumaLooks", StringComparison.Ordinal))
			{
				return;
			}
			ParticleSystem particleSystem = null;
			if (!t.TryGetComponent<ParticleSystem>(out particleSystem) || particleSystem == null)
			{
				return;
			}
			if (!RainSensor.NameMatches(t.name))
			{
				return;
			}
			for (int i = 0; i < this._tracked.Count; i++)
			{
				if (this._tracked[i] == particleSystem)
				{
					return;
				}
			}
			this._tracked.Add(particleSystem);
		}

		// Token: 0x0600019B RID: 411 RVA: 0x000175A4 File Offset: 0x000157A4
		private static bool NameMatches(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			for (int i = 0; i < RainSensor.NameRejects.Length; i++)
			{
				if (name.IndexOf(RainSensor.NameRejects[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return false;
				}
			}
			for (int j = 0; j < RainSensor.NameKeywords.Length; j++)
			{
				string text = RainSensor.NameKeywords[j];
				int num = 0;
				int num2;
				while ((num2 = name.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)) >= 0)
				{
					if (RainSensor.IsTokenStart(name, num2) && RainSensor.IsTokenEnd(name, num2 + text.Length))
					{
						return true;
					}
					num = num2 + 1;
				}
			}
			return false;
		}

		// Token: 0x0600019C RID: 412 RVA: 0x00017630 File Offset: 0x00015830
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0001766C File Offset: 0x0001586C
		private static bool IsTokenEnd(string s, int end)
		{
			if (end >= s.Length)
			{
				return true;
			}
			char c = s[end];
			return !char.IsLetter(c) || char.IsUpper(c);
		}

		// Token: 0x0600019E RID: 414 RVA: 0x0001769C File Offset: 0x0001589C
		private void FinishScan()
		{
			this._scanning = false;
			this._stack.Clear();
			this._emptyScans = ((this._tracked.Count == 0) ? (this._emptyScans + 1) : 0);
			this._nextScanAt = Time.realtimeSinceStartup + ((this._emptyScans >= 3) ? 30f : 10f) * PerfMode.ScanMul;
			if (this._tracked.Count != this._lastLoggedFound)
			{
				this._lastLoggedFound = this._tracked.Count;
				this._log.LogInfo(string.Format("RainSensor: {0} rain particle system(s) tracked ", this._tracked.Count) + string.Format("({0} transforms examined).", this._examined));
			}
			this._dimsDirty = true;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x0001776C File Offset: 0x0001596C
		private void ApplyDims()
		{
			bool flag = this._dimOn && this._gameRainOpacity < 0.999f;
			for (int i = this._dims.Count - 1; i >= 0; i--)
			{
				RainSensor.Dim dim = this._dims[i];
				if (dim.Ps == null || (dim.Lever != RainSensor.DimLever.StartColor && dim.Renderer == null))
				{
					if (dim.Instance != null)
					{
						UnityEngine.Object.Destroy(dim.Instance);
					}
					this._dims.RemoveAt(i);
				}
				else if (!flag)
				{
					this.RestoreDim(in dim);
					this._dims.RemoveAt(i);
				}
				else
				{
					this.ApplyDimValue(ref dim);
					this._dims[i] = dim;
				}
			}
			if (!flag)
			{
				if (this._summaryCount != 0)
				{
					this._summaryCount = 0;
					this._summaryGen = this._sceneGen;
				}
				return;
			}
			for (int j = 0; j < this._tracked.Count; j++)
			{
				ParticleSystem particleSystem = this._tracked[j];
				if (!(particleSystem == null))
				{
					bool flag2 = false;
					for (int k = 0; k < this._dims.Count; k++)
					{
						if (this._dims[k].Ps == particleSystem)
						{
							flag2 = true;
							break;
						}
					}
					if (!flag2)
					{
						ParticleSystemRenderer particleSystemRenderer = null;
						particleSystem.TryGetComponent<ParticleSystemRenderer>(out particleSystemRenderer);
						RainSensor.Dim dim2 = new RainSensor.Dim
						{
							Ps = particleSystem,
							Renderer = particleSystemRenderer,
							Lever = RainSensor.DimLever.None,
							LoggedGen = -1
						};
						bool flag3 = this._gameRainOpacity <= 0.02f;
						Material material = ((!flag3 && particleSystemRenderer != null) ? particleSystemRenderer.sharedMaterial : null);
						if (material != null)
						{
							int num = 0;
							if (material.HasProperty(ShaderIds.GameTintColor))
							{
								num = ShaderIds.GameTintColor;
							}
							else if (material.HasProperty(ShaderIds.GameColor))
							{
								num = ShaderIds.GameColor;
							}
							else if (material.HasProperty(ShaderIds.GameBaseColor))
							{
								num = ShaderIds.GameBaseColor;
							}
							if (num != 0)
							{
								try
								{
									Material material2 = new Material(material)
									{
										hideFlags = (HideFlags)61
									};
									particleSystemRenderer.sharedMaterial = material2;
									dim2.Original = material;
									dim2.Instance = material2;
									dim2.ColorId = num;
									dim2.Lever = RainSensor.DimLever.MaterialColor;
								}
								catch (Exception ex)
								{
									this._log.LogWarning("RainSensor: could not instance '" + material.name + "': " + ex.Message);
								}
							}
						}
						if (!flag3 && dim2.Lever == RainSensor.DimLever.None)
						{
							try
							{
								ParticleSystem.MinMaxGradient startColor = particleSystem.main.startColor;
								if (startColor.mode == null)
								{
									dim2.StartMode = startColor.mode;
									dim2.StartMin = (dim2.StartMax = startColor.color);
									dim2.Lever = RainSensor.DimLever.StartColor;
								}
								else if (startColor.mode == ParticleSystemGradientMode.TwoColors)
								{
									dim2.StartMode = startColor.mode;
									dim2.StartMin = startColor.colorMin;
									dim2.StartMax = startColor.colorMax;
									dim2.Lever = RainSensor.DimLever.StartColor;
								}
							}
							catch
							{
							}
						}
						if (dim2.Lever == RainSensor.DimLever.None && particleSystemRenderer != null)
						{
							dim2.Lever = RainSensor.DimLever.RendererEnable;
							try
							{
								dim2.RendererWasEnabled = particleSystemRenderer.enabled;
							}
							catch
							{
								dim2.RendererWasEnabled = true;
							}
						}
						if (dim2.Lever != RainSensor.DimLever.None)
						{
							this.ApplyDimValue(ref dim2);
							this._dims.Add(dim2);
						}
					}
				}
			}
			if (this._summaryGen != this._sceneGen || this._summaryCount != this._dims.Count)
			{
				this._summaryGen = this._sceneGen;
				this._summaryCount = this._dims.Count;
				this._log.LogInfo(string.Format("RainSensor game-rain dim: opacity {0:0.00} applied to ", this._gameRainOpacity) + string.Format("{0} of {1} tracked rain system(s).", this._dims.Count, this._tracked.Count));
			}
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00017BA4 File Offset: 0x00015DA4
		private void ApplyDimValue(ref RainSensor.Dim d)
		{
			float gameRainOpacity = this._gameRainOpacity;
			string text = null;
			try
			{
				switch (d.Lever)
				{
				case RainSensor.DimLever.MaterialColor:
					if (!(d.Original == null) && !(d.Instance == null) && (!(d.Renderer != null) || d.Renderer.sharedMaterial == d.Instance))
					{
						Color color = d.Original.GetColor(d.ColorId);
						Color color2 = RainSensor.ScaleForDim(color, gameRainOpacity);
						d.Instance.SetColor(d.ColorId, color2);
						if (d.LoggedGen != this._sceneGen)
						{
							text = string.Concat(new string[]
							{
								"material '",
								d.Original.name,
								"' (shader '",
								(d.Original.shader != null) ? d.Original.shader.name : "?",
								"') ",
								string.Format("property #{0} rgba({1:0.00},{2:0.00},{3:0.00},{4:0.00}) ", new object[] { d.ColorId, color.r, color.g, color.b, color.a }),
								string.Format("-> rgba({0:0.00},{1:0.00},{2:0.00},{3:0.00})", new object[] { color2.r, color2.g, color2.b, color2.a })
							});
						}
					}
					break;
				case RainSensor.DimLever.StartColor:
					if (!(d.Ps == null))
					{
						ParticleSystem.MainModule main = d.Ps.main;
						Color color3 = RainSensor.ScaleForDim(d.StartMin, gameRainOpacity);
						Color color4 = RainSensor.ScaleForDim(d.StartMax, gameRainOpacity);
						main.startColor = ((d.StartMode == ParticleSystemGradientMode.TwoColors) ? new ParticleSystem.MinMaxGradient(color3, color4) : new ParticleSystem.MinMaxGradient(color4));
						if (d.LoggedGen != this._sceneGen)
						{
							text = string.Format("ParticleSystem main.startColor ({0}) ", d.StartMode) + string.Format("rgba({0:0.00},{1:0.00},{2:0.00},{3:0.00}) ", new object[]
							{
								d.StartMax.r,
								d.StartMax.g,
								d.StartMax.b,
								d.StartMax.a
							}) + string.Format("-> rgba({0:0.00},{1:0.00},{2:0.00},{3:0.00})", new object[] { color4.r, color4.g, color4.b, color4.a });
						}
					}
					break;
				case RainSensor.DimLever.RendererEnable:
					if (!(d.Renderer == null))
					{
						bool flag = gameRainOpacity <= 0.02f;
						if (flag != d.RendererForced)
						{
							d.Renderer.enabled = !flag && d.RendererWasEnabled;
							d.RendererForced = flag;
						}
						if (d.LoggedGen != this._sceneGen)
						{
							text = "NO colour lane on material or startColor — renderer.enabled lever only (renderer " + (flag ? "DISABLED" : "left enabled; opacity > 0.02 cannot be applied") + ")";
						}
					}
					break;
				}
			}
			catch (Exception ex)
			{
				if (d.LoggedGen != this._sceneGen)
				{
					text = string.Format("lever {0} FAILED: {1}", d.Lever, ex.Message);
				}
			}
			if (text != null)
			{
				d.LoggedGen = this._sceneGen;
				string text2 = "?";
				try
				{
					if (d.Ps != null)
					{
						text2 = d.Ps.name;
					}
				}
				catch
				{
				}
				this._log.LogInfo(string.Format("RainSensor §J dim '{0}': opacity {1:0.00} via {2} — {3}", new object[] { text2, gameRainOpacity, d.Lever, text }));
			}
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00017FE8 File Offset: 0x000161E8
		private static Color ScaleForDim(Color c, float k)
		{
			return new Color(c.r * k, c.g * k, c.b * k, c.a * k);
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x00018010 File Offset: 0x00016210
		private void RestoreDim(in RainSensor.Dim d)
		{
			try
			{
				if (d.Renderer != null && d.Instance != null && d.Renderer.sharedMaterial == d.Instance && d.Original != null)
				{
					d.Renderer.sharedMaterial = d.Original;
				}
			}
			catch
			{
			}
			try
			{
				if (d.Lever == RainSensor.DimLever.StartColor && d.Ps != null)
				{
					ParticleSystem.MainModule main = d.Ps.main;
					main.startColor = ((d.StartMode == ParticleSystemGradientMode.TwoColors) ? new ParticleSystem.MinMaxGradient(d.StartMin, d.StartMax) : new ParticleSystem.MinMaxGradient(d.StartMax));
				}
			}
			catch
			{
			}
			try
			{
				if (d.Lever == RainSensor.DimLever.RendererEnable && d.RendererForced && d.Renderer != null)
				{
					d.Renderer.enabled = d.RendererWasEnabled;
				}
			}
			catch
			{
			}
			if (d.Instance != null)
			{
				try
				{
					UnityEngine.Object.Destroy(d.Instance);
				}
				catch
				{
				}
			}
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x00018150 File Offset: 0x00016350
		private void RestoreAllDims()
		{
			for (int i = this._dims.Count - 1; i >= 0; i--)
			{
				RainSensor.Dim dim = this._dims[i];
				this.RestoreDim(in dim);
			}
			this._dims.Clear();
		}

		// Token: 0x060001A4 RID: 420 RVA: 0x00018198 File Offset: 0x00016398
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.RestoreAllDims();
			this._scanning = false;
			this._stack.Clear();
			this._tracked.Clear();
			RainSensor.RainFactor = 0f;
			RainSensor.WetBuildup = 0f;
			try
			{
				Shader.SetGlobalFloat(ShaderIds.RainFactorGlobal, 0f);
				Shader.SetGlobalFloat(ShaderIds.WetBuildupGlobal, 0f);
				Shader.SetGlobalFloat(ShaderIds.CamCoveredGlobal, 0f);
			}
			catch
			{
			}
		}

		// Token: 0x0400036D RID: 877
		private const int TransformsPerTick = 40;

		// Token: 0x0400036E RID: 878
		private const int MaxTracked = 32;

		// Token: 0x0400036F RID: 879
		private const int MaxExaminedPerScan = 100000;

		// Token: 0x04000370 RID: 880
		private const float ScanIntervalSeconds = 10f;

		// Token: 0x04000371 RID: 881
		private const float EmptyScanIntervalSeconds = 30f;

		// Token: 0x04000372 RID: 882
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000373 RID: 883
		private const float RiseFallSeconds = 3f;

		// Token: 0x04000374 RID: 884
		private const float BuildupRiseSeconds = 35f;

		// Token: 0x04000375 RID: 885
		private const float BuildupDrainSeconds = 60f;

		// Token: 0x04000376 RID: 886
		private const float RainActiveEps = 0.001f;

		// Token: 0x04000377 RID: 887
		private static readonly string[] NameKeywords = new string[] { "rain", "storm", "drizzle" };

		// Token: 0x04000378 RID: 888
		private static readonly string[] NameRejects = new string[] { "rainbow", "brainstorm" };

		// Token: 0x0400037C RID: 892
		private readonly ManualLogSource _log;

		// Token: 0x0400037D RID: 893
		private readonly List<GameObject> _rootsScratch = new List<GameObject>(64);

		// Token: 0x0400037E RID: 894
		private readonly Stack<Transform> _stack = new Stack<Transform>(256);

		// Token: 0x0400037F RID: 895
		private readonly List<ParticleSystem> _tracked = new List<ParticleSystem>(32);

		// Token: 0x04000380 RID: 896
		private bool _enabled;

		// Token: 0x04000381 RID: 897
		private float _factor;

		// Token: 0x04000382 RID: 898
		private float _buildup;

		// Token: 0x04000383 RID: 899
		private readonly List<RainSensor.Dim> _dims = new List<RainSensor.Dim>(32);

		// Token: 0x04000384 RID: 900
		private bool _dimWanted;

		// Token: 0x04000385 RID: 901
		private bool _dimVrAllowed = true;

		// Token: 0x04000386 RID: 902
		private bool _dimDesktopAllowed = true;

		// Token: 0x04000387 RID: 903
		private bool _dimOn;

		// Token: 0x04000388 RID: 904
		private float _gameRainOpacity = 0.6f;

		// Token: 0x04000389 RID: 905
		private bool _dimsDirty;

		// Token: 0x0400038A RID: 906
		private int _sceneGen;

		// Token: 0x0400038B RID: 907
		private int _summaryGen = -1;

		// Token: 0x0400038C RID: 908
		private int _summaryCount = -1;

		// Token: 0x0400038D RID: 909
		private bool _scanning;

		// Token: 0x0400038E RID: 910
		private int _examined;

		// Token: 0x0400038F RID: 911
		private float _nextScanAt;

		// Token: 0x04000390 RID: 912
		private bool _sceneJustLoaded;

		// Token: 0x04000391 RID: 913
		private int _emptyScans;

		// Token: 0x04000392 RID: 914
		private int _lastLoggedFound = -1;

		// Token: 0x0200002A RID: 42
		private enum DimLever
		{
			// Token: 0x04000394 RID: 916
			None,
			// Token: 0x04000395 RID: 917
			MaterialColor,
			// Token: 0x04000396 RID: 918
			StartColor,
			// Token: 0x04000397 RID: 919
			RendererEnable
		}

		// Token: 0x0200002B RID: 43
		private struct Dim
		{
			// Token: 0x04000398 RID: 920
			public ParticleSystem Ps;

			// Token: 0x04000399 RID: 921
			public ParticleSystemRenderer Renderer;

			// Token: 0x0400039A RID: 922
			public Material Original;

			// Token: 0x0400039B RID: 923
			public Material Instance;

			// Token: 0x0400039C RID: 924
			public int ColorId;

			// Token: 0x0400039D RID: 925
			public RainSensor.DimLever Lever;

			// Token: 0x0400039E RID: 926
			public ParticleSystemGradientMode StartMode;

			// Token: 0x0400039F RID: 927
			public Color StartMin;

			// Token: 0x040003A0 RID: 928
			public Color StartMax;

			// Token: 0x040003A1 RID: 929
			public bool RendererWasEnabled;

			// Token: 0x040003A2 RID: 930
			public bool RendererForced;

			// Token: 0x040003A3 RID: 931
			public int LoggedGen;
		}
	}
}
