using GTA;
using GTA.Math;
using GTS.Library;

namespace GTS.OrbitalSystems
{
    public class Surface : Entity
    {
        public Surface(Prop prop, bool tile, float tileSize = 1024) : base(prop.Handle)
        {
            TileSize = tileSize;
            Tile = tile;
        }

        public Prop[,] TerrainGrid { get; private set; } = new Prop[3, 3];

        public float TileSize { get; }

        public bool Tile { get; }

        public void DoInfiniteTile(Vector3 playerPosition, float tileSize)
        {
            if (!Tile) return;

            var xOffset = 0;
            var yOffset = 0;
            Prop playerTerrain = null;

            for (var x = 0; x < 3; x++)
            {
                for (var y = 0; y < 3; y++)
                    if (playerPosition.X >= TerrainGrid[x, y].Position.X &&
                        playerPosition.X <= TerrainGrid[x, y].Position.X + tileSize &&
                        playerPosition.Y >= TerrainGrid[x, y].Position.Y &&
                        playerPosition.Y <= TerrainGrid[x, y].Position.Y + tileSize)
                    {
                        playerTerrain = TerrainGrid[x, y];
                        xOffset = 1 - x;
                        yOffset = 1 - y;
                        break;
                    }

                if (playerTerrain != null)
                    break;
            }

            if (playerTerrain != TerrainGrid[1, 1])
            {
                var newTerrainGrid = new Prop[3, 3];
                for (var x = 0; x < 3; x++)
                for (var y = 0; y < 3; y++)
                {
                    var newX = x + xOffset;
                    if (newX < 0)
                        newX = 2;
                    else if (newX > 2)
                        newX = 0;

                    var newY = y + yOffset;
                    if (newY < 0)
                        newY = 2;
                    else if (newY > 2)
                        newY = 0;

                    newTerrainGrid[newX, newY] = TerrainGrid[x, y];
                }

                TerrainGrid = newTerrainGrid;

                UpdateTilePositions();
            }
        }

        public void GenerateTerrain()
        {
            if (!Tile) return;

            IsVisible = false;

            TerrainGrid[0, 0] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[0, 1] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[0, 2] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 0] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 1] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 2] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 0] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 1] = Utils.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 2] = Utils.CreatePropNoOffset(Model, Position, false);

            UpdateTilePositions();
        }

        public void UpdateTilePositions()
        {
            TerrainGrid[0, 0].Position = new Vector3(
                TerrainGrid[1, 1].Position.X - TileSize,
                TerrainGrid[1, 1].Position.Y + TileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[0, 1].Position = new Vector3(
                TerrainGrid[1, 1].Position.X - TileSize,
                TerrainGrid[1, 1].Position.Y,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[0, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X - TileSize,
                TerrainGrid[1, 1].Position.Y - TileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[1, 0].Position = new Vector3(
                TerrainGrid[1, 1].Position.X,
                TerrainGrid[1, 1].Position.Y + TileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[1, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X,
                TerrainGrid[1, 1].Position.Y - TileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 0].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + TileSize,
                TerrainGrid[1, 1].Position.Y + TileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 1].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + TileSize,
                TerrainGrid[1, 1].Position.Y,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + TileSize,
                TerrainGrid[1, 1].Position.Y - TileSize,
                TerrainGrid[1, 1].Position.Z);
        }

        public new void Delete()
        {
            base.Delete();
            RemoveTiles();
        }

        public void RemoveTiles()
        {
            if (!Tile) return;

            if (TerrainGrid != null)
                for (var i = 0; i < TerrainGrid.GetLength(0); i++)
                for (var j = 0; j < TerrainGrid.GetLength(1); j++)
                    TerrainGrid[i, j]?.Delete();
        }
    }
}