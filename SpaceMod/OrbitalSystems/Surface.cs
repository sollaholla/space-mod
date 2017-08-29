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
        private Entity _lastTile;

        private Entity[,] _tiles = new Entity[0, 0];

        public Surface(IHandleable prop, float tileSize, int dimensions) : base(prop.Handle)
        {
            _tileSize = tileSize;
            _dimensions = dimensions;
        }

        private Entity LastTile
        {
            set
            {
                if (_lastTile != value)
                    RePositionTerrainTiles(value.Position);
                _lastTile = value;
            }
        }

        public bool CanUpdate { get; set; }

        public Vector3 Offset { get; set; }

        public void Update(Vector3 playerPos)
        {
            if (!CanUpdate) return;

            Entity newTile = null;
            var isInBounds = false;
            var div = _tileSize / 2;

            for (var i = -_dimensions; i <= _dimensions; i++)
            for (var j = -_dimensions; j <= _dimensions; j++)
            {
                var tile = _tiles[i + _dimensions, j + _dimensions];
                var tilePos = tile.Position;
                if (!(playerPos.X < (tilePos.X - Offset.X) + div) || !(playerPos.X > (tilePos.X - Offset.X) - div)) continue;
                if (!(playerPos.Y < (tilePos.Y - Offset.Y) + div) || !(playerPos.Y > (tilePos.Y - Offset.Y) - div)) continue;
                newTile = tile;
                isInBounds = true;
            }

            if (!isInBounds)
            {
                var nearestX = Convert.ToSingle(Math.Round(Convert.ToDouble(playerPos.X / Position.X)) * Position.X);
                var nearestY = Convert.ToSingle(Math.Round(Convert.ToDouble(playerPos.Y / Position.Y)) * Position.Y);
                RePositionTerrainTiles(new Vector3(nearestX, nearestY, Position.Z));
                return;
            }

            LastTile = newTile;
        }

        public void GenerateNeighbors()
        {
            if (!CanUpdate) return;

            _tiles = new Entity[_dimensions * 2 + 1, _dimensions * 2 + 1];
            for (var i = -_dimensions; i <= _dimensions; i++)
            for (var j = -_dimensions; j <= _dimensions; j++)
            {
                var pos = Position + new Vector3(i, j, 0) * _tileSize;
                var obj = World.CreateProp(Model.Hash, pos, Vector3.Zero, false, false) ?? new Prop(0);
                obj.FreezePosition = true;
                obj.Quaternion = Quaternion;
                obj.PositionNoOffset = pos;
                _tiles[i + _dimensions, j + _dimensions] = obj;
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
            if (_tiles != null)
            {
                for (var i = 0; i < _tiles.GetLength(0); i++)
                for (var j = 0; j < _tiles.GetLength(1); j++)
                    _tiles[i, j].Delete();
            }

            base.Delete();
        }
    }
}