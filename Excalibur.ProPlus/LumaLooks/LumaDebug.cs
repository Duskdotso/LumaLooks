using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace LumaLooks
{
	// Token: 0x02000013 RID: 19
	internal static class LumaDebug
	{
		// Token: 0x06000087 RID: 135 RVA: 0x0000848C File Offset: 0x0000668C
		internal static void Resolve(ManualLogSource log)
		{
			try
			{
				string text = Path.Combine(Path.Combine(Paths.ConfigPath, "LumaLooks"), "RAYDEBUG");
				if (!File.Exists(text))
				{
					LumaDebug.RayDebug = 0f;
				}
				else
				{
					int num = 1;
					try
					{
						string text2 = File.ReadAllText(text).Trim();
						int num2;
						if (text2.Length > 0 && int.TryParse(text2, out num2))
						{
							num = num2;
						}
					}
					catch
					{
					}
					LumaDebug.RayDebug = (float)num;
					if (log != null)
					{
						log.LogWarning(string.Format("LumaDebug: RAYDEBUG flag present -> _LumaRayDebug={0} ", num) + "(mode 1 = depth classification: RED pixels read as SKY, GREEN as SOLID; a rock showing RED proves a depth-write failure). Delete '" + text + "' to return to normal.");
					}
				}
			}
			catch (Exception ex)
			{
				LumaDebug.RayDebug = 0f;
				if (log != null)
				{
					log.LogWarning("LumaDebug: flag resolve skipped: " + ex.Message);
				}
			}
		}

		// Token: 0x040000F1 RID: 241
		internal static float RayDebug;
	}
}
