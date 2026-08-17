using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using BepInEx;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace LumaLooks
{
	internal sealed class LumaSettingsGui : MonoBehaviour
	{
		private enum Page
		{
			Groups,
			Home,
			Effects,
			Params,
			Presets
		}

		private struct PresetRec
		{
			public string Name;
			public string Desc;
			public string Json;
			public bool Mine;
		}

		private static readonly string[] Groups =
		{
			"Luma Looks",
			"Camera & Focus",
			"Lighting & Glow",
			"Sky & Atmosphere",
			"Particles",
			"Cinematic",
			"Colour Grade",
			"Video FX",
			"Misc"
		};

		private static readonly Dictionary<string, KeyValuePair<int, string>> GroupOf = BuildGroupMap();

		private bool _visible;
		private Page _page = Page.Groups;
		private int _group;
		private string _effectId;
		private Vector2 _scroll;
		private readonly List<PresetRec> _presets = new List<PresetRec>();
		private Rect _window = new Rect(20f, 20f, 420f, 560f);
		private KeyCode _toggleKey = KeyCode.L;
		private KeyCode _masterToggleKey = KeyCode.L;
		private bool _masterToggleShift = true;

		internal static LumaSettingsGui Instance { get; private set; }

		internal void Configure(KeyCode panelKey, KeyCode masterKey, bool masterShift)
		{
			_toggleKey = panelKey;
			_masterToggleKey = masterKey;
			_masterToggleShift = masterShift;
		}

		private void Awake()
		{
			Instance = this;
		}

		private void OnDestroy()
		{
			if (Instance == this)
			{
				Instance = null;
			}
		}

		private void Update()
		{
			if (UnityInput.Current.GetKeyDown(_toggleKey))
			{
				_visible = !_visible;
			}
			if (HeldShift() == _masterToggleShift && HeldCtrl() == false && HeldAlt() == false && UnityInput.Current.GetKeyDown(_masterToggleKey))
			{
				ToggleMasterEngine();
			}
		}

		private void OnGUI()
		{
			if (!_visible)
			{
				return;
			}
			_window = GUILayout.Window(47801, _window, DrawWindow, "Luma Looks Settings");
		}

		private void DrawWindow(int id)
		{
			_scroll = GUILayout.BeginScrollView(_scroll);
			switch (_page)
			{
			case Page.Groups:
				DrawGroupsPage();
				break;
			case Page.Home:
				DrawHomePage();
				break;
			case Page.Effects:
				DrawEffectsPage();
				break;
			case Page.Params:
				DrawParamsPage();
				break;
			case Page.Presets:
				DrawPresetsPage();
				break;
			}
			GUILayout.EndScrollView();
			GUI.DragWindow(new Rect(0f, 0f, 10000f, 24f));
		}

		private void DrawGroupsPage()
		{
			GUILayout.Label("Effect Categories", GUI.skin.box);
			for (int i = 0; i < Groups.Length; i++)
			{
				int on;
				int total;
				GroupCount(i, out on, out total);
				string label = Groups[i];
				if (i > 0 && total > 0)
				{
					label += string.Format("  ({0}/{1} on)", on, total);
				}
				if (GUILayout.Button(label))
				{
					_group = i;
					_page = i == 0 ? Page.Home : Page.Effects;
				}
			}
			GUILayout.Space(8f);
			if (GUILayout.Button("Presets"))
			{
				LoadPresets();
				_page = Page.Presets;
			}
			GUILayout.Label(EffectsOn() + " of " + Manifest.Effects.Length + " effects on");
		}

		private void DrawHomePage()
		{
			if (GUILayout.Button("< Back to categories"))
			{
				_page = Page.Groups;
				return;
			}
			Settings settings = Live;
			if (settings == null)
			{
				GUILayout.Label("Engine not ready.");
				return;
			}
			bool master = GUILayout.Toggle(settings.MasterPreference, "Master");
			if (master != settings.MasterPreference)
			{
				settings.Master = master;
				Apply();
			}
			bool vrAll = GUILayout.Toggle(settings.VrAll, "Headset FX (VR All)");
			if (vrAll != settings.VrAll)
			{
				settings.VrAll = vrAll;
				Apply();
			}
			int effectsOn = EffectsOn();
			bool enableAll = effectsOn == 0;
			if (GUILayout.Button(enableAll ? "Enable All Effects" : "Disable All Effects"))
			{
				SetAllEffects(settings, enableAll);
				Apply();
			}
			int qualityVr = settings.QualityVr;
			GUILayout.Label("Quality (Headset): " + QualityTiers.Name(qualityVr));
			int newQualityVr = Mathf.RoundToInt(GUILayout.HorizontalSlider(qualityVr, 0f, 4f));
			if (newQualityVr != qualityVr)
			{
				settings.QualityVr = newQualityVr;
				Apply();
			}
			int qualityDesktop = settings.QualityDesktop;
			GUILayout.Label("Quality (Desktop): " + QualityTiers.Name(qualityDesktop));
			int newQualityDesktop = Mathf.RoundToInt(GUILayout.HorizontalSlider(qualityDesktop, 0f, 4f));
			if (newQualityDesktop != qualityDesktop)
			{
				settings.QualityDesktop = newQualityDesktop;
				Apply();
			}
			GUILayout.Label("Effects on: " + effectsOn);
			GUILayout.Label("Engine: " + (LumaEngineBehaviour.Instance != null ? "online" : "offline"));
		}

		private void DrawEffectsPage()
		{
			if (GUILayout.Button("< Back to categories"))
			{
				_page = Page.Groups;
				return;
			}
			GUILayout.Label(Groups[_group], GUI.skin.box);
			Settings settings = Live;
			if (settings == null)
			{
				GUILayout.Label("Engine not ready.");
				return;
			}
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				if (effectDef == null || string.IsNullOrEmpty(effectDef.Id) || GroupOfEffect(effectDef.Id) != _group)
				{
					continue;
				}
				EffectSettings fx = settings.Effect(effectDef.Id);
				if (fx == null)
				{
					continue;
				}
				GUILayout.BeginHorizontal();
				bool enabled = GUILayout.Toggle(fx.Enabled, TitleOf(effectDef.Id), GUILayout.Width(220f));
				if (enabled != fx.Enabled)
				{
					fx.Enabled = enabled;
					Apply();
				}
				if (GUILayout.Button("Settings", GUILayout.Width(80f)))
				{
					_effectId = effectDef.Id;
					_page = Page.Params;
				}
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				GUILayout.Space(24f);
				bool vr = GUILayout.Toggle(fx.Vr, "VR");
				bool desktop = GUILayout.Toggle(fx.Desktop, "Desktop");
				if (vr != fx.Vr || desktop != fx.Desktop)
				{
					fx.Vr = vr;
					fx.Desktop = desktop;
					Apply();
				}
				GUILayout.EndHorizontal();
			}
		}

		private void DrawParamsPage()
		{
			if (GUILayout.Button("< Back to effects"))
			{
				_page = Page.Effects;
				return;
			}
			Settings settings = Live;
			EffectDef effectDef = EffectDefOf(_effectId);
			EffectSettings fx = settings != null ? settings.Effect(_effectId) : null;
			if (settings == null || effectDef == null || fx == null)
			{
				GUILayout.Label("Effect not available.");
				return;
			}
			GUILayout.Label(TitleOf(_effectId), GUI.skin.box);
			bool enabled = GUILayout.Toggle(fx.Enabled, "Enabled");
			if (enabled != fx.Enabled)
			{
				fx.Enabled = enabled;
				Apply();
			}
			if (effectDef.Params == null || effectDef.Params.Length == 0)
			{
				GUILayout.Label("No settings — on or off only.");
				return;
			}
			foreach (ParamDef param in effectDef.Params)
			{
				if (param == null || string.IsNullOrEmpty(param.Id))
				{
					continue;
				}
				if (!fx.Enabled)
				{
					GUI.enabled = false;
				}
				DrawParam(fx, param);
				GUI.enabled = true;
			}
		}

		private void DrawParam(EffectSettings fx, ParamDef param)
		{
			string label = Pretty(param.Id);
			switch (param.Type)
			{
			case ParamType.Float:
			{
				float value = fx.GetFloat(param.Id, param.Def);
				GUILayout.Label(label + ": " + value.ToString("0.##"));
				float newValue = GUILayout.HorizontalSlider(value, param.Min, param.Max);
				if (Math.Abs(newValue - value) > 0.0001f)
				{
					fx.Pars[param.Id] = newValue;
					Apply();
				}
				break;
			}
			case ParamType.Enum:
			{
				string current = fx.GetEnum(param.Id, param.DefStr ?? "");
				GUILayout.BeginHorizontal();
				GUILayout.Label(label, GUILayout.Width(160f));
				if (GUILayout.Button("<", GUILayout.Width(28f)))
				{
					StepEnum(fx, param, -1);
				}
				GUILayout.Label(current, GUILayout.Width(120f));
				if (GUILayout.Button(">", GUILayout.Width(28f)))
				{
					StepEnum(fx, param, 1);
				}
				GUILayout.EndHorizontal();
				break;
			}
			case ParamType.Bool:
			{
				bool on = fx.GetFloat(param.Id, param.Def) > 0.5f;
				bool newOn = GUILayout.Toggle(on, label);
				if (newOn != on)
				{
					fx.Pars[param.Id] = newOn ? 1f : 0f;
					Apply();
				}
				break;
			}
			case ParamType.Color:
			{
				string current = fx.GetEnum(param.Id, param.DefStr ?? "#FFFFFF");
				GUILayout.BeginHorizontal();
				GUILayout.Label(label, GUILayout.Width(160f));
				string hex = GUILayout.TextField(current, GUILayout.Width(120f));
				GUILayout.EndHorizontal();
				if (!string.Equals(hex, current, StringComparison.OrdinalIgnoreCase))
				{
					Color parsed;
					string normalized = hex.StartsWith("#", StringComparison.Ordinal) ? hex : "#" + hex;
					if (ColorUtility.TryParseHtmlString(normalized, out parsed))
					{
						fx.Pars[param.Id] = normalized;
						Apply();
					}
				}
				break;
			}
			}
		}

		private void DrawPresetsPage()
		{
			if (GUILayout.Button("< Back to categories"))
			{
				_page = Page.Groups;
				return;
			}
			if (GUILayout.Button("Reload Presets"))
			{
				LoadPresets();
			}
			if (_presets.Count == 0)
			{
				GUILayout.Label("No presets found.");
				return;
			}
			string lastHeader = null;
			for (int i = 0; i < _presets.Count; i++)
			{
				PresetRec preset = _presets[i];
				if (preset.Mine && lastHeader != "mine")
				{
					lastHeader = "mine";
					GUILayout.Label("Your Presets", GUI.skin.box);
				}
				else if (!preset.Mine && lastHeader != "built-in")
				{
					lastHeader = "built-in";
					GUILayout.Label("Built-in Presets", GUI.skin.box);
				}
				GUILayout.BeginVertical(GUI.skin.box);
				GUILayout.Label(preset.Name);
				if (!string.IsNullOrEmpty(preset.Desc))
				{
					GUILayout.Label(preset.Desc);
				}
				if (GUILayout.Button("Apply"))
				{
					ApplyPreset(preset);
				}
				GUILayout.EndVertical();
			}
		}

		private static Settings Live
		{
			get
			{
				LumaEngineBehaviour engine = LumaEngineBehaviour.Instance;
				return engine != null ? engine.Live : null;
			}
		}

		private static void Apply()
		{
			LumaEngineBehaviour engine = LumaEngineBehaviour.Instance;
			if (engine != null)
			{
				engine.ApplyAll();
			}
		}

		private void ToggleMasterEngine()
		{
			LumaEngineBehaviour engine = LumaEngineBehaviour.Instance;
			if (engine == null)
			{
				return;
			}
			Settings settings = engine.Live;
			if (settings == null)
			{
				return;
			}
			settings.Master = !settings.MasterPreference;
			engine.ApplyAll();
		}

		private void ApplyPreset(PresetRec preset)
		{
			LumaEngineBehaviour engine = LumaEngineBehaviour.Instance;
			if (engine == null || string.IsNullOrEmpty(preset.Json))
			{
				return;
			}
			engine.ApplySettingsJson(preset.Json);
		}

		private static int GroupOfEffect(string effectId)
		{
			KeyValuePair<int, string> pair;
			if (effectId == null || !GroupOf.TryGetValue(effectId, out pair))
			{
				return -1;
			}
			return pair.Key;
		}

		private static string TitleOf(string effectId)
		{
			KeyValuePair<int, string> pair;
			if (effectId == null || !GroupOf.TryGetValue(effectId, out pair))
			{
				return Pretty(effectId ?? "");
			}
			return pair.Value;
		}

		private static EffectDef EffectDefOf(string id)
		{
			return string.IsNullOrEmpty(id) ? null : Manifest.Get(id);
		}

		private static void GroupCount(int group, out int on, out int total)
		{
			on = 0;
			total = 0;
			Settings live = Live;
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				if (effectDef == null || string.IsNullOrEmpty(effectDef.Id) || GroupOfEffect(effectDef.Id) != group)
				{
					continue;
				}
				total++;
				EffectSettings fx = live != null ? live.Effect(effectDef.Id) : null;
				if (fx != null && fx.Enabled)
				{
					on++;
				}
			}
		}

		private static int EffectsOn()
		{
			int count = 0;
			Settings live = Live;
			if (live == null)
			{
				return 0;
			}
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				if (effectDef == null || string.IsNullOrEmpty(effectDef.Id))
				{
					continue;
				}
				EffectSettings fx = live.Effect(effectDef.Id);
				if (fx != null && fx.Enabled)
				{
					count++;
				}
			}
			return count;
		}

		private static void SetAllEffects(Settings settings, bool enabled)
		{
			foreach (EffectDef effectDef in Manifest.Effects)
			{
				if (effectDef == null || string.IsNullOrEmpty(effectDef.Id))
				{
					continue;
				}
				EffectSettings fx = settings.Effect(effectDef.Id);
				if (fx != null)
				{
					fx.Enabled = enabled;
				}
			}
		}

		private static void StepEnum(EffectSettings fx, ParamDef param, int dir)
		{
			if (param.Options == null || param.Options.Length == 0)
			{
				return;
			}
			int index = fx.GetEnumIndex(param.Id, param.Options, 0);
			index = (index + dir) % param.Options.Length;
			if (index < 0)
			{
				index += param.Options.Length;
			}
			fx.Pars[param.Id] = param.Options[index];
			Apply();
		}

		private void LoadPresets()
		{
			_presets.Clear();
			string appData = null;
			try
			{
				appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			}
			catch
			{
			}
			string bundleDir = LumaEngineBehaviour.BundleDir;
			if (string.IsNullOrEmpty(bundleDir) && appData != null)
			{
				bundleDir = Path.Combine(appData, "gorilla-tag-mod-manager", "mod-runtime");
			}
			if (!string.IsNullOrEmpty(bundleDir))
			{
				ReadPresetFile(Path.Combine(bundleDir, "luma-presets.json"), false);
			}
			if (appData != null)
			{
				ReadPresetFile(Path.Combine(appData, "gorilla-tag-mod-manager", "luma-user-presets.json"), true);
			}
		}

		private void ReadPresetFile(string path, bool mine)
		{
			try
			{
				if (!File.Exists(path))
				{
					return;
				}
				foreach (JToken token in JArray.Parse(File.ReadAllText(path)))
				{
					string name = (string)token["name"];
					JToken settings = token["settings"];
					if (string.IsNullOrEmpty(name) || settings == null)
					{
						continue;
					}
					_presets.Add(new PresetRec
					{
						Name = name,
						Desc = (string)token["description"],
						Json = settings.ToString(Formatting.None),
						Mine = mine
					});
				}
			}
			catch (Exception ex)
			{
				LumaEngineBehaviour.Log?.LogWarning("[luma] preset file unreadable (" + path + "): " + ex.Message);
			}
		}

		private static string Pretty(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return "";
			}
			StringBuilder builder = new StringBuilder(id.Length + 4);
			for (int i = 0; i < id.Length; i++)
			{
				char c = id[i];
				if (i > 0 && char.IsUpper(c))
				{
					builder.Append(' ');
				}
				builder.Append(char.ToUpperInvariant(c));
			}
			return builder.ToString();
		}

		private static bool HeldShift()
		{
			return UnityInput.Current.GetKey(KeyCode.LeftShift) || UnityInput.Current.GetKey(KeyCode.RightShift);
		}

		private static bool HeldCtrl()
		{
			return UnityInput.Current.GetKey(KeyCode.LeftControl) || UnityInput.Current.GetKey(KeyCode.RightControl);
		}

		private static bool HeldAlt()
		{
			return UnityInput.Current.GetKey(KeyCode.LeftAlt) || UnityInput.Current.GetKey(KeyCode.RightAlt);
		}

		private static Dictionary<string, KeyValuePair<int, string>> BuildGroupMap()
		{
			return new Dictionary<string, KeyValuePair<int, string>>
			{
				{ "fxaa", Pair(1, "Anti-Aliasing") },
				{ "sharpen", Pair(1, "Sharpen") },
				{ "motionBlur", Pair(1, "Motion Blur") },
				{ "dof", Pair(1, "Depth of Field") },
				{ "distanceBlur", Pair(1, "Distance Blur") },
				{ "ssgi", Pair(2, "Ray-Traced GI") },
				{ "trueDarkness", Pair(2, "True Darkness") },
				{ "bloom", Pair(2, "Bloom") },
				{ "ssao", Pair(2, "Ambient Occlusion") },
				{ "ssr", Pair(2, "Screen-Space Reflections") },
				{ "sunMoon", Pair(3, "Sun & Moon") },
				{ "nightSky", Pair(3, "Luma Sky") },
				{ "sunlight", Pair(3, "Volumetric Rays") },
				{ "lensFlare", Pair(3, "Lens Flare") },
				{ "lumaRain", Pair(3, "Luma Rain") },
				{ "clouds", Pair(3, "Clouds") },
				{ "haze", Pair(3, "Atmospheric Haze") },
				{ "underwater", Pair(3, "Underwater") },
				{ "dustMotes", Pair(4, "Dust Motes") },
				{ "fireflies", Pair(4, "Fireflies") },
				{ "embers", Pair(4, "Embers") },
				{ "fallingLeaves", Pair(4, "Falling Leaves") },
				{ "filmLook", Pair(5, "Film Look") },
				{ "drama", Pair(5, "Drama") },
				{ "letterbox", Pair(5, "Letterbox Bars") },
				{ "vignette", Pair(5, "Vignette") },
				{ "grain", Pair(5, "Film Grain") },
				{ "chromatic", Pair(5, "Chromatic Aberration") },
				{ "tonemap", Pair(6, "Tonemapping") },
				{ "colorAdjust", Pair(6, "Colour Adjustments") },
				{ "whiteBalance", Pair(6, "White Balance") },
				{ "adaptive", Pair(6, "Adaptive Colour Correction") },
				{ "halftone", Pair(7, "Halftone") },
				{ "cartoon", Pair(7, "Cartoon") },
				{ "scanlines", Pair(7, "Scanlines") },
				{ "pixelate", Pair(7, "Pixelate") },
				{ "birds", Pair(8, "Birds") },
				{ "butterflies", Pair(8, "Butterflies") },
				{ "bees", Pair(8, "Bees") }
			};
		}

		private static KeyValuePair<int, string> Pair(int group, string title)
		{
			return new KeyValuePair<int, string>(group, title);
		}
	}
}
