using System;
using System.IO;
using System.Text;
using BepInEx;
using LumaLooks;
using UnityEngine;

namespace LumaLooks
{
	[BepInPlugin(LumaEngineBehaviour.Guid, "Luma Looks", "1.0.0")]
	public sealed class LumaLooksPlugin : BaseUnityPlugin
	{
		private GameObject _host;

		private void Awake()
		{
			try
			{
				LumaEngineBehaviour.Log = base.Logger;
				LumaEngineBehaviour.BundleBytes = LumaAssets.Bundle();
				LumaEngineBehaviour.BundleDir = ResolveBundleDir();
				if (LumaEngineBehaviour.BundleBytes == null && LumaEngineBehaviour.BundleDir == null)
				{
					base.Logger.LogError("Luma Looks: no shader bundle embedded and none on disk — the engine will come up with no shaders.");
				}
				SeedDefaultSettings();
				this._host = new GameObject("LumaLooksEngine");
				UnityEngine.Object.DontDestroyOnLoad(this._host);
				this._host.hideFlags = HideFlags.HideAndDontSave;
				this._host.AddComponent<LumaEngineBehaviour>();
				LumaSettingsGui gui = this._host.AddComponent<LumaSettingsGui>();
				gui.Configure(KeyCode.L, KeyCode.L, true);
				base.Logger.LogInfo("Luma Looks loaded. Press L to open settings, Shift+L to toggle master.");
			}
			catch (Exception ex)
			{
				base.Logger.LogError("Luma Looks failed to start: " + ex);
			}
		}

		private void OnDestroy()
		{
			if (this._host != null)
			{
				UnityEngine.Object.Destroy(this._host);
				this._host = null;
			}
		}

		private static void SeedDefaultSettings()
		{
			try
			{
				string defaults = LumaAssets.Defaults();
				if (string.IsNullOrEmpty(defaults))
				{
					return;
				}
				string configDir = Path.Combine(Paths.ConfigPath, "LumaLooks");
				string settingsPath = Path.Combine(configDir, "settings.json");
				if (!File.Exists(settingsPath))
				{
					Directory.CreateDirectory(configDir);
					File.WriteAllText(settingsPath, defaults, new UTF8Encoding(false));
				}
			}
			catch (Exception ex)
			{
				LumaEngineBehaviour.Log?.LogWarning("Luma Looks: default-settings seed failed: " + ex.Message);
			}
		}

		private static string ResolveBundleDir()
		{
			try
			{
				string modRuntime = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "gorilla-tag-mod-manager", "mod-runtime");
				if (File.Exists(Path.Combine(modRuntime, "lumalooks.bundle")))
				{
					return modRuntime;
				}
			}
			catch
			{
			}
			try
			{
				string pluginDir = Path.Combine(Paths.GameRootPath, "BepInEx", "plugins", "LumaLooks");
				if (File.Exists(Path.Combine(pluginDir, "lumalooks.bundle")))
					return pluginDir;
			}
			catch
			{
			}
			return null;
		}
	}
}
