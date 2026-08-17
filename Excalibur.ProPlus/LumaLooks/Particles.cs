using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using Random = UnityEngine.Random;

namespace LumaLooks
{
	// Token: 0x02000019 RID: 25
	internal sealed class Particles
	{
		// Token: 0x060000E2 RID: 226 RVA: 0x0000C181 File Offset: 0x0000A381
		private static float DensityScale(float ui)
		{
			if (ui > 1f)
			{
				return 1f + (ui - 1f) * 0.6f;
			}
			return ui;
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x060000E3 RID: 227 RVA: 0x0000C1A0 File Offset: 0x0000A3A0
		// (set) Token: 0x060000E4 RID: 228 RVA: 0x0000C1A7 File Offset: 0x0000A3A7
		public static bool AnyEffectOn { get; private set; }

		// Token: 0x060000E5 RID: 229 RVA: 0x0000C1B0 File Offset: 0x0000A3B0
		private static Camera FindExternalCam(Camera head)
		{
			foreach (Camera camera in Camera.allCameras)
			{
				if (!(camera == null) && camera.enabled && camera != head)
				{
					RenderTexture targetTexture = camera.targetTexture;
					if (!(targetTexture == null) && targetTexture.width >= 400)
					{
						return camera;
					}
				}
			}
			return null;
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x0000C20C File Offset: 0x0000A40C
		private Vector3 AnchorPos(Camera head)
		{
			Camera camera = Particles.FindExternalCam(head);
			if (camera == null)
			{
				return head.transform.position;
			}
			Vector3 position = head.transform.position;
			Vector3 position2 = camera.transform.position;
			if ((position2 - position).sqrMagnitude < 64f)
			{
				return position;
			}
			this._anchorFlip = !this._anchorFlip;
			if (!this._anchorFlip)
			{
				return position;
			}
			return position2;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x0000C280 File Offset: 0x0000A480
		public Particles(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x0000C30C File Offset: 0x0000A50C
		public void AttachDynamicLights(DynamicLights dl)
		{
			this._dynamicLights = dl;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x0000C318 File Offset: 0x0000A518
		public void ConfigureDust(bool on, bool vrAllowed, bool desktopAllowed, float density, float size, float driftSpeed, float brightness, float shape)
		{
			this._dust.Want = on;
			this._dust.VrAllowed = vrAllowed;
			this._dust.DesktopAllowed = desktopAllowed;
			this._dust.Density = Mathf.Clamp(density, 0f, 3f);
			this._dust.SizeParam = Mathf.Clamp(size, 0f, 3f);
			this._dust.SpeedParam = Mathf.Clamp01(driftSpeed);
			this._dust.Brightness = Mathf.Clamp01(brightness);
			this._dust.Glow = 0f;
			this._dust.ShapeParam = Mathf.Clamp01(shape);
			this._dust.MatPushPending = true;
			this.RefreshAnyEffectOn();
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000C3DC File Offset: 0x0000A5DC
		public void ConfigureFireflies(bool on, bool vrAllowed, bool desktopAllowed, float density, float brightness, float wanderSpeed, float shape)
		{
			this._fireflies.Want = on;
			this._fireflies.VrAllowed = vrAllowed;
			this._fireflies.DesktopAllowed = desktopAllowed;
			this._fireflies.Density = Mathf.Clamp01(density);
			this._fireflies.Brightness = Mathf.Clamp01(brightness);
			this._fireflies.SpeedParam = Mathf.Clamp01(wanderSpeed);
			this._fireflies.Glow = 0.6f;
			this._fireflies.ShapeParam = Mathf.Clamp01(shape);
			this._fireflies.MatPushPending = true;
			this.RefreshAnyEffectOn();
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000C478 File Offset: 0x0000A678
		public void ConfigureEmbers(bool on, bool vrAllowed, bool desktopAllowed, float density, float riseSpeed, float brightness, float glow, float shape)
		{
			this._ember.Want = on;
			this._ember.VrAllowed = vrAllowed;
			this._ember.DesktopAllowed = desktopAllowed;
			this._ember.Density = Mathf.Clamp01(density);
			this._ember.SpeedParam = Mathf.Clamp01(riseSpeed);
			this._ember.Brightness = Mathf.Clamp01(brightness);
			this._ember.Glow = Mathf.Clamp01(glow);
			this._ember.ShapeParam = Mathf.Clamp01(shape);
			this._ember.MatPushPending = true;
			this.RefreshAnyEffectOn();
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000C518 File Offset: 0x0000A718
		public void ConfigureFallingLeaves(bool on, bool vrAllowed, bool desktopAllowed, float density, float fallSpeed, float size, float leafType, float shape)
		{
			this._leaves.Want = on;
			this._leaves.VrAllowed = vrAllowed;
			this._leaves.DesktopAllowed = desktopAllowed;
			this._leaves.Density = Mathf.Clamp(density, 0f, 3f);
			this._leaves.SizeParam = Mathf.Clamp(size, 0f, 3f);
			this._leaves.SpeedParam = Mathf.Clamp01(fallSpeed);
			this._leaves.Brightness = 1f;
			this._leaves.Glow = 0f;
			this._leaves.LeafTypeParam = Mathf.Clamp(leafType, 0f, 3f);
			this._leaves.ShapeParam = Mathf.Clamp01(shape);
			this._leaves.MatPushPending = true;
			this.RefreshAnyEffectOn();
		}

		// Token: 0x060000ED RID: 237 RVA: 0x0000C5F3 File Offset: 0x0000A7F3
		public void ConfigureVrBalanced(bool vrBalanced)
		{
			this._vrBalanced = vrBalanced;
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000C5FC File Offset: 0x0000A7FC
		private void RefreshAnyEffectOn()
		{
			Particles.AnyEffectOn = this._dust.Want || this._fireflies.Want || this._ember.Want || this._leaves.Want;
		}

		// Token: 0x060000EF RID: 239 RVA: 0x0000C638 File Offset: 0x0000A838
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == (LoadSceneMode)1)
			{
				return;
			}
			Particles.ForgetSceneObjects(this._dust);
			Particles.ForgetSceneObjects(this._fireflies);
			Particles.ForgetSceneObjects(this._ember);
			Particles.ForgetSceneObjects(this._leaves);
			this._settleUntil = -1f;
			this._leafGateLoggedZone = null;
			this._gtLeafTex = null;
			this._gtLeafFound = false;
			this._gtLeafResolved = false;
			this._gtLeafNextScan = -1f;
			this._leaves.MatPushPending = true;
		}

		// Token: 0x060000F0 RID: 240 RVA: 0x0000C6B4 File Offset: 0x0000A8B4
		private static void ForgetSceneObjects(Particles.Rig r)
		{
			r.Go = null;
			r.Ps = null;
			r.Psr = null;
			r.AppliedRate = -1f;
			r.AppliedSizeMin = -1f;
			r.AppliedSpeed = -1f;
			r.AppliedRise = -1f;
			r.EmitAcc = 0f;
		}

		// Token: 0x060000F1 RID: 241 RVA: 0x0000C710 File Offset: 0x0000A910
		public void Tick()
		{
			try
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (this._settleUntil < 0f)
				{
					this._settleUntil = realtimeSinceStartup + 2f;
				}
				bool flag = false;
				try
				{
					flag = XRSettings.isDeviceActive;
				}
				catch
				{
				}
				bool flag2 = flag && this._vrBalanced;
				bool flag3 = realtimeSinceStartup >= this._settleUntil;
				this.TickDust(flag, flag2, flag3);
				this.TickFireflies(flag, flag2, flag3);
				this.TickEmbers(flag, flag2, flag3);
				this.TickLeaves(flag, flag2, flag3);
				this.DiagHeartbeat(realtimeSinceStartup, flag);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("Particles tick skipped: " + ex.Message);
			}
		}

		// Token: 0x060000F2 RID: 242 RVA: 0x0000C7D0 File Offset: 0x0000A9D0
		private static bool TargetAllows(Particles.Rig r, bool vrActive)
		{
			if (!r.Want)
			{
				return false;
			}
			if (!vrActive)
			{
				return r.DesktopAllowed;
			}
			return r.VrAllowed;
		}

		// Token: 0x060000F3 RID: 243 RVA: 0x0000C7EC File Offset: 0x0000A9EC
		private void TickDust(bool vrActive, bool halved, bool settled)
		{
			Particles.Rig dust = this._dust;
			bool flag = Particles.TargetAllows(dust, vrActive);
			bool flag2 = flag && dust.Density > 0.001f && dust.Brightness > 0.001f && settled;
			if (!this.ResolveActive(dust, flag, flag2))
			{
				return;
			}
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			if (!this.EnsureSystem(dust, halved))
			{
				return;
			}
			dust.Go.transform.position = this.AnchorPos(main);
			float num = Mathf.LerpUnclamped(0.008f, 0.045f, dust.SizeParam);
			Particles.ApplySize(dust, num, num * 1.7f);
			float num2 = Particles.DensityScale(dust.Density) * 95f * (halved ? 0.5f : 1f);
			Particles.ApplyRate(dust, num2);
			if (!this._loggedHeavyDust && dust.Density > 1f)
			{
				this._loggedHeavyDust = true;
				this._log.LogInfo(string.Format("Particles: dustMotes density {0:0.##} -> {1:0}/s, ", dust.Density, num2) + string.Format("~{0:0} live (cap {1})", num2 * 9f, Particles.MaxParticlesFor(Particles.Kind.Dust, halved)) + (halved ? ", VR-Balanced halving ACTIVE." : ". VR Performance is NOT halving this (Quality mode, or the effect is off) — at the top of the slider that is ~1.9k simulated particles of transparent overdraw per eye."));
			}
			Particles.ApplyDrift(dust, Mathf.Lerp(0.02f, 0.3f, dust.SpeedParam), Mathf.Lerp(0.03f, 0.35f, dust.SpeedParam), 0.12f, 0.06f);
		}

		// Token: 0x060000F4 RID: 244 RVA: 0x0000C970 File Offset: 0x0000AB70
		private void TickFireflies(bool vrActive, bool halved, bool settled)
		{
			Particles.Rig fireflies = this._fireflies;
			bool flag = Particles.TargetAllows(fireflies, vrActive);
			float num = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.06f, 0.3f, WorldLight.DayFactor));
			if (WorldLight.SourceIsMoon)
			{
				num = 1f;
			}
			bool flag2 = MapSense.HasSky && !MapSense.IsBasement;
			bool flag3 = flag && flag2 && num > 0.02f && fireflies.Density > 0.001f && fireflies.Brightness > 0.001f && settled;
			if (!this.ResolveActive(fireflies, flag, flag3))
			{
				return;
			}
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			if (!this.EnsureSystem(fireflies, halved))
			{
				return;
			}
			Vector3 vector = this.AnchorPos(main);
			vector.y += -0.35f;
			fireflies.Go.transform.position = vector;
			Particles.ApplySize(fireflies, 0.022f, 0.032f);
			Particles.ApplyDrift(fireflies, Mathf.Lerp(0.05f, 0.35f, fireflies.SpeedParam), Mathf.Lerp(0.06f, 0.55f, fireflies.SpeedParam), 0.28f, 0.12f);
			Particles.ApplyRate(fireflies, fireflies.Density * num * 17f * (halved ? 0.5f : 1f));
		}

		// Token: 0x060000F5 RID: 245 RVA: 0x0000CAC8 File Offset: 0x0000ACC8
		private void TickEmbers(bool vrActive, bool halved, bool settled)
		{
			Particles.Rig ember = this._ember;
			bool flag = Particles.TargetAllows(ember, vrActive);
			bool flag2 = flag && ember.Density > 0.001f && ember.Brightness > 0.001f && settled;
			if (!this.ResolveActive(ember, flag, flag2))
			{
				return;
			}
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			int num = ((this._dynamicLights != null) ? this._dynamicLights.FireSourceCount : 0);
			if (num <= 0)
			{
				if (!this._loggedNoFireSources)
				{
					this._loggedNoFireSources = true;
					this._log.LogInfo("Particles: `embers` is on but no fire-class emitters are tracked (spark placement reuses DynamicLights' scan by design — enable the Dynamic Lights effect, or this map simply has no fires).");
				}
				this.DrainOrTeardown(ember);
				return;
			}
			this._loggedNoFireSources = false;
			if (!this.EnsureSystem(ember, halved))
			{
				return;
			}
			Particles.ApplySize(ember, 0.012f, 0.026f);
			Particles.ApplyDrift(ember, 0.1f, 0.35f, 0.55f, 0.3f);
			float num2 = ember.Density * 16f * (halved ? 0.5f : 1f);
			ember.EmitAcc += num2 * Time.unscaledDeltaTime * (float)num;
			int num3 = (int)ember.EmitAcc;
			if (num3 <= 0)
			{
				return;
			}
			ember.EmitAcc -= (float)num3;
			if (num3 > 120)
			{
				num3 = 120;
			}
			float num4 = Mathf.Lerp(0.35f, 1.7f, ember.SpeedParam);
			if (Mathf.Abs(num4 - ember.AppliedRise) > 0.0001f)
			{
				if (ember.AppliedRise > 0f)
				{
					Particles.RetrofitEmberRise(ember, num4 / ember.AppliedRise);
				}
				ember.AppliedRise = num4;
			}
			Vector3 position = main.transform.position;
			float num5 = 1024f;
			for (int i = 0; i < num3; i++)
			{
				int num6 = Random.Range(0, num);
				Vector3 vector;
				if (this._dynamicLights.TryGetFireSourcePosition(num6, out vector) && (vector - position).sqrMagnitude <= num5)
				{
					ember.Emit.position = new Vector3(vector.x + Random.Range(-0.18f, 0.18f), vector.y + Random.Range(-0.05f, 0.3f), vector.z + Random.Range(-0.18f, 0.18f));
					ember.Emit.velocity = new Vector3(Random.Range(-0.16f, 0.16f), num4 * Random.Range(0.75f, 1.35f), Random.Range(-0.16f, 0.16f));
					ember.Ps.Emit(ember.Emit, 1);
				}
			}
		}

		// Token: 0x060000F6 RID: 246 RVA: 0x0000CD5C File Offset: 0x0000AF5C
		private void TickLeaves(bool vrActive, bool halved, bool settled)
		{
			Particles.Rig leaves = this._leaves;
			bool flag = Particles.TargetAllows(leaves, vrActive);
			bool isForest = MapSense.IsForest;
			this.LogLeafGate(leaves.Want, isForest);
			bool flag2 = flag && isForest && leaves.Density > 0.001f && settled;
			if (!this.ResolveActive(leaves, flag, flag2))
			{
				return;
			}
			Camera main = Camera.main;
			if (main == null)
			{
				return;
			}
			if (!this.EnsureSystem(leaves, halved))
			{
				return;
			}
			this.ResolveGtLeafArt(Time.realtimeSinceStartup);
			if (leaves.MatPushPending)
			{
				this.PushMaterial(leaves);
			}
			float num = Mathf.LerpUnclamped(0.02f, 0.115f, leaves.SizeParam);
			Particles.ApplySize(leaves, num, num * 1.45f);
			Particles.ApplyLeafMotion(leaves);
			float num2 = Particles.DensityScale(leaves.Density) * 26f * (halved ? 0.5f : 1f);
			leaves.EmitAcc += num2 * Time.unscaledDeltaTime;
			int num3 = (int)leaves.EmitAcc;
			if (num3 <= 0)
			{
				return;
			}
			leaves.EmitAcc -= (float)num3;
			if (num3 > 48)
			{
				num3 = 48;
			}
			Vector3 position = main.transform.position;
			float num4 = 11f;
			float num5 = 2.5f;
			for (int i = 0; i < num3; i++)
			{
				leaves.Emit.position = new Vector3(position.x + Random.Range(-num4, num4), position.y + 3.5f + Random.Range(-num5, num5), position.z + Random.Range(-num4, num4));
				leaves.Emit.startColor = Particles.NextLeafLanes();
				leaves.Ps.Emit(leaves.Emit, 1);
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000CF14 File Offset: 0x0000B114
		private void ResolveGtLeafArt(float now)
		{
			if (this._gtLeafFound)
			{
				return;
			}
			if (this._gtLeafNextScan > 0f && now < this._gtLeafNextScan)
			{
				return;
			}
			this._gtLeafNextScan = now + 6f;
			Material material = null;
			int num = int.MaxValue;
			foreach (Material material2 in Resources.FindObjectsOfTypeAll<Material>())
			{
				if (!(material2 == null))
				{
					string name = material2.name;
					if (!string.IsNullOrEmpty(name))
					{
						int num2 = 0;
						while (num2 < Particles.GtLeafMaterialNames.Length && num2 < num)
						{
							if (string.Equals(name, Particles.GtLeafMaterialNames[num2], StringComparison.OrdinalIgnoreCase))
							{
								if (material2.HasProperty(ShaderIds.GtBaseMapAtlas) && !(material2.GetTexture(ShaderIds.GtBaseMapAtlas) == null))
								{
									material = material2;
									num = num2;
									break;
								}
								break;
							}
							else
							{
								num2++;
							}
						}
					}
				}
			}
			if (material == null)
			{
				if (!this._gtLeafResolved)
				{
					this._gtLeafResolved = true;
					this._log.LogInfo("Particles: LEAVES — GT leaf material not loaded yet (looked for " + string.Join("/", Particles.GtLeafMaterialNames) + "); using the procedural leaf archetypes meanwhile. Retrying every " + string.Format("{0:0}s while the effect is live.", 6f));
				}
				return;
			}
			this._gtLeafTex = material.GetTexture(ShaderIds.GtBaseMapAtlas);
			this._gtLeafSlice = (material.HasProperty(ShaderIds.GtBaseMapSlice) ? material.GetFloat(ShaderIds.GtBaseMapSlice) : 0f);
			this._gtLeafFound = true;
			this._gtLeafResolved = true;
			this._leaves.MatPushPending = true;
			this._log.LogInfo(string.Concat(new string[]
			{
				"Particles: LEAVES using GT's own art — material '",
				material.name,
				"', atlas ",
				string.Format("'{0}' slice {1:0}. The texture's ALPHA is the ", this._gtLeafTex.name, this._gtLeafSlice),
				"leaf silhouette, so the shape is GT's cut-out rather than ours."
			}));
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000D0E8 File Offset: 0x0000B2E8
		private static Color NextLeafLanes()
		{
			int num = Random.Range(0, 3);
			return new Color((num == 0) ? 0f : ((num == 1) ? 0.64f : 1f), Random.value, 0.5f, 1f);
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000D12C File Offset: 0x0000B32C
		private void DiagHeartbeat(float now, bool vrActive)
		{
			if (now < this._nextDiag)
			{
				return;
			}
			this._nextDiag = now + 30f;
			if (!this._dust.Want && !this._fireflies.Want && !this._ember.Want && !this._leaves.Want)
			{
				return;
			}
			if (this._diagLogged)
			{
				return;
			}
			this._diagLogged = true;
			Camera main = Camera.main;
			this._log.LogInfo(string.Concat(new string[]
			{
				"Particles: DIAG xr=",
				vrActive ? "1" : "0",
				" cam='",
				(main != null) ? main.name : "none",
				"' stereo=",
				(main != null && main.stereoEnabled) ? "1" : "0",
				" dust[",
				Particles.RigDiag(this._dust),
				"] fireflies[",
				Particles.RigDiag(this._fireflies),
				"] embers[",
				Particles.RigDiag(this._ember),
				"] leaves[",
				Particles.RigDiag(this._leaves),
				"]"
			}));
		}

		// Token: 0x060000FA RID: 250 RVA: 0x0000D27C File Offset: 0x0000B47C
		private static string RigDiag(Particles.Rig r)
		{
			if (!r.Want)
			{
				return "off";
			}
			if (!(r.Ps == null))
			{
				return "live=" + r.Ps.particleCount.ToString() + ",rate=" + r.AppliedRate.ToString("0.#");
			}
			return "wanted,noPS";
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000D2E0 File Offset: 0x0000B4E0
		private void LogLeafGate(bool want, bool forest)
		{
			if (!want)
			{
				this._leafGateLoggedZone = null;
				return;
			}
			string zoneName = MapSense.ZoneName;
			if (string.Equals(this._leafGateLoggedZone, zoneName, StringComparison.Ordinal))
			{
				return;
			}
			this._leafGateLoggedZone = zoneName;
			this._log.LogInfo(string.Concat(new string[]
			{
				"Particles: LEAVES[zone=",
				zoneName,
				"] gate=",
				forest ? "ALLOW" : "DENY",
				forest ? "." : " — fallingLeaves is FOREST-ONLY and fails CLOSED, so an unresolved/non-forest zone spawns no system at all. This is deliberate, not a missing map."
			}));
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000D364 File Offset: 0x0000B564
		private bool ResolveActive(Particles.Rig r, bool on, bool active)
		{
			if (r.LastResolvedOn != active)
			{
				r.LastResolvedOn = active;
				if (active && r.LoggedOnline)
				{
					this._log.LogInfo("Particles: " + r.GoName + " resumed.");
				}
			}
			if (!on)
			{
				Particles.Teardown(r);
				return false;
			}
			if (!active)
			{
				this.DrainOrTeardown(r);
				return false;
			}
			return true;
		}

		// Token: 0x060000FD RID: 253 RVA: 0x0000D3C4 File Offset: 0x0000B5C4
		private void DrainOrTeardown(Particles.Rig r)
		{
			r.EmitAcc = 0f;
			if (r.Ps == null)
			{
				Particles.Teardown(r);
				return;
			}
			Particles.ApplyRate(r, 0f);
			if (r.Ps.particleCount == 0)
			{
				Particles.Teardown(r);
			}
		}

		// Token: 0x060000FE RID: 254 RVA: 0x0000D404 File Offset: 0x0000B604
		private bool EnsureMaterial(Particles.Rig r)
		{
			if (r.Mat != null)
			{
				if (r.MatPushPending)
				{
					this.PushMaterial(r);
				}
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/WorldParticle") : null);
			if (shader == null)
			{
				if (!r.ShaderMissingLogged)
				{
					r.ShaderMissingLogged = true;
					this._log.LogWarning("Particles: shader 'LumaLooks/WorldParticle' not in the bundle — " + r.GoName + " disabled.");
				}
				return false;
			}
			r.Mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			r.Mat.SetFloat(ShaderIds.ParticleKind, (float)r.Kind);
			this.PushMaterial(r);
			return true;
		}

		// Token: 0x060000FF RID: 255 RVA: 0x0000D4B8 File Offset: 0x0000B6B8
		private void PushMaterial(Particles.Rig r)
		{
			if (r.Mat == null)
			{
				return;
			}
			r.Mat.SetFloat(ShaderIds.ParticleBrightness, Mathf.Clamp01(r.Brightness));
			r.Mat.SetFloat(ShaderIds.ParticleGlow, Mathf.Clamp01(r.Glow));
			r.Mat.SetFloat(ShaderIds.ParticleShape, r.ShapeParam);
			if (r.Kind == Particles.Kind.Leaf)
			{
				r.Mat.SetFloat(ShaderIds.ParticleLeafType, r.LeafTypeParam);
				if (this._gtLeafFound && this._gtLeafTex != null)
				{
					r.Mat.SetTexture(ShaderIds.LeafTex, this._gtLeafTex);
					r.Mat.SetFloat(ShaderIds.LeafSlice, this._gtLeafSlice);
					r.Mat.SetFloat(ShaderIds.LeafHasTex, 1f);
				}
				else
				{
					r.Mat.SetFloat(ShaderIds.LeafHasTex, 0f);
				}
			}
			r.MatPushPending = false;
		}

		// Token: 0x06000100 RID: 256 RVA: 0x0000D5B8 File Offset: 0x0000B7B8
		private bool EnsureSystem(Particles.Rig r, bool capHalved)
		{
			if (r.Ps != null)
			{
				if (r.MatPushPending)
				{
					this.PushMaterial(r);
				}
				if (capHalved != r.CapHalved)
				{
					r.CapHalved = capHalved;
					ParticleSystem.MainModule mainModule = r.Ps.main;
					mainModule.maxParticles = Particles.MaxParticlesFor(r.Kind, capHalved);
				}
				return true;
			}
			if (!this.EnsureMaterial(r))
			{
				return false;
			}
			r.Go = new GameObject(r.GoName);
			r.Go.transform.position = Vector3.zero;
			r.Go.transform.rotation = Quaternion.identity;
			r.Ps = r.Go.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = r.Ps.main;
			main.loop = true;
			main.playOnAwake = false;
			main.simulationSpace = (ParticleSystemSimulationSpace)1;
			main.startSpeed = 0f;
			main.startColor = Color.white;
			r.CapHalved = capHalved;
			main.maxParticles = Particles.MaxParticlesFor(r.Kind, capHalved);
			ParticleSystem.InheritVelocityModule inheritVelocity = r.Ps.inheritVelocity;
			inheritVelocity.enabled = false;
			ParticleSystem.ExternalForcesModule externalForces = r.Ps.externalForces;
			externalForces.enabled = false;
			ParticleSystem.CollisionModule collision = r.Ps.collision;
			collision.enabled = false;
			ParticleSystem.EmissionModule emission = r.Ps.emission;
			ParticleSystem.ShapeModule shape = r.Ps.shape;
			switch (r.Kind)
			{
			case Particles.Kind.Dust:
				main.startLifetime = 9f;
				main.gravityModifier = 0f;
				emission.enabled = true;
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f);
				emission.rateOverTimeMultiplier = 0f;
				shape.enabled = true;
				shape.shapeType = (ParticleSystemShapeType)5;
				shape.scale = new Vector3(18f, 11f, 18f);
				shape.randomDirectionAmount = 1f;
				goto IL_0308;
			case Particles.Kind.Firefly:
				main.startLifetime = 15f;
				main.gravityModifier = 0f;
				emission.enabled = true;
				emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f);
				emission.rateOverTimeMultiplier = 0f;
				shape.enabled = true;
				shape.shapeType = (ParticleSystemShapeType)5;
				shape.scale = new Vector3(26f, 3f, 26f);
				shape.randomDirectionAmount = 1f;
				goto IL_0308;
			case Particles.Kind.Leaf:
				main.startLifetime = 9f;
				main.gravityModifier = 0f;
				main.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.2831855f);
				emission.enabled = false;
				shape.enabled = false;
				Particles.BuildLeafFlutter(r);
				goto IL_0308;
			}
			main.startLifetime = 2.9f;
			main.gravityModifier = 0.05f;
			emission.enabled = false;
			shape.enabled = false;
			IL_0308:
			Particles.BuildLifetimeGradient(r);
			Particles.ConfigureRenderer(r);
			r.Ps.Play();
			if (!r.LoggedOnline)
			{
				r.LoggedOnline = true;
				this._log.LogInfo(string.Concat(new string[]
				{
					"Particles: ",
					r.GoName,
					" online (LumaLooks/WorldParticle, kind=",
					string.Format("{0}, world space, cap {1}", (int)r.Kind, Particles.MaxParticlesFor(r.Kind, capHalved)),
					capHalved ? ", VR-Balanced: rate+cap halved" : "",
					", shape=",
					(r.ShapeParam > 0.5f) ? "Square" : "Dot",
					(r.Kind == Particles.Kind.Leaf) ? string.Format(", leafType={0:0}", r.LeafTypeParam) : "",
					")."
				}));
			}
			return true;
		}

		// Token: 0x06000101 RID: 257 RVA: 0x0000D9B8 File Offset: 0x0000BBB8
		private static int MaxParticlesFor(Particles.Kind k, bool halved)
		{
			int num = ((k == Particles.Kind.Dust) ? 2048 : ((k == Particles.Kind.Firefly) ? 260 : ((k == Particles.Kind.Leaf) ? 640 : 512)));
			if (!halved)
			{
				return num;
			}
			return num / 2;
		}

		// Token: 0x06000102 RID: 258 RVA: 0x0000D9F4 File Offset: 0x0000BBF4
		private static void BuildLifetimeGradient(Particles.Rig r)
		{
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = r.Ps.colorOverLifetime;
			colorOverLifetime.enabled = true;
			Gradient gradient = new Gradient();
			switch (r.Kind)
			{
			case Particles.Kind.Firefly:
				gradient.colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.white, 1f)
				};
				gradient.alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.09f),
					new GradientAlphaKey(0.4f, 0.24f),
					new GradientAlphaKey(1f, 0.4f),
					new GradientAlphaKey(0.4f, 0.56f),
					new GradientAlphaKey(1f, 0.72f),
					new GradientAlphaKey(0.45f, 0.88f),
					new GradientAlphaKey(0f, 1f)
				};
				break;
			case Particles.Kind.Ember:
				gradient.colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0f),
					new GradientColorKey(new Color(1f, 0.72f, 0.3f), 0.3f),
					new GradientColorKey(new Color(1f, 0.42f, 0.1f), 0.65f),
					new GradientColorKey(new Color(0.75f, 0.16f, 0.04f), 1f)
				};
				gradient.alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.1f),
					new GradientAlphaKey(0.65f, 0.62f),
					new GradientAlphaKey(0f, 1f)
				};
				break;
			case Particles.Kind.Leaf:
				gradient.colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.white, 1f)
				};
				gradient.alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.06f),
					new GradientAlphaKey(1f, 0.85f),
					new GradientAlphaKey(0f, 1f)
				};
				break;
			default:
				gradient.colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.white, 1f)
				};
				gradient.alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.12f),
					new GradientAlphaKey(1f, 0.8f),
					new GradientAlphaKey(0f, 1f)
				};
				break;
			}
			colorOverLifetime.color = new ParticleSystem.MinMaxGradient(gradient);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x0000DD88 File Offset: 0x0000BF88
		private static void BuildLeafFlutter(Particles.Rig r)
		{
			AnimationCurve animationCurve = new AnimationCurve();
			for (int i = 0; i <= 8; i++)
			{
				animationCurve.AddKey((float)i / 8f, ((i & 1) == 0) ? 1f : 0.55f);
			}
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = r.Ps.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			sizeOverLifetime.separateAxes = true;
			sizeOverLifetime.x = new ParticleSystem.MinMaxCurve(1f, animationCurve);
			sizeOverLifetime.y = new ParticleSystem.MinMaxCurve(1f);
			sizeOverLifetime.z = new ParticleSystem.MinMaxCurve(1f);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x0000DE18 File Offset: 0x0000C018
		private static void ConfigureRenderer(Particles.Rig r)
		{
			r.Psr = r.Go.GetComponent<ParticleSystemRenderer>();
			r.Psr.renderMode = 0;
			r.Psr.alignment = (ParticleSystemRenderSpace)3;
			r.Psr.shadowCastingMode = 0;
			r.Psr.receiveShadows = false;
			r.Psr.minParticleSize = 0.0015f;
			if (r.Kind == Particles.Kind.Leaf)
			{
				r.Psr.maxParticleSize = 0.25f;
				r.Psr.sortMode = (ParticleSystemSortMode)1;
			}
			else
			{
				r.Psr.maxParticleSize = 0.08f;
				r.Psr.sortMode = 0;
			}
			r.Psr.sharedMaterial = r.Mat;
		}

		// Token: 0x06000105 RID: 261 RVA: 0x0000DECC File Offset: 0x0000C0CC
		private static void ApplyRate(Particles.Rig r, float rate)
		{
			if (r.Ps == null)
			{
				return;
			}
			if (Mathf.Abs(rate - r.AppliedRate) < 0.001f)
			{
				return;
			}
			r.AppliedRate = rate;
			ParticleSystem.EmissionModule emission = r.Ps.emission;
			if (!emission.enabled)
			{
				return;
			}
			emission.rateOverTimeMultiplier = rate;
		}

		// Token: 0x06000106 RID: 262 RVA: 0x0000DF24 File Offset: 0x0000C124
		private static void ApplySize(Particles.Rig r, float min, float max)
		{
			if (r.Ps == null)
			{
				return;
			}
			if (Mathf.Abs(min - r.AppliedSizeMin) < 1E-05f)
			{
				return;
			}
			float appliedSizeMin = r.AppliedSizeMin;
			r.AppliedSizeMin = min;
			ParticleSystem.MainModule main = r.Ps.main;
			main.startSize = new ParticleSystem.MinMaxCurve(min, max);
			if (appliedSizeMin > 1E-06f && min > 1E-06f)
			{
				Particles.RetrofitLiveSizes(r, min / appliedSizeMin);
			}
		}

		// Token: 0x06000107 RID: 263 RVA: 0x0000DF98 File Offset: 0x0000C198
		private static ParticleSystem.Particle[] RetroBuffer(Particles.Rig r)
		{
			ParticleSystem.Particle[] array;
			if ((array = r.Retro) == null)
			{
				array = (r.Retro = new ParticleSystem.Particle[Particles.MaxParticlesFor(r.Kind, false)]);
			}
			return array;
		}

		// Token: 0x06000108 RID: 264 RVA: 0x0000DFCC File Offset: 0x0000C1CC
		private static void RetrofitLiveSizes(Particles.Rig r, float scale)
		{
			if (Mathf.Abs(scale - 1f) < 0.0001f)
			{
				return;
			}
			if (r.Ps.particleCount <= 0)
			{
				return;
			}
			ParticleSystem.Particle[] array = Particles.RetroBuffer(r);
			int particles = r.Ps.GetParticles(array);
			for (int i = 0; i < particles; i++)
			{
				ParticleSystem.Particle[] array2 = array;
				int num = i;
				array2[num].startSize = array2[num].startSize * scale;
			}
			r.Ps.SetParticles(array, particles);
		}

		// Token: 0x06000109 RID: 265 RVA: 0x0000E040 File Offset: 0x0000C240
		private static void RetrofitEmberRise(Particles.Rig r, float scale)
		{
			if (Mathf.Abs(scale - 1f) < 0.0001f)
			{
				return;
			}
			if (r.Ps == null || r.Ps.particleCount <= 0)
			{
				return;
			}
			ParticleSystem.Particle[] array = Particles.RetroBuffer(r);
			int particles = r.Ps.GetParticles(array);
			for (int i = 0; i < particles; i++)
			{
				Vector3 velocity = array[i].velocity;
				if (velocity.y > 0f)
				{
					velocity.y *= scale;
					array[i].velocity = velocity;
				}
			}
			r.Ps.SetParticles(array, particles);
		}

		// Token: 0x0600010A RID: 266 RVA: 0x0000E0E0 File Offset: 0x0000C2E0
		private static void ApplyDrift(Particles.Rig r, float vel, float noiseStrength, float noiseFreq, float scroll)
		{
			if (r.Ps == null)
			{
				return;
			}
			if (Mathf.Abs(vel - r.AppliedSpeed) < 0.0001f)
			{
				return;
			}
			r.AppliedSpeed = vel;
			ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = r.Ps.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.space = (ParticleSystemSimulationSpace)1;
			velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-vel, vel);
			velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-vel * 0.35f, vel * 0.75f);
			velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-vel, vel);
			ParticleSystem.NoiseModule noise = r.Ps.noise;
			noise.enabled = noiseStrength > 0.0001f;
			noise.quality = 0;
			noise.octaveCount = 1;
			noise.damping = false;
			noise.frequency = noiseFreq;
			noise.strength = new ParticleSystem.MinMaxCurve(noiseStrength);
			noise.scrollSpeed = new ParticleSystem.MinMaxCurve(scroll);
		}

		// Token: 0x0600010B RID: 267 RVA: 0x0000E1C4 File Offset: 0x0000C3C4
		private static void ApplyLeafMotion(Particles.Rig r)
		{
			if (r.Ps == null)
			{
				return;
			}
			float num = Mathf.Lerp(0.25f, 1.4f, r.SpeedParam);
			if (Mathf.Abs(num - r.AppliedSpeed) < 0.0001f)
			{
				return;
			}
			r.AppliedSpeed = num;
			float num2 = Mathf.Lerp(0.15f, 0.45f, r.SpeedParam);
			float num3 = Mathf.Lerp(0.35f, 0.75f, r.SpeedParam);
			float num4 = Mathf.Lerp(0.6f, 2.4f, r.SpeedParam);
			ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = r.Ps.velocityOverLifetime;
			velocityOverLifetime.enabled = true;
			velocityOverLifetime.space = (ParticleSystemSimulationSpace)1;
			velocityOverLifetime.x = new ParticleSystem.MinMaxCurve(-num2, num2);
			velocityOverLifetime.y = new ParticleSystem.MinMaxCurve(-num * 1.25f, -num * 0.75f);
			velocityOverLifetime.z = new ParticleSystem.MinMaxCurve(-num2, num2);
			ParticleSystem.NoiseModule noise = r.Ps.noise;
			noise.enabled = true;
			noise.quality = 0;
			noise.octaveCount = 1;
			noise.damping = false;
			noise.frequency = 0.32f;
			noise.separateAxes = true;
			noise.strengthX = new ParticleSystem.MinMaxCurve(num3);
			noise.strengthY = new ParticleSystem.MinMaxCurve(num3 * 0.3f);
			noise.strengthZ = new ParticleSystem.MinMaxCurve(num3);
			noise.scrollSpeed = new ParticleSystem.MinMaxCurve(0.22f);
			ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = r.Ps.rotationOverLifetime;
			rotationOverLifetime.enabled = true;
			rotationOverLifetime.separateAxes = false;
			rotationOverLifetime.z = new ParticleSystem.MinMaxCurve(-num4, num4);
		}

		// Token: 0x0600010C RID: 268 RVA: 0x0000E358 File Offset: 0x0000C558
		private static void Teardown(Particles.Rig r)
		{
			if (r.Go != null)
			{
				try
				{
					UnityEngine.Object.Destroy(r.Go);
				}
				catch
				{
				}
			}
			Particles.ForgetSceneObjects(r);
		}

		// Token: 0x0600010D RID: 269 RVA: 0x0000E39C File Offset: 0x0000C59C
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			Particles.DisposeRig(this._dust);
			Particles.DisposeRig(this._fireflies);
			Particles.DisposeRig(this._ember);
			Particles.DisposeRig(this._leaves);
			this._leafGateLoggedZone = null;
			this._dynamicLights = null;
			Particles.AnyEffectOn = false;
		}

		// Token: 0x0600010E RID: 270 RVA: 0x0000E3FC File Offset: 0x0000C5FC
		private static void DisposeRig(Particles.Rig r)
		{
			Particles.Teardown(r);
			if (r.Mat != null)
			{
				try
				{
					UnityEngine.Object.Destroy(r.Mat);
				}
				catch
				{
				}
				r.Mat = null;
			}
			r.LoggedOnline = false;
			r.LastResolvedOn = false;
			r.MatPushPending = true;
		}

		// Token: 0x04000198 RID: 408
		private const string ParticleShaderName = "LumaLooks/WorldParticle";

		// Token: 0x04000199 RID: 409
		private const float SceneSettleSeconds = 2f;

		// Token: 0x0400019A RID: 410
		private const float OnEps = 0.001f;

		// Token: 0x0400019B RID: 411
		private const float MaxWideUi = 3f;

		// Token: 0x0400019C RID: 412
		private const int DustMaxParticles = 2048;

		// Token: 0x0400019D RID: 413
		private const float DustLifetime = 9f;

		// Token: 0x0400019E RID: 414
		private const float DustRateAtFull = 95f;

		// Token: 0x0400019F RID: 415
		private const float DustBoxXZ = 18f;

		// Token: 0x040001A0 RID: 416
		private const float DustBoxY = 11f;

		// Token: 0x040001A1 RID: 417
		private const float DustSizeMin = 0.008f;

		// Token: 0x040001A2 RID: 418
		private const float DustSizeMax = 0.045f;

		// Token: 0x040001A3 RID: 419
		private const int FireflyMaxParticles = 260;

		// Token: 0x040001A4 RID: 420
		private const float FireflyLifetime = 15f;

		// Token: 0x040001A5 RID: 421
		private const float FireflyRateAtFull = 17f;

		// Token: 0x040001A6 RID: 422
		private const float FireflyBoxXZ = 26f;

		// Token: 0x040001A7 RID: 423
		private const float FireflyBoxY = 3f;

		// Token: 0x040001A8 RID: 424
		private const float FireflyBoxYOffset = -0.35f;

		// Token: 0x040001A9 RID: 425
		private const float FireflyGlow = 0.6f;

		// Token: 0x040001AA RID: 426
		private const int EmberMaxParticles = 512;

		// Token: 0x040001AB RID: 427
		private const float EmberLifetime = 2.9f;

		// Token: 0x040001AC RID: 428
		private const float EmberRatePerSource = 16f;

		// Token: 0x040001AD RID: 429
		private const float EmberSourceRange = 32f;

		// Token: 0x040001AE RID: 430
		private const int EmberMaxEmitPerTick = 120;

		// Token: 0x040001AF RID: 431
		private const int LeafMaxParticles = 640;

		// Token: 0x040001B0 RID: 432
		private const float LeafLifetime = 9f;

		// Token: 0x040001B1 RID: 433
		private const float LeafRateAtFull = 26f;

		// Token: 0x040001B2 RID: 434
		private const float LeafBoxXZ = 22f;

		// Token: 0x040001B3 RID: 435
		private const float LeafBoxY = 5f;

		// Token: 0x040001B4 RID: 436
		private const float LeafBoxYOffset = 3.5f;

		// Token: 0x040001B5 RID: 437
		private const float LeafSizeMin = 0.02f;

		// Token: 0x040001B6 RID: 438
		private const float LeafSizeMax = 0.115f;

		// Token: 0x040001B7 RID: 439
		private const int LeafMaxEmitPerTick = 48;

		// Token: 0x040001B9 RID: 441
		private bool _anchorFlip;

		// Token: 0x040001BA RID: 442
		private readonly ManualLogSource _log;

		// Token: 0x040001BB RID: 443
		private readonly RenderEngine _engine;

		// Token: 0x040001BC RID: 444
		private DynamicLights _dynamicLights;

		// Token: 0x040001BD RID: 445
		private readonly Particles.Rig _dust = new Particles.Rig(Particles.Kind.Dust, "LumaLooks_DustMotes");

		// Token: 0x040001BE RID: 446
		private readonly Particles.Rig _fireflies = new Particles.Rig(Particles.Kind.Firefly, "LumaLooks_Glowbugs");

		// Token: 0x040001BF RID: 447
		private readonly Particles.Rig _ember = new Particles.Rig(Particles.Kind.Ember, "LumaLooks_Embers");

		// Token: 0x040001C0 RID: 448
		private readonly Particles.Rig _leaves = new Particles.Rig(Particles.Kind.Leaf, "LumaLooks_Autumn");

		// Token: 0x040001C1 RID: 449
		private bool _vrBalanced;

		// Token: 0x040001C2 RID: 450
		private float _settleUntil = -1f;

		// Token: 0x040001C3 RID: 451
		private bool _loggedNoFireSources;

		// Token: 0x040001C4 RID: 452
		private bool _loggedHeavyDust;

		// Token: 0x040001C5 RID: 453
		private string _leafGateLoggedZone;

		// Token: 0x040001C6 RID: 454
		private float _nextDiag;

		// Token: 0x040001C7 RID: 455
		private bool _diagLogged;

		// Token: 0x040001C8 RID: 456
		private static readonly string[] GtLeafMaterialNames = new string[] { "leafparticle", "fallleaves2", "fallleavesred", "Leaf", "MapleLeafA", "forestleaves" };

		// Token: 0x040001C9 RID: 457
		private Texture _gtLeafTex;

		// Token: 0x040001CA RID: 458
		private float _gtLeafSlice;

		// Token: 0x040001CB RID: 459
		private bool _gtLeafResolved;

		// Token: 0x040001CC RID: 460
		private bool _gtLeafFound;

		// Token: 0x040001CD RID: 461
		private float _gtLeafNextScan = -1f;

		// Token: 0x040001CE RID: 462
		private const float GtLeafRescanSeconds = 6f;

		// Token: 0x0200001A RID: 26
		private enum Kind
		{
			// Token: 0x040001D0 RID: 464
			Dust,
			// Token: 0x040001D1 RID: 465
			Firefly,
			// Token: 0x040001D2 RID: 466
			Ember,
			// Token: 0x040001D3 RID: 467
			Leaf
		}

		// Token: 0x0200001B RID: 27
		private sealed class Rig
		{
			// Token: 0x06000110 RID: 272 RVA: 0x0000E49C File Offset: 0x0000C69C
			public Rig(Particles.Kind k, string goName)
			{
				this.Kind = k;
				this.GoName = goName;
			}

			// Token: 0x040001D4 RID: 468
			public readonly Particles.Kind Kind;

			// Token: 0x040001D5 RID: 469
			public readonly string GoName;

			// Token: 0x040001D6 RID: 470
			public GameObject Go;

			// Token: 0x040001D7 RID: 471
			public ParticleSystem Ps;

			// Token: 0x040001D8 RID: 472
			public ParticleSystemRenderer Psr;

			// Token: 0x040001D9 RID: 473
			public Material Mat;

			// Token: 0x040001DA RID: 474
			public ParticleSystem.EmitParams Emit;

			// Token: 0x040001DB RID: 475
			public ParticleSystem.Particle[] Retro;

			// Token: 0x040001DC RID: 476
			public bool Want;

			// Token: 0x040001DD RID: 477
			public bool VrAllowed = true;

			// Token: 0x040001DE RID: 478
			public bool DesktopAllowed = true;

			// Token: 0x040001DF RID: 479
			public float Density;

			// Token: 0x040001E0 RID: 480
			public float Brightness = 0.5f;

			// Token: 0x040001E1 RID: 481
			public float Glow;

			// Token: 0x040001E2 RID: 482
			public float SizeParam;

			// Token: 0x040001E3 RID: 483
			public float SpeedParam;

			// Token: 0x040001E4 RID: 484
			public float ShapeParam;

			// Token: 0x040001E5 RID: 485
			public float LeafTypeParam = 3f;

			// Token: 0x040001E6 RID: 486
			public bool CapHalved;

			// Token: 0x040001E7 RID: 487
			public float AppliedRate = -1f;

			// Token: 0x040001E8 RID: 488
			public float AppliedSizeMin = -1f;

			// Token: 0x040001E9 RID: 489
			public float AppliedSpeed = -1f;

			// Token: 0x040001EA RID: 490
			public float AppliedRise = -1f;

			// Token: 0x040001EB RID: 491
			public float EmitAcc;

			// Token: 0x040001EC RID: 492
			public bool MatPushPending = true;

			// Token: 0x040001ED RID: 493
			public bool ShaderMissingLogged;

			// Token: 0x040001EE RID: 494
			public bool LoggedOnline;

			// Token: 0x040001EF RID: 495
			public bool LastResolvedOn;
		}
	}
}
