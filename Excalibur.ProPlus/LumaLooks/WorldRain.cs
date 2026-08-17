using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200005E RID: 94
	internal sealed class WorldRain
	{
		// Token: 0x060003AA RID: 938 RVA: 0x00035998 File Offset: 0x00033B98
		public WorldRain(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x060003AB RID: 939 RVA: 0x00035A14 File Offset: 0x00033C14
		public void Configure(bool enabled, float rainVisibility, bool vrAllowed, bool desktopAllowed, bool vrBalanced, float wind, float splashes)
		{
			this._want = enabled;
			this._rainVisibility = Mathf.Clamp01(rainVisibility);
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._vrBalanced = vrBalanced;
			this._wind = Mathf.Clamp01(wind);
			this._splashes = Mathf.Clamp01(splashes);
			this.PushOpacity();
		}

		// Token: 0x060003AC RID: 940 RVA: 0x00035A6C File Offset: 0x00033C6C
		private void PushOpacity()
		{
			if (this._mat != null)
			{
				this._mat.SetFloat(ShaderIds.RainOpacity, 0.4f + 0.6f * this._rainVisibility);
			}
			if (this._splashMat != null)
			{
				this._splashMat.SetFloat(ShaderIds.RainOpacity, Mathf.Max(0.55f, 0.4f + 0.6f * this._rainVisibility));
			}
		}

		// Token: 0x060003AD RID: 941 RVA: 0x00035AE4 File Offset: 0x00033CE4
		private Vector3 WindVelocity(float now, out float gust)
		{
			gust = 0f;
			if (this._wind <= 0.0001f)
			{
				return Vector3.zero;
			}
			float num = Mathf.PerlinNoise(now * 0.045f, 11.7f) * 3.1415927f * 2f;
			gust = Mathf.Clamp01(Mathf.PerlinNoise(now * 0.11f, 41.3f) * 1.35f - 0.12f);
			float num2 = 5.5f * this._wind * (0.35f + 0.65f * gust);
			return new Vector3(Mathf.Cos(num) * num2, 0f, Mathf.Sin(num) * num2);
		}

		// Token: 0x060003AE RID: 942 RVA: 0x00035B84 File Offset: 0x00033D84
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == LoadSceneMode.Single)
			{
				this.InvalidateGrid();
				return;
			}
			this._go = null;
			this._ps = null;
			this._psr = null;
			this._splashGo = null;
			this._splashPs = null;
			this._splashPsr = null;
			this.InvalidateGrid();
			this._emitAcc = 0f;
			this._splashAcc = 0f;
			this._settleUntil = -1f;
		}

		// Token: 0x060003AF RID: 943 RVA: 0x00035BF0 File Offset: 0x00033DF0
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
				bool flag2 = this._want && (flag ? this._vrAllowed : this._desktopAllowed);
				float rainFactor = RainSensor.RainFactor;
				float num = 0f;
				Vector3 vector = Vector3.zero;
				if (flag2)
				{
					vector = this.WindVelocity(realtimeSinceStartup, out num);
				}
				Shader.SetGlobalVector(ShaderIds.WindGlobal, new Vector4(vector.x, vector.y, vector.z, num));
				bool flag3 = flag && this._vrBalanced;
				float num2 = 1f + (num - 0.5f) * 2f * 0.45f * this._wind;
				float num3 = (flag2 ? (rainFactor * this._rainVisibility * 1600f * (flag3 ? 0.5f : 1f) * num2) : 0f);
				if (!flag2 || rainFactor <= 0.001f || this._rainVisibility <= 0.001f)
				{
					if (this._wasActive)
					{
						this._wasActive = false;
						this._emitAcc = 0f;
						this._splashAcc = 0f;
						this.InvalidateGrid();
					}
					if (!flag2)
					{
						this.Teardown();
					}
					else if (this._go != null && this._ps != null && this._ps.particleCount == 0)
					{
						this.Teardown();
					}
				}
				else
				{
					this._wasActive = true;
					if (realtimeSinceStartup >= this._settleUntil)
					{
						Camera main = Camera.main;
						if (!(main == null))
						{
							Vector3 position = main.transform.position;
							if (this.EnsureSystem(flag3))
							{
								this.RefreshCoverage(position);
								this.EmitFromOpenCells(position, num3, vector);
								this.EmitSplashes(position, rainFactor * this._splashes * 190f * (flag3 ? 0.5f : 1f));
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("WorldRain tick skipped: " + ex.Message);
			}
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x00035E48 File Offset: 0x00034048
		private void InvalidateGrid()
		{
			for (int i = 0; i < 81; i++)
			{
				this._cells[i].Valid = false;
			}
			this._cursor = 0;
			this._gridWarm = false;
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x00035E84 File Offset: 0x00034084
		private void RefreshCoverage(Vector3 camPos)
		{
			int num = (this._gridWarm ? 2 : 9);
			for (int i = 0; i < num; i++)
			{
				int cursor = this._cursor;
				this._cursor = (this._cursor + 1) % 81;
				if (this._cursor == 0)
				{
					this._gridWarm = true;
				}
				int num2 = cursor % 9 - 4;
				int num3 = cursor / 9 - 4;
				float num4 = camPos.x + (float)num2 * 2.6666667f;
				float num5 = camPos.z + (float)num3 * 2.6666667f;
				Vector3 vector = new Vector3(num4, camPos.y + 1f, num5);
				RaycastHit raycastHit = default;
				bool flag = Physics.Raycast(vector, Vector3.up, out raycastHit, 90f, -5, (QueryTriggerInteraction)1);
				this._cells[cursor].X = num4;
				this._cells[cursor].Z = num5;
				this._cells[cursor].Open = !flag;
				this._cells[cursor].Valid = true;
				if (this._splashes > 0.0001f)
				{
					RaycastHit raycastHit2 = default;
					bool flag2 = Physics.Raycast(vector, Vector3.down, out raycastHit2, 12f, -5, (QueryTriggerInteraction)1);
					this._cells[cursor].HasGround = flag2;
					this._cells[cursor].GroundY = (flag2 ? raycastHit2.point.y : 0f);
				}
				else
				{
					this._cells[cursor].HasGround = false;
				}
			}
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x00035FFC File Offset: 0x000341FC
		private void EmitFromOpenCells(Vector3 camPos, float rate, Vector3 wind)
		{
			this._emitAcc += rate * Time.unscaledDeltaTime;
			int num = (int)this._emitAcc;
			if (num <= 0)
			{
				return;
			}
			this._emitAcc -= (float)num;
			if (num > 240)
			{
				num = 240;
			}
			float num2 = 1.3333334f;
			for (int i = 0; i < num; i++)
			{
				ref WorldRain.Cell ptr = ref this._cells[UnityEngine.Random.Range(0, 81)];
				if (ptr.Valid && ptr.Open && Mathf.Abs(ptr.X - camPos.x) <= 14.666667f && Mathf.Abs(ptr.Z - camPos.z) <= 14.666667f)
				{
					float num3 = -1.3333334f;
					this._emit.position = new Vector3(ptr.X + UnityEngine.Random.Range(-num2, num2) + wind.x * num3, camPos.y + 12f + UnityEngine.Random.Range(-0.5f, 0.5f), ptr.Z + UnityEngine.Random.Range(-num2, num2) + wind.z * num3);
					this._emit.velocity = new Vector3(wind.x + UnityEngine.Random.Range(-0.2f, 0.2f), -9f * UnityEngine.Random.Range(0.9f, 1.1f), wind.z + UnityEngine.Random.Range(-0.2f, 0.2f));
					this._ps.Emit(this._emit, 1);
				}
			}
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x0003618C File Offset: 0x0003438C
		private void EmitSplashes(Vector3 camPos, float rate)
		{
			if (this._splashes <= 0.0001f || rate <= 0f)
			{
				this._splashAcc = 0f;
				return;
			}
			this._splashAcc += rate * Time.unscaledDeltaTime;
			int num = (int)this._splashAcc;
			if (num <= 0)
			{
				return;
			}
			this._splashAcc -= (float)num;
			if (num > 40)
			{
				num = 40;
			}
			if (!this.EnsureSplash())
			{
				return;
			}
			float num2 = 1.3333334f;
			float num3 = 81f;
			for (int i = 0; i < num; i++)
			{
				ref WorldRain.Cell ptr = ref this._cells[UnityEngine.Random.Range(0, 81)];
				if (ptr.Valid && ptr.Open && ptr.HasGround)
				{
					float num4 = ptr.X - camPos.x;
					float num5 = ptr.Z - camPos.z;
					if (num4 * num4 + num5 * num5 <= num3)
					{
						Vector3 vector = new Vector3(ptr.X + UnityEngine.Random.Range(-num2, num2), ptr.GroundY + 0.015f, ptr.Z + UnityEngine.Random.Range(-num2, num2));
						this._splashEmit.position = vector;
						this._splashEmit.velocity = Vector3.zero;
						this._splashPs.Emit(this._splashEmit, 1);
						if (UnityEngine.Random.value < 0.55f)
						{
							float num6 = UnityEngine.Random.value * 3.1415927f * 2f;
							float num7 = UnityEngine.Random.Range(0.15f, 0.5f);
							this._mistEmit.position = vector;
							this._mistEmit.velocity = new Vector3(Mathf.Cos(num6) * num7, UnityEngine.Random.Range(0.55f, 1.35f), Mathf.Sin(num6) * num7);
							this._ps.Emit(this._mistEmit, 1);
						}
					}
				}
			}
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x00036364 File Offset: 0x00034564
		private bool EnsureMaterial()
		{
			if (this._mat != null)
			{
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/RainParticle") : null);
			if (shader == null)
			{
				if (!this._shaderMissingLogged)
				{
					this._shaderMissingLogged = true;
					this._log.LogWarning("WorldRain: shader 'LumaLooks/RainParticle' not in the bundle — world rain disabled.");
				}
				return false;
			}
			this._mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this._mat.SetFloat(ShaderIds.RainMode, 0f);
			this.PushOpacity();
			return true;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x000363F4 File Offset: 0x000345F4
		private bool EnsureSplashMaterial()
		{
			if (this._splashMat != null)
			{
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/RainParticle") : null);
			if (shader == null)
			{
				return false;
			}
			this._splashMat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this._splashMat.SetFloat(ShaderIds.RainMode, 1f);
			this.PushOpacity();
			return true;
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x00036464 File Offset: 0x00034664
		private bool EnsureSystem(bool capHalved)
		{
			if (this._ps != null)
			{
				if (capHalved != this._capHalved)
				{
					this._capHalved = capHalved;
					ParticleSystem.MainModule mainModule = this._ps.main;
					mainModule.maxParticles = (capHalved ? 2048 : 4096);
				}
				return true;
			}
			if (!this.EnsureMaterial())
			{
				return false;
			}
			this._go = new GameObject("LumaLooks_WorldRain");
			this._go.transform.position = Vector3.zero;
			this._ps = this._go.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = this._ps.main;
			main.loop = true;
			main.playOnAwake = false;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.startLifetime = 2.6f;
			main.startSpeed = 0f;
			main.startSize = new ParticleSystem.MinMaxCurve(0.009f, 0.016f);
			main.startColor = Color.white;
			main.gravityModifier = 0f;
			this._capHalved = capHalved;
			main.maxParticles = (capHalved ? 2048 : 4096);
			ParticleSystem.EmissionModule emission = this._ps.emission;
			emission.enabled = false;
			ParticleSystem.ShapeModule shape = this._ps.shape;
			shape.enabled = false;
			ParticleSystem.InheritVelocityModule inheritVelocity = this._ps.inheritVelocity;
			inheritVelocity.enabled = false;
			ParticleSystem.ExternalForcesModule externalForces = this._ps.externalForces;
			externalForces.enabled = false;
			ParticleSystem.CollisionModule collision = this._ps.collision;
			collision.enabled = false;
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
			this._psr.renderMode = ParticleSystemRenderMode.Stretch;
			this._psr.lengthScale = 14f;
			this._psr.velocityScale = 0f;
			this._psr.cameraVelocityScale = 0f;
			this._psr.shadowCastingMode = 0;
			this._psr.receiveShadows = false;
			this._psr.sharedMaterial = this._mat;
			this._ps.Play();
			if (!this._mistPrimed)
			{
				this._mistPrimed = true;
				this._mistEmit.startLifetime = 0.5f;
				this._mistEmit.startSize = 0.014f;
			}
			if (!this._loggedActive)
			{
				this._loggedActive = true;
				this._log.LogInfo("WorldRain: world-space rain system online (LumaLooks/RainParticle).");
			}
			return true;
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x00036790 File Offset: 0x00034990
		private bool EnsureSplash()
		{
			if (this._splashPs != null)
			{
				return true;
			}
			if (!this.EnsureSplashMaterial())
			{
				return false;
			}
			this._splashGo = new GameObject("LumaLooks_RainSplash");
			this._splashGo.transform.position = Vector3.zero;
			this._splashPs = this._splashGo.AddComponent<ParticleSystem>();
			ParticleSystem.MainModule main = this._splashPs.main;
			main.loop = true;
			main.playOnAwake = false;
			main.simulationSpace = ParticleSystemSimulationSpace.World;
			main.startLifetime = 0.42f;
			main.startSpeed = 0f;
			main.startSize = 0.05f;
			main.startColor = Color.white;
			main.gravityModifier = 0f;
			main.maxParticles = 512;
			ParticleSystem.EmissionModule emission2 = this._splashPs.emission;
			emission2.enabled = false;
			ParticleSystem.ShapeModule shape2 = this._splashPs.shape;
			shape2.enabled = false;
			ParticleSystem.InheritVelocityModule inheritVelocity2 = this._splashPs.inheritVelocity;
			inheritVelocity2.enabled = false;
			ParticleSystem.ExternalForcesModule externalForces2 = this._splashPs.externalForces;
			externalForces2.enabled = false;
			ParticleSystem.CollisionModule collision2 = this._splashPs.collision;
			collision2.enabled = false;
			ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = this._splashPs.sizeOverLifetime;
			sizeOverLifetime.enabled = true;
			AnimationCurve animationCurve = new AnimationCurve();
			animationCurve.AddKey(0f, 0.1923077f);
			animationCurve.AddKey(1f, 1f);
			sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(5.2f, animationCurve);
			ParticleSystem.ColorOverLifetimeModule colorOverLifetime = this._splashPs.colorOverLifetime;
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
					new GradientAlphaKey(1f, 0.12f),
					new GradientAlphaKey(0.45f, 0.45f),
					new GradientAlphaKey(0f, 1f)
				}
			});
			this._splashPsr = this._splashGo.GetComponent<ParticleSystemRenderer>();
			this._splashPsr.renderMode = ParticleSystemRenderMode.Billboard;
			this._splashPsr.shadowCastingMode = 0;
			this._splashPsr.receiveShadows = false;
			this._splashPsr.sharedMaterial = this._splashMat;
			this._splashPs.Play();
			return true;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x00036A48 File Offset: 0x00034C48
		private void Teardown()
		{
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
			if (this._splashGo != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._splashGo);
				}
				catch
				{
				}
			}
			this._splashGo = null;
			this._splashPs = null;
			this._splashPsr = null;
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x00036AD8 File Offset: 0x00034CD8
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.Teardown();
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
			if (this._splashMat != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._splashMat);
				}
				catch
				{
				}
				this._splashMat = null;
			}
			try
			{
				Shader.SetGlobalVector(ShaderIds.WindGlobal, Vector4.zero);
			}
			catch
			{
			}
		}

		// Token: 0x0400084D RID: 2125
		private const string RainShaderName = "LumaLooks/RainParticle";

		// Token: 0x0400084E RID: 2126
		private const float BoxHalf = 12f;

		// Token: 0x0400084F RID: 2127
		private const float EmitterHeight = 12f;

		// Token: 0x04000850 RID: 2128
		private const float EmitterThickness = 0.5f;

		// Token: 0x04000851 RID: 2129
		private const int GridN = 9;

		// Token: 0x04000852 RID: 2130
		private const int CellCount = 81;

		// Token: 0x04000853 RID: 2131
		private const float CellSize = 2.6666667f;

		// Token: 0x04000854 RID: 2132
		private const int RaysPerTickSteady = 2;

		// Token: 0x04000855 RID: 2133
		private const int RaysPerTickWarmup = 9;

		// Token: 0x04000856 RID: 2134
		private const float RayStartAbove = 1f;

		// Token: 0x04000857 RID: 2135
		private const float RayUpDistance = 90f;

		// Token: 0x04000858 RID: 2136
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000859 RID: 2137
		private const float FallSpeed = 9f;

		// Token: 0x0400085A RID: 2138
		private const float XZJitterMax = 0.2f;

		// Token: 0x0400085B RID: 2139
		private const float ParticleLifetime = 2.6f;

		// Token: 0x0400085C RID: 2140
		private const float StreakWidthMin = 0.009f;

		// Token: 0x0400085D RID: 2141
		private const float StreakWidthMax = 0.016f;

		// Token: 0x0400085E RID: 2142
		private const float StreakLengthScale = 14f;

		// Token: 0x0400085F RID: 2143
		private const float WindSpeedMax = 5.5f;

		// Token: 0x04000860 RID: 2144
		private const float WindTurnRate = 0.045f;

		// Token: 0x04000861 RID: 2145
		private const float WindGustRate = 0.11f;

		// Token: 0x04000862 RID: 2146
		private const float GustRateSwing = 0.45f;

		// Token: 0x04000863 RID: 2147
		private const float SplashRadius = 9f;

		// Token: 0x04000864 RID: 2148
		private const float SplashRatePerSecond = 190f;

		// Token: 0x04000865 RID: 2149
		private const int MaxSplashPerTick = 40;

		// Token: 0x04000866 RID: 2150
		private const int MaxSplashParticles = 512;

		// Token: 0x04000867 RID: 2151
		private const float SplashLifetime = 0.42f;

		// Token: 0x04000868 RID: 2152
		private const float SplashStartSize = 0.05f;

		// Token: 0x04000869 RID: 2153
		private const float SplashEndSize = 0.26f;

		// Token: 0x0400086A RID: 2154
		private const float SplashLift = 0.015f;

		// Token: 0x0400086B RID: 2155
		private const float MistChance = 0.55f;

		// Token: 0x0400086C RID: 2156
		private const float MistSpeedMin = 0.55f;

		// Token: 0x0400086D RID: 2157
		private const float MistSpeedMax = 1.35f;

		// Token: 0x0400086E RID: 2158
		private const float MistLifetime = 0.5f;

		// Token: 0x0400086F RID: 2159
		private const float MistSize = 0.014f;

		// Token: 0x04000870 RID: 2160
		private const float RayDownDistance = 12f;

		// Token: 0x04000871 RID: 2161
		private const float RainRatePerSecond = 1600f;

		// Token: 0x04000872 RID: 2162
		private const int MaxEmitPerTick = 240;

		// Token: 0x04000873 RID: 2163
		private const int MaxParticles = 4096;

		// Token: 0x04000874 RID: 2164
		private const float RainActiveEps = 0.001f;

		// Token: 0x04000875 RID: 2165
		private readonly ManualLogSource _log;

		// Token: 0x04000876 RID: 2166
		private readonly RenderEngine _engine;

		// Token: 0x04000877 RID: 2167
		private readonly WorldRain.Cell[] _cells = new WorldRain.Cell[81];

		// Token: 0x04000878 RID: 2168
		private int _cursor;

		// Token: 0x04000879 RID: 2169
		private bool _gridWarm;

		// Token: 0x0400087A RID: 2170
		private GameObject _go;

		// Token: 0x0400087B RID: 2171
		private ParticleSystem _ps;

		// Token: 0x0400087C RID: 2172
		private ParticleSystemRenderer _psr;

		// Token: 0x0400087D RID: 2173
		private Material _mat;

		// Token: 0x0400087E RID: 2174
		private bool _shaderMissingLogged;

		// Token: 0x0400087F RID: 2175
		private ParticleSystem.EmitParams _emit;

		// Token: 0x04000880 RID: 2176
		private GameObject _splashGo;

		// Token: 0x04000881 RID: 2177
		private ParticleSystem _splashPs;

		// Token: 0x04000882 RID: 2178
		private ParticleSystemRenderer _splashPsr;

		// Token: 0x04000883 RID: 2179
		private Material _splashMat;

		// Token: 0x04000884 RID: 2180
		private ParticleSystem.EmitParams _splashEmit;

		// Token: 0x04000885 RID: 2181
		private ParticleSystem.EmitParams _mistEmit;

		// Token: 0x04000886 RID: 2182
		private bool _mistPrimed;

		// Token: 0x04000887 RID: 2183
		private float _splashAcc;

		// Token: 0x04000888 RID: 2184
		private bool _want;

		// Token: 0x04000889 RID: 2185
		private bool _vrAllowed = true;

		// Token: 0x0400088A RID: 2186
		private bool _desktopAllowed = true;

		// Token: 0x0400088B RID: 2187
		private float _rainVisibility = 0.25f;

		// Token: 0x0400088C RID: 2188
		private float _wind = 0.4f;

		// Token: 0x0400088D RID: 2189
		private float _splashes = 0.6f;

		// Token: 0x0400088E RID: 2190
		private bool _vrBalanced;

		// Token: 0x0400088F RID: 2191
		private bool _capHalved;

		// Token: 0x04000890 RID: 2192
		private float _emitAcc;

		// Token: 0x04000891 RID: 2193
		private float _settleUntil = -1f;

		// Token: 0x04000892 RID: 2194
		private bool _loggedActive;

		// Token: 0x04000893 RID: 2195
		private bool _wasActive;

		// Token: 0x0200005F RID: 95
		private struct Cell
		{
			// Token: 0x04000894 RID: 2196
			public float X;

			// Token: 0x04000895 RID: 2197
			public float Z;

			// Token: 0x04000896 RID: 2198
			public bool Open;

			// Token: 0x04000897 RID: 2199
			public bool Valid;

			// Token: 0x04000898 RID: 2200
			public float GroundY;

			// Token: 0x04000899 RID: 2201
			public bool HasGround;
		}
	}
}
