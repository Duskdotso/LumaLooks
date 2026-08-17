using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;
using Random = System.Random;

namespace LumaLooks
{
	// Token: 0x02000005 RID: 5
	internal sealed class Birds
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000011 RID: 17 RVA: 0x0000233E File Offset: 0x0000053E
		// (set) Token: 0x06000012 RID: 18 RVA: 0x00002345 File Offset: 0x00000545
		public static bool BirdsOn { get; private set; }

		// Token: 0x06000013 RID: 19 RVA: 0x00002350 File Offset: 0x00000550
		public Birds(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000023F4 File Offset: 0x000005F4
		public void Configure(bool want, bool vrAllowed, bool desktopAllowed, float density, float size, float altitude, float speed)
		{
			this._want = want;
			Birds.BirdsOn = want;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._density = Mathf.Clamp(density, 0f, 2f);
			this._size = Mathf.Clamp(size, 0.2f, 3f);
			this._altitude = Mathf.Clamp(altitude, 0.2f, 3f);
			this._speed = Mathf.Clamp(speed, 0.2f, 3f);
			if (this._count > 0)
			{
				this.Reseed();
			}
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002488 File Offset: 0x00000688
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
				bool flag2 = this._want && (flag ? this._vrAllowed : this._desktopAllowed);
				if (flag2 && GtClock.Available)
				{
					float gameHour = GtClock.GameHour;
					flag2 = gameHour >= 5.5f && gameHour <= 19.5f;
				}
				bool flag3 = MapSense.IsOutdoor && MapSense.HasSky;
				if (flag2 && !flag3)
				{
					flag2 = false;
				}
				if (!flag2)
				{
					string text = ((!this._want) ? "the effect is OFF in settings" : ((flag ? (!this._vrAllowed) : (!this._desktopAllowed)) ? (flag ? "the VR toggle is off" : "the Desktop toggle is off") : ((GtClock.Available && (GtClock.GameHour < 5.5f || GtClock.GameHour > 19.5f)) ? string.Format("it is night (game hour {0:0.0}; birds fly {1}-{2})", GtClock.GameHour, 5.5f, 19.5f) : ((!MapSense.IsOutdoor) ? "the map is not outdoor" : ((!MapSense.HasSky) ? "the map has no sky" : "unknown gate")))));
					if (text != this._lastGateReason)
					{
						this._lastGateReason = text;
						this._log.LogInfo("BIRDS: not flying - " + text + ".");
					}
					this.Release();
				}
				else if (this.EnsureResources())
				{
					Camera main = Camera.main;
					if (!(main == null))
					{
						Vector3 position = main.transform.position;
						if (this._count == 0)
						{
							this.Seed(position);
							if (!this._loggedGeometry)
							{
								this._loggedGeometry = true;
								float num = float.MaxValue;
								for (int i = 0; i < this._count; i++)
								{
									num = Mathf.Min(num, Vector3.Distance(this._birds[i].Pos, position));
								}
								float num2 = (this._seagull ? 0.95f : 0.55f) * this._size;
								float num3 = ((this._count > 0 && num > 0.01f) ? (Mathf.Atan2(num2, num) * 57.29578f) : 0f);
								this._log.LogInfo(string.Format("BIRDS GEOMETRY: {0} bird(s) in {1} flock(s), ", this._count, this._flockCount) + string.Format("nearest {0:0.#} m, wingspan {1:0.00} m, apparent ", num, num2) + string.Format("{0:0.000} deg -- ", num3) + ((this._count == 0) ? "NONE SEEDED" : ((num3 < 0.05f) ? "SUB-PIXEL: drawn but unresolvable" : "resolvable")));
							}
						}
						this._lastAnchor = position;
						this.Simulate(position, Time.deltaTime);
						this._submitted = 0;
						this.Draw();
						if (!this._loggedSubmit)
						{
							this._loggedSubmit = true;
							this._log.LogInfo(string.Format("BIRDS DRAW: submitted {0} instance(s) via ", this._submitted) + "RenderMeshInstanced. " + ((this._submitted == 0) ? "ZERO SUBMITTED - the draw loop is the fault" : "submitted; if still invisible the fault is the material/shader, not the steering."));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("BIRDS: tick failed - " + ex.Message);
			}
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002808 File Offset: 0x00000A08
		public void Dispose()
		{
			this.Release();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002810 File Offset: 0x00000A10
		private void Release()
		{
			Birds.BirdsOn = false;
			this._count = 0;
			this._loggedActive = false;
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
			if (this._mesh != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._mesh);
				}
				catch
				{
				}
				this._mesh = null;
			}
		}

		// Token: 0x06000018 RID: 24 RVA: 0x00002898 File Offset: 0x00000A98
		private bool EnsureResources()
		{
			if (this._mat != null && this._mesh != null)
			{
				return true;
			}
			Shader shader = ((this._engine != null) ? this._engine.GetShader("LumaLooks/Bird") : null);
			if (shader == null)
			{
				this._log.LogWarning("BIRDS: 'LumaLooks/Bird' missing from the bundle - no birds.");
				this._want = false;
				return false;
			}
			string text = MapSense.ZoneName ?? string.Empty;
			this._seagull = text.IndexOf("beach", StringComparison.OrdinalIgnoreCase) >= 0;
			if (this._mesh == null)
			{
				this._mesh = Birds.BuildMesh();
			}
			if (this._mat == null)
			{
				this._mat = new Material(shader)
				{
					hideFlags = (HideFlags)61
				};
				this._mat.enableInstancing = true;
				if (this._seagull)
				{
					this._mat.SetColor("_LumaBirdColor", new Color(0.92f, 0.93f, 0.95f));
					this._mat.SetColor("_LumaBirdBelly", new Color(0.98f, 0.98f, 1f));
					this._mat.SetFloat("_LumaBirdFlapRate", 2.1f);
					this._mat.SetFloat("_LumaBirdFlapAmp", 0.7f);
				}
				else
				{
					this._mat.SetColor("_LumaBirdColor", new Color(0.09f, 0.09f, 0.11f));
					this._mat.SetColor("_LumaBirdBelly", new Color(0.24f, 0.24f, 0.28f));
					this._mat.SetFloat("_LumaBirdFlapRate", 4.2f);
					this._mat.SetFloat("_LumaBirdFlapAmp", 0.95f);
				}
			}
			if (this._mpb == null)
			{
				this._mpb = new MaterialPropertyBlock();
			}
			if (!this._loggedActive)
			{
				this._loggedActive = true;
				this._log.LogInfo(string.Concat(new string[]
				{
					"BIRDS: flock online (",
					this._seagull ? "seagulls" : "songbirds",
					", zone '",
					text,
					"')."
				}));
			}
			return true;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x00002ACC File Offset: 0x00000CCC
		private static Mesh BuildMesh()
		{
			Mesh mesh = new Mesh
			{
				name = "LumaLooks_Bird",
				hideFlags = (HideFlags)61
			};
			Vector3[] array = new Vector3[]
			{
				new Vector3(0f, 0f, 0.55f),
				new Vector3(0f, 0.05f, -0.1f),
				new Vector3(0f, -0.05f, -0.1f),
				new Vector3(0f, 0f, -0.62f),
				new Vector3(0.1f, 0f, 0.16f),
				new Vector3(0.1f, 0f, -0.2f),
				new Vector3(1f, 0f, -0.1f),
				new Vector3(0.78f, 0f, -0.34f),
				new Vector3(-0.1f, 0f, 0.16f),
				new Vector3(-0.1f, 0f, -0.2f),
				new Vector3(-1f, 0f, -0.1f),
				new Vector3(-0.78f, 0f, -0.34f)
			};
			int[] array2 = new int[]
			{
				0, 1, 3, 0, 3, 2, 0, 2, 1, 1,
				2, 3, 4, 6, 5, 5, 6, 7, 8, 9,
				10, 9, 11, 10
			};
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.RecalculateNormals();
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 4f);
			return mesh;
		}

		// Token: 0x0600001A RID: 26 RVA: 0x00002C88 File Offset: 0x00000E88
		private void Reseed()
		{
			this._count = 0;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x00002C94 File Offset: 0x00000E94
		private void Seed(Vector3 anchor)
		{
			this._flockCount = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(2f, 6f, this._density * 0.5f)), 1, 6);
			int num = Mathf.Clamp(Mathf.RoundToInt(60f * this._density), 6, 240);
			int num2 = 0;
			for (int i = 0; i < this._flockCount; i++)
			{
				float num3 = (float)this._rng.NextDouble();
				float num4 = Mathf.Lerp(0.25f, 1.6f, num3 * num3);
				int num5 = Mathf.Clamp(Mathf.RoundToInt((float)num / (float)this._flockCount * num4), 2, 90);
				if (num2 + num5 > num)
				{
					num5 = Mathf.Max(2, num - num2);
				}
				this._flockSize[i] = num5;
				num2 += num5;
				if (num2 >= num)
				{
					this._flockCount = i + 1;
					break;
				}
			}
			if (this._birds.Length < num2)
			{
				this._birds = new Birds.Bird[num2];
				this._matrices = new Matrix4x4[Mathf.Min(num2, 1023)];
				this._phases = new float[this._matrices.Length];
				this._flapScales = new float[this._matrices.Length];
			}
			int num6 = 0;
			for (int j = 0; j < this._flockCount; j++)
			{
				Vector3 vector = anchor + this.RandomInDisc(84f) + Vector3.up * this.FlightHeight();
				this._flockCentre[j] = vector;
				Vector3 vector2 = this.RandomHeading();
				int num7 = 0;
				while (num7 < this._flockSize[j] && num6 < num2)
				{
					this._birds[num6].Flock = j;
					this._birds[num6].Pos = vector + this.RandomInSphere(6f + (float)this._flockSize[j] * 0.15f);
					this._birds[num6].Vel = vector2 * this.CruiseSpeed();
					this._birds[num6].Phase = (float)this._rng.NextDouble();
					this._birds[num6].FlapScale = 1f;
					this._birds[num6].GlideTimer = (float)this._rng.NextDouble() * 4f;
					num7++;
					num6++;
				}
			}
			this._count = num6;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002F05 File Offset: 0x00001105
		private float CruiseSpeed()
		{
			return Mathf.Lerp(7f, 11f, (float)this._rng.NextDouble()) * this._speed;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002F29 File Offset: 0x00001129
		private float FlightHeight()
		{
			return Mathf.Lerp(20f, 38f, (float)this._rng.NextDouble()) * this._altitude;
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002F50 File Offset: 0x00001150
		private Vector3 RandomHeading()
		{
			float num = (float)this._rng.NextDouble() * 3.1415927f * 2f;
			return new Vector3(Mathf.Cos(num), 0f, Mathf.Sin(num));
		}

		// Token: 0x0600001F RID: 31 RVA: 0x00002F8C File Offset: 0x0000118C
		private Vector3 RandomInDisc(float r)
		{
			float num = (float)this._rng.NextDouble() * 3.1415927f * 2f;
			float num2 = Mathf.Sqrt((float)this._rng.NextDouble()) * r;
			return new Vector3(Mathf.Cos(num) * num2, 0f, Mathf.Sin(num) * num2);
		}

		// Token: 0x06000020 RID: 32 RVA: 0x00002FE0 File Offset: 0x000011E0
		private Vector3 RandomInSphere(float r)
		{
			return new Vector3((float)this._rng.NextDouble() - 0.5f, (float)this._rng.NextDouble() - 0.5f, (float)this._rng.NextDouble() - 0.5f) * (r * 2f);
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00003034 File Offset: 0x00001234
		private void Simulate(Vector3 anchor, float dt)
		{
			if (dt <= 0f)
			{
				return;
			}
			dt = Mathf.Min(dt, 0.05f);
			for (int i = 0; i < this._flockCount; i++)
			{
				Vector3 vector = this._flockCentre[i];
				vector += new Vector3(Mathf.Sin(Time.time * 0.07f + (float)i * 2.3f), 0f, Mathf.Cos(Time.time * 0.05f + (float)i * 1.7f)) * (4f * dt * this._speed);
				Vector3 vector2 = new Vector3(vector.x - anchor.x, 0f, vector.z - anchor.z);
				if (vector2.magnitude > 120f)
				{
					vector = anchor + Vector3.ClampMagnitude(vector2, 120f) + Vector3.up * (vector.y - anchor.y);
				}
				this._flockCentre[i] = vector;
			}
			for (int j = 0; j < this._count; j++)
			{
				ref Birds.Bird ptr = ref this._birds[j];
				Vector3 vector3 = Vector3.zero;
				Vector3 vector4 = Vector3.zero;
				Vector3 vector5 = Vector3.zero;
				int num = 0;
				for (int k = 0; k < this._count; k++)
				{
					if (k != j && this._birds[k].Flock == ptr.Flock)
					{
						Vector3 vector6 = this._birds[k].Pos - ptr.Pos;
						float sqrMagnitude = vector6.sqrMagnitude;
						if (sqrMagnitude <= 400f && sqrMagnitude >= 0.0001f)
						{
							float num2 = Mathf.Sqrt(sqrMagnitude);
							if (num2 < 4.5f)
							{
								vector3 -= vector6 / num2 * (4.5f - num2);
							}
							vector4 += this._birds[k].Vel;
							vector5 += this._birds[k].Pos;
							num++;
						}
					}
				}
				Vector3 vector7 = Vector3.zero;
				if (num > 0)
				{
					vector4 /= (float)num;
					vector5 /= (float)num;
					vector7 += vector3 * 1.3f;
					vector7 += (vector4.normalized * ptr.Vel.magnitude - ptr.Vel) * 0.55f;
					vector7 += (vector5 - ptr.Pos) * 0.22f;
				}
				vector7 += (this._flockCentre[ptr.Flock] - ptr.Pos) * 0.2f;
				float num3 = anchor.y + 34f * this._altitude;
				vector7.y += (num3 - ptr.Pos.y) * 0.55f;
				float num4 = 9f * this._speed;
				vector7 = Vector3.ClampMagnitude(vector7, num4);
				ptr.Vel += vector7 * dt;
				float magnitude = ptr.Vel.magnitude;
				float num5 = Mathf.Lerp(7f, 11f, 0.5f) * this._speed;
				if (magnitude > 0.001f)
				{
					ptr.Vel = ptr.Vel / magnitude * Mathf.Lerp(magnitude, num5, 1.5f * dt);
				}
				ptr.Pos += ptr.Vel * dt;
				ptr.GlideTimer -= dt;
				if (ptr.GlideTimer <= 0f)
				{
					bool flag = ptr.FlapScale < 0.5f;
					ptr.FlapScale = (flag ? 1f : 0f);
					float num6 = (this._seagull ? 3.2f : 1.4f);
					ptr.GlideTimer = num6 * (0.5f + (float)this._rng.NextDouble());
				}
				float flapScale = ptr.FlapScale;
				ptr.FlapScale = Mathf.MoveTowards(ptr.FlapScale, flapScale, dt * 2f);
				ptr.Phase += dt * 0.15f;
				if (ptr.Phase > 1f)
				{
					ptr.Phase -= 1f;
				}
			}
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000034E4 File Offset: 0x000016E4
		private void Draw()
		{
			if (this._mesh == null || this._mat == null || this._count == 0)
			{
				return;
			}
			float num = (this._seagull ? 0.95f : 0.55f) * this._size;
			int i = 0;
			while (i < this._count)
			{
				int flock = this._birds[i].Flock;
				int num2 = 0;
				while (i < this._count && this._birds[i].Flock == flock && num2 < this._matrices.Length)
				{
					ref Birds.Bird ptr = ref this._birds[i];
					Vector3 vector = ((ptr.Vel.sqrMagnitude > 1E-06f) ? ptr.Vel.normalized : Vector3.forward);
					Vector3 vector2 = Quaternion.AngleAxis(Mathf.Clamp(Vector3.Dot(Vector3.Cross(Vector3.up, vector).normalized, ptr.Vel.normalized - vector) * 900f, -55f, 55f), vector) * Vector3.up;
					this._matrices[num2] = Matrix4x4.TRS(ptr.Pos, Quaternion.LookRotation(vector, vector2), Vector3.one * num);
					this._phases[num2] = ptr.Phase;
					this._flapScales[num2] = ptr.FlapScale;
					num2++;
					i++;
				}
				if (num2 == 0)
				{
					i++;
				}
				else
				{
					this._mpb.Clear();
					this._mpb.SetFloatArray("_LumaBirdPhase", this._phases);
					this._mpb.SetFloatArray("_LumaBirdFlapScale", this._flapScales);
					RenderParams renderParams = new RenderParams(this._mat);
					renderParams.worldBounds = new Bounds(this._lastAnchor, Vector3.one * 360f);
					renderParams.matProps = this._mpb;
					renderParams.shadowCastingMode = 0;
					renderParams.receiveShadows = false;
					renderParams.layer = 0;
					RenderParams renderParams2 = renderParams;
					Graphics.RenderMeshInstanced<Matrix4x4>(ref renderParams2, this._mesh, 0, this._matrices, num2, 0);
					this._submitted += num2;
				}
			}
		}

		// Token: 0x04000019 RID: 25
		private const string ShaderName = "LumaLooks/Bird";

		// Token: 0x0400001A RID: 26
		private const int MaxPerBatch = 1023;

		// Token: 0x0400001B RID: 27
		private const int MaxFlocks = 6;

		// Token: 0x0400001C RID: 28
		private const int MaxBirdsTotal = 240;

		// Token: 0x0400001D RID: 29
		private const float Range = 120f;

		// Token: 0x0400001E RID: 30
		private readonly ManualLogSource _log;

		// Token: 0x0400001F RID: 31
		private readonly RenderEngine _engine;

		// Token: 0x04000020 RID: 32
		private Mesh _mesh;

		// Token: 0x04000021 RID: 33
		private Material _mat;

		// Token: 0x04000022 RID: 34
		private MaterialPropertyBlock _mpb;

		// Token: 0x04000023 RID: 35
		private bool _want;

		// Token: 0x04000024 RID: 36
		private bool _vrAllowed;

		// Token: 0x04000025 RID: 37
		private bool _desktopAllowed;

		// Token: 0x04000026 RID: 38
		private float _density = 1f;

		// Token: 0x04000027 RID: 39
		private float _size = 1f;

		// Token: 0x04000028 RID: 40
		private float _altitude = 1f;

		// Token: 0x04000029 RID: 41
		private float _speed = 1f;

		// Token: 0x0400002A RID: 42
		private Birds.Bird[] _birds = Array.Empty<Birds.Bird>();

		// Token: 0x0400002B RID: 43
		private int _count;

		// Token: 0x0400002C RID: 44
		private int _flockCount;

		// Token: 0x0400002D RID: 45
		private readonly Vector3[] _flockCentre = new Vector3[6];

		// Token: 0x0400002E RID: 46
		private readonly int[] _flockSize = new int[6];

		// Token: 0x0400002F RID: 47
		private Matrix4x4[] _matrices = Array.Empty<Matrix4x4>();

		// Token: 0x04000030 RID: 48
		private float[] _phases = Array.Empty<float>();

		// Token: 0x04000031 RID: 49
		private float[] _flapScales = Array.Empty<float>();

		// Token: 0x04000032 RID: 50
		private Random _rng = new Random(20260810);

		// Token: 0x04000033 RID: 51
		private bool _seagull;

		// Token: 0x04000034 RID: 52
		private bool _loggedActive;

		// Token: 0x04000035 RID: 53
		private bool _loggedGeometry;

		// Token: 0x04000036 RID: 54
		private bool _loggedSubmit;

		// Token: 0x04000037 RID: 55
		private string _lastGateReason;

		// Token: 0x04000038 RID: 56
		private Vector3 _lastAnchor;

		// Token: 0x04000039 RID: 57
		private int _submitted;

		// Token: 0x0400003B RID: 59
		private const float DawnHour = 5.5f;

		// Token: 0x0400003C RID: 60
		private const float DuskHour = 19.5f;

		// Token: 0x02000006 RID: 6
		private struct Bird
		{
			// Token: 0x0400003D RID: 61
			public Vector3 Pos;

			// Token: 0x0400003E RID: 62
			public Vector3 Vel;

			// Token: 0x0400003F RID: 63
			public float Phase;

			// Token: 0x04000040 RID: 64
			public float FlapScale;

			// Token: 0x04000041 RID: 65
			public float GlideTimer;

			// Token: 0x04000042 RID: 66
			public int Flock;
		}
	}
}
