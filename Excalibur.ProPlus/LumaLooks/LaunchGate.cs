using System;
using System.IO;
using BepInEx;
using BepInEx.Logging;

namespace LumaLooks
{
	// Token: 0x02000012 RID: 18
	internal static class LaunchGate
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00008277 File Offset: 0x00006477
		public static bool Allowed
		{
			get
			{
				return true;
			}
		}

		// Token: 0x06000083 RID: 131 RVA: 0x0000827A File Offset: 0x0000647A
		public static void Resolve(ManualLogSource log)
		{
			LaunchGate._resolved = true;
			LaunchGate._allowed = true;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00008288 File Offset: 0x00006488
		public static void Poll(ManualLogSource log)
		{
			if (LaunchGate._allowed)
			{
				return;
			}
			LaunchGate.TryToken(log, false);
		}

		// Token: 0x06000085 RID: 133 RVA: 0x0000829C File Offset: 0x0000649C
		private static void TryToken(ManualLogSource log, bool startup)
		{
			string text = null;
			try
			{
				text = Path.Combine(Paths.ConfigPath, "LumaLooks", "launch.token");
				if (!File.Exists(text))
				{
					if (startup && !LaunchGate._loggedStartupMiss)
					{
						LaunchGate._loggedStartupMiss = true;
						if (log != null)
						{
							log.LogInfo("LAUNCHGATE: no launch token — Gorilla Tag was not started from the Luma Looks app, so the engine stays dormant (vanilla rendering). Launch through the app to enable effects. NOTE: if the game was ALREADY RUNNING when you pressed Launch, effects now switch on within a second of pressing it — no relaunch needed (live token poll).");
						}
					}
				}
				else
				{
					string text2 = File.ReadAllText(text).Trim();
					try
					{
						File.Delete(text);
					}
					catch (Exception ex)
					{
						if (log != null)
						{
							log.LogWarning("LAUNCHGATE: could not consume the token (" + ex.Message + ").");
						}
					}
					long num;
					if (!long.TryParse(text2, out num))
					{
						if (log != null)
						{
							log.LogWarning("LAUNCHGATE: token unreadable — staying dormant.");
						}
					}
					else
					{
						DateTime utcDateTime = DateTimeOffset.FromUnixTimeSeconds(num).UtcDateTime;
						TimeSpan timeSpan = DateTime.UtcNow - utcDateTime;
						if (timeSpan < TimeSpan.FromMinutes(-5.0) || timeSpan > LaunchGate.MaxAge)
						{
							if (log != null)
							{
								log.LogWarning(string.Format("LAUNCHGATE: token is stale ({0:0.#} min old, limit ", timeSpan.TotalMinutes) + string.Format("{0:0}) — staying dormant. Launch through the app again.", LaunchGate.MaxAge.TotalMinutes));
							}
						}
						else
						{
							LaunchGate._allowed = true;
							if (log != null)
							{
								log.LogInfo(string.Format("LAUNCHGATE: launched from the Luma Looks app ({0:0}s ago) — engine active", timeSpan.TotalSeconds) + (startup ? "." : " (activated on the already-running game via the live token poll — no relaunch needed)."));
							}
						}
					}
				}
			}
			catch (Exception ex2)
			{
				if (log != null)
				{
					log.LogWarning(string.Concat(new string[] { "LAUNCHGATE: could not evaluate the launch token at '", text, "' (", ex2.Message, ") — staying dormant." }));
				}
				LaunchGate._allowed = false;
			}
		}

		// Token: 0x040000EC RID: 236
		private const string TokenFileName = "launch.token";

		// Token: 0x040000ED RID: 237
		private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(15.0);

		// Token: 0x040000EE RID: 238
		private static bool _resolved;

		// Token: 0x040000EF RID: 239
		private static bool _allowed;

		// Token: 0x040000F0 RID: 240
		private static bool _loggedStartupMiss;
	}
}
