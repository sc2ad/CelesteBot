using Celeste.Mod;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    // Basically, QStates are all of the states of the player (possibly just the vision?)
    // And Actions are an array of doubles that corresponds to each action's reward.
    // However, each "action" is not just a button press, it is instead any legal combination of buttons.
    // Examples:
    // 1. Right, Jump, and Dash
    // 2. Left, Dash, LongJump, and Grab
    // NonExample: Left, Right, ...
    // Thus, we have quite the table.
    [Serializable]
    public class QTable
    {
        private static int actions;
        //private static Dictionary<InputData, int> actionIndexDict;
        private static List<InputData> actionIndexList;

        private Dictionary<QState, double[]> dynamicTable;
        private QState CurrentState;
        private QState CurrentStartState;

        public QTable()
        {
            // Dynamic lists only (because we don't know how many states we have)
            dynamicTable = new Dictionary<QState, double[]>();
            if (actionIndexList == null)
            {
                CreateActionDictionary();
            }
            actions = actionIndexList.Count;
        }
        public double GetValue(QState state, int actionIndex)
        {
            if (dynamicTable != null)
            {
                if (!ContainsState(state))
                {
                    dynamicTable.Add(state, new double[actions]);
                }
                return dynamicTable[GetStateInTable(state)][actionIndex];
            }
            return 0;
        }
        public double GetValue(QState state, InputData action)
        {
            return GetValue(state, GetActionIndex(action));
        }
        public void Add(QState state, int actionIndexTaken, double value)
        {
            if (dynamicTable != null)
            {
                double[] temp = new double[actions];
                temp[actionIndexTaken] = value;
                dynamicTable.Add(state, temp);
            } else
            {
                throw new Exception("Need to create the QTable before adding states to it!");
            }
        }
        public void Add(QState state, InputData action, double value)
        {
            Add(state, GetActionIndex(action), value);
        }
        public void Update(QState state, int actionIndexTaken, double value)
        {
            if (dynamicTable != null)
            {
                //double[] temp = new double[actions];
                //Logger.Log(CelesteBotInteropModule.ModLogKey, "Updating State: " + state);
                QState s = GetStateInTable(state);
                if (s != null)
                {
                    double[] temp = dynamicTable[s];
                    temp[actionIndexTaken] = value;
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "State already exists!");
                    dynamicTable[s] = temp;
                } else
                {
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "New State Added!");
                    Add(state, actionIndexTaken, value);
                }
            }
            else
            {
                throw new Exception("Need to create the QTable before updating states in it!");
            }
        }
        public void Update(QState state, InputData action, double value)
        {
            Update(state, GetActionIndex(action), value);
        }
        public bool ContainsState(QState state)
        {
            return GetStateInTable(state) != null;
        }
        public QState GetStateInTable(QState state)
        {
            if (dynamicTable != null)
            {
                if (CurrentStartState == state)
                {
                    return CurrentState;
                }
                foreach (QState s in dynamicTable.Keys)
                {
                    if (s.EqualsState(state))
                    {
                        //Logger.Log(CelesteBotInteropModule.ModLogKey, "s: " + s + " = " + state);
                        CurrentState = s;
                        CurrentStartState = state;
                        return s;
                    }
                }
                return null;
            }
            else
            {
                throw new Exception("Need to create the QTable before testing if it contains a state within it!");
            }
        }
        public int GetStateCount()
        {
            if (dynamicTable != null)
            {
                return dynamicTable.Keys.Count;
            }
            else
            {
                throw new Exception("Need to create the QTable before testing if it contains a state within it!");
            }
        }
        public double GetMax(QState state)
        {
            if (dynamicTable != null)
            {
                QState s = GetStateInTable(state);
                if (s == null)
                {
                    // The state doesn't exist in the dynamicTable
                    return 0;
                }
                return dynamicTable[s].Max();
            } else
            {
                throw new Exception("Need to create the QTable before getting the max of states within it!");
            }
        }
        public int GetMaxActionIndex(QState state)
        {
            if (dynamicTable != null)
            {
                int maxIndex = 0;
                if (!ContainsState(state))
                {
                    return GetActionIndex(GetRandomAction());
                }
                for (int i = 1; i < dynamicTable[GetStateInTable(state)].Length; i++)
                {
                    if (dynamicTable[GetStateInTable(state)][i] > dynamicTable[GetStateInTable(state)][maxIndex])
                    {
                        maxIndex = i;
                    }
                }
                return maxIndex;
            }
            else
            {
                throw new Exception("Need to create the QTable before getting the max of states within it!");
            }
        }
        /// <summary>
        /// Converts input data to an action index for QLearning
        /// </summary>
        /// <param name="data">The data that represents the input</param>
        /// <returns>The actionIndex for the QTable</returns>
        public static int GetActionIndex(InputData data)
        {
            if (actionIndexList == null || !actionIndexList.Contains(data))
            {
                CreateActionDictionary();
            }
            return actionIndexList.FindIndex((InputData d) => { return d == data; });
        }
        public static InputData GetAction(int index)
        {
            if (actionIndexList == null || index > actionIndexList.Count)
            {
                CreateActionDictionary();
            }
            return actionIndexList[index];
        }
        public static int GetActionCount()
        {
            if (actionIndexList == null)
            {
                CreateActionDictionary();
            }
            return actionIndexList.Count;
        }
        public static void CreateActionDictionary()
        {
            actionIndexList = new List<InputData>();
            float[] tempActionArr = new float[CelesteBotManager.OUTPUTS];

            CalculatePossibleActions(tempActionArr);
        }
        public static InputData GetRandomAction()
        {
            if (actionIndexList == null)
            {
                CreateActionDictionary();
            }
            int index = new Random(Guid.NewGuid().GetHashCode()).Next(actionIndexList.Count);
            return actionIndexList[index];
        }
        private static void CalculatePossibleActions(float[] arr, int index = 0)
        {
            if (index == arr.Length - 1)
            {
                // This is the last element of the list.
                // Iterate each option, and add each to the Dictionary.
                int start = -1;
                if (index > 1)
                {
                    start = 0;
                }
                for (int option = start; option <= 1; option++)
                {
                    arr[index] = option;
                    actionIndexList.Add(new InputData(arr));
                }
            }
            else
            {
                int start = -1;
                if (index > 1)
                {
                    start = 0;
                }
                for (int option = start; option <= 1; option++)
                {
                    arr[index] = option;
                    CalculatePossibleActions(arr, index + 1);
                }
            }
        }
        public static void SerializeTable(QTable table, string fileName)
        {
            if (table == null) { return; }

            try
            {
                using (Stream stream = File.Create(fileName))
                {
                    IFormatter formatter = new BinaryFormatter();
                    formatter.Serialize(stream, table);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to save the QTable!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }
        }
        public static QTable SerializeLoad(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(QTable); }

            QTable objectOut = new QTable();

            try
            {
                using (Stream stream = File.OpenRead(fileName))
                {
                    IFormatter formatter = new BinaryFormatter();
                    stream.Position = 0;
                    objectOut = (QTable)formatter.Deserialize(stream);
                    stream.Close();
                }
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to load the QTable!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }

            return objectOut;
        }
        public static void SaveTable(QTable table, string fileName)
        {
            if (table == null) { return; }

            try
            {
                List<string> s = new List<string>();
                foreach (QState q in table.dynamicTable.Keys)
                {
                    string z = q.ToString() + ":[";
                    foreach (double d in table.dynamicTable[q])
                    {
                        z += d + ",";
                    }
                    z += "]";
                    s.Add(z);
                }
                File.WriteAllLines(fileName, s.ToArray());
            } catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to save the QTable!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }
        }
        public static QTable LoadTable(string fileName)
        {
            if (string.IsNullOrEmpty(fileName)) { return default(QTable); }

            try
            {
                string[] s = File.ReadAllLines(fileName);
                QTable table = new QTable();

                foreach (string line in s)
                {
                    if (!line.Contains(":"))
                    {
                        // MALFORMATTED FILE
                        Logger.Log(CelesteBotInteropModule.ModLogKey, "Malformatted file line: " + line);
                        return default(QTable);
                    }
                    string[] spl = line.Split(':');
                    QState state = QState.FromString(spl[0]);

                    string[] doubles = spl[1].Split('[')[1].Split(']')[0].Split(',');
                    double[] d = new double[doubles.Length];

                    for (int i = 0; i < d.Length; i++)
                    {
                        d[i] = Convert.ToDouble(doubles[i]);
                    }
                    table.dynamicTable.Add(state, d);
                }
                return table;
            }
            catch (Exception ex)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "An exception happened when attempting to save the QTable!");
                Logger.Log(CelesteBotInteropModule.ModLogKey, ex.Message);
            }
            return default(QTable);
        }
    }
}
 