using Celeste.Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace CelesteBot_Everest_Interop
{
    public class Util
    {
        public static void SerializeObject(Population pop, string fileName)
        {
            if (pop == null) { return; }

            try
            {
                using (Stream stream = File.Create(fileName))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, pop);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to save a checkpoint!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }
        }
        public static Population DeSerializeObject(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(Population); }

            Population objectOut = default(Population);

            try
            {
                using (Stream stream = File.OpenRead(fileName))
                {
                    IFormatter formatter = new BinaryFormatter();
                    stream.Position = 0;
                    objectOut = (Population)formatter.Deserialize(stream);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to load a checkpoint!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }

            return objectOut;
        }
        public static IEnumerable<T> SliceRow<T>(T[,] array, int row)
        {
            for (var i = 0; i < array.GetLength(0); i++)
            {
                yield return array[i, row];
            }
        }
    }
}
