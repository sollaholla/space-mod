using System;
using GTA;
using GTA.Math;
using GTS.Library;

namespace GTS.OrbitalSystems
{
    public class Surface : Entity
    {
        private readonly int _dimensions;
        private readonly float _tileSize;

        private Entity[,] _tiles = new Entity[0, 0];
        private Entity _lastTile;

        private Entity LastTile {
            set {
                if (_lastTile != value)
                    RePositionTerrainTiles(value.Position);
                _lastTile = value;
            }
        }

        public bool CanUpdate { get; set; }

        public Surface(IHandleable prop, float tileSize, int dimensions) : base(prop.Handle)
        {
            _tileSize = tileSize;
            _dimensions = dimensions;
        }

        public void Update()
        {
            if (!CanUpdate) return;

            var player = Game.Player.Character;
            if (player == null) return;
            var playerPos = player.Position;

            Entity newTile = null;
            var isInBounds = false;
            var div = _tileSize / 2;

            for (var i = -_dimensions; i <= _dimensions; i++)
            {
                for (var j = -_dimensions; j <= _dimensions; j++)
                {
                    var tile = _tiles[i + _dimensions, j + _dimensions];
                    var tilePos = tile.Position;
                    if (!(playerPos.X < tilePos.X + div) || !(playerPos.X > tilePos.X - div)) continue;
                    if (!(playerPos.Y < tilePos.Y + div) || !(playerPos.Y > tilePos.Y - div)) continue;
                    newTile = tile;
                    isInBounds = true;
                }
            }

            if (!isInBounds)
            {
                var nearestX = (float)Math.Round(playerPos.X / _tileSize, MidpointRounding.AwayFromZero) * _tileSize;
                var nearestY = (float)Math.Round(playerPos.Y / _tileSize, MidpointRounding.AwayFromZero) * _tileSize;
                RePositionTerrainTiles(new Vector3(nearestX, nearestY, 0));
                return;
            }

            LastTile = newTile;
        }

        public void GenerateNeighbors()
        {
            if (!CanUpdate) return;

            _tiles = new Entity[_dimensions * 2 + 1, _dimensions * 2 + 1];
            for (var i = -_dimensions; i <= _dimensions; i++)
            {
                for (var j = -_dimensions; j <= _dimensions; j++)
                {
                    var obj = GtsLibNet.CreatePropNoOffset(Model.Hash, Position + new Vector3(i, j, 0) * _tileSize, true);
                    obj.FreezePosition = true;
                    obj.Quaternion = Quaternion;
                    _tiles[i + _dimensions, j + _dimensions] = obj;
                }
            }
            LastTile = this;
        }

        private void RePositionTerrainTiles(Vector3 rootTilePosition)
        {
            if (!CanUpdate) return;

            for (var i = -_dimensions; i <= _dimensions; i++)
            for (var j = -_dimensions; j <= _dimensions; j++)
                _tiles[i + _dimensions, j + _dimensions].Position = rootTilePosition + new Vector3(i, j, 0) * _tileSize;
        }

        public new void Delete()
        {
            base.Delete();

            for (var i = 0; i < _tiles.GetUpperBound(0); i++)
            {
                for (var j = 0; j < _tiles.GetUpperBound(1); j++)
                {
                    _tiles[i, j].Delete();
                }
            }
        }
    }
}