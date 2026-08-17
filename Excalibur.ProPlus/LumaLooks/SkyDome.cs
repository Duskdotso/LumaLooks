using System;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using Object = UnityEngine.Object;

namespace LumaLooks
{
	// Token: 0x02000046 RID: 70
	internal sealed class SkyDome
	{
		// Token: 0x0600023B RID: 571 RVA: 0x000247BC File Offset: 0x000229BC
		private static string[] ConcatTokens(string[] a, string[] b)
		{
			string[] array = new string[a.Length + b.Length];
			a.CopyTo(array, 0);
			b.CopyTo(array, a.Length);
			return array;
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x0600023C RID: 572 RVA: 0x000247E9 File Offset: 0x000229E9
		// (set) Token: 0x0600023D RID: 573 RVA: 0x000247F1 File Offset: 0x000229F1
		public bool DomeValid { get; private set; }

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x0600023E RID: 574 RVA: 0x000247FA File Offset: 0x000229FA
		// (set) Token: 0x0600023F RID: 575 RVA: 0x00024802 File Offset: 0x00022A02
		public float DomeDistance { get; private set; }

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000240 RID: 576 RVA: 0x0002480B File Offset: 0x00022A0B
		// (set) Token: 0x06000241 RID: 577 RVA: 0x00024813 File Offset: 0x00022A13
		public float DomeRadius { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000242 RID: 578 RVA: 0x0002481C File Offset: 0x00022A1C
		public int FoundCount
		{
			get
			{
				return this._hitCount;
			}
		}

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x06000243 RID: 579 RVA: 0x00024824 File Offset: 0x00022A24
		// (set) Token: 0x06000244 RID: 580 RVA: 0x0002482C File Offset: 0x00022A2C
		public int HiddenCount { get; private set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x06000245 RID: 581 RVA: 0x0002481C File Offset: 0x00022A1C
		public int TrackedCount
		{
			get
			{
				return this._hitCount;
			}
		}

		// Token: 0x06000246 RID: 582 RVA: 0x00024835 File Offset: 0x00022A35
		public Renderer GetTrackedRenderer(int i)
		{
			if (i >= this._hitCount)
			{
				return null;
			}
			return this._hits[i].R;
		}

		// Token: 0x06000247 RID: 583 RVA: 0x00024854 File Offset: 0x00022A54
		public SkyDome(ManualLogSource log)
		{
			this._log = log;
		}

		// Token: 0x06000248 RID: 584 RVA: 0x000248B0 File Offset: 0x00022AB0
		public void Tick(bool skyOn, float nightWeight, bool night, bool replacementLive)
		{
			if (!skyOn)
			{
				if (this._hitCount > 0 || this._scan != null)
				{
					this.RestoreAll();
				}
				return;
			}
			this._night = night;
			this._replacementLive = replacementLive;
			this._hideWeight = nightWeight;
			if (this._cam == null)
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (this._camWaitUntil < 0f)
				{
					this._camWaitUntil = realtimeSinceStartup + 5f;
				}
				bool flag = realtimeSinceStartup < this._camWaitUntil;
				if (flag || realtimeSinceStartup >= this._nextCamAt)
				{
					if (!flag)
					{
						this._nextCamAt = realtimeSinceStartup + 1f;
					}
					this._cam = Camera.main;
					if (this._cam != null)
					{
						this._rescanWanted = true;
						this._nextScanAt = 0f;
					}
				}
			}
			else
			{
				this._camWaitUntil = -1f;
			}
			string zoneName = MapSense.ZoneName;
			if (!string.Equals(this._zone, zoneName, StringComparison.Ordinal))
			{
				this._zone = zoneName;
				this._fruitless = 0;
				this._nextScanAt = 0f;
				this._rescanWanted = true;
				if (this._scan == null)
				{
					this._scanIdx = 0;
				}
			}
			this.PruneDead();
			this.StepScan();
			bool flag2 = nightWeight >= 0.5f;
			this._wantHidden = flag2;
			this.ApplyHideState();
			this.ResolveDomeDistance();
			this.MaybeLog();
		}

		// Token: 0x06000249 RID: 585 RVA: 0x000249F0 File Offset: 0x00022BF0
		private void StepScan()
		{
			if (this._scan == null)
			{
				if (this._hitCount >= 8)
				{
					return;
				}
				if (this._hitCount > 0 && !this._rescanWanted)
				{
					return;
				}
				if (this._cam == null && this._camWaitUntil >= 0f && Time.realtimeSinceStartup < this._camWaitUntil)
				{
					return;
				}
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (realtimeSinceStartup < this._nextScanAt)
				{
					return;
				}
				this._rescanWanted = false;
				this._nextScanAt = realtimeSinceStartup + ((this._fruitless >= 3) ? 45f : 10f) * PerfMode.ScanMul;
				this._scan = Object.FindObjectsByType<Renderer>(0);
				this._scanIdx = 0;
				this._nearMissCount = 0;
				this._backdropEstimate = 0f;
				return;
			}
			else
			{
				int num = Mathf.Min(this._scanIdx + 128, this._scan.Length);
				for (int i = this._scanIdx; i < num; i++)
				{
					Renderer renderer = this._scan[i];
					this._scan[i] = null;
					if (this._hitCount < 8 && !(renderer == null))
					{
						this.Consider(renderer);
					}
				}
				this._scanIdx = num;
				if (this._scanIdx < this._scan.Length)
				{
					return;
				}
				this._scan = null;
				if (this._hitCount > 0)
				{
					this._fruitless = 0;
					this.DomeValid = true;
					this.ResolveDomeDistance();
				}
				else
				{
					this._fruitless++;
					if (this._fruitless >= 3)
					{
						this.DomeValid = false;
						this.DomeDistance = 0f;
						this.DomeRadius = 0f;
						this.ResolveDomeDistance();
					}
				}
				this._scanJustFinished = true;
				return;
			}
		}

		// Token: 0x0600024A RID: 586 RVA: 0x00024B84 File Offset: 0x00022D84
		private void Consider(Renderer r)
		{
			if (!(r is MeshRenderer))
			{
				return;
			}
			if (!r.enabled)
			{
				return;
			}
			GameObject gameObject = r.gameObject;
			if (gameObject == null || !gameObject.activeInHierarchy)
			{
				return;
			}
			Bounds bounds = r.bounds;
			Vector3 extents = bounds.extents;
			float num = Mathf.Min(extents.x, extents.z);
			if (num < 100f)
			{
				return;
			}
			float num2 = Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z));
			for (int i = 0; i < this._hitCount; i++)
			{
				if (this._hits[i].R == r)
				{
					return;
				}
			}
			string name = gameObject.name;
			Material sharedMaterial = r.sharedMaterial;
			string text = null;
			string text2 = null;
			int num3 = 3000;
			bool flag = false;
			if (sharedMaterial != null)
			{
				text2 = sharedMaterial.name;
				num3 = sharedMaterial.renderQueue;
				Shader shader = sharedMaterial.shader;
				if (shader != null)
				{
					text = shader.name;
				}
				if (sharedMaterial.HasProperty(SkyDome.ZWriteId))
				{
					flag = sharedMaterial.GetFloat(SkyDome.ZWriteId) < 0.5f;
				}
			}
			if (text != null && text.StartsWith("LumaLooks/", StringComparison.Ordinal))
			{
				return;
			}
			string text3 = SkyDome.ShaderLeafName(text);
			bool flag2 = SkyDome.HasAnyToken(name, SkyDome.RejectTokens);
			bool flag3 = SkyDome.HasAnyToken(text2, SkyDome.RejectTokens);
			bool flag4 = SkyDome.HasAnyToken(text3, SkyDome.RigRejectTokens);
			bool flag5 = SkyDome.HasAnyToken(text, SkyDome.NatureRejectTokens);
			if (flag2 || flag3 || flag4 || flag5)
			{
				if (SkyDome.HasAnyToken(name, SkyDome.SkyTokens, true) || SkyDome.HasAnyToken(text2, SkyDome.SkyTokens, true) || SkyDome.HasAnyToken(text3, SkyDome.SkyTokens, true))
				{
					this.NoteNearMiss(name, text2, text, num2, flag2 ? "reject-token(name)" : (flag3 ? "reject-token(material)" : (flag4 ? "reject-token(shaderLeaf)" : "reject-token(shaderPath)")));
				}
				return;
			}
			bool flag6 = SkyDome.HasAnyToken(name, SkyDome.SkyTokens, true) || SkyDome.HasAnyToken(text2, SkyDome.SkyTokens, true) || SkyDome.HasAnyToken(text3, SkyDome.SkyTokens, true);
			bool flag7 = false;
			if (this._cam != null)
			{
				Vector3 position = this._cam.transform.position;
				flag7 = bounds.Contains(position) && bounds.max.y - position.y >= num * 0.3f;
			}
			bool flag8 = num3 <= 2000;
			bool flag9 = num3 < 2000;
			bool flag10 = text != null && (SkyDome.Contains(text, "unlit") || SkyDome.Contains(text, "skybox") || SkyDome.Contains(text, "sky"));
			bool flag11 = num >= 300f;
			bool flag12 = r.shadowCastingMode == null && !r.receiveShadows;
			int num4 = 0;
			if (flag6)
			{
				num4 += 2;
			}
			if (flag7)
			{
				num4++;
			}
			if (flag9)
			{
				num4++;
			}
			if (flag)
			{
				num4++;
			}
			if (flag11)
			{
				num4++;
			}
			if (flag12)
			{
				num4++;
			}
			if (!(flag6 ? (num4 >= 3) : (flag7 && flag8 && (flag || flag10))))
			{
				if (num4 >= 2)
				{
					this.NoteNearMiss(name, text2, text, num2, flag6 ? ("token-but-score" + num4.ToString()) : ("no-token/score" + num4.ToString()));
				}
				return;
			}
			int hitCount = this._hitCount;
			this._hitCount = hitCount + 1;
			int num5 = hitCount;
			this._hits[num5] = new SkyDome.DomeHit
			{
				R = r,
				Center = bounds.center,
				Radius = num,
				MaxExtent = num2,
				Score = num4,
				Hidden = false,
				OrigEnabled = r.enabled,
				Name = name,
				Shader = (text ?? "(no material)")
			};
		}

		// Token: 0x0600024B RID: 587 RVA: 0x00024F84 File Offset: 0x00023184
		private void PruneDead()
		{
			for (int i = this._hitCount - 1; i >= 0; i--)
			{
				Renderer r = this._hits[i].R;
				bool flag = r == null;
				bool flag2 = !flag && !r.gameObject.activeInHierarchy;
				if (flag || flag2)
				{
					if (!flag && this._hits[i].Hidden)
					{
						try
						{
							r.enabled = this._hits[i].OrigEnabled;
						}
						catch
						{
						}
					}
					for (int j = i; j < this._hitCount - 1; j++)
					{
						this._hits[j] = this._hits[j + 1];
					}
					SkyDome.DomeHit[] hits = this._hits;
					int num = this._hitCount - 1;
					this._hitCount = num;
					hits[num] = default(SkyDome.DomeHit);
					this._nextScanAt = 0f;
					this._rescanWanted = true;
					this.DomeValid = this._hitCount > 0 && this.DomeValid;
				}
			}
		}

		// Token: 0x0600024C RID: 588 RVA: 0x000250A8 File Offset: 0x000232A8
		private void ApplyHideState()
		{
			int num = 0;
			for (int i = 0; i < this._hitCount; i++)
			{
				Renderer r = this._hits[i].R;
				if (!(r == null))
				{
					if (this._wantHidden)
					{
						if (!this._hits[i].Hidden)
						{
							this._hits[i].OrigEnabled = r.enabled;
							this._hits[i].Hidden = true;
						}
						if (r.enabled)
						{
							r.enabled = false;
						}
						num++;
					}
					else if (this._hits[i].Hidden)
					{
						r.enabled = this._hits[i].OrigEnabled;
						this._hits[i].Hidden = false;
					}
				}
			}
			this.HiddenCount = num;
		}

		// Token: 0x0600024D RID: 589 RVA: 0x00025188 File Offset: 0x00023388
		private void ResolveDomeDistance()
		{
			if (this._hitCount != 0)
			{
				int num = 0;
				for (int i = 1; i < this._hitCount; i++)
				{
					if (this._hits[i].Radius > this._hits[num].Radius)
					{
						num = i;
					}
				}
				float radius = this._hits[num].Radius;
				this.DomeRadius = radius;
				float num2 = 0f;
				if (this._cam != null)
				{
					Vector3 position = this._cam.transform.position;
					float num3 = position.x - this._hits[num].Center.x;
					float num4 = position.z - this._hits[num].Center.z;
					num2 = Mathf.Sqrt(num3 * num3 + num4 * num4);
				}
				this.DomeDistance = Mathf.Max(Mathf.Max(radius - num2, radius * 0.6f), 50f);
				return;
			}
			if (this._fruitless < 3)
			{
				return;
			}
			this.DomeValid = false;
			this.DomeDistance = 0f;
			this.DomeRadius = 0f;
		}

		// Token: 0x0600024E RID: 590 RVA: 0x000252A8 File Offset: 0x000234A8
		public void RestoreAll()
		{
			int hitCount = this._hitCount;
			int hiddenCount = this.HiddenCount;
			for (int i = 0; i < this._hitCount; i++)
			{
				Renderer r = this._hits[i].R;
				if (r != null && this._hits[i].Hidden)
				{
					try
					{
						r.enabled = this._hits[i].OrigEnabled;
					}
					catch
					{
					}
				}
				this._hits[i] = default(SkyDome.DomeHit);
			}
			this._hitCount = 0;
			this.HiddenCount = 0;
			this._wantHidden = false;
			this._hideWeight = 0f;
			this.DomeValid = false;
			this.DomeDistance = 0f;
			this.DomeRadius = 0f;
			this._scan = null;
			this._scanIdx = 0;
			this._nextScanAt = 0f;
			this._fruitless = 0;
			this._loggedFound = -1;
			if (hitCount > 0)
			{
				this.LogState(false, string.Concat(new string[]
				{
					"(RESTORED — sky off/disposed; ",
					hiddenCount.ToString(),
					" of ",
					hitCount.ToString(),
					" tracked renderers handed back)"
				}));
			}
		}

		// Token: 0x0600024F RID: 591 RVA: 0x000253E8 File Offset: 0x000235E8
		public void NotifySceneChanged()
		{
			for (int i = 0; i < this._hitCount; i++)
			{
				this._hits[i] = default(SkyDome.DomeHit);
			}
			this._hitCount = 0;
			this.HiddenCount = 0;
			this._wantHidden = false;
			this.DomeValid = false;
			this.DomeDistance = 0f;
			this.DomeRadius = 0f;
			this._scan = null;
			this._scanIdx = 0;
			this._nextScanAt = 0f;
			this._fruitless = 0;
			this._cam = null;
			this._camWaitUntil = -1f;
			this._zone = null;
			this._hideWeight = 0f;
			this._loggedOnce = false;
			this._loggedFound = -1;
			this._loggedHiddenCount = -1;
			this._loggedZone = null;
		}

		// Token: 0x06000250 RID: 592 RVA: 0x000254AC File Offset: 0x000236AC
		public void Dispose()
		{
			try
			{
				this.RestoreAll();
			}
			catch
			{
			}
		}

		// Token: 0x06000251 RID: 593 RVA: 0x000254D4 File Offset: 0x000236D4
		private void MaybeLog()
		{
			if (this._scanJustFinished)
			{
				this._scanJustFinished = false;
				this.LogState(true, null);
				return;
			}
			if (this._loggedOnce && this._loggedHidden == this._wantHidden && this._loggedFound == this._hitCount && this._loggedNight == this._night && this._loggedReplacementLive == this._replacementLive && this._loggedHiddenCount == this.HiddenCount && string.Equals(this._loggedZone, this._zone, StringComparison.Ordinal))
			{
				return;
			}
			this.LogState(false, null);
		}

		// Token: 0x06000252 RID: 594 RVA: 0x00025564 File Offset: 0x00023764
		private void LogState(bool scanFinished, string note)
		{
			this._loggedOnce = true;
			this._loggedHidden = this._wantHidden;
			this._loggedFound = this._hitCount;
			this._loggedNight = this._night;
			this._loggedReplacementLive = this._replacementLive;
			this._loggedHiddenCount = this.HiddenCount;
			this._loggedZone = this._zone;
			this._sb.Length = 0;
			this._sb.Append("SKYDOME: night=").Append(this._night ? 1 : 0).Append(" found=")
				.Append(this._hitCount)
				.Append(" hidden=")
				.Append(this.HiddenCount)
				.Append(" hideWeight=")
				.Append(this._hideWeight.ToString("0.00"))
				.Append(" wantHidden=")
				.Append(this._wantHidden ? 1 : 0)
				.Append(" replacementLive=")
				.Append(this._replacementLive ? 1 : 0);
			for (int i = 0; i < this._hitCount; i++)
			{
				this._sb.Append(" [").Append(this._hits[i].Name ?? "?").Append('|')
					.Append(this._hits[i].Shader ?? "?")
					.Append("|ext=")
					.Append(this._hits[i].MaxExtent.ToString("0"))
					.Append("m|r=")
					.Append(this._hits[i].Radius.ToString("0"))
					.Append("m|score=")
					.Append(this._hits[i].Score)
					.Append(']');
			}
			this._sb.Append(" | domeValid=").Append(this.DomeValid ? 1 : 0).Append(" domeDistance=")
				.Append(this.DomeDistance.ToString("0"))
				.Append('m')
				.Append(" radius=")
				.Append(this.DomeRadius.ToString("0"))
				.Append('m')
				.Append(" zone=")
				.Append(this._zone ?? "?");
			if (this._hitCount == 0)
			{
				this._sb.Append(" — NO DOME DETECTED (backdropDistance fallback is in charge; the near-misses below are the candidates that were rejected)");
			}
			for (int j = 0; j < this._nearMissCount; j++)
			{
				this._sb.Append(" nearMiss").Append(j).Append('=')
					.Append(this._nearMiss[j]);
			}
			if (scanFinished)
			{
				this._sb.Append(" (scan complete)");
			}
			if (note != null)
			{
				this._sb.Append(' ').Append(note);
			}
			this._log.LogInfo(this._sb.ToString());
		}

		// Token: 0x06000253 RID: 595 RVA: 0x00025874 File Offset: 0x00023A74
		private void NoteNearMiss(string name, string matName, string shader, float maxExt, string why)
		{
			if (maxExt > this._backdropEstimate)
			{
				this._backdropEstimate = maxExt;
			}
			if (this._nearMissCount >= this._nearMiss.Length)
			{
				return;
			}
			string[] nearMiss = this._nearMiss;
			int nearMissCount = this._nearMissCount;
			this._nearMissCount = nearMissCount + 1;
			nearMiss[nearMissCount] = string.Concat(new string[]
			{
				name ?? "?",
				"|",
				matName ?? "?",
				"|",
				shader ?? "?",
				"|ext=",
				maxExt.ToString("0"),
				"m|",
				why
			});
		}

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x06000254 RID: 596 RVA: 0x00025923 File Offset: 0x00023B23
		internal float BackdropEstimate
		{
			get
			{
				return this._backdropEstimate;
			}
		}

		// Token: 0x06000255 RID: 597 RVA: 0x0002592C File Offset: 0x00023B2C
		private static string ShaderLeafName(string shaderName)
		{
			if (string.IsNullOrEmpty(shaderName))
			{
				return shaderName;
			}
			int num = shaderName.LastIndexOf('/');
			if (num < 0)
			{
				return shaderName;
			}
			return shaderName.Substring(num + 1);
		}

		// Token: 0x06000256 RID: 598 RVA: 0x0002595B File Offset: 0x00023B5B
		private static bool HasAnyToken(string name, string[] tokens)
		{
			return SkyDome.HasAnyToken(name, tokens, false);
		}

		// Token: 0x06000257 RID: 599 RVA: 0x00025968 File Offset: 0x00023B68
		private static bool HasAnyToken(string name, string[] tokens, bool requireEnd)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			for (int i = 0; i < tokens.Length; i++)
			{
				if (SkyDome.HasToken(name, tokens[i], requireEnd))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000258 RID: 600 RVA: 0x0002599C File Offset: 0x00023B9C
		private static bool HasToken(string name, string kw, bool requireEnd)
		{
			int num = 0;
			int num2;
			while ((num2 = name.IndexOf(kw, num, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				if (SkyDome.IsTokenStart(name, num2) && (!requireEnd || SkyDome.IsTokenEnd(name, num2 + kw.Length)))
				{
					return true;
				}
				num = num2 + 1;
			}
			return false;
		}

		// Token: 0x06000259 RID: 601 RVA: 0x000259E0 File Offset: 0x00023BE0
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x0600025A RID: 602 RVA: 0x00025A1C File Offset: 0x00023C1C
		private static bool IsTokenEnd(string s, int end)
		{
			if (end >= s.Length)
			{
				return true;
			}
			char c = s[end];
			return !char.IsLetter(c) || char.IsUpper(c);
		}

		// Token: 0x0600025B RID: 603 RVA: 0x00025A4C File Offset: 0x00023C4C
		private static bool Contains(string s, string kw)
		{
			return s != null && s.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		// Token: 0x040005C7 RID: 1479
		private const int MaxHidden = 8;

		// Token: 0x040005C8 RID: 1480
		private const float MinExtent = 100f;

		// Token: 0x040005C9 RID: 1481
		private const float EnormousExtent = 300f;

		// Token: 0x040005CA RID: 1482
		private const float ShellHeadroom = 0.3f;

		// Token: 0x040005CB RID: 1483
		private const int SliceRenderers = 128;

		// Token: 0x040005CC RID: 1484
		private const float ScanIntervalSeconds = 10f;

		// Token: 0x040005CD RID: 1485
		private const float ScanBackoffSeconds = 45f;

		// Token: 0x040005CE RID: 1486
		private const int FruitlessScans = 3;

		// Token: 0x040005CF RID: 1487
		private const int DomeLostScans = 3;

		// Token: 0x040005D0 RID: 1488
		private const float HideAt = 0.5f;

		// Token: 0x040005D1 RID: 1489
		private const float MinDomeDistance = 50f;

		// Token: 0x040005D2 RID: 1490
		private const float NearFraction = 0.6f;

		// Token: 0x040005D3 RID: 1491
		private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

		// Token: 0x040005D4 RID: 1492
		private static readonly string[] SkyTokens = new string[] { "skydome", "skysphere", "skybox", "sky", "backdrop", "horizon", "cyclorama", "celestial" };

		// Token: 0x040005D5 RID: 1493
		private static readonly string[] RigRejectTokens = new string[] { "player", "vrrig", "gorilla", "monke" };

		// Token: 0x040005D6 RID: 1494
		private static readonly string[] NatureRejectTokens = new string[]
		{
			"water", "ocean", "lake", "river", "lava", "terrain", "ground", "floor", "island", "cliff",
			"rock", "boulder", "tree", "foliage", "cave"
		};

		// Token: 0x040005D7 RID: 1495
		private static readonly string[] RejectTokens = SkyDome.ConcatTokens(SkyDome.RigRejectTokens, SkyDome.NatureRejectTokens);

		// Token: 0x040005D8 RID: 1496
		private readonly ManualLogSource _log;

		// Token: 0x040005D9 RID: 1497
		private readonly SkyDome.DomeHit[] _hits = new SkyDome.DomeHit[8];

		// Token: 0x040005DA RID: 1498
		private int _hitCount;

		// Token: 0x040005DB RID: 1499
		private Renderer[] _scan;

		// Token: 0x040005DC RID: 1500
		private int _scanIdx;

		// Token: 0x040005DD RID: 1501
		private float _nextScanAt;

		// Token: 0x040005DE RID: 1502
		private int _fruitless;

		// Token: 0x040005DF RID: 1503
		private bool _rescanWanted;

		// Token: 0x040005E0 RID: 1504
		private Camera _cam;

		// Token: 0x040005E1 RID: 1505
		private float _nextCamAt;

		// Token: 0x040005E2 RID: 1506
		private const float CamWaitSeconds = 5f;

		// Token: 0x040005E3 RID: 1507
		private float _camWaitUntil = -1f;

		// Token: 0x040005E4 RID: 1508
		private readonly string[] _nearMiss = new string[3];

		// Token: 0x040005E5 RID: 1509
		private int _nearMissCount;

		// Token: 0x040005EA RID: 1514
		private bool _wantHidden;

		// Token: 0x040005EB RID: 1515
		private bool _night;

		// Token: 0x040005EC RID: 1516
		private bool _replacementLive;

		// Token: 0x040005ED RID: 1517
		private float _hideWeight;

		// Token: 0x040005EE RID: 1518
		private string _zone;

		// Token: 0x040005EF RID: 1519
		private bool _loggedOnce;

		// Token: 0x040005F0 RID: 1520
		private bool _scanJustFinished;

		// Token: 0x040005F1 RID: 1521
		private bool _loggedHidden;

		// Token: 0x040005F2 RID: 1522
		private bool _loggedNight;

		// Token: 0x040005F3 RID: 1523
		private bool _loggedReplacementLive;

		// Token: 0x040005F4 RID: 1524
		private int _loggedHiddenCount = -1;

		// Token: 0x040005F5 RID: 1525
		private int _loggedFound = -1;

		// Token: 0x040005F6 RID: 1526
		private string _loggedZone;

		// Token: 0x040005F7 RID: 1527
		private readonly StringBuilder _sb = new StringBuilder(256);

		// Token: 0x040005F8 RID: 1528
		private float _backdropEstimate;

		// Token: 0x02000047 RID: 71
		private struct DomeHit
		{
			// Token: 0x040005F9 RID: 1529
			public Renderer R;

			// Token: 0x040005FA RID: 1530
			public Vector3 Center;

			// Token: 0x040005FB RID: 1531
			public float Radius;

			// Token: 0x040005FC RID: 1532
			public float MaxExtent;

			// Token: 0x040005FD RID: 1533
			public int Score;

			// Token: 0x040005FE RID: 1534
			public bool Hidden;

			// Token: 0x040005FF RID: 1535
			public bool OrigEnabled;

			// Token: 0x04000600 RID: 1536
			public string Name;

			// Token: 0x04000601 RID: 1537
			public string Shader;
		}
	}
}
