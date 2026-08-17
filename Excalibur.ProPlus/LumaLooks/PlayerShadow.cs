using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000021 RID: 33
	internal sealed class PlayerShadow
	{
		// Token: 0x0600011E RID: 286 RVA: 0x00010168 File Offset: 0x0000E368
		internal static Vector3 SunlightCascadeSplit(float dist)
		{
			float num = Mathf.Max(dist, 1f);
			return new Vector3(Mathf.Min(12f / num, PlayerShadow.CascadeBaseSplit.x), Mathf.Min(37.5f / num, PlayerShadow.CascadeBaseSplit.y), Mathf.Min(82.5f / num, PlayerShadow.CascadeBaseSplit.z));
		}

		// Token: 0x0600011F RID: 287 RVA: 0x000101C8 File Offset: 0x0000E3C8
		public PlayerShadow(ManualLogSource log)
		{
			this._log = log;
			this._occluders = new SunlightOccluders(log);
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x06000120 RID: 288 RVA: 0x000102B2 File Offset: 0x0000E4B2
		public void AttachSkyDome(SkyDome dome)
		{
			this._occluders.AttachSkyDome(dome);
		}

		// Token: 0x06000121 RID: 289 RVA: 0x000102C0 File Offset: 0x0000E4C0
		internal int RecordPrimeDraws(CommandBuffer cmd, Material mat)
		{
			if (this._occluders == null)
			{
				return 0;
			}
			return this._occluders.RecordPrimeDraws(cmd, mat);
		}

		// Token: 0x06000122 RID: 290 RVA: 0x000102D9 File Offset: 0x0000E4D9
		public void AttachGhostShader(Shader s)
		{
			this._ghostShader = s;
			this._occluders.AttachGhostShader(s);
		}

		// Token: 0x06000123 RID: 291 RVA: 0x000102EE File Offset: 0x0000E4EE
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed, float intensity, float softness, int mode)
		{
			this._want = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._intensity = Mathf.Clamp01(intensity);
			this._softness = Mathf.Clamp01(softness);
			this._mode = Mathf.Clamp(mode, 0, 2);
		}

		// Token: 0x06000124 RID: 292 RVA: 0x0001032E File Offset: 0x0000E52E
		public void ConfigureSunlightNeedShadowAtlas(bool want, bool vrAllowed, bool desktopAllowed, float uiReach, bool vrBalanced)
		{
			this._sunlightWantAtlas = want;
			this._sunlightVrAllowed = vrAllowed;
			this._sunlightDesktopAllowed = desktopAllowed;
			this._sunlightUiReach = Mathf.Clamp(uiReach, 0f, 1000f);
			this._sunlightVrBalanced = vrBalanced;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00010364 File Offset: 0x0000E564
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == (LoadSceneMode)1)
			{
				return;
			}
			this._modified.Clear();
			this._current.Clear();
			this._geometricApplied = false;
			this._sun = null;
			this._sunCaptured = false;
			this._nextScanAt = 0f;
			this._sceneJustLoaded = true;
			this._lastLoggedRenderers = -1;
			this._occluders.NotifySceneChanged();
		}

		// Token: 0x06000126 RID: 294 RVA: 0x000103C8 File Offset: 0x0000E5C8
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
				bool flag2 = this._want && (flag ? this._vrAllowed : this._desktopAllowed) && (this._mode == 0 || this._mode == 2);
				bool flag3 = this._sunlightWantAtlas && (flag ? this._sunlightVrAllowed : this._sunlightDesktopAllowed);
				this.UpdateSunOcclusion(this._masterOn);
				int num = ((!flag) ? 0 : (this._sunlightVrBalanced ? 2 : 1));
				int num2 = (flag3 ? ((num == 2) ? 2048 : 4096) : 0);
				if (flag3)
				{
					this.EnsureReflection();
					this._occluders.Maintain(this._tVRRig, Mathf.Max(this._sunlightUiReach, 22f), num);
				}
				else
				{
					this._occluders.RestoreAll();
				}
				if (!flag2 && !flag3)
				{
					if (this._geometricApplied || this._urpCaptured || this._sunCaptured || this._modified.Count > 0)
					{
						this.RestoreAll();
					}
					if (this._camLifts.Count > 0)
					{
						this.RestoreCameraLifts();
					}
					this._loggedAtlasOnly = false;
				}
				else if (!flag2)
				{
					float realtimeSinceStartup = Time.realtimeSinceStartup;
					if (realtimeSinceStartup >= this._nextScanAt)
					{
						this._nextScanAt = realtimeSinceStartup + 3f;
						this.RescanRigs();
					}
					this.MaintainUrpShadowSupport(flag3, num2);
					this.MaintainSun(false);
					this.MaintainCameraLifts(false, flag3);
					this._geometricApplied = false;
					if (!this._loggedAtlasOnly)
					{
						this._loggedAtlasOnly = true;
						Light sun = this._sun;
						UniversalRenderPipelineAsset universalRenderPipelineAsset = ActiveUrpAsset.Current;
						this._log.LogInfo(string.Concat(new string[]
						{
							"PlayerShadow: SUNLIGHT ATLAS KEEP-ALIVE engaged (the `sunlight` effect marches URP's main-light shadow atlas; `playerShadow` itself is off for this device). mainLightShadows=",
							(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.supportsMainLightShadows.ToString() : "?",
							" shadowDistance=",
							(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.shadowDistance.ToString("0.#") : "?",
							"m cascades=",
							(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.shadowCascadeCount.ToString() : "?",
							" mainLightRenderingMode=",
							(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.mainLightRenderingMode.ToString() : "?",
							" asset='",
							(universalRenderPipelineAsset != null) ? universalRenderPipelineAsset.name : "(none)",
							"' ",
							string.Format("(maintained = max(ui reach {0:0.#}m, floor {1:0.#}m) ", this._sunlightUiReach, 22f),
							"while sunlight is wanted — rays-realism2 §A: this is the value RenderEngine clamps sunlight `reach` to, so eff follows the slider instead of pinning at the 22 m player-shadow bubble; cascades forced to 4 near-biased (0.08/0.25/0.55) so the gorilla keeps near-cascade texel density) sun='",
							(sun != null) ? sun.name : "(none)",
							"' shadows=",
							(sun != null) ? sun.shadows.ToString() : "?",
							" strength=",
							(sun != null) ? sun.shadowStrength.ToString("0.##") : "?",
							" (strength deliberately NOT lifted — the shader forces it to 1 internally, so lifting here would only steal WorldLight's Shadow Strength slider). No player-rig casting is forced by this path. Fully reversible."
						}));
					}
				}
				else
				{
					this._loggedAtlasOnly = false;
					this.EnsureReflection();
					float realtimeSinceStartup2 = Time.realtimeSinceStartup;
					if (this._sceneJustLoaded)
					{
						this._sceneJustLoaded = false;
						this._nextScanAt = realtimeSinceStartup2 + 2f;
					}
					this.MaintainUrpShadowSupport(flag3, num2);
					this.MaintainCameraLifts(true, flag3);
					this.MaintainSun(true);
					if (realtimeSinceStartup2 >= this._nextScanAt)
					{
						this._nextScanAt = realtimeSinceStartup2 + 3f;
						this.RescanRigs();
					}
					this._geometricApplied = true;
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("PlayerShadow tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x000107E4 File Offset: 0x0000E9E4
		private void MaintainSun(bool allowStrengthLift)
		{
			Light light = WorldLight.ActiveSun;
			if (light == null)
			{
				light = RenderSettings.sun;
			}
			if (light == null)
			{
				return;
			}
			if (this._sunCaptured && light.GetInstanceID() != this._capturedSunId)
			{
				this.RestoreSun();
			}
			if (!this._sunCaptured)
			{
				this._capturedSunId = light.GetInstanceID();
				this._origSunShadows = light.shadows;
				this._sun = light;
				this._sunCaptured = true;
				this._shadowLifted = false;
			}
			if (allowStrengthLift)
			{
				LightShadows lightShadows = (LightShadows)((this._softness >= 0.5f) ? 2 : 1);
				if (light.shadows != lightShadows)
				{
					light.shadows = lightShadows;
				}
			}
			else if (light.shadows == null)
			{
				light.shadows = (LightShadows)2;
			}
			if (!allowStrengthLift)
			{
				if (this._shadowLifted)
				{
					if (Mathf.Abs(light.shadowStrength - 0.7f) <= 0.001f)
					{
						light.shadowStrength = this._preLiftShadow;
					}
					this._shadowLifted = false;
					return;
				}
			}
			else if (light.shadowStrength < 0.05f)
			{
				if (!this._shadowLifted)
				{
					this._preLiftShadow = light.shadowStrength;
					this._shadowLifted = true;
				}
				if (light.shadowStrength != 0.7f)
				{
					light.shadowStrength = 0.7f;
					return;
				}
			}
			else if (this._shadowLifted && Mathf.Abs(light.shadowStrength - 0.7f) > 0.001f)
			{
				this._shadowLifted = false;
			}
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0001093C File Offset: 0x0000EB3C
		private void MaintainUrpShadowSupport(bool sunlightAtlasOn, int targetShadowmapRes)
		{
			UniversalRenderPipelineAsset universalRenderPipelineAsset = ActiveUrpAsset.Current;
			if (universalRenderPipelineAsset == null)
			{
				return;
			}
			if (!PlayerShadow._fiResolved)
			{
				PlayerShadow._fiResolved = true;
				try
				{
					PlayerShadow._fiMainLightShadows = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightShadowsSupported", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				catch
				{
					PlayerShadow._fiMainLightShadows = null;
				}
			}
			if (!this._urpCaptured || this._urp != universalRenderPipelineAsset)
			{
				if (this._urpCaptured && this._urp != universalRenderPipelineAsset)
				{
					this.RestoreUrpShadowSupport();
				}
				this._urp = universalRenderPipelineAsset;
				this._origSupportsMainLightShadows = universalRenderPipelineAsset.supportsMainLightShadows;
				this._origShadowDistance = universalRenderPipelineAsset.shadowDistance;
				this._origShadowCascadeCount = universalRenderPipelineAsset.shadowCascadeCount;
				this._origCascade2Split = universalRenderPipelineAsset.cascade2Split;
				this._origCascade3Split = universalRenderPipelineAsset.cascade3Split;
				this._origCascade4Split = universalRenderPipelineAsset.cascade4Split;
				this._origMainLightShadowmapResolution = universalRenderPipelineAsset.mainLightShadowmapResolution;
				this._origSupportsHdr = universalRenderPipelineAsset.supportsHDR;
				this._urpCaptured = true;
				if (!this._origSupportsMainLightShadows)
				{
					this._log.LogInfo("PlayerShadow: URP asset had main-light shadows OFF — enabling them (reversibly) so the gorilla's geometric shadow renders.");
				}
			}
			if (!universalRenderPipelineAsset.supportsMainLightShadows && PlayerShadow._fiMainLightShadows != null)
			{
				try
				{
					PlayerShadow._fiMainLightShadows.SetValue(universalRenderPipelineAsset, true);
				}
				catch
				{
				}
			}
			if (this._masterOn && !universalRenderPipelineAsset.supportsHDR)
			{
				bool flag = false;
				try
				{
					universalRenderPipelineAsset.supportsHDR = true;
					flag = universalRenderPipelineAsset.supportsHDR;
				}
				catch
				{
				}
				if (!flag)
				{
					if (this._fiSupportsHdr == null)
					{
						try
						{
							this._fiSupportsHdr = typeof(UniversalRenderPipelineAsset).GetField("m_SupportsHDR", BindingFlags.Instance | BindingFlags.NonPublic);
						}
						catch
						{
							this._fiSupportsHdr = null;
						}
					}
					if (this._fiSupportsHdr != null)
					{
						try
						{
							this._fiSupportsHdr.SetValue(universalRenderPipelineAsset, true);
						}
						catch
						{
						}
					}
				}
				if (!this._loggedHdrAsset && universalRenderPipelineAsset.supportsHDR)
				{
					this._loggedHdrAsset = true;
					this._log.LogInfo("PlayerShadow: URP asset HDR enabled (reversibly) - the camera colour target can now carry values above 1.0, so lit-side brightening reaches the tonemap instead of clipping to white.");
				}
			}
			else if (!this._masterOn && !this._origSupportsHdr && universalRenderPipelineAsset.supportsHDR && this._urpCaptured)
			{
				try
				{
					universalRenderPipelineAsset.supportsHDR = false;
				}
				catch
				{
				}
				if (this._fiSupportsHdr != null && universalRenderPipelineAsset.supportsHDR)
				{
					try
					{
						this._fiSupportsHdr.SetValue(universalRenderPipelineAsset, false);
					}
					catch
					{
					}
				}
			}
			float num = (sunlightAtlasOn ? Mathf.Max(this._sunlightUiReach, 120f) : 120f);
			num = Mathf.Min(num, 150f);
			if (universalRenderPipelineAsset.shadowDistance < num)
			{
				try
				{
					universalRenderPipelineAsset.shadowDistance = num;
					this._appliedShadowDistance = num;
					goto IL_02C6;
				}
				catch
				{
					goto IL_02C6;
				}
			}
			if (this._appliedShadowDistance > num && Mathf.Abs(universalRenderPipelineAsset.shadowDistance - this._appliedShadowDistance) <= 0.01f)
			{
				float num2 = Mathf.Max(num, this._origShadowDistance);
				try
				{
					universalRenderPipelineAsset.shadowDistance = num2;
					this._appliedShadowDistance = ((num2 > this._origShadowDistance) ? num2 : (-1f));
				}
				catch
				{
				}
			}
			IL_02C6:
			this._cascadesApplied = true;
			try
			{
				if (universalRenderPipelineAsset.shadowCascadeCount != 4)
				{
					universalRenderPipelineAsset.shadowCascadeCount = 4;
				}
				Vector3 vector = PlayerShadow.SunlightCascadeSplit(num);
				if (universalRenderPipelineAsset.cascade4Split != vector)
				{
					universalRenderPipelineAsset.cascade4Split = vector;
				}
			}
			catch
			{
			}
			if (targetShadowmapRes > 0 && universalRenderPipelineAsset.mainLightShadowmapResolution < targetShadowmapRes)
			{
				try
				{
					universalRenderPipelineAsset.mainLightShadowmapResolution = targetShadowmapRes;
					this._appliedShadowmapRes = targetShadowmapRes;
					goto IL_0371;
				}
				catch
				{
					goto IL_0371;
				}
			}
			if (this._appliedShadowmapRes > 0 && targetShadowmapRes < this._appliedShadowmapRes && universalRenderPipelineAsset.mainLightShadowmapResolution == this._appliedShadowmapRes)
			{
				int num3 = Mathf.Max(targetShadowmapRes, this._origMainLightShadowmapResolution);
				try
				{
					universalRenderPipelineAsset.mainLightShadowmapResolution = num3;
					this._appliedShadowmapRes = ((num3 > this._origMainLightShadowmapResolution) ? num3 : 0);
				}
				catch
				{
				}
			}
			IL_0371:
			if (!PlayerShadow._fiRenderModeResolved)
			{
				PlayerShadow._fiRenderModeResolved = true;
				try
				{
					PlayerShadow._fiMainLightRenderMode = typeof(UniversalRenderPipelineAsset).GetField("m_MainLightRenderingMode", BindingFlags.Instance | BindingFlags.NonPublic);
				}
				catch
				{
					PlayerShadow._fiMainLightRenderMode = null;
				}
			}
			if (!this._renderModeCaptured)
			{
				this._origMainLightRenderMode = universalRenderPipelineAsset.mainLightRenderingMode;
				this._renderModeCaptured = true;
			}
			if (universalRenderPipelineAsset.mainLightRenderingMode != LightRenderingMode.PerPixel && PlayerShadow._fiMainLightRenderMode != null)
			{
				try
				{
					PlayerShadow._fiMainLightRenderMode.SetValue(universalRenderPipelineAsset, LightRenderingMode.PerPixel);
				}
				catch
				{
				}
			}
			float shadowDistance = universalRenderPipelineAsset.shadowDistance;
			bool supportsMainLightShadows = universalRenderPipelineAsset.supportsMainLightShadows;
			LightRenderingMode mainLightRenderingMode = universalRenderPipelineAsset.mainLightRenderingMode;
			int shadowCascadeCount = universalRenderPipelineAsset.shadowCascadeCount;
			bool flag2 = supportsMainLightShadows && shadowDistance >= num - 0.01f && mainLightRenderingMode == LightRenderingMode.PerPixel;
			int num4 = (flag2 ? 1 : 0) | (shadowCascadeCount << 1) | (Mathf.Clamp(Mathf.RoundToInt(shadowDistance), 0, 4095) << 5);
			if (num4 != this._loggedAtlasHealthy)
			{
				this._loggedAtlasHealthy = num4;
				this._log.LogInfo(string.Concat(new string[]
				{
					"PlayerShadow: URP shadow atlas ",
					flag2 ? "READY" : "NOT READY",
					" ",
					string.Format("(read back — supportsMainLightShadows={0} ", supportsMainLightShadows),
					string.Format("cascades={0} dist={1:0.#}m (maintained target ", shadowCascadeCount, shadowDistance),
					string.Format("{0:0.#}m{1}) ", num, sunlightAtlasOn ? ", sunlight wanted: max(ui reach, 22)" : ", 22 m player-shadow floor"),
					string.Format("mainLightRenderingMode={0} asset='{1}'). These are the three ", mainLightRenderingMode, universalRenderPipelineAsset.name),
					"ASSET-side preconditions the `sunlight` gate checks; the gate additionally requires the camera's Render Shadows toggle — maintained per camera since 2026-07-23 by this keep-alive's camera-lift (see the 'Render Shadows LIFTED' lines) — and a shadow-casting sun, whose live values the SUNLIGHT line's own read-backs carry."
				}));
			}
		}

		// Token: 0x06000129 RID: 297 RVA: 0x00010EF4 File Offset: 0x0000F0F4
		private void RestoreUrpShadowSupport()
		{
			if (!this._urpCaptured)
			{
				return;
			}
			if (this._urp != null)
			{
				try
				{
					if (PlayerShadow._fiMainLightShadows != null)
					{
						PlayerShadow._fiMainLightShadows.SetValue(this._urp, this._origSupportsMainLightShadows);
					}
					this._urp.shadowDistance = this._origShadowDistance;
					if (this._urp.supportsHDR != this._origSupportsHdr)
					{
						try
						{
							this._urp.supportsHDR = this._origSupportsHdr;
						}
						catch
						{
						}
						if (this._fiSupportsHdr != null && this._urp.supportsHDR != this._origSupportsHdr)
						{
							try
							{
								this._fiSupportsHdr.SetValue(this._urp, this._origSupportsHdr);
							}
							catch
							{
							}
						}
					}
					this._urp.shadowCascadeCount = this._origShadowCascadeCount;
					this._urp.cascade2Split = this._origCascade2Split;
					this._urp.cascade3Split = this._origCascade3Split;
					this._urp.cascade4Split = this._origCascade4Split;
					this._urp.mainLightShadowmapResolution = this._origMainLightShadowmapResolution;
					if (this._renderModeCaptured && PlayerShadow._fiMainLightRenderMode != null)
					{
						PlayerShadow._fiMainLightRenderMode.SetValue(this._urp, this._origMainLightRenderMode);
					}
				}
				catch
				{
				}
			}
			this._renderModeCaptured = false;
			this._cascadesApplied = false;
			this._appliedShadowDistance = -1f;
			this._appliedShadowmapRes = 0;
			this._loggedAtlasHealthy = -1;
			this._urp = null;
			this._urpCaptured = false;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x000110CC File Offset: 0x0000F2CC
		public void NoteQualifyingCamera(UniversalAdditionalCameraData data, bool isVr)
		{
			if (data == null)
			{
				return;
			}
			int frameCount = Time.frameCount;
			for (int i = 0; i < this._camLifts.Count; i++)
			{
				if (this._camLifts[i].Data == data)
				{
					PlayerShadow.CamLift camLift = this._camLifts[i];
					camLift.LastSeenFrame = frameCount;
					camLift.IsVr = isVr;
					this.ApplyLiftNow(ref camLift);
					this.ApplyHdrLift(ref camLift);
					this._camLifts[i] = camLift;
					return;
				}
			}
			if (this._camLifts.Count >= 16)
			{
				return;
			}
			PlayerShadow.CamLift camLift2 = new PlayerShadow.CamLift
			{
				Data = data,
				IsVr = isVr,
				LastSeenFrame = frameCount
			};
			this.ApplyLiftNow(ref camLift2);
			this.ApplyHdrLift(ref camLift2);
			this._camLifts.Add(camLift2);
		}

		// Token: 0x0600012B RID: 299 RVA: 0x000111A0 File Offset: 0x0000F3A0
		private void ApplyLiftNow(ref PlayerShadow.CamLift e)
		{
			if (e.Data == null)
			{
				return;
			}
			if (!this._sunlightWantAtlas || !(e.IsVr ? this._sunlightVrAllowed : this._sunlightDesktopAllowed))
			{
				return;
			}
			if (e.Data.renderShadows)
			{
				return;
			}
			if (!e.Lifted)
			{
				e.Orig = false;
				e.Lifted = true;
				if (!this._loggedBeginCamLift)
				{
					this._loggedBeginCamLift = true;
					this._log.LogInfo("PlayerShadow: applying the Render Shadows lift in beginCameraRendering (latest-writer-wins) — the Update-side lift was being reset before URP's shadow pass, leaving an EMPTY main-light atlas and letting the ray march glow through solids. This is the fix for that leak; logged once.");
				}
			}
			try
			{
				e.Data.renderShadows = true;
			}
			catch
			{
			}
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00011248 File Offset: 0x0000F448
		private void ApplyHdrLift(ref PlayerShadow.CamLift e)
		{
			if (e.Data == null || !this._masterOn)
			{
				return;
			}
			Camera camera = null;
			try
			{
				camera = e.Data.GetComponent<Camera>();
			}
			catch
			{
			}
			if (camera == null)
			{
				return;
			}
			if (camera.allowHDR)
			{
				return;
			}
			if (!e.HdrLifted)
			{
				e.OrigAllowHdr = camera.allowHDR;
				e.HdrLifted = true;
				if (!this._loggedHdrLift)
				{
					this._loggedHdrLift = true;
					this._log.LogInfo("PlayerShadow: lifting HDR on camera '" + camera.name + "' (reversibly). GT ships an 8-bit camera colour target, which hard-clips every lit-side multiply above 1.0 to white while darkening survives intact - that is why sun brightness deepened shadows without brightening the lit side. Logged once.");
				}
			}
			try
			{
				camera.allowHDR = true;
			}
			catch
			{
			}
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00011308 File Offset: 0x0000F508
		private void UpdateSunOcclusion(bool want)
		{
			if (!want)
			{
				this._sunVisValid = false;
				this._rigCount = 0;
				this.PushSunVis();
				return;
			}
			try
			{
				Transform transform = null;
				for (int i = 0; i < this._camLifts.Count; i++)
				{
					UniversalAdditionalCameraData data = this._camLifts[i].Data;
					if (data != null)
					{
						transform = data.transform;
						break;
					}
				}
				Vector3 resolvedSunDir = WorldLight.ResolvedSunDir;
				if (resolvedSunDir.sqrMagnitude < 1E-06f)
				{
					this._sunVisValid = false;
					this._rigCount = 0;
					this.PushSunVis();
					return;
				}
				resolvedSunDir.Normalize();
				Vector3 vector = Vector3.Cross(Vector3.up, resolvedSunDir);
				if (vector.sqrMagnitude < 1E-06f)
				{
					vector = Vector3.right;
				}
				vector.Normalize();
				this.RefreshRigList();
				float num = Mathf.Clamp01(1f - Mathf.Exp(-Time.deltaTime / 0.12f));
				this._rigCount = 0;
				int num2 = 0;
				while (num2 < this._rigXforms.Count && this._rigCount < 16)
				{
					Transform transform2 = this._rigXforms[num2];
					if (!(transform2 == null))
					{
						Vector3 vector2 = transform2.position + Vector3.up * 0.5f;
						int instanceID = transform2.GetInstanceID();
						float num3;
						if ((transform != null && (vector2 - transform.position).sqrMagnitude < 9f) || ((Time.frameCount + num2) & 3) == 0)
						{
							num3 = PlayerShadow.SunClearFraction(vector2, resolvedSunDir, vector);
						}
						else if (!this._rigVis.TryGetValue(instanceID, out num3))
						{
							num3 = PlayerShadow.SunClearFraction(vector2, resolvedSunDir, vector);
						}
						float num5;
						float num4 = (this._rigVis.TryGetValue(instanceID, out num5) ? Mathf.Lerp(num5, num3, num) : num3);
						this._rigVis[instanceID] = num4;
						Vector4[] rigSun = PlayerShadow._rigSun;
						int rigCount = this._rigCount;
						this._rigCount = rigCount + 1;
						rigSun[rigCount] = new Vector4(vector2.x, vector2.y, vector2.z, num4);
					}
					num2++;
				}
				for (int j = this._rigCount; j < 16; j++)
				{
					PlayerShadow._rigSun[j] = new Vector4(1000000f, 1000000f, 1000000f, 1f);
				}
				if (transform != null)
				{
					float num6 = PlayerShadow.SunClearFraction(transform.position - Vector3.up * 0.25f, resolvedSunDir, vector);
					this._sunVis = Mathf.Lerp(this._sunVis, num6, num);
					this._sunVisValid = true;
				}
				else
				{
					this._sunVisValid = this._rigCount > 0;
				}
				if (!this._loggedRigs && this._rigCount > 0)
				{
					this._loggedRigs = true;
					this._log.LogInfo("PlayerShadow: per-rig sun occlusion online - " + this._rigCount.ToString() + " rig(s), each getting their own sun-ward rays, so a player in shade darkens whether or not the wall was ever a shadow caster. Logged once.");
				}
			}
			catch
			{
				this._sunVisValid = false;
				this._rigCount = 0;
			}
			this.PushSunVis();
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00011638 File Offset: 0x0000F838
		private static float SunClearFraction(Vector3 c, Vector3 sunDir, Vector3 side)
		{
			int num = 0;
			for (int i = -1; i <= 1; i++)
			{
				Vector3 vector = c + side * ((float)i * 0.35f);
				Vector3 vector2 = vector + sunDir * 0.4f;
				RaycastHit raycastHit = default;
				if (!Physics.Raycast(vector, sunDir, out raycastHit, 0.4f, -1, QueryTriggerInteraction.Ignore) || raycastHit.distance <= 0.15f || !PlayerShadow.IsSolidOccluder(raycastHit.collider))
				{
					int num2 = Physics.RaycastNonAlloc(vector2, sunDir, PlayerShadow._hitBuf, 60f, -1, QueryTriggerInteraction.Ignore);
					bool flag = false;
					int num3 = 0;
					while (num3 < num2 && !flag)
					{
						if (PlayerShadow.IsSolidOccluder(PlayerShadow._hitBuf[num3].collider))
						{
							flag = true;
						}
						num3++;
					}
					if (!flag)
					{
						num++;
					}
				}
			}
			return (float)num / 3f;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00011704 File Offset: 0x0000F904
		private static bool IsSolidOccluder(Collider col)
		{
			if (col == null)
			{
				return false;
			}
			int instanceID = col.GetInstanceID();
			bool flag;
			if (PlayerShadow._solidCache.TryGetValue(instanceID, out flag))
			{
				return flag;
			}
			flag = PlayerShadow.ClassifySolid(col);
			if (PlayerShadow._solidCache.Count > 4096)
			{
				PlayerShadow._solidCache.Clear();
			}
			PlayerShadow._solidCache[instanceID] = flag;
			return flag;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00011764 File Offset: 0x0000F964
		private static bool NameLooksLikeBarrier(string n)
		{
			if (string.IsNullOrEmpty(n))
			{
				return false;
			}
			for (int i = 0; i < PlayerShadow.BarrierWords.Length; i++)
			{
				if (n.IndexOf(PlayerShadow.BarrierWords[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x000117A4 File Offset: 0x0000F9A4
		private static bool ClassifySolid(Collider col)
		{
			bool flag;
			try
			{
				Renderer renderer = col.GetComponent<Renderer>();
				if (renderer == null)
				{
					renderer = col.GetComponentInParent<Renderer>();
				}
				if (renderer == null)
				{
					renderer = col.GetComponentInChildren<Renderer>();
				}
				if (renderer == null && col.transform.parent != null)
				{
					renderer = col.transform.parent.GetComponentInChildren<Renderer>();
				}
				if (renderer == null)
				{
					flag = false;
				}
				else if (!renderer.enabled || renderer.forceRenderingOff || !renderer.gameObject.activeInHierarchy)
				{
					flag = false;
				}
				else
				{
					Material[] sharedMaterials = renderer.sharedMaterials;
					if (sharedMaterials == null || sharedMaterials.Length == 0)
					{
						flag = false;
					}
					else
					{
						foreach (Material material in sharedMaterials)
						{
							if (!(material == null) && material.renderQueue < 3000)
							{
								return true;
							}
						}
						flag = false;
					}
				}
			}
			catch
			{
				flag = true;
			}
			return flag;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x00011890 File Offset: 0x0000FA90
		private void RefreshRigList()
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup < this._nextRigScanAt && this._rigXforms.Count > 0)
			{
				return;
			}
			this._nextRigScanAt = realtimeSinceStartup + 1.5f;
			if (!this._rigTypeSearched)
			{
				this._rigTypeSearched = true;
				try
				{
					Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
					int num = 0;
					while (num < assemblies.Length && this._tRig == null)
					{
						try
						{
							this._tRig = assemblies[num].GetType("VRRig", false);
						}
						catch
						{
						}
						num++;
					}
				}
				catch
				{
					this._tRig = null;
				}
				if (this._tRig == null)
				{
					this._log.LogWarning("PlayerShadow: VRRig type not found - per-rig sun occlusion falls back to the local player only.");
				}
			}
			if (this._tRig == null)
			{
				this._rigXforms.Clear();
				return;
			}
			this._rigXforms.Clear();
			try
			{
				UnityEngine.Object[] array = UnityEngine.Object.FindObjectsByType(this._tRig, 0, 0);
				if (array != null)
				{
					int num2 = 0;
					while (num2 < array.Length && this._rigXforms.Count < 16)
					{
						Component component = array[num2] as Component;
						if (component != null && component.transform != null)
						{
							this._rigXforms.Add(component.transform);
						}
						num2++;
					}
				}
			}
			catch
			{
			}
			if (this._rigVis.Count > 64)
			{
				this._rigVis.Clear();
			}
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00011A18 File Offset: 0x0000FC18
		private void PushSunVis()
		{
			try
			{
				Shader.SetGlobalVector(ShaderIds.PlayerSun, new Vector4(this._sunVisValid ? this._sunVis : 1f, this._sunVisValid ? 1f : 0f, (float)this._rigCount, 0f));
				Shader.SetGlobalVectorArray(ShaderIds.RigSun, PlayerShadow._rigSun);
			}
			catch
			{
			}
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00011A90 File Offset: 0x0000FC90
		public void SetMasterOn(bool on)
		{
			bool masterOn = this._masterOn;
			this._masterOn = on;
			if (masterOn && !on)
			{
				this._sunVisValid = false;
				this._sunVis = 1f;
				this.PushSunVis();
			}
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00011ABC File Offset: 0x0000FCBC
		private void MaintainCameraLifts(bool geometricWant, bool sunlightAtlasWant)
		{
			int frameCount = Time.frameCount;
			for (int i = this._camLifts.Count - 1; i >= 0; i--)
			{
				PlayerShadow.CamLift camLift = this._camLifts[i];
				if (camLift.Data == null)
				{
					this._camLifts.RemoveAt(i);
				}
				else if (frameCount - camLift.LastSeenFrame > 240)
				{
					this.RestoreCamLift(ref camLift);
					this._camLifts.RemoveAt(i);
				}
				else if (!geometricWant && (!sunlightAtlasWant || !(camLift.IsVr ? this._sunlightVrAllowed : this._sunlightDesktopAllowed)))
				{
					if (camLift.Lifted)
					{
						this.RestoreCamLift(ref camLift);
						this._camLifts[i] = camLift;
					}
				}
				else if (!camLift.Data.renderShadows)
				{
					if (!camLift.Lifted)
					{
						camLift.Orig = false;
						camLift.Lifted = true;
						this._log.LogInfo("PlayerShadow: camera '" + camLift.Data.name + "' URP Render Shadows LIFTED (UniversalAdditionalCameraData.renderShadows false -> true, original cached per camera; GT bakes its lighting so its cameras ship with the flag off, and URP zeroes maxShadowDistance for such a camera — no main-light atlas, no volumetric shafts, no geometric shadow, however healthy the URP asset is). COST: with the flag on, URP renders the main-light shadow atlas for this camera — shadow-caster draws every frame. That IS the sunlight effect's cost model (the march samples that atlas); effect-off restores the flag, i.e. the zero-cost state.");
					}
					try
					{
						camLift.Data.renderShadows = true;
					}
					catch
					{
					}
					this._camLifts[i] = camLift;
				}
			}
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00011C00 File Offset: 0x0000FE00
		private void RestoreCamLift(ref PlayerShadow.CamLift e)
		{
			if (!e.Lifted)
			{
				return;
			}
			if (e.Data != null)
			{
				try
				{
					if (e.HdrLifted)
					{
						try
						{
							Camera component = e.Data.GetComponent<Camera>();
							if (component != null && component.allowHDR)
							{
								component.allowHDR = e.OrigAllowHdr;
								this._log.LogInfo("PlayerShadow: camera '" + e.Data.name + "' HDR RESTORED " + string.Format("to {0} (lift released).", e.OrigAllowHdr));
							}
						}
						catch
						{
						}
						e.HdrLifted = false;
					}
					if (e.Data.renderShadows)
					{
						e.Data.renderShadows = e.Orig;
						this._log.LogInfo(string.Concat(new string[]
						{
							"PlayerShadow: camera '",
							e.Data.name,
							"' URP Render Shadows ",
							string.Format("RESTORED to {0} (lift released — the camera is back ", e.Orig),
							"in its zero-cost no-atlas state)."
						}));
					}
				}
				catch
				{
				}
			}
			e.Lifted = false;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00011D38 File Offset: 0x0000FF38
		private void RestoreCameraLifts()
		{
			for (int i = 0; i < this._camLifts.Count; i++)
			{
				PlayerShadow.CamLift camLift = this._camLifts[i];
				this.RestoreCamLift(ref camLift);
				this._camLifts[i] = camLift;
			}
			this._camLifts.Clear();
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00011D88 File Offset: 0x0000FF88
		private void RestoreSun()
		{
			if (this._sunCaptured && this._sun != null)
			{
				try
				{
					this._sun.shadows = this._origSunShadows;
					if (this._shadowLifted && Mathf.Abs(this._sun.shadowStrength - 0.7f) <= 0.001f)
					{
						this._sun.shadowStrength = this._preLiftShadow;
					}
				}
				catch
				{
				}
			}
			this._sun = null;
			this._sunCaptured = false;
			this._shadowLifted = false;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00011E1C File Offset: 0x0001001C
		private void RescanRigs()
		{
			if (this._tVRRig == null)
			{
				return;
			}
			this._current.Clear();
			UnityEngine.Object[] array;
			try
			{
				array = UnityEngine.Object.FindObjectsByType(this._tVRRig, 0);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("PlayerShadow rig scan skipped: " + ex.Message);
				return;
			}
			int num = 0;
			int num2 = 0;
			while (num2 < array.Length && num < 256)
			{
				Component component = array[num2] as Component;
				if (!(component == null))
				{
					this._rendScratch.Clear();
					try
					{
						component.GetComponentsInChildren<Renderer>(true, this._rendScratch);
					}
					catch
					{
						goto IL_00D3;
					}
					for (int i = 0; i < this._rendScratch.Count; i++)
					{
						Renderer renderer = this._rendScratch[i];
						if (!(renderer == null))
						{
							if (num >= 256)
							{
								break;
							}
							if (this._current.Add(renderer))
							{
								num++;
							}
						}
					}
				}
				IL_00D3:
				num2++;
			}
			this._rendScratch.Clear();
			for (int j = this._modified.Count - 1; j >= 0; j--)
			{
				PlayerShadow.RendererState rendererState = this._modified[j];
				if (rendererState.R == null)
				{
					this._modified.RemoveAt(j);
				}
				else if (!this._current.Contains(rendererState.R))
				{
					try
					{
						rendererState.R.shadowCastingMode = rendererState.Orig;
					}
					catch
					{
					}
					try
					{
						if (this._rigGhostMat != null)
						{
							Material[] sharedMaterials = rendererState.R.sharedMaterials;
							int num3 = 0;
							for (int k = 0; k < sharedMaterials.Length; k++)
							{
								if (sharedMaterials[k] != this._rigGhostMat)
								{
									num3++;
								}
							}
							if (num3 != sharedMaterials.Length)
							{
								Material[] array2 = new Material[num3];
								int num4 = 0;
								for (int l = 0; l < sharedMaterials.Length; l++)
								{
									if (sharedMaterials[l] != this._rigGhostMat)
									{
										array2[num4++] = sharedMaterials[l];
									}
								}
								rendererState.R.sharedMaterials = array2;
							}
						}
					}
					catch
					{
					}
					this._modified.RemoveAt(j);
				}
			}
			foreach (Renderer renderer2 in this._current)
			{
				if (!this.AlreadyModified(renderer2))
				{
					ShadowCastingMode shadowCastingMode;
					try
					{
						shadowCastingMode = renderer2.shadowCastingMode;
						renderer2.shadowCastingMode = (ShadowCastingMode)1;
					}
					catch
					{
						continue;
					}
					this._modified.Add(new PlayerShadow.RendererState
					{
						R = renderer2,
						Orig = shadowCastingMode
					});
				}
			}
			if (this._modified.Count != this._lastLoggedRenderers)
			{
				this._lastLoggedRenderers = this._modified.Count;
				this._log.LogInfo(string.Format("PlayerShadow: {0} VRRig(s) found, {1} renderer(s) ", array.Length, this._modified.Count) + "now casting a real shadow (sun='" + ((this._sun != null) ? this._sun.name : "none") + "').");
			}
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00012198 File Offset: 0x00010398
		private bool AlreadyModified(Renderer r)
		{
			for (int i = 0; i < this._modified.Count; i++)
			{
				if (this._modified[i].R == r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x000121D4 File Offset: 0x000103D4
		private void RestoreRigs()
		{
			if (this._modified.Count == 0 && this._current.Count == 0)
			{
				return;
			}
			for (int i = 0; i < this._modified.Count; i++)
			{
				PlayerShadow.RendererState rendererState = this._modified[i];
				if (rendererState.R != null)
				{
					try
					{
						rendererState.R.shadowCastingMode = rendererState.Orig;
					}
					catch
					{
					}
				}
			}
			this._modified.Clear();
			this._current.Clear();
			this._lastLoggedRenderers = -1;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00012270 File Offset: 0x00010470
		private void RestoreAll()
		{
			this.RestoreRigs();
			this.RestoreSun();
			this.RestoreUrpShadowSupport();
			this.RestoreCameraLifts();
			this._occluders.RestoreAll();
			this._geometricApplied = false;
			this._lastLoggedRenderers = -1;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x000122A4 File Offset: 0x000104A4
		private void EnsureReflection()
		{
			if (this._reflectResolved)
			{
				return;
			}
			this._reflectResolved = true;
			try
			{
				this._tVRRig = PlayerShadow.FindType("VRRig");
			}
			catch (Exception ex)
			{
				this._log.LogWarning("PlayerShadow reflection failed: " + ex.Message);
			}
			if (!this._loggedReflection)
			{
				this._loggedReflection = true;
				this._log.LogInfo(string.Format("PlayerShadow reflection: VRRig={0} ", this._tVRRig != null) + "(same player-rig detection MetalSurfaces uses for the EXCLUDE mask).");
			}
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00012340 File Offset: 0x00010540
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

		// Token: 0x0600013F RID: 319 RVA: 0x00012418 File Offset: 0x00010618
		public void LogShadowChain(bool master, bool sunlightOn, float sunlightIntensity, bool playerShadowOn, string playerShadowMode)
		{
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup < this._nextChainLogAt)
			{
				return;
			}
			this._nextChainLogAt = realtimeSinceStartup + 30f;
			string text = "none";
			try
			{
				Light activeSun = WorldLight.ActiveSun;
				if (activeSun != null)
				{
					text = string.Format("'{0}' shadows={1} strength={2:0.##} ", activeSun.name, activeSun.shadows, activeSun.shadowStrength) + string.Format("intensity={0:0.##} enabled={1}", activeSun.intensity, activeSun.enabled);
				}
			}
			catch
			{
			}
			string text2 = "n/a";
			try
			{
				Vector3 normalized = SkySystem.UniSunDir.normalized;
				Vector3 normalized2 = WorldLight.ResolvedSunDir.normalized;
				Vector3 normalized3 = WorldLight.RayDir.normalized;
				Light activeSun2 = WorldLight.ActiveSun;
				Vector3 vector = ((activeSun2 != null) ? (-activeSun2.transform.forward).normalized : Vector3.zero);
				float num = Vector3.Angle(normalized, normalized2);
				float num2 = Vector3.Angle(normalized2, normalized3);
				float num3 = ((vector == Vector3.zero) ? (-1f) : Vector3.Angle(normalized2, vector));
				text2 = string.Format("disc↔resolved={0:0.#}° resolved↔ray={1:0.#}°(rayAngle slider, ", num, num2) + "expected to differ) resolved↔LIGHT=" + ((num3 < 0f) ? "no light" : (num3.ToString("0.#") + "°")) + ((num3 > 2f) ? "  <<< SHADOWS POINT WHERE THE SUN IS NOT: the light rotation is not landing" : "");
			}
			catch
			{
			}
			string text3 = "n/a";
			try
			{
				Transform transform = null;
				for (int i = 0; i < this._camLifts.Count; i++)
				{
					UniversalAdditionalCameraData data = this._camLifts[i].Data;
					if (data != null)
					{
						transform = data.transform;
						break;
					}
				}
				Vector3 normalized4 = WorldLight.ResolvedSunDir.normalized;
				RaycastHit raycastHit = default;
				if (transform == null)
				{
					text3 = "no rendering camera noted yet";
				}
				else if (normalized4 == Vector3.zero)
				{
					text3 = "no resolved sun direction";
				}
				else if (!Physics.Raycast(transform.position, normalized4, out raycastHit, 60f, -1, (QueryTriggerInteraction)1))
				{
					text3 = "sun path CLEAR to 60m (physics) — being lit here is CORRECT";
				}
				else
				{
					Renderer renderer;
					if ((renderer = raycastHit.collider.GetComponent<Renderer>()) == null && (renderer = raycastHit.collider.GetComponentInParent<Renderer>()) == null)
					{
						renderer = raycastHit.collider.GetComponentInChildren<Renderer>() ?? ((raycastHit.collider.transform.parent != null) ? raycastHit.collider.transform.parent.GetComponentInChildren<Renderer>() : null);
					}
					Renderer renderer2 = renderer;
					bool flag = PlayerShadow.IsSolidOccluder(raycastHit.collider);
					bool flag2 = renderer2 != null && this._occluders.IsCarryingCaster(renderer2);
					text3 = string.Concat(new string[]
					{
						string.Format("sun BLOCKED at {0:0.##}m by '{1}' ", raycastHit.distance, raycastHit.collider.name),
						string.Format("[layer {0}:", raycastHit.collider.gameObject.layer),
						LayerMask.LayerToName(raycastHit.collider.gameObject.layer),
						"] ",
						(!flag) ? "<<< NOT SOLID (invisible barrier or glass): sunlight now passes straight through this and it no longer shades the player" : ((renderer2 == null) ? "<<< blocker has NO Renderer (collision-only proxy): the caster scan walks renderers, so it never saw this and the atlas cannot know about it" : (string.Format("renderer='{0}' carryingCaster={1}", renderer2.name, flag2) + (flag2 ? " — atlas HAS it, so a still-lit player is a shader-side bug" : " <<< NOT A CASTER: the atlas honestly reports lit")))
					});
				}
			}
			catch (Exception ex)
			{
				text3 = "probe threw: " + ex.Message;
			}
			string text4 = "unresolved";
			try
			{
				UniversalRenderPipelineAsset universalRenderPipelineAsset = ActiveUrpAsset.Current;
				if (universalRenderPipelineAsset != null)
				{
					text4 = string.Format("'{0}' sup={1} ", universalRenderPipelineAsset.name, universalRenderPipelineAsset.supportsMainLightShadows) + string.Format("dist={0:0}m cascades={1} ", universalRenderPipelineAsset.shadowDistance, universalRenderPipelineAsset.shadowCascadeCount) + string.Format("mode={0} hdr={1}", universalRenderPipelineAsset.mainLightRenderingMode, universalRenderPipelineAsset.supportsHDR) + (universalRenderPipelineAsset.supportsHDR ? " (lifted by Luma - see ApplyHdrLift; GT ships this OFF)" : "  <<< HDR OFF AND THE LIFT DID NOT LAND: the camera target is still 8-bit, so every lit-side multiply above 1.0 clips to white while darkening survives intact. Check the URP asset write in MaintainUrpShadowSupport.");
				}
			}
			catch
			{
			}
			string text5;
			if (!master)
			{
				text5 = "BLOCKED: master is off/paused";
			}
			else if (!sunlightOn)
			{
				text5 = "BLOCKED: the `sunlight` effect is off — world casters are gated on it";
			}
			else if (sunlightIntensity <= 0.0001f)
			{
				text5 = "BLOCKED: sunlight intensity is 0 (documented zero-cost no-op)";
			}
			else
			{
				text5 = "gates OK — if shadows are still absent the answer is in `asset`/`sun`/casters above";
			}
			this._log.LogInfo(string.Concat(new string[]
			{
				"WORLD SHADOW CHAIN | ",
				text5,
				" || ",
				string.Format("master={0} sunlight={1}(intensity={2:0.##}) ", master, sunlightOn, sunlightIntensity),
				string.Format("playerShadow={0}(mode={1}) || sun={2} || ", playerShadowOn, playerShadowMode, text),
				"angles: ",
				text2,
				" || shadeProbe: ",
				text3,
				" || asset=",
				text4,
				" || casters: see the SunlightOccluders line (it only prints once the gates above pass; no line = the scan never ran)."
			}));
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00012988 File Offset: 0x00010B88
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			try
			{
				this.RestoreAll();
			}
			catch
			{
			}
		}

		// Token: 0x04000296 RID: 662
		private const float RescanSeconds = 3f;

		// Token: 0x04000297 RID: 663
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000298 RID: 664
		private const int MaxRigRenderers = 256;

		// Token: 0x04000299 RID: 665
		private const float ZeroShadowEps = 0.05f;

		// Token: 0x0400029A RID: 666
		private const float GroundedShadowStrength = 0.7f;

		// Token: 0x0400029B RID: 667
		internal const float MinShadowDistance = 22f;

		// Token: 0x0400029C RID: 668
		private const float GeometricShadowDistance = 120f;

		// Token: 0x0400029D RID: 669
		private readonly ManualLogSource _log;

		// Token: 0x0400029E RID: 670
		private readonly SunlightOccluders _occluders;

		// Token: 0x0400029F RID: 671
		private bool _reflectResolved;

		// Token: 0x040002A0 RID: 672
		private Type _tVRRig;

		// Token: 0x040002A1 RID: 673
		private bool _loggedReflection;

		// Token: 0x040002A2 RID: 674
		private bool _want;

		// Token: 0x040002A3 RID: 675
		private bool _vrAllowed = true;

		// Token: 0x040002A4 RID: 676
		private bool _desktopAllowed = true;

		// Token: 0x040002A5 RID: 677
		private float _intensity = 0.7f;

		// Token: 0x040002A6 RID: 678
		private float _softness = 0.4f;

		// Token: 0x040002A7 RID: 679
		private int _mode = 2;

		// Token: 0x040002A8 RID: 680
		private bool _sunlightWantAtlas;

		// Token: 0x040002A9 RID: 681
		private bool _sunlightVrAllowed = true;

		// Token: 0x040002AA RID: 682
		private bool _sunlightDesktopAllowed = true;

		// Token: 0x040002AB RID: 683
		private bool _sunlightVrBalanced;

		// Token: 0x040002AC RID: 684
		private float _sunlightUiReach = 60f;

		// Token: 0x040002AD RID: 685
		private bool _loggedAtlasOnly;

		// Token: 0x040002AE RID: 686
		private readonly List<PlayerShadow.CamLift> _camLifts = new List<PlayerShadow.CamLift>(4);

		// Token: 0x040002AF RID: 687
		private const int CamLiftStaleFrames = 240;

		// Token: 0x040002B0 RID: 688
		private const int MaxCamLifts = 16;

		// Token: 0x040002B1 RID: 689
		private bool _geometricApplied;

		// Token: 0x040002B2 RID: 690
		private float _nextScanAt;

		// Token: 0x040002B3 RID: 691
		private bool _sceneJustLoaded;

		// Token: 0x040002B4 RID: 692
		private int _lastLoggedRenderers = -1;

		// Token: 0x040002B5 RID: 693
		private readonly List<PlayerShadow.RendererState> _modified = new List<PlayerShadow.RendererState>(64);

		// Token: 0x040002B6 RID: 694
		private readonly HashSet<Renderer> _current = new HashSet<Renderer>();

		// Token: 0x040002B7 RID: 695
		private readonly List<Renderer> _rendScratch = new List<Renderer>(64);

		// Token: 0x040002B8 RID: 696
		private Light _sun;

		// Token: 0x040002B9 RID: 697
		private bool _sunCaptured;

		// Token: 0x040002BA RID: 698
		private int _capturedSunId;

		// Token: 0x040002BB RID: 699
		private LightShadows _origSunShadows;

		// Token: 0x040002BC RID: 700
		private bool _shadowLifted;

		// Token: 0x040002BD RID: 701
		private float _preLiftShadow;

		// Token: 0x040002BE RID: 702
		private UniversalRenderPipelineAsset _urp;

		// Token: 0x040002BF RID: 703
		private bool _urpCaptured;

		// Token: 0x040002C0 RID: 704
		private bool _origSupportsMainLightShadows;

		// Token: 0x040002C1 RID: 705
		private float _origShadowDistance;

		// Token: 0x040002C2 RID: 706
		private int _origShadowCascadeCount;

		// Token: 0x040002C3 RID: 707
		private float _origCascade2Split;

		// Token: 0x040002C4 RID: 708
		private Vector2 _origCascade3Split;

		// Token: 0x040002C5 RID: 709
		private Vector3 _origCascade4Split;

		// Token: 0x040002C6 RID: 710
		private bool _cascadesApplied;

		// Token: 0x040002C7 RID: 711
		private float _appliedShadowDistance = -1f;

		// Token: 0x040002C8 RID: 712
		private int _origMainLightShadowmapResolution;

		// Token: 0x040002C9 RID: 713
		private int _appliedShadowmapRes;

		// Token: 0x040002CA RID: 714
		private static readonly Vector3 CascadeBaseSplit = new Vector3(0.08f, 0.25f, 0.55f);

		// Token: 0x040002CB RID: 715
		private const float CascadeNearM = 12f;

		// Token: 0x040002CC RID: 716
		private const float CascadeMidM = 37.5f;

		// Token: 0x040002CD RID: 717
		private const float CascadeFarM = 82.5f;

		// Token: 0x040002CE RID: 718
		private static FieldInfo _fiMainLightShadows;

		// Token: 0x040002CF RID: 719
		private static bool _fiResolved;

		// Token: 0x040002D0 RID: 720
		private static FieldInfo _fiMainLightRenderMode;

		// Token: 0x040002D1 RID: 721
		private static bool _fiRenderModeResolved;

		// Token: 0x040002D2 RID: 722
		private bool _renderModeCaptured;

		// Token: 0x040002D3 RID: 723
		private LightRenderingMode _origMainLightRenderMode;

		// Token: 0x040002D4 RID: 724
		private int _loggedAtlasHealthy = -1;

		// Token: 0x040002D5 RID: 725
		private Shader _ghostShader;

		// Token: 0x040002D6 RID: 726
		private Material _rigGhostMat;

		// Token: 0x040002D7 RID: 727
		private bool _loggedBeginCamLift;

		// Token: 0x040002D8 RID: 728
		private float _sunVis = 1f;

		// Token: 0x040002D9 RID: 729
		private bool _sunVisValid;

		// Token: 0x040002DA RID: 730
		private const int MaxRigs = 16;

		// Token: 0x040002DB RID: 731
		private static readonly Vector4[] _rigSun = new Vector4[16];

		// Token: 0x040002DC RID: 732
		private readonly List<Transform> _rigXforms = new List<Transform>(16);

		// Token: 0x040002DD RID: 733
		private readonly Dictionary<int, float> _rigVis = new Dictionary<int, float>(16);

		// Token: 0x040002DE RID: 734
		private Type _tRig;

		// Token: 0x040002DF RID: 735
		private bool _rigTypeSearched;

		// Token: 0x040002E0 RID: 736
		private float _nextRigScanAt;

		// Token: 0x040002E1 RID: 737
		private int _rigCount;

		// Token: 0x040002E2 RID: 738
		private bool _loggedRigs;

		// Token: 0x040002E3 RID: 739
		private static readonly RaycastHit[] _hitBuf = new RaycastHit[8];

		// Token: 0x040002E4 RID: 740
		private static readonly Dictionary<int, bool> _solidCache = new Dictionary<int, bool>(256);

		// Token: 0x040002E5 RID: 741
		private static readonly string[] BarrierWords = new string[]
		{
			"wind", "barrier", "invisible", "bounds", "boundary", "blocker", "killz", "kill_", "playspace", "play space",
			"nowalk", "no_walk", "limit", "clamp", "zonetrigger"
		};

		// Token: 0x040002E6 RID: 742
		private bool _loggedHdrLift;

		// Token: 0x040002E7 RID: 743
		private bool _loggedHdrAsset;

		// Token: 0x040002E8 RID: 744
		private bool _origSupportsHdr;

		// Token: 0x040002E9 RID: 745
		private FieldInfo _fiSupportsHdr;

		// Token: 0x040002EA RID: 746
		private bool _masterOn;

		// Token: 0x040002EB RID: 747
		private const float ChainLogSeconds = 30f;

		// Token: 0x040002EC RID: 748
		private float _nextChainLogAt;

		// Token: 0x02000022 RID: 34
		private struct CamLift
		{
			// Token: 0x040002ED RID: 749
			public UniversalAdditionalCameraData Data;

			// Token: 0x040002EE RID: 750
			public bool IsVr;

			// Token: 0x040002EF RID: 751
			public bool Orig;

			// Token: 0x040002F0 RID: 752
			public bool Lifted;

			// Token: 0x040002F1 RID: 753
			public bool OrigAllowHdr;

			// Token: 0x040002F2 RID: 754
			public bool HdrLifted;

			// Token: 0x040002F3 RID: 755
			public int LastSeenFrame;
		}

		// Token: 0x02000023 RID: 35
		private struct RendererState
		{
			// Token: 0x040002F4 RID: 756
			public Renderer R;

			// Token: 0x040002F5 RID: 757
			public ShadowCastingMode Orig;
		}
	}
}
