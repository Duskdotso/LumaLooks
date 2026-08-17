using System;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace LumaLooks
{
	// Token: 0x0200004A RID: 74
	internal sealed class SkyShell
	{
		// Token: 0x17000041 RID: 65
		// (get) Token: 0x06000273 RID: 627 RVA: 0x000268A0 File Offset: 0x00024AA0
		// (set) Token: 0x06000274 RID: 628 RVA: 0x000268A8 File Offset: 0x00024AA8
		public bool HalfResDraw { get; set; }

		// Token: 0x17000042 RID: 66
		// (get) Token: 0x06000275 RID: 629 RVA: 0x000268B1 File Offset: 0x00024AB1
		public MeshRenderer ShellRenderer
		{
			get
			{
				return this._mr;
			}
		}

		// Token: 0x17000043 RID: 67
		// (get) Token: 0x06000276 RID: 630 RVA: 0x000268B9 File Offset: 0x00024AB9
		public Material ShellMaterial
		{
			get
			{
				return this._mat;
			}
		}

		// Token: 0x17000044 RID: 68
		// (get) Token: 0x06000277 RID: 631 RVA: 0x000268C1 File Offset: 0x00024AC1
		public Mesh ShellMesh
		{
			get
			{
				return this._mesh;
			}
		}

		// Token: 0x17000045 RID: 69
		// (get) Token: 0x06000278 RID: 632 RVA: 0x000268C9 File Offset: 0x00024AC9
		public float AppliedRadius
		{
			get
			{
				return this._appliedRadius;
			}
		}

		// Token: 0x17000046 RID: 70
		// (get) Token: 0x06000279 RID: 633 RVA: 0x000268D1 File Offset: 0x00024AD1
		public Matrix4x4 ShellMatrix
		{
			get
			{
				if (!(this._go != null))
				{
					return Matrix4x4.identity;
				}
				return this._go.transform.localToWorldMatrix;
			}
		}

		// Token: 0x17000047 RID: 71
		// (get) Token: 0x0600027A RID: 634 RVA: 0x000268F7 File Offset: 0x00024AF7
		// (set) Token: 0x0600027B RID: 635 RVA: 0x000268FE File Offset: 0x00024AFE
		public static bool Active { get; private set; }

		// Token: 0x17000048 RID: 72
		// (get) Token: 0x0600027C RID: 636 RVA: 0x00026906 File Offset: 0x00024B06
		public static bool DepthNeeded
		{
			get
			{
				return SkyShell.Active;
			}
		}

		// Token: 0x0600027D RID: 637 RVA: 0x00026910 File Offset: 0x00024B10
		public SkyShell(ManualLogSource log, RenderEngine engine)
		{
			this._log = log;
			this._engine = engine;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x0600027E RID: 638 RVA: 0x0002696D File Offset: 0x00024B6D
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed)
		{
			this._want = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._staticDirty = true;
		}

		// Token: 0x0600027F RID: 639 RVA: 0x0002698B File Offset: 0x00024B8B
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			if (m == (LoadSceneMode)1)
			{
				return;
			}
			this._go = null;
			this._mr = null;
			this._appliedRadius = -1f;
		}

		// Token: 0x06000280 RID: 640 RVA: 0x000269AC File Offset: 0x00024BAC
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
					this.Teardown();
					SkyShell.Active = false;
				}
				else
				{
					Camera main = Camera.main;
					if (main == null)
					{
						this.SetShellVisible(false);
						SkyShell.Active = false;
					}
					else if (!this.EnsureShell())
					{
						SkyShell.Active = false;
					}
					else
					{
						this.SetShellVisible(true);
						this._go.transform.position = main.transform.position;
						this._go.transform.rotation = Quaternion.identity;
						this._parkedCam = main;
						this.ApplyRadius(main);
						this.PushUniforms();
						SkyShell.Active = true;
					}
				}
			}
			catch (Exception ex)
			{
				try
				{
					this.SetShellVisible(false);
				}
				catch
				{
				}
				SkyShell.Active = false;
				this._log.LogWarning("SkyShell tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000281 RID: 641 RVA: 0x00026AD4 File Offset: 0x00024CD4
		private void SetShellVisible(bool on)
		{
			if (this.HalfResDraw)
			{
				on = false;
			}
			if (this._mr != null && this._mr.enabled != on)
			{
				this._mr.enabled = on;
			}
		}

		// Token: 0x06000282 RID: 642 RVA: 0x00026B0C File Offset: 0x00024D0C
		private void ApplyRadius(Camera cam)
		{
			float num;
			if (SkySystem.DomeValid && SkySystem.DomeDistance > 1f)
			{
				num = SkySystem.DomeDistance * 0.55f;
			}
			else
			{
				float backdropEstimate = SkySystem.BackdropEstimate;
				num = Mathf.Min(Mathf.Max((backdropEstimate > 1f) ? (backdropEstimate * 0.55f) : 80f, 80f), cam.farClipPlane * 0.5f);
			}
			num = Mathf.Max(num, 60f);
			float num2 = cam.farClipPlane * 0.8f;
			if (num2 > 1f)
			{
				num = Mathf.Min(num, num2);
			}
			if (this._appliedRadius > 0f && Mathf.Abs(num - this._appliedRadius) < this._appliedRadius * 0.01f)
			{
				return;
			}
			this._appliedRadius = num;
			this._go.transform.localScale = new Vector3(num, num, num);
			this._log.LogInfo(string.Format("SHELLRADIUS: {0:0}m (domeValid={1} ", num, SkySystem.DomeValid ? 1 : 0) + string.Format("domeDist={0:0}m backdropEst={1:0}m ", SkySystem.DomeDistance, SkySystem.BackdropEstimate) + string.Format("farClip={0:0}m) — while domeValid=0 this radius IS the ", cam.farClipPlane) + "occlusion cutoff: geometry NEARER than it hides the body via ZTest, geometry FARTHER than it still gets painted over (the 80m fallback was the 'moon shines through walls' bug).");
		}

		// Token: 0x06000283 RID: 643 RVA: 0x00026C48 File Offset: 0x00024E48
		private bool EnsureShell()
		{
			if (this._go != null)
			{
				return true;
			}
			if (this._mat == null)
			{
				RenderEngine engine = this._engine;
				Shader shader = ((engine != null) ? engine.GetShader("LumaLooks/SkyShell") : null);
				if (shader == null)
				{
					if (!this._shaderMissingLogged)
					{
						this._shaderMissingLogged = true;
						this._log.LogWarning("SkyShell: shader 'LumaLooks/SkyShell' not in the bundle — the 3D sun/moon/clouds are disabled (SkyReplace passes 2 and 3 keep running instead, so this degrades rather than goes dark).");
					}
					return false;
				}
				this._mat = new Material(shader)
				{
					hideFlags = (HideFlags)61
				};
				this._staticDirty = true;
				this._appliedDrawBody = -1f;
			}
			if (this._mesh == null)
			{
				this._mesh = SkyShell.BuildShellMesh();
			}
			this._go = new GameObject("LumaLooks_Firmament");
			this._go.transform.position = Vector3.zero;
			this._go.transform.rotation = Quaternion.identity;
			this._go.transform.localScale = Vector3.one;
			this._appliedRadius = -1f;
			this._go.AddComponent<MeshFilter>().sharedMesh = this._mesh;
			this._mr = this._go.AddComponent<MeshRenderer>();
			this._mr.sharedMaterial = this._mat;
			this._mr.shadowCastingMode = 0;
			this._mr.receiveShadows = false;
			this._mr.lightProbeUsage = 0;
			this._mr.reflectionProbeUsage = 0;
			this._mr.allowOcclusionWhenDynamic = false;
			if (!this._loggedOnline)
			{
				this._loggedOnline = true;
				this._log.LogInfo(string.Concat(new string[]
				{
					"SkyShell: LumaLooks_Firmament online — the sun, moon and clouds are now REAL ",
					string.Format("GEOMETRY inside GT's dome ({0}x{1} shell, ", 48, 16),
					"radius ",
					SkySystem.DomeValid ? "0.55x measured dome" : "default",
					"). SkyReplace passes 2 and 3 stand down; the shell owns both bodies and the cloud layer, so nothing is drawn twice."
				}));
			}
			return true;
		}

		// Token: 0x06000284 RID: 644 RVA: 0x00026E34 File Offset: 0x00025034
		private static Mesh BuildShellMesh()
		{
			float num = Mathf.Sin(-0.20943952f);
			Vector3[] array = new Vector3[833];
			int[] array2 = new int[4608];
			int num2 = 0;
			for (int i = 0; i <= 16; i++)
			{
				float num3 = Mathf.Lerp(num, 1f, (float)i / 16f);
				float num4 = Mathf.Sqrt(Mathf.Max(0f, 1f - num3 * num3));
				for (int j = 0; j <= 48; j++)
				{
					float num5 = (float)j / 48f * 3.1415927f * 2f;
					array[num2++] = new Vector3(Mathf.Cos(num5) * num4, num3, Mathf.Sin(num5) * num4);
				}
			}
			int num6 = 0;
			for (int k = 0; k < 16; k++)
			{
				for (int l = 0; l < 48; l++)
				{
					int num7 = k * 49 + l;
					int num8 = num7 + 1;
					int num9 = num7 + 49;
					int num10 = num9 + 1;
					array2[num6++] = num7;
					array2[num6++] = num9;
					array2[num6++] = num8;
					array2[num6++] = num8;
					array2[num6++] = num9;
					array2[num6++] = num10;
				}
			}
			Mesh mesh = new Mesh();
			mesh.name = "LumaLooks_FirmamentMesh";
			mesh.hideFlags = (HideFlags)61;
			mesh.vertices = array;
			mesh.triangles = array2;
			mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 2.2f);
			mesh.UploadMeshData(false);
			return mesh;
		}

		// Token: 0x06000285 RID: 645 RVA: 0x00026FCC File Offset: 0x000251CC
		private void PushUniforms()
		{
			if (this._mat == null)
			{
				return;
			}
			if (!this.HalfResDraw)
			{
				this._mat.SetVector(ShaderIds.ShellRTSize, Vector4.zero);
			}
			this._mat.SetVector(ShaderIds.SkySunDir, SkySystem.UniSunDir);
			this._mat.SetVector(ShaderIds.SkyParams, SkySystem.UniParams);
			this._mat.SetVector(ShaderIds.SkyParams3, SkySystem.UniParams3);
			this._mat.SetVector(ShaderIds.SkyBodyParams, SkySystem.UniBodyParams);
			this._mat.SetVector(ShaderIds.CloudParams2, SkySystem.UniCloudParams2);
			this._mat.SetVector(ShaderIds.CloudParams3, SkySystem.UniCloudParams3);
			this._mat.SetVector(ShaderIds.SkyReplaceParams2, SkySystem.UniReplaceParams2);
			float num = (SkyOverlay.BodiesLive ? 0f : 1f);
			if (num != this._appliedDrawBody)
			{
				this._appliedDrawBody = num;
				this._mat.SetFloat(ShaderIds.ShellDrawBody, num);
			}
			bool flag = RainSensor.RainFactor > 0f;
			if (!this._staticDirty && !flag && !this._rainPushed)
			{
				return;
			}
			this._staticDirty = false;
			this._rainPushed = flag;
			this._mat.SetVector(ShaderIds.SkySunTint, SkySystem.UniSunTint);
			this._mat.SetVector(ShaderIds.SkyMoonTint, SkySystem.UniMoonTint);
			this._mat.SetVector(ShaderIds.CloudParams, SkySystem.UniCloudParams);
			this._mat.SetVector(ShaderIds.CloudTint, SkySystem.UniCloudTint);
			Texture2D texture2D = ((this._engine != null) ? this._engine.GetTexture("MoonAlbedo") : null);
			if (texture2D != null)
			{
				this._mat.SetTexture(ShaderIds.MoonTex, texture2D);
			}
		}

		// Token: 0x06000286 RID: 646 RVA: 0x00027188 File Offset: 0x00025388
		public void SyncToCamera(Camera cam)
		{
			if (!SkyShell.Active || this._go == null || cam == null || cam != this._parkedCam)
			{
				return;
			}
			try
			{
				this._go.transform.position = cam.transform.position;
			}
			catch
			{
			}
		}

		// Token: 0x06000287 RID: 647 RVA: 0x000271F0 File Offset: 0x000253F0
		public void MarkDirty()
		{
			this._staticDirty = true;
		}

		// Token: 0x06000288 RID: 648 RVA: 0x000271FC File Offset: 0x000253FC
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
			this._mr = null;
			this._appliedRadius = -1f;
		}

		// Token: 0x06000289 RID: 649 RVA: 0x00027250 File Offset: 0x00025450
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
			this._loggedOnline = false;
			SkyShell.Active = false;
		}

		// Token: 0x04000627 RID: 1575
		private const string ShellShaderName = "LumaLooks/SkyShell";

		// Token: 0x04000628 RID: 1576
		private const string GoName = "LumaLooks_Firmament";

		// Token: 0x04000629 RID: 1577
		private const int Segments = 48;

		// Token: 0x0400062A RID: 1578
		private const int Rings = 16;

		// Token: 0x0400062B RID: 1579
		private const float MinElevationDeg = -12f;

		// Token: 0x0400062C RID: 1580
		private const float DomeFraction = 0.55f;

		// Token: 0x0400062D RID: 1581
		private const float DefaultRadius = 80f;

		// Token: 0x0400062E RID: 1582
		private const float MinRadius = 60f;

		// Token: 0x0400062F RID: 1583
		private const float FarClipFraction = 0.8f;

		// Token: 0x04000630 RID: 1584
		private readonly ManualLogSource _log;

		// Token: 0x04000631 RID: 1585
		private readonly RenderEngine _engine;

		// Token: 0x04000632 RID: 1586
		private GameObject _go;

		// Token: 0x04000633 RID: 1587
		private Camera _parkedCam;

		// Token: 0x04000634 RID: 1588
		private MeshRenderer _mr;

		// Token: 0x04000636 RID: 1590
		private Mesh _mesh;

		// Token: 0x04000637 RID: 1591
		private Material _mat;

		// Token: 0x04000638 RID: 1592
		private bool _want;

		// Token: 0x04000639 RID: 1593
		private bool _vrAllowed = true;

		// Token: 0x0400063A RID: 1594
		private bool _desktopAllowed = true;

		// Token: 0x0400063B RID: 1595
		private bool _staticDirty = true;

		// Token: 0x0400063C RID: 1596
		private bool _rainPushed;

		// Token: 0x0400063D RID: 1597
		private bool _loggedOnline;

		// Token: 0x0400063E RID: 1598
		private bool _shaderMissingLogged;

		// Token: 0x0400063F RID: 1599
		private float _appliedRadius = -1f;

		// Token: 0x04000640 RID: 1600
		private float _appliedDrawBody = -1f;
	}
}
