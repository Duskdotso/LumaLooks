using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace LumaLooks
{
	// Token: 0x02000016 RID: 22
	internal sealed class MetalSurfaces
	{
		// Token: 0x060000BE RID: 190 RVA: 0x0000A236 File Offset: 0x00008436
		internal static float EncodeR(float r)
		{
			return 0.4f + r / 10f * 0.6f;
		}

		// Token: 0x060000BF RID: 191 RVA: 0x0000A24C File Offset: 0x0000844C
		private static float[] BuildMaskTierValues()
		{
			float[] array = new float[10];
			for (int i = 0; i < MetalSurfaces.RTable.Length; i++)
			{
				array[i] = MetalSurfaces.EncodeR(MetalSurfaces.RTable[i]);
			}
			array[8] = 0.08f;
			array[9] = 0f;
			return array;
		}

		// Token: 0x060000C0 RID: 192 RVA: 0x0000A294 File Offset: 0x00008494
		private static string[] BuildRLabels()
		{
			string[] array = new string[MetalSurfaces.RTable.Length];
			for (int i = 0; i < MetalSurfaces.RTable.Length; i++)
			{
				array[i] = "R" + MetalSurfaces.RTable[i].ToString("0.##", CultureInfo.InvariantCulture);
			}
			return array;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x0000A2E8 File Offset: 0x000084E8
		private static List<Renderer>[] NewBuckets()
		{
			List<Renderer>[] array = new List<Renderer>[10];
			for (int i = 0; i < 10; i++)
			{
				array[i] = new List<Renderer>((i == 9) ? 64 : 32);
			}
			return array;
		}

		// Token: 0x060000C2 RID: 194 RVA: 0x0000A320 File Offset: 0x00008520
		public MetalSurfaces(ManualLogSource log)
		{
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x060000C3 RID: 195 RVA: 0x0000A3E4 File Offset: 0x000085E4
		public bool HasMasked
		{
			get
			{
				for (int i = 0; i < 10; i++)
				{
					if (this._buckets[i].Count > 0)
					{
						return true;
					}
				}
				return false;
			}
		}

		// Token: 0x060000C4 RID: 196 RVA: 0x0000A411 File Offset: 0x00008611
		public void Configure(bool want)
		{
			if (want && !this._want)
			{
				this._nextScanAt = 0f;
				this._emptyScans = 0;
			}
			this._want = want;
			this._dirty = true;
		}

		// Token: 0x060000C5 RID: 197 RVA: 0x0000A440 File Offset: 0x00008640
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._scanning = false;
			this._stack.Clear();
			this._sceneJustLoaded = true;
			this._emptyScans = 0;
			this._seenNames.Clear();
			this._unmappedLogged = 0;
			this._unmappedSuppressLogged = false;
			this._matCache.Clear();
			if (!this._tableResolved)
			{
				this._tableGaveUp = false;
				this._tableAttempts = 0;
			}
		}

		// Token: 0x060000C6 RID: 198 RVA: 0x0000A4A8 File Offset: 0x000086A8
		public void Tick()
		{
			try
			{
				if (this._dirty)
				{
					this._dirty = false;
					if (!this._want)
					{
						this.DropAll();
					}
				}
				if (this._want && !this._wantPrev)
				{
					this._nextScanAt = 0f;
					this._emptyScans = 0;
				}
				this._wantPrev = this._want;
				if (this._want)
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
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("MetalSurfaces tick skipped: " + ex.Message);
			}
		}

		// Token: 0x060000C7 RID: 199 RVA: 0x0000A57C File Offset: 0x0000877C
		private void DropAll()
		{
			this._scanning = false;
			this._stack.Clear();
			for (int i = 0; i < 10; i++)
			{
				this._buckets[i].Clear();
				this._scanBuckets[i].Clear();
			}
		}

		// Token: 0x060000C8 RID: 200 RVA: 0x0000A5C4 File Offset: 0x000087C4
		private void EnsureReflection()
		{
			if (this._reflectResolved)
			{
				return;
			}
			this._reflectResolved = true;
			try
			{
				this._tSurf = MetalSurfaces.FindType("GorillaSurfaceOverride");
				this._tVRRig = MetalSurfaces.FindType("VRRig");
				if (this._tSurf != null)
				{
					this._fiOverrideIndex = this._tSurf.GetField("overrideIndex", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				}
				Type type;
				if ((type = MetalSurfaces.FindType("GTPlayer")) == null)
				{
					type = MetalSurfaces.FindType("GorillaLocomotion.GTPlayer") ?? MetalSurfaces.FindType("Player");
				}
				this._tGTPlayer = type;
				if (this._tGTPlayer != null)
				{
					PropertyInfo propertyInfo;
					if ((propertyInfo = this._tGTPlayer.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) == null)
					{
						propertyInfo = this._tGTPlayer.GetProperty("Instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) ?? this._tGTPlayer.GetProperty("_instance", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					}
					this._mGTInstance = propertyInfo;
					this._fiPlayerSO = this._tGTPlayer.GetField("materialDatasSO", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (this._fiPlayerSO != null && this._fiPlayerSO.FieldType != null)
					{
						this._fiSODatas = this._fiPlayerSO.FieldType.GetField("datas", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					}
					FieldInfo field = this._tGTPlayer.GetField("iceThreshold", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					if (field != null && field.FieldType == typeof(float))
					{
						this._fiIceThreshold = field;
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("MetalSurfaces reflection failed: " + ex.Message);
			}
		}

		// Token: 0x060000C9 RID: 201 RVA: 0x0000A774 File Offset: 0x00008974
		private void LogReflectionLine(int rows, bool ok)
		{
			if (ok)
			{
				if (this._loggedReflectOk)
				{
					return;
				}
				this._loggedReflectOk = true;
			}
			else
			{
				if (this._loggedReflectFail)
				{
					return;
				}
				this._loggedReflectFail = true;
			}
			string text = string.Format("MetalSurfaces reflection: GorillaSurfaceOverride={0} ", this._tSurf != null) + string.Format("overrideIndex={0} GTPlayer={1} ", this._fiOverrideIndex != null, this._tGTPlayer != null) + string.Format("Instance={0} materialDatasSO={1} ", this._mGTInstance != null, this._fiPlayerSO != null) + string.Format("SO.datas={0} rows={1} VRRig={2}", this._fiSODatas != null, rows, this._tVRRig != null);
			if (ok)
			{
				this._log.LogInfo(text);
				return;
			}
			this._log.LogWarning(text + " — surface table UNAVAILABLE; falling back to renderer shared-material names only (untagged where that also fails, i.e. today's look).");
		}

		// Token: 0x060000CA RID: 202 RVA: 0x0000A878 File Offset: 0x00008A78
		private void ResolveMatDataMembers(Type t)
		{
			if (this._matDataMembersResolved || t == null)
			{
				return;
			}
			this._matDataMembersResolved = true;
			try
			{
				this._fiMatName = t.GetField("matName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				this._miTrimmedName = t.GetMethod("GetTrimmedMaterialName", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
				foreach (FieldInfo fieldInfo in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
				{
					if (this._fiSlip == null && fieldInfo.FieldType == typeof(float) && (fieldInfo.Name.StartsWith("slip", StringComparison.OrdinalIgnoreCase) || fieldInfo.Name.StartsWith("slide", StringComparison.OrdinalIgnoreCase)))
					{
						this._fiSlip = fieldInfo;
					}
					if (this._fiSlipOverride == null && fieldInfo.FieldType == typeof(bool) && fieldInfo.Name.IndexOf("slide", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						this._fiSlipOverride = fieldInfo;
					}
				}
				this._log.LogInfo(string.Concat(new string[]
				{
					string.Format("MetalSurfaces MaterialData members: matName={0} ", this._fiMatName != null),
					string.Format("GetTrimmedMaterialName={0} ", this._miTrimmedName != null),
					"slipField=",
					(this._fiSlip != null) ? this._fiSlip.Name : "none",
					" slipGate=",
					(this._fiSlipOverride != null) ? this._fiSlipOverride.Name : "none",
					" slipThreshold=",
					this._slipThreshold.ToString("0.###", CultureInfo.InvariantCulture),
					(this._fiIceThreshold != null) ? " (GTPlayer.iceThreshold)" : " (fallback)"
				}));
			}
			catch (Exception ex)
			{
				this._log.LogWarning("MetalSurfaces MaterialData reflect failed: " + ex.Message);
			}
		}

		// Token: 0x060000CB RID: 203 RVA: 0x0000AAA0 File Offset: 0x00008CA0
		private void EnsureSurfaceTable()
		{
			if (this._tableResolved || this._tableGaveUp)
			{
				return;
			}
			this._tableAttempts++;
			try
			{
				object obj = null;
				PropertyInfo propertyInfo = this._mGTInstance as PropertyInfo;
				if (propertyInfo != null)
				{
					obj = propertyInfo.GetValue(null, null);
				}
				else
				{
					FieldInfo fieldInfo = this._mGTInstance as FieldInfo;
					if (fieldInfo != null)
					{
						obj = fieldInfo.GetValue(null);
					}
				}
				Object @object = obj as Object;
				if (@object != null && @object == null)
				{
					obj = null;
				}
				if (obj != null && this._fiIceThreshold != null)
				{
					try
					{
						float num = Convert.ToSingle(this._fiIceThreshold.GetValue(obj));
						if (num > 0f && num <= 1f)
						{
							this._slipThreshold = num;
						}
					}
					catch
					{
					}
				}
				IList list = null;
				if (obj != null && this._fiPlayerSO != null && this._fiSODatas != null)
				{
					object obj2 = this._fiPlayerSO.GetValue(obj);
					Object object2 = obj2 as Object;
					if (object2 != null && object2 == null)
					{
						obj2 = null;
					}
					if (obj2 != null)
					{
						list = this._fiSODatas.GetValue(obj2) as IList;
					}
				}
				if (list == null)
				{
					if (this._tableAttempts >= 8)
					{
						this._tableGaveUp = true;
						this.LogReflectionLine(0, false);
					}
				}
				else
				{
					int count = list.Count;
					this._rowName = new string[count];
					this._rowBucket = new int[count];
					for (int i = 0; i < count; i++)
					{
						string text = null;
						float num2 = -1f;
						object obj3 = list[i];
						if (obj3 != null)
						{
							this.ResolveMatDataMembers(obj3.GetType());
							if (this._fiMatName != null)
							{
								text = this._fiMatName.GetValue(obj3) as string;
							}
							if (string.IsNullOrEmpty(text) && this._miTrimmedName != null)
							{
								text = this._miTrimmedName.Invoke(obj3, null) as string;
							}
							if (this._fiSlip != null)
							{
								bool flag = true;
								if (this._fiSlipOverride != null)
								{
									try
									{
										flag = Convert.ToBoolean(this._fiSlipOverride.GetValue(obj3));
									}
									catch
									{
									}
								}
								if (flag)
								{
									try
									{
										num2 = Convert.ToSingle(this._fiSlip.GetValue(obj3));
									}
									catch
									{
									}
								}
							}
						}
						text = MetalSurfaces.StripUber(text);
						this._rowName[i] = text;
						this._rowBucket[i] = this.ClassifyName(text, num2);
					}
					this._tableResolved = true;
					this.LogReflectionLine(count, true);
					this.LogSurfacesDump(count);
				}
			}
			catch (Exception ex)
			{
				if (this._tableAttempts >= 8)
				{
					this._tableGaveUp = true;
					this._log.LogWarning("MetalSurfaces: surface table resolve failed: " + ex.Message);
					this.LogReflectionLine(0, false);
				}
			}
		}

		// Token: 0x060000CC RID: 204 RVA: 0x0000ADBC File Offset: 0x00008FBC
		private void LogSurfacesDump(int n)
		{
			try
			{
				this._sb.Length = 0;
				this._sb.Append("MetalSurfaces: SURFACES: n=").Append(n).Append(" [");
				for (int i = 0; i < n; i++)
				{
					if (i > 0)
					{
						this._sb.Append(", ");
					}
					this._sb.Append(string.IsNullOrEmpty(this._rowName[i]) ? "<null>" : this._rowName[i]).Append('|');
					int num = this._rowBucket[i];
					if (num < 0)
					{
						this._sb.Append('?');
					}
					else
					{
						this._sb.Append(MetalSurfaces.RLabels[num], 1, MetalSurfaces.RLabels[num].Length - 1);
					}
				}
				this._sb.Append(']');
				this._log.LogInfo(this._sb.ToString());
				this._sb.Length = 0;
			}
			catch
			{
			}
		}

		// Token: 0x060000CD RID: 205 RVA: 0x0000AED4 File Offset: 0x000090D4
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
					goto IL_0092;
				}
				goto IL_0056;
				IL_0092:
				j++;
				continue;
				IL_0056:
				if (array != null)
				{
					for (int k = 0; k < array.Length; k++)
					{
						if (array[k] != null && array[k].Name == name)
						{
							return array[k];
						}
					}
					goto IL_0092;
				}
				goto IL_0092;
			}
			return null;
		}

		// Token: 0x060000CE RID: 206 RVA: 0x0000AFAC File Offset: 0x000091AC
		private void BeginScan()
		{
			this.EnsureReflection();
			this.EnsureSurfaceTable();
			this._stack.Clear();
			this._examined = 0;
			this._scanCount = 0;
			this._excludeCount = 0;
			this._surfaceCount = 0;
			this._excludeTruncated = false;
			this._surfaceTruncated = false;
			for (int i = 0; i < 10; i++)
			{
				this._scanBuckets[i].Clear();
			}
			if (!this._usedLogged)
			{
				this._usedCounts.Clear();
			}
			for (int j = 0; j < SceneManager.sceneCount; j++)
			{
				Scene sceneAt = SceneManager.GetSceneAt(j);
				if (sceneAt.isLoaded)
				{
					sceneAt.GetRootGameObjects(this._rootsScratch);
					for (int k = 0; k < this._rootsScratch.Count; k++)
					{
						GameObject gameObject = this._rootsScratch[k];
						if (gameObject != null && gameObject.activeInHierarchy)
						{
							this._stack.Push(gameObject.transform);
						}
					}
				}
			}
			this._rootsScratch.Clear();
			this._scanning = true;
			if (this._stack.Count == 0)
			{
				this.FinishScan();
			}
		}

		// Token: 0x060000CF RID: 207 RVA: 0x0000B0C4 File Offset: 0x000092C4
		private void StepScan()
		{
			int num = 48;
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
			if (this._stack.Count == 0 || this._examined >= 200000 || (this._excludeCount >= 256 && this._surfaceCount >= 128))
			{
				this.FinishScan();
			}
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x0000B18C File Offset: 0x0000938C
		private bool Examine(Transform t)
		{
			if (this._excludeCount >= 256 && this._surfaceCount >= 128)
			{
				return true;
			}
			if (this._tVRRig != null)
			{
				try
				{
					if (t.GetComponent(this._tVRRig) != null)
					{
						this.AddSubtreeExclude(t);
						return true;
					}
				}
				catch
				{
				}
			}
			if (MetalSurfaces.NameMatchesAny(t.name, MetalSurfaces.FoliageKeywords))
			{
				Renderer renderer = null;
				if (t.TryGetComponent<Renderer>(out renderer) && renderer != null)
				{
					this.AddBucket(8, renderer);
				}
				return false;
			}
			Renderer renderer2 = null;
			if (!t.TryGetComponent<Renderer>(out renderer2) || renderer2 == null)
			{
				return false;
			}
			if (!(renderer2 is MeshRenderer) && !(renderer2 is SkinnedMeshRenderer))
			{
				return false;
			}
			string text = null;
			int num = -1;
			if (this._tSurf != null && this._fiOverrideIndex != null && this._rowBucket != null)
			{
				object obj = null;
				try
				{
					obj = t.GetComponent(this._tSurf);
				}
				catch
				{
				}
				if (obj != null)
				{
					try
					{
						int num2 = Convert.ToInt32(this._fiOverrideIndex.GetValue(obj));
						if (num2 >= 0 && num2 < this._rowBucket.Length)
						{
							num = this._rowBucket[num2];
							if (num >= 0)
							{
								text = this._rowName[num2];
							}
						}
					}
					catch
					{
					}
				}
			}
			if (num < 0)
			{
				num = this.ClassifyRenderer(renderer2, out text);
			}
			if (num < 0)
			{
				return false;
			}
			if (this.AddBucket(num, renderer2) && !this._usedLogged && text != null)
			{
				this.CountUsed(text, num);
			}
			return false;
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000B324 File Offset: 0x00009524
		private void CountUsed(string name, int bucket)
		{
			MetalSurfaces.UsedRow usedRow;
			this._usedCounts.TryGetValue(name, out usedRow);
			usedRow.Count++;
			usedRow.Bucket = bucket;
			this._usedCounts[name] = usedRow;
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x0000B364 File Offset: 0x00009564
		private int ClassifyRenderer(Renderer r, out string usedName)
		{
			usedName = null;
			try
			{
				this._matScratch.Clear();
				r.GetSharedMaterials(this._matScratch);
				if (this._matScratch.Count > 0)
				{
					Material material = this._matScratch[0];
					if (material != null)
					{
						int instanceID = material.GetInstanceID();
						MetalSurfaces.MatClass matClass;
						if (!this._matCache.TryGetValue(instanceID, out matClass))
						{
							string text = MetalSurfaces.StripUber(material.name);
							matClass = new MetalSurfaces.MatClass
							{
								Bucket = this.ClassifyName(text, -1f),
								Name = text
							};
							if (this._matCache.Count < 8192)
							{
								this._matCache[instanceID] = matClass;
							}
							if (matClass.Bucket < 0)
							{
								this.NoteUnmapped(text, -1f);
							}
						}
						if (matClass.Bucket >= 0)
						{
							usedName = matClass.Name;
							return matClass.Bucket;
						}
					}
				}
			}
			catch
			{
			}
			finally
			{
				this._matScratch.Clear();
			}
			return -1;
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x0000B480 File Offset: 0x00009680
		private static string StripUber(string n)
		{
			if (string.IsNullOrEmpty(n))
			{
				return n;
			}
			if (!n.EndsWith("Uber", StringComparison.Ordinal))
			{
				return n;
			}
			return n.Substring(0, n.Length - 4);
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x0000B4AC File Offset: 0x000096AC
		private void AddSubtreeExclude(Transform root)
		{
			try
			{
				this._rendererScratch.Clear();
				root.GetComponentsInChildren<Renderer>(false, this._rendererScratch);
				for (int i = 0; i < this._rendererScratch.Count; i++)
				{
					Renderer renderer = this._rendererScratch[i];
					if (renderer != null && !this.AddChecked(this._scanBuckets[9], renderer, true))
					{
						break;
					}
				}
				this._rendererScratch.Clear();
			}
			catch
			{
			}
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x0000B530 File Offset: 0x00009730
		private bool AddBucket(int bucket, Renderer r)
		{
			return this.AddChecked(this._scanBuckets[bucket], r, bucket == 9 || bucket == 8);
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x0000B550 File Offset: 0x00009750
		private bool AddChecked(List<Renderer> list, Renderer r, bool isExclude)
		{
			if (isExclude)
			{
				if (this._excludeCount >= 256)
				{
					this._excludeTruncated = true;
					return false;
				}
				this._excludeCount++;
			}
			else
			{
				if (this._surfaceCount >= 128)
				{
					this._surfaceTruncated = true;
					return false;
				}
				this._surfaceCount++;
			}
			list.Add(r);
			this._scanCount++;
			return true;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x0000B5C0 File Offset: 0x000097C0
		private int ClassifyName(string name, float slip)
		{
			if (!string.IsNullOrEmpty(name))
			{
				bool flag = MetalSurfaces.NameMatchesAny(name, MetalSurfaces.IceVetoKeywords);
				for (int i = 0; i < MetalSurfaces.SpecificKeywords.Length; i++)
				{
					if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.SpecificKeywords[i]))
					{
						return MetalSurfaces.SpecificBuckets[i];
					}
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.MetalKeywords))
				{
					return 7;
				}
				if (!flag && MetalSurfaces.NameMatchesAny(name, MetalSurfaces.IceKeywords))
				{
					return 6;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.PolishedKeywords))
				{
					return 5;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.StoneKeywords))
				{
					return 4;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.ConcreteKeywords))
				{
					return 3;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.WoodKeywords))
				{
					return 2;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.GrassKeywords))
				{
					return 1;
				}
				if (MetalSurfaces.NameMatchesAny(name, MetalSurfaces.SoftKeywords))
				{
					return 0;
				}
			}
			if (slip > this._slipThreshold)
			{
				return 6;
			}
			return -1;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x0000B694 File Offset: 0x00009894
		private void NoteUnmapped(string name, float slip)
		{
			if (string.IsNullOrEmpty(name))
			{
				return;
			}
			if (!this._seenNames.Add(name))
			{
				return;
			}
			if (this._unmappedLogged >= 192)
			{
				if (!this._unmappedSuppressLogged)
				{
					this._unmappedSuppressLogged = true;
					this._log.LogInfo("MetalSurfaces: SURFACE UNMAPPED: further distinct names suppressed " + string.Format("after {0} (see the SURFACES dump for the authoritative table).", 192));
				}
				return;
			}
			this._unmappedLogged++;
			this._log.LogInfo(string.Concat(new string[]
			{
				"MetalSurfaces: SURFACE UNMAPPED: '",
				name,
				"'",
				(slip >= 0f) ? string.Format(" slip={0:0.###}", slip) : "",
				" -> untagged"
			}));
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x0000B764 File Offset: 0x00009964
		private void FinishScan()
		{
			this._scanning = false;
			this._stack.Clear();
			int num = 0;
			for (int i = 0; i < 10; i++)
			{
				List<Renderer> list = this._buckets[i];
				this._buckets[i] = this._scanBuckets[i];
				this._scanBuckets[i] = list;
				this._scanBuckets[i].Clear();
				num += this._buckets[i].Count;
			}
			this._emptyScans = ((num == 0) ? (this._emptyScans + 1) : 0);
			this._nextScanAt = Time.realtimeSinceStartup + ((this._emptyScans >= 2) ? 30f : 10f) * PerfMode.ScanMul;
			this._scansCompleted++;
			this._sb.Length = 0;
			this._sb.Append("MetalSurfaces: tiers");
			for (int j = 0; j < MetalSurfaces.RTable.Length; j++)
			{
				this._sb.Append(' ').Append(MetalSurfaces.RLabels[j]).Append('=')
					.Append(this._buckets[j].Count);
			}
			this._sb.Append(" foliage=").Append(this._buckets[8].Count).Append(" exclude=")
				.Append(this._buckets[9].Count)
				.Append(" truncated=")
				.Append(this._excludeTruncated)
				.Append(" surfTruncated=")
				.Append(this._surfaceTruncated)
				.Append(" (")
				.Append(this._examined)
				.Append(" transforms examined).");
			string text = this._sb.ToString();
			if (text != this._lastTierLine)
			{
				this._lastTierLine = text;
				this._log.LogInfo(text);
			}
			this._sb.Length = 0;
			if (!this._usedLogged && (this._usedCounts.Count > 0 || this._scansCompleted >= 3))
			{
				this._usedLogged = true;
				this.LogSurfacesUsed();
				this._usedCounts.Clear();
			}
		}

		// Token: 0x060000DA RID: 218 RVA: 0x0000B97C File Offset: 0x00009B7C
		private void LogSurfacesUsed()
		{
			try
			{
				this._sb.Length = 0;
				this._sb.Append("MetalSurfaces: SURFACES USED: [");
				bool flag = true;
				foreach (KeyValuePair<string, MetalSurfaces.UsedRow> keyValuePair in this._usedCounts)
				{
					if (!flag)
					{
						this._sb.Append(", ");
					}
					flag = false;
					int bucket = keyValuePair.Value.Bucket;
					this._sb.Append(keyValuePair.Key).Append('x').Append(keyValuePair.Value.Count)
						.Append('|');
					if (bucket < 0)
					{
						this._sb.Append('?');
					}
					else
					{
						this._sb.Append(MetalSurfaces.RLabels[bucket], 1, MetalSurfaces.RLabels[bucket].Length - 1);
					}
				}
				this._sb.Append(']');
				this._log.LogInfo(this._sb.ToString());
				this._sb.Length = 0;
			}
			catch
			{
			}
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000BAD4 File Offset: 0x00009CD4
		public void RecordMaskDraws(CommandBuffer cmd, Material[] tierMats)
		{
			if (tierMats == null || tierMats.Length < 10)
			{
				if (!this._loggedTierMatMismatch)
				{
					this._loggedTierMatMismatch = true;
					this._log.LogWarning(string.Format("MetalSurfaces: mask tier materials = {0}, ", (tierMats == null) ? 0 : tierMats.Length) + string.Format("need {0} (one baked instance per MetalSurfaces.MaskTierValues entry) ", 10) + "— mask draws SKIPPED.");
				}
				return;
			}
			int num = 0;
			for (int i = 0; i < 10; i++)
			{
				int num2 = MetalSurfaces.DrawTier(cmd, tierMats[i], this._buckets[i]);
				if (i == 9)
				{
					num = num2;
				}
			}
			this._excludeDrawnLogged = num;
		}

		// Token: 0x060000DC RID: 220 RVA: 0x0000BB68 File Offset: 0x00009D68
		private static int DrawTier(CommandBuffer cmd, Material mat, List<Renderer> list)
		{
			if (mat == null || list.Count == 0)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				Renderer renderer = list[i];
				if (!(renderer == null) && renderer.enabled && !renderer.forceRenderingOff && renderer.gameObject.activeInHierarchy)
				{
					cmd.DrawRenderer(renderer, mat, 0, 0);
					num++;
				}
			}
			return num;
		}

		// Token: 0x060000DD RID: 221 RVA: 0x0000BBD8 File Offset: 0x00009DD8
		private static bool NameMatchesAny(string name, string[] keywords)
		{
			if (string.IsNullOrEmpty(name))
			{
				return false;
			}
			foreach (string text in keywords)
			{
				int num = 0;
				int num2;
				while ((num2 = name.IndexOf(text, num, StringComparison.OrdinalIgnoreCase)) >= 0)
				{
					if (MetalSurfaces.IsTokenStart(name, num2) && MetalSurfaces.IsTokenEnd(name, num2 + text.Length))
					{
						return true;
					}
					num = num2 + 1;
				}
			}
			return false;
		}

		// Token: 0x060000DE RID: 222 RVA: 0x0000BC34 File Offset: 0x00009E34
		private static bool IsTokenStart(string s, int idx)
		{
			if (idx == 0)
			{
				return true;
			}
			char c = s[idx - 1];
			return !char.IsLetter(c) || (char.IsUpper(s[idx]) && char.IsLower(c));
		}

		// Token: 0x060000DF RID: 223 RVA: 0x0000BC70 File Offset: 0x00009E70
		private static bool IsTokenEnd(string s, int end)
		{
			if (end >= s.Length)
			{
				return true;
			}
			char c = s[end];
			return !char.IsLetter(c) || char.IsUpper(c);
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x0000BCA0 File Offset: 0x00009EA0
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this._scanning = false;
			this._stack.Clear();
			this.DropAll();
			this._seenNames.Clear();
			this._matCache.Clear();
			this._usedCounts.Clear();
		}

		// Token: 0x04000135 RID: 309
		private const int TransformsPerTick = 48;

		// Token: 0x04000136 RID: 310
		private const int MaxExaminedPerScan = 200000;

		// Token: 0x04000137 RID: 311
		private const int MaxExclude = 256;

		// Token: 0x04000138 RID: 312
		private const int MaxSurface = 128;

		// Token: 0x04000139 RID: 313
		private const int MaxMasked = 384;

		// Token: 0x0400013A RID: 314
		private const float ScanIntervalSeconds = 10f;

		// Token: 0x0400013B RID: 315
		private const float EmptyScanIntervalSeconds = 30f;

		// Token: 0x0400013C RID: 316
		private const float SceneSettleSeconds = 2f;

		// Token: 0x0400013D RID: 317
		private const float DefaultSlipThreshold = 0.9f;

		// Token: 0x0400013E RID: 318
		internal const float ValExclude = 0f;

		// Token: 0x0400013F RID: 319
		internal const float ValFoliage = 0.08f;

		// Token: 0x04000140 RID: 320
		private const float REncodeBase = 0.4f;

		// Token: 0x04000141 RID: 321
		private const float REncodeSpan = 0.6f;

		// Token: 0x04000142 RID: 322
		internal static readonly float[] RTable = new float[] { 0.25f, 2f, 4f, 6f, 7f, 8f, 9f, 10f };

		// Token: 0x04000143 RID: 323
		private const int BSoft = 0;

		// Token: 0x04000144 RID: 324
		private const int BGrass = 1;

		// Token: 0x04000145 RID: 325
		private const int BWood = 2;

		// Token: 0x04000146 RID: 326
		private const int BConcrete = 3;

		// Token: 0x04000147 RID: 327
		private const int BStone = 4;

		// Token: 0x04000148 RID: 328
		private const int BPolished = 5;

		// Token: 0x04000149 RID: 329
		private const int BIce = 6;

		// Token: 0x0400014A RID: 330
		private const int BMetal = 7;

		// Token: 0x0400014B RID: 331
		private const int BFoliage = 8;

		// Token: 0x0400014C RID: 332
		private const int BExclude = 9;

		// Token: 0x0400014D RID: 333
		private const int BucketCount = 10;

		// Token: 0x0400014E RID: 334
		internal static readonly float[] MaskTierValues = MetalSurfaces.BuildMaskTierValues();

		// Token: 0x0400014F RID: 335
		private static readonly string[] RLabels = MetalSurfaces.BuildRLabels();

		// Token: 0x04000150 RID: 336
		private static readonly string[] FoliageKeywords = new string[] { "leaf", "leaves", "bush", "plant", "fern", "vine", "foliage" };

		// Token: 0x04000151 RID: 337
		private static readonly string[] IceVetoKeywords = new string[]
		{
			"icecream", "icecold", "snowball", "snowman", "snowglobe", "snow_globe", "snowflake", "snowblower", "nosnow", "noice",
			"nofrost"
		};

		// Token: 0x04000152 RID: 338
		private static readonly string[][] SpecificKeywords = new string[][]
		{
			new string[] { "trailtile" },
			new string[] { "concrete_dirty" },
			new string[] { "bark_tile" },
			new string[] { "grassrock" }
		};

		// Token: 0x04000153 RID: 339
		private static readonly int[] SpecificBuckets = new int[] { -1, 3, 2, 1 };

		// Token: 0x04000154 RID: 340
		private static readonly string[] MetalKeywords = new string[]
		{
			"metal", "steel", "iron", "chrome", "girder", "girders", "pipe", "pipes", "brass", "copper",
			"aluminum", "aluminium", "traintrack", "traintracks"
		};

		// Token: 0x04000155 RID: 341
		private static readonly string[] IceKeywords = new string[] { "ice", "icy", "frost", "frozen", "snow", "crystal", "glacier", "slip" };

		// Token: 0x04000156 RID: 342
		private static readonly string[] PolishedKeywords = new string[] { "glass", "tile", "tiles", "marble", "polished", "mirror", "porcelain", "ceramic" };

		// Token: 0x04000157 RID: 343
		private static readonly string[] StoneKeywords = new string[]
		{
			"stone", "rock", "rocks", "brick", "bricks", "cliff", "cliffside", "cavern", "boulder", "granite",
			"slate", "cobble", "tombstone", "masonry"
		};

		// Token: 0x04000158 RID: 344
		private static readonly string[] ConcreteKeywords = new string[] { "concrete", "cement", "asphalt", "cinderblock", "plaster", "stucco" };

		// Token: 0x04000159 RID: 345
		private static readonly string[] WoodKeywords = new string[]
		{
			"wood", "wooden", "plank", "planks", "log", "logs", "bark", "timber", "crate", "lumber",
			"pallet", "washboard"
		};

		// Token: 0x0400015A RID: 346
		private static readonly string[] GrassKeywords = new string[]
		{
			"grass", "dirt", "sand", "mud", "soil", "moss", "turf", "hay", "straw", "gravel",
			"fungal"
		};

		// Token: 0x0400015B RID: 347
		private static readonly string[] SoftKeywords = new string[]
		{
			"wool", "cloth", "fabric", "carpet", "rug", "fur", "pillow", "cushion", "couch", "cobweb",
			"cobwebs", "felt", "canvas", "blanket", "plush", "yarn", "sponge"
		};

		// Token: 0x0400015C RID: 348
		private const int MaxMatCache = 8192;

		// Token: 0x0400015D RID: 349
		private const int MaxUnmappedLogged = 192;

		// Token: 0x0400015E RID: 350
		private const int TableResolveAttempts = 8;

		// Token: 0x0400015F RID: 351
		private readonly ManualLogSource _log;

		// Token: 0x04000160 RID: 352
		private readonly List<GameObject> _rootsScratch = new List<GameObject>(64);

		// Token: 0x04000161 RID: 353
		private readonly Stack<Transform> _stack = new Stack<Transform>(512);

		// Token: 0x04000162 RID: 354
		private readonly List<Renderer> _rendererScratch = new List<Renderer>(32);

		// Token: 0x04000163 RID: 355
		private readonly List<Material> _matScratch = new List<Material>(8);

		// Token: 0x04000164 RID: 356
		private readonly StringBuilder _sb = new StringBuilder(256);

		// Token: 0x04000165 RID: 357
		private readonly List<Renderer>[] _buckets = MetalSurfaces.NewBuckets();

		// Token: 0x04000166 RID: 358
		private readonly List<Renderer>[] _scanBuckets = MetalSurfaces.NewBuckets();

		// Token: 0x04000167 RID: 359
		private int _scanCount;

		// Token: 0x04000168 RID: 360
		private int _excludeCount;

		// Token: 0x04000169 RID: 361
		private int _surfaceCount;

		// Token: 0x0400016A RID: 362
		private bool _excludeTruncated;

		// Token: 0x0400016B RID: 363
		private bool _surfaceTruncated;

		// Token: 0x0400016C RID: 364
		private int _excludeDrawnLogged = -1;

		// Token: 0x0400016D RID: 365
		private bool _loggedTierMatMismatch;

		// Token: 0x0400016E RID: 366
		private readonly HashSet<string> _seenNames = new HashSet<string>();

		// Token: 0x0400016F RID: 367
		private int _unmappedLogged;

		// Token: 0x04000170 RID: 368
		private bool _unmappedSuppressLogged;

		// Token: 0x04000171 RID: 369
		private readonly Dictionary<string, MetalSurfaces.UsedRow> _usedCounts = new Dictionary<string, MetalSurfaces.UsedRow>(64);

		// Token: 0x04000172 RID: 370
		private bool _usedLogged;

		// Token: 0x04000173 RID: 371
		private int _scansCompleted;

		// Token: 0x04000174 RID: 372
		private string _lastTierLine;

		// Token: 0x04000175 RID: 373
		private readonly Dictionary<int, MetalSurfaces.MatClass> _matCache = new Dictionary<int, MetalSurfaces.MatClass>(256);

		// Token: 0x04000176 RID: 374
		private bool _want;

		// Token: 0x04000177 RID: 375
		private bool _dirty;

		// Token: 0x04000178 RID: 376
		private bool _scanning;

		// Token: 0x04000179 RID: 377
		private int _examined;

		// Token: 0x0400017A RID: 378
		private float _nextScanAt;

		// Token: 0x0400017B RID: 379
		private bool _sceneJustLoaded;

		// Token: 0x0400017C RID: 380
		private int _emptyScans;

		// Token: 0x0400017D RID: 381
		private bool _wantPrev;

		// Token: 0x0400017E RID: 382
		private bool _reflectResolved;

		// Token: 0x0400017F RID: 383
		private Type _tSurf;

		// Token: 0x04000180 RID: 384
		private Type _tVRRig;

		// Token: 0x04000181 RID: 385
		private FieldInfo _fiOverrideIndex;

		// Token: 0x04000182 RID: 386
		private Type _tGTPlayer;

		// Token: 0x04000183 RID: 387
		private MemberInfo _mGTInstance;

		// Token: 0x04000184 RID: 388
		private FieldInfo _fiPlayerSO;

		// Token: 0x04000185 RID: 389
		private FieldInfo _fiSODatas;

		// Token: 0x04000186 RID: 390
		private FieldInfo _fiIceThreshold;

		// Token: 0x04000187 RID: 391
		private float _slipThreshold = 0.9f;

		// Token: 0x04000188 RID: 392
		private bool _matDataMembersResolved;

		// Token: 0x04000189 RID: 393
		private FieldInfo _fiMatName;

		// Token: 0x0400018A RID: 394
		private MethodInfo _miTrimmedName;

		// Token: 0x0400018B RID: 395
		private FieldInfo _fiSlip;

		// Token: 0x0400018C RID: 396
		private FieldInfo _fiSlipOverride;

		// Token: 0x0400018D RID: 397
		private bool _tableResolved;

		// Token: 0x0400018E RID: 398
		private bool _tableGaveUp;

		// Token: 0x0400018F RID: 399
		private int _tableAttempts;

		// Token: 0x04000190 RID: 400
		private string[] _rowName;

		// Token: 0x04000191 RID: 401
		private int[] _rowBucket;

		// Token: 0x04000192 RID: 402
		private bool _loggedReflectOk;

		// Token: 0x04000193 RID: 403
		private bool _loggedReflectFail;

		// Token: 0x02000017 RID: 23
		private struct UsedRow
		{
			// Token: 0x04000194 RID: 404
			public int Count;

			// Token: 0x04000195 RID: 405
			public int Bucket;
		}

		// Token: 0x02000018 RID: 24
		private struct MatClass
		{
			// Token: 0x04000196 RID: 406
			public int Bucket;

			// Token: 0x04000197 RID: 407
			public string Name;
		}
	}
}
