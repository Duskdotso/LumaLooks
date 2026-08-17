using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Logging;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200002D RID: 45
	internal sealed class RenderEngine
	{
		// Token: 0x060001A8 RID: 424 RVA: 0x000182D8 File Offset: 0x000164D8
		private static string[] BuildNames(string prefix, int n)
		{
			string[] array = new string[n];
			for (int i = 0; i < n; i++)
			{
				array[i] = prefix + i.ToString();
			}
			return array;
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x060001A9 RID: 425 RVA: 0x00018309 File Offset: 0x00016509
		// (set) Token: 0x060001AA RID: 426 RVA: 0x00018311 File Offset: 0x00016511
		public bool Enabled { get; private set; }

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x060001AB RID: 427 RVA: 0x0001831A File Offset: 0x0001651A
		public bool SawStereoCamera
		{
			get
			{
				return this._sawStereoCamera;
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x060001AC RID: 428 RVA: 0x00018322 File Offset: 0x00016522
		public int ShaderCount
		{
			get
			{
				if (this._shaders == null)
				{
					return 0;
				}
				return this._shaders.Count;
			}
		}

		// Token: 0x060001AD RID: 429 RVA: 0x00018339 File Offset: 0x00016539
		public void AttachMetalSurfaces(MetalSurfaces m)
		{
			this._metal = m;
		}

		// Token: 0x060001AE RID: 430 RVA: 0x00018342 File Offset: 0x00016542
		public void AttachDynamicLights(DynamicLights d)
		{
			this._dynamicLights = d;
		}

		// Token: 0x060001AF RID: 431 RVA: 0x0001834B File Offset: 0x0001654B
		public void AttachPlayerShadow(PlayerShadow p)
		{
			this._playerShadow = p;
		}

		// Token: 0x060001B0 RID: 432 RVA: 0x00018354 File Offset: 0x00016554
		public void AttachSkyShell(SkyShell s)
		{
			this._skyShell = s;
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x060001B1 RID: 433 RVA: 0x0001835D File Offset: 0x0001655D
		private bool MaskAvailable
		{
			get
			{
				return this._metal != null && this._metal.HasMasked && this._maskTierMats != null;
			}
		}

		// Token: 0x060001B2 RID: 434 RVA: 0x0001837F File Offset: 0x0001657F
		public void AttachTextGuard(TextGuard t)
		{
			this._textGuard = t;
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x060001B3 RID: 435 RVA: 0x00018388 File Offset: 0x00016588
		private bool TextMaskAvailable
		{
			get
			{
				return this._textGuard != null && this._textGuard.HasText && this._textMaskMat != null;
			}
		}

		// Token: 0x060001B4 RID: 436 RVA: 0x000183B0 File Offset: 0x000165B0
		private static Vector4 LinearTint(int r8, int g8, int b8)
		{
			Color linear = new Color((float)r8 / 255f, (float)g8 / 255f, (float)b8 / 255f).linear;
			return new Vector4(linear.r, linear.g, linear.b, 0f);
		}

		// Token: 0x060001B5 RID: 437 RVA: 0x000183FF File Offset: 0x000165FF
		private static float ResolveSunlightVisPow(float t)
		{
			if (t >= 0.5f)
			{
				return Mathf.Lerp(1f, 0.6f, 2f * t - 1f);
			}
			return Mathf.Lerp(2.4f, 1f, 2f * t);
		}

		// Token: 0x060001B6 RID: 438 RVA: 0x0001843C File Offset: 0x0001663C
		private static float ResolveSunlightFilterScale(float t)
		{
			if (t >= 0.5f)
			{
				return Mathf.Lerp(1f, 1.4f, 2f * t - 1f);
			}
			return Mathf.Lerp(0.25f, 1f, 2f * t);
		}

		// Token: 0x060001B7 RID: 439 RVA: 0x0001847C File Offset: 0x0001667C
		private static float ResolveSunlightPhaseG(Resolved r)
		{
			float raysSunBrightness = SkySystem.RaysSunBrightness;
			float num = Mathf.Min(0.88f, Mathf.Lerp(0.65f, 0.85f, r.SunlightClarity) + 0.12f * Mathf.Clamp01((raysSunBrightness - 4f) / 4f));
			if (WorldLight.SourceIsMoon)
			{
				num = Mathf.Max(0.3f, num * 0.55f);
			}
			num *= Mathf.Lerp(1f, 0.18f, Mathf.Clamp01(r.SunlightSideGlow));
			return Mathf.Max(0.05f, num);
		}

		// Token: 0x060001B8 RID: 440 RVA: 0x0001850C File Offset: 0x0001670C
		private static float ResolveSunlightFloorScale(Resolved r)
		{
			float raysSunBrightness = SkySystem.RaysSunBrightness;
			return Mathf.Min(1f, ((raysSunBrightness > 4f) ? (4f / raysSunBrightness) : 1f) * Mathf.Lerp(0.35f, 0.05f, r.SunlightClarity));
		}

		// Token: 0x060001B9 RID: 441 RVA: 0x00018558 File Offset: 0x00016758
		private static float ResolveSunlightIntensity(Resolved r, float rain)
		{
			float num = Mathf.Clamp(Mathf.Sqrt(SkySystem.RaysSunBrightness / 4f), 0.25f, 2.5f);
			return r.SunlightIntensity * num * (WorldLight.SourceIsMoon ? 1.6f : 1f) * (1f - rain);
		}

		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060001BA RID: 442 RVA: 0x000185AC File Offset: 0x000167AC
		// (remove) Token: 0x060001BB RID: 443 RVA: 0x000185E4 File Offset: 0x000167E4
		public event Action OnSelfDisabled;

		// Token: 0x060001BC RID: 444 RVA: 0x0001861C File Offset: 0x0001681C
		public RenderEngine(ManualLogSource log)
		{
			this._log = log;
			this._passes = new LumaPass[]
			{
				new LumaPass(this, Stage.Clouds, true),
				new LumaPass(this, Stage.DepthPrime, true),
				new LumaPass(this, Stage.Mask, false),
				new LumaPass(this, Stage.Ssao, true),
				new LumaPass(this, Stage.Ssr, true),
				new LumaPass(this, Stage.Ssgi, true),
				new LumaPass(this, Stage.TrueDark, true),
				new LumaPass(this, Stage.Sunlight, true),
				new LumaPass(this, Stage.Composite, true),
				new LumaPass(this, Stage.PlayerShadow, true),
				new LumaPass(this, Stage.Bloom, false),
				new LumaPass(this, Stage.Dof, true),
				new LumaPass(this, Stage.MotionBlur, true),
				new LumaPass(this, Stage.Uber, false),
				new LumaPass(this, Stage.LensFlare, true),
				new LumaPass(this, Stage.TextMask, true),
				new LumaPass(this, Stage.Fxaa, false),
				new LumaPass(this, Stage.Cas, false),
				new LumaPass(this, Stage.VideoFx, false)
			};
			this._uberDepthPass = new LumaPass(this, Stage.Uber, true);
			this._skyReplacePass = new SkyReplacePass(this);
			this._skyDiagCb = new Action<AsyncGPUReadbackRequest>(this.OnSkyDiagReadback);
		}

		// Token: 0x060001BD RID: 445 RVA: 0x000189A8 File Offset: 0x00016BA8
		public bool LoadBundle(string pluginDir)
		{
			byte[] bundleBytes = LumaEngineBehaviour.BundleBytes;
			string text = ((pluginDir == null) ? null : Path.Combine(pluginDir, "lumalooks.bundle"));
			if (bundleBytes == null && (text == null || !File.Exists(text)))
			{
				this._log.LogError("lumalooks.bundle not found at '" + (text ?? "(no bundle dir)") + "' and none was embedded. Luma Looks is DISABLED (no shaders). Build the Unity bundle (unity/build-bundle.ps1) and install it beside LumaLooks.dll.");
				this.Enabled = false;
				return false;
			}
			AssetBundle assetBundle = null;
			try
			{
				assetBundle = ((bundleBytes != null) ? AssetBundle.LoadFromMemory(bundleBytes) : AssetBundle.LoadFromFile(text));
				if (assetBundle == null)
				{
					this._log.LogError("Failed to load lumalooks.bundle (" + ((bundleBytes != null) ? "LoadFromMemory" : "LoadFromFile") + " returned null). Luma Looks DISABLED.");
					this.Enabled = false;
					return false;
				}
				foreach (Shader shader in assetBundle.LoadAllAssets<Shader>())
				{
					if (!(shader == null))
					{
						this._shaders[shader.name] = shader;
						if (!this._mats.ContainsKey(shader.name))
						{
							this._mats[shader.name] = new Material(shader)
							{
								hideFlags = (HideFlags)61
							};
						}
					}
				}
				this._log.LogInfo(string.Format("Loaded {0} shaders from lumalooks.bundle.", this._shaders.Count));
				foreach (Texture2D texture2D in assetBundle.LoadAllAssets<Texture2D>())
				{
					if (!(texture2D == null))
					{
						this._textures[texture2D.name] = texture2D;
					}
				}
				if (!this._textures.ContainsKey("MoonAlbedo"))
				{
					this._log.LogInfo("Bundle carries no MoonAlbedo texture — moon renders as the textureless glowing disc.");
				}
				if (this._shaders.ContainsKey("Hidden/LumaLooks/BloomUpsample"))
				{
					this._bloomUpMats = new Material[3];
					for (int j = 0; j < this._bloomUpMats.Length; j++)
					{
						this._bloomUpMats[j] = new Material(this._shaders["Hidden/LumaLooks/BloomUpsample"])
						{
							hideFlags = (HideFlags)61
						};
					}
				}
				if (this._shaders.ContainsKey("Hidden/LumaLooks/BloomDownsample"))
				{
					this._bloomDownMats = new Material[4];
					for (int k = 0; k < this._bloomDownMats.Length; k++)
					{
						this._bloomDownMats[k] = new Material(this._shaders["Hidden/LumaLooks/BloomDownsample"])
						{
							hideFlags = (HideFlags)61
						};
					}
				}
				if (this._shaders.ContainsKey("Hidden/LumaLooks/SSGI"))
				{
					this._ssgiGiMat = new Material(this._shaders["Hidden/LumaLooks/SSGI"])
					{
						hideFlags = (HideFlags)61
					};
				}
				if (this._shaders.ContainsKey("Hidden/LumaLooks/MetalMask"))
				{
					Shader shader2 = this._shaders["Hidden/LumaLooks/MetalMask"];
					float[] maskTierValues = MetalSurfaces.MaskTierValues;
					this._maskTierMats = new Material[maskTierValues.Length];
					for (int l = 0; l < maskTierValues.Length; l++)
					{
						this._maskTierMats[l] = new Material(shader2)
						{
							hideFlags = (HideFlags)61
						};
						this._maskTierMats[l].SetFloat(ShaderIds.MaskTier, maskTierValues[l]);
					}
					this._textMaskMat = new Material(shader2)
					{
						hideFlags = (HideFlags)61,
						name = "LumaTextGuardWriter"
					};
					this._textMaskMat.SetFloat(ShaderIds.MaskTier, 1f);
					this._textQuad = RenderEngine.BuildTextQuad();
				}
			}
			catch (Exception ex)
			{
				this._log.LogError(string.Format("Exception while loading shaders: {0}. Luma Looks DISABLED.", ex));
				this.Enabled = false;
				return false;
			}
			finally
			{
				try
				{
					if (assetBundle != null)
					{
						assetBundle.Unload(false);
					}
				}
				catch
				{
				}
			}
			this.Enabled = this._mats.Count > 0;
			return this.Enabled;
		}

		// Token: 0x060001BE RID: 446 RVA: 0x00018DA4 File Offset: 0x00016FA4
		private Material Mat(string name)
		{
			Material material;
			if (!this._mats.TryGetValue(name, out material))
			{
				return null;
			}
			return material;
		}

		// Token: 0x060001BF RID: 447 RVA: 0x00018DC4 File Offset: 0x00016FC4
		public Shader GetShader(string name)
		{
			Shader shader;
			if (!this._shaders.TryGetValue(name, out shader))
			{
				return null;
			}
			return shader;
		}

		// Token: 0x060001C0 RID: 448 RVA: 0x00018DE4 File Offset: 0x00016FE4
		public Texture2D GetTexture(string name)
		{
			Texture2D texture2D;
			if (!this._textures.TryGetValue(name, out texture2D))
			{
				return null;
			}
			return texture2D;
		}

		// Token: 0x060001C1 RID: 449 RVA: 0x00018E04 File Offset: 0x00017004
		public Material GetMaterial(string shaderName)
		{
			return this.Mat(shaderName);
		}

		// Token: 0x060001C2 RID: 450 RVA: 0x00018E10 File Offset: 0x00017010
		private static Mesh BuildTextQuad()
		{
			Mesh mesh = new Mesh
			{
				name = "LumaTextGuardQuad",
				hideFlags = (HideFlags)61
			};
			mesh.vertices = new Vector3[]
			{
				new Vector3(0f, 0f, 0f),
				new Vector3(1f, 0f, 0f),
				new Vector3(0f, 1f, 0f),
				new Vector3(1f, 1f, 0f)
			};
			mesh.triangles = new int[]
			{
				0, 2, 1, 1, 2, 3, 0, 1, 2, 1,
				3, 2
			};
			mesh.RecalculateBounds();
			mesh.UploadMeshData(true);
			return mesh;
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x060001C3 RID: 451 RVA: 0x00018ED4 File Offset: 0x000170D4
		public Settings CurrentSettings
		{
			get
			{
				return this._settings;
			}
		}

		// Token: 0x060001C4 RID: 452 RVA: 0x00018EDC File Offset: 0x000170DC
		public void ApplySettings(Settings s)
		{
			this._settings = s ?? Settings.BuildDefaults();
			this._dirty = true;
		}

		// Token: 0x060001C5 RID: 453 RVA: 0x00018EF7 File Offset: 0x000170F7
		private void RecomputeIfDirty()
		{
			if (!this._dirty)
			{
				return;
			}
			this._dirty = false;
			this._resolved[0].Compute(this._settings, false);
			this._resolved[1].Compute(this._settings, true);
		}

		// Token: 0x060001C6 RID: 454 RVA: 0x00018F38 File Offset: 0x00017138
		public void BeginCamera(Camera cam)
		{
			WorldLight.ReapplyGtAmbient();
			if (!this.Enabled || this._settings == null || !this._settings.Master)
			{
				return;
			}
			if (cam == null || cam.cameraType != CameraType.Game || !cam.enabled)
			{
				return;
			}
			bool stereoEnabled = cam.stereoEnabled;
			if (stereoEnabled)
			{
				this._sawStereoCamera = true;
			}
			if (!stereoEnabled && cam.targetTexture != null && (cam.targetTexture.width < 512 || cam.targetTexture.height < 512))
			{
				return;
			}
			this.RecomputeIfDirty();
			Shader.SetGlobalVector(ShaderIds.CloudCamPos, cam.transform.position);
			if (!stereoEnabled && this._sawStereoCamera && this._resolved[1].PerfBalanced)
			{
				if (!this._loggedMirrorSkip)
				{
					this._loggedMirrorSkip = true;
					this._log.LogInfo("VR: skipping the desktop mirror camera '" + cam.name + "' — the whole effect chain was being recorded and executed TWICE per frame (+8.2% GPU pixels, +100% CPU record cost, +128 mask draws) for a window the player cannot see. Set VR Performance mode to Quality (or turn the effect off) to restore the graded mirror.");
				}
				Resolved resolved = this._resolved[0];
				float night = AdaptiveGrade.Night;
				this.PushHazeGlobals(resolved, night);
				Shader.SetGlobalVector(ShaderIds.WaterSSRParamsGlobal, new Vector4(0f, 0f, MapSense.IsOutdoor ? 1f : 0f, (float)this.HalfDiv(false)));
				if (resolved.Letterbox > 0.01f)
				{
					UniversalAdditionalCameraData universalAdditionalCameraData = CameraExtensions.GetUniversalAdditionalCameraData(cam);
					ScriptableRenderer scriptableRenderer = ((universalAdditionalCameraData != null) ? universalAdditionalCameraData.scriptableRenderer : null);
					if (scriptableRenderer != null)
					{
						this._mirrorLetterbox.Ratio = resolved.Letterbox;
						scriptableRenderer.EnqueuePass(this._mirrorLetterbox);
					}
				}
				return;
			}
			Resolved resolved2 = this._resolved[stereoEnabled ? 1 : 0];
			Shader.SetGlobalFloat(ShaderIds.FrameIndex, (float)(Time.frameCount & 255));
			if (LumaDebug.RayDebug != this._lastRayDebugPushed)
			{
				this._lastRayDebugPushed = LumaDebug.RayDebug;
				Shader.SetGlobalFloat(ShaderIds.RayDebug, LumaDebug.RayDebug);
			}
			bool isOutdoor = MapSense.IsOutdoor;
			bool isBasement = MapSense.IsBasement;
			int num = (stereoEnabled ? 1 : 0);
			if (!this._loggedTarget[num])
			{
				this._loggedTarget[num] = true;
				this._log.LogInfo(string.Format("Target={0} cam='{1}' {2}x{3} ", new object[]
				{
					stereoEnabled ? "VR" : "DESKTOP",
					cam.name,
					cam.pixelWidth,
					cam.pixelHeight
				}) + string.Format("stereo={0} rt={1} anyEnabled={2} | ", cam.stereoEnabled, cam.targetTexture != null, resolved2.AnyEnabled) + string.Format("AO={0} SSR={1} GI={2} TD={3} Comp={4} Bloom={5} ", new object[] { resolved2.AoOn, resolved2.SsrOn, resolved2.GiOn, resolved2.TdOn, resolved2.CompositeOn, resolved2.BloomOn }) + string.Format("DoF={0} MB={1} Uber={2} FXAA={3} CAS={4}", new object[] { resolved2.DofOn, resolved2.MbOn, resolved2.UberOn, resolved2.FxaaOn, resolved2.CasOn }));
			}
			float night2 = AdaptiveGrade.Night;
			float rainFactor = RainSensor.RainFactor;
			bool flag = rainFactor > 0.001f;
			bool flag2 = flag || RainSensor.WetBuildup > 0.001f;
			bool flag3 = resolved2.PuddlesOn && flag && !isBasement;
			bool flag4 = ((resolved2.SsrOn && (!resolved2.SsrRainOnly || flag)) || flag3) && !isBasement;
			bool flag5 = resolved2.TdOn || (resolved2.WetOn && flag2) || (resolved2.PuddlesOn && flag2) || (resolved2.SsrOn && resolved2.SsrRainOnly && flag && !isBasement);
			bool flag6 = flag5;
			if (flag5 && stereoEnabled && resolved2.PerfBalanced)
			{
				flag6 = Time.frameCount - this._tdLastRunFrame[num] >= 2;
			}
			if (flag6)
			{
				this._tdLastRunFrame[num] = Time.frameCount;
			}
			bool flag7 = flag5 && Time.frameCount - this._tdLastRunFrame[num] <= 2;
			UniversalAdditionalCameraData universalAdditionalCameraData2 = null;
			cam.TryGetComponent<UniversalAdditionalCameraData>(out universalAdditionalCameraData2);
			PlayerShadow playerShadow = this._playerShadow;
			if (playerShadow != null)
			{
				playerShadow.NoteQualifyingCamera(universalAdditionalCameraData2, stereoEnabled);
			}
			float num2;
			float num3;
			int num4;
			bool flag8 = this.ResolveSunlight(resolved2, cam, universalAdditionalCameraData2, rainFactor, out num2, out num3, out num4);
			bool flag9 = resolved2.SunMoonOn && (resolved2.SunlightSurfaceLight > 0f || resolved2.SunlightPlayerShade > 0f);
			bool flag10 = resolved2.AoOn || flag4 || resolved2.GiOn || flag5 || resolved2.HazeOn || ((resolved2.WetOn || resolved2.StormOn) && flag2) || flag8 || flag9;
			bool pshadowContact = resolved2.PShadowContact;
			bool flag11 = this.MaskAvailable && (flag10 || pshadowContact);
			Shader.SetGlobalFloat(ShaderIds.MaskValid, flag11 ? 1f : 0f);
			bool flag12 = resolved2.FxaaOn && this.TextMaskAvailable;
			this._fxPixelRun = resolved2.FxPixelOn;
			this._fxCartoonRun = resolved2.FxCartoonOn;
			this._fxHalftoneRun = resolved2.FxHalftoneOn;
			this._fxScanRun = resolved2.FxScanOn;
			this._flareRunFrame = resolved2.FlareOn && isOutdoor && resolved2.FlareIntensity * (1f - rainFactor) >= 0.005f;
			this._streakRunFrame = false;
			bool flareRunFrame = this._flareRunFrame;
			this.PushHazeGlobals(resolved2, night2);
			this.PushWaterSsrGlobals(resolved2, stereoEnabled, flag4, rainFactor, isOutdoor);
			bool waterDepthNeeded = WaterSurfaces.WaterDepthNeeded;
			bool depthNeeded = SkyShell.DepthNeeded;
			bool flag13 = SkySystem.ScreenSpaceOn && this.Mat("Hidden/LumaLooks/SkyReplace") != null;
			if (flag13)
			{
				this.PushSkyReplaceUniforms(cam, stereoEnabled);
			}
			if (!resolved2.AnyEnabled && !waterDepthNeeded && !flag13 && !depthNeeded)
			{
				return;
			}
			if (universalAdditionalCameraData2 == null)
			{
				universalAdditionalCameraData2 = CameraExtensions.GetUniversalAdditionalCameraData(cam);
			}
			if (universalAdditionalCameraData2 == null)
			{
				return;
			}
			ScriptableRenderer scriptableRenderer2 = universalAdditionalCameraData2.scriptableRenderer;
			if (scriptableRenderer2 == null)
			{
				return;
			}
			if (waterDepthNeeded)
			{
				scriptableRenderer2.EnqueuePass(this._waterDepthPass);
			}
			if (depthNeeded)
			{
				scriptableRenderer2.EnqueuePass(this._shellDepthPass);
			}
			if (flag13)
			{
				this._skyReplacePass.IsVr = stereoEnabled;
				scriptableRenderer2.EnqueuePass(this._skyReplacePass);
			}
			if (!resolved2.AnyEnabled)
			{
				return;
			}
			this.UpdateSun(resolved2.HazeOn);
			this.LogSunlightOnce(resolved2, cam, stereoEnabled, flag8, num2, num3, num4);
			bool flag14 = stereoEnabled || cam.targetTexture == null;
			if (!flag14)
			{
				RenderTexture targetTexture = cam.targetTexture;
				if (targetTexture != null && targetTexture.height > 0 && (float)targetTexture.width / (float)targetTexture.height >= 1.5f)
				{
					flag14 = true;
				}
			}
			this.ApplyResolvedToMaterials(resolved2, night2, rainFactor, flag4, flag7, flag8, num2, flag14, flag12);
			this.UpdatePlayerShadow(resolved2, cam);
			if (resolved2.MbOn)
			{
				this.UpdateMotionBlurUniforms(cam, stereoEnabled);
			}
			if (resolved2.FlareOn && !this._flareLoggedTarget[num])
			{
				this._flareLoggedTarget[num] = true;
				Vector3 vector = new Vector3(this._sunDir.x, this._sunDir.y, this._sunDir.z);
				Vector3 vector2 = cam.transform.position + vector.normalized * 1000f;
				Vector3 vector3 = cam.WorldToViewportPoint(vector2);
				bool flag15 = vector3.z > 0f && vector3.x >= 0f && vector3.x <= 1f && vector3.y >= 0f && vector3.y <= 1f;
				float num5 = 0.2126f * this._sunColor.x + 0.7152f * this._sunColor.y + 0.0722f * this._sunColor.z;
				string text = ((resolved2.FlareMode < 0.5f) ? "Sun" : ((resolved2.FlareMode < 1.5f) ? "Moon" : "Both"));
				bool flag16 = resolved2.FlareMode >= 1.5f || (resolved2.FlareMode < 0.5f && !WorldLight.SourceIsMoon) || (resolved2.FlareMode >= 0.5f && resolved2.FlareMode < 1.5f && WorldLight.SourceIsMoon);
				Vector4 uniReplaceParams = SkySystem.UniReplaceParams2;
				bool flag17 = uniReplaceParams.w > 0.5f && uniReplaceParams.z > 0f;
				this._log.LogInfo(string.Concat(new string[]
				{
					string.Format("FLARE[{0}]: enabled=1 enqueued={1} ", stereoEnabled ? "VR" : "DESKTOP", this._flareRunFrame ? 1 : 0),
					string.Format("zoneOutdoor={0} sunUV=({1:0.##},{2:0.##},z{3:0.#}) onScreen={4} ", new object[]
					{
						isOutdoor ? 1 : 0,
						vector3.x,
						vector3.y,
						vector3.z,
						flag15 ? 1 : 0
					}),
					string.Format("intensity={0:0.##} mode={1} sourceIsMoon={2} ", resolved2.FlareIntensity, text, WorldLight.SourceIsMoon),
					string.Format("modeGateOpen={0} sunLum={1:0.###} ", flag16 ? 1 : 0, num5),
					string.Format("bandArmed={0} domeDist={1:0.#}m ", flag17 ? 1 : 0, uniReplaceParams.z),
					string.Format("streaks={0:0} len={1:0.##} disp={2:0.##} ", resolved2.FlareStreakCount, resolved2.FlareStreakLen, resolved2.FlareDispersion),
					string.Format("ghost={0:0.##} shimmer={1:0.##}", resolved2.FlareGhost, resolved2.FlareShimmer)
				}));
			}
			if (!this._bodyUvLogged && (SkyShell.Active || SkySystem.SunPassOn))
			{
				Vector3 position = cam.transform.position;
				Vector3 normalized = new Vector3(this._sunDir.x, this._sunDir.y, this._sunDir.z).normalized;
				Vector3 vector4 = cam.WorldToViewportPoint(position + normalized * 100000f);
				if (vector4.z > 0f)
				{
					if (!this._bodyUvArmed)
					{
						this._bodyUvArmed = true;
						this._bodyUvOrigin = position;
						this._bodyUvStartDir = normalized;
						this._bodyUvStart = new Vector2(vector4.x, vector4.y);
						this._bodyUvStartTime = Time.realtimeSinceStartup;
					}
					else
					{
						float num6 = Vector3.Distance(position, this._bodyUvOrigin);
						if (num6 >= 60f)
						{
							this._bodyUvLogged = true;
							float num7 = Vector2.Distance(new Vector2(vector4.x, vector4.y), this._bodyUvStart);
							float num8 = Vector3.Angle(this._bodyUvStartDir, normalized);
							float num9 = Time.realtimeSinceStartup - this._bodyUvStartTime;
							Vector4 uniReplaceParams2 = SkySystem.UniReplaceParams2;
							this._log.LogInfo(string.Concat(new string[]
							{
								string.Format("BODYUV[{0}]: walked={1:0.#}m ", stereoEnabled ? "VR" : "DESKTOP", num6),
								string.Format("over {0:0.#}s duv={1:0.####} ", num9, num7),
								string.Format("uv0=({0:0.###},{1:0.###}) ", this._bodyUvStart.x, this._bodyUvStart.y),
								string.Format("uv1=({0:0.###},{1:0.###}) sunArc={2:0.###}deg ", vector4.x, vector4.y, num8),
								string.Format("isMoon={0} ", WorldLight.SourceIsMoon),
								string.Format("elev={0:0.###} ", WorldLight.SunElevation),
								"owner=",
								SkyShell.Active ? "shell" : "pass2",
								" ",
								string.Format("bandArmed={0} ", (uniReplaceParams2.w > 0.5f && uniReplaceParams2.z > 0f) ? 1 : 0),
								string.Format("domeDist={0:0.#}m", uniReplaceParams2.z)
							}));
						}
					}
				}
			}
			this.EnqueueIf(scriptableRenderer2, Stage.Mask, flag11, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Ssao, resolved2.AoOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Ssr, flag4, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Ssgi, resolved2.GiOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.TrueDark, flag6, stereoEnabled);
			this._primeRunFrame = flag8 && this._playerShadow != null;
			this.EnqueueIf(scriptableRenderer2, Stage.DepthPrime, this._primeRunFrame, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Sunlight, flag8, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Composite, flag10, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.PlayerShadow, pshadowContact, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Bloom, resolved2.BloomOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Dof, resolved2.BlurStageOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.MotionBlur, resolved2.MbOn, stereoEnabled);
			bool flag18 = resolved2.UnderwaterOn && WaterSurfaces.GlobalUnderwater > 0.0001f;
			if (resolved2.UberOn || flag18)
			{
				LumaPass lumaPass = (flag18 ? this._uberDepthPass : this._passes[13]);
				lumaPass.IsVr = stereoEnabled;
				scriptableRenderer2.EnqueuePass(lumaPass);
			}
			this.EnqueueIf(scriptableRenderer2, Stage.LensFlare, flareRunFrame, stereoEnabled);
			bool flag19 = resolved2.CloudsResDiv > 1 && SkyShell.Active && this._skyShell != null;
			if (this._skyShell != null)
			{
				this._skyShell.HalfResDraw = flag19;
			}
			this.EnqueueIf(scriptableRenderer2, Stage.Clouds, flag19, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.TextMask, flag12, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Fxaa, resolved2.FxaaOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.Cas, resolved2.CasOn, stereoEnabled);
			this.EnqueueIf(scriptableRenderer2, Stage.VideoFx, resolved2.VideoFxOn, stereoEnabled);
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00019D58 File Offset: 0x00017F58
		private void EnqueueIf(ScriptableRenderer renderer, Stage stage, bool on, bool isVr)
		{
			if (!on)
			{
				return;
			}
			LumaPass lumaPass = this._passes[(int)stage];
			lumaPass.IsVr = isVr;
			renderer.EnqueuePass(lumaPass);
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00019D84 File Offset: 0x00017F84
		private static Vector4 ComputeAmbient()
		{
			AmbientMode ambientMode = RenderSettings.ambientMode;
			Color color;
			if (ambientMode != AmbientMode.Flat)
			{
				if (ambientMode == AmbientMode.Trilight)
				{
					color = RenderSettings.ambientLight.linear;
				}
				else
				{
					SphericalHarmonicsL2 ambientProbe = RenderSettings.ambientProbe;
					color = new Color(Mathf.Max(0f, ambientProbe[0, 0] * 0.2820948f), Mathf.Max(0f, ambientProbe[1, 0] * 0.2820948f), Mathf.Max(0f, ambientProbe[2, 0] * 0.2820948f));
				}
			}
			else
			{
				color = RenderSettings.ambientSkyColor.linear;
			}
			return new Vector4(color.r, color.g, color.b, 0f);
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00019E33 File Offset: 0x00018033
		private static float RainHazeDensity(Resolved r)
		{
			return r.HazeDensity * (1f + 0.5f * RainSensor.RainFactor);
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00019E50 File Offset: 0x00018050
		private void PushHazeGlobals(Resolved r, float night)
		{
			Shader.SetGlobalVector(ShaderIds.PuddlesGlobal, new Vector4(r.PuddleCoverage, r.PuddleRipples, r.PuddleMirror, r.PuddlesOn ? 1f : 0f));
			Shader.SetGlobalVector(ShaderIds.Puddles2Global, new Vector4(r.PuddleRippleSize, r.PuddleRippleSpeed, r.PuddleRippleCount, r.PuddleOpenArea));
			Shader.SetGlobalVector(ShaderIds.Puddles3Global, new Vector4(r.PuddleFogginess, 0f, 0f, 0f));
			Shader.SetGlobalVector(ShaderIds.HazeParamsGlobal, new Vector4(RenderEngine.RainHazeDensity(r), r.HazeStart, r.HazeHeightFalloff, r.HazeWisps));
			Shader.SetGlobalVector(ShaderIds.HazeParams2Global, new Vector4(r.HazeSunScatter, r.HazeSkyVeil, r.HazeMaxBrightness * (1f - 0.45f * night), r.HazeOn ? 1f : 0f));
			Shader.SetGlobalVector(ShaderIds.HazeTintGlobal, r.HazeTint);
			if (r.HazeOn || r.PuddlesOn)
			{
				this._ambientVec = RenderEngine.ComputeAmbient();
				this._ambientVec.w = r.Exposure + AdaptiveGrade.ExposureOffset;
				Shader.SetGlobalVector(ShaderIds.AmbientColorGlobal, this._ambientVec);
			}
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00019F9C File Offset: 0x0001819C
		private void PushWaterSsrGlobals(Resolved r, bool isVr, bool ssrRun, float rain, bool zoneOutdoor)
		{
			int num = (isVr ? 1 : 0);
			bool flag = Time.frameCount - this._ssrLastRunFrame[num] <= 2;
			if (ssrRun)
			{
				this._ssrLastRunFrame[num] = Time.frameCount;
			}
			Shader.SetGlobalFloat(ShaderIds.WaterOpaqueValidGlobal, WaterSurfaces.WaterDepthNeeded ? 1f : 0f);
			if (!WaterSurfaces.WaterDepthNeeded)
			{
				Shader.SetGlobalVector(ShaderIds.WaterSSRParamsGlobal, Vector4.zero);
				return;
			}
			RenderEngine.RtSet rtSet = this._rt[num];
			bool flag2 = flag && rtSet.SsrTex != null;
			if (rtSet.SsrTex != null)
			{
				Shader.SetGlobalTexture(ShaderIds.WaterSSRTexGlobal, rtSet.SsrTex);
			}
			Shader.SetGlobalVector(ShaderIds.WaterSSRParamsGlobal, new Vector4(flag2 ? 1f : 0f, r.SsrIntensity * (r.SsrRainOnly ? rain : 1f), zoneOutdoor ? 1f : 0f, (float)this.HalfDiv(isVr)));
		}

		// Token: 0x060001CC RID: 460 RVA: 0x0001A08C File Offset: 0x0001828C
		private void PushSkyReplaceUniforms(Camera cam, bool isVr)
		{
			Material material = this.Mat("Hidden/LumaLooks/SkyReplace");
			if (material == null)
			{
				return;
			}
			material.SetVector(ShaderIds.SkySunDir, SkySystem.UniSunDir);
			material.SetVector(ShaderIds.SkyParams, SkySystem.UniParams);
			material.SetVector(ShaderIds.SkyParams2, SkySystem.UniParams2);
			material.SetVector(ShaderIds.SkyParams3, SkySystem.UniParams3);
			material.SetVector(ShaderIds.SkyDayZenith, SkySystem.UniSkyDayZenith);
			material.SetVector(ShaderIds.SkyDayHorizon, SkySystem.UniSkyDayHorizon);
			material.SetFloat(ShaderIds.SkyDaySat, SkySystem.UniSkyDaySat);
			material.SetFloat(ShaderIds.SkyDayHue, SkySystem.UniSkyDayHue);
			material.SetVector(ShaderIds.SkyAuroraA, SkySystem.UniAuroraA);
			material.SetVector(ShaderIds.SkyAuroraB, SkySystem.UniAuroraB);
			material.SetVector(ShaderIds.SkyBodyParams, SkySystem.UniBodyParams);
			material.SetVector(ShaderIds.SkySunTint, SkySystem.UniSunTint);
			material.SetVector(ShaderIds.SkyMoonTint, SkySystem.UniMoonTint);
			material.SetVector(ShaderIds.CloudParams, SkySystem.UniCloudParams);
			material.SetVector(ShaderIds.CloudParams2, SkySystem.UniCloudParams2);
			material.SetVector(ShaderIds.CloudParams3, SkySystem.UniCloudParams3);
			material.SetVector(ShaderIds.CloudTint, SkySystem.UniCloudTint);
			Texture2D texture = this.GetTexture("MoonAlbedo");
			if (texture != null)
			{
				material.SetTexture(ShaderIds.MoonTex, texture);
			}
			Vector4 uniReplaceParams = SkySystem.UniReplaceParams2;
			material.SetVector(ShaderIds.SkyReplaceParams2, uniReplaceParams);
			int num = (isVr ? 1 : 0);
			string zoneName = MapSense.ZoneName;
			bool flag = uniReplaceParams.x > 0.5f;
			string[] array = (flag ? this._skyDiagZoneNight : this._skyDiagZone);
			this._skyDiagArmedNow = !this._skyDiagInFlight && SystemInfo.supportsAsyncGPUReadback && !string.Equals(array[num], zoneName, StringComparison.Ordinal);
			if (this._skyDiagArmedNow)
			{
				array[num] = zoneName;
				this._skyDiagIsNight = flag;
			}
			float farClipPlane = cam.farClipPlane;
			float backdropDistance = SkySystem.BackdropDistance;
			float num2 = backdropDistance;
			if (num2 > 0f && farClipPlane > 100f && num2 > farClipPlane * 0.9f)
			{
				num2 = farClipPlane * 0.9f;
			}
			this._skyReplaceBase = new Vector4(num2, SkySystem.ReplaceStrength, -0.02f, 0f);
			bool flag2 = material.passCount > 2;
			bool flag3 = material.passCount > 3;
			this._skyPass0Run = SkySystem.NightPassOn;
			this._skyPass2Run = SkySystem.SunPassOn && flag2;
			this._skyPass3Run = SkySystem.CloudPassOn && flag3;
			if (!flag2 && SkySystem.SunPassOn && !this._loggedSkyNoSunPass)
			{
				this._loggedSkyNoSunPass = true;
				this._log.LogWarning(string.Format("SKYREPLACE: the bundle's SkyReplace shader has only {0} passes — ", material.passCount) + "the sky-dome-spec §D daytime sun-only pass (index 2, 'LumaSkySunOnly') is MISSING, so no custom sun will draw over GT's sky dome. Rebuild the Unity bundle. The night-only replacement (pass 0) is unaffected.");
			}
			if (!flag3 && SkySystem.CloudPassOn && !this._loggedSkyNoCloudPass)
			{
				this._loggedSkyNoCloudPass = true;
				this._log.LogWarning(string.Format("SKYREPLACE: the bundle's SkyReplace shader has only {0} passes — ", material.passCount) + "the sky-page-rebuild §2 daytime CLOUD pass (index 3, 'LumaSkyClouds') is MISSING, so no clouds will draw over GT's sky dome. Rebuild the Unity bundle. The night sky (pass 0) and the sun (pass 2) are unaffected; at night pass 0 still draws clouds, because they live inside LumaEvaluateSky.");
			}
			if (string.Equals(this._skyLogZone[num], zoneName, StringComparison.Ordinal) && this._skyLogNight[num] == flag)
			{
				return;
			}
			this._skyLogZone[num] = zoneName;
			this._skyLogNight[num] = flag;
			Shader shader = material.shader;
			this._log.LogInfo(string.Concat(new string[]
			{
				"SKYREPLACE[",
				isVr ? "VR" : "DESKTOP",
				"]: stage ENQUEUED=1 method=",
				SkySystem.ModeName,
				" ",
				string.Format("strength={0:0.##} backdrop={1:0}m ", SkySystem.ReplaceStrength, backdropDistance),
				string.Format("eff={0:0}m farClip={1:0}m ", num2, farClipPlane),
				"(backdrop is now read by NO pass — rule 2 uses the measured dome band) ",
				string.Format("guard={0:0.###} cam='{1}' clearFlags={2} ", -0.02f, cam.name, cam.clearFlags),
				string.Format("mat=ok shaderSupported={0} passes={1} ", (shader != null && shader.isSupported) ? 1 : 0, material.passCount),
				string.Format("zone={0} outdoor={1} hasSky={2} ", zoneName, MapSense.IsOutdoor, MapSense.HasSky),
				"(zone gate REMOVED — the sky is replaced in EVERY map) | ",
				string.Format("nightWeight={0:0.###} bodyWeight={1:0.###} ", uniReplaceParams.x, uniReplaceParams.y),
				string.Format("moonAllowed={0:0} ", SkySystem.UniParams3.z),
				string.Format("cloudsOn={0:0} dayWeight={1:0.###} ", SkySystem.UniCloudParams2.z, SkySystem.UniCloudParams2.w),
				string.Format("pass0(full sky, NIGHT)={0} pass2(sun only, DAY)={1} ", this._skyPass0Run ? 1 : 0, this._skyPass2Run ? 1 : 0),
				string.Format("pass3(clouds, DAY)={0} ", this._skyPass3Run ? 1 : 0),
				string.Format("sunBrightness={0:0.##} moonBrightness={1:0.##} ", SkySystem.UniParams3.x, SkySystem.UniBodyParams.x),
				string.Format("glowFalloff={0:0.##} auroraSpeed={1:0.##} ", SkySystem.UniBodyParams.z, SkySystem.UniParams3.y),
				string.Format("sunElevY={0:0.###} dayFactor={1:0.###} ", WorldLight.SunElevation, WorldLight.DayFactor),
				string.Format("domeDist={0:0}m domeValid={1:0} ", uniReplaceParams.z, uniReplaceParams.w),
				"(domeValid=0 => pass 2 falls back to the backdrop distance) | diag=",
				this._skyDiagArmedNow ? (flag ? "armed NIGHT (readback this frame)" : "armed DAY (readback this frame)") : "already-measured"
			}));
		}

		// Token: 0x060001CD RID: 461 RVA: 0x0001A648 File Offset: 0x00018848
		private void OnSkyDiagReadback(AsyncGPUReadbackRequest req)
		{
			this._skyDiagInFlight = false;
			try
			{
				if (req.hasError)
				{
					this._log.LogWarning("SKYREPLACE DIAG: GPU readback failed — sky-pixel fraction unavailable (the stage-enqueued line above still proves the pass ran).");
				}
				else
				{
					NativeArray<Color32> data = req.GetData<Color32>(0);
					int length = data.Length;
					if (length <= 0)
					{
						this._log.LogWarning("SKYREPLACE DIAG: empty readback.");
					}
					else
					{
						double num = 0.0;
						double num2 = 0.0;
						int num3 = 0;
						for (int i = 0; i < length; i++)
						{
							Color32 color = data[i];
							float num4 = (float)color.a * 0.003921569f;
							num += (double)num4;
							if (num4 > 0.5f)
							{
								num3++;
								num2 += (0.2126 * (double)color.r + 0.7152 * (double)color.g + 0.0722 * (double)color.b) * 0.00392156862745098;
							}
						}
						double num5 = num / (double)length;
						double num6 = ((num3 > 0) ? (num2 / (double)num3) : 0.0);
						this._log.LogInfo(string.Concat(new string[]
						{
							"SKYREPLACE DIAG[",
							this._skyDiagIsNight ? "NIGHT" : "DAY",
							" slot]: ",
							string.Format("skyPixelFraction={0:0.####} (sampled {1} px at 1/16 res, ", num5, length),
							"measured by the material's OWN diag pass into an alpha-bearing RT — never via the camera colour, whose default HDR format has no alpha) ",
							string.Format("skyPixels={0} meanSkyLuminance={1:0.####} ", num3, num6),
							"verdict=",
							(num5 <= 0.0001) ? "NO SKY PIXELS CLASSIFIED — classifier/depth is the fault" : ((num6 <= 0.0001) ? "sky pixels found but the sky EVALUATION is black — LumaEvaluateSky/uniforms are the fault" : "classifier found sky pixels AND the sky evaluation is non-black — the pass itself is good; if the frame still looks unchanged the fault is composite/ordering")
						}));
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("SKYREPLACE DIAG: readback handling threw: " + ex.Message);
			}
		}

		// Token: 0x060001CE RID: 462 RVA: 0x0001A850 File Offset: 0x00018A50
		private void UpdateSun(bool hazeOn)
		{
			if (hazeOn)
			{
				Material material = this.Mat("Hidden/LumaLooks/SceneComposite");
				if (material != null)
				{
					material.SetVector(ShaderIds.AmbientColor, this._ambientVec);
				}
			}
			Light light = WorldLight.ActiveSun;
			if (light == null)
			{
				light = RenderSettings.sun;
			}
			if (light == null)
			{
				Material material2 = this.Mat("Hidden/LumaLooks/SceneComposite");
				if (material2 != null)
				{
					material2.SetVector(ShaderIds.SunDirWS, RenderEngine.SunlessParkedDir);
					material2.SetVector(ShaderIds.SunColor, Vector4.zero);
				}
				Material material3 = this.Mat("Hidden/LumaLooks/LensFlare");
				if (material3 != null)
				{
					material3.SetVector(ShaderIds.SunDirWS, RenderEngine.SunlessParkedDir);
					material3.SetVector(ShaderIds.SunColor, Vector4.zero);
				}
				Material material4 = this.Mat("Hidden/LumaLooks/Sunlight");
				if (material4 != null)
				{
					material4.SetVector(ShaderIds.SunDirWS, RenderEngine.SunlessParkedDir);
					material4.SetVector(ShaderIds.RayDirWS, RenderEngine.SunlessParkedDir);
					material4.SetVector(ShaderIds.SunColor, Vector4.zero);
				}
				return;
			}
			Vector3 vector = WorldLight.ResolvedSunDir;
			if (vector.sqrMagnitude < 1E-08f)
			{
				vector = -light.transform.forward;
			}
			this._sunDir = new Vector4(vector.x, vector.y, vector.z, 0f);
			Color color = light.color.linear * light.intensity;
			this._sunColor = new Vector4(color.r, color.g, color.b, 0f);
			Vector3 rayDir = WorldLight.RayDir;
			Vector4 vector2 = new Vector4(rayDir.x, rayDir.y, rayDir.z, 0f);
			Material material5 = this.Mat("Hidden/LumaLooks/SceneComposite");
			if (material5 != null)
			{
				material5.SetVector(ShaderIds.SunDirWS, this._sunDir);
				material5.SetVector(ShaderIds.SunColor, this._sunColor);
				material5.SetVector(ShaderIds.RayDirWS, vector2);
			}
			Material material6 = this.Mat("Hidden/LumaLooks/LensFlare");
			if (material6 != null)
			{
				material6.SetVector(ShaderIds.SunDirWS, this._sunDir);
				material6.SetVector(ShaderIds.SunColor, this._sunColor);
			}
			Material material7 = this.Mat("Hidden/LumaLooks/Sunlight");
			if (material7 != null)
			{
				material7.SetVector(ShaderIds.SunDirWS, this._sunDir);
				material7.SetVector(ShaderIds.SunColor, this._sunColor);
				material7.SetVector(ShaderIds.RayDirWS, vector2);
			}
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0001AADC File Offset: 0x00018CDC
		private void HealUrpAtlasPreconditions(UniversalRenderPipelineAsset urp, ref RenderEngine.SunAtlasProbe p, Resolved rCam)
		{
			bool flag = false;
			try
			{
				flag = XRSettings.isDeviceActive;
			}
			catch
			{
			}
			Resolved resolved = this._resolved[flag ? 1 : 0];
			if (!resolved.SunlightOn || !resolved.SunMoonOn || resolved.SunlightIntensity <= 0f)
			{
				return;
			}
			if (!RenderEngine._fiHealResolved)
			{
				RenderEngine._fiHealResolved = true;
				try
				{
					RenderEngine._fiHealRenderMode = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightRenderingMode", BindingFlags.Instance | BindingFlags.NonPublic);
					RenderEngine._fiHealMainShadows = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightShadowsSupported", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				catch
				{
					RenderEngine._fiHealRenderMode = null;
					RenderEngine._fiHealMainShadows = null;
				}
			}
			if (p.Mode != LightRenderingMode.PerPixel && RenderEngine._fiHealRenderMode != null)
			{
				try
				{
					RenderEngine._fiHealRenderMode.SetValue(urp, LightRenderingMode.PerPixel);
					p.Mode = urp.mainLightRenderingMode;
					if (p.Mode == LightRenderingMode.PerPixel)
					{
						this._flapsMode++;
					}
				}
				catch
				{
				}
			}
			if (!p.Sup && RenderEngine._fiHealMainShadows != null)
			{
				try
				{
					RenderEngine._fiHealMainShadows.SetValue(urp, true);
					p.Sup = urp.supportsMainLightShadows;
					if (p.Sup)
					{
						this._flapsSup++;
					}
				}
				catch
				{
				}
			}
			if (p.Dist < 22f)
			{
				try
				{
					urp.shadowDistance = Mathf.Max(rCam.SunlightReach, 22f);
					p.Dist = urp.shadowDistance;
					if (p.Dist >= 22f)
					{
						this._flapsDist++;
					}
				}
				catch
				{
				}
			}
			if (rCam.SunlightOn && urp != null)
			{
				try
				{
					if (urp.shadowCascadeCount != 4)
					{
						urp.shadowCascadeCount = 4;
						if (urp.shadowCascadeCount == 4)
						{
							this._flapsCasc++;
						}
					}
					Vector3 vector = PlayerShadow.SunlightCascadeSplit(Mathf.Max(rCam.SunlightReach, 22f));
					if (urp.cascade4Split != vector)
					{
						urp.cascade4Split = vector;
					}
				}
				catch
				{
				}
			}
			if (rCam.SunlightOn && WorldLight.SunArcActive)
			{
				try
				{
					Light activeSun = WorldLight.ActiveSun;
					Vector3 rayDir = WorldLight.RayDir;
					if (activeSun != null && rayDir.sqrMagnitude > 1E-08f)
					{
						Vector3 normalized = rayDir.normalized;
						Vector3 vector2 = ((Mathf.Abs(normalized.y) > 0.999f) ? Vector3.forward : Vector3.up);
						Quaternion quaternion = Quaternion.LookRotation(-normalized, vector2);
						if (Quaternion.Angle(activeSun.transform.rotation, quaternion) > 0.5f)
						{
							activeSun.transform.rotation = quaternion;
							this._flapsRot++;
						}
					}
				}
				catch
				{
				}
			}
			int num = this._flapsMode + this._flapsSup + this._flapsDist + this._flapsCasc + this._flapsRot;
			if (num != this._flapsLoggedTotal && num > 0)
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (this._flapsLoggedTotal < 0 || realtimeSinceStartup >= this._nextFlapLogAt)
				{
					this._flapsLoggedTotal = num;
					this._nextFlapLogAt = realtimeSinceStartup + 30f;
					this._log.LogInfo(string.Concat(new string[]
					{
						"SUNLIGHT KEEP-ALIVE: URP-asset tug-of-war — re-asserted in ",
						string.Format("beginCameraRendering (last-writer-wins) mode×{0} ", this._flapsMode),
						string.Format("sup×{0} dist×{1} casc×{2} ", this._flapsSup, this._flapsDist, this._flapsCasc),
						string.Format("rot×{0} since engage. ", this._flapsRot),
						"Something (GT / URP asset churn) resets the main-light mode after the Update-side keep-alive; each reset used to drop the surface-sun patch for one frame — the walls/ground 'exposure flicker'. Counters climbing with NO visible flicker = healthy (we win the war); flicker visible WITH counters climbing = the reflection write stopped landing (URP field renamed)."
					}));
				}
			}
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x0001AEB8 File Offset: 0x000190B8
		private bool ResolveSunlight(Resolved r, Camera cam, UniversalAdditionalCameraData data, float rain, out float effReach, out float shadowDist, out int reason)
		{
			effReach = 0f;
			shadowDist = 0f;
			if (!r.SunlightOn)
			{
				reason = 1;
				return false;
			}
			if (!r.SunMoonOn)
			{
				reason = 7;
				return false;
			}
			if (this.Mat("Hidden/LumaLooks/Sunlight") == null)
			{
				reason = 2;
				return false;
			}
			if (RenderEngine.ResolveSunlightIntensity(r, rain) < 0.005f && 0.6f * r.SunlightSurfaceLight <= 0f)
			{
				reason = 3;
				return false;
			}
			if (!RenderEngine.SunSourceExists())
			{
				reason = 4;
				return false;
			}
			if (WorldLight.SunElevation <= 0f && (!WorldLight.SunArcActive || WorldLight.ResolvedSunDir.y <= 0f))
			{
				reason = 5;
				return false;
			}
			shadowDist = this.MainLightShadowDistance(cam, data, r);
			if (shadowDist <= 0f)
			{
				reason = 6;
				return false;
			}
			effReach = r.SunlightReach;
			if (effReach <= 0f)
			{
				reason = 6;
				return false;
			}
			reason = 0;
			return true;
		}

		// Token: 0x060001D1 RID: 465 RVA: 0x0001AFA3 File Offset: 0x000191A3
		private static bool SunSourceExists()
		{
			return WorldLight.ActiveSun != null || RenderSettings.sun != null;
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x0001AFC0 File Offset: 0x000191C0
		private float MainLightShadowDistance(Camera cam, UniversalAdditionalCameraData data, Resolved r)
		{
			RenderEngine.SunAtlasProbe sunAtlasProbe = default(RenderEngine.SunAtlasProbe);
			sunAtlasProbe.CamRenderShadows = data == null || data.renderShadows;
			UniversalRenderPipelineAsset universalRenderPipelineAsset = ActiveUrpAsset.Current;
			sunAtlasProbe.Asset = universalRenderPipelineAsset;
			if (universalRenderPipelineAsset != null)
			{
				sunAtlasProbe.Sup = universalRenderPipelineAsset.supportsMainLightShadows;
				sunAtlasProbe.Dist = universalRenderPipelineAsset.shadowDistance;
				sunAtlasProbe.Mode = universalRenderPipelineAsset.mainLightRenderingMode;
				if (sunAtlasProbe.Mode != LightRenderingMode.PerPixel || !sunAtlasProbe.Sup || sunAtlasProbe.Dist < 22f)
				{
					this.HealUrpAtlasPreconditions(universalRenderPipelineAsset, ref sunAtlasProbe, r);
				}
			}
			Light light = WorldLight.ActiveSun;
			if (light == null)
			{
				light = RenderSettings.sun;
			}
			sunAtlasProbe.Sun = light;
			if (light != null)
			{
				sunAtlasProbe.SunActive = light.isActiveAndEnabled;
				sunAtlasProbe.SunShadows = light.shadows;
			}
			float num = 0f;
			if (cam == null)
			{
				sunAtlasProbe.Sub = 1;
			}
			else if (!sunAtlasProbe.CamRenderShadows)
			{
				sunAtlasProbe.Sub = 2;
			}
			else if (universalRenderPipelineAsset == null)
			{
				sunAtlasProbe.Sub = 3;
			}
			else if (!sunAtlasProbe.Sup)
			{
				sunAtlasProbe.Sub = 4;
			}
			else if (sunAtlasProbe.Mode != LightRenderingMode.PerPixel)
			{
				sunAtlasProbe.Sub = 5;
			}
			else if (light == null)
			{
				sunAtlasProbe.Sub = 6;
			}
			else if (!sunAtlasProbe.SunActive)
			{
				sunAtlasProbe.Sub = 7;
			}
			else if (sunAtlasProbe.SunShadows == null)
			{
				sunAtlasProbe.Sub = 8;
			}
			else
			{
				float num2 = Mathf.Min(sunAtlasProbe.Dist, cam.farClipPlane);
				if (num2 >= cam.nearClipPlane)
				{
					sunAtlasProbe.Sub = 0;
					num = num2;
				}
				else
				{
					sunAtlasProbe.Sub = 9;
				}
			}
			this._atlasProbe = sunAtlasProbe;
			return num;
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x0001B174 File Offset: 0x00019374
		private void LogSunlightOnce(Resolved r, Camera cam, bool isVr, bool run, float effReach, float shadowDist, int reason)
		{
			if (!r.SunlightOn)
			{
				return;
			}
			int num = (isVr ? 1 : 0);
			int handle = SceneManager.GetActiveScene().handle;
			int num2 = ((reason == 6) ? this._atlasProbe.Sub : 0);
			if (this._sunlightLogScene[num] == handle && this._sunlightLogReason[num] == reason && this._sunlightLogSub[num] == num2)
			{
				return;
			}
			this._sunlightLogScene[num] = handle;
			this._sunlightLogReason[num] = reason;
			this._sunlightLogSub[num] = num2;
			Vector3 vector = new Vector3(this._sunDir.x, this._sunDir.y, this._sunDir.z);
			float num3 = Mathf.Asin(Mathf.Clamp(vector.y, -1f, 1f)) * 57.29578f;
			Light activeSun = WorldLight.ActiveSun;
			string text = "";
			if (reason == 6 || reason == 0)
			{
				RenderEngine.SunAtlasProbe atlasProbe = this._atlasProbe;
				text = string.Concat(new string[]
				{
					" | atlas read-backs (LIVE, from the SAME asset the gate used — the shared ",
					string.Format("ActiveUrpAsset resolver the keep-alive patches through): sup={0} ", atlasProbe.Sup),
					string.Format("dist={0:0.#}m mode={1} ", atlasProbe.Dist, atlasProbe.Mode),
					"asset='",
					(atlasProbe.Asset != null) ? atlasProbe.Asset.name : "(none)",
					"' ",
					string.Format("camRenderShadows={0} ", atlasProbe.CamRenderShadows),
					string.Format("sun='{0}' sunShadows={1} ", (atlasProbe.Sun != null) ? atlasProbe.Sun.name : "(none)", atlasProbe.SunShadows),
					string.Format("sunActive={0}", atlasProbe.SunActive)
				});
			}
			string text2 = ((num2 == 1) ? "camera is null" : ((num2 == 2) ? "the CAMERA's URP Render Shadows toggle is OFF (UniversalAdditionalCameraData.renderShadows=false — URP zeroes maxShadowDistance for this camera). Since 2026-07-23 the keep-alive LIFTS this per-camera flag (PlayerShadow camera-lift: original cached per camera, restored on effect-off/dispose/camera death), so this verdict persisting means the lift is NOT taking effect for this camera — the read-back still says false; check the PlayerShadow 'camera ... Render Shadows LIFTED' lines for which cameras it reached" : ((num2 == 3) ? "no URP asset is rendering (GraphicsSettings.currentRenderPipeline is not a UniversalRenderPipelineAsset)" : ((num2 == 4) ? "supportsMainLightShadows=false on the active asset — if the keep-alive is engaged this means its reflection write (m_MainLightShadowsSupported) is NOT landing" : ((num2 == 5) ? "mainLightRenderingMode is not PerPixel — URP publishes NO main-light atlas then; the keep-alive forces PerPixel by reflection (m_MainLightRenderingMode) from Tick AND (2026-08-04) re-asserts it in beginCameraRendering when a frame reads regressed (the exposure-flicker fix — see the SUNLIGHT KEEP-ALIVE tug-of-war line's flap counters), so this persisting means the reflection write is NOT landing in EITHER place (URP field renamed?)" : ((num2 == 6) ? "no sun light resolved (WorldLight.ActiveSun and RenderSettings.sun both null)" : ((num2 == 7) ? "the resolved sun light is disabled/inactive" : ((num2 == 8) ? "the resolved sun has Light.shadows=None (URP takes SetupForEmptyRendering — empty atlas). The keep-alive lifts None->Soft on the SAME ActiveSun??RenderSettings.sun chain, so if this persists check which light each side saw in the read-backs" : ((num2 == 9) ? "min(shadowDistance, farClipPlane) < nearClipPlane — URP zeroes the atlas range" : "unknown")))))))));
			string text3 = ((reason == 0) ? "running" : ((reason == 1) ? "OFF (effect disabled, or disabled for this target)" : ((reason == 7) ? "OFF (sunMoon disabled — rays need a visible sun/moon; enable Sun & Moon for this target. The shafts must emanate from the DRAWN disc, so the volumetric stage is refused while no body is present — zero cost: nothing enqueued, no shadow atlas kept alive and no per-camera Render-Shadows lift)" : ((reason == 2) ? "OFF (Hidden/LumaLooks/Sunlight is MISSING from the bundle — rebuild unity/build-bundle.ps1; BundleBuilder.ExpectedShaders must list it)" : ((reason == 3) ? "OFF (BOTH the resolved air intensity is below the 0.005 no-enqueue threshold AND surfaceLight is 0 — SURFACE-PATCH-REWORK decoupled the surface patch, so the stage now runs whenever surfaceLight > 0 even at air intensity ~0; both must be off for the exact zero-cost no-op. Air ~0 alone, or full rain fading the shafts out (rays-realism2 spec E), no longer skips the stage while surfaceLight brightens surfaces)" : ((reason == 4) ? "OFF (no directional light in the scene at all)" : ((reason == 5) ? "OFF (below the horizon with NO mirrored moon — Follow Game night: the map's own light is under the horizon and no stand-in body exists. Under Time of Day / Real Time the arc mirrors the render direction to the MOON after dusk and MOON RAYS run instead — intensity × moon factor, warmth clamped cool)" : ("OFF (no usable URP main-light shadow atlas — FAILING PRECONDITION: " + text2 + ". NOTE the record-time backstop catches three MORE causes this CPU gate cannot see — the camera's culling mask excluding the sun, no shadow caster visible to the light, or an invalid cascade slice — and those log a separate SUNLIGHT warning about URP's 1x1 EMPTY shadow map. There is NO screen-space fallback march by design.)"))))))));
			UniversalRenderPipelineAsset universalRenderPipelineAsset = ActiveUrpAsset.Current;
			float rainFactor = RainSensor.RainFactor;
			float num4 = RenderEngine.ResolveSunlightIntensity(r, rainFactor);
			this._log.LogInfo(string.Concat(new string[]
			{
				"SUNLIGHT[",
				isVr ? "VR" : "DESKTOP",
				"]: ",
				text3,
				" | ",
				string.Format("sunDirWS=({0:0.###},{1:0.###},{2:0.###}) elev={3:0.#}deg ", new object[] { vector.x, vector.y, vector.z, num3 }),
				string.Format("trueSolarElev={0:0.###} light='{1}' ", WorldLight.SunElevation, (activeSun != null) ? activeSun.name : "(none)"),
				string.Format("arc={0} isMoon={1} ", WorldLight.SunArcActive ? 1 : 0, WorldLight.SourceIsMoon),
				"(WORLD-ANCHORED: this vector is NOT derived from cam.transform anywhere in its chain — UpdateSun takes no Camera argument — so rotating or strafing cannot move a shaft) | ",
				string.Format("reach ui={0:0.#}m eff={1:0.#}m shadowDist={2:0.#}m ", r.SunlightReach, effReach, shadowDist),
				"cascades=",
				(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.shadowCascadeCount.ToString() : "?",
				" dist=",
				(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.shadowDistance.ToString("0.#") : "?",
				"m ",
				string.Format("farClip={0:0}m ", cam.farClipPlane),
				"(eff = min(ui, shadowDist); the keep-alive maintains shadowDistance = max(ui reach, 22) + 4 near-biased cascades while sunlight is wanted — rays-realism2 §A — so eff follows the slider instead of pinning at the 22 m player-shadow bubble; marching past the last cascade has NO occlusion data and URP returns 'lit' there, which would paint an unshadowed glow bed) | ",
				string.Format("intensity={0:0.##} resolved={1:0.###} ", r.SunlightIntensity, num4),
				string.Format("(x radianceMul={0:0.##} ", Mathf.Clamp(SkySystem.RaysSunBrightness / 4f, 0.25f, 2.5f)),
				string.Format("x moon={0:0.##} x (1-rain)) ", WorldLight.SourceIsMoon ? 1.6f : 1f),
				string.Format("rain={0:0.00} sigmaT={1:0.####}/m ", rainFactor, r.SunlightSigmaT),
				"(density x 0.03, NO floor — density 0 means a genuinely clear medium: T=1, scene undimmed, shafts purely additive) ",
				string.Format("steps={0:0} div={1} vrBalanced={2} ", r.SunlightSteps, this.HalfDiv(isVr), r.PerfBalanced ? 1 : 0),
				string.Format("thickness={0:0.##} visPow={1:0.##} ", r.SunlightRayThickness, RenderEngine.ResolveSunlightVisPow(r.SunlightRayThickness)),
				string.Format("filterScale={0:0.##} ", RenderEngine.ResolveSunlightFilterScale(r.SunlightRayThickness)),
				string.Format("streakGamma={0:0.##} ", Mathf.Lerp(2.6f, 0.9f, r.SunlightRayThickness)),
				string.Format("stepFloor={0} ", (r.SunlightRayThickness < 0.34f) ? 1 : 0),
				string.Format("enqueued={0} lane_z={1}{2}", run ? 1 : 0, run ? 1 : 0, text)
			}));
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0001B75C File Offset: 0x0001995C
		private void UpdatePlayerShadow(Resolved r, Camera cam)
		{
			if (!r.PShadowContact)
			{
				return;
			}
			Material material = this.Mat("Hidden/LumaLooks/PlayerShadow");
			if (material == null)
			{
				return;
			}
			float num = (WorldLight.SourceIsMoon ? 0.4f : 1f);
			num *= Mathf.Lerp(0.55f, 1f, Mathf.Clamp01(this._sunDir.y));
			material.SetFloat(ShaderIds.PShadowIntensity, r.PShadowIntensity * num);
			material.SetFloat(ShaderIds.PShadowSoftness, r.PShadowSoftness);
			Vector4 vector = new Vector4(this._sunDir.x, this._sunDir.y, this._sunDir.z, 0f);
			Vector3 vector2;
			if (this._sunDir.y <= 0.05f && this._dynamicLights != null && cam != null && this._dynamicLights.TryGetNearestLightPos(cam.transform.position, out vector2))
			{
				vector = new Vector4(vector2.x, vector2.y, vector2.z, 1f);
			}
			material.SetVector(ShaderIds.PShadowLight, vector);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0001B878 File Offset: 0x00019A78
		private void UpdateMotionBlurUniforms(Camera cam, bool isVr)
		{
			Material material = this.Mat("Hidden/LumaLooks/MotionBlur");
			if (material == null)
			{
				return;
			}
			int instanceID = cam.GetInstanceID();
			RenderEngine.PrevVp prevVp;
			this._prevVp.TryGetValue(instanceID, out prevVp);
			Matrix4x4 matrix4x;
			Matrix4x4 matrix4x2;
			if (isVr)
			{
				matrix4x = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix(0), true) * cam.GetStereoViewMatrix(0);
				matrix4x2 = GL.GetGPUProjectionMatrix(cam.GetStereoProjectionMatrix((Camera.StereoscopicEye)1), true) * cam.GetStereoViewMatrix((Camera.StereoscopicEye)1);
			}
			else
			{
				matrix4x = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true) * cam.worldToCameraMatrix;
				matrix4x2 = matrix4x;
			}
			if (!prevVp.has)
			{
				prevVp.vp0 = matrix4x;
				prevVp.vp1 = matrix4x2;
			}
			material.SetMatrix(ShaderIds.PrevVP0, prevVp.vp0);
			material.SetMatrix(ShaderIds.PrevVP1, prevVp.vp1);
			int frameCount = Time.frameCount;
			this._prevVp[instanceID] = new RenderEngine.PrevVp
			{
				vp0 = matrix4x,
				vp1 = matrix4x2,
				has = true,
				frame = frameCount
			};
			this.PrunePrevVp(frameCount);
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0001B988 File Offset: 0x00019B88
		private void PrunePrevVp(int frame)
		{
			if (frame < this._prevVpPruneAt || this._prevVp.Count <= 8)
			{
				return;
			}
			this._prevVpPruneAt = frame + 600;
			this._prevVpStale.Clear();
			foreach (KeyValuePair<int, RenderEngine.PrevVp> keyValuePair in this._prevVp)
			{
				if (frame - keyValuePair.Value.frame > 300)
				{
					this._prevVpStale.Add(keyValuePair.Key);
				}
			}
			for (int i = 0; i < this._prevVpStale.Count; i++)
			{
				this._prevVp.Remove(this._prevVpStale[i]);
			}
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0001BA5C File Offset: 0x00019C5C
		private void ApplyResolvedToMaterials(Resolved r, float night, float rain, bool ssrRun, bool coverageValid, bool sunlightRun, float sunlightReach, bool displayTarget, bool textMaskRun)
		{
			Material material = this.Mat("Hidden/LumaLooks/SSAO");
			if (material != null)
			{
				material.SetFloat(ShaderIds.AOIntensity, r.AoIntensity);
				material.SetFloat(ShaderIds.AORadius, r.AoRadius);
				material.SetFloat(ShaderIds.AOPower, r.AoPower);
				material.SetFloat(ShaderIds.AOSamples, r.AoSamples);
			}
			Material material2 = this.Mat("Hidden/LumaLooks/SSR");
			if (material2 != null)
			{
				material2.SetFloat(ShaderIds.SSRIntensity, r.SsrIntensity * (r.SsrRainOnly ? rain : 1f));
				material2.SetFloat(ShaderIds.SSRMaxDist, r.SsrMaxDist);
				material2.SetFloat(ShaderIds.SSRSteps, r.SsrSteps);
				material2.SetFloat(ShaderIds.SSRBlur, r.SsrBlur);
				material2.SetFloat(ShaderIds.SSRSurfaceAware, r.SsrSurfaceAware);
				material2.SetFloat(ShaderIds.SSRMetalSharp, r.SsrMetalSharp);
				material2.SetFloat(ShaderIds.SSRCoverageValid, coverageValid ? 1f : 0f);
				material2.SetFloat(ShaderIds.SSRRainOnly, r.SsrRainOnly ? 1f : 0f);
			}
			bool isForest = MapSense.IsForest;
			Shader.SetGlobalFloat(ShaderIds.StormFactorGlobal, (r.StormOn && isForest) ? (r.StormStrength * rain) : 0f);
			float num = (sunlightRun ? RenderEngine.ResolveSunlightIntensity(r, rain) : 0f);
			float num2 = RenderEngine.ResolveSunlightPhaseG(r);
			float num3 = RenderEngine.ResolveSunlightFloorScale(r);
			float num4 = Mathf.Clamp(Mathf.Sqrt(SkySystem.RaysBodySize / 1.2f), 0.75f, 1.5f);
			float num5 = Mathf.Clamp(Mathf.Sqrt(SkySystem.RaysBodyBrightness / 4f), 0.5f, 2.5f) * num4;
			float num6 = (r.SunMoonOn ? Mathf.Min(0.6f * r.SunlightSurfaceLight * num5, 1.5f) : 0f);
			Vector4 vector = new Vector4(num2, num3, num6, RenderEngine.ResolveSunlightVisPow(r.SunlightRayThickness));
			float num7 = (WorldLight.SourceIsMoon ? Mathf.Min(r.SunlightWarmth, 0.2f) : r.SunlightWarmth);
			Vector4 vector2 = (WorldLight.SourceIsMoon ? Vector4.Lerp(RenderEngine.SunlightTintMoon, RenderEngine.SunlightTintWarm, num7) : Vector4.Lerp(RenderEngine.SunlightTintCool, RenderEngine.SunlightTintWarm, num7));
			vector2.w = RenderEngine.ResolveSunlightFilterScale(r.SunlightRayThickness);
			Material material3 = this.Mat("Hidden/LumaLooks/Sunlight");
			if (material3 != null)
			{
				material3.SetVector(ShaderIds.SunlightParams, new Vector4(num, sunlightReach, r.SunlightSigmaT, r.SunlightSteps));
				material3.SetVector(ShaderIds.SunlightTint, vector2);
				material3.SetVector(ShaderIds.SunlightParams2, vector);
				material3.SetVector(ShaderIds.SunlightParams3, new Vector4(Mathf.Clamp01(r.SunlightShimmer), 0f, 0f, 0f));
				material3.SetVector(ShaderIds.SkyReplaceParams2, SkySystem.UniReplaceParams2);
			}
			Material ssgiGiMat = this._ssgiGiMat;
			if (ssgiGiMat != null)
			{
				ssgiGiMat.SetFloat(ShaderIds.GIIntensity, r.GiIntensity);
				ssgiGiMat.SetFloat(ShaderIds.GIRadius, r.GiRadius);
				ssgiGiMat.SetFloat(ShaderIds.GIRays, r.GiRays);
				ssgiGiMat.SetFloat(ShaderIds.GIColorBleed, r.GiColorBleed);
				ssgiGiMat.SetFloat(ShaderIds.GIEmissive, r.GiEmissive * 3f);
				ssgiGiMat.SetFloat(ShaderIds.GISharpness, r.GiSharpness);
			}
			Material material4 = this.Mat("Hidden/LumaLooks/SSGI");
			if (material4 != null)
			{
				material4.SetFloat(ShaderIds.TDReach, r.TdReach);
			}
			Material material5 = this.Mat("Hidden/LumaLooks/SceneComposite");
			if (material5 != null)
			{
				Vector4 compositeFlags = r.CompositeFlags;
				compositeFlags.y = (ssrRun ? 1f : 0f);
				compositeFlags.z = (sunlightRun ? 1f : 0f);
				Vector4 compositeFlags2 = r.CompositeFlags2;
				compositeFlags2.z = (coverageValid ? 1f : 0f);
				this._compositeFlags = compositeFlags;
				material5.SetVector(ShaderIds.CompositeFlags, compositeFlags);
				material5.SetVector(ShaderIds.CompositeFlags2, compositeFlags2);
				material5.SetVector(ShaderIds.SunlightParams2, vector);
				material5.SetVector(ShaderIds.SunlightTint, vector2);
				material5.SetVector(ShaderIds.CloudParams, SkySystem.UniCloudParams);
				material5.SetVector(ShaderIds.CloudParams2, SkySystem.UniCloudParams2);
				material5.SetVector(ShaderIds.CloudParams3, SkySystem.UniCloudParams3);
				material5.SetVector(ShaderIds.CloudShadow, new Vector4(r.CloudShadowStrength, r.CloudShadowSoftness, SkySystem.UniCloudParams2.z, 0f));
				float num8 = Mathf.Clamp01(this._sunDir.y);
				float num9 = Mathf.Lerp(0.35f, 1f, num8);
				material5.SetFloat(ShaderIds.PlayerShade, r.SunMoonOn ? (r.SunlightPlayerShade * num9 * num5) : 0f);
				material5.SetFloat(ShaderIds.RayExtinctionRelief, r.SunlightRayRelief);
				material5.SetFloat(ShaderIds.TDIntensity, Mathf.Min(1f, r.TdIntensity * (1f + 0.5f * night)));
				material5.SetFloat(ShaderIds.TDFloor, r.TdFloor);
				material5.SetFloat(ShaderIds.TDEnclosure, r.TdEnclosure);
				material5.SetFloat(ShaderIds.HazeDensity, RenderEngine.RainHazeDensity(r));
				material5.SetFloat(ShaderIds.HazeStart, r.HazeStart);
				material5.SetFloat(ShaderIds.HazeSunScatter, r.HazeSunScatter);
				material5.SetFloat(ShaderIds.HazeHeightFalloff, r.HazeHeightFalloff);
				material5.SetFloat(ShaderIds.HazeWisps, r.HazeWisps);
				material5.SetFloat(ShaderIds.HazeSkyVeil, r.HazeSkyVeil);
				material5.SetVector(ShaderIds.HazeTint, r.HazeTint);
				material5.SetFloat(ShaderIds.HazeMaxBrightness, r.HazeMaxBrightness * (1f - 0.45f * night));
				material5.SetFloat(ShaderIds.Exposure, r.Exposure + AdaptiveGrade.ExposureOffset);
				material5.SetFloat(ShaderIds.WetStrength, r.WetStrength);
				float num10 = r.BloomHighlights;
				if (!MapSense.IsOutdoor)
				{
					num10 *= 0.3f;
				}
				if (r.PerfBalanced)
				{
					num10 *= 0.75f;
				}
				num10 = 0f;
				material5.SetFloat(ShaderIds.HighlightBoost, num10);
			}
			if (this._bloomDownMats != null)
			{
				foreach (Material material6 in this._bloomDownMats)
				{
					material6.SetFloat(ShaderIds.BloomThreshold, Mathf.Max(0f, r.BloomThreshold - 0.1f * night));
					material6.SetFloat(ShaderIds.BloomScatter, r.BloomScatter);
					material6.SetFloat(ShaderIds.BloomHighlights, r.BloomHighlights);
				}
			}
			if (this._bloomUpMats != null)
			{
				Material[] array = this._bloomUpMats;
				for (int i = 0; i < array.Length; i++)
				{
					array[i].SetFloat(ShaderIds.BloomScatter, r.BloomScatter);
				}
			}
			Material material7 = this.Mat("Hidden/LumaLooks/DoF");
			if (material7 != null)
			{
				if (r.DistBlurOn)
				{
					if (r.DofOn && !this._loggedBlurBoth)
					{
						this._loggedBlurBoth = true;
						this._log.LogInfo("World Blur: both Depth of Field and Distance Blur are enabled — Distance Blur takes precedence (use one blur mode at a time).");
					}
					material7.SetFloat(ShaderIds.BlurMode, 1f);
					material7.SetFloat(ShaderIds.DistBlurStart, r.DistBlurStart);
					material7.SetFloat(ShaderIds.DistBlurEnd, r.DistBlurEnd);
					material7.SetFloat(ShaderIds.DoFMaxRadius, r.DistBlurMax);
					material7.SetFloat(ShaderIds.DoFStrength, 1f);
					material7.SetFloat(ShaderIds.DoFAutoFocus, 0f);
					material7.SetFloat(ShaderIds.DoFFocusDist, r.DofFocus);
					material7.SetFloat(ShaderIds.DoFNearStrength, 1f);
					material7.SetFloat(ShaderIds.DoFBokehGamma, r.DofBokehGamma);
				}
				else
				{
					material7.SetFloat(ShaderIds.BlurMode, 0f);
					material7.SetFloat(ShaderIds.DoFFocusDist, r.DofFocus);
					material7.SetFloat(ShaderIds.DoFStrength, r.DofStrength);
					material7.SetFloat(ShaderIds.DoFMaxRadius, r.DofMaxRadius);
					material7.SetFloat(ShaderIds.DoFAutoFocus, r.DofAutoFocus);
					material7.SetFloat(ShaderIds.DoFNearStrength, r.DofNearStrength);
					material7.SetFloat(ShaderIds.DoFBokehGamma, r.DofBokehGamma);
				}
				material7.SetFloat(ShaderIds.DoFFocusSpeed, r.DofFocusSpeed);
				material7.SetFloat(ShaderIds.DeltaTime, Mathf.Clamp(Time.deltaTime, 0f, 0.1f));
			}
			Material material8 = this.Mat("Hidden/LumaLooks/MotionBlur");
			if (material8 != null)
			{
				material8.SetFloat(ShaderIds.MBAmount, r.MbAmount);
				float num11 = ((r.MbAmount > 0.5f) ? 16f : 8f);
				if (r.PerfBalanced)
				{
					num11 = Mathf.Min(num11, 8f);
				}
				material8.SetFloat(ShaderIds.MBSamples, num11);
			}
			Material material9 = this.Mat("Hidden/LumaLooks/Uber");
			if (material9 != null)
			{
				material9.SetVector(ShaderIds.UberFlags, r.UberFlags);
				material9.SetFloat(ShaderIds.BloomIntensity, r.BloomIntensity);
				material9.SetVector(ShaderIds.BloomTint, r.BloomTint);
				material9.SetFloat(ShaderIds.Exposure, r.Exposure + AdaptiveGrade.ExposureOffset);
				material9.SetFloat(ShaderIds.WBWarmth, r.WbWarmth + AdaptiveGrade.WarmthOffset);
				material9.SetFloat(ShaderIds.WBTint, r.WbTint);
				material9.SetFloat(ShaderIds.Whites, r.Whites);
				material9.SetFloat(ShaderIds.Blacks, r.Blacks);
				material9.SetFloat(ShaderIds.Contrast, r.Contrast + AdaptiveGrade.ContrastOffset);
				material9.SetFloat(ShaderIds.Saturation, r.Saturation + AdaptiveGrade.SaturationOffset);
				material9.SetFloat(ShaderIds.Vibrance, r.Vibrance);
				material9.SetFloat(ShaderIds.FilmLook, r.FilmLook);
				material9.SetFloat(ShaderIds.FilmStrength, r.FilmStrength);
				material9.SetFloat(ShaderIds.Drama, r.Drama);
				material9.SetFloat(ShaderIds.Tonemap, r.Tonemap);
				material9.SetVector(ShaderIds.Vignette, new Vector4(r.VignetteI, r.VignetteS, 0f, 0f));
				material9.SetFloat(ShaderIds.Grain, r.Grain);
				material9.SetFloat(ShaderIds.GrainSpeed, r.GrainSpeed);
				material9.SetFloat(ShaderIds.Chromatic, r.Chromatic);
				material9.SetFloat(ShaderIds.Deband, r.Deband);
				material9.SetFloat(ShaderIds.Letterbox, displayTarget ? r.Letterbox : 0f);
				material9.SetFloat(ShaderIds.UWDistort, r.UwDistort);
				material9.SetFloat(ShaderIds.UWBlur, r.UwBlur);
				material9.SetFloat(ShaderIds.UWFogDensity, r.UwFogDensity);
				material9.SetFloat(ShaderIds.UWCaustics, r.UwCaustics);
			}
			Material material10 = this.Mat("Hidden/LumaLooks/FXAA");
			if (material10 != null)
			{
				material10.SetFloat(ShaderIds.FXAAQuality, r.FxaaQuality);
				material10.SetFloat(ShaderIds.TextMaskValid, textMaskRun ? 1f : 0f);
			}
			if (r.VideoFxOn)
			{
				Material material11 = this.Mat("Hidden/LumaLooks/VideoFX");
				if (material11 != null)
				{
					RenderEngine.PushVideoFx(material11, r);
					material11.SetFloat(ShaderIds.Letterbox, displayTarget ? r.Letterbox : 0f);
				}
			}
			Material material12 = this.Mat("Hidden/LumaLooks/CAS");
			if (material12 != null)
			{
				material12.SetFloat(ShaderIds.CASAmount, r.CasAmount);
			}
			Material material13 = this.Mat("Hidden/LumaLooks/LensFlare");
			if (material13 != null)
			{
				material13.SetFloat(ShaderIds.Letterbox, displayTarget ? r.Letterbox : 0f);
				material13.SetFloat(ShaderIds.FlareIntensity, r.FlareIntensity * (1f - rain));
				material13.SetFloat(ShaderIds.FlareStreakLen, r.FlareStreakLen);
				material13.SetFloat(ShaderIds.FlareEaseRate, r.FlareEaseRate);
				material13.SetFloat(ShaderIds.FlareMode, r.FlareMode);
				material13.SetVector(ShaderIds.FlareParams, new Vector4(r.FlareStreakCount, r.FlareDispersion, r.FlareGhost, r.FlareShimmer));
				material13.SetVector(ShaderIds.SkyReplaceParams2, SkySystem.UniReplaceParams2);
				material13.SetVector(ShaderIds.SunlightTint, vector2);
				material13.SetFloat(ShaderIds.RainVisibility, r.RainVisibility);
				material13.SetFloat(ShaderIds.CoverageValid, coverageValid ? 1f : 0f);
			}
			float num12 = (r.PerfBalanced ? 1f : 0f);
			Material material14 = this.Mat("Hidden/LumaLooks/PlayerShadow");
			if (material14 != null)
			{
				material14.SetFloat(ShaderIds.VrBalanced, num12);
			}
			if (material12 != null)
			{
				material12.SetFloat(ShaderIds.VrBalanced, num12);
			}
			if (material7 != null)
			{
				material7.SetFloat(ShaderIds.VrBalanced, num12);
			}
			if (material13 != null)
			{
				material13.SetFloat(ShaderIds.VrBalanced, num12);
			}
			if (this._ssgiGiMat != null)
			{
				this._ssgiGiMat.SetFloat(ShaderIds.VrBalanced, num12);
			}
			Material material15 = this.Mat("Hidden/LumaLooks/SSGI");
			if (material15 != null)
			{
				material15.SetFloat(ShaderIds.VrBalanced, num12);
			}
			Material material16 = this.Mat("Hidden/LumaLooks/SSR");
			if (material16 != null)
			{
				material16.SetFloat(ShaderIds.VrBalanced, num12);
			}
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0001C814 File Offset: 0x0001AA14
		private void EnsureResources(RenderTextureDescriptor camDesc, bool isVr)
		{
			int frameCount = Time.frameCount;
			if (this._ensuredFrame == frameCount && this._ensuredVr == (isVr ? 1 : 0))
			{
				return;
			}
			this._ensuredFrame = frameCount;
			this._ensuredVr = (isVr ? 1 : 0);
			RenderEngine.RtSet rtSet = this._rt[isVr ? 1 : 0];
			int num = this.HalfDiv(isVr);
			int num2 = ((num == 4) ? 2 : 1);
			int num3 = this.GiDiv(isVr);
			RenderEngine.Alloc(ref rtSet.SceneCopy, camDesc, 1, (GraphicsFormat)48, "_LumaSceneCopy");
			RenderEngine.Alloc(ref rtSet.AoScratch, camDesc, num, (GraphicsFormat)5, "_LumaAOScratch");
			RenderEngine.Alloc(ref rtSet.AoTex, camDesc, num, (GraphicsFormat)5, "_LumaAOTex");
			RenderEngine.Alloc(ref rtSet.SsrScratch, camDesc, num, (GraphicsFormat)48, "_LumaSSRScratch");
			if (RenderEngine.Alloc(ref rtSet.SsrTex, camDesc, num, (GraphicsFormat)48, "_LumaSSRTex"))
			{
				this._ssrLastRunFrame[isVr ? 1 : 0] = -1000;
			}
			if (RenderEngine.Alloc(ref rtSet.GiScratch, camDesc, num3, (GraphicsFormat)48, "_LumaGIScratch") | RenderEngine.Alloc(ref rtSet.GiTex, camDesc, num3, (GraphicsFormat)48, "_LumaGITex"))
			{
				this._giResetFrame[isVr ? 1 : 0] = Time.frameCount;
			}
			if (RenderEngine.Alloc(ref rtSet.TdScratch, camDesc, num, (GraphicsFormat)5, "_LumaTDScratch") | RenderEngine.Alloc(ref rtSet.TdTex, camDesc, num, (GraphicsFormat)5, "_LumaTDTex"))
			{
				this._tdLastRunFrame[isVr ? 1 : 0] = -1000;
			}
			RenderEngine.Alloc(ref rtSet.DofHalf, camDesc, 2, (GraphicsFormat)48, "_LumaDoFHalf");
			RenderEngine.AllocFixed(ref rtSet.DofFocus, 1, 1, (GraphicsFormat)45, "_LumaDoFFocus");
			RenderEngine.AllocFixed(ref rtSet.DofFocusPrev, 1, 1, (GraphicsFormat)45, "_LumaDoFFocusPrev");
			if (RenderEngine.AllocFixed(ref rtSet.FlareVis, 1, 1, (GraphicsFormat)45, "_LumaFlareVis") | RenderEngine.AllocFixed(ref rtSet.FlareVisPrev, 1, 1, (GraphicsFormat)45, "_LumaFlareVisPrev"))
			{
				rtSet.FlareSeedFrame = Time.frameCount;
			}
			if (this._compatPath)
			{
				RenderEngine.Alloc(ref rtSet.TmpFull, camDesc, 1, (GraphicsFormat)48, "_LumaTmpFull");
			}
			RenderEngine.Alloc(ref rtSet.MaskTex, camDesc, num, (GraphicsFormat)5, "_LumaMaskTex");
			RenderTexture renderTexture = ((rtSet.MaskTex != null) ? rtSet.MaskTex.rt : null);
			if (renderTexture != null && renderTexture.width > 0 && renderTexture.height > 0)
			{
				Shader.SetGlobalVector(ShaderIds.MaskTexel, new Vector4(1f / (float)renderTexture.width, 1f / (float)renderTexture.height, (float)renderTexture.width, (float)renderTexture.height));
			}
			RenderEngine.Alloc(ref rtSet.TextMaskTex, camDesc, 1, (GraphicsFormat)5, "_LumaTextMask");
			int num4 = Mathf.Clamp(this._resolved[isVr ? 1 : 0].CloudsResDiv, 1, 4);
			RenderEngine.Alloc(ref rtSet.CloudTex, camDesc, num4, (GraphicsFormat)48, "_LumaCloudTex");
			if (RenderEngine.Alloc(ref rtSet.SunlightTex, camDesc, num, (GraphicsFormat)48, "_LumaSunlightTex"))
			{
				this._sunlightNeedsClear[isVr ? 1 : 0] = true;
			}
			RenderEngine.Alloc(ref rtSet.SunlightScratch, camDesc, num, (GraphicsFormat)48, "_LumaSunlightScratch");
			RenderEngine.Alloc(ref rtSet.SunPrimeTex, camDesc, num, (GraphicsFormat)45, "_LumaDepthPrimeTex");
			if (rtSet.Bloom == null)
			{
				rtSet.Bloom = new RTHandle[4];
			}
			if (rtSet.BloomUp == null)
			{
				rtSet.BloomUp = new RTHandle[3];
			}
			for (int i = 0; i < 4; i++)
			{
				RenderEngine.Alloc(ref rtSet.Bloom[i], camDesc, (2 << i) * num2, (GraphicsFormat)48, RenderEngine.BloomRtNames[i]);
			}
			for (int j = 0; j < 3; j++)
			{
				RenderEngine.Alloc(ref rtSet.BloomUp[j], camDesc, (2 << j) * num2, (GraphicsFormat)48, RenderEngine.BloomUpRtNames[j]);
			}
			this.BindStableTextures(rtSet, Mathf.Max(1, camDesc.width / num3), Mathf.Max(1, camDesc.height / num3), num3, Mathf.Max(1, camDesc.width / num), Mathf.Max(1, camDesc.height / num), num);
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0001CBE8 File Offset: 0x0001ADE8
		private int HalfDiv(bool isVr)
		{
			int halfResDiv = this._resolved[isVr ? 1 : 0].HalfResDiv;
			if (halfResDiv != 2 && halfResDiv != 4)
			{
				return 2;
			}
			return halfResDiv;
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0001CC14 File Offset: 0x0001AE14
		private int GiDiv(bool isVr)
		{
			int giQuality = this._resolved[isVr ? 1 : 0].GiQuality;
			if (giQuality == 1)
			{
				return 2;
			}
			if (giQuality != 2)
			{
				return 4;
			}
			if (!isVr)
			{
				return 1;
			}
			return 2;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0001CC44 File Offset: 0x0001AE44
		private void PushGiFilterUniforms(Material m, int gd, bool isVr)
		{
			if (m == null)
			{
				return;
			}
			float num = ((gd >= 4) ? 1f : ((gd == 2) ? 0.5f : 0f));
			float num2 = ((gd >= 4) ? 0.6f : ((gd == 2) ? 0.35f : 0f));
			if (Time.frameCount - this._giResetFrame[isVr ? 1 : 0] <= 1)
			{
				num2 = 0f;
			}
			m.SetFloat(ShaderIds.GIDenoise, num);
			m.SetFloat(ShaderIds.GITemporal, num2);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0001CCC8 File Offset: 0x0001AEC8
		private static bool AllocFixed(ref RTHandle h, int w, int hgt, GraphicsFormat fmt, string name)
		{
			RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(Mathf.Max(1, w), Mathf.Max(1, hgt), fmt, 0);
			renderTextureDescriptor.msaaSamples = 1;
			renderTextureDescriptor.dimension = (TextureDimension)2;
			renderTextureDescriptor.vrUsage = 0;
			renderTextureDescriptor.useMipMap = false;
			renderTextureDescriptor.autoGenerateMips = false;
			renderTextureDescriptor.enableRandomWrite = false;
			renderTextureDescriptor.depthStencilFormat = 0;
			renderTextureDescriptor.depthBufferBits = 0;
			RenderTextureDescriptor renderTextureDescriptor2 = renderTextureDescriptor;
			return RenderingUtils.ReAllocateHandleIfNeeded(ref h, ref renderTextureDescriptor2, 0, (TextureWrapMode)1, 1, 0f, name);
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0001CD40 File Offset: 0x0001AF40
		private static bool Alloc(ref RTHandle h, RenderTextureDescriptor baseDesc, int div, GraphicsFormat fmt, string name)
		{
			RenderTextureDescriptor renderTextureDescriptor = baseDesc;
			renderTextureDescriptor.msaaSamples = 1;
			renderTextureDescriptor.depthBufferBits = 0;
			renderTextureDescriptor.depthStencilFormat = 0;
			renderTextureDescriptor.graphicsFormat = fmt;
			renderTextureDescriptor.enableRandomWrite = false;
			renderTextureDescriptor.width = Mathf.Max(1, baseDesc.width / div);
			renderTextureDescriptor.height = Mathf.Max(1, baseDesc.height / div);
			renderTextureDescriptor.useMipMap = false;
			renderTextureDescriptor.autoGenerateMips = false;
			return RenderingUtils.ReAllocateHandleIfNeeded(ref h, ref renderTextureDescriptor, (FilterMode)1, (TextureWrapMode)1, 1, 0f, name);
		}

		// Token: 0x060001DE RID: 478 RVA: 0x0001CDC8 File Offset: 0x0001AFC8
		private static void SetTexel(Material m, int w, int h)
		{
			if (m == null)
			{
				return;
			}
			w = Mathf.Max(1, w);
			h = Mathf.Max(1, h);
			m.SetVector(ShaderIds.BlitTexelSize, new Vector4(1f / (float)w, 1f / (float)h, (float)w, (float)h));
		}

		// Token: 0x060001DF RID: 479 RVA: 0x0001CE18 File Offset: 0x0001B018
		private void BindStableTextures(RenderEngine.RtSet set, int giW, int giH, int giDiv, int slW, int slH, int slDiv)
		{
			Material material = this.Mat("Hidden/LumaLooks/SceneComposite");
			if (material != null)
			{
				material.SetTexture(ShaderIds.AOTex, set.AoTex);
				material.SetTexture(ShaderIds.SSRTex, set.SsrTex);
				material.SetTexture(ShaderIds.GITex, set.GiTex);
				material.SetTexture(ShaderIds.TDTex, set.TdTex);
				material.SetTexture(ShaderIds.SunlightTex, set.SunlightTex);
				material.SetVector(ShaderIds.GIUpsample, new Vector4(1f / (float)Mathf.Max(1, giW), 1f / (float)Mathf.Max(1, giH), (float)giDiv, 0f));
				material.SetVector(ShaderIds.SunlightUpsample, new Vector4(1f / (float)Mathf.Max(1, slW), 1f / (float)Mathf.Max(1, slH), (float)slDiv, 0f));
				material.SetVector(ShaderIds.AOUpsample, new Vector4(1f / (float)Mathf.Max(1, slW), 1f / (float)Mathf.Max(1, slH), (float)slDiv, 0f));
			}
			Material material2 = this.Mat("Hidden/LumaLooks/SSR");
			if (material2 != null)
			{
				material2.SetTexture(ShaderIds.SSRSceneTex, set.SceneCopy);
			}
			Shader.SetGlobalTexture(ShaderIds.MaskTex, set.MaskTex);
			Material ssgiGiMat = this._ssgiGiMat;
			if (ssgiGiMat != null)
			{
				ssgiGiMat.SetTexture(ShaderIds.GIPrevTex, set.GiTex);
			}
			Material material3 = this.Mat("Hidden/LumaLooks/DoF");
			if (material3 != null)
			{
				material3.SetTexture(ShaderIds.DoFHalfResTex, set.DofHalf);
				material3.SetTexture(ShaderIds.DoFFocusTex, set.FocusPrev);
			}
			Material material4 = this.Mat("Hidden/LumaLooks/LensFlare");
			if (material4 != null)
			{
				material4.SetTexture(ShaderIds.TDTex, set.TdTex);
				material4.SetTexture(ShaderIds.FlareVisTex, set.FlarePrev);
				material4.SetFloat(ShaderIds.FlareEaseValid, (Time.frameCount - set.FlareSeedFrame <= 1) ? 0f : 1f);
				material4.SetFloat(ShaderIds.DeltaTime, Mathf.Clamp(Time.deltaTime, 0f, 0.1f));
			}
			Material material5 = this.Mat("Hidden/LumaLooks/SSR");
			if (material5 != null)
			{
				material5.SetTexture(ShaderIds.TDTex, set.TdTex);
			}
			Material material6 = this.Mat("Hidden/LumaLooks/FXAA");
			if (material6 != null)
			{
				material6.SetTexture(ShaderIds.TextMaskTex, set.TextMaskTex);
			}
			Material material7 = this.Mat("Hidden/LumaLooks/Uber");
			if (material7 != null && set.BloomUp != null && set.BloomUp.Length != 0)
			{
				material7.SetTexture(ShaderIds.BloomTex, set.BloomUp[0]);
			}
			if (this._bloomUpMats != null && set.Bloom != null && set.BloomUp != null)
			{
				for (int i = this._bloomUpMats.Length - 1; i >= 0; i--)
				{
					RTHandle rthandle = ((i == this._bloomUpMats.Length - 1) ? set.Bloom[i + 1] : set.BloomUp[i + 1]);
					this._bloomUpMats[i].SetTexture(ShaderIds.BloomLowTex, rthandle);
				}
			}
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x0001D190 File Offset: 0x0001B390
		public void RecordStage(Stage stage, bool isVr, RenderGraph rg, ContextContainer frame)
		{
			this.LogPathOnce(true);
			UniversalResourceData universalResourceData = frame.Get<UniversalResourceData>();
			UniversalCameraData universalCameraData = frame.Get<UniversalCameraData>();
			int num = (isVr ? 1 : 0);
			if (!this._loggedRgState[num])
			{
				this._loggedRgState[num] = true;
				RenderTextureDescriptor cameraTargetDescriptor = universalCameraData.cameraTargetDescriptor;
				this._log.LogInfo(string.Format("RGState[{0}] backbuffer={1} ", isVr ? "VR" : "DESKTOP", universalResourceData.isActiveTargetBackBuffer) + string.Format("msaa={0} fmt={1} {2}x{3} ", new object[] { cameraTargetDescriptor.msaaSamples, cameraTargetDescriptor.graphicsFormat, cameraTargetDescriptor.width, cameraTargetDescriptor.height }) + string.Format("hdr={0} postEnabled={1}", universalCameraData.isHdrEnabled, universalCameraData.postProcessEnabled));
			}
			this.EnsureResources(universalCameraData.cameraTargetDescriptor, isVr);
			RenderEngine.RtSet rtSet = this._rt[isVr ? 1 : 0];
			TextureHandle activeColorTexture = universalResourceData.activeColorTexture;
			int width = universalCameraData.cameraTargetDescriptor.width;
			int height = universalCameraData.cameraTargetDescriptor.height;
			int num2 = this.HalfDiv(isVr);
			switch (stage)
			{
			case Stage.Mask:
			{
				if (this._maskTierMats == null || this._metal == null || !this._metal.HasMasked)
				{
					return;
				}
				TextureHandle textureHandle = rg.ImportTexture(rtSet.MaskTex);
				this.RecordMaskPass(rg, textureHandle);
				return;
			}
			case Stage.Clouds:
			{
				SkyShell skyShell = this._skyShell;
				Mesh mesh = ((skyShell != null) ? skyShell.ShellMesh : null);
				SkyShell skyShell2 = this._skyShell;
				Material material = ((skyShell2 != null) ? skyShell2.ShellMaterial : null);
				Material material2 = this.Mat("Hidden/LumaLooks/CloudUpsample");
				if (!(mesh == null) && !(material == null) && !(material2 == null))
				{
					TextureHandle textureHandle2 = rg.ImportTexture(rtSet.CloudTex);
					int num3 = Mathf.Max(1, rtSet.CloudTex.rt.width);
					int num4 = Mathf.Max(1, rtSet.CloudTex.rt.height);
					material.SetVector(ShaderIds.ShellRTSize, new Vector4((float)num3, (float)num4, 0f, 0f));
					material2.SetFloat(ShaderIds.CloudUpsampleOn, 1f);
					material2.SetTexture(ShaderIds.CloudTex, rtSet.CloudTex);
					material2.SetFloat(ShaderIds.ShellRadius, Mathf.Max(0f, this._skyShell.AppliedRadius));
					this.RecordCloudDrawPass(rg, textureHandle2, mesh, this._skyShell.ShellMatrix, material);
					this.RgEffect(rg, universalResourceData, universalCameraData, material2, 0, "LumaCloudUpsample");
					return;
				}
				break;
			}
			case Stage.DepthPrime:
			{
				Material material3 = this.Mat("Hidden/LumaLooks/DepthPrime");
				if (!(material3 == null) && rtSet.SunPrimeTex != null)
				{
					this.RecordDepthPrimePass(rg, rg.ImportTexture(rtSet.SunPrimeTex), material3);
					return;
				}
				break;
			}
			case Stage.Ssao:
			{
				Material material4 = this.Mat("Hidden/LumaLooks/SSAO");
				if (material4 == null)
				{
					return;
				}
				TextureHandle textureHandle3 = rg.ImportTexture(rtSet.AoScratch);
				TextureHandle textureHandle4 = rg.ImportTexture(rtSet.AoTex);
				RenderEngine.SetTexel(material4, width / num2, height / num2);
				this.RgBlit(rg, activeColorTexture, textureHandle4, material4, 0, "LumaSSAO_Estimate");
				this.RgBlit(rg, textureHandle4, textureHandle3, material4, 1, "LumaSSAO_BlurH");
				this.RgBlit(rg, textureHandle3, textureHandle4, material4, 2, "LumaSSAO_BlurV");
				return;
			}
			case Stage.Ssr:
			{
				Material material5 = this.Mat("Hidden/LumaLooks/SSR");
				if (material5 == null)
				{
					return;
				}
				TextureHandle textureHandle5 = rg.ImportTexture(rtSet.SceneCopy);
				TextureHandle textureHandle6 = rg.ImportTexture(rtSet.SsrScratch);
				TextureHandle textureHandle7 = rg.ImportTexture(rtSet.SsrTex);
				RenderGraphUtils.AddBlitPass(rg, activeColorTexture, textureHandle5, Vector2.one, Vector2.zero, 0, 0, -1, 0, 0, 1, (RenderGraphUtils.BlitFilterMode)1, "LumaSSR_SceneCopy", "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 3985);
				RenderEngine.SetTexel(material5, width / num2, height / num2);
				this.RgBlit(rg, activeColorTexture, textureHandle6, material5, 0, "LumaSSR_Trace");
				this.RgBlit(rg, textureHandle6, textureHandle7, material5, 1, "LumaSSR_Blur");
				return;
			}
			case Stage.Ssgi:
			{
				Material ssgiGiMat = this._ssgiGiMat;
				if (ssgiGiMat == null)
				{
					return;
				}
				int num5 = this.GiDiv(isVr);
				TextureHandle textureHandle8 = rg.ImportTexture(rtSet.GiScratch);
				TextureHandle textureHandle9 = rg.ImportTexture(rtSet.GiTex);
				RenderEngine.SetTexel(ssgiGiMat, width / num5, height / num5);
				this.PushGiFilterUniforms(ssgiGiMat, num5, isVr);
				this.RgBlit(rg, activeColorTexture, textureHandle8, ssgiGiMat, 0, "LumaSSGI_Gather");
				this.RgBlit(rg, textureHandle8, textureHandle9, ssgiGiMat, 1, "LumaSSGI_BlurH");
				this.RgBlit(rg, textureHandle9, textureHandle8, ssgiGiMat, 2, "LumaSSGI_BlurV");
				if (num5 >= 4)
				{
					this.RgBlit(rg, textureHandle8, textureHandle9, ssgiGiMat, 2, "LumaSSGI_BlurV2");
					return;
				}
				RenderGraphUtils.AddCopyPass(rg, textureHandle8, textureHandle9, "LumaSSGI_Store", "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4026);
				return;
			}
			case Stage.TrueDark:
			{
				Material material6 = this.Mat("Hidden/LumaLooks/SSGI");
				if (material6 == null)
				{
					return;
				}
				TextureHandle textureHandle10 = rg.ImportTexture(rtSet.TdScratch);
				TextureHandle textureHandle11 = rg.ImportTexture(rtSet.TdTex);
				RenderEngine.SetTexel(material6, width / num2, height / num2);
				this.RgBlit(rg, activeColorTexture, textureHandle11, material6, 3, "LumaTrueDark_Gather");
				this.RgBlit(rg, textureHandle11, textureHandle10, material6, 1, "LumaTrueDark_BlurH");
				this.RgBlit(rg, textureHandle10, textureHandle11, material6, 2, "LumaTrueDark_BlurV");
				return;
			}
			case Stage.Sunlight:
			{
				Material material7 = this.Mat("Hidden/LumaLooks/Sunlight");
				if (material7 == null)
				{
					return;
				}
				TextureHandle textureHandle12 = rg.ImportTexture(rtSet.SunlightTex);
				if (this._sunlightNeedsClear[num])
				{
					this._sunlightNeedsClear[num] = false;
					this.RgClear(rg, textureHandle12, RenderEngine.SunlightIdentity, "LumaSunlight_ClearIdentity");
				}
				TextureHandle mainShadowsTexture = universalResourceData.mainShadowsTexture;
				bool flag = mainShadowsTexture.IsValid();
				bool flag2 = false;
				if (flag)
				{
					try
					{
						TextureDesc textureDesc = rg.GetTextureDesc(ref mainShadowsTexture);
						flag2 = textureDesc.width <= 1 || textureDesc.height <= 1;
					}
					catch
					{
					}
				}
				if (!flag || flag2)
				{
					Material material8 = this.Mat("Hidden/LumaLooks/SceneComposite");
					if (material8 != null)
					{
						this._compositeFlags.z = 0f;
						material8.SetVector(ShaderIds.CompositeFlags, this._compositeFlags);
					}
					if (!this._sunlightAtlasWarned)
					{
						this._sunlightAtlasWarned = true;
						this._log.LogWarning("SUNLIGHT: no usable URP main-light shadow atlas this frame — " + (flag2 ? "URP published its 1x1 EMPTY shadow map (SetupForEmptyRendering: the main light resolved to index -1 because the camera's culling mask excludes it, OR no shadow-casting renderer is visible to the light, OR a cascade slice was invalid). Its contents are UNDEFINED and _MainLightWorldToShadow is stale, so marching it would invent a whole-frame glow bed or a whole-frame darkening." : "resourceData.mainShadowsTexture is invalid (URP published no atlas at all).") + " Rendering nothing and clearing _LumaCompositeFlags.z — there is deliberately NO screen-space fallback march (a second, differently-behaved visibility source is how the old effect ended up with two looks).");
					}
					return;
				}
				TextureHandle textureHandle13 = rg.ImportTexture(rtSet.SunlightScratch);
				RenderEngine.SetTexel(material7, Mathf.Max(1, width / num2), Mathf.Max(1, height / num2));
				if (rtSet.SunPrimeTex != null)
				{
					material7.SetTexture(ShaderIds.DepthPrimeTex, rtSet.SunPrimeTex);
				}
				material7.SetFloat(ShaderIds.DepthPrimeOn, this._primeRunFrame ? 1f : 0f);
				this.RgBlit(rg, activeColorTexture, textureHandle13, material7, 0, "LumaSunlight", mainShadowsTexture, true);
				this.RgBlit(rg, textureHandle13, textureHandle12, material7, 1, "LumaSunlight_Filter");
				return;
			}
			case Stage.Composite:
				this.RgEffect(rg, universalResourceData, universalCameraData, this.Mat("Hidden/LumaLooks/SceneComposite"), 0, "LumaSceneComposite", universalResourceData.mainShadowsTexture, true);
				return;
			case Stage.PlayerShadow:
			{
				Material material9 = this.Mat("Hidden/LumaLooks/PlayerShadow");
				if (material9 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material9, width, height);
				this.RgEffect(rg, universalResourceData, universalCameraData, material9, 0, "LumaPlayerShadow");
				return;
			}
			case Stage.Bloom:
				this.RecordBloomRg(rg, activeColorTexture, rtSet, width, height, (num2 == 4) ? 2 : 1);
				return;
			case Stage.Dof:
			{
				Material material10 = this.Mat("Hidden/LumaLooks/DoF");
				if (material10 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material10, width, height);
				this.RgBlit(rg, activeColorTexture, rg.ImportTexture(rtSet.FocusCur), material10, 2, "LumaDoF_Focus");
				this.RgBlit(rg, activeColorTexture, rg.ImportTexture(rtSet.DofHalf), material10, 0, "LumaDoF_Gather");
				this.RgEffect(rg, universalResourceData, universalCameraData, material10, 1, "LumaDoF_Composite");
				return;
			}
			case Stage.MotionBlur:
				this.RgEffect(rg, universalResourceData, universalCameraData, this.Mat("Hidden/LumaLooks/MotionBlur"), 0, "LumaMotionBlur");
				return;
			case Stage.Uber:
				this.RgEffect(rg, universalResourceData, universalCameraData, this.Mat("Hidden/LumaLooks/Uber"), 0, "LumaUber");
				return;
			case Stage.LensFlare:
			{
				Material material11 = this.Mat("Hidden/LumaLooks/LensFlare");
				if (material11 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material11, width, height);
				if (this._flareRunFrame)
				{
					this.RgBlit(rg, activeColorTexture, rg.ImportTexture(rtSet.FlareCur), material11, 2, "LumaFlareVisEase");
					this.RgEffect(rg, universalResourceData, universalCameraData, material11, 0, "LumaLensFlare");
				}
				if (this._streakRunFrame)
				{
					this.RgEffect(rg, universalResourceData, universalCameraData, material11, 1, "LumaRainStreaks");
					return;
				}
				break;
			}
			case Stage.TextMask:
			{
				if (this._textMaskMat == null || this._textGuard == null || !this._textGuard.HasText)
				{
					return;
				}
				TextureHandle textureHandle14 = rg.ImportTexture(rtSet.TextMaskTex);
				this.RecordTextMaskPass(rg, textureHandle14);
				return;
			}
			case Stage.Fxaa:
			{
				Material material12 = this.Mat("Hidden/LumaLooks/FXAA");
				RenderEngine.SetTexel(material12, width, height);
				this.RgEffect(rg, universalResourceData, universalCameraData, material12, 0, "LumaFXAA");
				return;
			}
			case Stage.Cas:
			{
				Material material13 = this.Mat("Hidden/LumaLooks/CAS");
				RenderEngine.SetTexel(material13, width, height);
				this.RgEffect(rg, universalResourceData, universalCameraData, material13, 0, "LumaCAS");
				return;
			}
			case Stage.VideoFx:
			{
				Material material14 = this.Mat("Hidden/LumaLooks/VideoFX");
				if (!(material14 == null))
				{
					RenderEngine.SetTexel(material14, width, height);
					if (this._fxPixelRun)
					{
						this.RgEffect(rg, universalResourceData, universalCameraData, material14, 3, "LumaFxPixelate");
					}
					if (this._fxCartoonRun)
					{
						this.RgEffect(rg, universalResourceData, universalCameraData, material14, 1, "LumaFxCartoon");
					}
					if (this._fxHalftoneRun)
					{
						this.RgEffect(rg, universalResourceData, universalCameraData, material14, 0, "LumaFxHalftone");
					}
					if (this._fxScanRun)
					{
						this.RgEffect(rg, universalResourceData, universalCameraData, material14, 2, "LumaFxScanlines");
					}
				}
				break;
			}
			default:
				return;
			}
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x0001DB60 File Offset: 0x0001BD60
		public void RecordSkyReplace(RenderGraph rg, ContextContainer frame, bool isVr)
		{
			Material material = this.Mat("Hidden/LumaLooks/SkyReplace");
			if (material == null)
			{
				return;
			}
			this.LogPathOnce(true);
			UniversalResourceData universalResourceData = frame.Get<UniversalResourceData>();
			UniversalCameraData universalCameraData = frame.Get<UniversalCameraData>();
			if (universalResourceData.isActiveTargetBackBuffer)
			{
				if (!this._warnedBackbufferTarget)
				{
					this._warnedBackbufferTarget = true;
					this._log.LogWarning("Luma Looks: active target is the backbuffer despite requiresIntermediateTexture — skipping the sky replacement (backbuffer is not samplable on D3D11).");
				}
				return;
			}
			RenderTextureDescriptor cameraTargetDescriptor = universalCameraData.cameraTargetDescriptor;
			material.SetVector(ShaderIds.SkyReplaceParams, new Vector4(this._skyReplaceBase.x, this._skyReplaceBase.y, this._skyReplaceBase.z, (cameraTargetDescriptor.msaaSamples > 1) ? 1f : 0f));
			TextureHandle activeColorTexture = universalResourceData.activeColorTexture;
			if (this._skyPass0Run)
			{
				this.RgDraw(rg, activeColorTexture, material, 0, "LumaSkyReplace", true);
			}
			if (this._skyPass2Run)
			{
				this.RgDraw(rg, activeColorTexture, material, 2, "LumaSkySunOnly", true);
			}
			if (this._skyPass3Run)
			{
				this.RgDraw(rg, activeColorTexture, material, 3, "LumaSkyClouds", true);
			}
			if (!this._skyDiagArmedNow)
			{
				return;
			}
			this._skyDiagArmedNow = false;
			RenderEngine.RtSet rtSet = this._rt[isVr ? 1 : 0];
			RenderEngine.Alloc(ref rtSet.SkyDiag, cameraTargetDescriptor, 16, (GraphicsFormat)8, "_LumaSkyDiag");
			if (rtSet.SkyDiag == null)
			{
				return;
			}
			this._log.LogInfo(string.Concat(new string[]
			{
				"SKYREPLACE DIAG[",
				isVr ? "VR" : "DESKTOP",
				"]: arming ",
				this._skyDiagIsNight ? "NIGHT" : "DAY",
				" slot ",
				string.Format("(nightWeight={0:0.###}) — camColor fmt={1} ", SkySystem.UniReplaceParams2.x, cameraTargetDescriptor.graphicsFormat),
				string.Format("msaa={0} (diag renders its OWN R8G8B8A8_UNorm RT, so alpha survives ", cameraTargetDescriptor.msaaSamples),
				"even on alpha-less HDR camera targets)."
			}));
			TextureHandle textureHandle = rg.ImportTexture(rtSet.SkyDiag);
			this.RgDraw(rg, textureHandle, material, 1, "LumaSkyDiag", false);
			this.RecordSkyDiagReadback(rg, textureHandle, rtSet.SkyDiag);
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x0001DD70 File Offset: 0x0001BF70
		public void ExecuteSkyReplace(bool isVr, CommandBuffer cmd, RTHandle camColor, RenderTextureDescriptor camDesc)
		{
			Material material = this.Mat("Hidden/LumaLooks/SkyReplace");
			if (material == null)
			{
				return;
			}
			this.LogPathOnce(false);
			material.SetVector(ShaderIds.SkyReplaceParams, new Vector4(this._skyReplaceBase.x, this._skyReplaceBase.y, this._skyReplaceBase.z, (camDesc.msaaSamples > 1) ? 1f : 0f));
			if (this._skyPass0Run)
			{
				RenderEngine.CompatDraw(cmd, camColor, material, 0);
			}
			if (this._skyPass2Run)
			{
				RenderEngine.CompatDraw(cmd, camColor, material, 2);
			}
			if (this._skyPass3Run)
			{
				RenderEngine.CompatDraw(cmd, camColor, material, 3);
			}
			if (!this._skyDiagArmedNow)
			{
				return;
			}
			this._skyDiagArmedNow = false;
			RenderEngine.RtSet rtSet = this._rt[isVr ? 1 : 0];
			RenderEngine.Alloc(ref rtSet.SkyDiag, camDesc, 16, (GraphicsFormat)8, "_LumaSkyDiag");
			if (rtSet.SkyDiag == null)
			{
				return;
			}
			this._log.LogInfo(string.Concat(new string[]
			{
				"SKYREPLACE DIAG[",
				isVr ? "VR" : "DESKTOP",
				"]: arming ",
				this._skyDiagIsNight ? "NIGHT" : "DAY",
				" slot ",
				string.Format("(nightWeight={0:0.###}) — camColor fmt={1} ", SkySystem.UniReplaceParams2.x, camDesc.graphicsFormat),
				string.Format("msaa={0} (compat path; diag renders its OWN R8G8B8A8_UNorm RT).", camDesc.msaaSamples)
			}));
			RenderEngine.CompatDraw(cmd, rtSet.SkyDiag, material, 1);
			cmd.SetRenderTarget(camColor, 0, CubemapFace.Unknown, -1);
			this._skyDiagInFlight = true;
			this.RequestSkyDiagReadback(cmd, rtSet.SkyDiag);
		}

		// Token: 0x060001E3 RID: 483 RVA: 0x0001DF18 File Offset: 0x0001C118
		private void RecordSkyDiagReadback(RenderGraph rg, TextureHandle tex, RTHandle rt)
		{
			this._skyDiagInFlight = true;
			RenderEngine.LumaSkyDiagData lumaSkyDiagData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaSkyDiagData>("LumaSkyDiag_Readback", out lumaSkyDiagData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4499))
			{
				lumaSkyDiagData.Tex = tex;
				lumaSkyDiagData.Rt = rt;
				lumaSkyDiagData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref tex, (AccessFlags)1);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaSkyDiagData>(delegate(RenderEngine.LumaSkyDiagData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					d.Engine.RequestSkyDiagReadback(nativeCommandBuffer, d.Rt);
				});
			}
		}

		// Token: 0x060001E4 RID: 484 RVA: 0x0001DFAC File Offset: 0x0001C1AC
		private void RequestSkyDiagReadback(CommandBuffer cmd, RTHandle rt)
		{
			RenderTexture renderTexture = ((rt != null) ? rt.rt : null);
			if (renderTexture == null)
			{
				this._skyDiagInFlight = false;
				return;
			}
			try
			{
				cmd.RequestAsyncReadback(renderTexture, 0, 0, renderTexture.width, 0, renderTexture.height, 0, 1, TextureFormat.RGBA32, this._skyDiagCb);
			}
			catch (Exception ex)
			{
				this._skyDiagInFlight = false;
				this._log.LogWarning("SKYREPLACE DIAG: readback request failed: " + ex.Message);
			}
		}

		// Token: 0x060001E5 RID: 485 RVA: 0x0001E030 File Offset: 0x0001C230
		private void RecordBloomRg(RenderGraph rg, TextureHandle active, RenderEngine.RtSet set, int bw, int bh, int bm)
		{
			if (this._bloomDownMats == null || this._bloomUpMats == null)
			{
				return;
			}
			for (int i = 0; i < 4; i++)
			{
				this._bloomH[i] = rg.ImportTexture(set.Bloom[i]);
			}
			for (int j = 0; j < this._bloomUpMats.Length; j++)
			{
				this._bloomUpH[j] = rg.ImportTexture(set.BloomUp[j]);
			}
			RenderEngine.SetTexel(this._bloomDownMats[0], bw, bh);
			this.RgBlit(rg, active, this._bloomH[0], this._bloomDownMats[0], 0, "LumaBloom_Prefilter");
			for (int k = 1; k < 4; k++)
			{
				int num = (2 << k - 1) * bm;
				RenderEngine.SetTexel(this._bloomDownMats[k], bw / num, bh / num);
				this.RgBlit(rg, this._bloomH[k - 1], this._bloomH[k], this._bloomDownMats[k], 1, RenderEngine.BloomDownPassNames[k]);
			}
			for (int l = this._bloomUpMats.Length - 1; l >= 0; l--)
			{
				int num2 = (2 << l) * bm;
				RenderEngine.SetTexel(this._bloomUpMats[l], bw / num2, bh / num2);
				this.RgBlit(rg, this._bloomH[l], this._bloomUpH[l], this._bloomUpMats[l], 0, RenderEngine.BloomUpPassNames[l]);
			}
		}

		// Token: 0x060001E6 RID: 486 RVA: 0x0001E1A4 File Offset: 0x0001C3A4
		private void NoteExec(string name)
		{
			if (this._allExecLogged)
			{
				return;
			}
			int num = this._execNoteCalls + 1;
			this._execNoteCalls = num;
			if (num > 20000)
			{
				this._allExecLogged = true;
				return;
			}
			HashSet<string> execLogged = this._execLogged;
			lock (execLogged)
			{
				if (!this._execLogged.Add(name))
				{
					return;
				}
			}
			this._log.LogInfo("EXEC " + name + " — blit ran on the GPU timeline.");
		}

		// Token: 0x060001E7 RID: 487 RVA: 0x0001E238 File Offset: 0x0001C438
		private void ProbeShader(Material mat, string name)
		{
			if (this._shaderProbed)
			{
				return;
			}
			this._shaderProbed = true;
			try
			{
				Shader shader = ((mat != null) ? mat.shader : null);
				this._log.LogInfo((shader != null) ? string.Format("PROBE2 {0} shader='{1}' isSupported={2} passCount={3} renderQueue={4}", new object[] { name, shader.name, shader.isSupported, mat.passCount, mat.renderQueue }) : ("PROBE2 " + name + " material/shader NULL"));
			}
			catch (Exception ex)
			{
				this._log.LogInfo("PROBE2 " + name + " threw: " + ex.Message);
			}
		}

		// Token: 0x060001E8 RID: 488 RVA: 0x0001E30C File Offset: 0x0001C50C
		private void ProbeSrc(RTHandle src, string name)
		{
			if (this._srcProbed)
			{
				return;
			}
			this._srcProbed = true;
			try
			{
				RenderTexture renderTexture = ((src != null) ? src.rt : null);
				this._log.LogInfo((renderTexture != null) ? string.Format("PROBE {0} src rt='{1}' {2}x{3} fmt={4} — bindable.", new object[] { name, renderTexture.name, renderTexture.width, renderTexture.height, renderTexture.graphicsFormat }) : string.Concat(new string[]
				{
					"PROBE ",
					name,
					" src rt=NULL (handle wraps a RenderTargetIdentifier: '",
					((src != null) ? src.nameID.ToString() : null) ?? "null-handle",
					"') — MPB.SetTexture binds NOTHING → shader samples black. ROOT CAUSE."
				}));
			}
			catch (Exception ex)
			{
				this._log.LogInfo("PROBE " + name + " threw: " + ex.Message);
			}
		}

		// Token: 0x060001E9 RID: 489 RVA: 0x0001E418 File Offset: 0x0001C618
		private void RgDraw(RenderGraph rg, TextureHandle dst, Material mat, int pass, string name, bool readWrite)
		{
			if (mat == null)
			{
				return;
			}
			RenderEngine.LumaDrawData lumaDrawData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaDrawData>(name, out lumaDrawData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4670))
			{
				lumaDrawData.Dst = dst;
				lumaDrawData.Mat = mat;
				lumaDrawData.Pass = pass;
				lumaDrawData.Name = name;
				lumaDrawData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref dst, (AccessFlags)(readWrite ? 3 : 2));
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaDrawData>(delegate(RenderEngine.LumaDrawData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Dst, 0, CubemapFace.Unknown, -1);
					d.Engine.ProbeShader(d.Mat, d.Name);
					RenderEngine.BlitMpb.Clear();
					RenderEngine.BlitMpb.SetVector(ShaderIds.BlitScaleBias, RenderEngine.FullScaleBias);
					nativeCommandBuffer.DrawProcedural(Matrix4x4.identity, d.Mat, d.Pass, 0, 3, 1, RenderEngine.BlitMpb);
					d.Engine.NoteExec(d.Name);
				});
			}
		}

		// Token: 0x060001EA RID: 490 RVA: 0x0001E4C4 File Offset: 0x0001C6C4
		private void RgClear(RenderGraph rg, TextureHandle dst, Color col, string name)
		{
			RenderEngine.LumaClearData lumaClearData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaClearData>(name, out lumaClearData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4707))
			{
				lumaClearData.Dst = dst;
				lumaClearData.Col = col;
				lumaClearData.Name = name;
				lumaClearData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref dst, (AccessFlags)2);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaClearData>(delegate(RenderEngine.LumaClearData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Dst, 0, CubemapFace.Unknown, -1);
					nativeCommandBuffer.ClearRenderTarget(false, true, d.Col);
					d.Engine.NoteExec(d.Name);
				});
			}
		}

		// Token: 0x060001EB RID: 491 RVA: 0x0001E554 File Offset: 0x0001C754
		private void RgBlit(RenderGraph rg, TextureHandle src, TextureHandle dst, Material mat, int pass, string name)
		{
			this.RgBlit(rg, src, dst, mat, pass, name, default(TextureHandle), false);
		}

		// Token: 0x060001EC RID: 492 RVA: 0x0001E57C File Offset: 0x0001C77C
		private void RgBlit(RenderGraph rg, TextureHandle src, TextureHandle dst, Material mat, int pass, string name, TextureHandle extraRead, bool hasExtraRead)
		{
			RenderEngine.LumaBlitData lumaBlitData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaBlitData>(name, out lumaBlitData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4741))
			{
				lumaBlitData.Src = src;
				lumaBlitData.Dst = dst;
				lumaBlitData.Mat = mat;
				lumaBlitData.Pass = pass;
				lumaBlitData.Name = name;
				lumaBlitData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref src, (AccessFlags)1);
				unsafeRenderGraphBuilder.UseTexture(ref dst, (AccessFlags)2);
				if (hasExtraRead && extraRead.IsValid())
				{
					unsafeRenderGraphBuilder.UseTexture(ref extraRead, (AccessFlags)1);
					unsafeRenderGraphBuilder.UseAllGlobalTextures(true);
				}
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaBlitData>(delegate(RenderEngine.LumaBlitData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Dst, 0, CubemapFace.Unknown, -1);
					d.Engine.ProbeSrc(d.Src, d.Name);
					d.Engine.ProbeShader(d.Mat, d.Name);
					RenderEngine.BlitMpb.Clear();
					RenderEngine.BlitMpb.SetTexture(ShaderIds.BlitTexture, d.Src);
					RenderEngine.BlitMpb.SetVector(ShaderIds.BlitScaleBias, RenderEngine.FullScaleBias);
					nativeCommandBuffer.DrawProcedural(Matrix4x4.identity, d.Mat, d.Pass, 0, 3, 1, RenderEngine.BlitMpb);
					d.Engine.NoteExec(d.Name);
				});
			}
		}

		// Token: 0x060001ED RID: 493 RVA: 0x0001E644 File Offset: 0x0001C844
		private static void PushVideoFx(Material m, Resolved r)
		{
			m.SetFloat(ShaderIds.FxHalftoneAmount, r.FxHalftoneAmount);
			m.SetFloat(ShaderIds.FxHalftoneScale, r.FxHalftoneScale);
			m.SetFloat(ShaderIds.FxHalftoneColor, r.FxHalftoneColour);
			m.SetFloat(ShaderIds.FxCartoonAmount, r.FxCartoonAmount);
			m.SetFloat(ShaderIds.FxCartoonSteps, r.FxCartoonSteps);
			m.SetFloat(ShaderIds.FxCartoonOutline, r.FxCartoonOutline);
			m.SetFloat(ShaderIds.FxScanAmount, r.FxScanAmount);
			m.SetFloat(ShaderIds.FxScanCount, r.FxScanCount);
			m.SetFloat(ShaderIds.FxScanGrille, r.FxScanGrille);
			m.SetFloat(ShaderIds.FxPixelAmount, r.FxPixelAmount);
			m.SetFloat(ShaderIds.FxPixelSize, r.FxPixelSize);
			m.SetFloat(ShaderIds.FxPixelLevels, r.FxPixelLevels);
		}

		// Token: 0x060001EE RID: 494 RVA: 0x0001E720 File Offset: 0x0001C920
		private void RecordDepthPrimePass(RenderGraph rg, TextureHandle target, Material mat)
		{
			RenderEngine.LumaDepthPrimeData lumaDepthPrimeData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaDepthPrimeData>("LumaDepthPrime", out lumaDepthPrimeData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4851))
			{
				lumaDepthPrimeData.Target = target;
				lumaDepthPrimeData.Mat = mat;
				lumaDepthPrimeData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref target, (AccessFlags)2);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaDepthPrimeData>(delegate(RenderEngine.LumaDepthPrimeData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Target, 0, CubemapFace.Unknown, -1);
					nativeCommandBuffer.ClearRenderTarget(false, true, new Color(1000000f, 0f, 0f, 0f));
					PlayerShadow playerShadow = d.Engine._playerShadow;
					if (playerShadow != null)
					{
						playerShadow.RecordPrimeDraws(nativeCommandBuffer, d.Mat);
					}
					d.Engine.NoteExec("LumaDepthPrime");
				});
			}
		}

		// Token: 0x060001EF RID: 495 RVA: 0x0001E7AC File Offset: 0x0001C9AC
		private void RecordCloudDrawPass(RenderGraph rg, TextureHandle target, Mesh mesh, Matrix4x4 matrix, Material mat)
		{
			RenderEngine.LumaCloudData lumaCloudData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaCloudData>("LumaCloudDraw", out lumaCloudData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4872))
			{
				lumaCloudData.Target = target;
				lumaCloudData.Mesh = mesh;
				lumaCloudData.Matrix = matrix;
				lumaCloudData.Mat = mat;
				lumaCloudData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref target, (AccessFlags)2);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaCloudData>(delegate(RenderEngine.LumaCloudData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Target, 0, CubemapFace.Unknown, -1);
					nativeCommandBuffer.ClearRenderTarget(false, true, Color.clear);
					if (d.Mesh != null && d.Mat != null)
					{
						nativeCommandBuffer.DrawMesh(d.Mesh, d.Matrix, d.Mat, 0, 0);
					}
					d.Engine.NoteExec("LumaCloudDraw");
				});
			}
		}

		// Token: 0x060001F0 RID: 496 RVA: 0x0001E848 File Offset: 0x0001CA48
		private void RecordMaskPass(RenderGraph rg, TextureHandle maskTex)
		{
			RenderEngine.LumaMaskData lumaMaskData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaMaskData>("LumaMetalMask", out lumaMaskData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4891))
			{
				lumaMaskData.Mask = maskTex;
				lumaMaskData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref maskTex, (AccessFlags)2);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaMaskData>(delegate(RenderEngine.LumaMaskData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Mask, 0, CubemapFace.Unknown, -1);
					nativeCommandBuffer.ClearRenderTarget(false, true, new Color(0.35f, 0.35f, 0.35f, 1f));
					MetalSurfaces metal = d.Engine._metal;
					if (metal != null)
					{
						metal.RecordMaskDraws(nativeCommandBuffer, d.Engine._maskTierMats);
					}
					d.Engine.NoteExec("LumaMetalMask");
				});
			}
		}

		// Token: 0x060001F1 RID: 497 RVA: 0x0001E8CC File Offset: 0x0001CACC
		private void RecordTextMaskPass(RenderGraph rg, TextureHandle maskTex)
		{
			RenderEngine.LumaTextMaskData lumaTextMaskData = null;
			using (IUnsafeRenderGraphBuilder unsafeRenderGraphBuilder = rg.AddUnsafePass<RenderEngine.LumaTextMaskData>("LumaTextMask", out lumaTextMaskData, "C:\\Users\\trueg\\AppData\\Local\\Temp\\claude\\C--Users-trueg\\f0d9fd0e-70a1-4859-b2e6-58179115a636\\fixpe\\mod-proplus\\LumaLooks\\Engine\\RenderEngine.cs", 4929))
			{
				lumaTextMaskData.Mask = maskTex;
				lumaTextMaskData.Engine = this;
				unsafeRenderGraphBuilder.UseTexture(ref maskTex, (AccessFlags)2);
				unsafeRenderGraphBuilder.AllowPassCulling(false);
				unsafeRenderGraphBuilder.SetRenderFunc<RenderEngine.LumaTextMaskData>(delegate(RenderEngine.LumaTextMaskData d, UnsafeGraphContext ctx)
				{
					CommandBuffer nativeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(ctx.cmd);
					ctx.cmd.SetRenderTarget(d.Mask, 0, CubemapFace.Unknown, -1);
					nativeCommandBuffer.ClearRenderTarget(false, true, Color.clear);
					RenderEngine engine = d.Engine;
					TextGuard textGuard = engine._textGuard;
					if (textGuard != null)
					{
						textGuard.RecordMaskDraws(nativeCommandBuffer, engine._textMaskMat, engine._textQuad);
					}
					engine.NoteExec("LumaTextMask");
				});
			}
		}

		// Token: 0x060001F2 RID: 498 RVA: 0x0001E950 File Offset: 0x0001CB50
		private void RgEffect(RenderGraph rg, UniversalResourceData res, UniversalCameraData cam, Material mat, int pass, string name)
		{
			this.RgEffect(rg, res, cam, mat, pass, name, default(TextureHandle), false);
		}

		// Token: 0x060001F3 RID: 499 RVA: 0x0001E978 File Offset: 0x0001CB78
		private void RgEffect(RenderGraph rg, UniversalResourceData res, UniversalCameraData cam, Material mat, int pass, string name, TextureHandle extraRead, bool hasExtraRead)
		{
			if (mat == null)
			{
				return;
			}
			if (res.isActiveTargetBackBuffer)
			{
				if (!this._warnedBackbufferTarget)
				{
					this._warnedBackbufferTarget = true;
					this._log.LogWarning("Luma Looks: active target is the backbuffer despite requiresIntermediateTexture — skipping screen effects (backbuffer is not samplable on D3D11).");
				}
				return;
			}
			TextureHandle activeColorTexture = res.activeColorTexture;
			TextureHandle cameraColor = res.cameraColor;
			TextureDesc textureDesc = rg.GetTextureDesc(ref cameraColor);
			textureDesc.name = name;
			textureDesc.clearBuffer = false;
			textureDesc.discardBuffer = false;
			textureDesc.msaaSamples = (MSAASamples)1;
			TextureHandle textureHandle = rg.CreateTexture(ref textureDesc);
			this.RgBlit(rg, activeColorTexture, textureHandle, mat, pass, name, extraRead, hasExtraRead);
			res.cameraColor = textureHandle;
		}

		// Token: 0x060001F4 RID: 500 RVA: 0x0001EA14 File Offset: 0x0001CC14
		public void ExecuteStage(Stage stage, bool isVr, CommandBuffer cmd, RTHandle camColor, RenderTextureDescriptor camDesc)
		{
			this.LogPathOnce(false);
			this.EnsureResources(camDesc, isVr);
			RenderEngine.RtSet rtSet = this._rt[isVr ? 1 : 0];
			if (rtSet.TmpFull == null)
			{
				RenderEngine.Alloc(ref rtSet.TmpFull, camDesc, 1, (GraphicsFormat)48, "_LumaTmpFull");
			}
			int width = camDesc.width;
			int height = camDesc.height;
			int num = this.HalfDiv(isVr);
			switch (stage)
			{
			case Stage.Mask:
				if (this._maskTierMats == null || this._metal == null || !this._metal.HasMasked)
				{
					return;
				}
				cmd.SetRenderTarget(rtSet.MaskTex, 0, CubemapFace.Unknown, -1);
				cmd.ClearRenderTarget(false, true, new Color(0.35f, 0.35f, 0.35f, 1f));
				this._metal.RecordMaskDraws(cmd, this._maskTierMats);
				return;
			case Stage.Clouds:
			{
				SkyShell skyShell = this._skyShell;
				Mesh mesh = ((skyShell != null) ? skyShell.ShellMesh : null);
				SkyShell skyShell2 = this._skyShell;
				Material material = ((skyShell2 != null) ? skyShell2.ShellMaterial : null);
				Material material2 = this.Mat("Hidden/LumaLooks/CloudUpsample");
				if (mesh == null || material == null || material2 == null)
				{
					return;
				}
				int num2 = Mathf.Max(1, rtSet.CloudTex.rt.width);
				int num3 = Mathf.Max(1, rtSet.CloudTex.rt.height);
				material.SetVector(ShaderIds.ShellRTSize, new Vector4((float)num2, (float)num3, 0f, 0f));
				material2.SetFloat(ShaderIds.CloudUpsampleOn, 1f);
				material2.SetTexture(ShaderIds.CloudTex, rtSet.CloudTex);
				material2.SetFloat(ShaderIds.ShellRadius, Mathf.Max(0f, this._skyShell.AppliedRadius));
				cmd.SetRenderTarget(rtSet.CloudTex, 0, CubemapFace.Unknown, -1);
				cmd.ClearRenderTarget(false, true, Color.clear);
				cmd.DrawMesh(mesh, this._skyShell.ShellMatrix, material, 0, 0);
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material2, 0);
				return;
			}
			case Stage.DepthPrime:
				break;
			case Stage.Ssao:
			{
				Material material3 = this.Mat("Hidden/LumaLooks/SSAO");
				if (material3 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material3, width / num, height / num);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.AoTex, material3, 0);
				Blitter.BlitCameraTexture(cmd, rtSet.AoTex, rtSet.AoScratch, material3, 1);
				Blitter.BlitCameraTexture(cmd, rtSet.AoScratch, rtSet.AoTex, material3, 2);
				return;
			}
			case Stage.Ssr:
			{
				Material material4 = this.Mat("Hidden/LumaLooks/SSR");
				if (material4 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material4, width / num, height / num);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.SceneCopy, 0f, false);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.SsrScratch, material4, 0);
				Blitter.BlitCameraTexture(cmd, rtSet.SsrScratch, rtSet.SsrTex, material4, 1);
				return;
			}
			case Stage.Ssgi:
			{
				Material ssgiGiMat = this._ssgiGiMat;
				if (ssgiGiMat == null)
				{
					return;
				}
				int num4 = this.GiDiv(isVr);
				RenderEngine.SetTexel(ssgiGiMat, width / num4, height / num4);
				this.PushGiFilterUniforms(ssgiGiMat, num4, isVr);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.GiScratch, ssgiGiMat, 0);
				Blitter.BlitCameraTexture(cmd, rtSet.GiScratch, rtSet.GiTex, ssgiGiMat, 1);
				Blitter.BlitCameraTexture(cmd, rtSet.GiTex, rtSet.GiScratch, ssgiGiMat, 2);
				if (num4 >= 4)
				{
					Blitter.BlitCameraTexture(cmd, rtSet.GiScratch, rtSet.GiTex, ssgiGiMat, 2);
					return;
				}
				Blitter.BlitCameraTexture(cmd, rtSet.GiScratch, rtSet.GiTex, 0f, false);
				return;
			}
			case Stage.TrueDark:
			{
				Material material5 = this.Mat("Hidden/LumaLooks/SSGI");
				if (material5 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material5, width / num, height / num);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.TdTex, material5, 3);
				Blitter.BlitCameraTexture(cmd, rtSet.TdTex, rtSet.TdScratch, material5, 1);
				Blitter.BlitCameraTexture(cmd, rtSet.TdScratch, rtSet.TdTex, material5, 2);
				return;
			}
			case Stage.Sunlight:
			{
				Material material6 = this.Mat("Hidden/LumaLooks/Sunlight");
				if (material6 == null)
				{
					return;
				}
				int num5 = (isVr ? 1 : 0);
				if (this._sunlightNeedsClear[num5])
				{
					this._sunlightNeedsClear[num5] = false;
					cmd.SetRenderTarget(rtSet.SunlightTex, 0, CubemapFace.Unknown, -1);
					cmd.ClearRenderTarget(false, true, RenderEngine.SunlightIdentity);
				}
				RenderEngine.SetTexel(material6, Mathf.Max(1, width / num), Mathf.Max(1, height / num));
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.SunlightScratch, material6, 0);
				Blitter.BlitCameraTexture(cmd, rtSet.SunlightScratch, rtSet.SunlightTex, material6, 1);
				return;
			}
			case Stage.Composite:
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, this.Mat("Hidden/LumaLooks/SceneComposite"), 0);
				return;
			case Stage.PlayerShadow:
			{
				Material material7 = this.Mat("Hidden/LumaLooks/PlayerShadow");
				if (material7 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material7, width, height);
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material7, 0);
				return;
			}
			case Stage.Bloom:
				this.ExecuteBloomCompat(cmd, camColor, rtSet, width, height, (num == 4) ? 2 : 1);
				return;
			case Stage.Dof:
			{
				Material material8 = this.Mat("Hidden/LumaLooks/DoF");
				if (material8 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material8, width, height);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.FocusCur, material8, 2);
				Blitter.BlitCameraTexture(cmd, camColor, rtSet.DofHalf, material8, 0);
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material8, 1);
				return;
			}
			case Stage.MotionBlur:
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, this.Mat("Hidden/LumaLooks/MotionBlur"), 0);
				return;
			case Stage.Uber:
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, this.Mat("Hidden/LumaLooks/Uber"), 0);
				return;
			case Stage.LensFlare:
			{
				Material material9 = this.Mat("Hidden/LumaLooks/LensFlare");
				if (material9 == null)
				{
					return;
				}
				RenderEngine.SetTexel(material9, width, height);
				if (this._flareRunFrame)
				{
					Blitter.BlitCameraTexture(cmd, camColor, rtSet.FlareCur, material9, 2);
					RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material9, 0);
				}
				if (this._streakRunFrame)
				{
					RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material9, 1);
					return;
				}
				break;
			}
			case Stage.TextMask:
				if (this._textMaskMat == null || this._textGuard == null || !this._textGuard.HasText)
				{
					return;
				}
				cmd.SetRenderTarget(rtSet.TextMaskTex, 0, CubemapFace.Unknown, -1);
				cmd.ClearRenderTarget(false, true, Color.clear);
				this._textGuard.RecordMaskDraws(cmd, this._textMaskMat, this._textQuad);
				return;
			case Stage.Fxaa:
			{
				Material material10 = this.Mat("Hidden/LumaLooks/FXAA");
				RenderEngine.SetTexel(material10, width, height);
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material10, 0);
				return;
			}
			case Stage.Cas:
			{
				Material material11 = this.Mat("Hidden/LumaLooks/CAS");
				RenderEngine.SetTexel(material11, width, height);
				RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material11, 0);
				return;
			}
			case Stage.VideoFx:
			{
				Material material12 = this.Mat("Hidden/LumaLooks/VideoFX");
				if (!(material12 == null))
				{
					RenderEngine.SetTexel(material12, width, height);
					if (this._fxPixelRun)
					{
						RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material12, 3);
					}
					if (this._fxCartoonRun)
					{
						RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material12, 1);
					}
					if (this._fxHalftoneRun)
					{
						RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material12, 0);
					}
					if (this._fxScanRun)
					{
						RenderEngine.CompatEffect(cmd, camColor, rtSet.TmpFull, material12, 2);
					}
				}
				break;
			}
			default:
				return;
			}
		}

		// Token: 0x060001F5 RID: 501 RVA: 0x0001F148 File Offset: 0x0001D348
		private void ExecuteBloomCompat(CommandBuffer cmd, RTHandle camColor, RenderEngine.RtSet set, int bw, int bh, int bm)
		{
			if (this._bloomDownMats == null || this._bloomUpMats == null)
			{
				return;
			}
			RenderEngine.SetTexel(this._bloomDownMats[0], bw, bh);
			Blitter.BlitCameraTexture(cmd, camColor, set.Bloom[0], this._bloomDownMats[0], 0);
			for (int i = 1; i < 4; i++)
			{
				int num = (2 << i - 1) * bm;
				RenderEngine.SetTexel(this._bloomDownMats[i], bw / num, bh / num);
				Blitter.BlitCameraTexture(cmd, set.Bloom[i - 1], set.Bloom[i], this._bloomDownMats[i], 1);
			}
			for (int j = this._bloomUpMats.Length - 1; j >= 0; j--)
			{
				int num2 = (2 << j) * bm;
				RenderEngine.SetTexel(this._bloomUpMats[j], bw / num2, bh / num2);
				Blitter.BlitCameraTexture(cmd, set.Bloom[j], set.BloomUp[j], this._bloomUpMats[j], 0);
			}
		}

		// Token: 0x060001F6 RID: 502 RVA: 0x0001F230 File Offset: 0x0001D430
		private static void CompatEffect(CommandBuffer cmd, RTHandle camColor, RTHandle tmp, Material mat, int pass)
		{
			if (mat == null)
			{
				return;
			}
			Blitter.BlitCameraTexture(cmd, camColor, tmp, mat, pass);
			Blitter.BlitCameraTexture(cmd, tmp, camColor, 0f, false);
		}

		// Token: 0x060001F7 RID: 503 RVA: 0x0001F258 File Offset: 0x0001D458
		private static void CompatDraw(CommandBuffer cmd, RTHandle dst, Material mat, int pass)
		{
			if (mat == null || dst == null)
			{
				return;
			}
			cmd.SetRenderTarget(dst, 0, CubemapFace.Unknown, -1);
			RenderEngine.BlitMpb.Clear();
			RenderEngine.BlitMpb.SetVector(ShaderIds.BlitScaleBias, RenderEngine.FullScaleBias);
			cmd.DrawProcedural(Matrix4x4.identity, mat, pass, 0, 3, 1, RenderEngine.BlitMpb);
		}

		// Token: 0x060001F8 RID: 504 RVA: 0x0001F2B4 File Offset: 0x0001D4B4
		public void ReportException(Exception e)
		{
			this.RollErrorFrame();
			if (!this._frameHadError)
			{
				this._frameHadError = true;
				this._log.LogError(string.Format("Luma Looks render exception: {0}", e));
			}
		}

		// Token: 0x060001F9 RID: 505 RVA: 0x0001F2E1 File Offset: 0x0001D4E1
		public void NoteSuccess()
		{
			this.RollErrorFrame();
		}

		// Token: 0x060001FA RID: 506 RVA: 0x0001F2EC File Offset: 0x0001D4EC
		private void RollErrorFrame()
		{
			int frameCount = Time.frameCount;
			if (frameCount == this._errFrame)
			{
				return;
			}
			if (this._errFrame != -1)
			{
				if (this._frameHadError)
				{
					this._consecutiveErrorFrames++;
					if (this._consecutiveErrorFrames < 5)
					{
						goto IL_0077;
					}
					this._log.LogError(string.Format("Luma Looks disabled after {0} consecutive frames with render exceptions.", 5));
					this.Enabled = false;
					try
					{
						Action onSelfDisabled = this.OnSelfDisabled;
						if (onSelfDisabled != null)
						{
							onSelfDisabled();
						}
						goto IL_0077;
					}
					catch
					{
						goto IL_0077;
					}
				}
				this._consecutiveErrorFrames = 0;
			}
			IL_0077:
			this._errFrame = frameCount;
			this._frameHadError = false;
		}

		// Token: 0x060001FB RID: 507 RVA: 0x0001F390 File Offset: 0x0001D590
		private void LogPathOnce(bool renderGraph)
		{
			this._compatPath = !renderGraph;
			if (this._pathLogged)
			{
				return;
			}
			this._pathLogged = true;
			this._log.LogInfo(renderGraph ? "URP RenderGraph path active." : "URP Compatibility Mode path active.");
		}

		// Token: 0x060001FC RID: 508 RVA: 0x0001F3C8 File Offset: 0x0001D5C8
		public void Dispose()
		{
			for (int i = 0; i < this._rt.Length; i++)
			{
				this._rt[i].Release();
			}
			foreach (Material material in this._mats.Values)
			{
				if (material != null)
				{
					UnityEngine.Object.Destroy(material);
				}
			}
			if (this._ssgiGiMat != null)
			{
				UnityEngine.Object.Destroy(this._ssgiGiMat);
			}
			if (this._bloomUpMats != null)
			{
				foreach (Material material2 in this._bloomUpMats)
				{
					if (material2 != null)
					{
						UnityEngine.Object.Destroy(material2);
					}
				}
			}
			if (this._bloomDownMats != null)
			{
				foreach (Material material3 in this._bloomDownMats)
				{
					if (material3 != null)
					{
						UnityEngine.Object.Destroy(material3);
					}
				}
			}
			if (this._maskTierMats != null)
			{
				foreach (Material material4 in this._maskTierMats)
				{
					if (material4 != null)
					{
						UnityEngine.Object.Destroy(material4);
					}
				}
			}
			if (this._textMaskMat != null)
			{
				UnityEngine.Object.Destroy(this._textMaskMat);
				this._textMaskMat = null;
			}
			if (this._textQuad != null)
			{
				UnityEngine.Object.Destroy(this._textQuad);
				this._textQuad = null;
			}
			this._mats.Clear();
			this._shaders.Clear();
		}

		// Token: 0x040003A8 RID: 936
		private const string N_SSAO = "Hidden/LumaLooks/SSAO";

		// Token: 0x040003A9 RID: 937
		private const string N_SSR = "Hidden/LumaLooks/SSR";

		// Token: 0x040003AA RID: 938
		private const string N_SSGI = "Hidden/LumaLooks/SSGI";

		// Token: 0x040003AB RID: 939
		private const string N_COMPOSITE = "Hidden/LumaLooks/SceneComposite";

		// Token: 0x040003AC RID: 940
		private const string N_BLOOM_DOWN = "Hidden/LumaLooks/BloomDownsample";

		// Token: 0x040003AD RID: 941
		private const string N_BLOOM_UP = "Hidden/LumaLooks/BloomUpsample";

		// Token: 0x040003AE RID: 942
		private const string N_DOF = "Hidden/LumaLooks/DoF";

		// Token: 0x040003AF RID: 943
		private const string N_MB = "Hidden/LumaLooks/MotionBlur";

		// Token: 0x040003B0 RID: 944
		private const string N_UBER = "Hidden/LumaLooks/Uber";

		// Token: 0x040003B1 RID: 945
		private const string N_CLOUDUP = "Hidden/LumaLooks/CloudUpsample";

		// Token: 0x040003B2 RID: 946
		private const string N_FLARE = "Hidden/LumaLooks/LensFlare";

		// Token: 0x040003B3 RID: 947
		private const string N_FXAA = "Hidden/LumaLooks/FXAA";

		// Token: 0x040003B4 RID: 948
		private const string N_CAS = "Hidden/LumaLooks/CAS";

		// Token: 0x040003B5 RID: 949
		private const string N_VIDEOFX = "Hidden/LumaLooks/VideoFX";

		// Token: 0x040003B6 RID: 950
		private const string N_DEPTHPRIME = "Hidden/LumaLooks/DepthPrime";

		// Token: 0x040003B7 RID: 951
		private bool _fxHalftoneRun;

		// Token: 0x040003B8 RID: 952
		private bool _fxCartoonRun;

		// Token: 0x040003B9 RID: 953
		private bool _fxScanRun;

		// Token: 0x040003BA RID: 954
		private bool _fxPixelRun;

		// Token: 0x040003BB RID: 955
		private bool _primeRunFrame;

		// Token: 0x040003BC RID: 956
		private const string N_METALMASK = "Hidden/LumaLooks/MetalMask";

		// Token: 0x040003BD RID: 957
		private const string N_PSHADOW = "Hidden/LumaLooks/PlayerShadow";

		// Token: 0x040003BE RID: 958
		private const string N_SUNLIGHT = "Hidden/LumaLooks/Sunlight";

		// Token: 0x040003BF RID: 959
		private const string N_SKYREPLACE = "Hidden/LumaLooks/SkyReplace";

		// Token: 0x040003C0 RID: 960
		private const float SkyHorizonGuard = -0.02f;

		// Token: 0x040003C1 RID: 961
		private const float MaskDefault = 0.35f;

		// Token: 0x040003C2 RID: 962
		private const int BLOOM_LEVELS = 4;

		// Token: 0x040003C3 RID: 963
		private static readonly string[] BloomRtNames = RenderEngine.BuildNames("_LumaBloom", 4);

		// Token: 0x040003C4 RID: 964
		private static readonly string[] BloomUpRtNames = RenderEngine.BuildNames("_LumaBloomUp", 3);

		// Token: 0x040003C5 RID: 965
		private static readonly string[] BloomDownPassNames = RenderEngine.BuildNames("LumaBloom_Down", 4);

		// Token: 0x040003C6 RID: 966
		private static readonly string[] BloomUpPassNames = RenderEngine.BuildNames("LumaBloom_Up", 3);

		// Token: 0x040003C7 RID: 967
		private const string BloomPrefilterPass = "LumaBloom_Prefilter";

		// Token: 0x040003C8 RID: 968
		private readonly ManualLogSource _log;

		// Token: 0x040003C9 RID: 969
		private readonly Dictionary<string, Shader> _shaders = new Dictionary<string, Shader>();

		// Token: 0x040003CA RID: 970
		private readonly Dictionary<string, Material> _mats = new Dictionary<string, Material>();

		// Token: 0x040003CB RID: 971
		private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();

		// Token: 0x040003CC RID: 972
		private Material[] _bloomUpMats;

		// Token: 0x040003CD RID: 973
		private Material[] _bloomDownMats;

		// Token: 0x040003CE RID: 974
		private Material[] _maskTierMats;

		// Token: 0x040003CF RID: 975
		private Material _textMaskMat;

		// Token: 0x040003D0 RID: 976
		private Mesh _textQuad;

		// Token: 0x040003D1 RID: 977
		private Material _ssgiGiMat;

		// Token: 0x040003D2 RID: 978
		private readonly TextureHandle[] _bloomH = new TextureHandle[4];

		// Token: 0x040003D3 RID: 979
		private readonly TextureHandle[] _bloomUpH = new TextureHandle[3];

		// Token: 0x040003D4 RID: 980
		private Settings _settings = Settings.BuildDefaults();

		// Token: 0x040003D5 RID: 981
		private volatile bool _dirty = true;

		// Token: 0x040003D7 RID: 983
		private readonly bool[] _loggedTarget = new bool[2];

		// Token: 0x040003D8 RID: 984
		private readonly bool[] _loggedRgState = new bool[2];

		// Token: 0x040003D9 RID: 985
		private readonly Resolved[] _resolved = new Resolved[]
		{
			new Resolved(),
			new Resolved()
		};

		// Token: 0x040003DA RID: 986
		private readonly RenderEngine.RtSet[] _rt = new RenderEngine.RtSet[]
		{
			new RenderEngine.RtSet(),
			new RenderEngine.RtSet()
		};

		// Token: 0x040003DB RID: 987
		private int _ensuredFrame = -1;

		// Token: 0x040003DC RID: 988
		private int _ensuredVr = -1;

		// Token: 0x040003DD RID: 989
		private const int MaxErrorFrames = 5;

		// Token: 0x040003DE RID: 990
		private int _consecutiveErrorFrames;

		// Token: 0x040003DF RID: 991
		private int _errFrame = -1;

		// Token: 0x040003E0 RID: 992
		private bool _frameHadError;

		// Token: 0x040003E1 RID: 993
		private bool _pathLogged;

		// Token: 0x040003E2 RID: 994
		private bool _compatPath;

		// Token: 0x040003E3 RID: 995
		private bool _sawStereoCamera;

		// Token: 0x040003E4 RID: 996
		private bool _loggedMirrorSkip;

		// Token: 0x040003E5 RID: 997
		private readonly Dictionary<int, RenderEngine.PrevVp> _prevVp = new Dictionary<int, RenderEngine.PrevVp>();

		// Token: 0x040003E6 RID: 998
		private readonly List<int> _prevVpStale = new List<int>();

		// Token: 0x040003E7 RID: 999
		private int _prevVpPruneAt;

		// Token: 0x040003E8 RID: 1000
		private float _lastRayDebugPushed = float.NaN;

		// Token: 0x040003E9 RID: 1001
		private Vector4 _sunDir = new Vector4(0f, 1f, 0f, 0f);

		// Token: 0x040003EA RID: 1002
		private Vector4 _sunColor = new Vector4(1f, 1f, 1f, 0f);

		// Token: 0x040003EB RID: 1003
		private readonly int[] _tdLastRunFrame = new int[] { -1000, -1000 };

		// Token: 0x040003EC RID: 1004
		private readonly LumaPass[] _passes;

		// Token: 0x040003ED RID: 1005
		private readonly LumaPass _uberDepthPass;

		// Token: 0x040003EE RID: 1006
		private readonly DepthRequestPass _waterDepthPass = new DepthRequestPass();

		// Token: 0x040003EF RID: 1007
		private readonly DepthRequestPass _shellDepthPass = new DepthRequestPass();

		// Token: 0x040003F0 RID: 1008
		private readonly MirrorLetterboxPass _mirrorLetterbox = new MirrorLetterboxPass();

		// Token: 0x040003F1 RID: 1009
		private MetalSurfaces _metal;

		// Token: 0x040003F2 RID: 1010
		private DynamicLights _dynamicLights;

		// Token: 0x040003F3 RID: 1011
		private PlayerShadow _playerShadow;

		// Token: 0x040003F4 RID: 1012
		private SkyShell _skyShell;

		// Token: 0x040003F5 RID: 1013
		private TextGuard _textGuard;

		// Token: 0x040003F6 RID: 1014
		private readonly int[] _sunlightLogScene = new int[] { int.MinValue, int.MinValue };

		// Token: 0x040003F7 RID: 1015
		private readonly int[] _sunlightLogReason = new int[] { int.MinValue, int.MinValue };

		// Token: 0x040003F8 RID: 1016
		private readonly int[] _sunlightLogSub = new int[] { int.MinValue, int.MinValue };

		// Token: 0x040003F9 RID: 1017
		private readonly bool[] _sunlightNeedsClear = new bool[2];

		// Token: 0x040003FA RID: 1018
		private bool _sunlightAtlasWarned;

		// Token: 0x040003FB RID: 1019
		private Vector4 _compositeFlags;

		// Token: 0x040003FC RID: 1020
		private static readonly Color SunlightIdentity = new Color(0f, 0f, 0f, 1f);

		// Token: 0x040003FD RID: 1021
		private static readonly Vector4 SunlightTintCool = RenderEngine.LinearTint(220, 233, 255);

		// Token: 0x040003FE RID: 1022
		private static readonly Vector4 SunlightTintWarm = RenderEngine.LinearTint(255, 210, 160);

		// Token: 0x040003FF RID: 1023
		private static readonly Vector4 SunlightTintMoon = RenderEngine.LinearTint(251, 252, 255);

		// Token: 0x04000400 RID: 1024
		private const float MoonRayFactor = 1.6f;

		// Token: 0x04000401 RID: 1025
		private const float SunlightMinPush = 0.005f;

		// Token: 0x04000402 RID: 1026
		private const float SurfaceMax = 0.6f;

		// Token: 0x04000403 RID: 1027
		private readonly bool[] _flareLoggedTarget = new bool[2];

		// Token: 0x04000404 RID: 1028
		private const float BodyUvWalkMetres = 60f;

		// Token: 0x04000405 RID: 1029
		private bool _bodyUvArmed;

		// Token: 0x04000406 RID: 1030
		private bool _bodyUvLogged;

		// Token: 0x04000407 RID: 1031
		private Vector3 _bodyUvOrigin;

		// Token: 0x04000408 RID: 1032
		private Vector3 _bodyUvStartDir;

		// Token: 0x04000409 RID: 1033
		private Vector2 _bodyUvStart;

		// Token: 0x0400040A RID: 1034
		private float _bodyUvStartTime;

		// Token: 0x0400040B RID: 1035
		private bool _loggedBlurBoth;

		// Token: 0x0400040C RID: 1036
		private readonly SkyReplacePass _skyReplacePass;

		// Token: 0x0400040D RID: 1037
		private readonly string[] _skyLogZone = new string[2];

		// Token: 0x0400040E RID: 1038
		private readonly bool[] _skyLogNight = new bool[2];

		// Token: 0x0400040F RID: 1039
		private readonly string[] _skyDiagZone = new string[2];

		// Token: 0x04000410 RID: 1040
		private readonly string[] _skyDiagZoneNight = new string[2];

		// Token: 0x04000411 RID: 1041
		private bool _skyDiagArmedNow;

		// Token: 0x04000412 RID: 1042
		private bool _skyDiagInFlight;

		// Token: 0x04000413 RID: 1043
		private readonly Action<AsyncGPUReadbackRequest> _skyDiagCb;

		// Token: 0x04000414 RID: 1044
		private Vector4 _skyReplaceBase;

		// Token: 0x04000415 RID: 1045
		private bool _skyPass0Run;

		// Token: 0x04000416 RID: 1046
		private bool _skyPass2Run;

		// Token: 0x04000417 RID: 1047
		private bool _skyPass3Run;

		// Token: 0x04000418 RID: 1048
		private bool _skyDiagIsNight;

		// Token: 0x04000419 RID: 1049
		private bool _loggedSkyNoSunPass;

		// Token: 0x0400041A RID: 1050
		private bool _loggedSkyNoCloudPass;

		// Token: 0x0400041B RID: 1051
		private readonly int[] _giResetFrame = new int[] { -1000, -1000 };

		// Token: 0x0400041C RID: 1052
		private readonly int[] _ssrLastRunFrame = new int[] { -1000, -1000 };

		// Token: 0x0400041E RID: 1054
		private bool _flareRunFrame;

		// Token: 0x0400041F RID: 1055
		private bool _streakRunFrame;

		// Token: 0x04000420 RID: 1056
		private Vector4 _ambientVec = new Vector4(0.5f, 0.5f, 0.5f, 0f);

		// Token: 0x04000421 RID: 1057
		private static readonly Vector4 SunlessParkedDir = new Vector4(0f, 1f, 0f, 0f);

		// Token: 0x04000422 RID: 1058
		private const int SunGateOk = 0;

		// Token: 0x04000423 RID: 1059
		private const int SunGateOffTarget = 1;

		// Token: 0x04000424 RID: 1060
		private const int SunGateNoShader = 2;

		// Token: 0x04000425 RID: 1061
		private const int SunGateZeroIntensity = 3;

		// Token: 0x04000426 RID: 1062
		private const int SunGateNoSun = 4;

		// Token: 0x04000427 RID: 1063
		private const int SunGateBelowHorizon = 5;

		// Token: 0x04000428 RID: 1064
		private const int SunGateNoAtlas = 6;

		// Token: 0x04000429 RID: 1065
		private const int SunGateNoSunMoon = 7;

		// Token: 0x0400042A RID: 1066
		private const int SunAtlasOk = 0;

		// Token: 0x0400042B RID: 1067
		private const int SunAtlasNoCam = 1;

		// Token: 0x0400042C RID: 1068
		private const int SunAtlasCamRenderShadowsOff = 2;

		// Token: 0x0400042D RID: 1069
		private const int SunAtlasNoUrpAsset = 3;

		// Token: 0x0400042E RID: 1070
		private const int SunAtlasUnsupported = 4;

		// Token: 0x0400042F RID: 1071
		private const int SunAtlasModeNotPerPixel = 5;

		// Token: 0x04000430 RID: 1072
		private const int SunAtlasNoSun = 6;

		// Token: 0x04000431 RID: 1073
		private const int SunAtlasSunInactive = 7;

		// Token: 0x04000432 RID: 1074
		private const int SunAtlasSunShadowsNone = 8;

		// Token: 0x04000433 RID: 1075
		private const int SunAtlasDistBelowNear = 9;

		// Token: 0x04000434 RID: 1076
		private RenderEngine.SunAtlasProbe _atlasProbe;

		// Token: 0x04000435 RID: 1077
		private const float AtlasDistFloor = 22f;

		// Token: 0x04000436 RID: 1078
		private const float FlapLogSeconds = 30f;

		// Token: 0x04000437 RID: 1079
		private static FieldInfo _fiHealRenderMode;

		// Token: 0x04000438 RID: 1080
		private static FieldInfo _fiHealMainShadows;

		// Token: 0x04000439 RID: 1081
		private static bool _fiHealResolved;

		// Token: 0x0400043A RID: 1082
		private int _flapsMode;

		// Token: 0x0400043B RID: 1083
		private int _flapsSup;

		// Token: 0x0400043C RID: 1084
		private int _flapsDist;

		// Token: 0x0400043D RID: 1085
		private int _flapsCasc;

		// Token: 0x0400043E RID: 1086
		private int _flapsRot;

		// Token: 0x0400043F RID: 1087
		private int _flapsLoggedTotal = -1;

		// Token: 0x04000440 RID: 1088
		private float _nextFlapLogAt;

		// Token: 0x04000441 RID: 1089
		private const int SkyDiagDiv = 16;

		// Token: 0x04000442 RID: 1090
		private static readonly Vector4 FullScaleBias = new Vector4(1f, 1f, 0f, 0f);

		// Token: 0x04000443 RID: 1091
		private static readonly MaterialPropertyBlock BlitMpb = new MaterialPropertyBlock();

		// Token: 0x04000444 RID: 1092
		private readonly HashSet<string> _execLogged = new HashSet<string>();

		// Token: 0x04000445 RID: 1093
		private const int EXEC_LOG_BUDGET = 20000;

		// Token: 0x04000446 RID: 1094
		private int _execNoteCalls;

		// Token: 0x04000447 RID: 1095
		private volatile bool _allExecLogged;

		// Token: 0x04000448 RID: 1096
		private bool _shaderProbed;

		// Token: 0x04000449 RID: 1097
		private bool _srcProbed;

		// Token: 0x0400044A RID: 1098
		private bool _warnedBackbufferTarget;

		// Token: 0x0200002E RID: 46
		private struct PrevVp
		{
			// Token: 0x0400044B RID: 1099
			public Matrix4x4 vp0;

			// Token: 0x0400044C RID: 1100
			public Matrix4x4 vp1;

			// Token: 0x0400044D RID: 1101
			public bool has;

			// Token: 0x0400044E RID: 1102
			public int frame;
		}

		// Token: 0x0200002F RID: 47
		private struct SunAtlasProbe
		{
			// Token: 0x0400044F RID: 1103
			public UniversalRenderPipelineAsset Asset;

			// Token: 0x04000450 RID: 1104
			public bool CamRenderShadows;

			// Token: 0x04000451 RID: 1105
			public bool Sup;

			// Token: 0x04000452 RID: 1106
			public float Dist;

			// Token: 0x04000453 RID: 1107
			public LightRenderingMode Mode;

			// Token: 0x04000454 RID: 1108
			public Light Sun;

			// Token: 0x04000455 RID: 1109
			public LightShadows SunShadows;

			// Token: 0x04000456 RID: 1110
			public bool SunActive;

			// Token: 0x04000457 RID: 1111
			public int Sub;
		}

		// Token: 0x02000030 RID: 48
		private sealed class LumaSkyDiagData
		{
			// Token: 0x04000458 RID: 1112
			public TextureHandle Tex;

			// Token: 0x04000459 RID: 1113
			public RTHandle Rt;

			// Token: 0x0400045A RID: 1114
			public RenderEngine Engine;
		}

		// Token: 0x02000031 RID: 49
		private sealed class LumaBlitData
		{
			// Token: 0x0400045B RID: 1115
			public TextureHandle Src;

			// Token: 0x0400045C RID: 1116
			public TextureHandle Dst;

			// Token: 0x0400045D RID: 1117
			public Material Mat;

			// Token: 0x0400045E RID: 1118
			public int Pass;

			// Token: 0x0400045F RID: 1119
			public string Name;

			// Token: 0x04000460 RID: 1120
			public RenderEngine Engine;
		}

		// Token: 0x02000032 RID: 50
		private sealed class LumaDrawData
		{
			// Token: 0x04000461 RID: 1121
			public TextureHandle Dst;

			// Token: 0x04000462 RID: 1122
			public Material Mat;

			// Token: 0x04000463 RID: 1123
			public int Pass;

			// Token: 0x04000464 RID: 1124
			public string Name;

			// Token: 0x04000465 RID: 1125
			public RenderEngine Engine;
		}

		// Token: 0x02000033 RID: 51
		private sealed class LumaClearData
		{
			// Token: 0x04000466 RID: 1126
			public TextureHandle Dst;

			// Token: 0x04000467 RID: 1127
			public Color Col;

			// Token: 0x04000468 RID: 1128
			public string Name;

			// Token: 0x04000469 RID: 1129
			public RenderEngine Engine;
		}

		// Token: 0x02000034 RID: 52
		private sealed class LumaMaskData
		{
			// Token: 0x0400046A RID: 1130
			public TextureHandle Mask;

			// Token: 0x0400046B RID: 1131
			public RenderEngine Engine;
		}

		// Token: 0x02000035 RID: 53
		private sealed class LumaCloudData
		{
			// Token: 0x0400046C RID: 1132
			public TextureHandle Target;

			// Token: 0x0400046D RID: 1133
			public Mesh Mesh;

			// Token: 0x0400046E RID: 1134
			public Matrix4x4 Matrix;

			// Token: 0x0400046F RID: 1135
			public Material Mat;

			// Token: 0x04000470 RID: 1136
			public RenderEngine Engine;
		}

		// Token: 0x02000036 RID: 54
		private sealed class LumaDepthPrimeData
		{
			// Token: 0x04000471 RID: 1137
			public TextureHandle Target;

			// Token: 0x04000472 RID: 1138
			public Material Mat;

			// Token: 0x04000473 RID: 1139
			public RenderEngine Engine;
		}

		// Token: 0x02000037 RID: 55
		private sealed class LumaTextMaskData
		{
			// Token: 0x04000474 RID: 1140
			public TextureHandle Mask;

			// Token: 0x04000475 RID: 1141
			public RenderEngine Engine;
		}

		// Token: 0x02000038 RID: 56
		internal sealed class RtSet
		{
			// Token: 0x17000033 RID: 51
			// (get) Token: 0x06000206 RID: 518 RVA: 0x0001F65C File Offset: 0x0001D85C
			public RTHandle FocusPrev
			{
				get
				{
					if ((Time.frameCount & 1) != 0)
					{
						return this.DofFocusPrev;
					}
					return this.DofFocus;
				}
			}

			// Token: 0x17000034 RID: 52
			// (get) Token: 0x06000207 RID: 519 RVA: 0x0001F674 File Offset: 0x0001D874
			public RTHandle FocusCur
			{
				get
				{
					if ((Time.frameCount & 1) != 0)
					{
						return this.DofFocus;
					}
					return this.DofFocusPrev;
				}
			}

			// Token: 0x17000035 RID: 53
			// (get) Token: 0x06000208 RID: 520 RVA: 0x0001F68C File Offset: 0x0001D88C
			public RTHandle FlarePrev
			{
				get
				{
					if ((Time.frameCount & 1) != 0)
					{
						return this.FlareVisPrev;
					}
					return this.FlareVis;
				}
			}

			// Token: 0x17000036 RID: 54
			// (get) Token: 0x06000209 RID: 521 RVA: 0x0001F6A4 File Offset: 0x0001D8A4
			public RTHandle FlareCur
			{
				get
				{
					if ((Time.frameCount & 1) != 0)
					{
						return this.FlareVis;
					}
					return this.FlareVisPrev;
				}
			}

			// Token: 0x0600020A RID: 522 RVA: 0x0001F6BC File Offset: 0x0001D8BC
			public void Release()
			{
				RenderEngine.RtSet.Rel(ref this.SceneCopy);
				RenderEngine.RtSet.Rel(ref this.AoScratch);
				RenderEngine.RtSet.Rel(ref this.AoTex);
				RenderEngine.RtSet.Rel(ref this.SsrScratch);
				RenderEngine.RtSet.Rel(ref this.SsrTex);
				RenderEngine.RtSet.Rel(ref this.GiScratch);
				RenderEngine.RtSet.Rel(ref this.GiTex);
				RenderEngine.RtSet.Rel(ref this.TdScratch);
				RenderEngine.RtSet.Rel(ref this.TdTex);
				RenderEngine.RtSet.Rel(ref this.DofHalf);
				RenderEngine.RtSet.Rel(ref this.TmpFull);
				RenderEngine.RtSet.Rel(ref this.MaskTex);
				RenderEngine.RtSet.Rel(ref this.CloudTex);
				RenderEngine.RtSet.Rel(ref this.SunPrimeTex);
				RenderEngine.RtSet.Rel(ref this.TextMaskTex);
				RenderEngine.RtSet.Rel(ref this.SunlightTex);
				RenderEngine.RtSet.Rel(ref this.SunlightScratch);
				RenderEngine.RtSet.Rel(ref this.DofFocus);
				RenderEngine.RtSet.Rel(ref this.DofFocusPrev);
				RenderEngine.RtSet.Rel(ref this.SkyDiag);
				RenderEngine.RtSet.Rel(ref this.FlareVis);
				RenderEngine.RtSet.Rel(ref this.FlareVisPrev);
				if (this.Bloom != null)
				{
					for (int i = 0; i < this.Bloom.Length; i++)
					{
						RenderEngine.RtSet.Rel(ref this.Bloom[i]);
					}
				}
				if (this.BloomUp != null)
				{
					for (int j = 0; j < this.BloomUp.Length; j++)
					{
						RenderEngine.RtSet.Rel(ref this.BloomUp[j]);
					}
				}
			}

			// Token: 0x0600020B RID: 523 RVA: 0x0001F813 File Offset: 0x0001DA13
			private static void Rel(ref RTHandle h)
			{
				if (h != null)
				{
					h.Release();
					h = null;
				}
			}

			// Token: 0x04000476 RID: 1142
			public RTHandle SceneCopy;

			// Token: 0x04000477 RID: 1143
			public RTHandle AoScratch;

			// Token: 0x04000478 RID: 1144
			public RTHandle AoTex;

			// Token: 0x04000479 RID: 1145
			public RTHandle SsrScratch;

			// Token: 0x0400047A RID: 1146
			public RTHandle SsrTex;

			// Token: 0x0400047B RID: 1147
			public RTHandle GiScratch;

			// Token: 0x0400047C RID: 1148
			public RTHandle GiTex;

			// Token: 0x0400047D RID: 1149
			public RTHandle TdScratch;

			// Token: 0x0400047E RID: 1150
			public RTHandle TdTex;

			// Token: 0x0400047F RID: 1151
			public RTHandle DofHalf;

			// Token: 0x04000480 RID: 1152
			public RTHandle TmpFull;

			// Token: 0x04000481 RID: 1153
			public RTHandle MaskTex;

			// Token: 0x04000482 RID: 1154
			public RTHandle CloudTex;

			// Token: 0x04000483 RID: 1155
			public RTHandle SunPrimeTex;

			// Token: 0x04000484 RID: 1156
			public RTHandle TextMaskTex;

			// Token: 0x04000485 RID: 1157
			public RTHandle SunlightTex;

			// Token: 0x04000486 RID: 1158
			public RTHandle SunlightScratch;

			// Token: 0x04000487 RID: 1159
			public RTHandle DofFocus;

			// Token: 0x04000488 RID: 1160
			public RTHandle DofFocusPrev;

			// Token: 0x04000489 RID: 1161
			public RTHandle FlareVis;

			// Token: 0x0400048A RID: 1162
			public RTHandle FlareVisPrev;

			// Token: 0x0400048B RID: 1163
			public int FlareSeedFrame = -1000;

			// Token: 0x0400048C RID: 1164
			public RTHandle SkyDiag;

			// Token: 0x0400048D RID: 1165
			public RTHandle[] Bloom;

			// Token: 0x0400048E RID: 1166
			public RTHandle[] BloomUp;
		}
	}
}
