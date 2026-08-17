using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000007 RID: 7
	internal sealed class DynamicLights
	{
		// Token: 0x06000023 RID: 35 RVA: 0x00003724 File Offset: 0x00001924
		public DynamicLights(ManualLogSource log)
		{
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00003810 File Offset: 0x00001A10
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed, float intensity, float range, int maxLights, float particleBoost, float flicker)
		{
			this._enabledWant = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._intensity = Mathf.Clamp(intensity, 0f, 2f);
			this._range = Mathf.Clamp(range, 2f, 20f);
			this._maxLights = Mathf.Clamp(maxLights, 1, 16);
			this._particleBoost = Mathf.Clamp(particleBoost, 1f, 4f);
			this._flicker = Mathf.Clamp01(flicker);
			this._dirty = true;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x0000389C File Offset: 0x00001A9C
		public bool TryGetNearestLightPos(Vector3 refPos, out Vector3 pos)
		{
			pos = Vector3.zero;
			float num = float.MaxValue;
			bool flag = false;
			for (int i = 0; i < this._added.Count; i++)
			{
				Light l = this._added[i].L;
				if (!(l == null))
				{
					float sqrMagnitude = (l.transform.position - refPos).sqrMagnitude;
					if (sqrMagnitude < num)
					{
						num = sqrMagnitude;
						pos = l.transform.position;
						flag = true;
					}
				}
			}
			return flag;
		}

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000026 RID: 38 RVA: 0x00003925 File Offset: 0x00001B25
		public int FireSourceCount
		{
			get
			{
				return this._fireSources.Count;
			}
		}

		// Token: 0x06000027 RID: 39 RVA: 0x00003934 File Offset: 0x00001B34
		public bool TryGetFireSourcePosition(int index, out Vector3 pos)
		{
			pos = Vector3.zero;
			if (index < 0 || index >= this._fireSources.Count)
			{
				return false;
			}
			Transform transform = this._fireSources[index];
			if (transform == null)
			{
				return false;
			}
			pos = transform.position;
			return true;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00003985 File Offset: 0x00001B85
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._scanning = false;
			this._stack.Clear();
			this._candidates.Clear();
			this._fireSources.Clear();
			this._emissionKeywordCache.Clear();
			this._sceneJustLoaded = true;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x000039C4 File Offset: 0x00001BC4
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
				bool flag2 = this._enabledWant && (flag ? this._vrAllowed : this._desktopAllowed);
				if (flag2 != this._wantOn)
				{
					this._wantOn = flag2;
					this._dirty = true;
				}
				if (this._dirty)
				{
					this._dirty = false;
					this.ApplyConfig();
				}
				if (this._wantOn)
				{
					float realtimeSinceStartup = Time.realtimeSinceStartup;
					if (this._sceneJustLoaded)
					{
						this._sceneJustLoaded = false;
						this._nextScanAt = realtimeSinceStartup + 2f;
					}
					if (this._scanning)
					{
						this.StepScan();
					}
					else if (realtimeSinceStartup >= this._nextScanAt)
					{
						this.BeginScan();
					}
					this.UpdateFlicker(realtimeSinceStartup);
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("DynamicLights tick skipped: " + ex.Message);
			}
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00003AB8 File Offset: 0x00001CB8
		private void ApplyConfig()
		{
			if (!this._wantOn)
			{
				this.DestroyAll();
				this._scanning = false;
				this._stack.Clear();
				this._candidates.Clear();
				this._fireSources.Clear();
				this._wasOn = false;
				return;
			}
			for (int i = this._added.Count - 1; i >= 0; i--)
			{
				DynamicLights.AddedLight addedLight = this._added[i];
				if (addedLight.L == null)
				{
					this._added.RemoveAt(i);
				}
				else
				{
					addedLight.L.range = this._range * (addedLight.Fire ? 1.3f : 1f);
					addedLight.L.intensity = this._intensity;
				}
			}
			while (this._added.Count > this._maxLights)
			{
				DynamicLights.AddedLight addedLight2 = this._added[this._added.Count - 1];
				if (addedLight2.L != null)
				{
					this.RevertBoostsFor(addedLight2.L);
					UnityEngine.Object.Destroy(addedLight2.L);
				}
				this._added.RemoveAt(this._added.Count - 1);
			}
			for (int j = this._boosted.Count - 1; j >= 0; j--)
			{
				DynamicLights.BoostedPs boostedPs = this._boosted[j];
				if (boostedPs.Ps == null)
				{
					this._boosted.RemoveAt(j);
				}
				else if (boostedPs.Owner == null)
				{
					DynamicLights.TryRestoreBoost(boostedPs);
					this._boosted.RemoveAt(j);
				}
				else
				{
					this.ApplyBoost(boostedPs.Ps, boostedPs.OrigMult);
				}
			}
			if (!this._wasOn)
			{
				this._nextScanAt = 0f;
			}
			this._wasOn = true;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00003C7C File Offset: 0x00001E7C
		private void UpdateFlicker(float now)
		{
			if (this._flicker <= 0f)
			{
				return;
			}
			for (int i = 0; i < this._added.Count; i++)
			{
				DynamicLights.AddedLight addedLight = this._added[i];
				if (addedLight.Fire && !(addedLight.L == null))
				{
					float num = now + addedLight.Phase;
					float num2 = 0.65f * DynamicLights.ValueNoise(num * 7f) + 0.35f * DynamicLights.ValueNoise(num * 23f + 11.31f);
					float num3 = 1f + (num2 * 2f - 1f) * 0.45f * this._flicker;
					addedLight.L.intensity = this._intensity * Mathf.Max(0.05f, num3);
				}
			}
		}

		// Token: 0x0600002C RID: 44 RVA: 0x00003D4E File Offset: 0x00001F4E
		private static float Hash(float n)
		{
			float num = Mathf.Sin(n * 12.9898f) * 43758.547f;
			return num - Mathf.Floor(num);
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00003D6C File Offset: 0x00001F6C
		private static float ValueNoise(float t)
		{
			float num = Mathf.Floor(t);
			float num2 = t - num;
			num2 = num2 * num2 * (3f - 2f * num2);
			return Mathf.Lerp(DynamicLights.Hash(num), DynamicLights.Hash(num + 1f), num2);
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00003DB0 File Offset: 0x00001FB0
		private void EnsureBoosted(Transform fireRoot, Light owner)
		{
			if (fireRoot == null || owner == null)
			{
				return;
			}
			try
			{
				this._psScratch.Clear();
				fireRoot.GetComponentsInChildren<ParticleSystem>(false, this._psScratch);
				for (int i = 0; i < this._psScratch.Count; i++)
				{
					ParticleSystem particleSystem = this._psScratch[i];
					if (!(particleSystem == null))
					{
						if (this._boosted.Count >= 64)
						{
							break;
						}
						bool flag = false;
						for (int j = 0; j < this._boosted.Count; j++)
						{
							if (this._boosted[j].Ps == particleSystem)
							{
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							ParticleSystem.EmissionModule emission = particleSystem.emission;
							if (emission.enabled)
							{
								float rateOverTimeMultiplier = emission.rateOverTimeMultiplier;
								this._boosted.Add(new DynamicLights.BoostedPs
								{
									Ps = particleSystem,
									Owner = owner,
									OrigMult = rateOverTimeMultiplier
								});
								this.ApplyBoost(particleSystem, rateOverTimeMultiplier);
							}
						}
					}
				}
				this._psScratch.Clear();
			}
			catch (Exception ex)
			{
				this._log.LogWarning("DynamicLights: particle boost skipped: " + ex.Message);
			}
		}

		// Token: 0x0600002F RID: 47 RVA: 0x00003EF8 File Offset: 0x000020F8
		private void ApplyBoost(ParticleSystem ps, float origMult)
		{
			try
			{
				ParticleSystem.EmissionModule emission = ps.emission;
				float num = origMult * this._particleBoost;
				if (num > 300f)
				{
					num = Mathf.Max(origMult, 300f);
				}
				emission.rateOverTimeMultiplier = num;
			}
			catch
			{
			}
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00003F48 File Offset: 0x00002148
		private static void TryRestoreBoost(DynamicLights.BoostedPs b)
		{
			if (b.Ps == null)
			{
				return;
			}
			try
			{
				ParticleSystem.EmissionModule emission = b.Ps.emission;
				emission.rateOverTimeMultiplier = b.OrigMult;
			}
			catch
			{
			}
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00003F94 File Offset: 0x00002194
		private void RevertBoostsFor(Light owner)
		{
			for (int i = this._boosted.Count - 1; i >= 0; i--)
			{
				DynamicLights.BoostedPs boostedPs = this._boosted[i];
				if (boostedPs.Ps == null)
				{
					this._boosted.RemoveAt(i);
				}
				else if (boostedPs.Owner == owner)
				{
					DynamicLights.TryRestoreBoost(boostedPs);
					this._boosted.RemoveAt(i);
				}
			}
		}

		// Token: 0x06000032 RID: 50 RVA: 0x00004000 File Offset: 0x00002200
		private void RevertAllBoosts()
		{
			for (int i = 0; i < this._boosted.Count; i++)
			{
				DynamicLights.TryRestoreBoost(this._boosted[i]);
			}
			this._boosted.Clear();
		}

		// Token: 0x06000033 RID: 51 RVA: 0x00004040 File Offset: 0x00002240
		private void BeginScan()
		{
			this._stack.Clear();
			this._candidates.Clear();
			this._examined = 0;
			Camera main = Camera.main;
			this._scanCamPos = ((main != null) ? main.transform.position : Vector3.zero);
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene sceneAt = SceneManager.GetSceneAt(i);
				if (sceneAt.isLoaded)
				{
					sceneAt.GetRootGameObjects(this._rootsScratch);
					for (int j = 0; j < this._rootsScratch.Count; j++)
					{
						GameObject gameObject = this._rootsScratch[j];
						if (gameObject != null && gameObject.activeInHierarchy)
						{
							this._stack.Push(gameObject.transform);
						}
					}
				}
			}
			this._rootsScratch.Clear();
			for (int k = this._added.Count - 1; k >= 0; k--)
			{
				if (this._added[k].L == null)
				{
					this._added.RemoveAt(k);
				}
			}
			for (int l = this._boosted.Count - 1; l >= 0; l--)
			{
				if (this._boosted[l].Ps == null || this._boosted[l].Owner == null)
				{
					DynamicLights.TryRestoreBoost(this._boosted[l]);
					this._boosted.RemoveAt(l);
				}
			}
			this._scanning = true;
			if (this._stack.Count == 0)
			{
				this.FinishScan();
			}
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000041E0 File Offset: 0x000023E0
		private void StepScan()
		{
			int num = 40;
			while (num-- > 0 && this._stack.Count > 0)
			{
				Transform transform = this._stack.Pop();
				if (!(transform == null))
				{
					this._examined++;
					if (!this.Examine(transform))
					{
						int childCount = transform.childCount;
						for (int i = 0; i < childCount; i++)
						{
							Transform child = transform.GetChild(i);
							if (child.gameObject.activeSelf)
							{
								this._stack.Push(child);
							}
						}
					}
				}
			}
			if (this._stack.Count == 0 || this._examined >= 100000)
			{
				this.FinishScan();
			}
		}

		// Token: 0x06000035 RID: 53 RVA: 0x0000428C File Offset: 0x0000248C
		private bool Examine(Transform t)
		{
			bool flag = DynamicLights.NameMatches(t.name, DynamicLights.NameKeywords);
			bool flag2 = DynamicLights.NameMatches(t.name, DynamicLights.FireKeywords) && !DynamicLights.NameMatches(t.name, DynamicLights.FireRejects);
			if (flag2)
			{
				flag = true;
			}
			Color color = (flag2 ? DynamicLights.FireWarm : DynamicLights.WarmFallback);
			Renderer renderer = null;
			if (t.TryGetComponent<Renderer>(out renderer) && renderer != null)
			{
				try
				{
					Material sharedMaterial = renderer.sharedMaterial;
					if (sharedMaterial != null && sharedMaterial.HasProperty(DynamicLights.EmissionColorId))
					{
						Color color2 = sharedMaterial.GetColor(DynamicLights.EmissionColorId);
						float num = Mathf.Max(color2.r, Mathf.Max(color2.g, color2.b));
						if (num > 0.5f && this.EmissionActive(sharedMaterial))
						{
							flag = true;
							color = new Color(color2.r / num, color2.g / num, color2.b / num, 1f);
						}
					}
				}
				catch
				{
				}
			}
			if (!flag)
			{
				return false;
			}
			Light light = null;
			if (t.TryGetComponent<Light>(out light) && light != null && !this.IsOurs(light))
			{
				return true;
			}
			this.RecordCandidate(t, color, flag2);
			return true;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000043D4 File Offset: 0x000025D4
		private void RecordCandidate(Transform t, Color col, bool fire)
		{
			float sqrMagnitude = (t.position - this._scanCamPos).sqrMagnitude;
			if (this._candidates.Count < 64)
			{
				this._candidates.Add(new DynamicLights.Candidate
				{
					T = t,
					Col = col,
					SqrDist = sqrMagnitude,
					Fire = fire
				});
				return;
			}
			int num = 0;
			for (int i = 1; i < this._candidates.Count; i++)
			{
				if (this._candidates[i].SqrDist > this._candidates[num].SqrDist)
				{
					num = i;
				}
			}
			if (sqrMagnitude < this._candidates[num].SqrDist)
			{
				this._candidates[num] = new DynamicLights.Candidate
				{
					T = t,
					Col = col,
					SqrDist = sqrMagnitude,
					Fire = fire
				};
			}
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000044CC File Offset: 0x000026CC
		private bool EmissionActive(Material mat)
		{
			try
			{
				if ((mat.globalIlluminationFlags & (MaterialGlobalIlluminationFlags)4) != null)
				{
					return false;
				}
				Shader shader = mat.shader;
				if (shader != null)
				{
					bool flag;
					if (!this._emissionKeywordCache.TryGetValue(shader, out flag))
					{
						flag = false;
						string[] keywordNames = shader.keywordSpace.keywordNames;
						for (int i = 0; i < keywordNames.Length; i++)
						{
							if (keywordNames[i] == "_EMISSION")
							{
								flag = true;
								break;
							}
						}
						this._emissionKeywordCache[shader] = flag;
					}
					if (flag && !mat.IsKeywordEnabled("_EMISSION"))
					{
						return false;
					}
				}
			}
			catch
			{
			}
			return true;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x0000457C File Offset: 0x0000277C
		private static bool NameMatches(string name, string[] keywords)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			for (int i = 0; i < keywords.Length; i++)
			{
				if (name.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000045B4 File Offset: 0x000027B4
		private bool IsOurs(Light l)
		{
			for (int i = 0; i < this._added.Count; i++)
			{
				if (this._added[i].L == l)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000045F0 File Offset: 0x000027F0
		private void FinishScan()
		{
			this._scanning = false;
			this._stack.Clear();
			this._nextScanAt = Time.realtimeSinceStartup + 10f * PerfMode.ScanMul;
			Camera main = Camera.main;
			Vector3 vector = ((main != null) ? main.transform.position : Vector3.zero);
			for (int i = 0; i < this._candidates.Count; i++)
			{
				DynamicLights.Candidate candidate = this._candidates[i];
				candidate.SqrDist = ((candidate.T != null) ? (candidate.T.position - vector).sqrMagnitude : float.MaxValue);
				this._candidates[i] = candidate;
			}
			int num = Mathf.Min(this._maxLights, this._candidates.Count);
			for (int j = 0; j < num; j++)
			{
				int num2 = j;
				for (int k = j + 1; k < this._candidates.Count; k++)
				{
					if (this._candidates[k].SqrDist < this._candidates[num2].SqrDist)
					{
						num2 = k;
					}
				}
				if (num2 != j)
				{
					DynamicLights.Candidate candidate2 = this._candidates[j];
					this._candidates[j] = this._candidates[num2];
					this._candidates[num2] = candidate2;
				}
			}
			for (int l = this._added.Count - 1; l >= 0; l--)
			{
				DynamicLights.AddedLight addedLight = this._added[l];
				if (addedLight.L == null)
				{
					if (addedLight.SpotGo != null)
					{
						try
						{
							UnityEngine.Object.Destroy(addedLight.SpotGo);
						}
						catch
						{
						}
					}
					this._added.RemoveAt(l);
				}
				else
				{
					bool flag = false;
					for (int m = 0; m < num; m++)
					{
						if (this._candidates[m].T == addedLight.L.transform)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						this.RevertBoostsFor(addedLight.L);
						if (addedLight.SpotGo != null)
						{
							try
							{
								UnityEngine.Object.Destroy(addedLight.SpotGo);
							}
							catch
							{
							}
						}
						UnityEngine.Object.Destroy(addedLight.L);
						this._added.RemoveAt(l);
					}
				}
			}
			for (int n = 0; n < num; n++)
			{
				DynamicLights.Candidate candidate3 = this._candidates[n];
				if (!(candidate3.T == null))
				{
					int num3 = -1;
					for (int num4 = 0; num4 < this._added.Count; num4++)
					{
						if (this._added[num4].L != null && this._added[num4].L.transform == candidate3.T)
						{
							num3 = num4;
							break;
						}
					}
					Light light;
					if (num3 < 0)
					{
						if (this._added.Count >= this._maxLights)
						{
							break;
						}
						try
						{
							light = candidate3.T.gameObject.AddComponent<Light>();
						}
						catch
						{
							goto IL_0423;
						}
						if (light == null)
						{
							goto IL_0423;
						}
						this._added.Add(new DynamicLights.AddedLight
						{
							L = light,
							Fire = candidate3.Fire,
							Phase = (float)(light.GetInstanceID() & 1023) * 0.618034f
						});
					}
					else
					{
						DynamicLights.AddedLight addedLight2 = this._added[num3];
						addedLight2.Fire = candidate3.Fire;
						this._added[num3] = addedLight2;
						light = addedLight2.L;
					}
					this.ConfigureLight(light, candidate3.Col, candidate3.Fire);
					if (candidate3.Fire)
					{
						this.EnsureBoosted(candidate3.T, light);
					}
					int num5 = ((num3 >= 0) ? num3 : (this._added.Count - 1));
					if (candidate3.Fire)
					{
						this.RemoveLampSpot(num5);
					}
					else
					{
						this.EnsureLampSpot(num5, candidate3.T, candidate3.Col);
					}
				}
				IL_0423:;
			}
			if (this._candidates.Count != this._lastLoggedFound)
			{
				this._lastLoggedFound = this._candidates.Count;
				int num6 = 0;
				this._fireNames.Length = 0;
				for (int num7 = 0; num7 < this._candidates.Count; num7++)
				{
					if (this._candidates[num7].Fire)
					{
						num6++;
						if (this._fireNames.Length < 200 && this._candidates[num7].T != null)
						{
							if (this._fireNames.Length > 0)
							{
								this._fireNames.Append(", ");
							}
							this._fireNames.Append(this._candidates[num7].T.name);
						}
					}
				}
				this._log.LogInfo(string.Format("DynamicLights: {0} emitter candidate(s) found ", this._candidates.Count) + string.Format("({0} transforms examined), lighting {1} (max {2}), ", this._examined, this._added.Count, this._maxLights) + string.Format("{0} fire particle system(s) boosted. ", this._boosted.Count) + string.Format("fire-class={0} [{1}]", num6, this._fireNames));
			}
			if (!this._urpLifted && this._added.Count > 0)
			{
				this._urpLifted = true;
				try
				{
					UniversalRenderPipelineAsset universalRenderPipelineAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
					if (universalRenderPipelineAsset != null)
					{
						this._urpAsset = universalRenderPipelineAsset;
						this._urpOrigMax = universalRenderPipelineAsset.maxAdditionalLightsCount;
						FieldInfo field = typeof(UniversalRenderPipelineAsset).GetField("m_AdditionalLightsRenderingMode", BindingFlags.Instance | BindingFlags.NonPublic);
						if (field != null)
						{
							this._urpModeField = field;
							this._urpOrigMode = field.GetValue(universalRenderPipelineAsset);
							object obj = Enum.Parse(field.FieldType.IsEnum ? field.FieldType : this._urpOrigMode.GetType(), "PerPixel");
							if (!object.Equals(this._urpOrigMode, obj))
							{
								field.SetValue(universalRenderPipelineAsset, obj);
							}
						}
						if (universalRenderPipelineAsset.maxAdditionalLightsCount < 16)
						{
							universalRenderPipelineAsset.maxAdditionalLightsCount = 16;
						}
						this._log.LogInfo("DynamicLights: URP additional lights lifted (mode PerPixel, " + string.Format("budget {0} -> {1}; restored on disable).", this._urpOrigMax, universalRenderPipelineAsset.maxAdditionalLightsCount));
					}
				}
				catch (Exception ex)
				{
					this._log.LogWarning("DynamicLights: URP lift failed: " + ex.Message);
				}
			}
			this._fireSources.Clear();
			int num8 = 0;
			while (num8 < this._candidates.Count && this._fireSources.Count < 16)
			{
				DynamicLights.Candidate candidate4 = this._candidates[num8];
				if (candidate4.Fire && candidate4.T != null)
				{
					this._fireSources.Add(candidate4.T);
				}
				num8++;
			}
			this._candidates.Clear();
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00004D84 File Offset: 0x00002F84
		private void ConfigureLight(Light l, Color col, bool fire)
		{
			l.type = (LightType)2;
			l.shadows = 0;
			l.color = col;
			l.range = this._range * (fire ? 1.3f : 0.65f);
			l.intensity = (fire ? this._intensity : (this._intensity * 0.2f));
		}

		// Token: 0x0600003C RID: 60 RVA: 0x00004DE0 File Offset: 0x00002FE0
		private void EnsureLampSpot(int addedIdx, Transform host, Color col)
		{
			if (addedIdx < 0 || addedIdx >= this._added.Count || host == null)
			{
				return;
			}
			DynamicLights.AddedLight addedLight = this._added[addedIdx];
			if (addedLight.SpotGo == null)
			{
				GameObject gameObject;
				try
				{
					gameObject = new GameObject("LumaLampSpot");
				}
				catch
				{
					return;
				}
				gameObject.transform.SetParent(host, false);
				gameObject.transform.localPosition = Vector3.zero;
				addedLight.SpotGo = gameObject;
				this._added[addedIdx] = addedLight;
			}
			addedLight.SpotGo.transform.rotation = Quaternion.LookRotation(Vector3.down, Vector3.forward);
			Light light;
			try
			{
				light = addedLight.SpotGo.GetComponent<Light>() ?? addedLight.SpotGo.AddComponent<Light>();
			}
			catch
			{
				return;
			}
			if (light == null)
			{
				return;
			}
			light.type = 0;
			light.shadows = 0;
			light.color = col;
			light.range = this._range * 1.2f;
			light.spotAngle = 118f;
			light.innerSpotAngle = 55f;
			light.intensity = this._intensity * 0.85f;
		}

		// Token: 0x0600003D RID: 61 RVA: 0x00004F20 File Offset: 0x00003120
		private void RemoveLampSpot(int addedIdx)
		{
			if (addedIdx < 0 || addedIdx >= this._added.Count)
			{
				return;
			}
			DynamicLights.AddedLight addedLight = this._added[addedIdx];
			if (addedLight.SpotGo != null)
			{
				try
				{
					UnityEngine.Object.Destroy(addedLight.SpotGo);
				}
				catch
				{
				}
				addedLight.SpotGo = null;
				this._added[addedIdx] = addedLight;
			}
		}

		// Token: 0x0600003E RID: 62 RVA: 0x00004F90 File Offset: 0x00003190
		private void DestroyAll()
		{
			this.RevertAllBoosts();
			for (int i = 0; i < this._added.Count; i++)
			{
				GameObject spotGo = this._added[i].SpotGo;
				if (spotGo != null)
				{
					try
					{
						UnityEngine.Object.Destroy(spotGo);
					}
					catch
					{
					}
				}
				Light l = this._added[i].L;
				if (l != null)
				{
					UnityEngine.Object.Destroy(l);
				}
			}
			this._added.Clear();
		}

		// Token: 0x0600003F RID: 63 RVA: 0x0000501C File Offset: 0x0000321C
		private void RestoreUrpBudget()
		{
			if (!this._urpLifted)
			{
				return;
			}
			this._urpLifted = false;
			try
			{
				if (this._urpAsset != null)
				{
					this._urpAsset.maxAdditionalLightsCount = this._urpOrigMax;
					if (this._urpModeField != null && this._urpOrigMode != null)
					{
						this._urpModeField.SetValue(this._urpAsset, this._urpOrigMode);
					}
				}
			}
			catch
			{
			}
			this._urpAsset = null;
			this._urpModeField = null;
			this._urpOrigMode = null;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000050B0 File Offset: 0x000032B0
		public void Dispose()
		{
			this.RestoreUrpBudget();
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			try
			{
				this.DestroyAll();
			}
			catch
			{
			}
			this._scanning = false;
			this._stack.Clear();
			this._candidates.Clear();
			this._fireSources.Clear();
		}

		// Token: 0x04000043 RID: 67
		private const int TransformsPerTick = 40;

		// Token: 0x04000044 RID: 68
		private const int MaxCandidates = 64;

		// Token: 0x04000045 RID: 69
		private const int MaxExaminedPerScan = 100000;

		// Token: 0x04000046 RID: 70
		private const int HardMaxLights = 16;

		// Token: 0x04000047 RID: 71
		private const int MaxBoosted = 64;

		// Token: 0x04000048 RID: 72
		private const int MaxFireSources = 16;

		// Token: 0x04000049 RID: 73
		private const float ScanIntervalSeconds = 10f;

		// Token: 0x0400004A RID: 74
		private const float SceneSettleSeconds = 2f;

		// Token: 0x0400004B RID: 75
		private const float EmissionThreshold = 0.5f;

		// Token: 0x0400004C RID: 76
		private const float FireRangeMul = 1.3f;

		// Token: 0x0400004D RID: 77
		private const float LampDownFrac = 0.85f;

		// Token: 0x0400004E RID: 78
		private const float LampSideFrac = 0.2f;

		// Token: 0x0400004F RID: 79
		private const float LampSpotRangeMul = 1.2f;

		// Token: 0x04000050 RID: 80
		private const float LampSpillRangeMul = 0.65f;

		// Token: 0x04000051 RID: 81
		private const float LampSpotAngle = 118f;

		// Token: 0x04000052 RID: 82
		private const float LampSpotInnerAngle = 55f;

		// Token: 0x04000053 RID: 83
		private const float MaxAbsoluteRate = 300f;

		// Token: 0x04000054 RID: 84
		private const float FlickerAmplitude = 0.45f;

		// Token: 0x04000055 RID: 85
		private static readonly Color WarmFallback = new Color(1f, 0.7058824f, 0.41960785f, 1f);

		// Token: 0x04000056 RID: 86
		private static readonly Color FireWarm = new Color(1f, 0.6039216f, 0.2901961f, 1f);

		// Token: 0x04000057 RID: 87
		private static readonly string[] NameKeywords = new string[] { "lantern", "lamp", "torch", "campfire", "candle", "bulb", "fire" };

		// Token: 0x04000058 RID: 88
		private static readonly string[] FireKeywords = new string[] { "fire", "torch", "flame" };

		// Token: 0x04000059 RID: 89
		private static readonly string[] FireRejects = new string[] { "firefly", "fireflies", "firework" };

		// Token: 0x0400005A RID: 90
		private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

		// Token: 0x0400005B RID: 91
		private readonly ManualLogSource _log;

		// Token: 0x0400005C RID: 92
		private readonly List<GameObject> _rootsScratch = new List<GameObject>(64);

		// Token: 0x0400005D RID: 93
		private readonly Stack<Transform> _stack = new Stack<Transform>(256);

		// Token: 0x0400005E RID: 94
		private readonly List<DynamicLights.Candidate> _candidates = new List<DynamicLights.Candidate>(64);

		// Token: 0x0400005F RID: 95
		private readonly List<DynamicLights.AddedLight> _added = new List<DynamicLights.AddedLight>(16);

		// Token: 0x04000060 RID: 96
		private readonly List<DynamicLights.BoostedPs> _boosted = new List<DynamicLights.BoostedPs>(64);

		// Token: 0x04000061 RID: 97
		private readonly List<ParticleSystem> _psScratch = new List<ParticleSystem>(8);

		// Token: 0x04000062 RID: 98
		private readonly List<Transform> _fireSources = new List<Transform>(16);

		// Token: 0x04000063 RID: 99
		private readonly Dictionary<Shader, bool> _emissionKeywordCache = new Dictionary<Shader, bool>();

		// Token: 0x04000064 RID: 100
		private bool _enabledWant;

		// Token: 0x04000065 RID: 101
		private bool _urpLifted;

		// Token: 0x04000066 RID: 102
		private UniversalRenderPipelineAsset _urpAsset;

		// Token: 0x04000067 RID: 103
		private int _urpOrigMax;

		// Token: 0x04000068 RID: 104
		private FieldInfo _urpModeField;

		// Token: 0x04000069 RID: 105
		private object _urpOrigMode;

		// Token: 0x0400006A RID: 106
		private bool _vrAllowed = true;

		// Token: 0x0400006B RID: 107
		private bool _desktopAllowed = true;

		// Token: 0x0400006C RID: 108
		private bool _wantOn;

		// Token: 0x0400006D RID: 109
		private float _intensity = 1f;

		// Token: 0x0400006E RID: 110
		private float _range = 8f;

		// Token: 0x0400006F RID: 111
		private int _maxLights = 8;

		// Token: 0x04000070 RID: 112
		private float _particleBoost = 2f;

		// Token: 0x04000071 RID: 113
		private readonly StringBuilder _fireNames = new StringBuilder(224);

		// Token: 0x04000072 RID: 114
		private float _flicker = 0.35f;

		// Token: 0x04000073 RID: 115
		private bool _dirty;

		// Token: 0x04000074 RID: 116
		private bool _wasOn;

		// Token: 0x04000075 RID: 117
		private bool _scanning;

		// Token: 0x04000076 RID: 118
		private int _examined;

		// Token: 0x04000077 RID: 119
		private float _nextScanAt;

		// Token: 0x04000078 RID: 120
		private bool _sceneJustLoaded;

		// Token: 0x04000079 RID: 121
		private int _lastLoggedFound = -1;

		// Token: 0x0400007A RID: 122
		private Vector3 _scanCamPos;

		// Token: 0x02000008 RID: 8
		private struct Candidate
		{
			// Token: 0x0400007B RID: 123
			public Transform T;

			// Token: 0x0400007C RID: 124
			public Color Col;

			// Token: 0x0400007D RID: 125
			public float SqrDist;

			// Token: 0x0400007E RID: 126
			public bool Fire;
		}

		// Token: 0x02000009 RID: 9
		private struct AddedLight
		{
			// Token: 0x0400007F RID: 127
			public Light L;

			// Token: 0x04000080 RID: 128
			public bool Fire;

			// Token: 0x04000081 RID: 129
			public float Phase;

			// Token: 0x04000082 RID: 130
			public GameObject SpotGo;
		}

		// Token: 0x0200000A RID: 10
		private struct BoostedPs
		{
			// Token: 0x04000083 RID: 131
			public ParticleSystem Ps;

			// Token: 0x04000084 RID: 132
			public Light Owner;

			// Token: 0x04000085 RID: 133
			public float OrigMult;
		}
	}
}
