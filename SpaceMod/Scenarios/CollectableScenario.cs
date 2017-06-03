using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using DefaultMissions.DefaultMissions.CollectableSerialization;
using GTA;
using GTA.Math;
using GTA.Native;
using SpaceMod;
using SpaceMod.Extensions;
using SpaceMod.Lib;
using SpaceMod.Scenario;

namespace DefaultMissions
{
	public class CollectableScenario : CustomScenario
	{
		private const string Path = SpaceModDatabase.PathToScenarios + "/Default/collectables.xml";

		public CollectableScenario()
		{
			Collectables = new CDataCollectables();
			CurrentPickups = new List<Tuple<CDataCollectablesItem, Prop>>();
		}

		public CDataCollectables Collectables { get; private set; }
		public List<Tuple<CDataCollectablesItem, Prop>> CurrentPickups { get; }

		public override void Start()
		{
			if (!File.Exists(Path))
				return;

			Collectables = MyXmlSerializer.Deserialize<CDataCollectables>(Path);
			if (Collectables == default(CDataCollectables))
				return;

			foreach (var cDataCollectable in Collectables.DataCollectables)
			{
				if (cDataCollectable.Collected) continue;
				if (string.IsNullOrEmpty(cDataCollectable.Scene) || CurrentScene.SceneFile != cDataCollectable.Scene) continue;
				if (string.IsNullOrEmpty(cDataCollectable.ModelName)) continue;

				Model model = new Model(cDataCollectable.ModelName);
				if (!model.Request(5000)) continue;

				Vector3 position = cDataCollectable.Position;
				Prop pickup = new Prop(Function.Call<int>(Hash.CREATE_AMBIENT_PICKUP, (int)PickupType.CustomScript, position.X, position.Y, position.Z, 0, 1, model.Hash, false, false)) {IsPersistent = true};
				if (!Entity.Exists(pickup)) continue;
				pickup.Rotation = cDataCollectable.Rotation;
				pickup.FreezePosition = cDataCollectable.FreezePosition;
				CurrentPickups.Add(new Tuple<CDataCollectablesItem, Prop>(cDataCollectable, pickup));
			}
		}

		public override void OnUpdate()
		{
			CurrentPickups?.ForEach(i =>
			{
				if (!i.Item2.Exists() && !i.Item1.Collected)
				{
					i.Item1.Collected = true;
					ScaleFormMessages.Message.SHOW_SHARD_CENTERED_MP_MESSAGE(Game.GetGXTEntry("BM_LABEL_7"),
						$"{Collectables.DataCollectables.FindAll(x => x.Collected).Count}/{Collectables.DataCollectables.Count} {Game.GetGXTEntry("BM_LABEL_8")}", HudColor.HudColourBlack,
						HudColor.HudColourPickup);
					Effects.Start(ScreenEffect.MenuMgHeistOut);
				}
			});
		}

		public override void OnEnded(bool success)
		{
			CleanUp();

			if (Collectables == default(CDataCollectables))
				return;
			MyXmlSerializer.Serialize(Path, Collectables);
		}

		public override void OnAborted()
		{
			CleanUp();
		}

		private void CleanUp()
		{
			while (CurrentPickups.Count > 0)
			{
				var p = CurrentPickups[CurrentPickups.Count - 1];
				p.Item2.Delete();
				CurrentPickups.RemoveAt(CurrentPickups.Count - 1);
			}
		}
	}

	namespace DefaultMissions.CollectableSerialization
	{
		public class CDataCollectablesItem : ISpatial
		{
			[XmlAttribute("collected")]
			public bool Collected { get; set; }
			[XmlAttribute("frozen")]
			public bool FreezePosition { get; set; }

			public Vector3 Position { get; set; }
			public Vector3 Rotation { get; set; }
			public string ModelName { get; set; }
			public string Scene { get; set; }
		}

		public class CDataCollectables
		{
			public CDataCollectables()
			{
				DataCollectables = new List<CDataCollectablesItem>();
			}

			[XmlArray("dataCollectables")]
			[XmlArrayItem("Item")]
			public List<CDataCollectablesItem> DataCollectables { get; set; }
		}
	}
}
