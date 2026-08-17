using System;
using System.Collections.Generic;
using BepInEx.Logging;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000027 RID: 39
	internal sealed class RainCoverage
	{
		// Token: 0x06000176 RID: 374 RVA: 0x00015DF4 File Offset: 0x00013FF4
		public RainCoverage(ManualLogSource log)
		{
			this._log = log;
			for (int i = 0; i < 9216; i++)
			{
				this._cells[i] = -100000f;
			}
		}

		// Token: 0x06000177 RID: 375 RVA: 0x00015E58 File Offset: 0x00014058
		public void Configure(bool enabled)
		{
			this._want = enabled;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x00015E61 File Offset: 0x00014061
		public void ConfigureShelterNeeded(bool needed)
		{
			this._needShelter = needed;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x00015E6C File Offset: 0x0001406C
		public void Tick()
		{
			try
			{
				if (!this._want || (RainSensor.RainFactor <= 0.001f && !this._needShelter))
				{
					Shader.SetGlobalFloat(RainCoverage.CoverageValid, 0f);
				}
				else
				{
					Camera main = Camera.main;
					if (main == null)
					{
						Shader.SetGlobalFloat(RainCoverage.CoverageValid, 0f);
					}
					else
					{
						Vector3 position = main.transform.position;
						float num = Mathf.Floor(position.x / 1f) * 1f - 48f;
						float num2 = Mathf.Floor(position.z / 1f) * 1f - 48f;
						if (num != this._pendingOriginX || num2 != this._pendingOriginZ)
						{
							this._pendingOriginX = num;
							this._pendingOriginZ = num2;
							if (this._sweepRemaining <= 0)
							{
								this._sweepRemaining = 9216;
							}
						}
						float num3 = Mathf.Abs(this._pendingOriginX - this._originX);
						float num4 = Mathf.Abs(this._pendingOriginZ - this._originZ);
						if (this._originPublished && (num3 > 24f || num4 > 24f))
						{
							this._originX = this._pendingOriginX;
							this._originZ = this._pendingOriginZ;
							this._sweepRemaining = 9216;
						}
						if (!this._originPublished)
						{
							this._originX = this._pendingOriginX;
							this._originZ = this._pendingOriginZ;
							this._originPublished = true;
						}
						this.EnsureTexture();
						float num5 = position.y + 120f;
						for (int i = 0; i < 320; i++)
						{
							int cursor = this._cursor;
							this._cursor = (this._cursor + 1) % 9216;
							if (this._sweepRemaining > 0)
							{
								this._sweepRemaining--;
							}
							int num6 = cursor % 96;
							int num7 = cursor / 96;
							float num8 = this._pendingOriginX + ((float)num6 + 0.5f) * 1f;
							float num9 = this._pendingOriginZ + ((float)num7 + 0.5f) * 1f;
							int num10 = Physics.RaycastNonAlloc(new Vector3(num8, num5, num9), Vector3.down, this._hits, 260f, -5, (QueryTriggerInteraction)1);
							float num11 = -100000f;
							float num12 = float.MaxValue;
							for (int j = 0; j < num10; j++)
							{
								if (this._hits[j].distance < num12 && !this.IsFoliage(this._hits[j].collider))
								{
									num12 = this._hits[j].distance;
									num11 = this._hits[j].point.y;
								}
							}
							this._cells[cursor] = num11;
						}
						if (this._sweepRemaining <= 0)
						{
							this._originX = this._pendingOriginX;
							this._originZ = this._pendingOriginZ;
						}
						this._tex.SetPixelData<float>(this._cells, 0, 0);
						this._tex.Apply(false, false);
						Shader.SetGlobalTexture(RainCoverage.CoverageTex, this._tex);
						Shader.SetGlobalVector(RainCoverage.CoverageRect, new Vector4(this._originX, this._originZ, 0.010416667f, 0.010416667f));
						Shader.SetGlobalFloat(RainCoverage.CoverageValid, 1f);
						if (!this._loggedOnce)
						{
							this._loggedOnce = true;
							this._log.LogInfo(string.Format("RainCoverage: world-space roof-height probe online ({0}x{1} @ {2}m = {3}m).", new object[] { 96, 96, 1f, 96f }));
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("RainCoverage tick skipped: " + ex.Message);
				try
				{
					Shader.SetGlobalFloat(RainCoverage.CoverageValid, 0f);
				}
				catch
				{
				}
			}
		}

		// Token: 0x0600017A RID: 378 RVA: 0x00016260 File Offset: 0x00014460
		private static bool NameIsFoliage(string n)
		{
			if (string.IsNullOrEmpty(n))
			{
				return false;
			}
			n = n.ToLowerInvariant();
			return n.Contains("leaf") || n.Contains("leaves") || n.Contains("foliage") || n.Contains("bush") || n.Contains("fern") || n.Contains("ivy") || n.Contains("vine") || n.Contains("shrub") || n.Contains("grass") || n.Contains("frond") || n.Contains("canopy");
		}

		// Token: 0x0600017B RID: 379 RVA: 0x00016314 File Offset: 0x00014514
		private bool IsFoliage(Collider c)
		{
			if (c == null)
			{
				return false;
			}
			bool flag;
			if (this._foliage.TryGetValue(c, out flag))
			{
				return flag;
			}
			bool flag2 = RainCoverage.NameIsFoliage(c.name);
			if (!flag2 && c.transform.parent != null)
			{
				flag2 = RainCoverage.NameIsFoliage(c.transform.parent.name);
			}
			if (!flag2)
			{
				Renderer component = c.GetComponent<Renderer>();
				if (component != null && component.sharedMaterial != null)
				{
					flag2 = RainCoverage.NameIsFoliage(component.sharedMaterial.name);
				}
			}
			if (this._foliage.Count > 4096)
			{
				this._foliage.Clear();
			}
			this._foliage[c] = flag2;
			return flag2;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x000163D4 File Offset: 0x000145D4
		private void EnsureTexture()
		{
			if (this._tex != null)
			{
				return;
			}
			this._tex = new Texture2D(96, 96, (TextureFormat)18, false, true)
			{
				name = "LumaRainCoverage",
				wrapMode = (TextureWrapMode)1,
				filterMode = (FilterMode)1,
				hideFlags = (HideFlags)61
			};
		}

		// Token: 0x0600017D RID: 381 RVA: 0x00016424 File Offset: 0x00014624
		public void Dispose()
		{
			try
			{
				Shader.SetGlobalFloat(RainCoverage.CoverageValid, 0f);
			}
			catch
			{
			}
			if (this._tex != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._tex);
				}
				catch
				{
				}
				this._tex = null;
			}
		}

		// Token: 0x0400032F RID: 815
		private const int GridN = 96;

		// Token: 0x04000330 RID: 816
		private const int CellCount = 9216;

		// Token: 0x04000331 RID: 817
		private const float CellSize = 1f;

		// Token: 0x04000332 RID: 818
		private const float Extent = 96f;

		// Token: 0x04000333 RID: 819
		private const float CastStartAbove = 120f;

		// Token: 0x04000334 RID: 820
		private const float CastDistance = 260f;

		// Token: 0x04000335 RID: 821
		private const int RaysPerTick = 320;

		// Token: 0x04000336 RID: 822
		private const float RainActiveEps = 0.001f;

		// Token: 0x04000337 RID: 823
		private const float NoRoof = -100000f;

		// Token: 0x04000338 RID: 824
		private readonly ManualLogSource _log;

		// Token: 0x04000339 RID: 825
		private readonly float[] _cells = new float[9216];

		// Token: 0x0400033A RID: 826
		private readonly RaycastHit[] _hits = new RaycastHit[24];

		// Token: 0x0400033B RID: 827
		private readonly Dictionary<Collider, bool> _foliage = new Dictionary<Collider, bool>(512);

		// Token: 0x0400033C RID: 828
		private int _cursor;

		// Token: 0x0400033D RID: 829
		private Texture2D _tex;

		// Token: 0x0400033E RID: 830
		private float _originX;

		// Token: 0x0400033F RID: 831
		private float _originZ;

		// Token: 0x04000340 RID: 832
		private float _pendingOriginX;

		// Token: 0x04000341 RID: 833
		private float _pendingOriginZ;

		// Token: 0x04000342 RID: 834
		private int _sweepRemaining;

		// Token: 0x04000343 RID: 835
		private bool _originPublished;

		// Token: 0x04000344 RID: 836
		private bool _want;

		// Token: 0x04000345 RID: 837
		private bool _loggedOnce;

		// Token: 0x04000346 RID: 838
		private static readonly int CoverageTex = Shader.PropertyToID("_LumaRainCoverageTex");

		// Token: 0x04000347 RID: 839
		private static readonly int CoverageRect = Shader.PropertyToID("_LumaRainCoverageRect");

		// Token: 0x04000348 RID: 840
		private static readonly int CoverageValid = Shader.PropertyToID("_LumaRainCoverageValid");

		// Token: 0x04000349 RID: 841
		private bool _needShelter;
	}
}
