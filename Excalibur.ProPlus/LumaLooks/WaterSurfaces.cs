using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000054 RID: 84
	internal sealed class WaterSurfaces
	{
		// Token: 0x1700006F RID: 111
		// (get) Token: 0x06000309 RID: 777 RVA: 0x0002B447 File Offset: 0x00029647
		// (set) Token: 0x0600030A RID: 778 RVA: 0x0002B44E File Offset: 0x0002964E
		public static float GlobalUnderwater { get; private set; }

		// Token: 0x17000070 RID: 112
		// (get) Token: 0x0600030B RID: 779 RVA: 0x0002B456 File Offset: 0x00029656
		// (set) Token: 0x0600030C RID: 780 RVA: 0x0002B45D File Offset: 0x0002965D
		public static bool WaterDepthNeeded { get; private set; }

		// Token: 0x0600030D RID: 781 RVA: 0x0002B468 File Offset: 0x00029668
		public WaterSurfaces(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x0600030E RID: 782 RVA: 0x0002B59E File Offset: 0x0002979E
		public void NotifyEngineDisabled()
		{
			if (this._engineDisabled)
			{
				return;
			}
			this._engineDisabled = true;
			this._dirty = true;
		}

		// Token: 0x0600030F RID: 783 RVA: 0x0002B5B8 File Offset: 0x000297B8
		public void Configure(bool waterOn, bool underwaterOn, bool vrAllowed, bool desktopAllowed, float waveStrength, float waveSpeed, float waveHeight, float clarity, float reflection, float wetness, int surfaceStyle, bool vrPerfBalanced, Color deepTint, Color shallowTint, float refraction, float glint)
		{
			this._waterWant = waterOn;
			this._uwWant = underwaterOn;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._waveStrength = Mathf.Clamp01(waveStrength);
			this._waveSpeed = Mathf.Clamp01(waveSpeed);
			this._waveHeight = Mathf.Clamp01(waveHeight);
			this._clarity = Mathf.Clamp01(clarity);
			this._reflection = Mathf.Clamp01(reflection);
			this._vrPerfBalanced = vrPerfBalanced;
			this._wetness = Mathf.Clamp01(wetness);
			this._refraction = Mathf.Clamp01(refraction);
			this._glint = Mathf.Clamp01(glint);
			this._surfaceStyle = ((surfaceStyle >= 1 && surfaceStyle <= 4) ? ((float)surfaceStyle) : 0f);
			this._style = WaterSurfaces.WaterStyle.Resolve(this._surfaceStyle).ApplyUserTints(deepTint, shallowTint);
			this._deepTint = deepTint;
			this._shallowTint = shallowTint;
			this._dirty = true;
		}

		// Token: 0x06000310 RID: 784 RVA: 0x0002B6A0 File Offset: 0x000298A0
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._scanning = false;
			this._stack.Clear();
			this._sceneJustLoaded = true;
			this._emptyScans = 0;
			this._sceneGen++;
		}

		// Token: 0x06000311 RID: 785 RVA: 0x0002B6D0 File Offset: 0x000298D0
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
				bool flag2 = !this._engineDisabled && this._waterWant && (flag ? this._vrAllowed : this._desktopAllowed);
				if (flag2 != this._waterOn)
				{
					this._waterOn = flag2;
					this._dirty = true;
					if (flag2)
					{
						this._nextScanAt = 0f;
						this._emptyScans = 0;
					}
				}
				int num = ((flag && this._vrPerfBalanced) ? 4 : 6);
				if (num != this._specComponents)
				{
					this._specComponents = num;
					this._dirty = true;
				}
				if (this._dirty)
				{
					this._dirty = false;
					this.ApplyConfig();
				}
				bool flag3 = this._waterOn || this._uwWant;
				if (flag3 && !this._scanWantedPrev)
				{
					this._nextScanAt = 0f;
					this._emptyScans = 0;
				}
				this._scanWantedPrev = flag3;
				if (flag3)
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
					this.ValidateTracked();
					if (this._waterOn)
					{
						this.PushSunGlobals();
						this.ProbeBodyScales();
					}
				}
				if (this._waterOn || this._uwWant || Particles.AnyEffectOn || RainParticles.RainOn || Birds.BirdsOn || Insects.AnyOn)
				{
					this.PushSunVectorGlobals();
				}
				if (this._waterOn)
				{
					Color linear = this._style.Scatter.linear;
					Shader.SetGlobalVector(ShaderIds.WaterScatterGlobal, new Vector4(linear.r, linear.g, linear.b, 0f));
					Shader.SetGlobalVector(ShaderIds.WaterSigmaGlobal, new Vector4(this._style.Sigma.x, this._style.Sigma.y, this._style.Sigma.z, 0f));
				}
				else
				{
					Shader.SetGlobalVector(ShaderIds.WaterSigmaGlobal, Vector4.zero);
					Shader.SetGlobalVector(ShaderIds.WaterScatterGlobal, Vector4.zero);
				}
				this.UpdateUnderwater();
				this.PushWaterPlaneGlobals();
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WaterSurfaces tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000312 RID: 786 RVA: 0x0002B94C File Offset: 0x00029B4C
		private void ApplyConfig()
		{
			if (!this._waterOn)
			{
				this.RevertAll();
				if (!this._uwWant)
				{
					this._scanning = false;
					this._stack.Clear();
					this._tracked.Clear();
				}
				return;
			}
			this.PushParams();
			for (int i = 0; i < this._tracked.Count; i++)
			{
				this.SwapAt(i);
			}
		}

		// Token: 0x06000313 RID: 787 RVA: 0x0002B9B0 File Offset: 0x00029BB0
		private bool EnsureMaterial()
		{
			if (this._waterMat != null)
			{
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/Water") : null);
			if (shader == null)
			{
				if (!this._waterShaderMissingLogged)
				{
					this._waterShaderMissingLogged = true;
					this._log.LogWarning("WaterSurfaces: shader 'LumaLooks/Water' not in the bundle — water surface disabled.");
				}
				return false;
			}
			this._waterMat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this.PushParams();
			return true;
		}

		// Token: 0x06000314 RID: 788 RVA: 0x0002BA2C File Offset: 0x00029C2C
		private void PushParams()
		{
			if (this._waterMat == null)
			{
				return;
			}
			this._waterMat.SetFloat(ShaderIds.WaterWaveStrength, this._waveStrength);
			this._waterMat.SetFloat(ShaderIds.WaterWaveSpeed, this._waveSpeed);
			this._waterMat.SetFloat(ShaderIds.WaterWaveHeight, this._waveHeight);
			this._waterMat.SetFloat(ShaderIds.WaterClarity, this._clarity);
			this._waterMat.SetFloat(ShaderIds.WaterReflection, this._reflection);
			this._waterMat.SetFloat(ShaderIds.WaterRefraction, this._refraction);
			this._waterMat.SetFloat(ShaderIds.WaterGlint, this._glint);
			this._waterMat.SetFloat(ShaderIds.WaterSpecClamp, (this._surfaceStyle >= 0.5f) ? 1.2f : 2.5f);
			this._waterMat.SetVector(ShaderIds.WaterSpectrum, new Vector4((float)this._specComponents, 0.9f, 0.72f, 0.6f));
			this._waterMat.SetColor(ShaderIds.WaterDeepTint, this._deepTint.linear);
			this._waterMat.SetColor(ShaderIds.WaterShallowTint, this._shallowTint.linear);
			this._waterMat.SetColor(ShaderIds.WaterScatter, this._style.Scatter.linear);
			this._waterMat.SetVector(ShaderIds.WaterSigma, this._style.Sigma);
			this._waterMat.SetFloat(ShaderIds.WaterRough, this._style.Rough);
			this._waterMat.SetColor(ShaderIds.WaterSkyTint, this._style.SkyTint.linear);
			this._waterMat.SetFloat(ShaderIds.WaterUseBaseTex, this._style.UseBaseTex);
		}

		// Token: 0x06000315 RID: 789 RVA: 0x0002BC04 File Offset: 0x00029E04
		private void PushSunVectorGlobals()
		{
			Light light = WorldLight.ActiveSun;
			if (light == null)
			{
				light = RenderSettings.sun;
			}
			Vector3 vector = WorldLight.ResolvedSunDir;
			if (vector.sqrMagnitude < 1E-08f)
			{
				vector = ((light != null) ? (-light.transform.forward) : Vector3.up);
			}
			Shader.SetGlobalVector(ShaderIds.SunDirWSGlobal, new Vector4(vector.x, vector.y, vector.z, 0f));
			Color color = ((light != null) ? (light.color.linear * light.intensity) : Color.white);
			Shader.SetGlobalVector(ShaderIds.SunColorGlobal, new Vector4(color.r, color.g, color.b, 0f));
		}

		// Token: 0x06000316 RID: 790 RVA: 0x0002BCD4 File Offset: 0x00029ED4
		private void PushSunGlobals()
		{
			float num = 0f;
			if (RenderSettings.fog)
			{
				num = ((RenderSettings.fogMode == FogMode.Linear) ? 1f : ((RenderSettings.fogMode == FogMode.Exponential) ? 2f : 3f));
			}
			Shader.SetGlobalVector(ShaderIds.SceneFogParamsGlobal, new Vector4(num, RenderSettings.fogDensity, RenderSettings.fogStartDistance, RenderSettings.fogEndDistance));
			Color linear = RenderSettings.fogColor.linear;
			Shader.SetGlobalVector(ShaderIds.SceneFogColorGlobal, new Vector4(linear.r, linear.g, linear.b, 0f));
		}

		// Token: 0x06000317 RID: 791 RVA: 0x0002BD68 File Offset: 0x00029F68
		private void PushWaterPlaneGlobals()
		{
			bool flag = false;
			bool flag2 = false;
			float num = -1f;
			Bounds bounds = default(Bounds);
			Renderer renderer = ((this._uwFactor > 0f) ? this._submergedIn : null);
			bool flag3 = false;
			Bounds bounds2 = default(Bounds);
			for (int i = 0; i < this._tracked.Count; i++)
			{
				WaterSurfaces.Tracked tracked = this._tracked[i];
				if (!(tracked.R == null))
				{
					if (tracked.Originals != null)
					{
						flag = true;
					}
					Bounds bounds3 = tracked.R.bounds;
					Vector3 extents = bounds3.extents;
					if (extents.y <= 2f && extents.y <= Mathf.Max(extents.x, extents.z) * 0.5f)
					{
						if (renderer != null && tracked.R == renderer)
						{
							bounds2 = bounds3;
							flag3 = true;
						}
						float num2 = extents.x * extents.z;
						if (num2 > num)
						{
							num = num2;
							bounds = bounds3;
							flag2 = true;
						}
					}
				}
			}
			if (flag3)
			{
				bounds = bounds2;
			}
			WaterSurfaces.WaterDepthNeeded = this._waterOn && flag;
			Shader.SetGlobalFloat(ShaderIds.WaterHeaveAmpGlobal, (this._waterOn && flag) ? (this._waveHeight * 0.35f) : 0f);
			Shader.SetGlobalVector(ShaderIds.WaterSSRParamsGlobal, (this._waterOn && flag) ? new Vector4(0f, 0f, (MapSense.IsOutdoor && !MapSense.IsBasement) ? 1f : 0f, 2f) : Vector4.zero);
			if (flag2)
			{
				Shader.SetGlobalVector(ShaderIds.WaterPlaneGlobal, new Vector4(bounds.max.y, bounds.min.x - 2f, bounds.min.z - 2f, 1f));
				Shader.SetGlobalVector(ShaderIds.WaterPlane2Global, new Vector4(bounds.max.x + 2f, bounds.max.z + 2f, this._wetness, this._waterOn ? 1f : 0f));
				return;
			}
			Shader.SetGlobalVector(ShaderIds.WaterPlaneGlobal, Vector4.zero);
			Shader.SetGlobalVector(ShaderIds.WaterPlane2Global, Vector4.zero);
		}

		// Token: 0x06000318 RID: 792 RVA: 0x0002BFB4 File Offset: 0x0002A1B4
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

		// Token: 0x06000319 RID: 793 RVA: 0x0002C06C File Offset: 0x0002A26C
		private void StepScan()
		{
			int num = 40;
			while (num-- > 0 && this._stack.Count > 0)
			{
				Transform transform = this._stack.Pop();
				if (!(transform == null))
				{
					this._examined++;
					if (!this.Examine(transform))
					{
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
			}
			if (this._stack.Count == 0 || this._examined >= 100000)
			{
				this.FinishScan();
			}
		}

		// Token: 0x0600031A RID: 794 RVA: 0x0002C118 File Offset: 0x0002A318
		private bool Examine(Transform t)
		{
			MeshRenderer meshRenderer = null;
			if (!t.TryGetComponent<MeshRenderer>(out meshRenderer) || meshRenderer == null)
			{
				return false;
			}
			bool flag = WaterSurfaces.NameMatches(t.name);
			if (!flag)
			{
				try
				{
					Material sharedMaterial = meshRenderer.sharedMaterial;
					if (sharedMaterial != null)
					{
						flag = WaterSurfaces.NameMatches(sharedMaterial.name);
					}
				}
				catch
				{
				}
			}
			if (!flag)
			{
				return false;
			}
			for (int i = 0; i < this._tracked.Count; i++)
			{
				if (this._tracked[i].R == meshRenderer)
				{
					return true;
				}
			}
			if (this._tracked.Count >= 16)
			{
				return true;
			}
			this._tracked.Add(new WaterSurfaces.Tracked
			{
				R = meshRenderer,
				Originals = null,
				LoggedGen = -1
			});
			if (this._waterOn)
			{
				this.SwapAt(this._tracked.Count - 1);
			}
			return true;
		}

		// Token: 0x0600031B RID: 795 RVA: 0x0002C208 File Offset: 0x0002A408
		private static bool NameMatches(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			for (int i = 0; i < WaterSurfaces.NameRejects.Length; i++)
			{
				if (name.IndexOf(WaterSurfaces.NameRejects[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return false;
				}
			}
			for (int j = 0; j < WaterSurfaces.NameKeywords.Length; j++)
			{
				string text = WaterSurfaces.NameKeywords[j];
				int num = 0;
				int num2;
				while ((num2 = name.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)) >= 0)
				{
					if (WaterSurfaces.IsTokenStart(name, num2) && WaterSurfaces.IsTokenEnd(name, num2 + text.Length))
					{
						return true;
					}
					num = num2 + 1;
				}
			}
			return false;
		}

		// Token: 0x0600031C RID: 796 RVA: 0x0002C294 File Offset: 0x0002A494
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x0600031D RID: 797 RVA: 0x0002C2D0 File Offset: 0x0002A4D0
		private static bool IsTokenEnd(string s, int end)
		{
			if (end >= s.Length)
			{
				return true;
			}
			char c = s[end];
			return !char.IsLetter(c) || char.IsUpper(c);
		}

		// Token: 0x0600031E RID: 798 RVA: 0x0002C300 File Offset: 0x0002A500
		private void FinishScan()
		{
			this._scanning = false;
			this._stack.Clear();
			this._emptyScans = ((this._tracked.Count == 0) ? (this._emptyScans + 1) : 0);
			this._nextScanAt = Time.realtimeSinceStartup + ((this._emptyScans >= 2) ? 60f : 12f);
			if (this._tracked.Count != this._lastLoggedFound)
			{
				this._lastLoggedFound = this._tracked.Count;
				this._log.LogInfo(string.Format("WaterSurfaces: {0} water renderer(s) tracked ", this._tracked.Count) + string.Format("({0} transforms examined), surface swap {1}.", this._examined, this._waterOn ? "ON" : "off"));
			}
		}

		// Token: 0x0600031F RID: 799 RVA: 0x0002C3D8 File Offset: 0x0002A5D8
		private void SwapAt(int i)
		{
			WaterSurfaces.Tracked tracked = this._tracked[i];
			if (tracked.Originals != null || tracked.R == null)
			{
				return;
			}
			if (!this.EnsureMaterial())
			{
				return;
			}
			try
			{
				Material[] sharedMaterials = tracked.R.sharedMaterials;
				tracked.R.sharedMaterial = this._waterMat;
				tracked.Originals = sharedMaterials;
				tracked.ScaleProbed = false;
				this.ApplyBodyMpb(ref tracked);
				this._tracked[i] = tracked;
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WaterSurfaces: could not swap a water renderer: " + ex.Message);
			}
		}

		// Token: 0x06000320 RID: 800 RVA: 0x0002C488 File Offset: 0x0002A688
		private void RevertAt(int i)
		{
			WaterSurfaces.Tracked tracked = this._tracked[i];
			if (tracked.Originals != null && tracked.R != null)
			{
				try
				{
					tracked.R.sharedMaterials = tracked.Originals;
				}
				catch
				{
				}
				try
				{
					tracked.R.SetPropertyBlock(null);
				}
				catch
				{
				}
			}
			tracked.Originals = null;
			this._tracked[i] = tracked;
		}

		// Token: 0x06000321 RID: 801 RVA: 0x0002C514 File Offset: 0x0002A714
		private void ApplyBodyMpb(ref WaterSurfaces.Tracked tr)
		{
			if (tr.R == null)
			{
				return;
			}
			MaterialPropertyBlock materialPropertyBlock;
			if ((materialPropertyBlock = tr.Mpb) == null)
			{
				materialPropertyBlock = (tr.Mpb = new MaterialPropertyBlock());
			}
			MaterialPropertyBlock materialPropertyBlock2 = materialPropertyBlock;
			materialPropertyBlock2.Clear();
			materialPropertyBlock2.SetFloat(ShaderIds.WaterHasBaseTex, 0f);
			Material material = ((tr.Originals != null && tr.Originals.Length != 0) ? tr.Originals[0] : null);
			bool flag = tr.LoggedGen != this._sceneGen;
			Texture texture = null;
			string text = null;
			string text2 = "<none>";
			string text3 = "<none>";
			if (material != null)
			{
				try
				{
					if (flag)
					{
						text2 = material.name;
						text3 = ((material.shader != null) ? material.shader.name : "<null shader>");
					}
					Vector4 vector;
					texture = WaterSurfaces.ProbeBaseTexture(material, out vector, out text);
					if (texture != null)
					{
						materialPropertyBlock2.SetTexture(ShaderIds.WaterBaseTex, texture);
						materialPropertyBlock2.SetVector(ShaderIds.WaterBaseTexST, vector);
						materialPropertyBlock2.SetFloat(ShaderIds.WaterHasBaseTex, 1f);
					}
					else
					{
						materialPropertyBlock2.SetFloat(ShaderIds.WaterHasBaseTex, 0f);
					}
					Color color = Color.white;
					if (material.HasProperty(ShaderIds.GameColor))
					{
						color = material.GetColor(ShaderIds.GameColor);
					}
					else if (material.HasProperty(ShaderIds.GameBaseColor))
					{
						color = material.GetColor(ShaderIds.GameBaseColor);
					}
					materialPropertyBlock2.SetColor(ShaderIds.WaterBaseColor, color.linear);
				}
				catch (Exception ex)
				{
					if (flag)
					{
						this._log.LogWarning("WaterSurfaces §D: base-texture probe threw on '" + text2 + "': " + ex.Message);
					}
				}
			}
			materialPropertyBlock2.SetFloat(ShaderIds.WaterBodyScale, tr.ScaleProbed ? tr.BodyScale : 1f);
			try
			{
				tr.R.SetPropertyBlock(materialPropertyBlock2);
			}
			catch
			{
			}
			if (flag)
			{
				tr.LoggedGen = this._sceneGen;
				string text4 = "?";
				try
				{
					text4 = tr.R.name;
				}
				catch
				{
				}
				if (material == null)
				{
					this._log.LogInfo("WaterSurfaces §D '" + text4 + "': no original material cached (not swapped yet) — _LumaWaterHasBaseTex = 0.");
					return;
				}
				if (texture != null)
				{
					this._log.LogInfo(string.Concat(new string[]
					{
						"WaterSurfaces §D '",
						text4,
						"': material '",
						text2,
						"' shader '",
						text3,
						"' — texture found on '",
						text,
						"' = '",
						texture.name,
						"' ",
						string.Format("({0}x{1}) — _LumaWaterHasBaseTex = 1.", texture.width, texture.height)
					}));
					return;
				}
				this._log.LogWarning(string.Concat(new string[] { "WaterSurfaces §D '", text4, "': material '", text2, "' shader '", text3, "' — NO assigned texture on _MainTex/_BaseMap or any albedo-like shader property — _LumaWaterHasBaseTex = 0 (Native goes near-transparent; the scene must show through, not a pale sheet)." }));
			}
		}

		// Token: 0x06000322 RID: 802 RVA: 0x0002C82C File Offset: 0x0002AA2C
		private static Texture ProbeBaseTexture(Material m, out Vector4 st, out string propName)
		{
			st = new Vector4(1f, 1f, 0f, 0f);
			propName = null;
			if (m == null)
			{
				return null;
			}
			if (m.HasProperty(ShaderIds.GameMainTex))
			{
				Texture texture = m.GetTexture(ShaderIds.GameMainTex);
				if (texture != null)
				{
					propName = "_MainTex";
					if (m.HasProperty(ShaderIds.GameMainTexST))
					{
						st = m.GetVector(ShaderIds.GameMainTexST);
					}
					return texture;
				}
			}
			if (m.HasProperty(ShaderIds.GameBaseMap))
			{
				Texture texture2 = m.GetTexture(ShaderIds.GameBaseMap);
				if (texture2 != null)
				{
					propName = "_BaseMap";
					if (m.HasProperty(ShaderIds.GameBaseMapST))
					{
						st = m.GetVector(ShaderIds.GameBaseMapST);
					}
					return texture2;
				}
			}
			Shader shader = m.shader;
			if (shader == null)
			{
				return null;
			}
			int propertyCount;
			try
			{
				propertyCount = shader.GetPropertyCount();
			}
			catch
			{
				return null;
			}
			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < propertyCount; j++)
				{
					try
					{
						if (shader.GetPropertyType(j) == UnityEngine.Rendering.ShaderPropertyType.Texture)
						{
							string propertyName = shader.GetPropertyName(j);
							if (!string.IsNullOrEmpty(propertyName))
							{
								if (!WaterSurfaces.ContainsAny(propertyName, WaterSurfaces.TexNameReject))
								{
									if (i != 0 || WaterSurfaces.ContainsAny(propertyName, WaterSurfaces.TexNamePrefer))
									{
										Texture texture3 = m.GetTexture(propertyName);
										if (!(texture3 == null))
										{
											propName = propertyName;
											int num = Shader.PropertyToID(propertyName + "_ST");
											if (m.HasProperty(num))
											{
												st = m.GetVector(num);
											}
											return texture3;
										}
									}
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			return null;
		}

		// Token: 0x06000323 RID: 803 RVA: 0x0002CA04 File Offset: 0x0002AC04
		private static bool ContainsAny(string s, string[] fragments)
		{
			for (int i = 0; i < fragments.Length; i++)
			{
				if (s.IndexOf(fragments[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000324 RID: 804 RVA: 0x0002CA30 File Offset: 0x0002AC30
		private void ProbeBodyScales()
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			for (int i = 0; i < this._tracked.Count; i++)
			{
				WaterSurfaces.Tracked tracked = this._tracked[i];
				if (!(tracked.R == null) && tracked.Originals != null && (!tracked.ScaleProbed || realtimeSinceStartup >= tracked.NextProbeAt))
				{
					float num = this.ComputeBodyScale(tracked.R);
					tracked.BodyScale = num;
					tracked.ScaleProbed = true;
					tracked.NextProbeAt = realtimeSinceStartup + 8f;
					this.ApplyBodyMpb(ref tracked);
					this._tracked[i] = tracked;
					return;
				}
			}
		}

		// Token: 0x06000325 RID: 805 RVA: 0x0002CAD4 File Offset: 0x0002ACD4
		private float ComputeBodyScale(Renderer r)
		{
			Bounds bounds = r.bounds;
			float y = bounds.max.y;
			float num = bounds.size.x * bounds.size.z;
			float num2 = Mathf.Lerp(bounds.min.x, bounds.max.x, 0.15f);
			float num3 = Mathf.Lerp(bounds.min.x, bounds.max.x, 0.85f);
			float num4 = Mathf.Lerp(bounds.min.z, bounds.max.z, 0.15f);
			float num5 = Mathf.Lerp(bounds.min.z, bounds.max.z, 0.85f);
			this._probePts[0] = new Vector3(num2, y, num4);
			this._probePts[1] = new Vector3(num3, y, num4);
			this._probePts[2] = new Vector3(num2, y, num5);
			this._probePts[3] = new Vector3(num3, y, num5);
			this._probePts[4] = new Vector3((num2 + num3) * 0.5f, y, (num4 + num5) * 0.5f);
			int num6 = 0;
			for (int i = 0; i < 5; i++)
			{
				RaycastHit raycastHit = default;
				if (Physics.Raycast(this._probePts[i] + Vector3.down * 0.2f, Vector3.down, out raycastHit, 60f, -1, QueryTriggerInteraction.Ignore))
				{
					this._probeDepths[num6++] = raycastHit.distance + 0.2f;
				}
			}
			float num7;
			if (num6 == 0)
			{
				num7 = 60f;
			}
			else
			{
				for (int j = 1; j < num6; j++)
				{
					float num8 = this._probeDepths[j];
					int num9 = j - 1;
					while (num9 >= 0 && this._probeDepths[num9] > num8)
					{
						this._probeDepths[num9 + 1] = this._probeDepths[num9];
						num9--;
					}
					this._probeDepths[num9 + 1] = num8;
				}
				num7 = this._probeDepths[num6 / 2];
			}
			float num10 = Mathf.Clamp01(num / 400f) * Mathf.Clamp01(num7 / 2.5f);
			return Mathf.Max(0.08f, num10);
		}

		// Token: 0x06000326 RID: 806 RVA: 0x0002CD20 File Offset: 0x0002AF20
		private void RevertAll()
		{
			for (int i = 0; i < this._tracked.Count; i++)
			{
				this.RevertAt(i);
			}
		}

		// Token: 0x06000327 RID: 807 RVA: 0x0002CD4C File Offset: 0x0002AF4C
		private void ValidateTracked()
		{
			for (int i = this._tracked.Count - 1; i >= 0; i--)
			{
				WaterSurfaces.Tracked tracked = this._tracked[i];
				if (tracked.R == null)
				{
					this._tracked.RemoveAt(i);
				}
				else
				{
					Vector3 extents = tracked.R.bounds.extents;
					if (extents.x <= 0.0001f && extents.y <= 0.0001f && extents.z <= 0.0001f)
					{
						if (tracked.Originals != null)
						{
							try
							{
								tracked.R.sharedMaterials = tracked.Originals;
							}
							catch
							{
							}
							try
							{
								tracked.R.SetPropertyBlock(null);
							}
							catch
							{
							}
							this._log.LogWarning("WaterSurfaces: bounds of '" + tracked.R.name + "' vanished — reverted its original material.");
						}
						this._tracked.RemoveAt(i);
					}
				}
			}
		}

		// Token: 0x06000328 RID: 808 RVA: 0x0002CE5C File Offset: 0x0002B05C
		private void UpdateUnderwater()
		{
			float num = 0f;
			Renderer renderer = null;
			if (this._uwWant)
			{
				Camera main = Camera.main;
				if (main != null)
				{
					Vector3 position = main.transform.position;
					for (int i = 0; i < this._tracked.Count; i++)
					{
						Renderer r = this._tracked[i].R;
						if (!(r == null))
						{
							Bounds bounds = r.bounds;
							Vector3 extents = bounds.extents;
							if (extents.y <= 2f && extents.y <= Mathf.Max(extents.x, extents.z) * 0.5f && position.y < bounds.max.y + 0.08f && position.y > bounds.min.y - 60f && position.x > bounds.min.x - 0.5f && position.x < bounds.max.x + 0.5f && position.z > bounds.min.z - 0.5f && position.z < bounds.max.z + 0.5f)
							{
								num = 1f;
								renderer = r;
								break;
							}
						}
					}
				}
			}
			this._uwFactor = Mathf.MoveTowards(this._uwFactor, num, Time.unscaledDeltaTime / 0.6f);
			WaterSurfaces.GlobalUnderwater = this._uwFactor;
			Shader.SetGlobalFloat(ShaderIds.Underwater, this._uwFactor);
			if (num > 0.5f)
			{
				this._submergedIn = renderer;
				return;
			}
			if (this._uwFactor <= 0f)
			{
				this._submergedIn = null;
			}
		}

		// Token: 0x06000329 RID: 809 RVA: 0x0002D02C File Offset: 0x0002B22C
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			try
			{
				this.RevertAll();
			}
			catch
			{
			}
			this._scanning = false;
			this._stack.Clear();
			this._tracked.Clear();
			WaterSurfaces.GlobalUnderwater = 0f;
			WaterSurfaces.WaterDepthNeeded = false;
			this._submergedIn = null;
			try
			{
				Shader.SetGlobalFloat(ShaderIds.Underwater, 0f);
			}
			catch
			{
			}
			try
			{
				Shader.SetGlobalVector(ShaderIds.WaterPlaneGlobal, Vector4.zero);
				Shader.SetGlobalVector(ShaderIds.WaterPlane2Global, Vector4.zero);
				Shader.SetGlobalFloat(ShaderIds.WaterHeaveAmpGlobal, 0f);
				Shader.SetGlobalVector(ShaderIds.WaterSSRParamsGlobal, Vector4.zero);
			}
			catch
			{
			}
			if (this._waterMat != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._waterMat);
				}
				catch
				{
				}
				this._waterMat = null;
			}
		}

		// Token: 0x0400071C RID: 1820
		private const string WaterShaderName = "LumaLooks/Water";

		// Token: 0x0400071D RID: 1821
		private const int TransformsPerTick = 40;

		// Token: 0x0400071E RID: 1822
		private const int MaxWater = 16;

		// Token: 0x0400071F RID: 1823
		private const int MaxExaminedPerScan = 100000;

		// Token: 0x04000720 RID: 1824
		private const float ScanIntervalSeconds = 12f;

		// Token: 0x04000721 RID: 1825
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000722 RID: 1826
		private const float UnderwaterLerpSeconds = 0.6f;

		// Token: 0x04000723 RID: 1827
		private const float SurfaceEpsilon = 0.08f;

		// Token: 0x04000724 RID: 1828
		private const float HorizontalExpand = 0.5f;

		// Token: 0x04000725 RID: 1829
		private const float DepthBelow = 60f;

		// Token: 0x04000726 RID: 1830
		private const float MaxPlaneExtentY = 2f;

		// Token: 0x04000727 RID: 1831
		private const float PlaneAspectMax = 0.5f;

		// Token: 0x04000728 RID: 1832
		private const float EmptyScanIntervalSeconds = 60f;

		// Token: 0x04000729 RID: 1833
		private const float WetnessBoundsExpand = 2f;

		// Token: 0x0400072A RID: 1834
		private const float SpecClampNative = 2.5f;

		// Token: 0x0400072B RID: 1835
		private const float SpecClampRaft = 1.2f;

		// Token: 0x0400072C RID: 1836
		private const float SpectrumSpread = 0.9f;

		// Token: 0x0400072D RID: 1837
		private const float SpectrumDecay = 0.72f;

		// Token: 0x0400072E RID: 1838
		private const float SpectrumWindAngle = 0.6f;

		// Token: 0x0400072F RID: 1839
		private const int SpectrumComponents = 6;

		// Token: 0x04000730 RID: 1840
		private const int SpectrumComponentsVrBalanced = 4;

		// Token: 0x04000731 RID: 1841
		private static readonly string[] TexNamePrefer = new string[] { "albedo", "diffuse", "main", "base", "color", "colour", "tex" };

		// Token: 0x04000732 RID: 1842
		private static readonly string[] TexNameReject = new string[]
		{
			"bump", "normal", "mask", "metal", "specular", "smooth", "occlusion", "emiss", "detail", "noise",
			"ramp", "lut", "cube", "reflect", "refract", "distort", "flow", "foam", "depth", "displace",
			"height", "caustic"
		};

		// Token: 0x04000733 RID: 1843
		private const float ProbeStartBelow = 0.2f;

		// Token: 0x04000734 RID: 1844
		private const float ProbeMaxDist = 60f;

		// Token: 0x04000735 RID: 1845
		private const float WaveRefArea = 400f;

		// Token: 0x04000736 RID: 1846
		private const float WaveRefDepth = 2.5f;

		// Token: 0x04000737 RID: 1847
		private const float WaveScaleFloor = 0.08f;

		// Token: 0x04000738 RID: 1848
		private const int WaterMaskLayers = -1;

		// Token: 0x04000739 RID: 1849
		private const float ProbeRefreshSeconds = 8f;

		// Token: 0x0400073A RID: 1850
		private static readonly string[] NameKeywords = new string[] { "water", "ocean", "lake", "river", "pool" };

		// Token: 0x0400073B RID: 1851
		private static readonly string[] NameRejects = new string[] { "waterfall", "underwater" };

		// Token: 0x0400073E RID: 1854
		private readonly ManualLogSource _log;

		// Token: 0x0400073F RID: 1855
		private readonly RenderEngine _engine;

		// Token: 0x04000740 RID: 1856
		private readonly List<GameObject> _rootsScratch = new List<GameObject>(64);

		// Token: 0x04000741 RID: 1857
		private readonly Stack<Transform> _stack = new Stack<Transform>(256);

		// Token: 0x04000742 RID: 1858
		private readonly List<WaterSurfaces.Tracked> _tracked = new List<WaterSurfaces.Tracked>(16);

		// Token: 0x04000743 RID: 1859
		private Material _waterMat;

		// Token: 0x04000744 RID: 1860
		private bool _waterShaderMissingLogged;

		// Token: 0x04000745 RID: 1861
		private bool _waterWant;

		// Token: 0x04000746 RID: 1862
		private bool _uwWant;

		// Token: 0x04000747 RID: 1863
		private bool _vrAllowed = true;

		// Token: 0x04000748 RID: 1864
		private bool _desktopAllowed = true;

		// Token: 0x04000749 RID: 1865
		private bool _waterOn;

		// Token: 0x0400074A RID: 1866
		private float _waveStrength = 0.5f;

		// Token: 0x0400074B RID: 1867
		private float _waveSpeed = 0.4f;

		// Token: 0x0400074C RID: 1868
		private float _clarity = 0.8f;

		// Token: 0x0400074D RID: 1869
		private float _waveHeight = 0.35f;

		// Token: 0x0400074E RID: 1870
		private float _wetness = 0.5f;

		// Token: 0x0400074F RID: 1871
		private float _refraction = 0.6f;

		// Token: 0x04000750 RID: 1872
		private float _glint = 0.5f;

		// Token: 0x04000751 RID: 1873
		private float _reflection = 0.7f;

		// Token: 0x04000752 RID: 1874
		private bool _vrPerfBalanced;

		// Token: 0x04000753 RID: 1875
		private int _specComponents = 6;

		// Token: 0x04000754 RID: 1876
		private float _surfaceStyle;

		// Token: 0x04000755 RID: 1877
		private WaterSurfaces.WaterStyle _style = WaterSurfaces.WaterStyle.Resolve(0f);

		// Token: 0x04000756 RID: 1878
		private Color _deepTint = new Color(0.157f, 0.341f, 0.322f, 1f);

		// Token: 0x04000757 RID: 1879
		private Color _shallowTint = new Color(0.431f, 0.667f, 0.627f, 1f);

		// Token: 0x04000758 RID: 1880
		private bool _dirty;

		// Token: 0x04000759 RID: 1881
		private readonly Vector3[] _probePts = new Vector3[5];

		// Token: 0x0400075A RID: 1882
		private readonly float[] _probeDepths = new float[5];

		// Token: 0x0400075B RID: 1883
		private bool _scanning;

		// Token: 0x0400075C RID: 1884
		private int _examined;

		// Token: 0x0400075D RID: 1885
		private float _nextScanAt;

		// Token: 0x0400075E RID: 1886
		private bool _sceneJustLoaded;

		// Token: 0x0400075F RID: 1887
		private int _lastLoggedFound = -1;

		// Token: 0x04000760 RID: 1888
		private int _emptyScans;

		// Token: 0x04000761 RID: 1889
		private bool _scanWantedPrev;

		// Token: 0x04000762 RID: 1890
		private int _sceneGen;

		// Token: 0x04000763 RID: 1891
		private float _uwFactor;

		// Token: 0x04000764 RID: 1892
		private Renderer _submergedIn;

		// Token: 0x04000765 RID: 1893
		private bool _engineDisabled;

		// Token: 0x02000055 RID: 85
		internal struct WaterStyle
		{
			// Token: 0x0600032B RID: 811 RVA: 0x0002D2A0 File Offset: 0x0002B4A0
			public static WaterSurfaces.WaterStyle Resolve(float styleId)
			{
				WaterSurfaces.WaterStyle waterStyle = default(WaterSurfaces.WaterStyle);
				switch (Mathf.RoundToInt(styleId))
				{
				case 1:
					waterStyle.Scatter = new Color(0.02f, 0.115f, 0.23f);
					waterStyle.Sigma = new Vector3(0.85f, 0.28f, 0.14f);
					waterStyle.Rough = 0.11f;
					waterStyle.SkyTint = new Color(0.052f, 0.46f, 0.69f);
					waterStyle.UseBaseTex = 0f;
					break;
				case 2:
					waterStyle.Scatter = new Color(0.045f, 0.105f, 0.085f);
					waterStyle.Sigma = new Vector3(0.75f, 0.42f, 0.55f);
					waterStyle.Rough = 0.035f;
					waterStyle.SkyTint = new Color(0.16f, 0.26f, 0.31f);
					waterStyle.UseBaseTex = 0f;
					break;
				case 3:
					waterStyle.Scatter = new Color(0.085f, 0.42f, 0.44f);
					waterStyle.Sigma = new Vector3(0.55f, 0.11f, 0.09f);
					waterStyle.Rough = 0.05f;
					waterStyle.SkyTint = new Color(0.1f, 0.45f, 0.55f);
					waterStyle.UseBaseTex = 0f;
					break;
				case 4:
					waterStyle.Scatter = new Color(0.055f, 0.062f, 0.03f);
					waterStyle.Sigma = new Vector3(2.6f, 2.1f, 3f);
					waterStyle.Rough = 0.16f;
					waterStyle.SkyTint = new Color(0.13f, 0.15f, 0.11f);
					waterStyle.UseBaseTex = 0f;
					break;
				default:
					waterStyle.Scatter = new Color(0.431f, 0.667f, 0.627f);
					waterStyle.Sigma = new Vector3(1f, 0.7f, 0.75f);
					waterStyle.Rough = 0.055f;
					waterStyle.SkyTint = new Color(0.3f, 0.4f, 0.52f);
					waterStyle.UseBaseTex = 1f;
					break;
				}
				return waterStyle;
			}

			// Token: 0x0600032C RID: 812 RVA: 0x0002D4F8 File Offset: 0x0002B6F8
			public WaterSurfaces.WaterStyle ApplyUserTints(Color deep, Color shallow)
			{
				if (this.UseBaseTex < 0.5f)
				{
					return this;
				}
				WaterSurfaces.WaterStyle waterStyle = this;
				waterStyle.Scatter = shallow;
				waterStyle.Sigma = new Vector3(Mathf.Lerp(2.2f, 0.35f, Mathf.Clamp01(deep.r)), Mathf.Lerp(2.2f, 0.35f, Mathf.Clamp01(deep.g)), Mathf.Lerp(2.2f, 0.35f, Mathf.Clamp01(deep.b)));
				return waterStyle;
			}

			// Token: 0x04000766 RID: 1894
			public Color Scatter;

			// Token: 0x04000767 RID: 1895
			public Vector3 Sigma;

			// Token: 0x04000768 RID: 1896
			public float Rough;

			// Token: 0x04000769 RID: 1897
			public Color SkyTint;

			// Token: 0x0400076A RID: 1898
			public float UseBaseTex;
		}

		// Token: 0x02000056 RID: 86
		private struct Tracked
		{
			// Token: 0x0400076B RID: 1899
			public Renderer R;

			// Token: 0x0400076C RID: 1900
			public Material[] Originals;

			// Token: 0x0400076D RID: 1901
			public MaterialPropertyBlock Mpb;

			// Token: 0x0400076E RID: 1902
			public float BodyScale;

			// Token: 0x0400076F RID: 1903
			public bool ScaleProbed;

			// Token: 0x04000770 RID: 1904
			public float NextProbeAt;

			// Token: 0x04000771 RID: 1905
			public int LoggedGen;
		}
	}
}
