using Celeste.Mod;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    [Serializable]
    /// <summary>
    /// This class is used to represent various states of the player/game.
    /// These QStates can be added/updated in the QTable in order to create a functioning Q-Learning AI.
    /// </summary>
    public class QState
    {
        public float[] Vision;
        /// <summary>
        /// Constructs a state using the player.
        /// </summary>
        /// <param name="player">The player that the state should be created from</param>
        public QState(CelestePlayer player)
        {
            Vision = new float[player.Vision.Length];
            player.Vision.CopyTo(Vision, 0);
            Vision = Vision.Take(CelesteBotManager.VISION_2D_X_SIZE * CelesteBotManager.VISION_2D_Y_SIZE).ToArray();
            //Vision = player.Vision;
        }
        private QState(float[] Vision)
        {
            this.Vision = Vision;
        }
        // Compares their visions
        public bool EqualsState(QState st)
        {
            try
            {
                for (int i = 0; i < Vision.Length; i++)
                {
                    //Logger.Log(CelesteBotInteropModule.ModLogKey, Vision[i] + " = " + st.Vision[i]);
                    if (Vision[i] != st.Vision[i])
                    {
                        //Logger.Log(CelesteBotInteropModule.ModLogKey, Vision[i] + " != " + st.Vision[i]);
                        return false;
                    }
                }
                return true;
            } catch (IndexOutOfRangeException e)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "THIS SHOULD NEVER HAPPEN!");
                // Oh well...
            }
            return false;
        }
        // ToString
        public override string ToString()
        {
            string s = "";
            //s += player;
            s += "[";
            foreach (float f in Vision)
            {
                s += f + ",";
            }
            s += "]";
            return s;
        }
        public static QState FromString(string str)
        {
            string[] floats = str.Split('[')[1].Split(']')[0].Split(',');
            float[] f = new float[floats.Length];
            for (int i = 0; i < floats.Length; i++)
            {
                f[i] = (float)Convert.ToDouble(floats[i]);
            }
            return new QState(f);
        }
    }
}
