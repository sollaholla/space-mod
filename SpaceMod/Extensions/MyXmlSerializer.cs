using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace SpaceMod.Extensions
{
    public static class MyXmlSerializer
    {
        public static T Deserialize<T>(string path)
        {
            var obj = default(T);
	        if (!File.Exists(path))
	        {
				Debug.Log($"XmlSerializer::Deserialize - File {path} does not exist!");
		        return obj;
	        }
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var reader = new StreamReader(path);
                obj = (T)serializer.Deserialize(reader);
                reader.Close();
            }
            catch (Exception ex)
            {
                File.WriteAllText(@"./scripts/SpaceModSerializer.log",
                    $"[{DateTime.UtcNow.ToShortDateString()}] {ex.Message}\n{ex.StackTrace}");
            }
            return obj;
        }

        public static void Serialize<T>(string path, T obj)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(T));
                var writer = new StreamWriter(path);
                serializer.Serialize(writer, obj);
                writer.Close();
            }
            catch (Exception ex)
            {
                File.WriteAllText(@"./scripts/SpaceModSerializer.log",
                    $"[{DateTime.UtcNow.ToShortDateString()}] {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}
