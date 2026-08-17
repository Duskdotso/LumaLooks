using System;
using System.Collections.Generic;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Rendering;

namespace LumaLooks
{
	// Token: 0x0200004C RID: 76
	internal sealed class SunlightOccluders
	{
		// Token: 0x060002E0 RID: 736 RVA: 0x00029318 File Offset: 0x00027518
		private void NoteBig(Renderer r, float size, int why)
		{
			int num = -1;
			float num2 = size;
			for (int i = 0; i < 4; i++)
			{
				if (this._bigSize[i] < num2)
				{
					num2 = this._bigSize[i];
					num = i;
				}
			}
			if (num < 0)
			{
				return;
			}
			this._bigR[num] = r;
			this._bigSize[num] = size;
			this._bigWhy[num] = why;
		}

		// Token: 0x060002E1 RID: 737 RVA: 0x0002936C File Offset: 0x0002756C
		private static string WhyName(int why)
		{
			switch (why)
			{
			case 1:
				return "tooSmall";
			case 2:
				return "notOpaque";
			case 3:
				return "outOfRadius";
			case 4:
				return "alreadyCasting";
			case 5:
				return "capEvicted";
			case 6:
				return "ghostNoMesh";
			case 7:
				return "ghostOverBudget";
			default:
				return "?";
			}
		}

		// Token: 0x060002E2 RID: 738 RVA: 0x000293D0 File Offset: 0x000275D0
		internal int RecordPrimeDraws(CommandBuffer cmd, Material mat)
		{
			if (cmd == null || mat == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < this._ghosts.Count; i++)
			{
				Renderer source = this._ghosts[i].Source;
				if (!(source == null) && source.enabled && !source.forceRenderingOff && source.gameObject.activeInHierarchy)
				{
					cmd.DrawRenderer(source, mat, 0, 0);
					num++;
				}
			}
			for (int j = 0; j < this._forced.Count; j++)
			{
				Renderer r = this._forced[j].R;
				if (!(r == null) && r.enabled && !r.forceRenderingOff && r.gameObject.activeInHierarchy)
				{
					cmd.DrawRenderer(r, mat, 0, 0);
					num++;
				}
			}
			return num;
		}

		// Token: 0x060002E3 RID: 739 RVA: 0x000294AC File Offset: 0x000276AC
		internal bool IsCarryingCaster(Renderer r)
		{
			return !(r == null) && (this._ghostSet.Contains(r) || this._forcedSet.Contains(r));
		}

		// Token: 0x060002E4 RID: 740 RVA: 0x000294D5 File Offset: 0x000276D5
		public void AttachGhostShader(Shader s)
		{
			this._ghostShader = s;
		}

		// Token: 0x060002E5 RID: 741 RVA: 0x000294E0 File Offset: 0x000276E0
		public SunlightOccluders(ManualLogSource log)
		{
			this._log = log;
		}

		// Token: 0x060002E6 RID: 742 RVA: 0x000295A5 File Offset: 0x000277A5
		public void AttachSkyDome(SkyDome dome)
		{
			this._dome = dome;
		}

		// Token: 0x060002E7 RID: 743 RVA: 0x000295B0 File Offset: 0x000277B0
		public void Maintain(Type tVRRig, float radius, int tier)
		{
			this._tVRRig = tVRRig;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup < this._nextScanAt)
			{
				return;
			}
			this._nextScanAt = realtimeSinceStartup + 3f * PerfMode.ScanMul;
			try
			{
				this.Scan(radius, tier);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("SunlightOccluders scan skipped: " + ex.Message);
			}
		}

		// Token: 0x060002E8 RID: 744 RVA: 0x00029620 File Offset: 0x00027820
		private void Scan(float radius, int tier)
		{
			int num = ((tier == 0) ? 128 : ((tier == 1) ? 96 : 64));
			Camera main = Camera.main;
			bool flag = main != null;
			Vector3 vector = (flag ? main.transform.position : Vector3.zero);
			float num2 = radius * radius;
			MeshRenderer[] array;
			try
			{
				array = UnityEngine.Object.FindObjectsByType<MeshRenderer>(0);
			}
			catch (Exception ex)
			{
				this._log.LogWarning("SunlightOccluders FindObjectsByType skipped: " + ex.Message);
				return;
			}
			this._candidates.Clear();
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			int num9 = 0;
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			int num13 = 0;
			for (int i = 0; i < 4; i++)
			{
				this._bigR[i] = null;
				this._bigSize[i] = 0f;
				this._bigWhy[i] = 0;
			}
			foreach (MeshRenderer meshRenderer in array)
			{
				if (meshRenderer == null)
				{
					num9++;
				}
				else if (!meshRenderer.enabled)
				{
					num9++;
				}
				else
				{
					GameObject gameObject = meshRenderer.gameObject;
					if (gameObject == null || !gameObject.activeInHierarchy)
					{
						num9++;
					}
					else
					{
						Material sharedMaterial = meshRenderer.sharedMaterial;
						if (sharedMaterial == null)
						{
							num9++;
						}
						else if (sharedMaterial.renderQueue > 2500 && (sharedMaterial.renderQueue >= 3000 || (!sharedMaterial.IsKeywordEnabled("_ALPHATEST_ON") && (!(sharedMaterial.shader != null) || sharedMaterial.shader.name == null || sharedMaterial.shader.name.IndexOf("Cutout", StringComparison.OrdinalIgnoreCase) < 0))))
						{
							num5++;
							Vector3 extents = meshRenderer.bounds.extents;
							this.NoteBig(meshRenderer, Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)), 2);
						}
						else
						{
							Bounds bounds = meshRenderer.bounds;
							Vector3 extents2 = bounds.extents;
							float num14 = Mathf.Max(extents2.x, Mathf.Max(extents2.y, extents2.z));
							if (num14 < 0.8f)
							{
								num3++;
								this.NoteBig(meshRenderer, num14, 1);
							}
							else if (Mathf.Min(extents2.x, extents2.z) >= 100f && !meshRenderer.receiveShadows)
							{
								num4++;
							}
							else
							{
								float num15 = 0f;
								if (flag)
								{
									num15 = bounds.SqrDistance(vector);
									if (num15 > num2)
									{
										num6++;
										this.NoteBig(meshRenderer, num14, 3);
										goto IL_04AB;
									}
								}
								bool flag2 = this._forcedSet.Contains(meshRenderer);
								bool flag3 = false;
								if (!this.HasShadowCasterPass(sharedMaterial))
								{
									if (this._ghostSet.Contains(meshRenderer))
									{
										num12++;
										goto IL_04AB;
									}
									bool flag4 = false;
									try
									{
										flag4 = meshRenderer.isPartOfStaticBatch;
									}
									catch
									{
									}
									if (flag4 && this.IsBatchMeshGhosted(meshRenderer))
									{
										num12++;
										goto IL_04AB;
									}
									if (this._ghostShader == null)
									{
										num13++;
										if (!this._ghostShaderMissingLogged)
										{
											this._ghostShaderMissingLogged = true;
											this._log.LogWarning("SunlightOccluders: ShadowGhost shader not attached (old bundle?) — pass-less GT geometry cannot be given shadow casters, the atlas stays empty of walls.");
											goto IL_04AB;
										}
										goto IL_04AB;
									}
									else
									{
										flag3 = true;
									}
								}
								else if (!flag2 && meshRenderer.shadowCastingMode != null)
								{
									num7++;
									this.NoteBig(meshRenderer, num14, 4);
									goto IL_04AB;
								}
								Shader shader = sharedMaterial.shader;
								if (shader != null)
								{
									string name = shader.name;
									if (name != null && (name.StartsWith("LumaLooks/", StringComparison.Ordinal) || name.StartsWith("Hidden/LumaLooks/", StringComparison.Ordinal)))
									{
										num10++;
										goto IL_04AB;
									}
								}
								if (this.IsTrackedDome(meshRenderer))
								{
									num8++;
								}
								else
								{
									if (this._tVRRig != null)
									{
										Component component = null;
										try
										{
											component = meshRenderer.GetComponentInParent(this._tVRRig);
										}
										catch
										{
										}
										if (component != null)
										{
											num11++;
											goto IL_04AB;
										}
									}
									float num16 = extents2.x * extents2.y * extents2.z;
									float num17 = num14 * num14 / (num15 + 4f);
									if (num14 >= 8f)
									{
										num17 *= 2.5f;
									}
									if (this._winners.Contains(meshRenderer))
									{
										num17 *= 1.6f;
									}
									this._candidates.Add(new SunlightOccluders.Candidate
									{
										R = meshRenderer,
										DistSq = num15,
										Volume = num16,
										Size = num14,
										Score = num17,
										Ghost = flag3
									});
								}
							}
						}
					}
				}
				IL_04AB:;
			}
			this._candidates.Sort(SunlightOccluders._comparer);
			int count = this._candidates.Count;
			int num18 = Mathf.Min(count, num);
			bool flag5 = count > num;
			Renderer renderer = null;
			float num19 = 0f;
			int num20 = 0;
			for (int k = num18; k < count; k++)
			{
				this.NoteBig(this._candidates[k].R, this._candidates[k].Size, 5);
				if (this._candidates[k].Size >= 8f && this._candidates[k].Size > num19)
				{
					renderer = this._candidates[k].R;
					num19 = this._candidates[k].Size;
					num20 = 5;
				}
			}
			this._winners.Clear();
			for (int l = 0; l < num18; l++)
			{
				this._winners.Add(this._candidates[l].R);
			}
			for (int m = this._forced.Count - 1; m >= 0; m--)
			{
				SunlightOccluders.RendererState rendererState = this._forced[m];
				if (rendererState.R == null)
				{
					this._forcedSet.Remove(rendererState.R);
					this._forced.RemoveAt(m);
				}
				else if (!this._winners.Contains(rendererState.R))
				{
					try
					{
						rendererState.R.shadowCastingMode = rendererState.Orig;
					}
					catch
					{
					}
					this._forcedSet.Remove(rendererState.R);
					this._forced.RemoveAt(m);
				}
			}
			for (int n = this._ghosts.Count - 1; n >= 0; n--)
			{
				SunlightOccluders.GhostState ghostState = this._ghosts[n];
				if (ghostState.Source == null || ghostState.Go == null)
				{
					if (ghostState.Go != null)
					{
						try
						{
							UnityEngine.Object.Destroy(ghostState.Go);
						}
						catch
						{
						}
					}
					if (ghostState.BatchMeshId != 0)
					{
						this._batchGhosts.Remove(ghostState.BatchMeshId);
					}
					this._ghostSet.Remove(ghostState.Source);
					this._ghosts.RemoveAt(n);
				}
			}
			int num21 = ((tier == 0) ? 128 : ((tier == 1) ? 48 : 40));
			int num22 = num21 - this._ghosts.Count;
			int num23 = 0;
			int num24 = 0;
			int num25 = 0;
			int num26 = 0;
			int num27 = 8;
			for (int num28 = 0; num28 < num18; num28++)
			{
				SunlightOccluders.Candidate candidate = this._candidates[num28];
				if (candidate.Ghost)
				{
					if (num22 <= 0 && flag && num27 > 0)
					{
						num27--;
						if (this.TryReclaimGhostSlot(vector, num2))
						{
							num22++;
							num26++;
						}
					}
					if (num22 > 0)
					{
						int num29 = this.TryCreateGhost(candidate.R);
						if (num29 > 0)
						{
							num22--;
							num23++;
						}
						else if (num29 < 0)
						{
							num25++;
							this.NoteBig(candidate.R, candidate.Size, 6);
							if (candidate.Size >= 8f && candidate.Size > num19)
							{
								renderer = candidate.R;
								num19 = candidate.Size;
								num20 = 6;
							}
						}
					}
					else
					{
						num24++;
						this.NoteBig(candidate.R, candidate.Size, 7);
						if (candidate.Size >= 8f && candidate.Size > num19)
						{
							renderer = candidate.R;
							num19 = candidate.Size;
							num20 = 7;
						}
					}
				}
				else
				{
					Renderer r = candidate.R;
					if (!this._forcedSet.Contains(r))
					{
						ShadowCastingMode shadowCastingMode;
						try
						{
							shadowCastingMode = r.shadowCastingMode;
							r.shadowCastingMode = (ShadowCastingMode)1;
						}
						catch
						{
							goto IL_08E8;
						}
						this._forced.Add(new SunlightOccluders.RendererState
						{
							R = r,
							Orig = shadowCastingMode
						});
						this._forcedSet.Add(r);
					}
				}
				IL_08E8:;
			}
			this._candidates.Clear();
			this.LogState(this._forced.Count, flag5, count, radius, tier, array.Length, num3, num4, num5, num6, num7, num8, num9, num10, num11, num12, num13, this._ghosts.Count, num23, num24, num21, num25, num26);
			this.MaybeLogLargeSolidReject(renderer, num19, num20);
		}

		// Token: 0x060002E9 RID: 745 RVA: 0x00029FD0 File Offset: 0x000281D0
		private void MaybeLogLargeSolidReject(Renderer r, float size, int why)
		{
			if (r == null || why == 0)
			{
				return;
			}
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			if (realtimeSinceStartup < this._nextBigRejectLogAt)
			{
				return;
			}
			this._nextBigRejectLogAt = realtimeSinceStartup + 60f;
			string text;
			try
			{
				text = r.name;
			}
			catch
			{
				text = "<dead>";
			}
			this._log.LogWarning(string.Concat(new string[]
			{
				"SunlightOccluders: LARGE SOLID WITHOUT A CASTER — '",
				text,
				"' ",
				string.Format("({0:0}m half-extent) was refused by rule '{1}' this ", size, SunlightOccluders.WhyName(why)),
				"scan. If rays are leaking through solid geometry, this is the renderer and the reason (capEvicted = lost its winner slot to higher-scoring candidates; ghostOverBudget = ghost cap pinned and no out-of-range ghost was reclaimable; ghostNoMesh = no usable MeshFilter/sharedMesh to ghost)."
			}));
		}

		// Token: 0x060002EA RID: 746 RVA: 0x0002A074 File Offset: 0x00028274
		private bool HasShadowCasterPass(Material m)
		{
			Shader shader = m.shader;
			if (shader == null)
			{
				return false;
			}
			int instanceID = shader.GetInstanceID();
			bool flag;
			if (this._casterPassCache.TryGetValue(instanceID, out flag))
			{
				return flag;
			}
			try
			{
				flag = m.FindPass("ShadowCaster") >= 0;
			}
			catch
			{
				flag = false;
			}
			this._casterPassCache[instanceID] = flag;
			return flag;
		}

		// Token: 0x060002EB RID: 747 RVA: 0x0002A0E4 File Offset: 0x000282E4
		private bool IsBatchMeshGhosted(Renderer r)
		{
			MeshFilter meshFilter = null;
			try
			{
				meshFilter = r.GetComponent<MeshFilter>();
			}
			catch
			{
			}
			Mesh mesh = ((meshFilter != null) ? meshFilter.sharedMesh : null);
			return mesh != null && this._batchGhosts.ContainsKey(mesh.GetInstanceID());
		}

		// Token: 0x060002EC RID: 748 RVA: 0x0002A140 File Offset: 0x00028340
		private bool TryReclaimGhostSlot(Vector3 center, float radiusSq)
		{
			int num = -1;
			float num2 = radiusSq;
			for (int i = 0; i < this._ghosts.Count; i++)
			{
				Renderer source = this._ghosts[i].Source;
				if (!(source == null))
				{
					float num3;
					try
					{
						num3 = source.bounds.SqrDistance(center);
					}
					catch
					{
						goto IL_0048;
					}
					if (num3 > num2)
					{
						num2 = num3;
						num = i;
					}
				}
				IL_0048:;
			}
			if (num < 0)
			{
				return false;
			}
			SunlightOccluders.GhostState ghostState = this._ghosts[num];
			if (ghostState.Go != null)
			{
				try
				{
					UnityEngine.Object.Destroy(ghostState.Go);
				}
				catch
				{
				}
			}
			if (ghostState.BatchMeshId != 0)
			{
				this._batchGhosts.Remove(ghostState.BatchMeshId);
			}
			this._ghostSet.Remove(ghostState.Source);
			this._ghosts.RemoveAt(num);
			return true;
		}

		// Token: 0x060002ED RID: 749 RVA: 0x0002A230 File Offset: 0x00028430
		private int TryCreateGhost(Renderer src)
		{
			if (src == null)
			{
				return -1;
			}
			if (this._ghostMat == null)
			{
				if (this._ghostShader == null)
				{
					return -1;
				}
				this._ghostMat = new Material(this._ghostShader)
				{
					name = "LumaLooks_ShadowGhost",
					hideFlags = (HideFlags)61
				};
			}
			MeshFilter meshFilter = null;
			try
			{
				meshFilter = src.GetComponent<MeshFilter>();
			}
			catch
			{
			}
			Mesh mesh = ((meshFilter != null) ? meshFilter.sharedMesh : null);
			if (mesh == null)
			{
				return -1;
			}
			bool flag = false;
			try
			{
				flag = src.isPartOfStaticBatch;
			}
			catch
			{
			}
			if (flag)
			{
				int instanceID = mesh.GetInstanceID();
				if (this._batchGhosts.ContainsKey(instanceID))
				{
					this._ghostSet.Add(src);
					return 0;
				}
				try
				{
					GameObject gameObject = new GameObject("LumaLooks_ShadowGhost_Batch");
					gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
					MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
					this.ConfigureGhostRenderer(meshRenderer, mesh);
					this._ghosts.Add(new SunlightOccluders.GhostState
					{
						Source = src,
						Go = gameObject,
						BatchMeshId = instanceID
					});
					this._batchGhosts[instanceID] = gameObject;
					this._ghostSet.Add(src);
					return 1;
				}
				catch
				{
					return -1;
				}
			}
			int num;
			try
			{
				GameObject gameObject2 = new GameObject("LumaLooks_ShadowGhost");
				gameObject2.transform.SetParent(src.transform, false);
				gameObject2.AddComponent<MeshFilter>().sharedMesh = mesh;
				MeshRenderer meshRenderer2 = gameObject2.AddComponent<MeshRenderer>();
				this.ConfigureGhostRenderer(meshRenderer2, mesh);
				this._ghosts.Add(new SunlightOccluders.GhostState
				{
					Source = src,
					Go = gameObject2
				});
				this._ghostSet.Add(src);
				num = 1;
			}
			catch
			{
				num = -1;
			}
			return num;
		}

		// Token: 0x060002EE RID: 750 RVA: 0x0002A424 File Offset: 0x00028624
		private void ConfigureGhostRenderer(MeshRenderer mr, Mesh mesh)
		{
			int subMeshCount = mesh.subMeshCount;
			if (subMeshCount <= 1)
			{
				mr.sharedMaterial = this._ghostMat;
			}
			else
			{
				Material[] array = new Material[subMeshCount];
				for (int i = 0; i < subMeshCount; i++)
				{
					array[i] = this._ghostMat;
				}
				mr.sharedMaterials = array;
			}
			mr.shadowCastingMode = (ShadowCastingMode)3;
			mr.receiveShadows = false;
			mr.lightProbeUsage = 0;
			mr.reflectionProbeUsage = 0;
			mr.motionVectorGenerationMode = (MotionVectorGenerationMode)2;
			mr.allowOcclusionWhenDynamic = false;
		}

		// Token: 0x060002EF RID: 751 RVA: 0x0002A498 File Offset: 0x00028698
		private bool IsTrackedDome(Renderer r)
		{
			if (this._dome == null)
			{
				return false;
			}
			int trackedCount = this._dome.TrackedCount;
			for (int i = 0; i < trackedCount; i++)
			{
				if (this._dome.GetTrackedRenderer(i) == r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0002A4DC File Offset: 0x000286DC
		public void RestoreAll()
		{
			if (this._forced.Count == 0 && this._ghosts.Count == 0)
			{
				this._nextScanAt = 0f;
				return;
			}
			int num = 0;
			for (int i = 0; i < this._forced.Count; i++)
			{
				SunlightOccluders.RendererState rendererState = this._forced[i];
				if (rendererState.R != null)
				{
					try
					{
						rendererState.R.shadowCastingMode = rendererState.Orig;
						num++;
					}
					catch
					{
					}
				}
			}
			this._forced.Clear();
			this._forcedSet.Clear();
			int num2 = 0;
			for (int j = 0; j < this._ghosts.Count; j++)
			{
				SunlightOccluders.GhostState ghostState = this._ghosts[j];
				if (ghostState.Go != null)
				{
					try
					{
						UnityEngine.Object.Destroy(ghostState.Go);
						num2++;
					}
					catch
					{
					}
				}
			}
			this._ghosts.Clear();
			this._ghostSet.Clear();
			this._batchGhosts.Clear();
			this._nextScanAt = 0f;
			this.ResetLogLatch();
			if (num > 0 || num2 > 0)
			{
				this._log.LogInfo(string.Format("SunlightOccluders: RESTORED {0} forced world caster(s) and destroyed {1} ", num, num2) + "shadow ghost(s) (sunlight off / gated off) — every renderer's shadowCastingMode is back exactly as GT shipped it.");
			}
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x0002A644 File Offset: 0x00028844
		public void NotifySceneChanged()
		{
			this._nextScanAt = 0f;
			this.ResetLogLatch();
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0002A657 File Offset: 0x00028857
		private void ResetLogLatch()
		{
			this._loggedForced = -1;
			this._loggedTruncated = -1;
			this._loggedTier = -1;
			this._loggedRes = -1;
			this._loggedGhosts = -1;
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0002A67C File Offset: 0x0002887C
		private void LogState(int forced, bool truncated, int candidates, float radius, int tier, int scanned, int rjSmall, int rjDome, int rjQueue, int rjFar, int rjCasting, int rjExcluded, int rjInactive, int rjOurs, int rjRig, int rjGhosted, int rjNoGhost, int ghostsAlive, int ghostsCreated, int ghostOverBudget, int ghostCap, int ghostFailed, int ghostsReclaimed)
		{
			int num = ((tier == 2) ? 2048 : 4096);
			int num2 = (truncated ? 1 : 0);
			if (forced == this._loggedForced && num2 == this._loggedTruncated && tier == this._loggedTier && num == this._loggedRes && ghostsAlive == this._loggedGhosts)
			{
				return;
			}
			this._loggedForced = forced;
			this._loggedTruncated = num2;
			this._loggedTier = tier;
			this._loggedRes = num;
			this._loggedGhosts = ghostsAlive;
			string text = ((tier == 0) ? "Desktop" : ((tier == 1) ? "VR" : "VR-Balanced"));
			this._log.LogInfo(string.Concat(new string[]
			{
				string.Format("SunlightOccluders: forcedCasters={0} ghosts={1}/{2} ", forced, ghostsAlive, ghostCap),
				string.Format("(+{0} this scan, overBudget={1}, ", ghostsCreated, ghostOverBudget),
				string.Format("failed={0}, reclaimed={1}) (truncated={2}, ", ghostFailed, ghostsReclaimed, truncated),
				string.Format("candidates={0}) within radius={1:0}m tier={2} res={3} ", new object[] { candidates, radius, text, num }),
				string.Format("| scanned={0} rejected: tooSmall={1} domeSized={2} ", scanned, rjSmall, rjDome),
				string.Format("notOpaque={0} outOfRadius={1} alreadyCasting={2} ", rjQueue, rjFar, rjCasting),
				string.Format("trackedDome={0} inactive={1} ourShader={2} ", rjExcluded, rjInactive, rjOurs),
				string.Format("playerRig={0} alreadyGhosted={1} noGhostShader={2} ", rjRig, rjGhosted, rjNoGhost),
				"(sum == scanned by construction — GHOSTS are the 2026-07-24 root-cause fix: GT stripped ShadowCaster from every shader it ships, so ghosts>0 is what 'walls exist in the shadow atlas' actually looks like; alreadyCasting now counts only shaders that REALLY have a ShadowCaster pass)",
				this.BiggestRejectedText()
			}));
		}

		// Token: 0x060002F4 RID: 756 RVA: 0x0002A854 File Offset: 0x00028A54
		private string BiggestRejectedText()
		{
			StringBuilder stringBuilder = new StringBuilder(160);
			stringBuilder.Append(" | biggestRejected: ");
			bool flag = false;
			for (int i = 0; i < 4; i++)
			{
				if (!(this._bigR[i] == null) && this._bigSize[i] > 0f)
				{
					if (flag)
					{
						stringBuilder.Append(", ");
					}
					flag = true;
					string text;
					try
					{
						text = this._bigR[i].name;
					}
					catch
					{
						text = "<dead>";
					}
					stringBuilder.Append(text).Append('|').Append(this._bigSize[i].ToString("0"))
						.Append("m|")
						.Append(SunlightOccluders.WhyName(this._bigWhy[i]));
				}
			}
			if (!flag)
			{
				stringBuilder.Append("(none)");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x040006BB RID: 1723
		private const float RescanSeconds = 3f;

		// Token: 0x040006BC RID: 1724
		private const float MinOccluderExtent = 0.8f;

		// Token: 0x040006BD RID: 1725
		private const int BigWatch = 4;

		// Token: 0x040006BE RID: 1726
		private const int WhySmall = 1;

		// Token: 0x040006BF RID: 1727
		private const int WhyQueue = 2;

		// Token: 0x040006C0 RID: 1728
		private const int WhyFar = 3;

		// Token: 0x040006C1 RID: 1729
		private const int WhyCasting = 4;

		// Token: 0x040006C2 RID: 1730
		private const int WhyCapEvicted = 5;

		// Token: 0x040006C3 RID: 1731
		private const int WhyNoMesh = 6;

		// Token: 0x040006C4 RID: 1732
		private const int WhyGhostBudget = 7;

		// Token: 0x040006C5 RID: 1733
		private readonly Renderer[] _bigR = new Renderer[4];

		// Token: 0x040006C6 RID: 1734
		private readonly float[] _bigSize = new float[4];

		// Token: 0x040006C7 RID: 1735
		private readonly int[] _bigWhy = new int[4];

		// Token: 0x040006C8 RID: 1736
		private const float DomeExtent = 100f;

		// Token: 0x040006C9 RID: 1737
		private const int MaxOpaqueQueue = 2500;

		// Token: 0x040006CA RID: 1738
		private const int CapDesktop = 128;

		// Token: 0x040006CB RID: 1739
		private const int CapVrQuality = 96;

		// Token: 0x040006CC RID: 1740
		private const int CapVrBalanced = 64;

		// Token: 0x040006CD RID: 1741
		internal const int TierDesktop = 0;

		// Token: 0x040006CE RID: 1742
		internal const int TierVrQuality = 1;

		// Token: 0x040006CF RID: 1743
		internal const int TierVrBalanced = 2;

		// Token: 0x040006D0 RID: 1744
		private readonly ManualLogSource _log;

		// Token: 0x040006D1 RID: 1745
		private Type _tVRRig;

		// Token: 0x040006D2 RID: 1746
		private SkyDome _dome;

		// Token: 0x040006D3 RID: 1747
		private readonly List<SunlightOccluders.RendererState> _forced = new List<SunlightOccluders.RendererState>(64);

		// Token: 0x040006D4 RID: 1748
		private readonly HashSet<Renderer> _forcedSet = new HashSet<Renderer>();

		// Token: 0x040006D5 RID: 1749
		private readonly List<SunlightOccluders.GhostState> _ghosts = new List<SunlightOccluders.GhostState>(32);

		// Token: 0x040006D6 RID: 1750
		private readonly HashSet<Renderer> _ghostSet = new HashSet<Renderer>();

		// Token: 0x040006D7 RID: 1751
		private readonly Dictionary<int, GameObject> _batchGhosts = new Dictionary<int, GameObject>(8);

		// Token: 0x040006D8 RID: 1752
		private Shader _ghostShader;

		// Token: 0x040006D9 RID: 1753
		private Material _ghostMat;

		// Token: 0x040006DA RID: 1754
		private bool _ghostShaderMissingLogged;

		// Token: 0x040006DB RID: 1755
		private readonly Dictionary<int, bool> _casterPassCache = new Dictionary<int, bool>(32);

		// Token: 0x040006DC RID: 1756
		private const int GhostCapDesktop = 128;

		// Token: 0x040006DD RID: 1757
		private const int GhostCapVrQuality = 48;

		// Token: 0x040006DE RID: 1758
		private const int GhostCapVrBalanced = 40;

		// Token: 0x040006DF RID: 1759
		private const float LargeSolidExtent = 8f;

		// Token: 0x040006E0 RID: 1760
		private const float CasterIncumbencyBonus = 1.6f;

		// Token: 0x040006E1 RID: 1761
		private const float LargeSolidPriority = 2.5f;

		// Token: 0x040006E2 RID: 1762
		private const int MaxReclaimsPerScan = 8;

		// Token: 0x040006E3 RID: 1763
		private const float BigRejectLogSeconds = 60f;

		// Token: 0x040006E4 RID: 1764
		private float _nextBigRejectLogAt;

		// Token: 0x040006E5 RID: 1765
		private readonly List<SunlightOccluders.Candidate> _candidates = new List<SunlightOccluders.Candidate>(128);

		// Token: 0x040006E6 RID: 1766
		private readonly HashSet<Renderer> _winners = new HashSet<Renderer>();

		// Token: 0x040006E7 RID: 1767
		private static readonly SunlightOccluders.NearestLargestComparer _comparer = new SunlightOccluders.NearestLargestComparer();

		// Token: 0x040006E8 RID: 1768
		private float _nextScanAt;

		// Token: 0x040006E9 RID: 1769
		private int _loggedForced = -1;

		// Token: 0x040006EA RID: 1770
		private int _loggedTruncated = -1;

		// Token: 0x040006EB RID: 1771
		private int _loggedTier = -1;

		// Token: 0x040006EC RID: 1772
		private int _loggedRes = -1;

		// Token: 0x040006ED RID: 1773
		private int _loggedGhosts = -1;

		// Token: 0x0200004D RID: 77
		private struct RendererState
		{
			// Token: 0x040006EE RID: 1774
			public Renderer R;

			// Token: 0x040006EF RID: 1775
			public ShadowCastingMode Orig;
		}

		// Token: 0x0200004E RID: 78
		private struct GhostState
		{
			// Token: 0x040006F0 RID: 1776
			public Renderer Source;

			// Token: 0x040006F1 RID: 1777
			public GameObject Go;

			// Token: 0x040006F2 RID: 1778
			public int BatchMeshId;
		}

		// Token: 0x0200004F RID: 79
		private struct Candidate
		{
			// Token: 0x040006F3 RID: 1779
			public Renderer R;

			// Token: 0x040006F4 RID: 1780
			public float DistSq;

			// Token: 0x040006F5 RID: 1781
			public float Volume;

			// Token: 0x040006F6 RID: 1782
			public float Score;

			// Token: 0x040006F7 RID: 1783
			public float Size;

			// Token: 0x040006F8 RID: 1784
			public bool Ghost;
		}

		// Token: 0x02000050 RID: 80
		private sealed class NearestLargestComparer : IComparer<SunlightOccluders.Candidate>
		{
			// Token: 0x060002F6 RID: 758 RVA: 0x0002A94C File Offset: 0x00028B4C
			public int Compare(SunlightOccluders.Candidate a, SunlightOccluders.Candidate b)
			{
				if (a.Score > b.Score)
				{
					return -1;
				}
				if (a.Score < b.Score)
				{
					return 1;
				}
				if (a.Volume > b.Volume)
				{
					return -1;
				}
				if (a.Volume < b.Volume)
				{
					return 1;
				}
				if (a.DistSq < b.DistSq)
				{
					return -1;
				}
				if (a.DistSq > b.DistSq)
				{
					return 1;
				}
				return 0;
			}
		}
	}
}
