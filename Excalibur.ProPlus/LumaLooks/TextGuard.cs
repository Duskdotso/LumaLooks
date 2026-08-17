using System;
using System.Collections.Generic;
using BepInEx.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LumaLooks
{
	// Token: 0x02000052 RID: 82
	internal sealed class TextGuard
	{
		// Token: 0x060002FE RID: 766 RVA: 0x0002AC20 File Offset: 0x00028E20
		public TextGuard(ManualLogSource log)
		{
			this._log = log;
			SceneManager.sceneLoaded += new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
		}

		// Token: 0x1700006E RID: 110
		// (get) Token: 0x060002FF RID: 767 RVA: 0x0002ACBA File Offset: 0x00028EBA
		public bool HasText
		{
			get
			{
				return this._renderers.Count > 0 || this._rects.Count > 0;
			}
		}

		// Token: 0x06000300 RID: 768 RVA: 0x0002ACDA File Offset: 0x00028EDA
		public void Configure(bool on, bool vrAllowed, bool desktopAllowed)
		{
			if (on && !this._on)
			{
				this._nextScanAt = 0f;
			}
			this._on = on;
			this._vrAllowed = vrAllowed;
			this._desktopAllowed = desktopAllowed;
			this._dirty = true;
		}

		// Token: 0x06000301 RID: 769 RVA: 0x0002AD0E File Offset: 0x00028F0E
		private void OnSceneLoaded(Scene s, LoadSceneMode m)
		{
			this._renderers.Clear();
			this._rects.Clear();
			this._scanRenderers.Clear();
			this._scanRects.Clear();
			this._sceneJustLoaded = true;
		}

		// Token: 0x06000302 RID: 770 RVA: 0x0002AD44 File Offset: 0x00028F44
		public void Tick()
		{
			try
			{
				if (this._dirty)
				{
					this._dirty = false;
					if (!this._on)
					{
						this.DropAll();
					}
				}
				if (this._on)
				{
					if (!this._vrAllowed && !this._desktopAllowed)
					{
						this.DropAll();
					}
					else
					{
						float realtimeSinceStartup = Time.realtimeSinceStartup;
						if (this._sceneJustLoaded)
						{
							this._sceneJustLoaded = false;
							this._nextScanAt = realtimeSinceStartup + 2f;
						}
						if (realtimeSinceStartup >= this._nextScanAt)
						{
							this._nextScanAt = realtimeSinceStartup + 5f * PerfMode.ScanMul;
							this.Scan();
						}
					}
				}
			}
			catch (Exception ex)
			{
				this._log.LogWarning("TextGuard tick skipped: " + ex.Message);
			}
		}

		// Token: 0x06000303 RID: 771 RVA: 0x0002AE08 File Offset: 0x00029008
		private void DropAll()
		{
			this._renderers.Clear();
			this._rects.Clear();
			this._scanRenderers.Clear();
			this._scanRects.Clear();
		}

		// Token: 0x06000304 RID: 772 RVA: 0x0002AE38 File Offset: 0x00029038
		private void Scan()
		{
			this._scanRenderers.Clear();
			this._scanRects.Clear();
			this._truncated = false;
			foreach (TMP_Text tmp_Text in UnityEngine.Object.FindObjectsByType<TMP_Text>(0))
			{
				MeshRenderer meshRenderer = null;
				if (!(tmp_Text == null) && tmp_Text.TryGetComponent<MeshRenderer>(out meshRenderer) && meshRenderer != null)
				{
					this.AddRenderer(meshRenderer);
				}
			}
			foreach (TMP_SubMesh tmp_SubMesh in UnityEngine.Object.FindObjectsByType<TMP_SubMesh>(0))
			{
				MeshRenderer meshRenderer2 = null;
				if (!(tmp_SubMesh == null) && tmp_SubMesh.TryGetComponent<MeshRenderer>(out meshRenderer2) && meshRenderer2 != null)
				{
					this.AddRenderer(meshRenderer2);
				}
			}
			foreach (TextMesh textMesh in UnityEngine.Object.FindObjectsByType<TextMesh>(0))
			{
				MeshRenderer meshRenderer3 = null;
				if (!(textMesh == null) && textMesh.TryGetComponent<MeshRenderer>(out meshRenderer3) && meshRenderer3 != null)
				{
					this.AddRenderer(meshRenderer3);
				}
			}
			foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(0))
			{
				if (!(canvas == null) && canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
				{
					canvas.GetComponentsInChildren<Text>(false, this._uiTextScratch);
					for (int m = 0; m < this._uiTextScratch.Count; m++)
					{
						Text text = this._uiTextScratch[m];
						if (text != null)
						{
							this.AddRect(text);
						}
					}
					canvas.GetComponentsInChildren<TMP_Text>(false, this._tmpScratch);
					for (int n = 0; n < this._tmpScratch.Count; n++)
					{
						TMP_Text tmp_Text2 = this._tmpScratch[n];
						MeshRenderer meshRenderer4 = null;
						if (!(tmp_Text2 == null) && (!tmp_Text2.TryGetComponent<MeshRenderer>(out meshRenderer4) || !(meshRenderer4 != null)))
						{
							this.AddRect(tmp_Text2);
						}
					}
				}
			}
			this._uiTextScratch.Clear();
			this._tmpScratch.Clear();
			List<Renderer> renderers = this._renderers;
			this._renderers = this._scanRenderers;
			this._scanRenderers = renderers;
			this._scanRenderers.Clear();
			List<TextGuard.RectEntry> rects = this._rects;
			this._rects = this._scanRects;
			this._scanRects = rects;
			this._scanRects.Clear();
			if (this._renderers.Count != this._lastLoggedRenderers || this._rects.Count != this._lastLoggedRects || this._truncated != this._lastLoggedTruncated)
			{
				this._lastLoggedRenderers = this._renderers.Count;
				this._lastLoggedRects = this._rects.Count;
				this._lastLoggedTruncated = this._truncated;
				this._log.LogInfo(string.Format("TEXTGUARD: renderers={0} rects={1} (truncated={2})", this._renderers.Count, this._rects.Count, this._truncated));
			}
		}

		// Token: 0x06000305 RID: 773 RVA: 0x0002B12C File Offset: 0x0002932C
		private void AddRenderer(Renderer r)
		{
			if (this._scanRenderers.Count >= 128)
			{
				this._truncated = true;
				return;
			}
			this._scanRenderers.Add(r);
		}

		// Token: 0x06000306 RID: 774 RVA: 0x0002B154 File Offset: 0x00029354
		private void AddRect(Behaviour comp)
		{
			if (this._scanRects.Count >= 128)
			{
				this._truncated = true;
				return;
			}
			RectTransform rectTransform = comp.transform as RectTransform;
			if (rectTransform == null)
			{
				return;
			}
			this._scanRects.Add(new TextGuard.RectEntry
			{
				Rect = rectTransform,
				Comp = comp,
				Canvas = comp.GetComponentInParent<Canvas>(),
				Group = comp.GetComponentInParent<CanvasGroup>()
			});
		}

		// Token: 0x06000307 RID: 775 RVA: 0x0002B1D0 File Offset: 0x000293D0
		public void RecordMaskDraws(CommandBuffer cmd, Material mat, Mesh quad)
		{
			if (mat == null)
			{
				return;
			}
			List<Renderer> renderers = this._renderers;
			for (int i = 0; i < renderers.Count; i++)
			{
				Renderer renderer = renderers[i];
				if (!(renderer == null) && renderer.enabled && renderer.gameObject.activeInHierarchy)
				{
					cmd.DrawRenderer(renderer, mat, 0, 0);
				}
			}
			if (quad == null)
			{
				return;
			}
			List<TextGuard.RectEntry> rects = this._rects;
			for (int j = 0; j < rects.Count; j++)
			{
				TextGuard.RectEntry rectEntry = rects[j];
				RectTransform rect = rectEntry.Rect;
				if (!(rect == null) && !(rectEntry.Comp == null) && rectEntry.Comp.isActiveAndEnabled && !(rectEntry.Canvas == null) && rectEntry.Canvas.isActiveAndEnabled && (!(rectEntry.Group != null) || rectEntry.Group.alpha > 0.01f))
				{
					rect.GetWorldCorners(this._corners);
					Vector3 vector = this._corners[0];
					Vector3 vector2 = this._corners[3] - vector;
					Vector3 vector3 = this._corners[1] - vector;
					if (vector2.sqrMagnitude >= 1E-10f && vector3.sqrMagnitude >= 1E-10f)
					{
						Vector3 vector4 = Vector3.Cross(vector2, vector3);
						float magnitude = vector4.magnitude;
						if (magnitude > 1E-08f)
						{
							vector4 /= magnitude;
						}
						else
						{
							vector4 = Vector3.forward;
						}
						Matrix4x4 matrix4x = default(Matrix4x4);
						matrix4x.SetColumn(0, new Vector4(vector2.x, vector2.y, vector2.z, 0f));
						matrix4x.SetColumn(1, new Vector4(vector3.x, vector3.y, vector3.z, 0f));
						matrix4x.SetColumn(2, new Vector4(vector4.x, vector4.y, vector4.z, 0f));
						matrix4x.SetColumn(3, new Vector4(vector.x, vector.y, vector.z, 1f));
						cmd.DrawMesh(quad, matrix4x, mat, 0, 0);
					}
				}
			}
		}

		// Token: 0x06000308 RID: 776 RVA: 0x0002B42E File Offset: 0x0002962E
		public void Dispose()
		{
			SceneManager.sceneLoaded -= new UnityAction<Scene, LoadSceneMode>(this.OnSceneLoaded);
			this.DropAll();
		}

		// Token: 0x04000702 RID: 1794
		private const int MaxRenderers = 128;

		// Token: 0x04000703 RID: 1795
		private const int MaxRects = 128;

		// Token: 0x04000704 RID: 1796
		private const float ScanIntervalSeconds = 5f;

		// Token: 0x04000705 RID: 1797
		private const float SceneSettleSeconds = 2f;

		// Token: 0x04000706 RID: 1798
		private readonly ManualLogSource _log;

		// Token: 0x04000707 RID: 1799
		private List<Renderer> _renderers = new List<Renderer>(64);

		// Token: 0x04000708 RID: 1800
		private List<TextGuard.RectEntry> _rects = new List<TextGuard.RectEntry>(64);

		// Token: 0x04000709 RID: 1801
		private List<Renderer> _scanRenderers = new List<Renderer>(64);

		// Token: 0x0400070A RID: 1802
		private List<TextGuard.RectEntry> _scanRects = new List<TextGuard.RectEntry>(64);

		// Token: 0x0400070B RID: 1803
		private readonly List<Text> _uiTextScratch = new List<Text>(32);

		// Token: 0x0400070C RID: 1804
		private readonly List<TMP_Text> _tmpScratch = new List<TMP_Text>(32);

		// Token: 0x0400070D RID: 1805
		private readonly Vector3[] _corners = new Vector3[4];

		// Token: 0x0400070E RID: 1806
		private bool _on;

		// Token: 0x0400070F RID: 1807
		private bool _vrAllowed;

		// Token: 0x04000710 RID: 1808
		private bool _desktopAllowed = true;

		// Token: 0x04000711 RID: 1809
		private bool _dirty;

		// Token: 0x04000712 RID: 1810
		private float _nextScanAt;

		// Token: 0x04000713 RID: 1811
		private bool _sceneJustLoaded;

		// Token: 0x04000714 RID: 1812
		private bool _truncated;

		// Token: 0x04000715 RID: 1813
		private int _lastLoggedRenderers = -1;

		// Token: 0x04000716 RID: 1814
		private int _lastLoggedRects = -1;

		// Token: 0x04000717 RID: 1815
		private bool _lastLoggedTruncated;

		// Token: 0x02000053 RID: 83
		private struct RectEntry
		{
			// Token: 0x04000718 RID: 1816
			public RectTransform Rect;

			// Token: 0x04000719 RID: 1817
			public Behaviour Comp;

			// Token: 0x0400071A RID: 1818
			public Canvas Canvas;

			// Token: 0x0400071B RID: 1819
			public CanvasGroup Group;
		}
	}
}
