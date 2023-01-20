using Chromatics.Layers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chromatics.Helpers
{
    public static class FileOperationsHelper
    {
        public static void SaveLayerMappings(ConcurrentDictionary<int, Layer> mappings)
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";
            
            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    var serializer = new JsonSerializer();
                    serializer.Converters.Add(new DictionaryConverter());
                    serializer.NullValueHandling = NullValueHandling.Ignore;

                    serializer.Serialize(sw, mappings);
                    sw.WriteLine();
                    sw.Close();
                }


            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static ConcurrentDictionary<int, Layer> LoadLayerMappings()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";
            var result = new ConcurrentDictionary<int, Layer>();

            try
            {
                using (var sr = new StreamReader(path))
                {
                    result = JsonConvert.DeserializeObject<ConcurrentDictionary<int, Layer>>(sr.ReadToEnd(), new DictionaryConverter());
                    sr.Close();
                }

                if (result != null)
                    return result;

                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public static bool CheckLayerMappingsExist()
        {
            var enviroment = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            var path = $"{enviroment}/layers.chromatics3";

            if (File.Exists(path))
                return true;

            return false;
        }
    }
}
