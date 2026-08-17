using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200000C RID: 12
	internal sealed class Insects
	{
		// Token: 0x0600005B RID: 91 RVA: 0x00005F34 File Offset: 0x00004134
		private static Insects.Tuning TuningFor(Insects.Kind k)
		{
			if (k == Insects.Kind.Butterfly)
			{
				return new Insects.Tuning
				{
					Radius = 26f,
					HeightLow = -2.2f,
					HeightHigh = 3f,
					Scale = 0.18f,
					CruiseLow = 0.8f,
					CruiseHigh = 1.6f,
					TurnRate = 2.6f,
					RetargetLow = 1.1f,
					RetargetHigh = 3.2f,
					FlapRate = 7f,
					FlapAmp = 1.45f,
					BobAmount = 0.085f,
					Population = 34,
					Clusters = 1
				};
			}
			return new Insects.Tuning
			{
				Radius = 16f,
				HeightLow = -2f,
				HeightHigh = 1.2f,
				Scale = 0.07f,
				CruiseLow = 1.4f,
				CruiseHigh = 3.2f,
				TurnRate = 22f,
				RetargetLow = 0.25f,
				RetargetHigh = 0.9f,
				FlapRate = 12f,
				FlapAmp = 0.55f,
				BobAmount = 0f,
				Population = 40,
				Clusters = 4
			};
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x0600005C RID: 92 RVA: 0x0000609C File Offset: 0x0000429C
		// (set) Token: 0x0600005D RID: 93 RVA: 0x000060A3 File Offset: 0x000042A3
		public static bool ButterfliesOn { get; private set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600005E RID: 94 RVA: 0x000060AB File Offset: 0x000042AB
		// (set) Token: 0x0600005F RID: 95 RVA: 0x000060B2 File Offset: 0x000042B2
		public static bool BeesOn { get; private set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000060 RID: 96 RVA: 0x000060BA File Offset: 0x000042BA
		public static bool AnyOn
		{
			get
			{
				return Insects.ButterfliesOn || Insects.BeesOn;
			}
		}

		// Token: 0x06000061 RID: 97 RVA: 0x000060CC File Offset: 0x000042CC
		public Insects(ManualLogSource log, RenderEngine engine, Insects.Kind kind)
		{
			this._log = log;
			this._engine = engine;
			this._kind = kind;
			this._t = Insects.TuningFor(kind);
			this._tag = ((kind == Insects.Kind.Butterfly) ? "BUTTERFLIES" : "BEES");
			this._rng = new System.Random((kind == Insects.Kind.Butterfly) ? 20260814 : 20260815);
		}

		// Token: 0x06000062 RID: 98 RVA: 0x000061A0 File Offset: 0x000043A0
		public void Configure(bool want, bool vrAllowed, bool desktopAllowed, float density, float size, float height, float speed)
		{
			this._want = want;
			if (this._kind == Insects.Kind.Butterfly)
			{
				Insects.ButterfliesOn = want;
			}
			else
			{
				Insects.BeesOn = want;
			}
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._density = Mathf.Clamp(density, 0f, 2f);
			this._size = Mathf.Clamp(size, 0.2f, 3f);
			this._height = Mathf.Clamp(height, 0.2f, 3f);
			this._speed = Mathf.Clamp(speed, 0.2f, 3f);
			if (this._count > 0)
			{
				this._count = 0;
			}
		}

		// Token: 0x06000063 RID: 99 RVA: 0x00006244 File Offset: 0x00004444
		private bool ZoneAllows(string zone)
		{
			if (string.IsNullOrEmpty(zone))
			{
				return true;
			}
			for (int i = 0; i < Insects.UnnaturalZones.Length; i++)
			{
				if (zone.IndexOf(Insects.UnnaturalZones[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x06000064 RID: 100 RVA: 0x00006284 File Offset: 0x00004484
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
					flag2 = gameHour >= 6f && gameHour <= 19f;
				}
				if (flag2 && (!MapSense.IsOutdoor || !MapSense.HasSky))
				{
					flag2 = false;
				}
				string text = MapSense.ZoneName ?? string.Empty;
				if (flag2 && !this.ZoneAllows(text))
				{
					if (!this._loggedZone)
					{
						this._loggedZone = true;
						this._log.LogInfo(this._tag + ": zone '" + text + "' is built rather than grown - skipped on purpose (natural maps only).");
					}
					flag2 = false;
				}
				if (!flag2)
				{
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
									num = Mathf.Min(num, Vector3.Distance(this._bugs[i].Pos, position));
								}
								float num2 = this._t.Scale * this._size;
								float num3 = ((this._count > 0 && num > 0.01f) ? (Mathf.Atan2(num2, num) * 57.29578f) : 0f);
								this._log.LogInfo(string.Format("{0} GEOMETRY: {1} in zone '{2}', nearest ", this._tag, this._count, text) + string.Format("{0:0.##} m, wingspan {1:0.000} m, apparent ", num, num2) + string.Format("{0:0.000} deg -- ", num3) + ((this._count == 0) ? "NONE SEEDED" : ((num3 < 0.05f) ? "SUB-PIXEL: drawn but unresolvable" : "resolvable")));
							}
						}
						this._lastAnchor = position;
						this.Simulate(position, Time.deltaTime);
						this._submitted = 0;
						this.Draw();
						if (!this._loggedSubmit)
						{
							this._loggedSubmit = true;
							this._log.LogInfo(string.Format("{0} DRAW: submitted {1} instance(s) across ", this._tag, this._submitted) + string.Format("{0} material(s) via RenderMeshInstanced, bounds ", this._mats.Length) + string.Format("centred {0} extent {1:0} m. ", position, this._t.Radius * 4f + 20f) + ((this._submitted == 0) ? "ZERO SUBMITTED - the draw loop is the fault" : "submitted; if still invisible the fault is the material/shader, not the steering."));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning(this._tag + ": tick failed - " + ex.Message);
			}
		}

		// Token: 0x06000065 RID: 101 RVA: 0x000065B0 File Offset: 0x000047B0
		public void Dispose()
		{
			this.Release();
		}

		// Token: 0x06000066 RID: 102 RVA: 0x000065B8 File Offset: 0x000047B8
		private void Release()
		{
			if (this._kind == Insects.Kind.Butterfly)
			{
				Insects.ButterfliesOn = false;
			}
			else
			{
				Insects.BeesOn = false;
			}
			this._count = 0;
			this._loggedActive = false;
			for (int i = 0; i < this._mats.Length; i++)
			{
				if (this._mats[i] != null)
				{
					try
					{
						UnityEngine.Object.Destroy(this._mats[i]);
					}
					catch
					{
					}
				}
			}
			this._mats = Array.Empty<Material>();
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

		// Token: 0x06000067 RID: 103 RVA: 0x0000666C File Offset: 0x0000486C
		private bool EnsureResources()
		{
			if (this._mesh != null && this._mats.Length != 0)
			{
				return true;
			}
			Shader shader = ((this._engine != null) ? this._engine.GetShader("LumaLooks/Bird") : null);
			if (shader == null)
			{
				this._log.LogWarning(this._tag + ": 'LumaLooks/Bird' missing from the bundle - none drawn.");
				this._want = false;
				return false;
			}
			if (this._mesh == null)
			{
				this._mesh = ((this._kind == Insects.Kind.Butterfly) ? Insects.BuildButterflyMesh() : Insects.BuildBeeMesh());
			}
			if (this._mats.Length == 0)
			{
				int num = ((this._kind == Insects.Kind.Butterfly) ? Insects.ButterflySpecies.Length : 1);
				this._mats = new Material[num];
				for (int i = 0; i < num; i++)
				{
					Color color = ((this._kind == Insects.Kind.Butterfly) ? Insects.ButterflySpecies[i][0] : Insects.BeeColours[0]);
					Color color2 = ((this._kind == Insects.Kind.Butterfly) ? Insects.ButterflySpecies[i][1] : Insects.BeeColours[1]);
					Material material = new Material(shader)
					{
						hideFlags = (HideFlags)61
					};
					material.enableInstancing = true;
					material.SetColor("_LumaBirdColor", color);
					material.SetColor("_LumaBirdBelly", color2);
					material.SetFloat("_LumaBirdFlapRate", this._t.FlapRate);
					material.SetFloat("_LumaBirdFlapAmp", this._t.FlapAmp);
					this._mats[i] = material;
				}
			}
			if (this._mpb == null)
			{
				this._mpb = new MaterialPropertyBlock();
			}
			if (!this._loggedActive)
			{
				this._loggedActive = true;
				this._log.LogInfo(this._tag + ": online (" + this._mats.Length.ToString() + " species, shader reused from Birds).");
			}
			return true;
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00006848 File Offset: 0x00004A48
		private static Mesh BuildButterflyMesh()
		{
			Mesh mesh = new Mesh
			{
				name = "LumaLooks_Butterfly",
				hideFlags = (HideFlags)61
			};
			Vector3[] array = new Vector3[]
			{
				new Vector3(0f, -0.1f, 0.26f),
				new Vector3(0.045f, -0.1f, 0.02f),
				new Vector3(-0.045f, -0.1f, 0.02f),
				new Vector3(0f, -0.1f, -0.44f),
				new Vector3(0.06f, 0.12f, 0.14f),
				new Vector3(0.06f, 0.12f, -0.06f),
				new Vector3(0.86f, 0.12f, 0.4f),
				new Vector3(1f, 0.12f, -0.04f),
				new Vector3(0.06f, 0.12f, -0.08f),
				new Vector3(0.06f, 0.12f, -0.34f),
				new Vector3(0.74f, 0.12f, -0.16f),
				new Vector3(0.52f, 0.12f, -0.52f),
				new Vector3(-0.06f, 0.12f, 0.14f),
				new Vector3(-0.06f, 0.12f, -0.06f),
				new Vector3(-0.86f, 0.12f, 0.4f),
				new Vector3(-1f, 0.12f, -0.04f),
				new Vector3(-0.06f, 0.12f, -0.08f),
				new Vector3(-0.06f, 0.12f, -0.34f),
				new Vector3(-0.74f, 0.12f, -0.16f),
				new Vector3(-0.52f, 0.12f, -0.52f)
			};
			int[] array2 = new int[]
			{
				0, 1, 3, 0, 3, 2, 0, 2, 1, 1,
				2, 3, 4, 6, 5, 5, 6, 7, 8, 10,
				9, 9, 10, 11, 12, 13, 14, 13, 15, 14,
				16, 17, 18, 17, 19, 18
			};
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.RecalculateNormals();
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 6f);
			return mesh;
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00006AE4 File Offset: 0x00004CE4
		private static Mesh BuildBeeMesh()
		{
			Mesh mesh = new Mesh
			{
				name = "LumaLooks_Bee",
				hideFlags = (HideFlags)61
			};
			Vector3[] array = new Vector3[]
			{
				new Vector3(0f, -0.1f, 0.42f),
				new Vector3(0.19f, -0.1f, 0.06f),
				new Vector3(-0.19f, -0.1f, 0.06f),
				new Vector3(0f, 0.059999995f, 0.04f),
				new Vector3(0f, -0.1f, -0.52f),
				new Vector3(0.14f, 0.12f, 0.12f),
				new Vector3(0.14f, 0.12f, -0.08f),
				new Vector3(0.92f, 0.12f, 0.1f),
				new Vector3(0.8f, 0.12f, -0.16f),
				new Vector3(-0.14f, 0.12f, 0.12f),
				new Vector3(-0.14f, 0.12f, -0.08f),
				new Vector3(-0.92f, 0.12f, 0.1f),
				new Vector3(-0.8f, 0.12f, -0.16f)
			};
			int[] array2 = new int[]
			{
				0, 3, 1, 0, 2, 3, 0, 1, 2, 4,
				1, 3, 4, 3, 2, 4, 2, 1, 5, 7,
				6, 6, 7, 8, 9, 10, 11, 10, 12, 11
			};
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.RecalculateNormals();
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 6f);
			return mesh;
		}

		// Token: 0x0600006A RID: 106 RVA: 0x00006CBC File Offset: 0x00004EBC
		private void Seed(Vector3 anchor)
		{
			int num = Mathf.Clamp(Mathf.RoundToInt((float)this._t.Population * this._density), 0, 160);
			if (num == 0)
			{
				this._count = 0;
				return;
			}
			if (this._bugs.Length < num)
			{
				this._bugs = new Insects.Insect[num];
				this._matrices = new Matrix4x4[Mathf.Min(num, 1023)];
				this._phases = new float[this._matrices.Length];
				this._flapScales = new float[this._matrices.Length];
			}
			int num2 = Mathf.Clamp(this._t.Clusters, 1, this._clusterCentre.Length);
			for (int i = 0; i < num2; i++)
			{
				this._clusterCentre[i] = anchor + this.RandomInDisc(this._t.Radius * 0.6f) + Vector3.up * this.Band();
			}
			for (int j = 0; j < num; j++)
			{
				int num3 = ((num2 == 1) ? 0 : (j % num2));
				this._bugs[j].Cluster = num3;
				this._bugs[j].Pos = ((num2 == 1) ? (anchor + this.RandomInDisc(this._t.Radius) + Vector3.up * this.Band()) : (this._clusterCentre[num3] + this.RandomInSphere(1.6f)));
				this._bugs[j].Vel = this.RandomHeading() * this.Cruise();
				this._bugs[j].Target = this._bugs[j].Pos;
				this._bugs[j].RetargetTimer = (float)this._rng.NextDouble() * this._t.RetargetHigh;
				this._bugs[j].Phase = (float)this._rng.NextDouble();
				this._bugs[j].Species = ((this._mats.Length > 1) ? this._rng.Next(this._mats.Length) : 0);
			}
			this._count = num;
		}

		// Token: 0x0600006B RID: 107 RVA: 0x00006F03 File Offset: 0x00005103
		private float Cruise()
		{
			return Mathf.Lerp(this._t.CruiseLow, this._t.CruiseHigh, (float)this._rng.NextDouble()) * this._speed;
		}

		// Token: 0x0600006C RID: 108 RVA: 0x00006F33 File Offset: 0x00005133
		private float Band()
		{
			return Mathf.Lerp(this._t.HeightLow, this._t.HeightHigh, (float)this._rng.NextDouble()) * this._height;
		}

		// Token: 0x0600006D RID: 109 RVA: 0x00006F64 File Offset: 0x00005164
		private Vector3 RandomHeading()
		{
			float num = (float)this._rng.NextDouble() * 3.1415927f * 2f;
			return new Vector3(Mathf.Cos(num), 0f, Mathf.Sin(num));
		}

		// Token: 0x0600006E RID: 110 RVA: 0x00006FA0 File Offset: 0x000051A0
		private Vector3 RandomInDisc(float r)
		{
			float num = (float)this._rng.NextDouble() * 3.1415927f * 2f;
			float num2 = Mathf.Sqrt((float)this._rng.NextDouble()) * r;
			return new Vector3(Mathf.Cos(num) * num2, 0f, Mathf.Sin(num) * num2);
		}

		// Token: 0x0600006F RID: 111 RVA: 0x00006FF4 File Offset: 0x000051F4
		private Vector3 RandomInSphere(float r)
		{
			return new Vector3((float)this._rng.NextDouble() - 0.5f, (float)this._rng.NextDouble() - 0.5f, (float)this._rng.NextDouble() - 0.5f) * (r * 2f);
		}

		// Token: 0x06000070 RID: 112 RVA: 0x00007048 File Offset: 0x00005248
		private void Simulate(Vector3 anchor, float dt)
		{
			if (dt <= 0f)
			{
				return;
			}
			dt = Mathf.Min(dt, 0.05f);
			int num = Mathf.Clamp(this._t.Clusters, 1, this._clusterCentre.Length);
			for (int i = 0; i < num; i++)
			{
				Vector3 vector = this._clusterCentre[i];
				vector += new Vector3(Mathf.Sin(Time.time * 0.13f + (float)i * 2.1f), 0f, Mathf.Cos(Time.time * 0.11f + (float)i * 1.3f)) * (0.6f * dt * this._speed);
				Vector3 vector2 = new Vector3(vector.x - anchor.x, 0f, vector.z - anchor.z);
				if (vector2.magnitude > this._t.Radius * 2.5f)
				{
					vector = anchor + this.RandomInDisc(this._t.Radius) + Vector3.up * this.Band();
				}
				this._clusterCentre[i] = vector;
			}
			for (int j = 0; j < this._count; j++)
			{
				ref Insects.Insect ptr = ref this._bugs[j];
				ptr.RetargetTimer -= dt;
				if (ptr.RetargetTimer <= 0f)
				{
					Vector3 vector3 = ((num == 1) ? ptr.Pos : this._clusterCentre[ptr.Cluster]);
					float num2 = ((num == 1) ? this._t.Radius : 2.2f);
					ptr.Target = vector3 + this.RandomInDisc(num2) + Vector3.up * this.Band();
					ptr.RetargetTimer = Mathf.Lerp(this._t.RetargetLow, this._t.RetargetHigh, (float)this._rng.NextDouble()) / Mathf.Max(this._speed, 0.2f);
				}
				Vector3 vector4 = ptr.Target - ptr.Pos;
				float num3 = ((vector4.magnitude > 0.25f) ? this.Cruise() : (this.Cruise() * 0.15f));
				Vector3 vector5 = Vector3.ClampMagnitude((((vector4.sqrMagnitude > 0.0001f) ? (vector4.normalized * num3) : Vector3.zero) - ptr.Vel) * 4f, this._t.TurnRate * this._speed);
				ptr.Vel += vector5 * dt;
				float num4 = Time.time * ((this._kind == Insects.Kind.Bee) ? 11f : 2.4f) + ptr.Phase * 31.4f;
				Vector3 vector6 = Vector3.Cross(Vector3.up, (ptr.Vel.sqrMagnitude > 0.0001f) ? ptr.Vel.normalized : Vector3.forward);
				ptr.Vel += vector6 * (Mathf.Sin(num4) * ((this._kind == Insects.Kind.Bee) ? 2.6f : 0.9f) * dt * this._speed);
				ptr.Pos += ptr.Vel * dt;
				float num5 = anchor.y - 1.8f;
				if (ptr.Pos.y < num5)
				{
					ptr.Pos.y = num5;
					if (ptr.Vel.y < 0f)
					{
						ptr.Vel.y = -ptr.Vel.y * 0.4f;
					}
				}
				Vector3 vector7 = ptr.Pos - anchor;
				vector7.y = 0f;
				float num6 = this._t.Radius * 2.5f;
				if (vector7.sqrMagnitude > num6 * num6)
				{
					ptr.Pos = anchor + this.RandomInDisc(this._t.Radius) + Vector3.up * this.Band();
					ptr.Vel = this.RandomHeading() * this.Cruise();
					ptr.Target = ptr.Pos;
					ptr.RetargetTimer = 0f;
				}
				ptr.Phase += dt * 0.21f;
				if (ptr.Phase > 1f)
				{
					ptr.Phase -= 1f;
				}
			}
		}

		// Token: 0x06000071 RID: 113 RVA: 0x000074FC File Offset: 0x000056FC
		private void Draw()
		{
			if (this._mesh == null || this._mats.Length == 0 || this._count == 0)
			{
				return;
			}
			float num = this._t.Scale * this._size;
			float time = Time.time;
			for (int i = 0; i < this._mats.Length; i++)
			{
				if (!(this._mats[i] == null))
				{
					int num2 = 0;
					int num3 = 0;
					while (num3 < this._count && num2 < this._matrices.Length)
					{
						ref Insects.Insect ptr = ref this._bugs[num3];
						if (ptr.Species == i)
						{
							Vector3 vector = ((ptr.Vel.sqrMagnitude > 1E-06f) ? ptr.Vel.normalized : Vector3.forward);
							Vector3 pos = ptr.Pos;
							if (this._t.BobAmount > 0f)
							{
								pos.y += Mathf.Sin(time * this._t.FlapRate + ptr.Phase * 6.2831855f) * this._t.BobAmount * this._size;
							}
							Vector3 vector2 = Quaternion.AngleAxis(Mathf.Clamp(Vector3.Dot(Vector3.Cross(Vector3.up, vector).normalized, ptr.Vel.normalized - vector) * 400f, -25f, 25f), vector) * Vector3.up;
							this._matrices[num2] = Matrix4x4.TRS(pos, Quaternion.LookRotation(vector, vector2), Vector3.one * num);
							this._phases[num2] = ptr.Phase;
							this._flapScales[num2] = 1f;
							num2++;
						}
						num3++;
					}
					if (num2 != 0)
					{
						this._mpb.Clear();
						this._mpb.SetFloatArray("_LumaBirdPhase", this._phases);
						this._mpb.SetFloatArray("_LumaBirdFlapScale", this._flapScales);
						RenderParams renderParams = new RenderParams(this._mats[i]);
						renderParams.worldBounds = new Bounds(this._lastAnchor, Vector3.one * (this._t.Radius * 4f + 20f));
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
		}

		// Token: 0x0400009D RID: 157
		private const string ShaderName = "LumaLooks/Bird";

		// Token: 0x0400009E RID: 158
		private const int MaxPerBatch = 1023;

		// Token: 0x0400009F RID: 159
		private const int MaxInsects = 160;

		// Token: 0x040000A0 RID: 160
		private const float DawnHour = 6f;

		// Token: 0x040000A1 RID: 161
		private const float DuskHour = 19f;

		// Token: 0x040000A2 RID: 162
		private static readonly Color[][] ButterflySpecies = new Color[][]
		{
			new Color[]
			{
				new Color(0.95f, 0.45f, 0.1f),
				new Color(0.12f, 0.1f, 0.09f)
			},
			new Color[]
			{
				new Color(0.96f, 0.96f, 0.92f),
				new Color(0.2f, 0.19f, 0.18f)
			},
			new Color[]
			{
				new Color(0.32f, 0.5f, 0.92f),
				new Color(0.14f, 0.14f, 0.18f)
			},
			new Color[]
			{
				new Color(0.94f, 0.86f, 0.32f),
				new Color(0.22f, 0.2f, 0.12f)
			}
		};

		// Token: 0x040000A3 RID: 163
		private static readonly Color[] BeeColours = new Color[]
		{
			new Color(0.86f, 0.86f, 0.84f),
			new Color(0.85f, 0.62f, 0.1f)
		};

		// Token: 0x040000A4 RID: 164
		private readonly ManualLogSource _log;

		// Token: 0x040000A5 RID: 165
		private readonly RenderEngine _engine;

		// Token: 0x040000A6 RID: 166
		private readonly Insects.Kind _kind;

		// Token: 0x040000A7 RID: 167
		private readonly Insects.Tuning _t;

		// Token: 0x040000A8 RID: 168
		private readonly string _tag;

		// Token: 0x040000A9 RID: 169
		private Mesh _mesh;

		// Token: 0x040000AA RID: 170
		private Material[] _mats = Array.Empty<Material>();

		// Token: 0x040000AB RID: 171
		private MaterialPropertyBlock _mpb;

		// Token: 0x040000AC RID: 172
		private bool _want;

		// Token: 0x040000AD RID: 173
		private bool _vrAllowed;

		// Token: 0x040000AE RID: 174
		private bool _desktopAllowed;

		// Token: 0x040000AF RID: 175
		private float _density = 1f;

		// Token: 0x040000B0 RID: 176
		private float _size = 1f;

		// Token: 0x040000B1 RID: 177
		private float _height = 1f;

		// Token: 0x040000B2 RID: 178
		private float _speed = 1f;

		// Token: 0x040000B3 RID: 179
		private Insects.Insect[] _bugs = Array.Empty<Insects.Insect>();

		// Token: 0x040000B4 RID: 180
		private int _count;

		// Token: 0x040000B5 RID: 181
		private readonly Vector3[] _clusterCentre = new Vector3[8];

		// Token: 0x040000B6 RID: 182
		private Matrix4x4[] _matrices = Array.Empty<Matrix4x4>();

		// Token: 0x040000B7 RID: 183
		private float[] _phases = Array.Empty<float>();

		// Token: 0x040000B8 RID: 184
		private float[] _flapScales = Array.Empty<float>();

		// Token: 0x040000B9 RID: 185
		private System.Random _rng;

		// Token: 0x040000BA RID: 186
		private bool _loggedActive;

		// Token: 0x040000BB RID: 187
		private bool _loggedGeometry;

		// Token: 0x040000BC RID: 188
		private bool _loggedZone;

		// Token: 0x040000BD RID: 189
		private bool _loggedSubmit;

		// Token: 0x040000BE RID: 190
		private Vector3 _lastAnchor;

		// Token: 0x040000BF RID: 191
		private int _submitted;

		// Token: 0x040000C2 RID: 194
		private static readonly string[] UnnaturalZones = new string[] { "city", "metropolis", "clouds", "virtual", "arcade", "basement", "rotating" };

		// Token: 0x040000C3 RID: 195
		private const float WingY = 0.12f;

		// Token: 0x040000C4 RID: 196
		private const float BodyY = -0.1f;

		// Token: 0x0200000D RID: 13
		internal enum Kind
		{
			// Token: 0x040000C6 RID: 198
			Butterfly,
			// Token: 0x040000C7 RID: 199
			Bee
		}

		// Token: 0x0200000E RID: 14
		private struct Tuning
		{
			// Token: 0x040000C8 RID: 200
			public float Radius;

			// Token: 0x040000C9 RID: 201
			public float HeightLow;

			// Token: 0x040000CA RID: 202
			public float HeightHigh;

			// Token: 0x040000CB RID: 203
			public float Scale;

			// Token: 0x040000CC RID: 204
			public float CruiseLow;

			// Token: 0x040000CD RID: 205
			public float CruiseHigh;

			// Token: 0x040000CE RID: 206
			public float TurnRate;

			// Token: 0x040000CF RID: 207
			public float RetargetLow;

			// Token: 0x040000D0 RID: 208
			public float RetargetHigh;

			// Token: 0x040000D1 RID: 209
			public float FlapRate;

			// Token: 0x040000D2 RID: 210
			public float FlapAmp;

			// Token: 0x040000D3 RID: 211
			public float BobAmount;

			// Token: 0x040000D4 RID: 212
			public int Population;

			// Token: 0x040000D5 RID: 213
			public int Clusters;
		}

		// Token: 0x0200000F RID: 15
		private struct Insect
		{
			// Token: 0x040000D6 RID: 214
			public Vector3 Pos;

			// Token: 0x040000D7 RID: 215
			public Vector3 Vel;

			// Token: 0x040000D8 RID: 216
			public Vector3 Target;

			// Token: 0x040000D9 RID: 217
			public float RetargetTimer;

			// Token: 0x040000DA RID: 218
			public float Phase;

			// Token: 0x040000DB RID: 219
			public int Species;

			// Token: 0x040000DC RID: 220
			public int Cluster;
		}
	}
}
