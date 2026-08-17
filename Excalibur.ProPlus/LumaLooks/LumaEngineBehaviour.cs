using System;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Logger = BepInEx.Logging.Logger;

namespace LumaLooks
{
	// Token: 0x02000024 RID: 36
	public sealed class LumaEngineBehaviour : MonoBehaviour
	{
		// Token: 0x06000142 RID: 322 RVA: 0x00012A9C File Offset: 0x00010C9C
		private void Awake()
		{
			if (LumaEngineBehaviour.Log == null)
			{
				LumaEngineBehaviour.Log = Logger.CreateLogSource("Luma Looks");
			}
			LaunchGate.Resolve(LumaEngineBehaviour.Log);
			LumaDebug.Resolve(LumaEngineBehaviour.Log);
			try
			{
				LumaEngineBehaviour.Instance = this;
				this._configPath = Path.Combine(Path.Combine(Paths.ConfigPath, "LumaLooks"), "settings.json");
				this._settings = this.LoadSettings();
				this._engine = new RenderEngine(LumaEngineBehaviour.Log);
				this._engine.OnSelfDisabled += this.OnEngineSelfDisabled;
				string bundleDir = LumaEngineBehaviour.BundleDir;
				bool flag = this._engine.LoadBundle(bundleDir);
				this._engine.ApplySettings(this._settings);
				this._worldLight = new WorldLight(LumaEngineBehaviour.Log);
				this.ApplyWorldLight();
				this._dynamicLights = new DynamicLights(LumaEngineBehaviour.Log);
				this._engine.AttachDynamicLights(this._dynamicLights);
				this.ApplyDynamicLights();
				this._waterSurfaces = new WaterSurfaces(LumaEngineBehaviour.Log, this._engine);
				this.ApplyWaterSurfaces();
				this._rainSensor = new RainSensor(LumaEngineBehaviour.Log);
				this.ApplyRainSensor();
				this._rainCoverage = new RainCoverage(LumaEngineBehaviour.Log);
				this.ApplyRainCoverage();
				this._worldRain = new WorldRain(LumaEngineBehaviour.Log, this._engine);
				this.ApplyWorldRain();
				this._particles = new Particles(LumaEngineBehaviour.Log, this._engine);
				this._particles.AttachDynamicLights(this._dynamicLights);
				this.ApplyParticles();
				this._skySystem = new SkySystem(LumaEngineBehaviour.Log, this._engine);
				this._skyShell = new SkyShell(LumaEngineBehaviour.Log, this._engine);
				this._engine.AttachSkyShell(this._skyShell);
				this._lumaRainFx = new RainParticles(LumaEngineBehaviour.Log, this._engine);
				this._birds = new Birds(LumaEngineBehaviour.Log, this._engine);
				SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoadedReapply);
				this._butterflies = new Insects(LumaEngineBehaviour.Log, this._engine, Insects.Kind.Butterfly);
				this._bees = new Insects(LumaEngineBehaviour.Log, this._engine, Insects.Kind.Bee);
				this._lumaSkybox = new LumaSkybox(LumaEngineBehaviour.Log);
				this._skyOverlay = new SkyOverlay(LumaEngineBehaviour.Log, this._engine, this._skySystem);
				this.ApplySkySystem();
				this._metalSurfaces = new MetalSurfaces(LumaEngineBehaviour.Log);
				this._engine.AttachMetalSurfaces(this._metalSurfaces);
				this.ApplyMetalSurfaces();
				this._textGuard = new TextGuard(LumaEngineBehaviour.Log);
				this._engine.AttachTextGuard(this._textGuard);
				this.ApplyTextGuard();
				this._mapSense = new MapSense(LumaEngineBehaviour.Log);
				this.ApplyMapSense();
				this._playerShadow = new PlayerShadow(LumaEngineBehaviour.Log);
				this._engine.AttachPlayerShadow(this._playerShadow);
				this._playerShadow.AttachSkyDome(this._skySystem.Dome);
				this._playerShadow.AttachGhostShader(this._engine.GetShader("Hidden/LumaLooks/ShadowGhost"));
				this.ApplyPlayerShadow();
				this._waves = new WaterWaves(LumaEngineBehaviour.Log, this._engine);
				this.ApplyWaterWaves();
				if (flag)
				{
					RenderPipelineManager.beginCameraRendering += this.OnBeginCamera;
					this._renderHooked = true;
					this._runInBgPrev = Application.runInBackground;
					Application.runInBackground = true;
					this._runInBgLifted = true;
					LumaEngineBehaviour.Log.LogInfo(string.Format("RUNBG: runInBackground lifted (was {0}) - the game keeps ticking while the Luma panel has focus.", this._runInBgPrev));
					LumaEngineBehaviour.Log.LogInfo("Luma Looks engine online.");
				}
				else
				{
					LumaEngineBehaviour.Log.LogWarning("Luma Looks shader engine is disabled (bundle missing); IPC + world-light still active.");
				}
			}
			catch (Exception ex)
			{
				LumaEngineBehaviour.Log.LogError(string.Format("Luma Looks failed to initialise (disabled): {0}", ex));
				this.Cleanup();
			}
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00012EA8 File Offset: 0x000110A8
		private void OnBeginCamera(ScriptableRenderContext context, Camera cam)
		{
			if (this._engine == null || !this._engine.Enabled)
			{
				return;
			}
			long num = Telemetry.Begin();
			try
			{
				this._engine.BeginCamera(cam);
			}
			catch (Exception ex)
			{
				this._engine.ReportException(ex);
			}
			finally
			{
				Telemetry.End(num);
			}
			try
			{
				SkyShell skyShell = this._skyShell;
				if (skyShell != null)
				{
					skyShell.SyncToCamera(cam);
				}
			}
			catch
			{
			}
			try
			{
				RainParticles lumaRainFx = this._lumaRainFx;
				if (lumaRainFx != null)
				{
					lumaRainFx.SyncToCamera(cam);
				}
			}
			catch
			{
			}
			try
			{
				LumaSkybox lumaSkybox = this._lumaSkybox;
				if (lumaSkybox != null)
				{
					lumaSkybox.ApplyAtRender();
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00012F7C File Offset: 0x0001117C
		private void Update()
		{
			float unscaledDeltaTime = Time.unscaledDeltaTime;
			if (unscaledDeltaTime > 0.0001f)
			{
				this._fps += (1f / unscaledDeltaTime - this._fps) * 0.1f;
			}
			this._launchPollAt -= Time.unscaledDeltaTime;
			if (this._launchPollAt <= 0f)
			{
				this._launchPollAt = 1f;
				LaunchGate.Poll(LumaEngineBehaviour.Log);
			}
			long num = Telemetry.Begin();
			bool flag = false;
			try
			{
				flag = XRSettings.isDeviceActive;
			}
			catch
			{
			}
			PerfMode.LowCpu = this._settings != null && this._settings.Master && QualityTiers.LowCpu(this._settings.QualityFor(flag));
			if (this._reapplyAtFrame > 0 && Time.frameCount >= this._reapplyAtFrame)
			{
				this._reapplyAtFrame = -1;
				try
				{
					this.ApplyAll();
					LumaEngineBehaviour.Log.LogInfo("Map change: re-applied all settings (the automatic version of the off/on toggle that used to be needed to bring the rays back).");
				}
				catch (Exception ex)
				{
					LumaEngineBehaviour.Log.LogWarning("Map-change re-apply failed - " + ex.Message);
				}
			}
			WorldLight worldLight = this._worldLight;
			if (worldLight != null)
			{
				worldLight.Tick();
			}
			PlayerShadow playerShadow = this._playerShadow;
			if (playerShadow != null)
			{
				playerShadow.Tick();
			}
			Settings settings = this._settings;
			EffectSettings effectSettings = ((settings != null) ? settings.Effect("adaptive") : null);
			AdaptiveGrade.Configure(this._settings != null && this._settings.Master && effectSettings != null && effectSettings.Enabled, (effectSettings != null) ? effectSettings.GetFloat("strength", 0.7f) : 0.7f, (effectSettings != null) ? effectSettings.GetFloat("speed", 4f) : 4f);
			AdaptiveGrade.Tick(Time.deltaTime);
			DynamicLights dynamicLights = this._dynamicLights;
			if (dynamicLights != null)
			{
				dynamicLights.Tick();
			}
			WaterSurfaces waterSurfaces = this._waterSurfaces;
			if (waterSurfaces != null)
			{
				waterSurfaces.Tick();
			}
			RainSensor rainSensor = this._rainSensor;
			if (rainSensor != null)
			{
				rainSensor.Tick();
			}
			RainCoverage rainCoverage = this._rainCoverage;
			if (rainCoverage != null)
			{
				rainCoverage.Tick();
			}
			WorldRain worldRain = this._worldRain;
			if (worldRain != null)
			{
				worldRain.Tick();
			}
			MapSense mapSense = this._mapSense;
			if (mapSense != null)
			{
				mapSense.Tick();
			}
			Particles particles = this._particles;
			if (particles != null)
			{
				particles.Tick();
			}
			SkyOverlay skyOverlay = this._skyOverlay;
			if (skyOverlay != null)
			{
				skyOverlay.Tick();
			}
			SkyShell skyShell = this._skyShell;
			if (skyShell != null)
			{
				skyShell.Tick();
			}
			RainParticles lumaRainFx = this._lumaRainFx;
			if (lumaRainFx != null)
			{
				lumaRainFx.Tick();
			}
			Birds birds = this._birds;
			if (birds != null)
			{
				birds.Tick();
			}
			Insects butterflies = this._butterflies;
			if (butterflies != null)
			{
				butterflies.Tick();
			}
			Insects bees = this._bees;
			if (bees != null)
			{
				bees.Tick();
			}
			LumaSkybox lumaSkybox = this._lumaSkybox;
			if (lumaSkybox != null)
			{
				lumaSkybox.Tick();
			}
			SkySystem skySystem = this._skySystem;
			if (skySystem != null)
			{
				skySystem.Tick();
			}
			MetalSurfaces metalSurfaces = this._metalSurfaces;
			if (metalSurfaces != null)
			{
				metalSurfaces.Tick();
			}
			TextGuard textGuard = this._textGuard;
			if (textGuard != null)
			{
				textGuard.Tick();
			}
			WaterWaves waves = this._waves;
			if (waves != null)
			{
				waves.Tick();
			}
			Telemetry.End(num);
			Telemetry.Tick(unscaledDeltaTime);
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (this._savePending && realtimeSinceStartup >= this._saveAt)
			{
				this._savePending = false;
				this.SaveSettings();
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000147 RID: 327 RVA: 0x000134CC File Offset: 0x000116CC
		// (set) Token: 0x06000148 RID: 328 RVA: 0x000134D3 File Offset: 0x000116D3
		internal static LumaEngineBehaviour Instance { get; private set; }

		// Token: 0x06000149 RID: 329 RVA: 0x000134DC File Offset: 0x000116DC
		internal string DiagReadiness()
		{
			string text;
			try
			{
				if (this._engine == null)
				{
					text = "engine=null (not started)";
				}
				else
				{
					text = string.Format("shaders={0} sawStereoCamera={1} enabled={2}", this._engine.ShaderCount, this._engine.SawStereoCamera, this._engine.Enabled);
				}
			}
			catch (Exception ex)
			{
				text = "diag-failed: " + ex.Message;
			}
			return text;
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600014A RID: 330 RVA: 0x0001355C File Offset: 0x0001175C
		internal int ShaderCountSafe
		{
			get
			{
				int num;
				try
				{
					num = ((this._engine != null) ? this._engine.ShaderCount : 0);
				}
				catch
				{
					num = 0;
				}
				return num;
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x0600014B RID: 331 RVA: 0x00013598 File Offset: 0x00011798
		internal bool SawStereoSafe
		{
			get
			{
				bool flag;
				try
				{
					flag = this._engine != null && this._engine.SawStereoCamera;
				}
				catch
				{
					flag = false;
				}
				return flag;
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600014C RID: 332 RVA: 0x000135D4 File Offset: 0x000117D4
		internal bool EngineEnabledSafe
		{
			get
			{
				bool flag;
				try
				{
					flag = this._engine != null && this._engine.Enabled;
				}
				catch
				{
					flag = false;
				}
				return flag;
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600014D RID: 333 RVA: 0x00013610 File Offset: 0x00011810
		internal Settings Live
		{
			get
			{
				return this._settings;
			}
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00013618 File Offset: 0x00011818
		internal void ApplySettingsJson(string json)
		{
			if (string.IsNullOrEmpty(json))
			{
				return;
			}
			this._settings = Settings.Parse(json, false);
			this.ApplyAll();
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00013636 File Offset: 0x00011836
		private void OnSceneLoadedReapply(Scene sc, LoadSceneMode mode)
		{
			this._reapplyAtFrame = Time.frameCount + 30;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00013648 File Offset: 0x00011848
		internal void ApplyAll()
		{
			int failed = 0;
			Step("engine", () =>
			{
				RenderEngine engine = this._engine;
				if (engine == null)
				{
					return;
				}
				engine.ApplySettings(this._settings);
			}, ref failed);
			Step("worldLight", this.ApplyWorldLight, ref failed);
			Step("dynamicLights", this.ApplyDynamicLights, ref failed);
			Step("waterSurfaces", this.ApplyWaterSurfaces, ref failed);
			Step("rainSensor", this.ApplyRainSensor, ref failed);
			Step("rainCoverage", this.ApplyRainCoverage, ref failed);
			Step("worldRain", this.ApplyWorldRain, ref failed);
			Step("particles", this.ApplyParticles, ref failed);
			Step("skySystem", this.ApplySkySystem, ref failed);
			Step("metalSurfaces", this.ApplyMetalSurfaces, ref failed);
			Step("textGuard", this.ApplyTextGuard, ref failed);
			Step("mapSense", this.ApplyMapSense, ref failed);
			Step("playerShadow", this.ApplyPlayerShadow, ref failed);
			Step("waterWaves", this.ApplyWaterWaves, ref failed);
			LumaEngineBehaviour.Log.LogInfo(string.Format("APPLY: master={0} effects={1} ", this._settings.Master, this._settings.Effects.Count) + ((failed == 0) ? "all subsystems ok" : (failed.ToString() + " subsystem(s) FAILED - see above")));
			this._savePending = true;
			this._saveAt = Time.realtimeSinceStartup + 1f;
		}

		private static void Step(string name, Action action, ref int failed)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				failed++;
				LumaEngineBehaviour.Log.LogWarning("APPLY " + name + " failed: " + ex.Message);
			}
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00013828 File Offset: 0x00011A28
		private void ApplyWorldLight()
		{
			if (this._worldLight == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("sunMoon");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			float num = ((effectSettings != null) ? effectSettings.GetFloat("sunIntensity", 1.15f) : 1.15f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("sunWarmth", 0.1f) : 0.1f);
			float num3 = ((effectSettings != null) ? effectSettings.GetFloat("ambientIntensity", 1.1f) : 1.1f);
			float num4 = ((effectSettings != null) ? effectSettings.GetFloat("shadowStrength", 1f) : 1f);
			this._worldLight.Configure(flag, num, num2, num3, num4);
			EffectSettings effectSettings2 = this._settings.Effect("lensFlare");
			bool flag2 = this._settings.Master && effectSettings2 != null && effectSettings2.Enabled;
			float num5 = ((effectSettings2 != null) ? ((float)effectSettings2.GetEnumIndex("mode", new string[] { "Sun", "Moon", "Both" }, 2)) : 2f);
			this._worldLight.SetFlareDiagContext(flag2, num5);
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00013968 File Offset: 0x00011B68
		private void ApplyDynamicLights()
		{
			if (this._dynamicLights == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("dynamicLights");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			float num = ((effectSettings != null) ? effectSettings.GetFloat("intensity", 1f) : 1f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("range", 8f) : 8f);
			int num3 = ((effectSettings != null) ? Mathf.RoundToInt(effectSettings.GetFloat("maxLights", 8f)) : 8);
			float num4 = ((effectSettings != null) ? effectSettings.GetFloat("particleBoost", 2f) : 2f);
			float num5 = ((effectSettings != null) ? effectSettings.GetFloat("flicker", 0.35f) : 0.35f);
			bool flag2 = effectSettings == null || effectSettings.Vr;
			bool flag3 = effectSettings == null || effectSettings.Desktop;
			this._dynamicLights.Configure(flag, flag2, flag3, num, num2, num3, num4, num5);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00013A70 File Offset: 0x00011C70
		private void ApplyRainCoverage()
		{
			RainCoverage rainCoverage = this._rainCoverage;
			if (rainCoverage != null)
			{
				rainCoverage.Configure(this._settings != null && this._settings.Master);
			}
			bool flag = false;
			if (this._settings != null && this._settings.Master)
			{
				string[] array = new string[] { "trueDarkness", "playerShadow", "ssr", "lensFlare", "lumaRain", "sunlight" };
				for (int i = 0; i < array.Length; i++)
				{
					EffectSettings effectSettings = this._settings.Effect(array[i]);
					if (effectSettings != null && effectSettings.Enabled)
					{
						flag = true;
						break;
					}
				}
			}
			RainCoverage rainCoverage2 = this._rainCoverage;
			if (rainCoverage2 == null)
			{
				return;
			}
			rainCoverage2.ConfigureShelterNeeded(flag);
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00013B30 File Offset: 0x00011D30
		private void ApplyRainSensor()
		{
			if (this._rainSensor == null || this._settings == null)
			{
				return;
			}
			bool master = this._settings.Master;
			EffectSettings effectSettings = this._settings.Effect("lumaRain");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			bool flag2 = effectSettings == null || effectSettings.Vr;
			bool flag3 = effectSettings == null || effectSettings.Desktop;
			EffectSettings effectSettings2 = this._settings.Effect("ssr");
			bool flag4 = this._settings.Master && effectSettings2 != null && effectSettings2.Enabled && string.Equals(effectSettings2.GetEnum("mode", "RainOnly"), "RainOnly", StringComparison.OrdinalIgnoreCase);
			EffectSettings effectSettings3 = this._settings.Effect("sunMoon");
			bool flag5 = this._settings.Master && effectSettings3 != null && effectSettings3.Enabled;
			EffectSettings effectSettings4 = this._settings.Effect("sunlight");
			bool flag6 = this._settings.Master && effectSettings4 != null && effectSettings4.Enabled && effectSettings4.GetFloat("intensity", 0.5f) > 0f;
			EffectSettings effectSettings5 = this._settings.Effect("lensFlare");
			bool flag7 = this._settings.Master && effectSettings5 != null && effectSettings5.Enabled;
			this._rainSensor.Configure(master || flag4 || flag5 || flag6 || flag7 || flag, flag, flag2, flag3, 0f);
			float num = 0f;
			try
			{
				num = Mathf.Clamp01(RainSensor.RainFactor);
			}
			catch
			{
			}
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("intensity", 1f) : 1f);
			RainSensor.SetStorm(flag ? Mathf.Clamp01(num * Mathf.Max(1f, num2)) : 0f);
			RainParticles lumaRainFx = this._lumaRainFx;
			if (lumaRainFx != null)
			{
				lumaRainFx.Configure(flag, flag2, flag3, (effectSettings != null) ? effectSettings.GetFloat("intensity", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("fallSpeed", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("dropSize", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("wind", 0.2f) : 0.2f, effectSettings != null && effectSettings.GetEnumIndex("storm", new string[] { "Off", "On" }, 0) == 1, (effectSettings != null) ? effectSettings.GetFloat("lightning", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("lightningSpeed", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("lightningRandomness", 0.5f) : 0.5f);
			}
			PlayerShadow playerShadow = this._playerShadow;
			if (playerShadow != null)
			{
				playerShadow.SetMasterOn(this._settings.Master);
			}
			EffectSettings effectSettings6 = this._settings.Effect("birds");
			bool flag8 = this._settings.Master && effectSettings6 != null && effectSettings6.Enabled;
			Birds birds = this._birds;
			if (birds != null)
			{
				birds.Configure(flag8, effectSettings6 == null || effectSettings6.Vr, effectSettings6 == null || effectSettings6.Desktop, (effectSettings6 != null) ? effectSettings6.GetFloat("density", 1f) : 1f, (effectSettings6 != null) ? effectSettings6.GetFloat("size", 1f) : 1f, (effectSettings6 != null) ? effectSettings6.GetFloat("altitude", 1f) : 1f, (effectSettings6 != null) ? effectSettings6.GetFloat("speed", 1f) : 1f);
			}
			EffectSettings effectSettings7 = this._settings.Effect("butterflies");
			Insects butterflies = this._butterflies;
			if (butterflies != null)
			{
				butterflies.Configure(this._settings.Master && effectSettings7 != null && effectSettings7.Enabled, effectSettings7 == null || effectSettings7.Vr, effectSettings7 == null || effectSettings7.Desktop, (effectSettings7 != null) ? effectSettings7.GetFloat("density", 1f) : 1f, (effectSettings7 != null) ? effectSettings7.GetFloat("size", 1f) : 1f, (effectSettings7 != null) ? effectSettings7.GetFloat("height", 1f) : 1f, (effectSettings7 != null) ? effectSettings7.GetFloat("speed", 1f) : 1f);
			}
			EffectSettings effectSettings8 = this._settings.Effect("bees");
			Insects bees = this._bees;
			if (bees == null)
			{
				return;
			}
			bees.Configure(this._settings.Master && effectSettings8 != null && effectSettings8.Enabled, effectSettings8 == null || effectSettings8.Vr, effectSettings8 == null || effectSettings8.Desktop, (effectSettings8 != null) ? effectSettings8.GetFloat("density", 1f) : 1f, (effectSettings8 != null) ? effectSettings8.GetFloat("size", 1f) : 1f, (effectSettings8 != null) ? effectSettings8.GetFloat("height", 1f) : 1f, (effectSettings8 != null) ? effectSettings8.GetFloat("speed", 1f) : 1f);
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0001409C File Offset: 0x0001229C
		private bool VrPerfBalanced()
		{
			return this._settings != null && this._settings.Master && QualityTiers.CheapTail(this._settings.QualityVr);
		}

		// Token: 0x06000156 RID: 342 RVA: 0x000140C8 File Offset: 0x000122C8
		private bool On(string id)
		{
			Settings settings = this._settings;
			EffectSettings effectSettings = ((settings != null) ? settings.Effect(id) : null);
			return this._settings != null && this._settings.Master && effectSettings != null && effectSettings.Enabled;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x0001410C File Offset: 0x0001230C
		private void ApplyWorldRain()
		{
			if (this._worldRain == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("rain");
			bool flag = this.On("rain");
			float num = ((effectSettings != null) ? effectSettings.GetFloat("amount", 0.25f) : 0.25f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("wind", 0.4f) : 0.4f);
			bool flag2 = effectSettings == null || effectSettings.Vr;
			bool flag3 = effectSettings == null || effectSettings.Desktop;
			EffectSettings effectSettings2 = this._settings.Effect("rainSplash");
			float num3 = ((this.On("rainSplash") && effectSettings2 != null) ? effectSettings2.GetFloat("amount", 0.6f) : 0f);
			this._worldRain.Configure(flag, num, flag2, flag3, this.VrPerfBalanced(), num2, num3);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000141F0 File Offset: 0x000123F0
		private void ApplyParticles()
		{
			if (this._particles == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("dustMotes");
			this._particles.ConfigureDust(this._settings.Master && effectSettings != null && effectSettings.Enabled, (effectSettings == null || effectSettings.Vr) && this._settings.VrAll, effectSettings == null || effectSettings.Desktop, (effectSettings != null) ? effectSettings.GetFloat("density", 0.4f) : 0.4f, (effectSettings != null) ? effectSettings.GetFloat("size", 0.35f) : 0.35f, (effectSettings != null) ? effectSettings.GetFloat("driftSpeed", 0.3f) : 0.3f, (effectSettings != null) ? effectSettings.GetFloat("brightness", 0.5f) : 0.5f, (effectSettings != null) ? ((float)effectSettings.GetEnumIndex("shape", LumaEngineBehaviour.ParticleShapeOptions, 0)) : 0f);
			EffectSettings effectSettings2 = this._settings.Effect("fireflies");
			this._particles.ConfigureFireflies(this._settings.Master && effectSettings2 != null && effectSettings2.Enabled, (effectSettings2 == null || effectSettings2.Vr) && this._settings.VrAll, effectSettings2 == null || effectSettings2.Desktop, (effectSettings2 != null) ? effectSettings2.GetFloat("density", 0.4f) : 0.4f, (effectSettings2 != null) ? effectSettings2.GetFloat("brightness", 0.6f) : 0.6f, (effectSettings2 != null) ? effectSettings2.GetFloat("wanderSpeed", 0.35f) : 0.35f, (effectSettings2 != null) ? ((float)effectSettings2.GetEnumIndex("shape", LumaEngineBehaviour.ParticleShapeOptions, 0)) : 0f);
			EffectSettings effectSettings3 = this._settings.Effect("embers");
			this._particles.ConfigureEmbers(this._settings.Master && effectSettings3 != null && effectSettings3.Enabled, (effectSettings3 == null || effectSettings3.Vr) && this._settings.VrAll, effectSettings3 == null || effectSettings3.Desktop, (effectSettings3 != null) ? effectSettings3.GetFloat("density", 0.5f) : 0.5f, (effectSettings3 != null) ? effectSettings3.GetFloat("riseSpeed", 0.5f) : 0.5f, (effectSettings3 != null) ? effectSettings3.GetFloat("brightness", 0.7f) : 0.7f, (effectSettings3 != null) ? effectSettings3.GetFloat("glow", 0.5f) : 0.5f, (effectSettings3 != null) ? ((float)effectSettings3.GetEnumIndex("shape", LumaEngineBehaviour.ParticleShapeOptions, 0)) : 0f);
			EffectSettings effectSettings4 = this._settings.Effect("fallingLeaves");
			this._particles.ConfigureFallingLeaves(this._settings.Master && effectSettings4 != null && effectSettings4.Enabled, (effectSettings4 == null || effectSettings4.Vr) && this._settings.VrAll, effectSettings4 == null || effectSettings4.Desktop, (effectSettings4 != null) ? effectSettings4.GetFloat("density", 0.5f) : 0.5f, (effectSettings4 != null) ? effectSettings4.GetFloat("fallSpeed", 0.4f) : 0.4f, (effectSettings4 != null) ? effectSettings4.GetFloat("size", 0.5f) : 0.5f, (effectSettings4 != null) ? ((float)effectSettings4.GetEnumIndex("leafType", LumaEngineBehaviour.LeafTypeOptions, 3)) : 3f, (effectSettings4 != null) ? ((float)effectSettings4.GetEnumIndex("shape", LumaEngineBehaviour.ParticleShapeOptions, 0)) : 0f);
			this._particles.ConfigureVrBalanced(this.VrPerfBalanced() || PerfMode.LowCpu);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0001458C File Offset: 0x0001278C
		private void ApplySkySystem()
		{
			if (this._skySystem == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("sunMoon");
			EffectSettings effectSettings2 = this._settings.Effect("nightSky");
			EffectSettings effectSettings3 = effectSettings2;
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			bool flag2 = this._settings.Master && effectSettings2 != null && effectSettings2.Enabled;
			bool flag3 = this._settings.Master && effectSettings3 != null && effectSettings3.Enabled;
			bool flag4 = effectSettings == null || effectSettings.Vr;
			bool flag5 = effectSettings == null || effectSettings.Desktop;
			bool flag6 = effectSettings2 == null || effectSettings2.Vr;
			bool flag7 = effectSettings2 == null || effectSettings2.Desktop;
			bool flag8 = effectSettings2 == null || effectSettings2.Vr;
			bool flag9 = effectSettings2 == null || effectSettings2.Desktop;
			EffectSettings effectSettings4 = this._settings.Effect("lensFlare");
			bool flag10 = this._settings.Master && effectSettings4 != null && effectSettings4.Enabled;
			bool flag11 = effectSettings4 == null || effectSettings4.Vr;
			bool flag12 = effectSettings4 == null || effectSettings4.Desktop;
			float num = ((effectSettings != null) ? effectSettings.GetFloat("sunBrightness", 4f) : 4f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("sunSize", 1.2f) : 1.2f);
			Color color = ((effectSettings != null) ? effectSettings.GetColor("sunTint", new Color(1f, 0.9098f, 0.7529f)) : new Color(1f, 0.9098f, 0.7529f));
			float num3 = ((effectSettings != null) ? effectSettings.GetFloat("moonBrightness", 0.3f) : 0.3f);
			float num4 = ((effectSettings != null) ? effectSettings.GetFloat("moonSize", 0.9f) : 0.9f);
			Color color2 = ((effectSettings != null) ? effectSettings.GetColor("moonTint", new Color(0.749f, 0.8314f, 1f)) : new Color(0.749f, 0.8314f, 1f));
			float num5 = ((effectSettings != null) ? effectSettings.GetFloat("glowFalloff", 0.5f) : 0.5f);
			int num6 = ((effectSettings != null) ? effectSettings.GetEnumIndex("position", new string[] { "Follow Game", "Time of Day", "Real Time" }, 0) : 0);
			float num7 = ((effectSettings != null) ? effectSettings.GetFloat("timeOfDay", 10f) : 10f);
			float num8 = ((effectSettings3 != null) ? effectSettings3.GetFloat("cloudCoverage", 0.4f) : 0.4f);
			float num9 = ((effectSettings3 != null) ? effectSettings3.GetFloat("cloudDensity", 0.5f) : 0.5f);
			float num10 = ((effectSettings3 != null) ? effectSettings3.GetFloat("cloudSpeed", 0.3f) : 0.3f);
			float num11 = ((effectSettings3 != null) ? effectSettings3.GetFloat("height", 0.5f) : 0.5f);
			float num12 = ((effectSettings3 != null) ? effectSettings3.GetFloat("thickness", 0.5f) : 0.5f);
			float num13 = ((effectSettings3 != null) ? effectSettings3.GetFloat("softness", 0.5f) : 0.5f);
			Color color3 = ((effectSettings3 != null) ? effectSettings3.GetColor("cloudTint", Color.white) : Color.white);
			float num14 = ((effectSettings3 != null) ? effectSettings3.GetFloat("cloudSunGlow", 0.5f) : 0.5f);
			bool flag13 = effectSettings2 == null || effectSettings2.GetEnumIndex("customSkies", new string[] { "Off", "On" }, 1) == 1;
			float num15 = ((effectSettings2 != null) ? effectSettings2.GetFloat("horizonWarmth", 0.5f) : 0.5f);
			float num16 = ((effectSettings2 != null) ? effectSettings2.GetFloat("starDensity", 0.5f) : 0.5f);
			float num17 = ((effectSettings2 != null) ? effectSettings2.GetFloat("starBrightness", 0.6f) : 0.6f);
			float num18 = ((effectSettings2 != null) ? effectSettings2.GetFloat("starSize", 0.35f) : 0.35f);
			int num19 = 0;
			float num20 = ((effectSettings2 != null) ? effectSettings2.GetFloat("auroraIntensity", 0.5f) : 0.5f);
			float num21 = ((effectSettings2 != null) ? effectSettings2.GetFloat("auroraSpeed", 0.5f) : 0.5f);
			Color color4 = ((effectSettings2 != null) ? effectSettings2.GetColor("dayZenith", new Color(0.18f, 0.498f, 0.859f)) : new Color(0.18f, 0.498f, 0.859f));
			Color color5 = ((effectSettings2 != null) ? effectSettings2.GetColor("dayHorizon", new Color(0.612f, 0.784f, 0.933f)) : new Color(0.612f, 0.784f, 0.933f));
			float num22 = ((effectSettings2 != null) ? effectSettings2.GetFloat("daySaturation", 1f) : 1f);
			float num23 = ((effectSettings2 != null) ? effectSettings2.GetFloat("dayHue", 0.5f) : 0.5f);
			Color color6 = ((effectSettings2 != null) ? effectSettings2.GetColor("auroraColorA", new Color(0.235f, 0.941f, 0.549f)) : new Color(0.235f, 0.941f, 0.549f));
			Color color7 = ((effectSettings2 != null) ? effectSettings2.GetColor("auroraColorB", new Color(0.235f, 0.612f, 0.941f)) : new Color(0.235f, 0.612f, 0.941f));
			int num24 = ((effectSettings2 != null) ? effectSettings2.GetEnumIndex("method", new string[] { "Auto", "ScreenSpace", "Skybox" }, 0) : 0);
			int num25 = ((effectSettings2 != null) ? effectSettings2.GetEnumIndex("replaceWhen", new string[] { "Always", "Night Only" }, 0) : 0);
			float num26 = ((effectSettings2 != null) ? effectSettings2.GetFloat("strength", 1f) : 1f);
			float num27 = ((effectSettings2 != null) ? effectSettings2.GetFloat("backdropDistance", 300f) : 300f);
			WorldLight worldLight = this._worldLight;
			if (worldLight != null)
			{
				worldLight.ConfigureSunPosition(flag && num24 != 2, flag4, flag5, num6, num7);
			}
			this._skySystem.Configure(flag, flag4, flag5, num, num2, color, num3, num4, color2, num5, num6, num7, flag3, flag6, flag7, this.VrPerfBalanced(), num8, num9, num10, num11, num12, num13, color3, num14, flag2, flag8, flag9, num24, num25, num26, num27, num15, num16, num17, num18, num19, num20, num21, color4, color5, num22, num23, color6, color7, flag10, flag11, flag12);
			LumaSkybox lumaSkybox = this._lumaSkybox;
			if (lumaSkybox != null)
			{
				lumaSkybox.Configure(flag2 && flag13, flag8, flag9);
			}
			SkyShell skyShell = this._skyShell;
			if (skyShell != null)
			{
				skyShell.Configure(flag || flag3, (flag && flag4) || (flag3 && flag6), (flag && flag5) || (flag3 && flag7));
			}
			SkyShell skyShell2 = this._skyShell;
			if (skyShell2 != null)
			{
				skyShell2.MarkDirty();
			}
			SkyOverlay skyOverlay = this._skyOverlay;
			if (skyOverlay != null)
			{
				skyOverlay.Configure(flag, flag4, flag5);
			}
			SkyOverlay skyOverlay2 = this._skyOverlay;
			if (skyOverlay2 == null)
			{
				return;
			}
			skyOverlay2.MarkDirty();
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00014C90 File Offset: 0x00012E90
		private void ApplyMetalSurfaces()
		{
			if (this._metalSurfaces == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("ssr");
			EffectSettings effectSettings2 = this._settings.Effect("ssgi");
			EffectSettings effectSettings3 = this._settings.Effect("playerShadow");
			EffectSettings effectSettings4 = this._settings.Effect("bloom");
			EffectSettings effectSettings5 = this._settings.Effect("ssao");
			EffectSettings effectSettings6 = this._settings.Effect("trueDarkness");
			EffectSettings effectSettings7 = this._settings.Effect("sunlight");
			bool flag = this._settings.Master && ((effectSettings != null && effectSettings.Enabled) || (effectSettings2 != null && effectSettings2.Enabled) || (effectSettings3 != null && effectSettings3.Enabled) || (effectSettings5 != null && effectSettings5.Enabled) || (effectSettings6 != null && effectSettings6.Enabled) || (effectSettings7 != null && effectSettings7.Enabled) || (effectSettings4 != null && effectSettings4.Enabled && effectSettings4.GetFloat("highlights", 0.7f) > 0.0001f));
			this._metalSurfaces.Configure(flag);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00014DB8 File Offset: 0x00012FB8
		private void ApplyTextGuard()
		{
			if (this._textGuard == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("fxaa");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			this._textGuard.Configure(flag, effectSettings == null || effectSettings.Vr, effectSettings == null || effectSettings.Desktop);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00014E28 File Offset: 0x00013028
		private void ApplyPlayerShadow()
		{
			if (this._playerShadow == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("playerShadow");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			float num = ((effectSettings != null) ? effectSettings.GetFloat("intensity", 0.7f) : 0.7f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("softness", 0.4f) : 0.4f);
			int num3 = ((effectSettings != null) ? effectSettings.GetEnumIndex("mode", new string[] { "Sun", "Contact", "Both" }, 0) : 0);
			bool flag2 = effectSettings == null || effectSettings.Vr;
			bool flag3 = effectSettings == null || effectSettings.Desktop;
			this._playerShadow.Configure(flag, flag2, flag3, num, num2, num3);
			EffectSettings effectSettings2 = this._settings.Effect("sunlight");
			EffectSettings effectSettings3 = this._settings.Effect("sunMoon");
			bool flag4 = effectSettings3 != null && effectSettings3.Enabled;
			bool flag5 = this._settings.Master && effectSettings2 != null && effectSettings2.Enabled && effectSettings2.GetFloat("intensity", 0.5f) > 0f && flag4;
			this._playerShadow.ConfigureSunlightNeedShadowAtlas(flag5, (effectSettings2 == null || effectSettings2.Vr) && (effectSettings3 == null || effectSettings3.Vr), (effectSettings2 == null || effectSettings2.Desktop) && (effectSettings3 == null || effectSettings3.Desktop), (effectSettings2 != null) ? effectSettings2.GetFloat("reach", 60f) : 60f, this.VrPerfBalanced());
			try
			{
				EffectSettings effectSettings4 = this._settings.Effect("playerShadow");
				EffectSettings effectSettings5 = this._settings.Effect("sunlight");
				PlayerShadow playerShadow = this._playerShadow;
				if (playerShadow != null)
				{
					playerShadow.LogShadowChain(this._settings.Master, effectSettings5 != null && effectSettings5.Enabled, (effectSettings5 != null) ? effectSettings5.GetFloat("intensity", 0f) : 0f, effectSettings4 != null && effectSettings4.Enabled, (effectSettings4 != null) ? effectSettings4.GetEnum("mode", "Sun") : "?");
				}
			}
			catch
			{
			}
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0001508C File Offset: 0x0001328C
		private void ApplyMapSense()
		{
			if (this._mapSense == null || this._settings == null)
			{
				return;
			}
			bool master = this._settings.Master;
			this._mapSense.Configure(master);
		}

		// Token: 0x0600015E RID: 350 RVA: 0x000150C4 File Offset: 0x000132C4
		private void ApplyWaterSurfaces()
		{
			if (this._waterSurfaces == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("water");
			EffectSettings effectSettings2 = this._settings.Effect("underwater");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled;
			bool flag2 = this._settings.Master && effectSettings2 != null && effectSettings2.Enabled;
			float num = ((effectSettings != null) ? effectSettings.GetFloat("waveStrength", 0.5f) : 0.5f);
			float num2 = ((effectSettings != null) ? effectSettings.GetFloat("waveSpeed", 0.4f) : 0.4f);
			float num3 = ((effectSettings != null) ? effectSettings.GetFloat("waveHeight", 0.35f) : 0.35f);
			float num4 = ((effectSettings != null) ? effectSettings.GetFloat("clarity", 0.8f) : 0.8f);
			float num5 = ((effectSettings != null) ? effectSettings.GetFloat("reflection", 0.7f) : 0.7f);
			float num6 = ((effectSettings != null) ? effectSettings.GetFloat("wetness", 0.5f) : 0.5f);
			int num7 = ((effectSettings != null) ? effectSettings.GetEnumIndex("surfaceStyle", new string[] { "Native", "Raft", "Lake", "Tropical", "Swamp" }, 0) : 0);
			float num8 = ((effectSettings != null) ? effectSettings.GetFloat("refraction", 0.6f) : 0.6f);
			float num9 = ((effectSettings != null) ? effectSettings.GetFloat("glint", 0.5f) : 0.5f);
			Color color = ((effectSettings != null) ? effectSettings.GetColor("deepTint", new Color(0.086f, 0.275f, 0.373f)) : new Color(0.086f, 0.275f, 0.373f));
			Color color2 = ((effectSettings != null) ? effectSettings.GetColor("shallowTint", new Color(0.247f, 0.643f, 0.561f)) : new Color(0.247f, 0.643f, 0.561f));
			bool flag3 = effectSettings == null || effectSettings.Vr;
			bool flag4 = effectSettings == null || effectSettings.Desktop;
			this._waterSurfaces.Configure(flag, flag2, flag3, flag4, num, num2, num3, num4, num5, num6, num7, this.VrPerfBalanced(), color, color2, num8, num9);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00015318 File Offset: 0x00013518
		private void ApplyWaterWaves()
		{
			if (this._waves == null || this._settings == null)
			{
				return;
			}
			EffectSettings effectSettings = this._settings.Effect("waves");
			bool flag = this._settings.Master && effectSettings != null && effectSettings.Enabled && (effectSettings.Vr || effectSettings.Desktop);
			this._waves.Configure(flag, (effectSettings == null || effectSettings.Vr) && this._settings.VrAll, effectSettings == null || effectSettings.Desktop, (effectSettings != null) ? effectSettings.GetFloat("height", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("sizeResponse", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("scale", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("speed", 1f) : 1f, (effectSettings != null) ? effectSettings.GetFloat("crest", 0.35f) : 0.35f, (effectSettings != null) ? effectSettings.GetFloat("detail", 0.5f) : 0.5f, (effectSettings != null) ? effectSettings.GetFloat("openness", 0.7f) : 0.7f, (effectSettings != null) ? effectSettings.GetFloat("splash", 1f) : 1f);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00015478 File Offset: 0x00013678
		private Settings LoadSettings()
		{
			try
			{
				if (File.Exists(this._configPath))
				{
					return Settings.Parse(File.ReadAllText(this._configPath), true);
				}
			}
			catch (Exception ex)
			{
				LumaEngineBehaviour.Log.LogWarning("Could not read settings.json: " + ex.Message);
			}
			return Settings.BuildDefaults();
		}

		// Token: 0x06000161 RID: 353 RVA: 0x000154DC File Offset: 0x000136DC
		private void SaveSettings()
		{
			try
			{
				Directory.CreateDirectory(Path.GetDirectoryName(this._configPath));
				File.WriteAllText(this._configPath, this._settings.ToJson());
			}
			catch (Exception ex)
			{
				LumaEngineBehaviour.Log.LogWarning("Could not write settings.json: " + ex.Message);
			}
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00015540 File Offset: 0x00013740
		private void OnEngineSelfDisabled()
		{
			if (this._renderHooked)
			{
				RenderPipelineManager.beginCameraRendering -= this.OnBeginCamera;
				this._renderHooked = false;
			}
			WaterSurfaces waterSurfaces = this._waterSurfaces;
			if (waterSurfaces != null)
			{
				waterSurfaces.NotifyEngineDisabled();
			}
			SkySystem skySystem = this._skySystem;
			if (skySystem != null)
			{
				skySystem.NotifyEngineDisabled();
			}
			try
			{
				Shader.SetGlobalVector(ShaderIds.HazeParams2Global, Vector4.zero);
			}
			catch
			{
			}
			LumaEngineBehaviour.Log.LogError("Luma Looks render engine self-disabled; game rendering left stock.");
		}

		// Token: 0x06000163 RID: 355 RVA: 0x000155C4 File Offset: 0x000137C4
		private void OnDestroy()
		{
			this.Cleanup();
		}

		// Token: 0x06000164 RID: 356 RVA: 0x000155CC File Offset: 0x000137CC
		private void Cleanup()
		{
			if (this._renderHooked)
			{
				RenderPipelineManager.beginCameraRendering -= this.OnBeginCamera;
				this._renderHooked = false;
			}
			try
			{
				if (this._savePending)
				{
					this.SaveSettings();
				}
			}
			catch
			{
			}
			try
			{
				WorldLight worldLight = this._worldLight;
				if (worldLight != null)
				{
					worldLight.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				DynamicLights dynamicLights = this._dynamicLights;
				if (dynamicLights != null)
				{
					dynamicLights.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				WaterSurfaces waterSurfaces = this._waterSurfaces;
				if (waterSurfaces != null)
				{
					waterSurfaces.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				RainSensor rainSensor = this._rainSensor;
				if (rainSensor != null)
				{
					rainSensor.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				RainCoverage rainCoverage = this._rainCoverage;
				if (rainCoverage != null)
				{
					rainCoverage.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				WorldRain worldRain = this._worldRain;
				if (worldRain != null)
				{
					worldRain.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Particles particles = this._particles;
				if (particles != null)
				{
					particles.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				SkyOverlay skyOverlay = this._skyOverlay;
				if (skyOverlay != null)
				{
					skyOverlay.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				SkyShell skyShell = this._skyShell;
				if (skyShell != null)
				{
					skyShell.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				RainParticles lumaRainFx = this._lumaRainFx;
				if (lumaRainFx != null)
				{
					lumaRainFx.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoadedReapply);
			}
			catch
			{
			}
			try
			{
				Birds birds = this._birds;
				if (birds != null)
				{
					birds.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Insects butterflies = this._butterflies;
				if (butterflies != null)
				{
					butterflies.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				Insects bees = this._bees;
				if (bees != null)
				{
					bees.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				LumaSkybox lumaSkybox = this._lumaSkybox;
				if (lumaSkybox != null)
				{
					lumaSkybox.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				SkySystem skySystem = this._skySystem;
				if (skySystem != null)
				{
					skySystem.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				MetalSurfaces metalSurfaces = this._metalSurfaces;
				if (metalSurfaces != null)
				{
					metalSurfaces.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				TextGuard textGuard = this._textGuard;
				if (textGuard != null)
				{
					textGuard.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				MapSense mapSense = this._mapSense;
				if (mapSense != null)
				{
					mapSense.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				PlayerShadow playerShadow = this._playerShadow;
				if (playerShadow != null)
				{
					playerShadow.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				WaterWaves waves = this._waves;
				if (waves != null)
				{
					waves.Dispose();
				}
			}
			catch
			{
			}
			try
			{
				if (this._runInBgLifted)
				{
					Application.runInBackground = this._runInBgPrev;
					this._runInBgLifted = false;
				}
			}
			catch
			{
			}
			try
			{
				RenderEngine engine = this._engine;
				if (engine != null)
				{
					engine.Dispose();
				}
			}
			catch
			{
			}
			this._worldLight = null;
			this._dynamicLights = null;
			this._waterSurfaces = null;
			this._rainSensor = null;
			this._worldRain = null;
			this._particles = null;
			this._skySystem = null;
			this._skyShell = null;
			this._lumaRainFx = null;
			this._birds = null;
			this._butterflies = null;
			this._bees = null;
			this._lumaSkybox = null;
			this._skyOverlay = null;
			this._metalSurfaces = null;
			this._textGuard = null;
			this._mapSense = null;
			this._playerShadow = null;
			this._waves = null;
			this._engine = null;
		}

		// Token: 0x040002F6 RID: 758
		public const string Guid = "claude.lumalooks";

		// Token: 0x040002F7 RID: 759
		public static ManualLogSource Log;

		// Token: 0x040002F8 RID: 760
		public static string BundleDir;

		// Token: 0x040002F9 RID: 761
		public static byte[] BundleBytes;

		// Token: 0x040002FA RID: 762
		private RenderEngine _engine;

		// Token: 0x040002FB RID: 763
		private WorldLight _worldLight;

		// Token: 0x040002FC RID: 764
		private DynamicLights _dynamicLights;

		// Token: 0x040002FD RID: 765
		private WaterSurfaces _waterSurfaces;

		// Token: 0x040002FE RID: 766
		private RainSensor _rainSensor;

		// Token: 0x040002FF RID: 767
		private RainCoverage _rainCoverage;

		// Token: 0x04000300 RID: 768
		private WorldRain _worldRain;

		// Token: 0x04000301 RID: 769
		private Particles _particles;

		// Token: 0x04000302 RID: 770
		private SkySystem _skySystem;

		// Token: 0x04000303 RID: 771
		private SkyShell _skyShell;

		// Token: 0x04000304 RID: 772
		private RainParticles _lumaRainFx;

		// Token: 0x04000305 RID: 773
		private Birds _birds;

		// Token: 0x04000306 RID: 774
		private int _reapplyAtFrame = -1;

		// Token: 0x04000307 RID: 775
		private Insects _butterflies;

		// Token: 0x04000308 RID: 776
		private Insects _bees;

		// Token: 0x04000309 RID: 777
		private LumaSkybox _lumaSkybox;

		// Token: 0x0400030A RID: 778
		private SkyOverlay _skyOverlay;

		// Token: 0x0400030B RID: 779
		private MetalSurfaces _metalSurfaces;

		// Token: 0x0400030C RID: 780
		private TextGuard _textGuard;

		// Token: 0x0400030D RID: 781
		private MapSense _mapSense;

		// Token: 0x0400030E RID: 782
		private PlayerShadow _playerShadow;

		// Token: 0x0400030F RID: 783
		private WaterWaves _waves;

		// Token: 0x04000311 RID: 785
		private Settings _settings;

		// Token: 0x04000312 RID: 786
		private string _configPath;

		// Token: 0x04000313 RID: 787
		private bool _renderHooked;

		// Token: 0x04000314 RID: 788
		private bool _runInBgPrev;

		// Token: 0x04000315 RID: 789
		private bool _runInBgLifted;

		// Token: 0x04000316 RID: 790
		private float _fps = 90f;

		// Token: 0x04000317 RID: 791
		private float _nextStats;

		// Token: 0x04000318 RID: 792
		private float _launchPollAt;

		// Token: 0x04000319 RID: 793
		private readonly StringBuilder _sb = new StringBuilder(64);

		// Token: 0x0400031A RID: 794
		private bool _savePending;

		// Token: 0x0400031B RID: 795
		private float _saveAt;

		// Token: 0x0400031D RID: 797
		private static readonly string[] CloudsOnOff = new string[] { "Off", "On" };

		// Token: 0x0400031E RID: 798
		private static readonly string[] ParticleShapeOptions = new string[] { "Dot", "Square" };

		// Token: 0x0400031F RID: 799
		private static readonly string[] LeafTypeOptions = new string[] { "Oak", "Maple", "Pine Needle", "Mixed" };
	}
}
