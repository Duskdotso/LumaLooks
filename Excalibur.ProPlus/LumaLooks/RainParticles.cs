using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

namespace LumaLooks
{
	// Token: 0x02000028 RID: 40
	internal sealed class RainParticles
	{
		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600017F RID: 383 RVA: 0x000164B7 File Offset: 0x000146B7
		// (set) Token: 0x06000180 RID: 384 RVA: 0x000164BE File Offset: 0x000146BE
		public static bool RainOn { get; private set; }

		// Token: 0x06000181 RID: 385 RVA: 0x000164C8 File Offset: 0x000146C8
		public RainParticles(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x00016544 File Offset: 0x00014744
		public void Configure(bool want, bool vrAllowed, bool desktopAllowed, float intensity, float fallSpeed, float dropSize, float wind, bool storm, float boltIntensity, float boltSpeed, float boltRandom)
		{
			this._storm = storm;
			this._boltIntensity = Mathf.Clamp(boltIntensity, 0f, 2f);
			this._boltSpeed = Mathf.Clamp(boltSpeed, 0.1f, 3f);
			this._boltRandom = Mathf.Clamp01(boltRandom);
			this._want = want;
			RainParticles.RainOn = want;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._intensity = Mathf.Clamp(intensity, 0f, 5f);
			this._fallSpeed = Mathf.Clamp(fallSpeed, 0.1f, 4f);
			this._dropSize = Mathf.Clamp(dropSize, 0.1f, 4f);
			this._wind = Mathf.Clamp01(wind);
			this._appliedRate = -1f;
			if (this._ps != null)
			{
				this.ApplyShape();
			}
		}

		// Token: 0x06000183 RID: 387 RVA: 0x00016620 File Offset: 0x00014820
		public void Tick()
		{
			try
			{
				this.UpdateLightning();
				bool flag = false;
				try
				{
					flag = XRSettings.isDeviceActive;
				}
				catch
				{
				}
				if (!this._want || !(flag ? this._vrAllowed : this._desktopAllowed))
				{
					this.Teardown();
				}
				else
				{
					Camera main = Camera.main;
					if (main == null)
					{
						this.Teardown();
					}
					else if (this.EnsureRig())
					{
						this._go.transform.position = main.transform.position + Vector3.up * 24f;
						float num = 0f;
						try
						{
							num = Mathf.Clamp01(RainSensor.RainFactor);
						}
						catch
						{
						}
						float num2 = 122500f;
						float num3 = ((num <= 0.001f) ? 0f : (0.12f * num2 * this._intensity * num));
						if (!Mathf.Approximately(num3, this._appliedRate))
						{
							this._appliedRate = num3;
							ParticleSystem.EmissionModule emission = this._ps.emission;
							emission.rateOverTimeMultiplier = num3;
							if (num3 <= 0f)
							{
								if (this._ps.isPlaying)
								{
									this._ps.Stop(true, (ParticleSystemStopBehavior)1);
								}
							}
							else if (!this._ps.isPlaying)
							{
								this._ps.Play();
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("LUMARAIN: tick failed - " + ex.Message);
			}
		}

		// Token: 0x06000184 RID: 388 RVA: 0x000167D4 File Offset: 0x000149D4
		public void SyncToCamera(Camera cam)
		{
			if (this._go == null || cam == null)
			{
				return;
			}
			try
			{
				Vector3 position = this._go.transform.position;
				Vector3 position2 = cam.transform.position;
				float num = 147f;
				if (Mathf.Abs(position2.x - position.x) > num || Mathf.Abs(position2.z - position.z) > num || Mathf.Abs(position2.y - (position.y - 24f)) > 175f)
				{
					this._go.transform.position = position2 + Vector3.up * 24f;
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000185 RID: 389 RVA: 0x000168A0 File Offset: 0x00014AA0
		public void Dispose()
		{
			this.Teardown();
		}

		// Token: 0x06000186 RID: 390 RVA: 0x000168A8 File Offset: 0x00014AA8
		private void UpdateLightning()
		{
			float num = 0f;
			try
			{
				num = Mathf.Clamp01(RainSensor.RainFactor);
			}
			catch
			{
			}
			if (!this._want || !this._storm || this._boltIntensity <= 0.001f || num <= 0.001f)
			{
				if (this._lightningPushed)
				{
					this._flash = 0f;
					this._flickerLeft = 0;
					Shader.SetGlobalFloat(RainParticles.LightningId, 0f);
					this._lightningPushed = false;
				}
				return;
			}
			float unscaledTime = Time.unscaledTime;
			float num2 = Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
			if (this._nextStrikeAt <= 0f)
			{
				this._nextStrikeAt = unscaledTime + 4.5f;
			}
			if (unscaledTime >= this._nextStrikeAt)
			{
				float num3 = Mathf.Lerp(1f, Random.Range(0.2f, 1.9f), this._boltRandom);
				this._nextStrikeAt = unscaledTime + Mathf.Max(0.35f, 9f / this._boltSpeed * num3);
				this._strikeStrength = this._boltIntensity * Mathf.Lerp(1f, Random.Range(0.35f, 1.25f), this._boltRandom);
				this._flash = this._strikeStrength;
				this._flickerLeft = ((Random.value < 0.65f) ? Random.Range(1, 3) : 0);
				this._nextFlickerAt = unscaledTime + Random.Range(0.05f, 0.13f);
			}
			else if (this._flickerLeft > 0 && unscaledTime >= this._nextFlickerAt)
			{
				this._flickerLeft--;
				this._flash = Mathf.Max(this._flash, this._strikeStrength * Random.Range(0.35f, 0.7f));
				this._nextFlickerAt = unscaledTime + Random.Range(0.04f, 0.11f);
			}
			this._flash *= Mathf.Exp(-num2 / 0.075f);
			if (this._flash < 0.0001f)
			{
				this._flash = 0f;
			}
			Shader.SetGlobalFloat(RainParticles.LightningId, this._flash);
			this._lightningPushed = true;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x00016AC8 File Offset: 0x00014CC8
		private bool EnsureRig()
		{
			if (this._ps != null)
			{
				return true;
			}
			if (!this.EnsureMaterial())
			{
				return false;
			}
			this._go = new GameObject("LumaLooks_Rain");
			this._ps = this._go.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = this._ps.main;
			main.loop = true;
			main.playOnAwake = false;
			main.simulationSpace = (ParticleSystemSimulationSpace)1;
			main.startLifetime = 3.2f;
			main.maxParticles = 48000;
			main.startColor = new Color(0.78f, 0.84f, 0.95f, 0.55f);
			ParticleSystem.InheritVelocityModule inheritVelocity = this._ps.inheritVelocity;
			inheritVelocity.enabled = false;
			ParticleSystem.ExternalForcesModule externalForces = this._ps.externalForces;
			externalForces.enabled = false;
			ParticleSystem.CollisionModule collision = this._ps.collision;
			collision.enabled = false;
			ParticleSystem.EmissionModule emission = this._ps.emission;
			emission.enabled = true;
			emission.rateOverTime = new ParticleSystem.MinMaxCurve(1f);
			ParticleSystem.ShapeModule shape = this._ps.shape;
			shape.enabled = true;
			shape.shapeType = (ParticleSystemShapeType)5;
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = this._ps.colorOverLifetime;
			colorOverLifetime.enabled = true;
			colorOverLifetime.color = new ParticleSystem.MinMaxGradient(new Gradient
			{
				colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(Color.white, 0f),
					new GradientColorKey(Color.white, 1f)
				},
				alphaKeys = new GradientAlphaKey[]
				{
					new GradientAlphaKey(0f, 0f),
					new GradientAlphaKey(1f, 0.06f),
					new GradientAlphaKey(1f, 0.82f),
					new GradientAlphaKey(0f, 1f)
				}
			});
			this._psr = this._go.GetComponent<ParticleSystemRenderer>();
			this._psr.renderMode = (ParticleSystemRenderMode)1;
			this._psr.velocityScale = 0.06f;
			this._psr.lengthScale = 2.2f;
			this._psr.alignment = 0;
			this._psr.sharedMaterial = this._mat;
			this._psr.shadowCastingMode = 0;
			this._psr.receiveShadows = false;
			this._psr.sortingFudge = 0f;
			this.ApplyShape();
			this._ps.Play();
			this._log.LogInfo("LUMARAIN: rain rig created.");
			return true;
		}

		// Token: 0x06000188 RID: 392 RVA: 0x00016D64 File Offset: 0x00014F64
		private void ApplyShape()
		{
			ParticleSystem.MainModule main = this._ps.main;
			main.startSpeed = new ParticleSystem.MinMaxCurve(7.5f * this._fallSpeed, 10.5f * this._fallSpeed);
			main.startSize = new ParticleSystem.MinMaxCurve(0.045f * this._dropSize, 0.075f * this._dropSize);
			main.gravityModifier = 0.45f;
			ParticleSystem.ShapeModule shape = this._ps.shape;
			shape.scale = new Vector3(350f, 8f, 350f);
			shape.rotation = new Vector3(90f - this._wind * 22f, 0f, 0f);
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00016E28 File Offset: 0x00015028
		private bool EnsureMaterial()
		{
			if (this._mat != null)
			{
				return true;
			}
			Shader shader = ((this._engine != null) ? this._engine.GetShader("LumaLooks/RainParticle") : null);
			if (shader == null)
			{
				this._log.LogWarning("LUMARAIN: 'LumaLooks/RainParticle' missing from the bundle - no rain.");
				return false;
			}
			this._mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this._mat.SetFloat("_LumaRainMode", 0f);
			this._mat.SetFloat("_LumaRainOpacity", 0.55f);
			this._mat.SetFloat("_LumaRainRoofGate", 1f);
			return true;
		}

		// Token: 0x0600018A RID: 394 RVA: 0x00016ED0 File Offset: 0x000150D0
		private void Teardown()
		{
			RainParticles.RainOn = false;
			if (this._go != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._go);
				}
				catch
				{
				}
			}
			this._go = null;
			this._ps = null;
			this._psr = null;
			if (this._lightningPushed)
			{
				try
				{
					Shader.SetGlobalFloat(RainParticles.LightningId, 0f);
				}
				catch
				{
				}
			}
			this._flash = 0f;
			this._flickerLeft = 0;
			this._lightningPushed = false;
			if (this._mat != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._mat);
				}
				catch
				{
				}
				this._mat = null;
			}
			this._appliedRate = -1f;
		}

		// Token: 0x0400034A RID: 842
		private const string ShaderName = "LumaLooks/RainParticle";

		// Token: 0x0400034B RID: 843
		private const string GoName = "LumaLooks_Rain";

		// Token: 0x0400034D RID: 845
		private const float BoxWidth = 350f;

		// Token: 0x0400034E RID: 846
		private const float BoxHeight = 8f;

		// Token: 0x0400034F RID: 847
		private const float SpawnAbove = 24f;

		// Token: 0x04000350 RID: 848
		private const float Lifetime = 3.2f;

		// Token: 0x04000351 RID: 849
		private const int MaxDrops = 48000;

		// Token: 0x04000352 RID: 850
		private const float DropsPerSqM = 0.12f;

		// Token: 0x04000353 RID: 851
		private readonly ManualLogSource _log;

		// Token: 0x04000354 RID: 852
		private readonly RenderEngine _engine;

		// Token: 0x04000355 RID: 853
		private GameObject _go;

		// Token: 0x04000356 RID: 854
		private ParticleSystem _ps;

		// Token: 0x04000357 RID: 855
		private ParticleSystemRenderer _psr;

		// Token: 0x04000358 RID: 856
		private Material _mat;

		// Token: 0x04000359 RID: 857
		private bool _want;

		// Token: 0x0400035A RID: 858
		private bool _vrAllowed;

		// Token: 0x0400035B RID: 859
		private bool _desktopAllowed;

		// Token: 0x0400035C RID: 860
		private float _intensity = 1f;

		// Token: 0x0400035D RID: 861
		private float _fallSpeed = 1f;

		// Token: 0x0400035E RID: 862
		private float _dropSize = 1f;

		// Token: 0x0400035F RID: 863
		private float _wind = 0.2f;

		// Token: 0x04000360 RID: 864
		private float _appliedRate = -1f;

		// Token: 0x04000361 RID: 865
		private static readonly int LightningId = Shader.PropertyToID("_LumaLightning");

		// Token: 0x04000362 RID: 866
		private const float StrikeBaseSeconds = 9f;

		// Token: 0x04000363 RID: 867
		private bool _storm;

		// Token: 0x04000364 RID: 868
		private float _boltIntensity = 1f;

		// Token: 0x04000365 RID: 869
		private float _boltSpeed = 1f;

		// Token: 0x04000366 RID: 870
		private float _boltRandom = 0.5f;

		// Token: 0x04000367 RID: 871
		private float _flash;

		// Token: 0x04000368 RID: 872
		private float _nextStrikeAt;

		// Token: 0x04000369 RID: 873
		private int _flickerLeft;

		// Token: 0x0400036A RID: 874
		private float _nextFlickerAt;

		// Token: 0x0400036B RID: 875
		private float _strikeStrength;

		// Token: 0x0400036C RID: 876
		private bool _lightningPushed;
	}
}
