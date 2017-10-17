using System.Collections.Generic;
using GTA;
using GTA.Math;
using GTA.Native;
using GTS.Library;
using GTS.Scenes;

namespace GTS.Events
{
    public class HeliTransport
    {
        private readonly List<Vehicle> _helis;
        private readonly List<Pilot> _pilots;

        public HeliTransport()
        {
            _helis = new List<Vehicle>();
            _pilots = new List<Pilot>();
        }

        public void Update()
        {
            //foreach (var pilot in _pilots)
            //{
            //    var dist = pilot.Position.DistanceToSquared(Game.Player.Character.Position);
            //    if (!(dist < 4)) continue;
            //    GtsLibNet.DisplayHelpTextWithGxt(GtsLabels.INPUT_CONTEXT_GENERIC);
            //    if (!Game.IsControlJustPressed(2, Control.Context)) continue;
            //    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, pilot, "GENERIC_HI", "Speech_Params_Force_Shouted_Critical");
            //    Script.Wait(1200);
            //    Game.FadeScreenOut(1000);
            //    Script.Wait(1000);
            //    World.CurrentDayTime += new TimeSpan(0, 0, 20);
            //    Game.Player.Character.Position = pilot.Destination - Vector3.WorldUp;
            //    Game.Player.Character.Heading = pilot.DestHeading;
            //    Game.FadeScreenIn(2000);
            //    Script.Wait(2000);
            //    Function.Call(Hash._PLAY_AMBIENT_SPEECH1, Game.Player.Character, "GENERIC_THANKS", "Speech_Params_Force_Shouted_Critical");
            //    Script.Wait(1000);
            //    Game.Player.Money -= 15;
            //}
        }

        public void Load()
        {
            //var positions = new[]
            //{
            //    new Vector3(-6546.155f, -1332.292f, 31.23943f),
            //    new Vector3(-6573.748f,-1342.227f,31.23943f),
            //    new Vector3(-1168.388f, -1719.005f, 5.231533f)
            //};

            //var model = new Model("buzzard");
            //model.Request();
            //while (!model.IsLoaded)
            //    Script.Yield();

            //foreach (var position in positions)
            //{
            //    var veh = World.CreateVehicle(model, position);
            //    _helis.Add(veh);
            //}

            //CreatePeds();
        }

        public void ShowHelp()
        {
            //if (!_pilots.Any())
            //    return;

            //Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            //var cam = World.CreateCamera(_pilots[0].Position + _pilots[0].ForwardVector * 2, Vector3.Zero, 60);
            //cam.PointAt(_pilots[0]);
            //cam.Shake(CameraShake.Hand, 0.5f);
            //World.RenderingCamera = cam;
            //Effects.Start(ScreenEffect.CamPushInNeutral);
            //GtsLibNet.DisplayHelpTextWithGxt("HELI_INFO1");
            //var timout = DateTime.Now + new TimeSpan(0, 0, 0, 10);
            //while (DateTime.Now < timout)
            //{
            //    Function.Call(Hash.HIDE_HUD_AND_RADAR_THIS_FRAME);
            //    Script.Yield();
            //}
            //World.RenderingCamera = null;
            //Effects.Start(ScreenEffect.CamPushInNeutral);
        }

        private void CreatePeds()
        {
            Script.Yield();
            var model = new Model(PedHash.Pilot02SMM);
            model.Request();
            while (!model.IsLoaded)
                Script.Yield();
            var ped = World.CreatePed(model, new Vector3(-6543.132f, -1331.071f, 29.23944f), 283.5533f);
            ped.TaskStartScenarioInPlace("world_human_smoking");
            ped.SetDefaultClothes();
            var b = ped.AddBlip();
            b.Sprite = BlipSprite.Helicopter;
            b.Name = "NASA Heli Transport";
            b.IsShortRange = true;
            b.Color = Scene.MarkerBlipColor;
            _pilots.Add(new Pilot(ped.Handle, new Vector3(-1163.048f, -1713.792f, 4.236674f), 137.986f));

            var ped2 = World.CreatePed(model, new Vector3(-1165.701f, -1715.857f, 3.237385f), 306.5582f);
            ped2.SetDefaultClothes();
            ped2.TaskStartScenarioInPlace("world_human_guard_stand");
            var b2 = ped2.AddBlip();
            b2.Sprite = BlipSprite.Helicopter;
            b2.Name = "NASA Heli Transport";
            b2.IsShortRange = true;
            b2.Color = Scene.MarkerBlipColor;
            _pilots.Add(new Pilot(ped2.Handle, new Vector3(-6540.545f, -1329.75f, 30.23945f), 128.9673f));
        }

        public void Delete()
        {
            //foreach (var vehicle in _helis)
            //    vehicle.Delete();

            //foreach (var ped in _pilots)
            //    ped.Delete();
        }

        private class Pilot : Entity
        {
            public Pilot(int handle, Vector3 destination, float destHeading) : base(handle)
            {
                Destination = destination;
                DestHeading = destHeading;
            }

            public Vector3 Destination { get; }
            public float DestHeading { get; }

            public static explicit operator Ped(Pilot v)
            {
                return new Ped(v.Handle);
            }
        }
    }
}