using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;

namespace DefaultMissions.Scenes.SceneTypes
{
	public class MarsMissionSatelliteScene : IMiniScene
	{
		private readonly Vector3 _spawn = new Vector3(10000, 10000, 10000);
		private readonly List<Entity> _entities;
		private Camera _cam;

		public MarsMissionSatelliteScene()
		{
			_entities = new List<Entity>();
		}

		public bool Failed { get; private set; }

		public void Spawn()
		{
			Model model = new Model("fibufo");

			if (model.Request(5000))
			{
				Prop p = new Prop(World.CreateProp(model, _spawn + new Vector3(0, 0, 5), false, false).Handle) {FreezePosition = true};
				_entities.Add(p);
			}

			// TODO: Add europa surface here when its finished.
			Model surfaceModel = new Model("europa_surface");

			if (surfaceModel.Request(5000))
			{
				Prop p = new Prop(World.CreateProp(surfaceModel, _spawn, false, false).Handle) {FreezePosition = true};
				_entities.Add(p);
			}

			//Model domeModel = new Model("spacedome");

			//if (domeModel.Request(5000))
			//{
			//	Prop p = new Prop(World.CreateProp(domeModel, _spawn, false, false).Handle) { FreezePosition = true };
			//	_entities.Add(p);
			//}

			if (_entities.Count < 2)
				Failed = true;

			if (!Failed)
			{
				_cam = World.CreateCamera(_spawn + new Vector3(15, 15, 45), Vector3.Zero, 80);
				_cam.PointAt(_spawn);
				_cam.Shake(CameraShake.Vibrate, 0.5f);

				World.RenderingCamera = _cam;
			}
		}

		public void Update()
		{
			_entities[0].Rotation += new Vector3(0, 0, 15 * Game.LastFrameTime);
			Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
			Game.DisableAllControlsThisFrame(2);
		}

		public void Remove()
		{
			while (_entities.Count > 0)
			{
				var entity = _entities[_entities.Count - 1];
				entity.Delete();
				_entities.RemoveAt(_entities.Count - 1);
			}

			_cam?.Destroy();

			World.RenderingCamera = null;
		}
	}
}
