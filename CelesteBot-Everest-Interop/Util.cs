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
                Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Write, FileShare.Write);
                IFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, pop);
                stream.Close();
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
                Stream stream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read);
                IFormatter formatter = new BinaryFormatter();
                objectOut = (Population)formatter.Deserialize(stream);
                stream.Close();
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to load a checkpoint!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }

            return objectOut;
        }
    }
}
