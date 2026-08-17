using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace LumaLooks
{
	internal static class LumaAssets
	{
		internal static byte[] Bundle()
		{
			return LumaAssets.Read("LumaLooks.lumalooks.bundle");
		}

		internal static string Defaults()
		{
			byte[] array = LumaAssets.Read("LumaLooks.LumaLooks.default-settings.json");
			if (array == null)
			{
				return null;
			}
			try
			{
				return new UTF8Encoding(false).GetString(array);
			}
			catch
			{
				return null;
			}
		}

		private static byte[] Read(string name)
		{
			try
			{
				Assembly assembly = Assembly.GetExecutingAssembly();
				string resourceName = name;
				string[] manifestResourceNames = assembly.GetManifestResourceNames();
				if (Array.IndexOf(manifestResourceNames, name) < 0)
				{
					foreach (string candidate in manifestResourceNames)
					{
						if (candidate != null && candidate.EndsWith(name, StringComparison.OrdinalIgnoreCase))
						{
							resourceName = candidate;
							break;
						}
					}
				}
				using (Stream manifestResourceStream = assembly.GetManifestResourceStream(resourceName))
				{
					if (manifestResourceStream == null)
					{
						return null;
					}
					using (MemoryStream memoryStream = new MemoryStream())
					{
						manifestResourceStream.CopyTo(memoryStream);
						byte[] bytes = memoryStream.ToArray();
						return bytes.Length != 0 ? bytes : null;
					}
				}
			}
			catch
			{
				return null;
			}
		}
	}
}
