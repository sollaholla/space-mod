using System.Collections.Generic;
using System.Drawing;
using GTA;
using GTA.Math;
using NativeUI;

namespace SpaceMod.DataClasses.SceneTypes
{
    public class EarthOrbitScene : Scene
    {
        public static Vector3[] Positions => new[]
        {
            new Vector3(-6870.744f, -12107.31f, 8620.764f), /*Earth*/
            new Vector3(-15370.74f, -12107.31f, 8620.764f) /*Moon*/
        };

        private PlanetSystem _planetSystem;
        private Prop _earth;
        private Prop _moon;
        private Prop _issl;
        private readonly UIMenu _selectionMenu = new UIMenu(string.Empty, "SELECT A DESTINATION", new Point(0, -105));

        public EarthOrbitScene()
        {
            _selectionMenu.SetBannerType(new UIResRectangle());
            _selectionMenu.OnMenuClose += sender =>
            {
                End(null);
            };
            var earthItem = new UIMenuItem("Earth", "Travel to earth.");
            _selectionMenu.AddItem(earthItem);
            earthItem.Activated += (sender, item) =>
            {
                End(null);
            };
            var isslItem = new UIMenuItem("ISSL", "Travel to the space station.");
            _selectionMenu.AddItem(isslItem);
            isslItem.Activated += (sender, item) =>
            {
                End(new IsslScene());
            };
            var back = new UIMenuItem("Back", "Leave orbit again.");
            _selectionMenu.AddItem(back);
            back.Activated += (sender, item) =>
            {
                End(new EarthOrbitScene(), SceneStartDirection.FromTarget);
            };
        }

        public override void Init()
        {
            _earth = World.CreateProp(Constants.EarthLargeModel, Vector3.Zero, false, false);
            _moon = World.CreateProp(Constants.MoonMedModel, Vector3.Zero, false, false);
            _issl = World.CreateProp(Constants.IsslModel, Vector3.Zero, false, false);
            var sun = World.CreateProp(Constants.SunSmallModel, Vector3.Zero, false, false);
            var galaxy = World.CreateProp(Constants.SpaceDomeModel, Vector3.Zero, false, false);

            ResetPlayerOrigin();

            var planets = new List<Planet>
            {
                new Planet(_earth.Handle, PlayerPed, Vector3.Zero, -3.5f) /*Earth*/,
                new Planet(_moon.Handle, galaxy, Vector3.Zero, 3.0f) /*Moon*/
            };
            var stars = new List<Star>
            {
                new Star(sun.Handle, Constants.SunOffsetNearEarth) /*Sun*/
            };

            TeleportPlayerToGalaxy();

            _earth.Position = Positions[0];
            _moon.Position = Positions[1];

            // Move and rotate the issl.
            // TODO: Get constant values for this.
            _issl.Position = _earth.Position - _earth.RightVector * 1200 + _earth.UpVector * 150;
            var rotation = _issl.Rotation;
            rotation.Y = -30;
            _issl.Rotation = rotation;

            sun.Position = Constants.GalaxyCenter;
            _planetSystem = new PlanetSystem(galaxy.Handle, planets, stars, -1.5f);

            // Since this is the "earth" orbit scene the target is the earth,
            // and if we have a start direction of "ToTarget" then we're going to face the earth
            // and visa versa. (i.e. if we where 'coming' from the moon, and 'going' to earth)
            SetStartDirection(_earth.Position, PlayerPed.IsInVehicle() ? PlayerPed.CurrentVehicle as Entity : PlayerPed,
                StartDirection);
        }

        public override void Update()
        {
            _planetSystem.Process(Constants.GetValidGalaxyDomePosition(PlayerPed));
            GoToMoon();
            GoToEarth();

            if (!_selectionMenu.Visible) return;
            _selectionMenu.ProcessControl();
            _selectionMenu.ProcessMouse();
            _selectionMenu.Draw();
        }

        private void GoToMoon()
        {
            var dist = PlayerPosition.DistanceTo(_moon.Position);
            if (dist > 2500) return;

            // Check the moon orbit scene for reasons why i'm rotating towards the earth here.
            End(new MoonOrbitScene(), SceneStartDirection.ToTarget);
        }

        private void GoToEarth()
        {
            var dist = PlayerPosition.DistanceTo(_earth.Position);
            if (dist > 1500) return;
            if (_selectionMenu.Visible) return;
            _selectionMenu.Visible = !_selectionMenu.Visible;
            Game.TimeScale = 0;
        }

        public override void Abort()
        {
            Done();
        }

        public override void CleanUp()
        {
            Done();
        }

        private void Done()
        {
            _planetSystem.Abort();
            _issl?.Delete();
            Game.TimeScale = 1;
        }
    }
}
