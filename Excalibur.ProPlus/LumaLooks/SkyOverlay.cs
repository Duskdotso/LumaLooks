using System;
using System.Text;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x02000048 RID: 72
	internal sealed class SkyOverlay
	{
		// Token: 0x17000040 RID: 64
		// (get) Token: 0x0600025D RID: 605 RVA: 0x00025B94 File Offset: 0x00023D94
		// (set) Token: 0x0600025E RID: 606 RVA: 0x00025B9B File Offset: 0x00023D9B
		public static bool BodiesLive { get; private set; }

		// Token: 0x0600025F RID: 607 RVA: 0x00025BA4 File Offset: 0x00023DA4
		public SkyOverlay(ManualLogSource log, RenderEngine engine, SkySystem sky)
		{
			this._log = log;
			this._engine = engine;
			this._sky = sky;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x06000260 RID: 608 RVA: 0x00025C2F File Offset: 0x00023E2F
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed)
		{
			this._want = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._staticDirty = true;
		}

		// Token: 0x06000261 RID: 609 RVA: 0x00025C4D File Offset: 0x00023E4D
		public void MarkDirty()
		{
			this._staticDirty = true;
		}

		// Token: 0x06000262 RID: 610 RVA: 0x00025C58 File Offset: 0x00023E58
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == (LoadSceneMode)1)
			{
				return;
			}
			for (int i = 0; i < this._ghostCount; i++)
			{
				this._ghosts[i] = default(SkyOverlay.Ghost);
			}
			this._ghostCount = 0;
			for (int j = 0; j < this._skippedCount; j++)
			{
				this._skipped[j] = null;
			}
			this._skippedCount = 0;
		}

		// Token: 0x06000263 RID: 611 RVA: 0x00025CB8 File Offset: 0x00023EB8
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
				SkyDome skyDome = ((this._sky != null) ? this._sky.Dome : null);
				if (!flag2 || skyDome == null || !this.EnsureMaterial())
				{
					this.TeardownAll();
					SkyOverlay.BodiesLive = false;
					this.MaybeLog(0, (skyDome != null) ? skyDome.TrackedCount : 0, SkyShell.Active);
				}
				else
				{
					this.SyncGhosts(skyDome);
					bool active = SkyShell.Active;
					bool flag3 = SkySystem.UniParams3.x > 0.0001f || SkySystem.UniBodyParams.x > 0.0001f;
					bool flag4 = active && flag3 && !this.AnyVisibleSkippedHit();
					int num = this.UpdateGhosts(flag4);
					SkyOverlay.BodiesLive = num > 0;
					this.PushUniforms();
					this.MaybeLog(num, skyDome.TrackedCount, active);
				}
			}
			catch (Exception ex)
			{
				this.DisableAllGhosts();
				SkyOverlay.BodiesLive = false;
				this._log.LogWarning("SkyOverlay tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000264 RID: 612 RVA: 0x00025DF4 File Offset: 0x00023FF4
		private void DisableAllGhosts()
		{
			for (int i = 0; i < this._ghostCount; i++)
			{
				MeshRenderer mr = this._ghosts[i].Mr;
				if (!(mr == null))
				{
					try
					{
						if (mr.enabled)
						{
							mr.enabled = false;
						}
					}
					catch
					{
					}
				}
			}
		}

		// Token: 0x06000265 RID: 613 RVA: 0x00025E54 File Offset: 0x00024054
		private void SyncGhosts(SkyDome dome)
		{
			for (int i = this._ghostCount - 1; i >= 0; i--)
			{
				Renderer source = this._ghosts[i].Source;
				if (source == null || !SkyOverlay.IsTracked(dome, source))
				{
					this.RemoveGhost(i);
				}
			}
			for (int j = this._skippedCount - 1; j >= 0; j--)
			{
				Renderer renderer = this._skipped[j];
				if (renderer == null || !SkyOverlay.IsTracked(dome, renderer))
				{
					for (int k = j; k < this._skippedCount - 1; k++)
					{
						this._skipped[k] = this._skipped[k + 1];
					}
					Renderer[] skipped = this._skipped;
					int num = this._skippedCount - 1;
					this._skippedCount = num;
					skipped[num] = null;
				}
			}
			int trackedCount = dome.TrackedCount;
			int num2 = 0;
			while (num2 < trackedCount && this._ghostCount < 8)
			{
				Renderer trackedRenderer = dome.GetTrackedRenderer(num2);
				if (!(trackedRenderer == null) && !this.HasGhostFor(trackedRenderer) && !this.IsSkipped(trackedRenderer))
				{
					this.TryAttach(trackedRenderer);
				}
				num2++;
			}
		}

		// Token: 0x06000266 RID: 614 RVA: 0x00025F68 File Offset: 0x00024168
		private static bool IsTracked(SkyDome dome, Renderer r)
		{
			int trackedCount = dome.TrackedCount;
			for (int i = 0; i < trackedCount; i++)
			{
				if (dome.GetTrackedRenderer(i) == r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000267 RID: 615 RVA: 0x00025F98 File Offset: 0x00024198
		private bool HasGhostFor(Renderer r)
		{
			for (int i = 0; i < this._ghostCount; i++)
			{
				if (this._ghosts[i].Source == r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000268 RID: 616 RVA: 0x00025FD0 File Offset: 0x000241D0
		private bool IsSkipped(Renderer r)
		{
			for (int i = 0; i < this._skippedCount; i++)
			{
				if (this._skipped[i] == r)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000269 RID: 617 RVA: 0x00025FFC File Offset: 0x000241FC
		private bool AnyVisibleSkippedHit()
		{
			for (int i = 0; i < this._skippedCount; i++)
			{
				Renderer renderer = this._skipped[i];
				if (!(renderer == null) && renderer.enabled && renderer.gameObject.activeInHierarchy)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600026A RID: 618 RVA: 0x00026044 File Offset: 0x00024244
		private void TryAttach(Renderer src)
		{
			int num;
			if (src.isPartOfStaticBatch)
			{
				if (this._skippedCount < this._skipped.Length)
				{
					Renderer[] skipped = this._skipped;
					num = this._skippedCount;
					this._skippedCount = num + 1;
					skipped[num] = src;
				}
				if (!this._staticBatchLogged)
				{
					this._staticBatchLogged = true;
					this._log.LogWarning("SkyOverlay: tracked dome '" + src.name + "' is statically batched — its MeshFilter reports the pre-transformed COMBINED batch mesh, which cannot be ghosted. While that hit is visible the overlay stands down (BodiesLive false) and the shell draws the bodies.");
				}
				return;
			}
			MeshFilter component = src.GetComponent<MeshFilter>();
			Mesh mesh = ((component != null) ? component.sharedMesh : null);
			if (mesh == null)
			{
				if (this._skippedCount < this._skipped.Length)
				{
					Renderer[] skipped2 = this._skipped;
					num = this._skippedCount;
					this._skippedCount = num + 1;
					skipped2[num] = src;
				}
				if (!this._meshlessLogged)
				{
					this._meshlessLogged = true;
					this._log.LogWarning("SkyOverlay: tracked dome '" + src.name + "' has no usable MeshFilter mesh — no ghost for that hit. While it is visible the overlay stands down (BodiesLive false) and the shell draws the bodies.");
				}
				return;
			}
			GameObject gameObject = new GameObject("LumaLooks_Overlay");
			Transform transform = gameObject.transform;
			MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
			meshFilter.sharedMesh = mesh;
			MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
			meshRenderer.sharedMaterials = this.BuildMaterialArray(mesh.subMeshCount);
			meshRenderer.shadowCastingMode = 0;
			meshRenderer.receiveShadows = false;
			meshRenderer.lightProbeUsage = 0;
			meshRenderer.reflectionProbeUsage = 0;
			meshRenderer.allowOcclusionWhenDynamic = false;
			meshRenderer.enabled = false;
			SkyOverlay.Ghost[] ghosts = this._ghosts;
			num = this._ghostCount;
			this._ghostCount = num + 1;
			ghosts[num] = new SkyOverlay.Ghost
			{
				Source = src,
				SourceTr = src.transform,
				SourceMf = component,
				Go = gameObject,
				GoTr = transform,
				Mf = meshFilter,
				Mr = meshRenderer,
				Mesh = mesh,
				SubMeshes = mesh.subMeshCount,
				Name = src.name
			};
			if (!this._loggedOnline)
			{
				this._loggedOnline = true;
				this._log.LogInfo(string.Concat(new string[]
				{
					"SkyOverlay: LumaLooks_Overlay online — ghosting dome '",
					src.name,
					"' ",
					string.Format("({0} submesh(es)) with {1}: the sun and ", mesh.subMeshCount, "LumaLooks/SkyOverlay"),
					"moon now draw ON the dome surface itself (additive, queue 2850, shared mesh = same depth). While live the shell stands its body down (_LumaShellDrawBody = 0) and keeps the clouds, which blend NEARER at 2900 so a cloud still dims the sun behind it."
				}));
			}
		}

		// Token: 0x0600026B RID: 619 RVA: 0x00026290 File Offset: 0x00024490
		private Material[] BuildMaterialArray(int subMeshCount)
		{
			Material[] array = new Material[Mathf.Max(1, subMeshCount)];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this._mat;
			}
			return array;
		}

		// Token: 0x0600026C RID: 620 RVA: 0x000262C4 File Offset: 0x000244C4
		private int UpdateGhosts(bool drawAllowed)
		{
			int num = 0;
			for (int i = this._ghostCount - 1; i >= 0; i--)
			{
				SkyOverlay.Ghost ghost = this._ghosts[i];
				Renderer source = ghost.Source;
				if (source == null || ghost.Go == null)
				{
					this.RemoveGhost(i);
				}
				else
				{
					Mesh mesh = ((ghost.SourceMf != null) ? ghost.SourceMf.sharedMesh : null);
					if (mesh != ghost.Mesh)
					{
						if (mesh == null)
						{
							this.RemoveGhost(i);
							goto IL_0169;
						}
						ghost.Mesh = mesh;
						ghost.Mf.sharedMesh = mesh;
						if (mesh.subMeshCount != ghost.SubMeshes)
						{
							ghost.SubMeshes = mesh.subMeshCount;
							ghost.Mr.sharedMaterials = this.BuildMaterialArray(ghost.SubMeshes);
						}
						this._ghosts[i] = ghost;
					}
					ghost.GoTr.SetPositionAndRotation(ghost.SourceTr.position, ghost.SourceTr.rotation);
					Vector3 lossyScale = ghost.SourceTr.lossyScale;
					if (ghost.GoTr.localScale != lossyScale)
					{
						ghost.GoTr.localScale = lossyScale;
					}
					bool flag = source.enabled && source.gameObject.activeInHierarchy && drawAllowed;
					if (ghost.Mr.enabled != flag)
					{
						ghost.Mr.enabled = flag;
					}
					if (flag)
					{
						num++;
					}
				}
				IL_0169:;
			}
			return num;
		}

		// Token: 0x0600026D RID: 621 RVA: 0x00026448 File Offset: 0x00024648
		private void RemoveGhost(int i)
		{
			GameObject go = this._ghosts[i].Go;
			if (go != null)
			{
				try
				{
					UnityEngine.Object.Destroy(go);
				}
				catch
				{
				}
			}
			for (int j = i; j < this._ghostCount - 1; j++)
			{
				this._ghosts[j] = this._ghosts[j + 1];
			}
			SkyOverlay.Ghost[] ghosts = this._ghosts;
			int num = this._ghostCount - 1;
			this._ghostCount = num;
			ghosts[num] = default(SkyOverlay.Ghost);
		}

		// Token: 0x0600026E RID: 622 RVA: 0x000264DC File Offset: 0x000246DC
		private bool EnsureMaterial()
		{
			if (this._mat != null)
			{
				return true;
			}
			RenderEngine engine = this._engine;
			Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/SkyOverlay") : null);
			if (shader == null)
			{
				if (!this._shaderMissingLogged)
				{
					this._shaderMissingLogged = true;
					this._log.LogWarning("SkyOverlay: shader 'LumaLooks/SkyOverlay' not in the bundle — the on-dome sun/moon overlay is disabled (BodiesLive stays false, so the shell keeps drawing the bodies: this degrades, it never double-draws or goes dark).");
				}
				return false;
			}
			this._mat = new Material(shader)
			{
				hideFlags = (HideFlags)61
			};
			this._staticDirty = true;
			return true;
		}

		// Token: 0x0600026F RID: 623 RVA: 0x00026558 File Offset: 0x00024758
		private void PushUniforms()
		{
			if (this._mat == null)
			{
				return;
			}
			this._mat.SetVector(ShaderIds.SkySunDir, SkySystem.UniSunDir);
			this._mat.SetVector(ShaderIds.SkyParams, SkySystem.UniParams);
			this._mat.SetVector(ShaderIds.SkyParams3, SkySystem.UniParams3);
			this._mat.SetVector(ShaderIds.SkyBodyParams, SkySystem.UniBodyParams);
			if (!this._staticDirty)
			{
				return;
			}
			this._staticDirty = false;
			this._mat.SetVector(ShaderIds.SkySunTint, SkySystem.UniSunTint);
			this._mat.SetVector(ShaderIds.SkyMoonTint, SkySystem.UniMoonTint);
			Texture2D texture2D = ((this._engine != null) ? this._engine.GetTexture("MoonAlbedo") : null);
			if (texture2D != null)
			{
				this._mat.SetTexture(ShaderIds.MoonTex, texture2D);
			}
		}

		// Token: 0x06000270 RID: 624 RVA: 0x00026638 File Offset: 0x00024838
		private void MaybeLog(int enabledGhosts, int domes, bool shellActive)
		{
			if (this._loggedOnce && this._loggedLive == SkyOverlay.BodiesLive && this._loggedGhosts == this._ghostCount && this._loggedDomes == domes && this._loggedEnabled == enabledGhosts && this._loggedShellActive == shellActive)
			{
				return;
			}
			if (!this._loggedOnce && this._ghostCount == 0 && domes == 0 && !SkyOverlay.BodiesLive)
			{
				return;
			}
			this._loggedOnce = true;
			this._loggedLive = SkyOverlay.BodiesLive;
			this._loggedGhosts = this._ghostCount;
			this._loggedDomes = domes;
			this._loggedEnabled = enabledGhosts;
			this._loggedShellActive = shellActive;
			this._sb.Length = 0;
			this._sb.Append("SKYOVERLAY: live=").Append(SkyOverlay.BodiesLive ? 1 : 0).Append(" ghosts=")
				.Append(this._ghostCount)
				.Append(" domes=")
				.Append(domes)
				.Append(" shellActive=")
				.Append(shellActive ? 1 : 0);
			for (int i = 0; i < this._ghostCount; i++)
			{
				bool flag = false;
				MeshRenderer mr = this._ghosts[i].Mr;
				if (mr != null)
				{
					flag = mr.enabled;
				}
				this._sb.Append(" [").Append(this._ghosts[i].Name ?? "?").Append("|sub=")
					.Append(this._ghosts[i].SubMeshes)
					.Append("|on=")
					.Append(flag ? 1 : 0)
					.Append(']');
			}
			this._log.LogInfo(this._sb.ToString());
		}

		// Token: 0x06000271 RID: 625 RVA: 0x000267F0 File Offset: 0x000249F0
		private void TeardownAll()
		{
			for (int i = this._ghostCount - 1; i >= 0; i--)
			{
				this.RemoveGhost(i);
			}
			for (int j = 0; j < this._skippedCount; j++)
			{
				this._skipped[j] = null;
			}
			this._skippedCount = 0;
		}

		// Token: 0x06000272 RID: 626 RVA: 0x00026838 File Offset: 0x00024A38
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.TeardownAll();
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
			this._loggedOnline = false;
			SkyOverlay.BodiesLive = false;
		}

		// Token: 0x04000602 RID: 1538
		private const string OverlayShaderName = "LumaLooks/SkyOverlay";

		// Token: 0x04000603 RID: 1539
		private const string GoName = "LumaLooks_Overlay";

		// Token: 0x04000604 RID: 1540
		private const int MaxGhosts = 8;

		// Token: 0x04000605 RID: 1541
		private readonly ManualLogSource _log;

		// Token: 0x04000606 RID: 1542
		private readonly RenderEngine _engine;

		// Token: 0x04000607 RID: 1543
		private readonly SkySystem _sky;

		// Token: 0x04000608 RID: 1544
		private Material _mat;

		// Token: 0x04000609 RID: 1545
		private readonly SkyOverlay.Ghost[] _ghosts = new SkyOverlay.Ghost[8];

		// Token: 0x0400060A RID: 1546
		private int _ghostCount;

		// Token: 0x0400060B RID: 1547
		private readonly Renderer[] _skipped = new Renderer[8];

		// Token: 0x0400060C RID: 1548
		private int _skippedCount;

		// Token: 0x0400060D RID: 1549
		private bool _want;

		// Token: 0x0400060E RID: 1550
		private bool _vrAllowed = true;

		// Token: 0x0400060F RID: 1551
		private bool _desktopAllowed = true;

		// Token: 0x04000610 RID: 1552
		private bool _staticDirty = true;

		// Token: 0x04000611 RID: 1553
		private bool _shaderMissingLogged;

		// Token: 0x04000612 RID: 1554
		private bool _meshlessLogged;

		// Token: 0x04000613 RID: 1555
		private bool _staticBatchLogged;

		// Token: 0x04000614 RID: 1556
		private bool _loggedOnline;

		// Token: 0x04000615 RID: 1557
		private bool _loggedOnce;

		// Token: 0x04000616 RID: 1558
		private bool _loggedLive;

		// Token: 0x04000617 RID: 1559
		private int _loggedGhosts = -1;

		// Token: 0x04000618 RID: 1560
		private int _loggedDomes = -1;

		// Token: 0x04000619 RID: 1561
		private int _loggedEnabled = -1;

		// Token: 0x0400061A RID: 1562
		private bool _loggedShellActive;

		// Token: 0x0400061B RID: 1563
		private readonly StringBuilder _sb = new StringBuilder(192);

		// Token: 0x02000049 RID: 73
		private struct Ghost
		{
			// Token: 0x0400061D RID: 1565
			public Renderer Source;

			// Token: 0x0400061E RID: 1566
			public Transform SourceTr;

			// Token: 0x0400061F RID: 1567
			public MeshFilter SourceMf;

			// Token: 0x04000620 RID: 1568
			public GameObject Go;

			// Token: 0x04000621 RID: 1569
			public Transform GoTr;

			// Token: 0x04000622 RID: 1570
			public MeshFilter Mf;

			// Token: 0x04000623 RID: 1571
			public MeshRenderer Mr;

			// Token: 0x04000624 RID: 1572
			public Mesh Mesh;

			// Token: 0x04000625 RID: 1573
			public int SubMeshes;

			// Token: 0x04000626 RID: 1574
			public string Name;
		}
	}
}
