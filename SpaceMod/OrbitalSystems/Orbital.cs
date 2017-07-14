using GTA;
using GTA.Math;
using SpaceMod.Lib;
using System.Collections.Generic;

namespace SpaceMod.OrbitalSystems
{
    public class Orbital : AttachedOrbital
    {
        public Orbital(Prop prop, string name, float rotationSpeed) : base(prop, Vector3.Zero)
        {
            Name = name;

            RotationSpeed = rotationSpeed;
        }

        public string Name { get; set; }

        public bool WormHole { get; set; }

        public float RotationSpeed { get; set; }

        public Prop[,] TerrainGrid { get; private set; } = new Prop[3, 3];

        private float tileSize;
        
        public void Rotate()
        {
            Vector3 rotation = Rotation;
            rotation.Z += Game.LastFrameTime * RotationSpeed;
            Rotation = rotation;
        }

        public void DoInfiniteTile(Vector3 playerPosition, float tileSize)
        {
            if (this.tileSize == 0)
            {
                GenerateTerrain(tileSize);
            }

            int xOffset = 0;
            int yOffset = 0;
            Prop playerTerrain = null;

            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if ((playerPosition.X >= TerrainGrid[x, y].Position.X) &&
                        (playerPosition.X <= (TerrainGrid[x, y].Position.X + tileSize)) &&
                        (playerPosition.Y >= TerrainGrid[x, y].Position.Y) &&
                        (playerPosition.Y <= (TerrainGrid[x, y].Position.Y + tileSize)))
                    {
                        playerTerrain = TerrainGrid[x, y];
                        xOffset = 1 - x;
                        yOffset = 1 - y;
                        break;
                    }
                }

                if (playerTerrain != null)
                {
                    break;
                }
            }

            if (playerTerrain != TerrainGrid[1, 1])
            {
                Prop[,] newTerrainGrid = new Prop[3, 3];

                for (int x = 0; x < 3; x++)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        int newX = x + xOffset;

                        if (newX < 0)
                        {
                            newX = 2;
                        }
                        else if (newX > 2)
                        {
                            newX = 0;
                        }

                        int newY = y + yOffset;

                        if (newY < 0)
                        {
                            newY = 2;
                        }
                        else if (newY > 2)
                        {
                            newY = 0;
                        }

                        newTerrainGrid[newX, newY] = TerrainGrid[x, y];
                    }
                }

                TerrainGrid = newTerrainGrid;

                UpdateTilePositions();
            }
        }

        private void GenerateTerrain(float terrainSize = 1024)
        {
            this.tileSize = terrainSize;

            IsVisible = false;

            TerrainGrid[0, 0] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[0, 1] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[0, 2] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 0] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 1] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[1, 2] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 0] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 1] = SpaceModLib.CreatePropNoOffset(Model, Position, false);
            TerrainGrid[2, 2] = SpaceModLib.CreatePropNoOffset(Model, Position, false);

            UpdateTilePositions();
        }

        public void UpdateTilePositions()
        {
            TerrainGrid[0, 0].Position = new Vector3(
            TerrainGrid[1, 1].Position.X - tileSize,
            TerrainGrid[1, 1].Position.Y + tileSize,
            TerrainGrid[1, 1].Position.Z);

            TerrainGrid[0, 1].Position = new Vector3(
                TerrainGrid[1, 1].Position.X - tileSize,
                TerrainGrid[1, 1].Position.Y,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[0, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X - tileSize,
                TerrainGrid[1, 1].Position.Y - tileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[1, 0].Position = new Vector3(
                TerrainGrid[1, 1].Position.X,
                TerrainGrid[1, 1].Position.Y + tileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[1, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X,
                TerrainGrid[1, 1].Position.Y - tileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 0].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + tileSize,
                TerrainGrid[1, 1].Position.Y + tileSize,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 1].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + tileSize,
                TerrainGrid[1, 1].Position.Y,
                TerrainGrid[1, 1].Position.Z);

            TerrainGrid[2, 2].Position = new Vector3(
                TerrainGrid[1, 1].Position.X + tileSize,
                TerrainGrid[1, 1].Position.Y - tileSize,
                TerrainGrid[1, 1].Position.Z);
        }

        public new void Delete()
        {
            base.Delete();
            
            RemoveTiles();
        }

        public void RemoveTiles()
        {
            tileSize = 0;

            if (TerrainGrid != null)
            {
                for (int i = 0; i < TerrainGrid.GetLength(0); i++)
                {
                    for (int j = 0; j < TerrainGrid.GetLength(1); j++)
                    {
                        TerrainGrid[i, j]?.Delete();
                    }
                }
            }
        }
    }
}
