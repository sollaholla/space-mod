using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using System.Xml;
using GTA;
using GTA.Math;
using GTA.Native;
using SolomanMenu;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.Scenario;
using SpaceMod.Scenes;
//using Control = GTA.Control;
using Menu = SolomanMenu.Menu;

namespace SpaceMod
{
	public class Core : Script
	{
		private float _enterOrbitHeight = 5000;

		private Keys _optionsMenuKey = Keys.NumPad9;

		private MenuConnector _menuConnector;

		private Menu _menu;

		private readonly object _tickLock;

		private CustomScene _currentScene;

		public Core()
		{
			_tickLock = new object();

			Scenarios = new List<CustomScenario>();

			Instance = this;
			KeyUp += OnKeyUp;
			Tick += OnTick;
			Aborted += OnAborted;

			ReadSettings();
			SaveSettings();

			CreateCustomMenu();
		}

		public Ped PlayerPed => Game.Player.Character;

		public Vector3 PlayerPosition {
			get { return PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle.Position : PlayerPed.Position; }
			set {
				if (PlayerPed.IsInVehicle())
				{
					PlayerPed.CurrentVehicle.Position = value;
				}
				else
				{
					PlayerPed.Position = value;
				}
			}
		}

		public List<CustomScenario> Scenarios { get; private set; }

		internal static Core Instance { get; private set; }

		internal CustomScene GetCurrentScene()
		{
			return _currentScene;
		}

		private void OnAborted(object sender, EventArgs eventArgs)
		{
			SpaceModLib.SetGravityLevel(0);

			if (!PlayerPed.IsDead)
				Game.FadeScreenIn(0);

			PlayerPed.HasGravity = true;
			PlayerPed.FreezePosition = false;

			Game.TimeScale = 1.0f;
			World.RenderingCamera = null;
			_currentScene?.Delete();

			if (Scenarios != null)
			{
				while (Scenarios.Count > 0)
				{
					var scen = Scenarios[0];
					scen.OnAborted();
					Scenarios.RemoveAt(0);
				}
			}

			if (_currentScene != null && _currentScene != default(CustomScene))
				PlayerPosition = SpaceModDatabase.TrevorAirport;

			if (_currentScene != null)
				ResetWeather();

			_currentScene = null;
		}

		private void OnKeyUp(object sender, KeyEventArgs keyEventArgs)
		{
			if (_menuConnector.AreMenusVisible()) return;
			if (keyEventArgs.KeyCode != _optionsMenuKey) return;
			_menu.Draw = true;
		}

		private void OnTick(object sender, EventArgs eventArgs)
		{

			if (!Monitor.TryEnter(_tickLock)) return;

			try
			{
				_menuConnector.UpdateMenus();

				if (PlayerPed.IsDead && _currentScene != null)
				{
					CurrentSceneOnExited(null, "cmd_earth", Vector3.Zero);

					return;
				}

				DisableWantedStars();

				if (_currentScene != null)
				{
					Scenarios?.ForEach(scenario => scenario.Update());

					Function.Call(Hash._CLEAR_CLOUD_HAT);

					TimeType timeType = _currentScene.SceneData.CurrentIplData?.Time ?? _currentScene.SceneData.Time;

					switch (timeType)
					{
						case TimeType.Night:
							World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 22, 0, 0);
							break;
						case TimeType.Day:
							World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 9, 0, 0);
							break;
						case TimeType.Evening:
							World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 18, 0, 0);
							break;
					}

					if (_currentScene.OverrideWeather == (Weather)14)
					{
						Function.Call(Hash.SET_WEATHER_TYPE_NOW, "HALLOWEEN");
					}
					else
					{
						World.Weather = _currentScene.OverrideWeather;
					}

					if (PlayerPed.CurrentVehicle != null)
					{
						PlayerPed.CurrentVehicle.HasGravity = _currentScene.SceneData.UseGravity;
					}
					else
					{
						PlayerPed.HasGravity = _currentScene.SceneData.UseGravity;
					}

					_currentScene.Update();
				}
				else
				{
					float height = PlayerPed.HeightAboveGround;

					if (height > _enterOrbitHeight)
					{
						CustomXmlScene scene =
							MyXmlSerializer.Deserialize<CustomXmlScene>(SpaceModDatabase.PathToScenes + "/" + "EarthOrbit.space");

						SetCurrentScene(scene);
					}
				}
			}
			finally
			{
				Monitor.Exit(_tickLock);
			}
		}

		private void ReadSettings()
		{
			_enterOrbitHeight = Settings.GetValue("mod", "enter_orbit_height", _enterOrbitHeight);
			_optionsMenuKey = Settings.GetValue("mod", "options_menu_key", _optionsMenuKey);

			StaticSettings.ShowCustomUi = Settings.GetValue("settings", "show_custom_ui", StaticSettings.ShowCustomUi);
			StaticSettings.UseScenarios = Settings.GetValue("settings", "use_scenarios", StaticSettings.UseScenarios);

			StaticSettings.MouseControlFlySensitivity = Settings.GetValue("vehicle_settings", "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
			StaticSettings.VehicleSurfaceSpawn = Settings.GetValue("vehicle_settings", "vehicle_surface_spawn", StaticSettings.VehicleSurfaceSpawn);
			StaticSettings.VehicleFlySpeed = Settings.GetValue("vehicle_settings", "vehicle_fly_speed", StaticSettings.VehicleFlySpeed);
		}

		private void SaveSettings()
		{
			Settings.SetValue("mod", "enter_orbit_height", _enterOrbitHeight);
			Settings.SetValue("mod", "options_menu_key", _optionsMenuKey);

			Settings.SetValue("settings", "show_custom_ui", StaticSettings.ShowCustomUi);
			Settings.SetValue("settings", "use_scenarios", StaticSettings.UseScenarios);

			Settings.SetValue("vehicle_settings", "mouse_control_fly_sensitivity", StaticSettings.MouseControlFlySensitivity);
			Settings.SetValue("vehicle_settings", "vehicle_surface_spawn", StaticSettings.VehicleSurfaceSpawn);
			Settings.SetValue("vehicle_settings", "vehicle_fly_speed", StaticSettings.VehicleFlySpeed);
			Settings.Save();
		}

		private void CreateCustomMenu()
		{
			_menuConnector = new MenuConnector();

			_menu = new Menu("Space Mod", Color.FromArgb(125, Color.Black), Color.Black,
				Color.Purple)
			{ MenuItemHeight = 26 };

			#region scenes

			var scenesMenu = _menu.AddParentMenu("Scenes", _menu);
			scenesMenu.Width = _menu.Width;

			var files = Directory.GetFiles(SpaceModDatabase.PathToScenes).Where(file => file.EndsWith(".space")).ToArray();
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];
				var fileName = Path.GetFileName(file);
				var menuItem = new SolomanMenu.MenuItem(fileName);
				menuItem.ItemActivated += (sender, item) =>
				{
					var customXmlScene = MyXmlSerializer.Deserialize<CustomXmlScene>(file);
					if (customXmlScene == default(CustomXmlScene))
					{
						UI.Notify($"Failed to load {file}");
						return;
					}
					SetCurrentScene(customXmlScene, fileName);
				};
				scenesMenu.Add(menuItem);
			}

			#endregion

			#region settings

			var settingsMenu = _menu.AddParentMenu("Settings", _menu);

			#region ui settings

			var userInterfaceMenu = settingsMenu.AddParentMenu("Interface", _menu);

			var showCustomUiCheckbox = new CheckboxMenuItem("Show Custom UI", StaticSettings.ShowCustomUi);
			showCustomUiCheckbox.Checked += (sender, check) =>
			{
				StaticSettings.ShowCustomUi = check;
			};

			userInterfaceMenu.Add(showCustomUiCheckbox);

			#endregion

			#region vehicle settings

			var vehicleSettingsMenu = settingsMenu.AddParentMenu("Vehicles", _menu);
			vehicleSettingsMenu.Width = _menu.Width;

			var vehicleSpeedList = new ListMenuItem("Vehicle Speed",
				Enumerable.Range(1, 10).Select(i => (dynamic)(i * 5)).ToList());
			vehicleSpeedList.IndexChanged += (sender, index, item) =>
			{
				StaticSettings.VehicleFlySpeed = item;
			};

			var flySensitivity = (int)StaticSettings.MouseControlFlySensitivity;
			var vehicleSensitivityList = new ListMenuItem("Mouse Control Sensitivity",
				Enumerable.Range(0, flySensitivity > 15 ? flySensitivity + 5 : 15).Select(i => (dynamic)i).ToList(),
				flySensitivity);
			vehicleSensitivityList.IndexChanged += (sender, index, item) =>
			{
				StaticSettings.MouseControlFlySensitivity = item;
			};

			vehicleSettingsMenu.Add(vehicleSpeedList);
			vehicleSettingsMenu.Add(vehicleSensitivityList);

			#endregion

			#region scene settings

			var sceneSettingsMenu = settingsMenu.AddParentMenu("Scenes", _menu);

			var useScenariosCheckbox = new CheckboxMenuItem("Use Scenarios", StaticSettings.UseScenarios);
			useScenariosCheckbox.Checked += (sender, check) =>
			{
				StaticSettings.UseScenarios = check;
			};

			sceneSettingsMenu.Add(useScenariosCheckbox);

			#endregion

			#region player settings

			var playerSettingsMenu = settingsMenu.AddParentMenu("Player", _menu);

			var useFloatingCheckbox = new CheckboxMenuItem("Use Floating", StaticSettings.UseFloating);
			useFloatingCheckbox.Checked += (sender, check) =>
			{
				StaticSettings.UseFloating = check;
			};

			playerSettingsMenu.Add(useFloatingCheckbox);

			#endregion

			var saveSettingsItem = new SolomanMenu.MenuItem("Save Settings");
			saveSettingsItem.ItemActivated += (sender, item) =>
			{
				SaveSettings();
				UI.Notify("Settings ~b~saved~s~.", true);
			};

			settingsMenu.Add(saveSettingsItem);

			#endregion

			#region debug

			var debugButton = new SolomanMenu.MenuItem("Debug Player");
			debugButton.ItemActivated += (sender, item) =>
			{
				Debug.LogEntityData(PlayerPed);
			};

			_menu.Add(debugButton);

			#endregion

			_menuConnector.Menus.Add(_menu);
			_menuConnector.Menus.Add(settingsMenu);
			_menuConnector.Menus.Add(userInterfaceMenu);
			_menuConnector.Menus.Add(vehicleSettingsMenu);
			_menuConnector.Menus.Add(sceneSettingsMenu);
			_menuConnector.Menus.Add(playerSettingsMenu);
			_menuConnector.Menus.Add(scenesMenu);
		}

		private static void DisableWantedStars()
		{
			SpaceModLib.TerminateScriptByName("re_prison");
			SpaceModLib.TerminateScriptByName("re_prisonlift");
			SpaceModLib.TerminateScriptByName("am_prison");
			SpaceModLib.TerminateScriptByName("re_lossantosintl");
			SpaceModLib.TerminateScriptByName("re_armybase");
			Game.MaxWantedLevel = 0;
		}

		private static void ResetWeather()
		{
			World.Weather = Weather.ExtraSunny;
			World.CurrentDayTime = new TimeSpan(World.CurrentDayTime.Days, 12, 0, 0);
		}

		private void SetCurrentScene(CustomXmlScene customXmlScene, string fileName = default(string))
		{
			Game.FadeScreenOut(1000);
			Wait(1000);
			CreateSceneFromCustomXmlScene(customXmlScene, fileName);
			Wait(1000);
			Game.FadeScreenIn(1000);
		}

		private void CreateSceneFromCustomXmlScene(CustomXmlScene customXmlScene, string fileName = default(string))
		{
			lock (_tickLock)
			{
				EndActiveScenarios();

				if (_currentScene != null && _currentScene != default(CustomScene))
				{
					_currentScene.Delete();
				}

				_currentScene = new CustomScene(customXmlScene) { SceneFile = fileName };
				_currentScene.Start();
				_currentScene.Exited += CurrentSceneOnExited;

				if (PlayerPed.IsInVehicle())
				{
					PlayerPed.CurrentVehicle.Rotation = Vector3.Zero;
				}
				else
				{
					PlayerPed.Rotation = Vector3.Zero;
				}

				if (StaticSettings.UseScenarios)
				{
					Scenarios = customXmlScene.CustomScenarios?.Select(x =>
					{
						var assembly = Assembly.LoadFrom(SpaceModDatabase.PathToScenarios + "/" + x.Name);
						if (assembly == null)
						{
							Debug.Log("Failed to load assembly from: " + x.Name, DebugMessageType.Error);
							return null;
						}
						var name = x.PathToClass;
						var type = assembly.GetType(name);
						var scenario = (CustomScenario)Activator.CreateInstance(type);
						return scenario.IsScenarioComplete() ? null : scenario;

					}).Where(x => x != null).ToList();

					Scenarios?.ForEach(scenario =>
					{
						scenario.Start();
						scenario.Completed += ScenarioOnCompleted;
					});
				}

				SpaceModLib.SetGravityLevel(_currentScene.SceneData.GravityLevel);
			}
		}

		private void ScenarioOnCompleted(CustomScenario scenario, bool success)
		{
			lock (_tickLock)
			{
				Scenarios.Remove(scenario);
			}
		}

		private void CurrentSceneOnExited(CustomScene scene, string newSceneFile, Vector3 exitRotation)
		{
			lock (_tickLock)
			{
				if (!PlayerPed.IsDead)
				{
					Game.FadeScreenOut(1000);
					Wait(1000);
				}

				_currentScene?.Delete();
				_currentScene = null;

				ResetWeather();
				EndActiveScenarios();

				if (newSceneFile != "cmd_earth")
				{
					var newScene =
						MyXmlSerializer.Deserialize<CustomXmlScene>(SpaceModDatabase.PathToScenes + "/" + newSceneFile);

					if (newScene == default(CustomXmlScene))
					{
						UI.Notify(
							"Your custom scene ~r~failed~s~ to load, because the file specified was " +
							"either not written correctly or was invalid.");
						throw new XmlException(newSceneFile);
					}

					newScene.LastSceneFile = scene.SceneFile;

					CreateSceneFromCustomXmlScene(newScene, newSceneFile);

					if (PlayerPed.IsInVehicle())
					{
						PlayerPed.CurrentVehicle.Rotation = exitRotation;
					}
					else
					{
						PlayerPed.Rotation = exitRotation;
					}
				}
				else
				{
					if (!PlayerPed.IsDead)
					{
						if (PlayerPed.IsInVehicle())
						{
							var playerPedCurrentVehicle = PlayerPed.CurrentVehicle;
							playerPedCurrentVehicle.Position = SpaceModDatabase.EarthAtmosphereEnterPosition;
							playerPedCurrentVehicle.Rotation = Vector3.Zero;
							playerPedCurrentVehicle.Heading = 243;
							playerPedCurrentVehicle.HasGravity = true;
						}
						else
						{
							PlayerPosition = SpaceModDatabase.TrevorAirport;
						}
					}

					PlayerPed.HasGravity = true;
					SpaceModLib.SetGravityLevel(0);
				}

				if (!PlayerPed.IsDead)
				{
					Wait(1000);
					Game.FadeScreenIn(1000);
				}
			}
		}

		private void EndActiveScenarios()
		{
			if (Scenarios != null)
			{
				while (Scenarios.Count > 0)
				{
					var scen = Scenarios[0];
					scen.OnEnded(false);
					Scenarios.RemoveAt(0);
				}
			}
		}
	}
}
