using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000014 RID: 20
	internal sealed class LumaSkybox
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000088 RID: 136 RVA: 0x00008568 File Offset: 0x00006768
		public string Directory
		{
			get
			{
				return this._dir;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000089 RID: 137 RVA: 0x00008570 File Offset: 0x00006770
		public bool Active
		{
			get
			{
				return this._active;
			}
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00008578 File Offset: 0x00006778
		public LumaSkybox(ManualLogSource log)
		{
			this._log = log;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x000085E7 File Offset: 0x000067E7
		public void Configure(bool want, bool vrAllowed, bool desktopAllowed)
		{
			this._want = want;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
		}

		// Token: 0x0600008C RID: 140 RVA: 0x00008600 File Offset: 0x00006800
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
				if (!this._want || !(flag ? this._vrAllowed : this._desktopAllowed))
				{
					if (this._active)
					{
						this.RestoreVanilla();
						this._active = false;
					}
				}
				else
				{
					this.EnsureDirectory();
					if (Time.unscaledTime >= this._nextScan)
					{
						this._nextScan = Time.unscaledTime + 3f;
						this.Scan();
					}
					if (!this._anyImages)
					{
						if (this._active)
						{
							this.RestoreVanilla();
							this._active = false;
						}
					}
					else
					{
						this._active = true;
						if (Time.unscaledTime >= this._nextSweep)
						{
							this._nextSweep = Time.unscaledTime + 8f;
							this.RefreshSkyMaterials();
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("LUMASKY: tick failed - " + ex.Message);
			}
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00008704 File Offset: 0x00006904
		public void ApplyAtRender()
		{
			if (!this._active)
			{
				return;
			}
			try
			{
				int slotCount = GtClock.SlotCount;
				int slotIndex = GtClock.SlotIndex;
				if (slotIndex >= 0 && slotCount > 0)
				{
					int num = (slotIndex + 1) % slotCount;
					this.EnsureIds();
					Texture2D slotTexture = this.GetSlotTexture(slotIndex);
					Texture2D slotTexture2 = this.GetSlotTexture(num);
					for (int i = 0; i < this._globalFromIds.Length; i++)
					{
						if (slotTexture != null)
						{
							this.SetGlobalWithCapture(this._globalFromIds[i], slotTexture);
						}
						if (slotTexture2 != null)
						{
							this.SetGlobalWithCapture(this._globalToIds[i], slotTexture2);
						}
					}
					Texture2D texture2D = ((slotTexture != null) ? slotTexture : slotTexture2);
					if (texture2D != null && this._skyMats.Count > 0)
					{
						for (int j = 0; j < this._skyMats.Count; j++)
						{
							Material material = this._skyMats[j];
							Dictionary<int, Texture> dictionary;
							if (!(material == null) && this._matOriginals.TryGetValue(material, out dictionary))
							{
								foreach (int num2 in dictionary.Keys)
								{
									material.SetTexture(num2, texture2D);
								}
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("LUMASKY: apply failed - " + ex.Message);
			}
		}

		// Token: 0x0600008E RID: 142 RVA: 0x000088A4 File Offset: 0x00006AA4
		public void Dispose()
		{
			try
			{
				this.RestoreVanilla();
			}
			catch
			{
			}
			for (int i = 0; i < 32; i++)
			{
				if (this._slotTex[i] != null)
				{
					try
					{
						UnityEngine.Object.Destroy(this._slotTex[i]);
					}
					catch
					{
					}
					this._slotTex[i] = null;
				}
				this._slotPath[i] = null;
				this._slotStamp[i] = 0L;
				this._slotFailed[i] = false;
			}
			this._active = false;
			this._anyImages = false;
		}

		// Token: 0x0600008F RID: 143 RVA: 0x0000893C File Offset: 0x00006B3C
		private static bool TryLoadImage(Texture2D tex, byte[] bytes)
		{
			if (!LumaSkybox._loadImageResolved)
			{
				LumaSkybox._loadImageResolved = true;
				try
				{
					Type type = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
					if (type != null)
					{
						LumaSkybox._miLoadImage = type.GetMethod("LoadImage", new Type[]
						{
							typeof(Texture2D),
							typeof(byte[]),
							typeof(bool)
						});
					}
				}
				catch
				{
				}
			}
			if (LumaSkybox._miLoadImage == null)
			{
				return false;
			}
			object obj = LumaSkybox._miLoadImage.Invoke(null, new object[] { tex, bytes, false });
			return obj is bool && (bool)obj;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00008A00 File Offset: 0x00006C00
		private void EnsureIds()
		{
			if (this._globalFromIds != null)
			{
				return;
			}
			this._globalFromIds = new int[LumaSkybox.GlobalFromNames.Length];
			this._globalToIds = new int[LumaSkybox.GlobalToNames.Length];
			for (int i = 0; i < LumaSkybox.GlobalFromNames.Length; i++)
			{
				this._globalFromIds[i] = Shader.PropertyToID(LumaSkybox.GlobalFromNames[i]);
			}
			for (int j = 0; j < LumaSkybox.GlobalToNames.Length; j++)
			{
				this._globalToIds[j] = Shader.PropertyToID(LumaSkybox.GlobalToNames[j]);
			}
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00008A88 File Offset: 0x00006C88
		private void SetGlobalWithCapture(int id, Texture tex)
		{
			if (!this._vanillaGlobals.ContainsKey(id))
			{
				Texture texture = null;
				try
				{
					texture = Shader.GetGlobalTexture(id);
				}
				catch
				{
				}
				if (!this.IsOurs(texture))
				{
					this._vanillaGlobals[id] = texture;
				}
			}
			Shader.SetGlobalTexture(id, tex);
			this._touchedGlobals = true;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00008AE8 File Offset: 0x00006CE8
		private bool IsOurs(Texture t)
		{
			if (t == null)
			{
				return false;
			}
			for (int i = 0; i < 32; i++)
			{
				if (this._slotTex[i] == t)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00008B1C File Offset: 0x00006D1C
		private void RestoreVanilla()
		{
			if (this._touchedGlobals)
			{
				foreach (KeyValuePair<int, Texture> keyValuePair in this._vanillaGlobals)
				{
					try
					{
						Shader.SetGlobalTexture(keyValuePair.Key, keyValuePair.Value);
					}
					catch
					{
					}
				}
				this._touchedGlobals = false;
			}
			this._vanillaGlobals.Clear();
			foreach (KeyValuePair<Material, Dictionary<int, Texture>> keyValuePair2 in this._matOriginals)
			{
				Material key = keyValuePair2.Key;
				if (!(key == null))
				{
					foreach (KeyValuePair<int, Texture> keyValuePair3 in keyValuePair2.Value)
					{
						try
						{
							key.SetTexture(keyValuePair3.Key, keyValuePair3.Value);
						}
						catch
						{
						}
					}
				}
			}
			this._matOriginals.Clear();
			this._skyMats.Clear();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00008C70 File Offset: 0x00006E70
		private void EnsureDirectory()
		{
			if (this._dirReady)
			{
				return;
			}
			this._dirReady = true;
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string text = null;
				string[] array = new string[]
				{
					Path.Combine(folderPath, "LumaLooks"),
					Path.Combine(folderPath, "gorilla-tag-mod-manager")
				};
				for (int i = 0; i < array.Length; i++)
				{
					if (global::System.IO.Directory.Exists(array[i]))
					{
						text = array[i];
						break;
					}
				}
				if (text == null)
				{
					text = array[array.Length - 1];
				}
				this._dir = Path.Combine(text, "luma-skies");
				if (!global::System.IO.Directory.Exists(this._dir))
				{
					global::System.IO.Directory.CreateDirectory(this._dir);
				}
				this.WriteReadme();
				this._log.LogInfo("LUMASKY: sky folder is " + this._dir);
			}
			catch (Exception ex)
			{
				this._dir = null;
				this._log.LogWarning("LUMASKY: could not create the sky folder - " + ex.Message);
			}
		}

		// Token: 0x06000095 RID: 149 RVA: 0x00008D68 File Offset: 0x00006F68
		private void WriteReadme()
		{
			try
			{
				string text = Path.Combine(this._dir, "README.txt");
				if (!File.Exists(text))
				{
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine("LUMA SKY - your own skybox for each time of day");
					stringBuilder.AppendLine("================================================");
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Drop an image in here for any time of day and Gorilla Tag will use it");
					stringBuilder.AppendLine("instead of its own sky at that time - on every map. Times you do not");
					stringBuilder.AppendLine("fill in keep the game's normal sky, and the game still fades smoothly");
					stringBuilder.AppendLine("from one to the next.");
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Accepted files: .png  .jpg  .jpeg");
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Name a file by its slot number, by its name, or both:");
					stringBuilder.AppendLine("    03.png            03 - Morning.png            Morning.png");
					stringBuilder.AppendLine();
					string[] slotNames = GtClock.SlotNames;
					if (slotNames != null && slotNames.Length != 0)
					{
						stringBuilder.AppendLine("This game's times of day:");
						for (int i = 0; i < slotNames.Length; i++)
						{
							stringBuilder.AppendLine("    " + i.ToString("00") + "   " + slotNames[i]);
						}
					}
					else
					{
						stringBuilder.AppendLine("Slots are numbered 00-09. Launch once with Luma running and this");
						stringBuilder.AppendLine("file will be rewritten with the real names for your version.");
					}
					stringBuilder.AppendLine();
					stringBuilder.AppendLine("Edits are picked up within a few seconds - no restart needed. Replacing");
					stringBuilder.AppendLine("a file reloads it automatically. Delete a file to go back to the game's");
					stringBuilder.AppendLine("own sky for that time of day.");
					File.WriteAllText(text, stringBuilder.ToString());
				}
			}
			catch
			{
			}
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00008EE0 File Offset: 0x000070E0
		private void Scan()
		{
			if (this._dir == null)
			{
				this._anyImages = false;
				return;
			}
			string[] files;
			try
			{
				files = global::System.IO.Directory.GetFiles(this._dir);
			}
			catch
			{
				this._anyImages = false;
				return;
			}
			string[] slotNames = GtClock.SlotNames;
			int num = GtClock.SlotCount;
			if (num <= 0 || num > 32)
			{
				num = 10;
			}
			bool flag = false;
			for (int i = 0; i < num; i++)
			{
				string text = this.FindFileForSlot(files, i, slotNames);
				if (text == null)
				{
					if (this._slotPath[i] != null)
					{
						this.DropSlot(i);
					}
				}
				else
				{
					flag = true;
					long num2 = 0L;
					try
					{
						num2 = File.GetLastWriteTimeUtc(text).Ticks;
					}
					catch
					{
					}
					if (!string.Equals(text, this._slotPath[i], StringComparison.OrdinalIgnoreCase) || num2 != this._slotStamp[i])
					{
						this.DropSlot(i);
						this._slotPath[i] = text;
						this._slotStamp[i] = num2;
					}
				}
			}
			this._anyImages = flag;
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00008FEC File Offset: 0x000071EC
		private void DropSlot(int slot)
		{
			if (this._slotTex[slot] != null)
			{
				try
				{
					UnityEngine.Object.Destroy(this._slotTex[slot]);
				}
				catch
				{
				}
				this._slotTex[slot] = null;
			}
			this._slotPath[slot] = null;
			this._slotStamp[slot] = 0L;
			this._slotFailed[slot] = false;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00009050 File Offset: 0x00007250
		private string FindFileForSlot(string[] files, int slot, string[] names)
		{
			string text = ((names != null && slot < names.Length) ? names[slot] : null);
			string text2 = slot.ToString("00");
			string text3 = slot.ToString();
			string text4 = null;
			for (int i = 0; i < files.Length; i++)
			{
				string extension = Path.GetExtension(files[i]);
				if (extension != null)
				{
					bool flag = false;
					for (int j = 0; j < LumaSkybox.Extensions.Length; j++)
					{
						if (string.Equals(extension, LumaSkybox.Extensions[j], StringComparison.OrdinalIgnoreCase))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(files[i]);
						if (!string.IsNullOrEmpty(fileNameWithoutExtension))
						{
							string text5 = fileNameWithoutExtension.Trim();
							if (text5.StartsWith(text2, StringComparison.Ordinal) || text5.StartsWith(text3, StringComparison.Ordinal))
							{
								string text6 = (text5.StartsWith(text2, StringComparison.Ordinal) ? text5.Substring(text2.Length) : text5.Substring(text3.Length));
								if (text6.Length == 0 || text6[0] == ' ' || text6[0] == '-' || text6[0] == '_')
								{
									return files[i];
								}
							}
							if (text != null && text4 == null && string.Equals(text5, text.Trim(), StringComparison.OrdinalIgnoreCase))
							{
								text4 = files[i];
							}
						}
					}
				}
			}
			return text4;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00009194 File Offset: 0x00007394
		private Texture2D GetSlotTexture(int slot)
		{
			if (slot < 0 || slot >= 32)
			{
				return null;
			}
			if (this._slotTex[slot] != null)
			{
				return this._slotTex[slot];
			}
			if (this._slotFailed[slot] || this._slotPath[slot] == null)
			{
				return null;
			}
			Texture2D texture2D2;
			try
			{
				byte[] array = File.ReadAllBytes(this._slotPath[slot]);
				Texture2D texture2D = new Texture2D(2, 2, (TextureFormat)4, true, false);
				if (!LumaSkybox.TryLoadImage(texture2D, array))
				{
					UnityEngine.Object.Destroy(texture2D);
					this._slotFailed[slot] = true;
					this._log.LogWarning("LUMASKY: could not decode " + Path.GetFileName(this._slotPath[slot]));
					texture2D2 = null;
				}
				else
				{
					texture2D.wrapModeU = 0;
					texture2D.wrapModeV = (TextureWrapMode)1;
					texture2D.filterMode = (FilterMode)1;
					texture2D.anisoLevel = 4;
					texture2D.Apply(true, true);
					this._slotTex[slot] = texture2D;
					this._log.LogInfo("LUMASKY: loaded " + Path.GetFileName(this._slotPath[slot]) + " for slot " + slot.ToString());
					texture2D2 = texture2D;
				}
			}
			catch (Exception ex)
			{
				this._slotFailed[slot] = true;
				this._log.LogWarning("LUMASKY: failed to load slot " + slot.ToString() + " - " + ex.Message);
				texture2D2 = null;
			}
			return texture2D2;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x000092DC File Offset: 0x000074DC
		private void RefreshSkyMaterials()
		{
			this._skyMats.Clear();
			Material[] array;
			try
			{
				array = Resources.FindObjectsOfTypeAll<Material>();
			}
			catch
			{
				return;
			}
			foreach (Material material in array)
			{
				if (!(material == null))
				{
					Shader shader = material.shader;
					if (!(shader == null))
					{
						bool flag = false;
						for (int j = 0; j < LumaSkybox.SkyMaterialShaders.Length; j++)
						{
							if (shader.name == LumaSkybox.SkyMaterialShaders[j])
							{
								flag = true;
								break;
							}
						}
						if (flag)
						{
							this._skyMats.Add(material);
							if (!this._matOriginals.ContainsKey(material))
							{
								Dictionary<int, Texture> dictionary = new Dictionary<int, Texture>();
								try
								{
									string[] texturePropertyNames = material.GetTexturePropertyNames();
									for (int k = 0; k < texturePropertyNames.Length; k++)
									{
										int num = Shader.PropertyToID(texturePropertyNames[k]);
										dictionary[num] = material.GetTexture(num);
									}
								}
								catch
								{
								}
								this._matOriginals[material] = dictionary;
							}
						}
					}
				}
			}
			List<Material> list = new List<Material>();
			foreach (KeyValuePair<Material, Dictionary<int, Texture>> keyValuePair in this._matOriginals)
			{
				if (keyValuePair.Key == null)
				{
					list.Add(keyValuePair.Key);
				}
			}
			for (int l = 0; l < list.Count; l++)
			{
				this._matOriginals.Remove(list[l]);
			}
		}

		// Token: 0x040000F2 RID: 242
		private static readonly string[] GlobalFromNames = new string[] { "_GlobalDayNightSkyTex1", "_GlobalDayNightSky2Tex1", "_GlobalDayNightSky3Tex1" };

		// Token: 0x040000F3 RID: 243
		private static readonly string[] GlobalToNames = new string[] { "_GlobalDayNightSkyTex2", "_GlobalDayNightSky2Tex2", "_GlobalDayNightSky3Tex2" };

		// Token: 0x040000F4 RID: 244
		private static readonly string[] SkyMaterialShaders = new string[] { "Gorilla/GTSkybox", "GorillaTag/SkyboxLerp" };

		// Token: 0x040000F5 RID: 245
		private static readonly string[] Extensions = new string[] { ".png", ".jpg", ".jpeg" };

		// Token: 0x040000F6 RID: 246
		private const float ScanSeconds = 3f;

		// Token: 0x040000F7 RID: 247
		private const float SweepSeconds = 8f;

		// Token: 0x040000F8 RID: 248
		private const int MaxSlots = 32;

		// Token: 0x040000F9 RID: 249
		private readonly ManualLogSource _log;

		// Token: 0x040000FA RID: 250
		private int[] _globalFromIds;

		// Token: 0x040000FB RID: 251
		private int[] _globalToIds;

		// Token: 0x040000FC RID: 252
		private readonly string[] _slotPath = new string[32];

		// Token: 0x040000FD RID: 253
		private readonly long[] _slotStamp = new long[32];

		// Token: 0x040000FE RID: 254
		private readonly Texture2D[] _slotTex = new Texture2D[32];

		// Token: 0x040000FF RID: 255
		private readonly bool[] _slotFailed = new bool[32];

		// Token: 0x04000100 RID: 256
		private bool _want;

		// Token: 0x04000101 RID: 257
		private bool _vrAllowed;

		// Token: 0x04000102 RID: 258
		private bool _desktopAllowed;

		// Token: 0x04000103 RID: 259
		private bool _active;

		// Token: 0x04000104 RID: 260
		private bool _anyImages;

		// Token: 0x04000105 RID: 261
		private float _nextScan;

		// Token: 0x04000106 RID: 262
		private float _nextSweep;

		// Token: 0x04000107 RID: 263
		private bool _dirReady;

		// Token: 0x04000108 RID: 264
		private string _dir;

		// Token: 0x04000109 RID: 265
		private readonly Dictionary<int, Texture> _vanillaGlobals = new Dictionary<int, Texture>();

		// Token: 0x0400010A RID: 266
		private readonly List<Material> _skyMats = new List<Material>();

		// Token: 0x0400010B RID: 267
		private readonly Dictionary<Material, Dictionary<int, Texture>> _matOriginals = new Dictionary<Material, Dictionary<int, Texture>>();

		// Token: 0x0400010C RID: 268
		private bool _touchedGlobals;

		// Token: 0x0400010D RID: 269
		private static MethodInfo _miLoadImage;

		// Token: 0x0400010E RID: 270
		private static bool _loadImageResolved;
	}
}
