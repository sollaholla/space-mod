using System;
using System.IO;

namespace GTS.Extensions
{
    public static class XmlSerializer
    {
        public static T Deserialize<T>(string path) where T : class
        {
            T obj = null;
            Debug.Log("Attempting to deserialize: " + Path.GetFileName(path));

            if (!File.Exists(path))
            {
                Debug.Log($"Deserialize - File {Path.GetFileName(path)} does not exist!");
                return null;
            }
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var reader = new StreamReader(path);
                obj = (T) serializer.Deserialize(reader);
                reader.Close();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
            }
            return obj;
        }

        public static void Serialize<T>(string path, T obj) where T : class
        {
            try
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var writer = new StreamWriter(path);
                serializer.Serialize(writer, obj);
                writer.Close();
            }
            catch (Exception ex)
            {
                Debug.Log(ex.Message + Environment.NewLine + ex.StackTrace, DebugMessageType.Error);
            }
        }
    }
}