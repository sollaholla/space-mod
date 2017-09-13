using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTS.Scenes.Interiors;

namespace GTS
{
    public class MapLoader
    {
        private const string Path = Database.PathToScenes + "\\LoadOnStart\\";

        private readonly List<Interior> _ints = new List<Interior>();

        public void LoadMaps()
        {
            try
            {
                if (!File.Exists(Path)) return;
                var files = Directory.GetFiles(Path).Where(x => x.EndsWith(".xml")).ToArray();
                foreach (var file in files)
                {
                    try
                    {
                        var interior = new Interior(file, InteriorType.MapEditor);
                        interior.Request();
                        _ints.Add(interior);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void RemoveMaps()
        {
            foreach (var interior in _ints)
                interior?.Remove();
        }
    }
}
