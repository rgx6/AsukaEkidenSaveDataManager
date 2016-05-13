using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;

namespace AsukaEkidenSaveDataManager
{
    [Serializable]
    public class Config
    {
        private static readonly string filePath = Path.Combine(
            Path.GetDirectoryName(Assembly.GetEntryAssembly().Location),
            "config.xml");

        public static Config Current { get; set; }

        public string SaveDataFolder { get; set; }

        public static void Load()
        {
            try
            {
                if (filePath == null || !File.Exists(filePath))
                {
                    throw new FileNotFoundException(filePath);
                }

                var serializer = new XmlSerializer(typeof(Config));

                using (var sr = new StreamReader(filePath, new UTF8Encoding(false)))
                {
                    Current = (Config)serializer.Deserialize(sr);
                }
            }
            catch (Exception e)
            {
                Current = new Config();
                Debug.WriteLine(e);
            }
        }

        public void Save()
        {
            var serializer = new XmlSerializer(typeof(Config));

            using (var sw = new StreamWriter(filePath, false, new UTF8Encoding(false)))
            {
                try
                {
                    serializer.Serialize(sw, this);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                }
            }
        }
    }
}
