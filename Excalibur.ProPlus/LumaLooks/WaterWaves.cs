using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000057 RID: 87
	internal sealed class WaterWaves
	{
		// Token: 0x0600032D RID: 813 RVA: 0x0002D584 File Offset: 0x0002B784
		static WaterWaves()
		{
			for (int i = 0; i < 6; i++)
			{
				float num = (34f + WaterWaves.OctSpread[i]) * 0.017453292f;
				WaterWaves.WaveDirs[i] = new Vector2(Mathf.Cos(num), Mathf.Sin(num));
			}
		}

		// Token: 0x0600032E RID: 814 RVA: 0x0002D648 File Offset: 0x0002B848
		public WaterWaves(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x0600032F RID: 815 RVA: 0x0002D828 File Offset: 0x0002BA28
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed, float height, float sizeResponse, float scale, float speed, float crest, float detail, float openness, float splash)
		{
			bool on2 = this._on;
			if (!Mathf.Approximately(scale, this._scale) || !Mathf.Approximately(detail, this._detail))
			{
				this._rebuildMeshes = true;
			}
			if (!Mathf.Approximately(height, this._height) || !Mathf.Approximately(sizeResponse, this._sizeResponse) || !Mathf.Approximately(openness, this._openness))
			{
				this._rebuildMeshes = true;
			}
			this._on = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._height = height;
			this._sizeResponse = sizeResponse;
			this._scale = scale;
			this._speed = speed;
			this._crest = crest;
			this._detail = detail;
			this._openness = openness;
			this._splash = splash;
			this.RecomputeCrestMean();
			if (on && !on2)
			{
				this._nextScanAt = 0f;
				this._sceneSettleAt = 0f;
			}
		}

		// Token: 0x06000330 RID: 816 RVA: 0x0002D90C File Offset: 0x0002BB0C
		private void RecomputeCrestMean()
		{
			this.RebuildProfileLut();
			float num = Mathf.Clamp01(this._crest);
			if (num <= 0.001f)
			{
				this._crestMean = 0f;
				return;
			}
			float num2 = 1f + 2f * num;
			double num3 = 0.0;
			for (int i = 0; i < 256; i++)
			{
				double num4 = (1.0 + Math.Sin(6.283185307179586 * (double)i / 256.0)) * 0.5;
				num3 += Math.Pow(num4, (double)num2);
			}
			this._crestMean = (float)(2.0 * (num3 / 256.0) - 1.0);
			this.RebuildProfileLut();
		}

		// Token: 0x06000331 RID: 817 RVA: 0x0002D9D0 File Offset: 0x0002BBD0
		private void RebuildProfileLut()
		{
			float num = Mathf.Clamp01(this._crest);
			float num2 = 1f + 2f * num;
			for (int i = 0; i <= 1024; i++)
			{
				float num3 = 6.2831855f * (float)i / 1024f;
				float num4 = Mathf.Sin(num3);
				float num5 = Mathf.Cos(num3);
				if (num <= 0.001f)
				{
					this._lutH[i] = num4;
					this._lutD[i] = num5;
				}
				else
				{
					float num6 = 0.5f * (num4 + 1f);
					this._lutH[i] = 2f * Mathf.Pow(num6, num2) - 1f - this._crestMean;
					this._lutD[i] = num2 * Mathf.Pow(num6, num2 - 1f) * num5;
				}
			}
		}

		// Token: 0x06000332 RID: 818 RVA: 0x0002DA93 File Offset: 0x0002BC93
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._sceneDirty = true;
			this._loggedThisScene = false;
		}

		// Token: 0x06000333 RID: 819 RVA: 0x0002DAA4 File Offset: 0x0002BCA4
		public void Tick()
		{
			if (this._on && this.TargetAllows())
			{
				float unscaledTime = Time.unscaledTime;
				if (this._sceneDirty)
				{
					this._sceneDirty = false;
					this._sceneSettleAt = unscaledTime + 2f;
					this.ForgetAll();
					this._volumes.Clear();
					this._nextScanAt = this._sceneSettleAt;
					this._nextVolumeScanAt = 0f;
					this._scanCursor = -1;
				}
				if (unscaledTime >= this._nextVolumeScanAt)
				{
					this._nextVolumeScanAt = unscaledTime + 12f * Mathf.Max(1f, PerfMode.ScanMul);
					this.RefreshVolumes();
				}
				if (this._scanCursor >= 0)
				{
					this.StepScan();
				}
				else if (unscaledTime >= this._nextScanAt && unscaledTime >= this._sceneSettleAt)
				{
					this.BeginScan(unscaledTime);
				}
				Vector3 vector = WaterWaves.CameraPos();
				for (int i = 0; i < this._bodies.Count; i++)
				{
					WaterWaves.Body body = this._bodies[i];
					if (!(body.Filter == null) && !(body.Source == null))
					{
						bool flag = (body.BuiltAtCam - vector).sqrMagnitude > 225f;
						if ((this._rebuildMeshes || flag) && unscaledTime >= body.NextRebuildAt)
						{
							body.NextRebuildAt = unscaledTime + 3f;
							this.Retessellate(body, vector);
						}
					}
				}
				this._rebuildMeshes = false;
				this._waveClock += Time.deltaTime;
				if (this._waveClock >= 0.033333335f)
				{
					this._waveClock = 0f;
					this.Animate(Time.time);
				}
				this.TickSplash(Time.deltaTime);
				return;
			}
			if (this._splashPs != null || this._mistPs != null)
			{
				this.TeardownSplash();
			}
			if (this._bodies.Count > 0)
			{
				this.ReleaseAll();
				return;
			}
			if (this._hiddenBatches.Count > 0)
			{
				this.RestoreHiddenBatches();
			}
		}

		// Token: 0x06000334 RID: 820 RVA: 0x0002DC8A File Offset: 0x0002BE8A
		private bool TargetAllows()
		{
			if (!XRSettings.isDeviceActive)
			{
				return this._desktopAllowed;
			}
			return this._vrAllowed;
		}

		// Token: 0x06000335 RID: 821 RVA: 0x0002DCA0 File Offset: 0x0002BEA0
		private static Vector3 CameraPos()
		{
			Camera camera = Camera.main;
			if (camera != null)
			{
				return camera.transform.position;
			}
			camera = Camera.current;
			if (!(camera != null))
			{
				return Vector3.zero;
			}
			return camera.transform.position;
		}

		// Token: 0x06000336 RID: 822 RVA: 0x0002DCE8 File Offset: 0x0002BEE8
		private void RefreshVolumes()
		{
			if (!this._volumeTypeResolved)
			{
				this._volumeTypeResolved = true;
				this._waterVolumeType = WaterWaves.FindType("GorillaLocomotion.Swimming.WaterVolume") ?? WaterWaves.FindType("WaterVolume");
				if (this._waterVolumeType == null)
				{
					this._log.LogWarning("WAVES: GT's WaterVolume type did not resolve — wave size falls back to per-surface extent (a game update renamed it?).");
				}
			}
			if (this._waterVolumeType == null)
			{
				return;
			}
			this._volumes.Clear();
			UnityEngine.Object[] array;
			try
			{
				array = UnityEngine.Object.FindObjectsByType(this._waterVolumeType, 0);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WAVES: WaterVolume scan failed (" + ex.Message + ")");
				return;
			}
			for (int i = 0; i < array.Length; i++)
			{
				Component component = array[i] as Component;
				if (!(component == null))
				{
					this._colScratch.Clear();
					component.GetComponentsInChildren<Collider>(true, this._colScratch);
					bool flag = false;
					Bounds bounds = default(Bounds);
					for (int j = 0; j < this._colScratch.Count; j++)
					{
						Collider collider = this._colScratch[j];
						if (!(collider == null))
						{
							if (!flag)
							{
								bounds = collider.bounds;
								flag = true;
							}
							else
							{
								bounds.Encapsulate(collider.bounds);
							}
						}
					}
					if (flag)
					{
						WaterWaves.Volume volume = new WaterWaves.Volume
						{
							B = bounds
						};
						volume.Flow = this.ReadCurrentSpeed(component);
						volume.Extent = Mathf.Max(bounds.size.x, bounds.size.z);
						volume.Area = Mathf.Max(bounds.size.x * bounds.size.z, 0.0001f);
						volume.Open = this.ProbeOpenness(bounds);
						this.ResolveWaveShape(ref volume);
						this._volumes.Add(volume);
					}
				}
			}
			this._volumes.Sort((WaterWaves.Volume x, WaterWaves.Volume y) => x.Area.CompareTo(y.Area));
			this.FindJunctions();
		}

		// Token: 0x06000337 RID: 823 RVA: 0x0002DF0C File Offset: 0x0002C10C
		private void FindJunctions()
		{
			this._junctions.Clear();
			for (int i = 0; i < this._volumes.Count; i++)
			{
				for (int j = 0; j < this._volumes.Count; j++)
				{
					if (i != j && this._junctions.Count < 6)
					{
						WaterWaves.Volume volume = this._volumes[i];
						WaterWaves.Volume volume2 = this._volumes[j];
						if (volume2.Flow <= 0.15f && volume.Area <= volume2.Area * 0.9f)
						{
							float num = volume.B.max.y - volume2.B.max.y;
							if ((volume.Flow > 0.15f || num >= 0.4f) && volume.B.min.y <= volume2.B.max.y + 2.5f && volume.B.max.y >= volume2.B.max.y - 0.5f)
							{
								float x = volume.B.center.x;
								float z = volume.B.center.z;
								if (x >= volume2.B.min.x - 1f && x <= volume2.B.max.x + 1f && z >= volume2.B.min.z - 1f && z <= volume2.B.max.z + 1f)
								{
									WaterWaves.Junction junction = new WaterWaves.Junction
									{
										P = new Vector3(x, volume2.B.max.y + 0.05f, z),
										Radius = Mathf.Clamp(Mathf.Min(volume.B.size.x, volume.B.size.z) * 0.5f, 0.4f, 4f),
										Strength = Mathf.Clamp01(Mathf.Clamp01(volume.Flow / 2f) * 0.7f + Mathf.Clamp01(num / 3f) * 0.6f),
										Drop = Mathf.Clamp(num, 0f, 8f)
									};
									if (junction.Strength >= 0.08f)
									{
										bool flag = false;
										for (int k = 0; k < this._junctions.Count; k++)
										{
											if ((this._junctions[k].P - junction.P).sqrMagnitude < 4f)
											{
												flag = true;
												break;
											}
										}
										if (!flag)
										{
											this._junctions.Add(junction);
										}
									}
								}
							}
						}
					}
				}
			}
			this.CacheJunctionsForWaves();
			if (this._junctions.Count > 0)
			{
				WaterWaves.Junction junction2 = this._junctions[0];
				this._log.LogInfo(string.Concat(new string[]
				{
					string.Format("WAVES: {0} waterfall junction(s) — first at ", this._junctions.Count),
					"(",
					WaterWaves.F2(junction2.P.x),
					",",
					WaterWaves.F2(junction2.P.y),
					",",
					WaterWaves.F2(junction2.P.z),
					") r=",
					WaterWaves.F2(junction2.Radius),
					"m drop=",
					WaterWaves.F2(junction2.Drop),
					"m strength=",
					WaterWaves.F2(junction2.Strength)
				}));
			}
		}

		// Token: 0x06000338 RID: 824 RVA: 0x0002E314 File Offset: 0x0002C514
		private void CacheJunctionsForWaves()
		{
			this._jCount = Mathf.Min(this._junctions.Count, 6);
			for (int i = 0; i < this._jCount; i++)
			{
				this._jx[i] = this._junctions[i].P.x;
				this._jz[i] = this._junctions[i].P.z;
				this._jStr[i] = this._junctions[i].Strength;
				this._jRad[i] = this._junctions[i].Radius;
			}
		}

		// Token: 0x06000339 RID: 825 RVA: 0x0002E3B8 File Offset: 0x0002C5B8
		private float JunctionBoost(Vector3 wp)
		{
			float num = 0f;
			for (int i = 0; i < this._jCount; i++)
			{
				float num2 = wp.x - this._jx[i];
				float num3 = wp.z - this._jz[i];
				float num4 = Mathf.Sqrt(num2 * num2 + num3 * num3);
				float num5 = Mathf.Max(3f, this._jRad[i] * 4f);
				if (num4 < num5)
				{
					float num6 = 1f - num4 / num5;
					float num7 = this._jStr[i] * num6 * num6;
					if (num7 > num)
					{
						num = num7;
					}
				}
			}
			return Mathf.Clamp01(num);
		}

		// Token: 0x0600033A RID: 826 RVA: 0x0002E450 File Offset: 0x0002C650
		private float StrongestJunction()
		{
			float num = 0f;
			for (int i = 0; i < this._junctions.Count; i++)
			{
				if (this._junctions[i].Strength > num)
				{
					num = this._junctions[i].Strength;
				}
			}
			return num;
		}

		// Token: 0x0600033B RID: 827 RVA: 0x0002E4A0 File Offset: 0x0002C6A0
		private void TickSplash(float dt)
		{
			if (this._splash <= 0.01f || this._junctions.Count <= 0)
			{
				if (this._splashPs != null && this._splashPs.particleCount == 0 && (this._mistPs == null || this._mistPs.particleCount == 0))
				{
					this.TeardownSplash();
				}
				return;
			}
			if (!this.EnsureSplashSystem())
			{
				return;
			}
			Vector3 vector = WaterWaves.CameraPos();
			float num = 0f;
			for (int i = 0; i < this._junctions.Count; i++)
			{
				WaterWaves.Junction junction = this._junctions[i];
				if ((junction.P - vector).sqrMagnitude <= 2025f)
				{
					num += junction.Strength * junction.Radius * 34f * this._splash;
				}
			}
			if (num <= 0.01f)
			{
				return;
			}
			bool isDeviceActive = XRSettings.isDeviceActive;
			int num2 = (isDeviceActive ? 110 : 240);
			int num3 = (isDeviceActive ? 90 : 190);
			this._emitAccum += num * dt;
			this._mistAccum += num * 0.85f * dt;
			this._foamAccum += num * 0.3f * dt;
			ParticleSystem.EmitParams emitParams = default(ParticleSystem.EmitParams);
			int num4 = Mathf.Min((int)this._emitAccum, 40);
			if (num4 > 0)
			{
				this._emitAccum -= (float)num4;
			}
			if (this._splashPs.particleCount >= num2)
			{
				num4 = 0;
			}
			int num5 = 0;
			WaterWaves.Junction junction2;
			while (num5 < num4 && this.PickJunction(vector, out junction2))
			{
				Vector2 vector2 = UnityEngine.Random.insideUnitCircle * junction2.Radius;
				emitParams.position = junction2.P + new Vector3(vector2.x, 0.02f, vector2.y);
				float num6 = Mathf.Lerp(0.9f, 2.4f, junction2.Strength) * UnityEngine.Random.Range(0.5f, 1.15f);
				Vector2 vector3 = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.15f, 1.6f) * (0.4f + junction2.Strength);
				emitParams.velocity = new Vector3(vector3.x, num6, vector3.y);
				emitParams.startLifetime = UnityEngine.Random.Range(0.45f, 1.2f);
				emitParams.startSize = UnityEngine.Random.Range(0.018f, 0.055f) * (0.7f + junction2.Strength);
				emitParams.startColor = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.9f, 1f));
				this._splashPs.Emit(emitParams, 1);
				num5++;
			}
			int num7 = Mathf.Min((int)this._mistAccum, 24);
			if (num7 > 0)
			{
				this._mistAccum -= (float)num7;
			}
			if (this._mistPs.particleCount >= num3)
			{
				num7 = 0;
			}
			int num8 = 0;
			WaterWaves.Junction junction3;
			while (num8 < num7 && this.PickJunction(vector, out junction3))
			{
				Vector2 vector4 = UnityEngine.Random.insideUnitCircle * (junction3.Radius * 1.15f);
				float num9 = UnityEngine.Random.Range(0f, Mathf.Max(0.5f, Mathf.Min(junction3.Drop, 3.5f)));
				emitParams.position = junction3.P + new Vector3(vector4.x, 0.05f + num9, vector4.y);
				Vector2 vector5 = UnityEngine.Random.insideUnitCircle * (0.15f + junction3.Strength * 0.45f);
				emitParams.velocity = new Vector3(vector5.x, UnityEngine.Random.Range(0.18f, 0.7f), vector5.y);
				emitParams.startLifetime = UnityEngine.Random.Range(2.4f, 4.6f);
				emitParams.startSize = UnityEngine.Random.Range(0.12f, 0.38f) * (0.75f + junction3.Strength);
				emitParams.startColor = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.16f, 0.34f));
				this._mistPs.Emit(emitParams, 1);
				num8++;
			}
			int num10 = Mathf.Min((int)this._foamAccum, 16);
			if (num10 > 0)
			{
				this._foamAccum -= (float)num10;
			}
			if (this._mistPs.particleCount >= num3)
			{
				num10 = 0;
			}
			int num11 = 0;
			WaterWaves.Junction junction4;
			while (num11 < num10 && this.PickJunction(vector, out junction4))
			{
				Vector2 vector6 = UnityEngine.Random.insideUnitCircle * (junction4.Radius * 0.9f);
				emitParams.position = junction4.P + new Vector3(vector6.x, 0.03f, vector6.y);
				Vector2 vector7 = UnityEngine.Random.insideUnitCircle.normalized * UnityEngine.Random.Range(0.25f, 1.15f) * (0.5f + junction4.Strength);
				emitParams.velocity = new Vector3(vector7.x, 0.01f, vector7.y);
				emitParams.startLifetime = UnityEngine.Random.Range(2.2f, 4.5f);
				emitParams.startSize = UnityEngine.Random.Range(0.1f, 0.32f) * (0.8f + junction4.Strength);
				emitParams.startColor = new Color(1f, 1f, 1f, UnityEngine.Random.Range(0.5f, 0.8f));
				this._mistPs.Emit(emitParams, 1);
				num11++;
			}
		}

		// Token: 0x0600033C RID: 828 RVA: 0x0002EA6C File Offset: 0x0002CC6C
		private bool PickJunction(Vector3 cam, out WaterWaves.Junction jn)
		{
			for (int i = 0; i < 4; i++)
			{
				WaterWaves.Junction junction = this._junctions[UnityEngine.Random.Range(0, this._junctions.Count)];
				if ((junction.P - cam).sqrMagnitude <= 2025f)
				{
					jn = junction;
					return true;
				}
			}
			jn = default(WaterWaves.Junction);
			return false;
		}

		// Token: 0x0600033D RID: 829 RVA: 0x0002EAD0 File Offset: 0x0002CCD0
		private bool EnsureSplashSystem()
		{
			if (this._splashPs != null && this._mistPs != null)
			{
				return true;
			}
			if (this._splashMat == null)
			{
				Shader shader = ((this._engine != null) ? this._engine.GetShader("LumaLooks/WorldParticle") : null);
				if (shader == null)
				{
					if (!this._splashShaderMissingLogged)
					{
						this._splashShaderMissingLogged = true;
						this._log.LogWarning("WAVES: bundle shader 'LumaLooks/WorldParticle' missing — waterfall splash disabled (waves are unaffected).");
					}
					return false;
				}
				this._splashMat = new Material(shader)
				{
					hideFlags = (HideFlags)61
				};
				this._splashMat.SetFloat(ShaderIds.ParticleKind, 0f);
				this._splashMat.SetFloat(ShaderIds.ParticleBrightness, 1f);
				this._splashMat.SetFloat(ShaderIds.ParticleGlow, 0f);
				this._splashMat.SetFloat(ShaderIds.ParticleShape, 1f);
			}
			if (this._mistMat == null)
			{
				Shader shader2 = ((this._engine != null) ? this._engine.GetShader("LumaLooks/WorldParticle") : null);
				if (shader2 == null)
				{
					return false;
				}
				this._mistMat = new Material(shader2)
				{
					hideFlags = (HideFlags)61
				};
				this._mistMat.SetFloat(ShaderIds.ParticleKind, 0f);
				this._mistMat.SetFloat(ShaderIds.ParticleBrightness, 1f);
				this._mistMat.SetFloat(ShaderIds.ParticleGlow, 0.25f);
				this._mistMat.SetFloat(ShaderIds.ParticleShape, 0f);
			}
			if (this._splashPs == null)
			{
				this._splashGo = new GameObject("LumaWaves_Splash");
				this._splashPs = this.MakeSpraySystem(this._splashGo, 1f, 300, this._splashMat);
			}
			if (this._mistPs == null)
			{
				this._mistGo = new GameObject("LumaWaves_Mist");
				this._mistPs = this.MakeSpraySystem(this._mistGo, 0f, 260, this._mistMat);
			}
			this._log.LogInfo("WAVES: waterfall splash online — droplets + mist + foam (LumaLooks/WorldParticle, Square, world space).");
			return true;
		}

		// Token: 0x0600033E RID: 830 RVA: 0x0002ECEC File Offset: 0x0002CEEC
		private ParticleSystem MakeSpraySystem(GameObject go, float gravity, int max, Material mat)
		{
			ParticleSystem particleSystem = go.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = particleSystem.main;
			main.loop = true;
			main.playOnAwake = false;
			main.simulationSpace = (ParticleSystemSimulationSpace)1;
			main.startSpeed = 0f;
			main.startColor = Color.white;
			main.gravityModifier = gravity;
			main.maxParticles = max;
			ParticleSystem.EmissionModule emission = particleSystem.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = particleSystem.shape;
			shape.enabled = false;
			ParticleSystem.CollisionModule collision = particleSystem.collision;
			collision.enabled = false;
			ParticleSystem.InheritVelocityModule inheritVelocity = particleSystem.inheritVelocity;
			inheritVelocity.enabled = false;
			ParticleSystem.ExternalForcesModule externalForces = particleSystem.externalForces;
			externalForces.enabled = false;
			ParticleSystemRenderer component = go.GetComponent<ParticleSystemRenderer>();
			component.sharedMaterial = mat;
			component.renderMode = 0;
			component.alignment = (ParticleSystemRenderSpace)3;
			component.shadowCastingMode = 0;
			component.receiveShadows = false;
			particleSystem.Play();
			return particleSystem;
		}

		// Token: 0x0600033F RID: 831 RVA: 0x0002EDD4 File Offset: 0x0002CFD4
		private void TeardownSplash()
		{
			try
			{
				if (this._splashGo != null)
				{
					UnityEngine.Object.Destroy(this._splashGo);
				}
			}
			catch
			{
			}
			try
			{
				if (this._mistGo != null)
				{
					UnityEngine.Object.Destroy(this._mistGo);
				}
			}
			catch
			{
			}
			try
			{
				if (this._splashMat != null)
				{
					UnityEngine.Object.Destroy(this._splashMat);
				}
			}
			catch
			{
			}
			try
			{
				if (this._mistMat != null)
				{
					UnityEngine.Object.Destroy(this._mistMat);
				}
			}
			catch
			{
			}
			this._splashGo = null;
			this._mistGo = null;
			this._splashPs = null;
			this._mistPs = null;
			this._splashMat = null;
			this._mistMat = null;
			this._emitAccum = 0f;
			this._mistAccum = 0f;
			this._foamAccum = 0f;
		}

		// Token: 0x06000340 RID: 832 RVA: 0x0002EEDC File Offset: 0x0002D0DC
		private float ReadCurrentSpeed(Component volume)
		{
			float num;
			try
			{
				if (this._piVolumeCurrent == null)
				{
					this._piVolumeCurrent = volume.GetType().GetProperty("Current", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (this._piVolumeCurrent == null)
					{
						return 0f;
					}
				}
				object value = this._piVolumeCurrent.GetValue(volume, null);
				if (value == null)
				{
					num = 0f;
				}
				else
				{
					if (this._piCurrentSpeed == null)
					{
						this._piCurrentSpeed = value.GetType().GetProperty("Speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						if (this._piCurrentSpeed == null)
						{
							return 0f;
						}
					}
					num = Mathf.Abs(Convert.ToSingle(this._piCurrentSpeed.GetValue(value, null)));
				}
			}
			catch
			{
				num = 0f;
			}
			return num;
		}

		// Token: 0x06000341 RID: 833 RVA: 0x0002EFB0 File Offset: 0x0002D1B0
		private float ProbeOpenness(Bounds b)
		{
			try
			{
				RaycastHit raycastHit = default;
				if (Physics.Raycast(new Vector3(b.center.x, b.max.y + 0.15f, b.center.z), Vector3.up, out raycastHit, 40f, -1, QueryTriggerInteraction.Ignore))
				{
					return Mathf.Clamp01(raycastHit.distance / 12f);
				}
			}
			catch
			{
			}
			return 1f;
		}

		// Token: 0x06000342 RID: 834 RVA: 0x0002F034 File Offset: 0x0002D234
		private void ResolveWaveShape(ref WaterWaves.Volume v)
		{
			float num = Mathf.Max(v.Extent, 0.05f);
			float num2 = 0.02f * Mathf.Sqrt(num);
			float num3 = Mathf.Lerp(0.05f, num2, Mathf.Clamp01(this._sizeResponse));
			float num4 = Mathf.Lerp(1f, Mathf.Clamp01(0.15f + 0.85f * v.Open), Mathf.Clamp01(this._openness));
			float num5 = Mathf.Lerp(1f, 0.22f, Mathf.Clamp01(v.Flow / 2.5f));
			v.Amp = num3 * num4 * num5;
			v.AmpCap = num * 0.08f;
			float num6 = Mathf.Clamp(num * 0.38f, 0.12f, 14f);
			v.Wl = Mathf.Lerp(1.2f, num6, Mathf.Clamp01(this._sizeResponse)) * Mathf.Max(this._scale, 0.05f);
		}

		// Token: 0x06000343 RID: 835 RVA: 0x0002F124 File Offset: 0x0002D324
		private void BeginScan(float now)
		{
			this._nextScanAt = now + 12f * Mathf.Max(1f, PerfMode.ScanMul);
			try
			{
				this._scanBuffer = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WAVES: renderer scan failed (" + ex.Message + ")");
				return;
			}
			this._scanCursor = 0;
			this._scanFound.Clear();
		}

		// Token: 0x06000344 RID: 836 RVA: 0x0002F1A4 File Offset: 0x0002D3A4
		private void StepScan()
		{
			if (this._scanBuffer == null)
			{
				this._scanCursor = -1;
				return;
			}
			int num = Mathf.Min(this._scanCursor + 256, this._scanBuffer.Length);
			while (this._scanCursor < num)
			{
				MeshRenderer meshRenderer = this._scanBuffer[this._scanCursor];
				if (!(meshRenderer == null) && (meshRenderer.name == null || !meshRenderer.name.StartsWith("LumaWaves_", StringComparison.Ordinal)) && this.IsWaterSurface(meshRenderer))
				{
					if (meshRenderer.enabled && meshRenderer.gameObject.activeInHierarchy)
					{
						this._drawnWater.Add(meshRenderer);
						if (this._inventory.Count < 24)
						{
							this._inventory.Add(meshRenderer);
						}
					}
					else
					{
						this._sourceWater.Add(meshRenderer);
						this._pooledSeen++;
					}
				}
				this._scanCursor++;
			}
			if (this._scanCursor < this._scanBuffer.Length)
			{
				return;
			}
			this._scanCursor = -1;
			this._scanBuffer = null;
			this.BuildBodiesFromScan();
			this._drawnWater.Clear();
			this._sourceWater.Clear();
			this._dropScratch.Clear();
			for (int i = 0; i < this._bodies.Count; i++)
			{
				if (!this._scanFound.Contains(this._bodies[i]))
				{
					this._dropScratch.Add(this._bodies[i]);
				}
			}
			for (int j = 0; j < this._dropScratch.Count; j++)
			{
				this.Release(this._dropScratch[j]);
				this._bodies.Remove(this._dropScratch[j]);
			}
			this._dropScratch.Clear();
			Vector3 vector = WaterWaves.CameraPos();
			for (int k = 0; k < this._scanFound.Count; k++)
			{
				WaterWaves.Body body = this._scanFound[k];
				if (!this._bodies.Contains(body))
				{
					this._bodies.Add(body);
					this.Retessellate(body, vector);
				}
			}
			this.LogState();
		}

		// Token: 0x06000345 RID: 837 RVA: 0x0002F3C4 File Offset: 0x0002D5C4
		private void BuildBodiesFromScan()
		{
			this._scanFound.Clear();
			for (int i = 0; i < this._bodies.Count; i++)
			{
				WaterWaves.Body body = this._bodies[i];
				if (body.Owned && body.Hidden != null && body.Go != null)
				{
					this._scanFound.Add(body);
				}
			}
			int num = 0;
			while (num < this._drawnWater.Count && this._scanFound.Count < 12)
			{
				MeshRenderer meshRenderer = this._drawnWater[num];
				if (!(meshRenderer == null))
				{
					if (this.CanRebuildInPlace(meshRenderer))
					{
						this.AdoptBody(meshRenderer, false);
					}
					else
					{
						MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
						Mesh mesh = ((component != null) ? component.sharedMesh : null);
						if (mesh != null)
						{
							string text;
							Mesh mesh2 = this.TryRecoverBatchedSubmesh(meshRenderer, mesh, out text);
							if (mesh2 != null)
							{
								GameObject gameObject = new GameObject("LumaWaves_" + meshRenderer.name);
								if (this._recoveredSpaceRoot != null)
								{
									gameObject.transform.SetParent(this._recoveredSpaceRoot, false);
									gameObject.transform.localPosition = Vector3.zero;
									gameObject.transform.localRotation = Quaternion.identity;
									gameObject.transform.localScale = Vector3.one;
								}
								else
								{
									gameObject.transform.SetParent(null, false);
									gameObject.transform.position = Vector3.zero;
									gameObject.transform.rotation = Quaternion.identity;
									gameObject.transform.localScale = Vector3.one;
								}
								gameObject.layer = meshRenderer.gameObject.layer;
								MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
								MeshRenderer meshRenderer2 = gameObject.AddComponent<MeshRenderer>();
								meshRenderer2.sharedMaterial = meshRenderer.sharedMaterial;
								meshRenderer2.shadowCastingMode = meshRenderer.shadowCastingMode;
								meshRenderer2.receiveShadows = meshRenderer.receiveShadows;
								meshRenderer2.lightProbeUsage = meshRenderer.lightProbeUsage;
								meshRenderer2.reflectionProbeUsage = meshRenderer.reflectionProbeUsage;
								meshRenderer2.motionVectorGenerationMode = meshRenderer.motionVectorGenerationMode;
								meshFilter.sharedMesh = mesh2;
								Bounds bounds = meshRenderer2.bounds;
								Bounds bounds2 = meshRenderer.bounds;
								float num2 = Mathf.Max(bounds2.size.magnitude, 1f);
								float num3 = (bounds.center - bounds2.center).magnitude + (bounds.size - bounds2.size).magnitude;
								if (num3 > num2 * 0.35f)
								{
									this._log.LogWarning(string.Concat(new string[]
									{
										"WAVES: placement check FAILED for '",
										meshRenderer.name,
										"' (ours at ",
										WaterWaves.F2(bounds.center.x),
										",",
										WaterWaves.F2(bounds.center.y),
										",",
										WaterWaves.F2(bounds.center.z),
										" size ",
										WaterWaves.F2(bounds.size.x),
										"x",
										WaterWaves.F2(bounds.size.z),
										" vs renderer ",
										WaterWaves.F2(bounds2.center.x),
										",",
										WaterWaves.F2(bounds2.center.y),
										",",
										WaterWaves.F2(bounds2.center.z),
										" size ",
										WaterWaves.F2(bounds2.size.x),
										"x",
										WaterWaves.F2(bounds2.size.z),
										", err ",
										WaterWaves.F2(num3),
										" m, space '",
										this._recoveredSpaceName,
										"') — discarding it, GT's water is untouched."
									}));
									UnityEngine.Object.Destroy(gameObject);
									UnityEngine.Object.Destroy(mesh2);
									goto IL_0871;
								}
								WaterWaves.Body body2 = new WaterWaves.Body
								{
									Filter = meshFilter,
									Rend = meshRenderer2,
									Tf = gameObject.transform,
									Source = mesh2,
									Owned = true,
									Go = gameObject
								};
								body2.Hidden = meshRenderer;
								this._scanFound.Add(body2);
								meshRenderer.enabled = false;
								this._hiddenBatches.Add(meshRenderer);
								Bounds bounds3 = mesh2.bounds;
								Bounds bounds4 = meshRenderer.bounds;
								this._log.LogInfo(string.Concat(new string[]
								{
									"WAVES: recovered '",
									meshRenderer.name,
									"' off the GPU ",
									string.Format("({0} verts, {1} tris, ", mesh2.vertexCount, mesh2.triangles.Length / 3),
									"size ",
									WaterWaves.F2(bounds3.size.x),
									"x",
									WaterWaves.F2(bounds3.size.y),
									"x",
									WaterWaves.F2(bounds3.size.z),
									" vs renderer ",
									WaterWaves.F2(bounds4.size.x),
									"x",
									WaterWaves.F2(bounds4.size.y),
									"x",
									WaterWaves.F2(bounds4.size.z),
									", ",
									(this._lastLiquidFrac >= 0f) ? string.Format("{0}% of it inside GT water volumes", Mathf.RoundToInt(this._lastLiquidFrac * 100f)) : "no water volumes to check against",
									", space='",
									this._recoveredSpaceName,
									"') — drawing it ourselves with GT's own material and hiding the batch."
								}));
								goto IL_0871;
							}
							else
							{
								this._log.LogWarning(string.Concat(new string[] { "WAVES: GPU recovery of '", meshRenderer.name, "' failed (", text, ") — falling back to its disabled originals." }));
							}
						}
						Bounds bounds5 = meshRenderer.bounds;
						float num4 = 0f;
						int num5 = 0;
						int num6 = 0;
						int num7 = 0;
						while (num7 < this._sourceWater.Count && this._scanFound.Count < 12)
						{
							MeshRenderer meshRenderer3 = this._sourceWater[num7];
							if (!(meshRenderer3 == null) && this.CanRebuildInPlace(meshRenderer3))
							{
								if (!meshRenderer3.gameObject.activeInHierarchy)
								{
									num6++;
								}
								else
								{
									Bounds bounds6 = meshRenderer3.bounds;
									if (bounds5.Intersects(bounds6))
									{
										num4 += bounds6.size.x * bounds6.size.z;
										this.AdoptBody(meshRenderer3, true);
										num5++;
									}
								}
							}
							num7++;
						}
						float num8 = Mathf.Max(bounds5.size.x * bounds5.size.z, 0.0001f);
						if (num5 > 0 && num4 / num8 >= 0.25f)
						{
							meshRenderer.enabled = false;
							this._hiddenBatches.Add(meshRenderer);
							this._log.LogInfo(string.Concat(new string[]
							{
								"WAVES: '",
								meshRenderer.name,
								"' is static-batched (mesh not readable) — ",
								string.Format("waving its {0} original surface(s) instead and hiding the batch ", num5),
								string.Format("(coverage {0:0.00}×).", num4 / num8)
							}));
						}
						else
						{
							for (int j = this._scanFound.Count - 1; j >= 0; j--)
							{
								if (this._scanFound[j].Adopted)
								{
									this.Release(this._scanFound[j]);
									this._scanFound.RemoveAt(j);
								}
							}
							this._log.LogWarning(string.Concat(new string[]
							{
								"WAVES: '",
								meshRenderer.name,
								"' cannot be waved — its mesh is static-batched ",
								string.Format("and not readable, and only {0} usable original surface(s) ", num5),
								string.Format("(coverage {0:0.00}× of a {1}x{2} m ", num4 / num8, WaterWaves.F2(bounds5.size.x), WaterWaves.F2(bounds5.size.z)),
								string.Format("footprint, plus {0} more under inactive parents) were found to ", num6),
								"replace it. Left exactly as GT shipped it."
							}));
						}
					}
				}
				IL_0871:
				num++;
			}
		}

		// Token: 0x06000346 RID: 838 RVA: 0x0002FC68 File Offset: 0x0002DE68
		private void AdoptBody(MeshRenderer r, bool adopted)
		{
			MeshFilter component = r.GetComponent<MeshFilter>();
			if (component == null)
			{
				return;
			}
			for (int i = 0; i < this._bodies.Count; i++)
			{
				if (this._bodies[i].Filter == component)
				{
					this._scanFound.Add(this._bodies[i]);
					return;
				}
			}
			WaterWaves.Body body = new WaterWaves.Body
			{
				Filter = component,
				Rend = r,
				Tf = r.transform,
				Source = component.sharedMesh,
				Adopted = adopted
			};
			this._scanFound.Add(body);
		}

		// Token: 0x06000347 RID: 839 RVA: 0x0002FD0C File Offset: 0x0002DF0C
		private bool CanRebuildInPlace(MeshRenderer r)
		{
			if (r.isPartOfStaticBatch)
			{
				return false;
			}
			MeshFilter component = r.GetComponent<MeshFilter>();
			if (component == null)
			{
				return false;
			}
			Mesh sharedMesh = component.sharedMesh;
			return !(sharedMesh == null) && sharedMesh.isReadable && sharedMesh.subMeshCount == 1 && r.sharedMaterials.Length == 1 && sharedMesh.vertexCount <= 40000;
		}

		// Token: 0x06000348 RID: 840 RVA: 0x0002FD78 File Offset: 0x0002DF78
		private void LogState()
		{
			StringBuilder stringBuilder = new StringBuilder(400);
			for (int i = 0; i < this._inventory.Count; i++)
			{
				MeshRenderer meshRenderer = this._inventory[i];
				if (!(meshRenderer == null))
				{
					MeshFilter component = meshRenderer.GetComponent<MeshFilter>();
					Mesh mesh = ((component != null) ? component.sharedMesh : null);
					Bounds bounds = meshRenderer.bounds;
					stringBuilder.Append(" | ").Append(meshRenderer.name).Append('[')
						.Append(meshRenderer.enabled ? "drawn" : "src")
						.Append(meshRenderer.gameObject.activeInHierarchy ? "" : ",goOff")
						.Append(meshRenderer.isPartOfStaticBatch ? ",BATCHED" : "")
						.Append((mesh == null) ? ",noMesh" : (mesh.isReadable ? ",readable" : ",UNREADABLE"))
						.Append(",sub=")
						.Append((mesh != null) ? mesh.subMeshCount : 0)
						.Append(",v=")
						.Append((mesh != null) ? mesh.vertexCount : 0)
						.Append(",size=")
						.Append(WaterWaves.F2(bounds.size.x))
						.Append('x')
						.Append(WaterWaves.F2(bounds.size.z))
						.Append(']');
				}
			}
			string text = stringBuilder.ToString();
			bool flag = text != this._lastInventory;
			this._lastInventory = text;
			if (!flag && this._loggedThisScene)
			{
				return;
			}
			this._loggedThisScene = true;
			StringBuilder stringBuilder2 = new StringBuilder(260);
			stringBuilder2.Append("WAVES: ").Append(this._bodies.Count).Append(" water surface(s), ")
				.Append(this._volumes.Count)
				.Append(" swimmable volume(s)");
			int num = this._volumes.Count - 1;
			while (num >= 0 && num >= this._volumes.Count - 3)
			{
				stringBuilder2.Append(" | BIGvol[extent=").Append(WaterWaves.F2(this._volumes[num].Extent)).Append("m open=")
					.Append(WaterWaves.F2(this._volumes[num].Open))
					.Append(" amp=")
					.Append(WaterWaves.F2(Mathf.Min(this._volumes[num].Amp * Mathf.Max(this._height, 0f), this._volumes[num].AmpCap)))
					.Append("m(cap ")
					.Append(WaterWaves.F2(this._volumes[num].AmpCap))
					.Append(") wl=")
					.Append(WaterWaves.F2(this._volumes[num].Wl))
					.Append("m]");
				num--;
			}
			int num2 = 0;
			while (num2 < this._volumes.Count && num2 < 2)
			{
				stringBuilder2.Append(" | smallvol[extent=").Append(WaterWaves.F2(this._volumes[num2].Extent)).Append("m amp=")
					.Append(WaterWaves.F2(Mathf.Min(this._volumes[num2].Amp * Mathf.Max(this._height, 0f), this._volumes[num2].AmpCap)))
					.Append("m]");
				num2++;
			}
			int num3 = 0;
			while (num3 < this._bodies.Count && num3 < 4)
			{
				WaterWaves.Body body = this._bodies[num3];
				stringBuilder2.Append(" | ").Append((body.Rend != null) ? body.Rend.name : "?").Append("[src=")
					.Append((body.Source != null) ? body.Source.vertexCount : 0)
					.Append("v out=")
					.Append((body.Verts != null) ? body.Verts.Length : 0)
					.Append("v inVolume=")
					.Append(body.InVolume)
					.Append(" active=")
					.Append((body.Active != null) ? body.Active.Length : 0)
					.Append(" quad=")
					.Append(WaterWaves.F2(body.QuadSize))
					.Append("m minWave=")
					.Append(WaterWaves.F2(body.QuadSize * 6f))
					.Append("m]");
				num3++;
			}
			if (this._bodies.Count == 0)
			{
				stringBuilder2.Append(" — NOTHING WAVED");
			}
			stringBuilder2.Append("  DRAWN(").Append(this._inventory.Count).Append(", +")
				.Append(this._pooledSeen)
				.Append(" pooled/inactive ignored)")
				.Append(text);
			for (int j = 0; j < this._inventory.Count; j++)
			{
				MeshRenderer meshRenderer2 = this._inventory[j];
				if (!(meshRenderer2 == null) && meshRenderer2.enabled)
				{
					bool flag2 = false;
					int num4 = 0;
					while (num4 < this._bodies.Count && !flag2)
					{
						flag2 = this._bodies[num4].Rend == meshRenderer2 || this._bodies[num4].Hidden == meshRenderer2;
						num4++;
					}
					if (!flag2)
					{
						stringBuilder2.Append("  ⚠ STILL FLAT: ").Append(meshRenderer2.name).Append(" [")
							.Append(WaterWaves.F2(meshRenderer2.bounds.size.x))
							.Append('x')
							.Append(WaterWaves.F2(meshRenderer2.bounds.size.z))
							.Append(" m]");
					}
				}
			}
			this._log.LogInfo(stringBuilder2.ToString());
		}

		// Token: 0x06000349 RID: 841 RVA: 0x00030414 File Offset: 0x0002E614
		private bool IsWaterSurface(MeshRenderer r)
		{
			Material sharedMaterial = r.sharedMaterial;
			if (sharedMaterial == null)
			{
				return false;
			}
			Shader shader = sharedMaterial.shader;
			if (shader == null)
			{
				return false;
			}
			if (this._waterShader != null && shader == this._waterShader)
			{
				return !WaterWaves.IsLava(sharedMaterial, r);
			}
			string name = shader.name;
			if (name == null || !name.StartsWith("GorillaTag/WaterSurface", StringComparison.Ordinal))
			{
				return false;
			}
			if (this._waterShader == null)
			{
				this._waterShader = shader;
			}
			return !WaterWaves.IsLava(sharedMaterial, r);
		}

		// Token: 0x0600034A RID: 842 RVA: 0x000304A8 File Offset: 0x0002E6A8
		private static bool IsLava(Material mat, Renderer r)
		{
			return (mat.name != null && mat.name.IndexOf("lava", StringComparison.OrdinalIgnoreCase) >= 0) || (r.name != null && r.name.IndexOf("lava", StringComparison.OrdinalIgnoreCase) >= 0);
		}

		// Token: 0x0600034B RID: 843 RVA: 0x000304F4 File Offset: 0x0002E6F4
		private void Retessellate(WaterWaves.Body b, Vector3 cam)
		{
			if (b.Filter == null || b.Source == null)
			{
				return;
			}
			try
			{
				Mesh source = b.Source;
				Vector3[] vertices = source.vertices;
				int[] triangles = source.triangles;
				if (vertices != null && triangles != null && triangles.Length >= 3)
				{
					Vector2[] uv = source.uv;
					Vector2[] uv2 = source.uv2;
					Vector2[] uv3 = source.uv3;
					Vector3[] normals = source.normals;
					Color[] colors = source.colors;
					Vector4[] tangents = source.tangents;
					int num = (XRSettings.isDeviceActive ? 10000 : 26000);
					if (PerfMode.LowCpu)
					{
						num /= 2;
					}
					this.PreSplitLargeNear(b, cam, num, ref vertices, ref triangles, ref uv, ref uv2, ref uv3, ref normals, ref colors, ref tangents);
					int num2 = triangles.Length / 3;
					Transform tf = b.Tf;
					int[] array = new int[num2];
					Vector3[] array2 = new Vector3[num2];
					float[] array3 = new float[num2];
					bool[] array4 = new bool[num2];
					for (int i = 0; i < num2; i++)
					{
						Vector3 vector = tf.TransformPoint(vertices[triangles[i * 3]]);
						Vector3 vector2 = tf.TransformPoint(vertices[triangles[i * 3 + 1]]);
						Vector3 vector3 = tf.TransformPoint(vertices[triangles[i * 3 + 2]]);
						array2[i] = (vector + vector2 + vector3) / 3f;
						array3[i] = (Vector3.Distance(vector, vector2) + Vector3.Distance(vector2, vector3) + Vector3.Distance(vector3, vector)) / 3f;
						Vector3 vector4 = Vector3.Cross(vector2 - vector, vector3 - vector);
						array4[i] = vector4.sqrMagnitude > 1E-09f && vector4.normalized.y > 0.5f;
					}
					int num3 = (XRSettings.isDeviceActive ? 10000 : 26000);
					if (PerfMode.LowCpu)
					{
						num3 /= 2;
					}
					int num4 = 0;
					for (int j = 0; j < this._bodies.Count; j++)
					{
						if (this._bodies[j] != b && this._bodies[j].Verts != null)
						{
							num4 += this._bodies[j].Verts.Length;
						}
					}
					int num5 = Mathf.Max(256, num3 - num4);
					float num6 = 1f;
					int num7 = 0;
					for (int k = 0; k < 12; k++)
					{
						num7 = 0;
						for (int l = 0; l < num2; l++)
						{
							array[l] = (array4[l] ? this.SubdivisionFor(array2[l], array3[l], cam, num6) : 1);
							num7 += WaterWaves.VertsPerTri(array[l]);
						}
						if (num7 <= num5)
						{
							break;
						}
						num6 *= 1.6f;
					}
					if (num7 > num5)
					{
						num7 = 0;
						for (int m = 0; m < num2; m++)
						{
							array[m] = 1;
							num7 += WaterWaves.VertsPerTri(1);
						}
					}
					double num8 = 0.0;
					int num9 = 0;
					for (int n = 0; n < num2; n++)
					{
						if (Vector3.Distance(array2[n], cam) <= 32f)
						{
							num8 += (double)(array3[n] / (float)Mathf.Max(1, array[n]));
							num9++;
						}
					}
					b.QuadSize = ((num9 > 0) ? Mathf.Clamp((float)(num8 / (double)num9), 0.05f, 8f) : 1f);
					this.Tessellate(b, vertices, triangles, uv, uv2, uv3, normals, colors, tangents, array, num7);
					b.BuiltAtCam = cam;
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning(string.Concat(new string[]
				{
					"WAVES: could not rebuild '",
					(b.Rend != null) ? b.Rend.name : "?",
					"' (",
					ex.GetType().Name,
					": ",
					ex.Message,
					") — left as GT shipped it."
				}));
				this.Release(b);
			}
		}

		// Token: 0x0600034C RID: 844 RVA: 0x0003092C File Offset: 0x0002EB2C
		private int SubdivisionFor(Vector3 centroid, float edge, Vector3 cam, float coarsen)
		{
			if (Vector3.Distance(centroid, cam) >= 32f)
			{
				return 1;
			}
			float num = Mathf.Max(this._scale * 0.12f, 0.05f) * coarsen / (0.6f + this._detail);
			return Mathf.Clamp(Mathf.CeilToInt(edge / Mathf.Max(num, 0.02f)), 1, 128);
		}

		// Token: 0x0600034D RID: 845 RVA: 0x00030990 File Offset: 0x0002EB90
		private void PreSplitLargeNear(WaterWaves.Body b, Vector3 cam, int budget, ref Vector3[] sv, ref int[] st, ref Vector2[] uv0, ref Vector2[] uv1, ref Vector2[] uv2, ref Vector3[] sn, ref Color[] sc, ref Vector4[] stan)
		{
			int num = st.Length / 3;
			if (num <= 0 || num > 4096)
			{
				return;
			}
			Transform tf = b.Tf;
			bool flag = uv0 != null && uv0.Length == sv.Length;
			bool flag2 = uv1 != null && uv1.Length == sv.Length;
			bool flag3 = uv2 != null && uv2.Length == sv.Length;
			bool flag4 = sc != null && sc.Length == sv.Length;
			bool flag5 = sn != null && sn.Length == sv.Length;
			bool flag6 = stan != null && stan.Length == sv.Length;
			int[] array = new int[num];
			int num2 = 0;
			int num3 = 0;
			bool flag7 = false;
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = tf.TransformPoint(sv[st[i * 3]]);
				Vector3 vector2 = tf.TransformPoint(sv[st[i * 3 + 1]]);
				Vector3 vector3 = tf.TransformPoint(sv[st[i * 3 + 2]]);
				float num4 = (Vector3.Distance(vector, vector2) + Vector3.Distance(vector2, vector3) + Vector3.Distance(vector3, vector)) / 3f;
				float num5 = Vector3.Distance((vector + vector2 + vector3) / 3f, cam);
				int num6 = 1;
				if (num4 > 14.400001f && num5 < 40f)
				{
					num6 = Mathf.Clamp(Mathf.CeilToInt(num4 / 14.400001f), 1, 10);
				}
				array[i] = num6;
				if (num6 > 1)
				{
					flag7 = true;
				}
				num2 += WaterWaves.VertsPerTri(num6);
				num3 += num6 * num6;
			}
			if (!flag7)
			{
				return;
			}
			int num7 = Mathf.Max(48, budget / 2);
			int num8 = 0;
			while (num8 < 8 && num2 > num7)
			{
				for (int j = 0; j < num; j++)
				{
					if (array[j] > 1)
					{
						array[j] = Mathf.Max(1, array[j] - 1);
					}
				}
				num2 = 0;
				num3 = 0;
				for (int k = 0; k < num; k++)
				{
					num2 += WaterWaves.VertsPerTri(array[k]);
					num3 += array[k] * array[k];
				}
				num8++;
			}
			if (num2 > num7)
			{
				return;
			}
			Vector3[] array2 = new Vector3[num2];
			Vector3[] array3 = (flag5 ? new Vector3[num2] : null);
			Vector2[] array4 = (flag ? new Vector2[num2] : null);
			Vector2[] array5 = (flag2 ? new Vector2[num2] : null);
			Vector2[] array6 = (flag3 ? new Vector2[num2] : null);
			Color[] array7 = (flag4 ? new Color[num2] : null);
			Vector4[] array8 = (flag6 ? new Vector4[num2] : null);
			int[] array9 = new int[num3 * 3];
			int num9 = 0;
			int num10 = 0;
			for (int l = 0; l < num; l++)
			{
				int num11 = array[l];
				int num12 = st[l * 3];
				int num13 = st[l * 3 + 1];
				int num14 = st[l * 3 + 2];
				int num15 = num9;
				for (int m = 0; m <= num11; m++)
				{
					for (int n = 0; n <= num11 - m; n++)
					{
						float num16 = (float)(num11 - m - n) / (float)num11;
						float num17 = (float)n / (float)num11;
						float num18 = (float)m / (float)num11;
						array2[num9] = sv[num12] * num16 + sv[num13] * num17 + sv[num14] * num18;
						if (array3 != null)
						{
							array3[num9] = (sn[num12] * num16 + sn[num13] * num17 + sn[num14] * num18).normalized;
						}
						if (array4 != null)
						{
							array4[num9] = uv0[num12] * num16 + uv0[num13] * num17 + uv0[num14] * num18;
						}
						if (array5 != null)
						{
							array5[num9] = uv1[num12] * num16 + uv1[num13] * num17 + uv1[num14] * num18;
						}
						if (array6 != null)
						{
							array6[num9] = uv2[num12] * num16 + uv2[num13] * num17 + uv2[num14] * num18;
						}
						if (array7 != null)
						{
							array7[num9] = sc[num12] * num16 + sc[num13] * num17 + sc[num14] * num18;
						}
						if (array8 != null)
						{
							array8[num9] = stan[num12] * num16 + stan[num13] * num17 + stan[num14] * num18;
						}
						num9++;
					}
				}
				int num19 = 0;
				for (int num20 = 0; num20 < num11; num20++)
				{
					int num21 = num11 - num20 + 1;
					int num22 = num19 + num21;
					for (int num23 = 0; num23 < num11 - num20; num23++)
					{
						array9[num10++] = num15 + num19 + num23;
						array9[num10++] = num15 + num19 + num23 + 1;
						array9[num10++] = num15 + num22 + num23;
						if (num23 < num11 - num20 - 1)
						{
							array9[num10++] = num15 + num19 + num23 + 1;
							array9[num10++] = num15 + num22 + num23 + 1;
							array9[num10++] = num15 + num22 + num23;
						}
					}
					num19 = num22;
				}
			}
			if (num10 < array9.Length)
			{
				Array.Resize<int>(ref array9, num10);
			}
			sv = array2;
			st = array9;
			uv0 = array4;
			uv1 = array5;
			uv2 = array6;
			sc = array7;
			sn = array3;
			stan = array8;
		}

		// Token: 0x0600034E RID: 846 RVA: 0x00030FEF File Offset: 0x0002F1EF
		private static int VertsPerTri(int k)
		{
			return (k + 1) * (k + 2) / 2;
		}

		// Token: 0x0600034F RID: 847 RVA: 0x00030FFC File Offset: 0x0002F1FC
		private void Tessellate(WaterWaves.Body b, Vector3[] sv, int[] st, Vector2[] uv0, Vector2[] uv1, Vector2[] uv2, Vector3[] sn, Color[] sc, Vector4[] stan, int[] ks, int vcount)
		{
			int num = st.Length / 3;
			int num2 = 0;
			for (int i = 0; i < num; i++)
			{
				num2 += ks[i] * ks[i] * 3;
			}
			Vector3[] array = new Vector3[vcount];
			Vector3[] array2 = new Vector3[vcount];
			Vector2[] array3 = ((uv0 != null && uv0.Length == sv.Length) ? new Vector2[vcount] : null);
			Vector2[] array4 = ((uv1 != null && uv1.Length == sv.Length) ? new Vector2[vcount] : null);
			Vector2[] array5 = ((uv2 != null && uv2.Length == sv.Length) ? new Vector2[vcount] : null);
			Color[] array6 = ((sc != null && sc.Length == sv.Length) ? new Color[vcount] : null);
			Vector4[] array7 = ((stan != null && stan.Length == sv.Length) ? new Vector4[vcount] : null);
			int[] array8 = new int[num2];
			int num3 = 0;
			int num4 = 0;
			for (int j = 0; j < num; j++)
			{
				int num5 = ks[j];
				int num6 = st[j * 3];
				int num7 = st[j * 3 + 1];
				int num8 = st[j * 3 + 2];
				int num9 = num3;
				for (int k = 0; k <= num5; k++)
				{
					for (int l = 0; l <= num5 - k; l++)
					{
						float num10 = (float)(num5 - k - l) / (float)num5;
						float num11 = (float)l / (float)num5;
						float num12 = (float)k / (float)num5;
						array[num3] = sv[num6] * num10 + sv[num7] * num11 + sv[num8] * num12;
						array2[num3] = ((sn != null && sn.Length == sv.Length) ? (sn[num6] * num10 + sn[num7] * num11 + sn[num8] * num12).normalized : Vector3.up);
						if (array3 != null)
						{
							array3[num3] = uv0[num6] * num10 + uv0[num7] * num11 + uv0[num8] * num12;
						}
						if (array4 != null)
						{
							array4[num3] = uv1[num6] * num10 + uv1[num7] * num11 + uv1[num8] * num12;
						}
						if (array5 != null)
						{
							array5[num3] = uv2[num6] * num10 + uv2[num7] * num11 + uv2[num8] * num12;
						}
						if (array6 != null)
						{
							array6[num3] = sc[num6] * num10 + sc[num7] * num11 + sc[num8] * num12;
						}
						if (array7 != null)
						{
							array7[num3] = stan[num6] * num10 + stan[num7] * num11 + stan[num8] * num12;
						}
						num3++;
					}
				}
				int num13 = 0;
				for (int m = 0; m < num5; m++)
				{
					int num14 = num5 - m + 1;
					int num15 = num13 + num14;
					for (int n = 0; n < num5 - m; n++)
					{
						array8[num4++] = num9 + num13 + n;
						array8[num4++] = num9 + num13 + n + 1;
						array8[num4++] = num9 + num15 + n;
						if (n < num5 - m - 1)
						{
							array8[num4++] = num9 + num13 + n + 1;
							array8[num4++] = num9 + num15 + n + 1;
							array8[num4++] = num9 + num15 + n;
						}
					}
					num13 = num15;
				}
			}
			if (num4 < array8.Length)
			{
				Array.Resize<int>(ref array8, num4);
			}
			Mesh mesh = new Mesh
			{
				name = "LumaWaves_" + ((b.Rend != null) ? b.Rend.name : "water")
			};
			mesh.indexFormat = (IndexFormat)((num3 > 65000) ? 1 : 0);
			mesh.MarkDynamic();
			mesh.vertices = array;
			mesh.triangles = array8;
			mesh.normals = array2;
			if (array3 != null)
			{
				mesh.uv = array3;
			}
			if (array4 != null)
			{
				mesh.uv2 = array4;
			}
			if (array5 != null)
			{
				mesh.uv3 = array5;
			}
			if (array6 != null)
			{
				mesh.colors = array6;
			}
			if (array7 != null)
			{
				mesh.tangents = array7;
			}
			if (b.Generated != null)
			{
				UnityEngine.Object.Destroy(b.Generated);
			}
			b.Generated = mesh;
			b.BaseLocal = array;
			if (b.Verts == null || b.Verts.Length != num3)
			{
				b.Verts = new Vector3[num3];
			}
			if (b.Normals == null || b.Normals.Length != num3)
			{
				b.Normals = new Vector3[num3];
			}
			if (b.WorldXZ == null || b.WorldXZ.Length != num3)
			{
				b.WorldXZ = new Vector2[num3];
			}
			if (b.Amp == null || b.Amp.Length != num3)
			{
				b.Amp = new float[num3];
			}
			if (b.AmpCap == null || b.AmpCap.Length != num3)
			{
				b.AmpCap = new float[num3];
			}
			if (b.Wl == null || b.Wl.Length != num3)
			{
				b.Wl = new float[num3];
			}
			Array.Copy(array, b.Verts, num3);
			Array.Copy(array2, b.Normals, num3);
			this.CacheWorldSpace(b);
			this.AssignVertexBodies(b);
			this.ComputeLocalConditions(b, WaterWaves.CameraPos());
			Bounds bounds = b.Source.bounds;
			float num16 = Mathf.Abs(b.MaxAmp * b.LocalPerWorldY);
			bounds.Expand(new Vector3(0f, Mathf.Max(0.5f, num16 * 4f), 0f));
			mesh.bounds = bounds;
			b.Filter.sharedMesh = mesh;
			if (b.Adopted && b.Rend != null)
			{
				b.Rend.enabled = true;
			}
			b.Applied = true;
		}

		// Token: 0x06000350 RID: 848 RVA: 0x00031690 File Offset: 0x0002F890
		private void CacheWorldSpace(WaterWaves.Body b)
		{
			Transform tf = b.Tf;
			for (int i = 0; i < b.BaseLocal.Length; i++)
			{
				Vector3 vector = tf.TransformPoint(b.BaseLocal[i]);
				b.WorldXZ[i] = new Vector2(vector.x, vector.z);
			}
			Vector3 vector2 = tf.InverseTransformVector(Vector3.up);
			b.LocalPerWorldY = ((Mathf.Abs(vector2.y) > 1E-05f) ? vector2.y : 1f);
		}

		// Token: 0x06000351 RID: 849 RVA: 0x00031718 File Offset: 0x0002F918
		private void AssignVertexBodies(WaterWaves.Body b)
		{
			float num = 2f;
			WaterWaves.Volume volume = new WaterWaves.Volume
			{
				Extent = num,
				Open = 1f,
				Area = num * num
			};
			this.ResolveWaveShape(ref volume);
			Transform tf = b.Tf;
			b.InVolume = 0;
			b.MaxAmp = 0f;
			for (int i = 0; i < b.BaseLocal.Length; i++)
			{
				Vector3 vector = tf.TransformPoint(b.BaseLocal[i]);
				float num2 = volume.Amp;
				float num3 = volume.Wl;
				float num4 = volume.AmpCap;
				for (int j = 0; j < this._volumes.Count; j++)
				{
					WaterWaves.Volume volume2 = this._volumes[j];
					if (vector.x >= volume2.B.min.x && vector.x <= volume2.B.max.x && vector.z >= volume2.B.min.z && vector.z <= volume2.B.max.z && vector.y >= volume2.B.min.y - 1f && vector.y <= volume2.B.max.y + 1f)
					{
						num2 = volume2.Amp;
						num3 = volume2.Wl;
						num4 = volume2.AmpCap;
						b.InVolume++;
						break;
					}
				}
				b.Amp[i] = num2;
				b.AmpCap[i] = num4;
				b.Wl[i] = num3;
				if (num4 > b.MaxAmp)
				{
					b.MaxAmp = num4;
				}
			}
		}

		// Token: 0x06000352 RID: 850 RVA: 0x000318FC File Offset: 0x0002FAFC
		private void Animate(float time)
		{
			float num = time * Mathf.Max(this._speed, 0f);
			for (int i = 0; i < this._bodies.Count; i++)
			{
				WaterWaves.Body body = this._bodies[i];
				if (body.Filter == null || body.Rend == null || body.Generated == null || body.BaseLocal == null)
				{
					this._dropScratch.Add(body);
				}
				else if (body.Rend.isVisible && body.MaxAmp > 0.0001f && this._height > 0.0001f)
				{
					this.WriteWaves(body, num);
					body.Generated.SetVertices(new List<Vector3>(body.Verts), 0, body.Verts.Length, MeshUpdateFlags.Default);
				}
			}
			if (this._dropScratch.Count > 0)
			{
				for (int j = 0; j < this._dropScratch.Count; j++)
				{
					this.Release(this._dropScratch[j]);
					this._bodies.Remove(this._dropScratch[j]);
				}
				this._dropScratch.Clear();
			}
		}

		// Token: 0x06000353 RID: 851 RVA: 0x00031A2C File Offset: 0x0002FC2C
		private void WriteWaves(WaterWaves.Body b, float t)
		{
			float num = Mathf.Clamp01(this._detail);
			for (int i = 0; i < 6; i++)
			{
				this._group[i] = ((i < 2) ? (0.72f + 0.28f * Mathf.Sin(0.098f * t * (float)(i + 1) + WaterWaves.OctPhase[i] * 1.7f)) : 1f);
			}
			float num2 = 1f / ((Mathf.Abs(b.LocalPerWorldY) > 1E-05f) ? b.LocalPerWorldY : 1f);
			Transform tf = b.Tf;
			bool flag = b.Local != null && b.Local.Length == b.BaseLocal.Length;
			float num3 = b.QuadSize * 4f;
			int[] active = b.Active;
			int num4 = ((active != null) ? active.Length : b.BaseLocal.Length);
			for (int j = 0; j < num4; j++)
			{
				int num5 = ((active != null) ? active[j] : j);
				Vector3 vector = b.BaseLocal[num5];
				float num6 = (flag ? b.Local[num5] : 1f);
				float num7 = Mathf.Min(b.Amp[num5] * Mathf.Max(this._height, 0f), b.AmpCap[num5]) * num6;
				if (num7 <= 1E-05f)
				{
					b.Verts[num5] = vector;
				}
				else
				{
					float num8 = Mathf.Max(b.Wl[num5], 0.02f);
					float x = b.WorldXZ[num5].x;
					float y = b.WorldXZ[num5].y;
					float num9 = 0f;
					for (int k = 0; k < 6; k++)
					{
						float num10 = WaterWaves.OctAmp[k] * ((k >= 3) ? Mathf.Lerp(0.35f, 1f, num) : 1f);
						if (num10 > 0.0001f)
						{
							float num11 = num8 * WaterWaves.OctLen[k];
							if (WaterWaves.OctLenCap[k] > 0f)
							{
								num11 = Mathf.Min(num11, Mathf.Min(WaterWaves.OctLenCap[k], num8));
							}
							if ((k <= 0 || num11 >= num3) && num11 >= 0.05f)
							{
								float num12 = 6.2831855f / num11;
								Vector2 vector2 = WaterWaves.WaveDirs[k];
								float num13 = Mathf.Sqrt(9.81f * num12);
								float num14 = num12 * (vector2.x * x + vector2.y * y) - num13 * t + WaterWaves.OctPhase[k];
								float num15 = num7 * num10 * this._group[k];
								float num16 = num14 * 0.15915494f;
								float num17 = (num16 - Mathf.Floor(num16)) * 1024f;
								int num18 = (int)num17;
								float num19 = num17 - (float)num18;
								float num20 = this._lutH[num18] + (this._lutH[num18 + 1] - this._lutH[num18]) * num19;
								num9 += num15 * num20;
							}
						}
					}
					for (int l = 0; l < this._jCount; l++)
					{
						float num21 = x - this._jx[l];
						float num22 = y - this._jz[l];
						float num23 = num21 * num21 + num22 * num22;
						float num24 = Mathf.Max(6f, this._jRad[l] * 6f);
						if (num23 < num24 * num24)
						{
							float num25 = Mathf.Sqrt(num23);
							if (num25 >= 0.05f)
							{
								float num26 = 1f - num25 / num24;
								num26 *= num26;
								float num27 = Mathf.Clamp(this._jRad[l] * 1.8f, 0.8f, 5f) * Mathf.Max(this._scale, 0.3f);
								float num28 = 6.2831855f / num27;
								float num29 = Mathf.Sqrt(9.81f * num28);
								float num30 = (num28 * num25 - num29 * t) * 0.15915494f;
								float num31 = (num30 - Mathf.Floor(num30)) * 1024f;
								int num32 = (int)num31;
								float num33 = num31 - (float)num32;
								float num34 = this._lutH[num32] + (this._lutH[num32 + 1] - this._lutH[num32]) * num33;
								num9 += num7 * 0.55f * this._jStr[l] * num26 * num34;
							}
						}
					}
					b.Verts[num5] = new Vector3(vector.x, vector.y + num9 * num2, vector.z);
				}
			}
		}

		// Token: 0x06000354 RID: 852 RVA: 0x00031E90 File Offset: 0x00030090
		private void ComputeLocalConditions(WaterWaves.Body b, Vector3 cam)
		{
			int num = b.BaseLocal.Length;
			if (b.Local == null || b.Local.Length != num)
			{
				b.Local = new float[num];
			}
			Transform tf = b.Tf;
			this.BuildBoundaryEdges(b);
			float num2 = 42f;
			float num3 = num2 * num2;
			this._depthCache.Clear();
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = tf.TransformPoint(b.BaseLocal[i]);
				float num4 = Vector3.Distance(vector, cam);
				if (num4 >= 32f)
				{
					b.Local[i] = 0f;
				}
				else
				{
					float num5 = 1f;
					if (this._boundarySeg.Count > 0)
					{
						float num6 = this.DistanceToBoundary(new Vector2(vector.x, vector.z));
						float num7 = Mathf.Clamp(b.Wl[i] * 0.35f, 1.2f, 6f);
						num5 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(num6 / num7));
					}
					float num8 = 1f;
					if ((vector - cam).sqrMagnitude < num3)
					{
						long num9 = ((long)Mathf.FloorToInt(vector.x / 4f) << 32) ^ (long)((ulong)Mathf.FloorToInt(vector.z / 4f));
						float num10;
						if (!this._depthCache.TryGetValue(num9, out num10))
						{
							num10 = this.ProbeSite(vector);
							this._depthCache[num9] = num10;
						}
						num8 = num10;
					}
					float num11 = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 32f, num4));
					float num12 = num5 * num8;
					float num13 = this.JunctionBoost(vector);
					b.Local[i] = Mathf.Clamp01(Mathf.Max(num12, num13) * num11);
				}
			}
			int num14 = 0;
			for (int j = 0; j < num; j++)
			{
				if (b.Local[j] > 0.002f)
				{
					num14++;
				}
			}
			if (b.Active == null || b.Active.Length != num14)
			{
				b.Active = new int[num14];
			}
			int num15 = 0;
			for (int k = 0; k < num; k++)
			{
				if (b.Local[k] > 0.002f)
				{
					b.Active[num15++] = k;
				}
			}
		}

		// Token: 0x06000355 RID: 853 RVA: 0x000320E4 File Offset: 0x000302E4
		private float ProbeSite(Vector3 wp)
		{
			float num = 1f;
			float num2 = 1f;
			try
			{
				Vector3 vector = wp + Vector3.up * 0.25f;
				RaycastHit raycastHit = default;
				if (Physics.Raycast(vector, Vector3.down, out raycastHit, 30f, -1, (QueryTriggerInteraction)1))
				{
					float num3 = Mathf.Max(0f, raycastHit.distance - 0.25f);
					num = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num3 - 0.15f) / 2.2f));
				}
				float num4 = 6f;
				for (int i = 0; i < 4; i++)
				{
					Vector3 vector2 = ((i == 0) ? Vector3.forward : ((i == 1) ? Vector3.back : ((i == 2) ? Vector3.right : Vector3.left)));
					RaycastHit raycastHit2 = default;
					if (Physics.Raycast(vector, vector2, out raycastHit2, 6f, -1, (QueryTriggerInteraction)1))
					{
						num4 = Mathf.Min(num4, raycastHit2.distance);
					}
				}
				num2 = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((num4 - 0.5f) / 4.5f));
			}
			catch
			{
			}
			return Mathf.Min(num, num2);
		}

		// Token: 0x06000356 RID: 854 RVA: 0x00032208 File Offset: 0x00030408
		private void BuildBoundaryEdges(WaterWaves.Body b)
		{
			this._boundarySeg.Clear();
			Mesh source = b.Source;
			if (source == null || !source.isReadable)
			{
				return;
			}
			Vector3[] vertices = source.vertices;
			int[] triangles = source.triangles;
			if (vertices == null || triangles == null || triangles.Length < 3 || vertices.Length > 20000)
			{
				return;
			}
			Transform tf = b.Tf;
			this._edgeCount.Clear();
			Dictionary<int, long> dictionary = new Dictionary<int, long>(vertices.Length);
			for (int i = 0; i < vertices.Length; i++)
			{
				Vector3 vector = vertices[i];
				dictionary[i] = ((long)Mathf.RoundToInt(vector.x * 100f) << 42) ^ ((long)Mathf.RoundToInt(vector.y * 100f) << 21) ^ (long)Mathf.RoundToInt(vector.z * 100f);
			}
			int num = 0;
			while (num + 2 < triangles.Length)
			{
				for (int j = 0; j < 3; j++)
				{
					int num2 = triangles[num + j];
					int num3 = triangles[num + (j + 1) % 3];
					long num4 = dictionary[num2];
					long num5 = dictionary[num3];
					long num6 = ((num4 < num5) ? num4 : num5);
					long num7 = ((num4 < num5) ? num5 : num4);
					WaterWaves.EdgeKey edgeKey = new WaterWaves.EdgeKey
					{
						A = num6,
						B = num7,
						I0 = num2,
						I1 = num3
					};
					int num8;
					if (this._edgeCount.TryGetValue(edgeKey.Pair, out num8))
					{
						this._edgeCount[edgeKey.Pair] = num8 + 1;
					}
					else
					{
						this._edgeCount[edgeKey.Pair] = 1;
						this._edgeFirst[edgeKey.Pair] = edgeKey;
					}
				}
				num += 3;
			}
			foreach (KeyValuePair<long, int> keyValuePair in this._edgeCount)
			{
				if (keyValuePair.Value == 1)
				{
					WaterWaves.EdgeKey edgeKey2 = this._edgeFirst[keyValuePair.Key];
					Vector3 vector2 = tf.TransformPoint(vertices[edgeKey2.I0]);
					Vector3 vector3 = tf.TransformPoint(vertices[edgeKey2.I1]);
					this._boundarySeg.Add(new Vector4(vector2.x, vector2.z, vector3.x, vector3.z));
					if (this._boundarySeg.Count > 4000)
					{
						break;
					}
				}
			}
			this._edgeCount.Clear();
			this._edgeFirst.Clear();
		}

		// Token: 0x06000357 RID: 855 RVA: 0x000324C4 File Offset: 0x000306C4
		private float DistanceToBoundary(Vector2 p)
		{
			float num = float.MaxValue;
			for (int i = 0; i < this._boundarySeg.Count; i++)
			{
				Vector4 vector = this._boundarySeg[i];
				Vector2 vector2 = new Vector2(vector.x, vector.y);
				Vector2 vector3 = new Vector2(vector.z, vector.w) - vector2;
				float sqrMagnitude = vector3.sqrMagnitude;
				float num2 = ((sqrMagnitude > 1E-06f) ? Mathf.Clamp01(Vector2.Dot(p - vector2, vector3) / sqrMagnitude) : 0f);
				float sqrMagnitude2 = (p - (vector2 + vector3 * num2)).sqrMagnitude;
				if (sqrMagnitude2 < num)
				{
					num = sqrMagnitude2;
				}
			}
			if (num != 3.4028235E+38f)
			{
				return Mathf.Sqrt(num);
			}
			return 999f;
		}

		// Token: 0x06000358 RID: 856 RVA: 0x0003259C File Offset: 0x0003079C
		private Mesh TryRecoverBatchedSubmesh(MeshRenderer r, Mesh batch, out string why)
		{
			why = null;
			GraphicsBuffer graphicsBuffer = null;
			Dictionary<int, byte[]> dictionary = new Dictionary<int, byte[]>(4);
			Mesh mesh;
			try
			{
				VertexAttributeDescriptor[] vertexAttributes = batch.GetVertexAttributes();
				if (vertexAttributes == null || vertexAttributes.Length == 0)
				{
					why = "no vertex attributes";
					mesh = null;
				}
				else
				{
					try
					{
						batch.vertexBufferTarget |= GraphicsBuffer.Target.Vertex;
					}
					catch
					{
					}
					try
					{
						batch.indexBufferTarget |= GraphicsBuffer.Target.Index;
					}
					catch
					{
					}
					int vertexCount = batch.vertexCount;
					if (vertexCount <= 0 || vertexCount > 40000)
					{
						why = "vertexCount " + vertexCount.ToString() + " out of range";
						mesh = null;
					}
					else
					{
						for (int i = 0; i < vertexAttributes.Length; i++)
						{
							int stream = vertexAttributes[i].stream;
							if (!dictionary.ContainsKey(stream))
							{
								int vertexBufferStride = batch.GetVertexBufferStride(stream);
								if (vertexBufferStride <= 0)
								{
									why = "stream " + stream.ToString() + " stride " + vertexBufferStride.ToString();
									return null;
								}
								GraphicsBuffer vertexBuffer = batch.GetVertexBuffer(stream);
								if (vertexBuffer == null)
								{
									why = "stream " + stream.ToString() + " buffer unavailable";
									return null;
								}
								try
								{
									byte[] array = new byte[vertexCount * vertexBufferStride];
									vertexBuffer.GetData(array);
									dictionary[stream] = array;
								}
								finally
								{
									vertexBuffer.Dispose();
								}
							}
						}
						Vector3[] array2 = new Vector3[vertexCount];
						Vector2[] array3 = null;
						Vector2[] array4 = null;
						Color[] array5 = null;
						Vector3[] array6 = null;
						bool flag = false;
						foreach (VertexAttributeDescriptor vertexAttributeDescriptor in vertexAttributes)
						{
							byte[] array7;
							if (dictionary.TryGetValue(vertexAttributeDescriptor.stream, out array7))
							{
								int vertexBufferStride2 = batch.GetVertexBufferStride(vertexAttributeDescriptor.stream);
								int num = WaterWaves.AttributeOffset(vertexAttributes, vertexAttributeDescriptor);
								if (vertexAttributeDescriptor.attribute == null)
								{
									for (int k = 0; k < vertexCount; k++)
									{
										array2[k] = WaterWaves.ReadV3(array7, k * vertexBufferStride2 + num, vertexAttributeDescriptor.format);
									}
									flag = true;
								}
								else if (vertexAttributeDescriptor.attribute == VertexAttribute.Normal)
								{
									array6 = new Vector3[vertexCount];
									for (int l = 0; l < vertexCount; l++)
									{
										array6[l] = WaterWaves.ReadV3(array7, l * vertexBufferStride2 + num, vertexAttributeDescriptor.format);
									}
								}
								else if (vertexAttributeDescriptor.attribute == VertexAttribute.Tangent)
								{
									array3 = new Vector2[vertexCount];
									for (int m = 0; m < vertexCount; m++)
									{
										array3[m] = WaterWaves.ReadV2(array7, m * vertexBufferStride2 + num, vertexAttributeDescriptor.format);
									}
								}
								else if (vertexAttributeDescriptor.attribute == VertexAttribute.TexCoord0)
								{
									array4 = new Vector2[vertexCount];
									for (int n = 0; n < vertexCount; n++)
									{
										array4[n] = WaterWaves.ReadV2(array7, n * vertexBufferStride2 + num, vertexAttributeDescriptor.format);
									}
								}
								else if (vertexAttributeDescriptor.attribute == VertexAttribute.Color)
								{
									array5 = new Color[vertexCount];
									for (int num2 = 0; num2 < vertexCount; num2++)
									{
										array5[num2] = WaterWaves.ReadColor(array7, num2 * vertexBufferStride2 + num, vertexAttributeDescriptor.format, vertexAttributeDescriptor.dimension);
									}
								}
							}
						}
						if (!flag)
						{
							why = "no position attribute";
							mesh = null;
						}
						else
						{
							graphicsBuffer = batch.GetIndexBuffer();
							if (graphicsBuffer == null)
							{
								why = "index buffer unavailable";
								mesh = null;
							}
							else
							{
								bool flag2 = batch.indexFormat == 0;
								byte[] array8 = new byte[graphicsBuffer.count * graphicsBuffer.stride];
								graphicsBuffer.GetData(array8);
								Bounds bounds = r.bounds;
								float num3 = Mathf.Max(bounds.size.magnitude, 1f);
								List<Matrix4x4> list = new List<Matrix4x4>(3);
								List<Transform> list2 = new List<Transform>(3);
								List<string> list3 = new List<string>(3);
								Transform transform = WaterWaves.StaticBatchRoot(r);
								if (transform != null)
								{
									list.Add(transform.localToWorldMatrix);
									list2.Add(transform);
									list3.Add("batchRoot:" + transform.name);
								}
								list.Add(Matrix4x4.identity);
								list2.Add(null);
								list3.Add("world");
								if (r.transform != transform)
								{
									list.Add(r.transform.localToWorldMatrix);
									list2.Add(r.transform);
									list3.Add("renderer:" + r.name);
								}
								int subMeshCount = batch.subMeshCount;
								Vector3[] array9 = new Vector3[subMeshCount];
								Vector3[] array10 = new Vector3[subMeshCount];
								bool[] array11 = new bool[subMeshCount];
								for (int num4 = 0; num4 < subMeshCount; num4++)
								{
									SubMeshDescriptor subMesh = batch.GetSubMesh(num4);
									if (subMesh.indexCount >= 3)
									{
										Vector3 vector = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
										Vector3 vector2 = new Vector3(float.MinValue, float.MinValue, float.MinValue);
										bool flag3 = false;
										for (int num5 = 0; num5 < subMesh.indexCount; num5++)
										{
											int num6 = WaterWaves.ReadIndex(array8, subMesh.indexStart + num5, flag2) + subMesh.baseVertex;
											if (num6 >= 0 && num6 < vertexCount)
											{
												Vector3 vector3 = array2[num6];
												vector = Vector3.Min(vector, vector3);
												vector2 = Vector3.Max(vector2, vector3);
												flag3 = true;
											}
										}
										if (flag3)
										{
											array9[num4] = vector;
											array10[num4] = vector2;
											array11[num4] = true;
										}
									}
								}
								int num7 = -1;
								int num8 = 0;
								int num9 = -1;
								float num10 = 0f;
								float num11 = 0f;
								float num12 = float.MaxValue;
								string text = "none";
								for (int num13 = 0; num13 < list.Count; num13++)
								{
									for (int num14 = 0; num14 < subMeshCount; num14++)
									{
										if (array11[num14])
										{
											Vector3 vector4 = array9[num14];
											Vector3 vector5 = array10[num14];
											int num15 = 1;
											while (num14 + num15 <= subMeshCount)
											{
												int num16 = num14 + num15 - 1;
												if (!array11[num16])
												{
													break;
												}
												if (num15 > 1)
												{
													vector4 = Vector3.Min(vector4, array9[num16]);
													vector5 = Vector3.Max(vector5, array10[num16]);
												}
												Bounds bounds2 = WaterWaves.TransformedBounds(vector4, vector5, list[num13]);
												float num17 = (bounds2.center - bounds.center).magnitude + (bounds2.size - bounds.size).magnitude;
												if (num17 < num12)
												{
													num12 = num17;
													text = string.Concat(new string[]
													{
														"[",
														num14.ToString(),
														"..",
														(num14 + num15 - 1).ToString(),
														"] in ",
														list3[num13],
														" err ",
														WaterWaves.F2(num17),
														" m (size ",
														WaterWaves.F2(bounds2.size.x),
														"x",
														WaterWaves.F2(bounds2.size.y),
														"x",
														WaterWaves.F2(bounds2.size.z),
														")"
													});
												}
												if (num17 <= num3 * 0.25f)
												{
													float num18 = ((this._volumes.Count > 0) ? this.LiquidFractionOfRange(array8, flag2, batch, num14, num15, array2, list[num13]) : 1f);
													float num19 = Mathf.Max(2f, num3 * 0.01f);
													bool flag4 = num9 < 0 || num17 < num10 - num19;
													if (!flag4 && num9 >= 0 && num17 <= num10 + num19)
													{
														flag4 = num18 > num11 + 0.02f || (Mathf.Abs(num18 - num11) <= 0.02f && num15 < num8);
													}
													if (flag4)
													{
														num7 = num14;
														num8 = num15;
														num9 = num13;
														num10 = num17;
														num11 = num18;
													}
												}
												num15++;
											}
										}
									}
								}
								if (num9 < 0)
								{
									why = string.Concat(new string[]
									{
										"no submesh range in any candidate space matches the renderer bounds (renderer ",
										WaterWaves.F2(bounds.size.x),
										"x",
										WaterWaves.F2(bounds.size.y),
										"x",
										WaterWaves.F2(bounds.size.z),
										" m, ",
										subMeshCount.ToString(),
										" submeshes, tolerance ",
										WaterWaves.F2(num3 * 0.25f),
										" m; closest was ",
										text,
										")"
									});
									mesh = null;
								}
								else if (this._volumes.Count > 0 && num11 < 0.05f)
								{
									why = string.Concat(new string[]
									{
										"best bounds match is not liquid at all - ",
										Mathf.RoundToInt(num11 * 100f).ToString(),
										"% of it stands in any of GT's ",
										this._volumes.Count.ToString(),
										" water volumes"
									});
									mesh = null;
								}
								else
								{
									Matrix4x4 matrix4x = list[num9];
									this._recoveredSpaceRoot = list2[num9];
									this._recoveredSpaceName = list3[num9];
									this._lastLiquidFrac = ((this._volumes.Count > 0) ? num11 : (-1f));
									int num20 = 0;
									for (int num21 = num7; num21 < num7 + num8; num21++)
									{
										num20 += batch.GetSubMesh(num21).indexCount;
									}
									if (num20 < 3)
									{
										why = "chosen subrange has no triangles";
										mesh = null;
									}
									else
									{
										int[] array12 = new int[num20];
										int num22 = 0;
										for (int num23 = num7; num23 < num7 + num8; num23++)
										{
											SubMeshDescriptor subMesh2 = batch.GetSubMesh(num23);
											for (int num24 = 0; num24 < subMesh2.indexCount; num24++)
											{
												array12[num22++] = WaterWaves.ReadIndex(array8, subMesh2.indexStart + num24, flag2) + subMesh2.baseVertex;
											}
										}
										Bounds bounds3 = WaterWaves.IndexedBounds(array12, array2, matrix4x);
										float num25 = (bounds3.center - bounds.center).magnitude + (bounds3.size - bounds.size).magnitude;
										if (num25 > num3 * 0.25f)
										{
											why = "recovered geometry does not match the renderer bounds (err " + WaterWaves.F2(num25) + " m)";
											mesh = null;
										}
										else
										{
											this._log.LogInfo(string.Concat(new string[]
											{
												"WAVES: batch solve for '",
												r.name,
												"' -> submeshes [",
												num7.ToString(),
												"..",
												(num7 + num8 - 1).ToString(),
												"] of ",
												subMeshCount.ToString(),
												", space=",
												this._recoveredSpaceName,
												", boundsErr=",
												WaterWaves.F2(num10),
												" m, liquid=",
												(this._volumes.Count > 0) ? (Mathf.RoundToInt(num11 * 100f).ToString() + "%") : "n/a"
											}));
											Dictionary<int, int> dictionary2 = new Dictionary<int, int>(array12.Length);
											List<Vector3> list4 = new List<Vector3>(array12.Length);
											List<Vector2> list5 = ((array3 != null) ? new List<Vector2>(array12.Length) : null);
											List<Vector2> list6 = ((array4 != null) ? new List<Vector2>(array12.Length) : null);
											List<Color> list7 = ((array5 != null) ? new List<Color>(array12.Length) : null);
											List<Vector3> list8 = ((array6 != null) ? new List<Vector3>(array12.Length) : null);
											for (int num26 = 0; num26 < array12.Length; num26++)
											{
												int num27 = array12[num26];
												if (num27 < 0 || num27 >= vertexCount)
												{
													why = "index " + num27.ToString() + " out of range";
													return null;
												}
												int count;
												if (!dictionary2.TryGetValue(num27, out count))
												{
													count = list4.Count;
													dictionary2[num27] = count;
													list4.Add(array2[num27]);
													if (list5 != null)
													{
														list5.Add(array3[num27]);
													}
													if (list6 != null)
													{
														list6.Add(array4[num27]);
													}
													if (list7 != null)
													{
														list7.Add(array5[num27]);
													}
													if (list8 != null)
													{
														list8.Add(array6[num27]);
													}
												}
												array12[num26] = count;
											}
											if (list4.Count < 3)
											{
												why = "submesh had no usable vertices";
												mesh = null;
											}
											else
											{
												if (this._volumes.Count > 0)
												{
													int num28 = 0;
													int num29 = 0;
													int num30 = Mathf.Max(1, list4.Count / 500);
													for (int num31 = 0; num31 < list4.Count; num31 += num30)
													{
														num29++;
														if (this.InAnyVolume(matrix4x.MultiplyPoint3x4(list4[num31])))
														{
															num28++;
														}
													}
													float num32 = ((num29 > 0) ? ((float)num28 / (float)num29) : 0f);
													if (num32 < 0.15f)
													{
														why = string.Concat(new string[]
														{
															"recovered geometry is not liquid - only ",
															Mathf.RoundToInt(num32 * 100f).ToString(),
															"% of it stands in one of GT's ",
															this._volumes.Count.ToString(),
															" water volumes"
														});
														return null;
													}
													this._lastLiquidFrac = num32;
												}
												else
												{
													this._lastLiquidFrac = -1f;
												}
												Mesh mesh2 = new Mesh();
												mesh2.name = "LumaWavesSource_" + r.name;
												mesh2.indexFormat = (IndexFormat)((list4.Count > 65000) ? 1 : 0);
												mesh2.SetVertices(list4);
												mesh2.SetTriangles(array12, 0);
												if (list5 != null)
												{
													mesh2.SetUVs(0, list5);
												}
												if (list6 != null)
												{
													mesh2.SetUVs(1, list6);
												}
												if (list7 != null)
												{
													mesh2.SetColors(list7);
												}
												if (list8 != null)
												{
													mesh2.SetNormals(list8);
												}
												else
												{
													mesh2.RecalculateNormals();
												}
												mesh2.RecalculateBounds();
												mesh = mesh2;
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				why = ex.GetType().Name + ": " + ex.Message;
				mesh = null;
			}
			finally
			{
				try
				{
					if (graphicsBuffer != null)
					{
						graphicsBuffer.Dispose();
					}
				}
				catch
				{
				}
			}
			return mesh;
		}

		// Token: 0x06000359 RID: 857 RVA: 0x00033488 File Offset: 0x00031688
		private static int AttributeOffset(VertexAttributeDescriptor[] attrs, VertexAttributeDescriptor self)
		{
			int num = 0;
			for (int i = 0; i < attrs.Length; i++)
			{
				if (attrs[i].stream == self.stream)
				{
					if (attrs[i].attribute == self.attribute)
					{
						break;
					}
					num += WaterWaves.FormatSize(attrs[i].format) * attrs[i].dimension;
				}
			}
			return num;
		}

		// Token: 0x0600035A RID: 858 RVA: 0x000334F1 File Offset: 0x000316F1
		private static int FormatSize(VertexAttributeFormat f)
		{
			switch (f)
			{
			case VertexAttributeFormat.Float32:
			case VertexAttributeFormat.UInt32:
				return 4;
			case VertexAttributeFormat.Float16:
			case VertexAttributeFormat.UNorm16:
			case VertexAttributeFormat.SNorm16:
			case VertexAttributeFormat.UInt16:
				return 2;
			}
			return 1;
		}

		// Token: 0x0600035B RID: 859 RVA: 0x00033530 File Offset: 0x00031730
		private static float ReadComp(byte[] b, int at, VertexAttributeFormat f)
		{
			switch (f)
			{
			case VertexAttributeFormat.Float32:
				return BitConverter.ToSingle(b, at);
			case VertexAttributeFormat.Float16:
				return Mathf.HalfToFloat(BitConverter.ToUInt16(b, at));
			case VertexAttributeFormat.UNorm8:
				return (float)b[at] / 255f;
			case VertexAttributeFormat.SNorm8:
				return Mathf.Max((float)((sbyte)b[at]) / 127f, -1f);
			case VertexAttributeFormat.UNorm16:
				return (float)BitConverter.ToUInt16(b, at) / 65535f;
			case VertexAttributeFormat.SNorm16:
				return Mathf.Max((float)BitConverter.ToInt16(b, at) / 32767f, -1f);
			case VertexAttributeFormat.UInt8:
				return (float)b[at];
			case VertexAttributeFormat.SInt8:
				return (float)((sbyte)b[at]);
			case VertexAttributeFormat.UInt16:
				return (float)BitConverter.ToUInt16(b, at);
			case VertexAttributeFormat.UInt32:
				return BitConverter.ToUInt32(b, at);
			case VertexAttributeFormat.SInt32:
				return BitConverter.ToInt32(b, at);
			default:
				return 0f;
			}
		}

		// Token: 0x0600035C RID: 860 RVA: 0x00033604 File Offset: 0x00031804
		private static Vector3 ReadV3(byte[] b, int at, VertexAttributeFormat f)
		{
			int num = WaterWaves.FormatSize(f);
			return new Vector3(WaterWaves.ReadComp(b, at, f), WaterWaves.ReadComp(b, at + num, f), WaterWaves.ReadComp(b, at + num * 2, f));
		}

		// Token: 0x0600035D RID: 861 RVA: 0x0003363C File Offset: 0x0003183C
		private static Vector2 ReadV2(byte[] b, int at, VertexAttributeFormat f)
		{
			int num = WaterWaves.FormatSize(f);
			return new Vector2(WaterWaves.ReadComp(b, at, f), WaterWaves.ReadComp(b, at + num, f));
		}

		// Token: 0x0600035E RID: 862 RVA: 0x00033668 File Offset: 0x00031868
		private static Color ReadColor(byte[] b, int at, VertexAttributeFormat f, int dim)
		{
			int num = WaterWaves.FormatSize(f);
			float num2 = WaterWaves.ReadComp(b, at, f);
			float num3 = ((dim > 1) ? WaterWaves.ReadComp(b, at + num, f) : num2);
			float num4 = ((dim > 2) ? WaterWaves.ReadComp(b, at + num * 2, f) : num2);
			float num5 = ((dim > 3) ? WaterWaves.ReadComp(b, at + num * 3, f) : 1f);
			return new Color(num2, num3, num4, num5);
		}

		// Token: 0x0600035F RID: 863 RVA: 0x000336CE File Offset: 0x000318CE
		private static int ReadIndex(byte[] b, int i, bool i16)
		{
			if (!i16)
			{
				return (int)BitConverter.ToUInt32(b, i * 4);
			}
			return (int)BitConverter.ToUInt16(b, i * 2);
		}

		// Token: 0x06000360 RID: 864 RVA: 0x000336E8 File Offset: 0x000318E8
		private bool InAnyVolume(Vector3 w)
		{
			for (int i = 0; i < this._volumes.Count; i++)
			{
				Bounds b = this._volumes[i].B;
				if (w.x >= b.min.x && w.x <= b.max.x && w.z >= b.min.z && w.z <= b.max.z && w.y >= b.min.y - 1.5f && w.y <= b.max.y + 1.5f)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000361 RID: 865 RVA: 0x000337AC File Offset: 0x000319AC
		private static Bounds IndexedBounds(int[] tris, Vector3[] pos, Matrix4x4 l2w)
		{
			bool flag = false;
			Bounds bounds = default(Bounds);
			foreach (int num in tris)
			{
				if (num >= 0 && num < pos.Length)
				{
					Vector3 vector = l2w.MultiplyPoint3x4(pos[num]);
					if (!flag)
					{
						bounds = new Bounds(vector, Vector3.zero);
						flag = true;
					}
					else
					{
						bounds.Encapsulate(vector);
					}
				}
			}
			return bounds;
		}

		// Token: 0x06000362 RID: 866 RVA: 0x00033810 File Offset: 0x00031A10
		private static Transform StaticBatchRoot(Renderer r)
		{
			if (!WaterWaves._sbRootResolved)
			{
				WaterWaves._sbRootResolved = true;
				try
				{
					WaterWaves._piStaticBatchRoot = typeof(Renderer).GetProperty("staticBatchRootTransform", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				catch
				{
				}
			}
			if (WaterWaves._piStaticBatchRoot != null)
			{
				try
				{
					Transform transform = WaterWaves._piStaticBatchRoot.GetValue(r, null) as Transform;
					if (transform != null)
					{
						return transform;
					}
				}
				catch
				{
				}
			}
			return null;
		}

		// Token: 0x06000363 RID: 867 RVA: 0x0003389C File Offset: 0x00031A9C
		private static Bounds TransformedBounds(Vector3 mn, Vector3 mx, Matrix4x4 m)
		{
			bool flag = false;
			Bounds bounds = default(Bounds);
			for (int i = 0; i < 8; i++)
			{
				Vector3 vector = new Vector3(((i & 1) == 0) ? mn.x : mx.x, ((i & 2) == 0) ? mn.y : mx.y, ((i & 4) == 0) ? mn.z : mx.z);
				Vector3 vector2 = m.MultiplyPoint3x4(vector);
				if (!flag)
				{
					bounds = new Bounds(vector2, Vector3.zero);
					flag = true;
				}
				else
				{
					bounds.Encapsulate(vector2);
				}
			}
			return bounds;
		}

		// Token: 0x06000364 RID: 868 RVA: 0x00033928 File Offset: 0x00031B28
		private float LiquidFractionOfRange(byte[] idxRaw, bool i16, Mesh batch, int first, int count, Vector3[] pos, Matrix4x4 m)
		{
			int num = 0;
			int num2 = 0;
			for (int j = first; j < first + count; j++)
			{
				SubMeshDescriptor subMesh = batch.GetSubMesh(j);
				if (subMesh.indexCount >= 3)
				{
					int num3 = Mathf.Max(1, subMesh.indexCount / 400);
					for (int k = 0; k < subMesh.indexCount; k += num3)
					{
						int num4 = WaterWaves.ReadIndex(idxRaw, subMesh.indexStart + k, i16) + subMesh.baseVertex;
						if (num4 >= 0 && num4 < pos.Length)
						{
							num2++;
							if (this.InAnyVolume(m.MultiplyPoint3x4(pos[num4])))
							{
								num++;
							}
						}
					}
				}
			}
			if (num2 <= 0)
			{
				return 0f;
			}
			return (float)num / (float)num2;
		}

		// Token: 0x06000365 RID: 869 RVA: 0x000339E8 File Offset: 0x00031BE8
		private static bool TryGetStaticBatchRange(Renderer r, out int first, out int count)
		{
			first = 0;
			count = 0;
			if (!WaterWaves._sbResolved)
			{
				WaterWaves._sbResolved = true;
				try
				{
					WaterWaves._piStaticBatchInfo = typeof(Renderer).GetProperty("staticBatchInfo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (WaterWaves._piStaticBatchInfo != null)
					{
						Type propertyType = WaterWaves._piStaticBatchInfo.PropertyType;
						WaterWaves._fiFirstSubMesh = propertyType.GetField("firstSubMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
						WaterWaves._fiSubMeshCount = propertyType.GetField("subMeshCount", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					}
				}
				catch
				{
				}
			}
			if (WaterWaves._piStaticBatchInfo == null || WaterWaves._fiFirstSubMesh == null || WaterWaves._fiSubMeshCount == null)
			{
				return false;
			}
			bool flag;
			try
			{
				object value = WaterWaves._piStaticBatchInfo.GetValue(r, null);
				if (value == null)
				{
					flag = false;
				}
				else
				{
					first = Convert.ToInt32(WaterWaves._fiFirstSubMesh.GetValue(value));
					count = Convert.ToInt32(WaterWaves._fiSubMeshCount.GetValue(value));
					flag = count > 0;
				}
			}
			catch
			{
				flag = false;
			}
			return flag;
		}

		// Token: 0x06000366 RID: 870 RVA: 0x00033AF0 File Offset: 0x00031CF0
		private void Release(WaterWaves.Body b)
		{
			if (b.Owned)
			{
				try
				{
					if (b.Go != null)
					{
						UnityEngine.Object.Destroy(b.Go);
					}
				}
				catch
				{
				}
				try
				{
					if (b.Source != null)
					{
						UnityEngine.Object.Destroy(b.Source);
					}
				}
				catch
				{
				}
				try
				{
					if (b.Hidden != null)
					{
						b.Hidden.enabled = true;
						this._hiddenBatches.Remove(b.Hidden);
					}
				}
				catch
				{
				}
				b.Go = null;
				b.Source = null;
				b.Hidden = null;
			}
			else
			{
				try
				{
					if (b.Applied && b.Filter != null && b.Source != null)
					{
						b.Filter.sharedMesh = b.Source;
					}
				}
				catch
				{
				}
			}
			try
			{
				if (b.Adopted && b.Rend != null)
				{
					b.Rend.enabled = false;
				}
			}
			catch
			{
			}
			try
			{
				if (b.Generated != null)
				{
					UnityEngine.Object.Destroy(b.Generated);
				}
			}
			catch
			{
			}
			b.Generated = null;
			b.BaseLocal = null;
			b.Verts = null;
			b.Normals = null;
			b.WorldXZ = null;
			b.Amp = null;
			b.AmpCap = null;
			b.Wl = null;
			b.Local = null;
			b.Active = null;
			b.Applied = false;
		}

		// Token: 0x06000367 RID: 871 RVA: 0x00033CAC File Offset: 0x00031EAC
		private void ReleaseAll()
		{
			for (int i = 0; i < this._bodies.Count; i++)
			{
				this.Release(this._bodies[i]);
			}
			this._bodies.Clear();
			this.RestoreHiddenBatches();
		}

		// Token: 0x06000368 RID: 872 RVA: 0x00033CF4 File Offset: 0x00031EF4
		private void RestoreHiddenBatches()
		{
			for (int i = 0; i < this._hiddenBatches.Count; i++)
			{
				try
				{
					if (this._hiddenBatches[i] != null)
					{
						this._hiddenBatches[i].enabled = true;
					}
				}
				catch
				{
				}
			}
			this._hiddenBatches.Clear();
		}

		// Token: 0x06000369 RID: 873 RVA: 0x00033D60 File Offset: 0x00031F60
		private void ForgetAll()
		{
			for (int i = 0; i < this._bodies.Count; i++)
			{
				try
				{
					if (this._bodies[i].Generated != null)
					{
						UnityEngine.Object.Destroy(this._bodies[i].Generated);
					}
				}
				catch
				{
				}
				try
				{
					if (this._bodies[i].Owned && this._bodies[i].Go != null)
					{
						UnityEngine.Object.Destroy(this._bodies[i].Go);
					}
				}
				catch
				{
				}
				try
				{
					if (this._bodies[i].Owned && this._bodies[i].Source != null)
					{
						UnityEngine.Object.Destroy(this._bodies[i].Source);
					}
				}
				catch
				{
				}
			}
			this._bodies.Clear();
			this._scanFound.Clear();
			this._junctions.Clear();
			this._splashGo = null;
			this._splashPs = null;
			this._mistGo = null;
			this._mistPs = null;
			this._hiddenBatches.Clear();
			this._drawnWater.Clear();
			this._sourceWater.Clear();
		}

		// Token: 0x0600036A RID: 874 RVA: 0x00033ED4 File Offset: 0x000320D4
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.TeardownSplash();
			this.ReleaseAll();
			this._scanBuffer = null;
			this._scanCursor = -1;
		}

		// Token: 0x0600036B RID: 875 RVA: 0x00033F01 File Offset: 0x00032101
		private static string F2(float v)
		{
			return v.ToString("0.##", CultureInfo.InvariantCulture);
		}

		// Token: 0x0600036C RID: 876 RVA: 0x00033F14 File Offset: 0x00032114
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
			int num = name.LastIndexOf('.');
			string text = ((num >= 0) ? name.Substring(num + 1) : name);
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
					goto IL_00B3;
				}
				goto IL_0077;
				IL_00B3:
				j++;
				continue;
				IL_0077:
				if (array != null)
				{
					for (int k = 0; k < array.Length; k++)
					{
						if (array[k] != null && array[k].Name == text)
						{
							return array[k];
						}
					}
					goto IL_00B3;
				}
				goto IL_00B3;
			}
			return null;
		}

		// Token: 0x04000772 RID: 1906
		private const float ScanIntervalSeconds = 12f;

		// Token: 0x04000773 RID: 1907
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000774 RID: 1908
		private const int RenderersPerSlice = 256;

		// Token: 0x04000775 RID: 1909
		private const int MaxBodies = 12;

		// Token: 0x04000776 RID: 1910
		private const string OwnPrefix = "LumaWaves_";

		// Token: 0x04000777 RID: 1911
		private const int MaxSourceVerts = 40000;

		// Token: 0x04000778 RID: 1912
		private const int MaxSourceTris = 60000;

		// Token: 0x04000779 RID: 1913
		private const int VertBudgetDesktop = 26000;

		// Token: 0x0400077A RID: 1914
		private const int VertBudgetVr = 10000;

		// Token: 0x0400077B RID: 1915
		private const int MaxSubdiv = 128;

		// Token: 0x0400077C RID: 1916
		private const float TargetQuadsPerWave = 6f;

		// Token: 0x0400077D RID: 1917
		private const float DetailFullDistance = 18f;

		// Token: 0x0400077E RID: 1918
		private const float DetailFadeDistance = 32f;

		// Token: 0x0400077F RID: 1919
		private const float RebuildMoveDistance = 15f;

		// Token: 0x04000780 RID: 1920
		private const float RebuildMinInterval = 3f;

		// Token: 0x04000781 RID: 1921
		private const float TaperStart = 22f;

		// Token: 0x04000782 RID: 1922
		private const float TaperEnd = 32f;

		// Token: 0x04000783 RID: 1923
		private const int Octaves = 6;

		// Token: 0x04000784 RID: 1924
		private static readonly float[] OctLen = new float[] { 1f, 0.62f, 0.38f, 0.23f, 0.13f, 0.07f };

		// Token: 0x04000785 RID: 1925
		private static readonly float[] OctAmp = new float[] { 1f, 0.6f, 0.36f, 0.21f, 0.12f, 0.07f };

		// Token: 0x04000786 RID: 1926
		private static readonly float[] OctSpread = new float[] { 0f, -47f, 63f, -26f, 88f, -112f };

		// Token: 0x04000787 RID: 1927
		private static readonly float[] OctPhase = new float[] { 0f, 1.71f, 3.94f, 0.63f, 5.22f, 2.39f };

		// Token: 0x04000788 RID: 1928
		private static readonly float[] OctLenCap = new float[] { 0f, 0f, 0f, 6f, 2.4f, 1.1f };

		// Token: 0x04000789 RID: 1929
		private static readonly Vector2[] WaveDirs = new Vector2[6];

		// Token: 0x0400078A RID: 1930
		private readonly ManualLogSource _log;

		// Token: 0x0400078B RID: 1931
		private readonly RenderEngine _engine;

		// Token: 0x0400078C RID: 1932
		private bool _on;

		// Token: 0x0400078D RID: 1933
		private bool _vrAllowed = true;

		// Token: 0x0400078E RID: 1934
		private bool _desktopAllowed = true;

		// Token: 0x0400078F RID: 1935
		private float _height = 1f;

		// Token: 0x04000790 RID: 1936
		private float _sizeResponse = 1f;

		// Token: 0x04000791 RID: 1937
		private float _scale = 1f;

		// Token: 0x04000792 RID: 1938
		private float _speed = 1f;

		// Token: 0x04000793 RID: 1939
		private float _crest = 0.35f;

		// Token: 0x04000794 RID: 1940
		private float _detail = 0.5f;

		// Token: 0x04000795 RID: 1941
		private float _openness = 0.7f;

		// Token: 0x04000796 RID: 1942
		private float _splash = 1f;

		// Token: 0x04000797 RID: 1943
		private float _crestMean;

		// Token: 0x04000798 RID: 1944
		private readonly float[] _group = new float[8];

		// Token: 0x04000799 RID: 1945
		private const int LutSize = 1024;

		// Token: 0x0400079A RID: 1946
		private const float InvTwoPi = 0.15915494f;

		// Token: 0x0400079B RID: 1947
		private readonly float[] _lutH = new float[1025];

		// Token: 0x0400079C RID: 1948
		private readonly float[] _lutD = new float[1025];

		// Token: 0x0400079D RID: 1949
		private bool _rebuildMeshes;

		// Token: 0x0400079E RID: 1950
		private readonly List<WaterWaves.Body> _bodies = new List<WaterWaves.Body>(12);

		// Token: 0x0400079F RID: 1951
		private readonly List<WaterWaves.Body> _dropScratch = new List<WaterWaves.Body>(4);

		// Token: 0x040007A0 RID: 1952
		private readonly List<WaterWaves.Volume> _volumes = new List<WaterWaves.Volume>(16);

		// Token: 0x040007A1 RID: 1953
		private readonly List<Collider> _colScratch = new List<Collider>(8);

		// Token: 0x040007A2 RID: 1954
		private readonly List<Renderer> _hiddenBatches = new List<Renderer>(4);

		// Token: 0x040007A3 RID: 1955
		private readonly List<MeshRenderer> _drawnWater = new List<MeshRenderer>(8);

		// Token: 0x040007A4 RID: 1956
		private readonly List<MeshRenderer> _sourceWater = new List<MeshRenderer>(32);

		// Token: 0x040007A5 RID: 1957
		private readonly List<MeshRenderer> _inventory = new List<MeshRenderer>(24);

		// Token: 0x040007A6 RID: 1958
		private string _lastInventory = "";

		// Token: 0x040007A7 RID: 1959
		private int _pooledSeen;

		// Token: 0x040007A8 RID: 1960
		private Shader _waterShader;

		// Token: 0x040007A9 RID: 1961
		private Type _waterVolumeType;

		// Token: 0x040007AA RID: 1962
		private bool _volumeTypeResolved;

		// Token: 0x040007AB RID: 1963
		private float _nextScanAt;

		// Token: 0x040007AC RID: 1964
		private float _nextVolumeScanAt;

		// Token: 0x040007AD RID: 1965
		private bool _sceneDirty = true;

		// Token: 0x040007AE RID: 1966
		private float _sceneSettleAt;

		// Token: 0x040007AF RID: 1967
		private MeshRenderer[] _scanBuffer;

		// Token: 0x040007B0 RID: 1968
		private int _scanCursor = -1;

		// Token: 0x040007B1 RID: 1969
		private readonly List<WaterWaves.Body> _scanFound = new List<WaterWaves.Body>(12);

		// Token: 0x040007B2 RID: 1970
		private bool _loggedThisScene;

		// Token: 0x040007B3 RID: 1971
		private float[] _jx = new float[6];

		// Token: 0x040007B4 RID: 1972
		private float[] _jz = new float[6];

		// Token: 0x040007B5 RID: 1973
		private float[] _jStr = new float[6];

		// Token: 0x040007B6 RID: 1974
		private float[] _jRad = new float[6];

		// Token: 0x040007B7 RID: 1975
		private int _jCount;

		// Token: 0x040007B8 RID: 1976
		private PropertyInfo _piVolumeCurrent;

		// Token: 0x040007B9 RID: 1977
		private PropertyInfo _piCurrentSpeed;

		// Token: 0x040007BA RID: 1978
		private readonly Dictionary<long, int> _edgeCount = new Dictionary<long, int>(1024);

		// Token: 0x040007BB RID: 1979
		private readonly Dictionary<long, WaterWaves.EdgeKey> _edgeFirst = new Dictionary<long, WaterWaves.EdgeKey>(1024);

		// Token: 0x040007BC RID: 1980
		private readonly List<Vector4> _boundarySeg = new List<Vector4>(256);

		// Token: 0x040007BD RID: 1981
		private readonly Dictionary<long, float> _depthCache = new Dictionary<long, float>(512);

		// Token: 0x040007BE RID: 1982
		private readonly List<WaterWaves.Junction> _junctions = new List<WaterWaves.Junction>(8);

		// Token: 0x040007BF RID: 1983
		private const int MaxJunctions = 6;

		// Token: 0x040007C0 RID: 1984
		private const float SplashSightRange = 45f;

		// Token: 0x040007C1 RID: 1985
		private GameObject _splashGo;

		// Token: 0x040007C2 RID: 1986
		private GameObject _mistGo;

		// Token: 0x040007C3 RID: 1987
		private ParticleSystem _splashPs;

		// Token: 0x040007C4 RID: 1988
		private ParticleSystem _mistPs;

		// Token: 0x040007C5 RID: 1989
		private Material _splashMat;

		// Token: 0x040007C6 RID: 1990
		private Material _mistMat;

		// Token: 0x040007C7 RID: 1991
		private float _emitAccum;

		// Token: 0x040007C8 RID: 1992
		private float _mistAccum;

		// Token: 0x040007C9 RID: 1993
		private float _foamAccum;

		// Token: 0x040007CA RID: 1994
		private float _waveClock;

		// Token: 0x040007CB RID: 1995
		private bool _splashShaderMissingLogged;

		// Token: 0x040007CC RID: 1996
		private float _lastLiquidFrac = -1f;

		// Token: 0x040007CD RID: 1997
		private static bool _sbResolved;

		// Token: 0x040007CE RID: 1998
		private static PropertyInfo _piStaticBatchInfo;

		// Token: 0x040007CF RID: 1999
		private static FieldInfo _fiFirstSubMesh;

		// Token: 0x040007D0 RID: 2000
		private static FieldInfo _fiSubMeshCount;

		// Token: 0x040007D1 RID: 2001
		private static bool _sbRootResolved;

		// Token: 0x040007D2 RID: 2002
		private static PropertyInfo _piStaticBatchRoot;

		// Token: 0x040007D3 RID: 2003
		private Transform _recoveredSpaceRoot;

		// Token: 0x040007D4 RID: 2004
		private string _recoveredSpaceName = "?";

		// Token: 0x02000058 RID: 88
		private struct Volume
		{
			// Token: 0x040007D5 RID: 2005
			public Bounds B;

			// Token: 0x040007D6 RID: 2006
			public float Area;

			// Token: 0x040007D7 RID: 2007
			public float Amp;

			// Token: 0x040007D8 RID: 2008
			public float Wl;

			// Token: 0x040007D9 RID: 2009
			public float AmpCap;

			// Token: 0x040007DA RID: 2010
			public float Extent;

			// Token: 0x040007DB RID: 2011
			public float Open;

			// Token: 0x040007DC RID: 2012
			public float Flow;
		}

		// Token: 0x02000059 RID: 89
		private sealed class Body
		{
			// Token: 0x040007DD RID: 2013
			public MeshFilter Filter;

			// Token: 0x040007DE RID: 2014
			public Renderer Rend;

			// Token: 0x040007DF RID: 2015
			public Transform Tf;

			// Token: 0x040007E0 RID: 2016
			public Mesh Source;

			// Token: 0x040007E1 RID: 2017
			public Mesh Generated;

			// Token: 0x040007E2 RID: 2018
			public Vector3[] BaseLocal;

			// Token: 0x040007E3 RID: 2019
			public Vector3[] Verts;

			// Token: 0x040007E4 RID: 2020
			public Vector3[] Normals;

			// Token: 0x040007E5 RID: 2021
			public Vector2[] WorldXZ;

			// Token: 0x040007E6 RID: 2022
			public float[] Amp;

			// Token: 0x040007E7 RID: 2023
			public float[] AmpCap;

			// Token: 0x040007E8 RID: 2024
			public float[] Wl;

			// Token: 0x040007E9 RID: 2025
			public float[] Local;

			// Token: 0x040007EA RID: 2026
			public int[] Active;

			// Token: 0x040007EB RID: 2027
			public float QuadSize = 1f;

			// Token: 0x040007EC RID: 2028
			public float LocalPerWorldY = 1f;

			// Token: 0x040007ED RID: 2029
			public float MaxAmp;

			// Token: 0x040007EE RID: 2030
			public Vector3 BuiltAtCam;

			// Token: 0x040007EF RID: 2031
			public float NextRebuildAt;

			// Token: 0x040007F0 RID: 2032
			public bool Applied;

			// Token: 0x040007F1 RID: 2033
			public bool Adopted;

			// Token: 0x040007F2 RID: 2034
			public bool Owned;

			// Token: 0x040007F3 RID: 2035
			public GameObject Go;

			// Token: 0x040007F4 RID: 2036
			public Renderer Hidden;

			// Token: 0x040007F5 RID: 2037
			public int InVolume;
		}

		// Token: 0x0200005A RID: 90
		private struct EdgeKey
		{
			// Token: 0x17000071 RID: 113
			// (get) Token: 0x0600036E RID: 878 RVA: 0x0003402E File Offset: 0x0003222E
			public long Pair
			{
				get
				{
					return this.A * 31L + this.B;
				}
			}

			// Token: 0x040007F6 RID: 2038
			public long A;

			// Token: 0x040007F7 RID: 2039
			public long B;

			// Token: 0x040007F8 RID: 2040
			public int I0;

			// Token: 0x040007F9 RID: 2041
			public int I1;
		}

		// Token: 0x0200005B RID: 91
		private struct Junction
		{
			// Token: 0x040007FA RID: 2042
			public Vector3 P;

			// Token: 0x040007FB RID: 2043
			public float Radius;

			// Token: 0x040007FC RID: 2044
			public float Strength;

			// Token: 0x040007FD RID: 2045
			public float Drop;
		}
	}
}
