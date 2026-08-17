using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace LumaLooks
{
	// Token: 0x02000043 RID: 67
	internal sealed class EffectSettings
	{
		// Token: 0x06000229 RID: 553 RVA: 0x0002229C File Offset: 0x0002049C
		public float GetFloat(string id, float fallback)
		{
			object obj;
			if (this.Pars.TryGetValue(id, out obj) && obj != null)
			{
				try
				{
					return Convert.ToSingle(obj, CultureInfo.InvariantCulture);
				}
				catch
				{
				}
				return fallback;
			}
			return fallback;
		}

		// Token: 0x0600022A RID: 554 RVA: 0x000222E0 File Offset: 0x000204E0
		public string GetEnum(string id, string fallback)
		{
			object obj;
			if (this.Pars.TryGetValue(id, out obj))
			{
				string text = obj as string;
				if (text != null)
				{
					return text;
				}
			}
			return fallback;
		}

		// Token: 0x0600022B RID: 555 RVA: 0x0002230C File Offset: 0x0002050C
		public int GetEnumIndex(string id, string[] options, int fallback)
		{
			string @enum = this.GetEnum(id, null);
			if (@enum != null)
			{
				for (int i = 0; i < options.Length; i++)
				{
					if (string.Equals(options[i], @enum, StringComparison.OrdinalIgnoreCase))
					{
						return i;
					}
				}
			}
			return fallback;
		}

		// Token: 0x0600022C RID: 556 RVA: 0x00022344 File Offset: 0x00020544
		public Color GetColor(string id, Color fallback)
		{
			object obj;
			if (this.Pars.TryGetValue(id, out obj))
			{
				string text = obj as string;
				Color color = default;
				if (text != null && ColorUtility.TryParseHtmlString(text, out color))
				{
					return color;
				}
			}
			return fallback;
		}

		// Token: 0x040004C8 RID: 1224
		public bool Enabled;

		// Token: 0x040004C9 RID: 1225
		public bool Vr;

		// Token: 0x040004CA RID: 1226
		public bool Desktop;

		// Token: 0x040004CB RID: 1227
		public readonly Dictionary<string, object> Pars = new Dictionary<string, object>();
	}
}
