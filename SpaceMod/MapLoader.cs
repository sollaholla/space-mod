using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GTS.Scenes.Interiors;
using GTSCommon;

namespace GTS
{
    public class MapLoader
    {
        private const string Path = Database.PathToInteriors + "\\LoadOnStart\\";

        private readonly List<Interior> _ints = new List<Interior>();

        public void LoadMaps()
        {
            try
            {
                if (!Directory.Exists(Path)) return;
                var files = Directory.GetFiles(Path).Where(x => x.EndsWith(".xml")).ToArray();
                foreach (var file in files)
                    try
                    {
                        var interior = new Interior("LoadOnStart\\" + System.IO.Path.GetFileNameWithoutExtension(file),
                            InteriorType.MapEditor);
                        interior.Request();
                        _ints.Add(interior);
                    }
                    catch (Exception)
                    {
                        // ignored
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