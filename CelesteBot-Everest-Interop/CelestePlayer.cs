using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class CelestePlayer
    {
        int[,] Vision2D = new int[CelesteBotManager.VISION_2D_X_SIZE, CelesteBotManager.VISION_2D_Y_SIZE];
        public Player player;
        Vector2 startPos = new Vector2(0,0);

        public float Fitness = -1;
        private float AverageSpeed = 0;
        public float UnadjustedFitness;
        public Genome Brain;
        public ArrayList ReplayActions = new ArrayList();
        public float[] Vision = new float[CelesteBotManager.INPUTS];
        public float[] Actions = new float[CelesteBotManager.OUTPUTS];
        public int Lifespan = 0;
        public bool Dead = false;
        public bool Replay = false;
        public int Gen = 0;
        private Stopwatch timer;
        private Stopwatch deathTimer;
        public string Name;
        public string SpeciesName = "Not yet defined";

        private Vector2 MaxPlayerPos = new Vector2(-10000, -10000);

        public CelestePlayer()
        {
            Brain = new Genome(CelesteBotManager.INPUTS, CelesteBotManager.OUTPUTS);
            Name = CelesteBotManager.GetUniqueOrganismName();
            timer = new Stopwatch();
            deathTimer = new Stopwatch();
        }
        public void Update()
        {
            if (Dead)
            {
                return;
            }
            if (player == null)
            {
                try
                {
                    player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
                    if (!player.Dead && Fitness == -1)
                    {
                        startPos = player.BottomCenter;
                    }
                } catch (NullReferenceException e)
                {
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Player has not been created yet, or is null for some other reason.");
                    return;
                }
            }
            // This is to make sure that we don't try to reset while we are respawning
            if (player.Dead)
            {
                if (!deathTimer.IsRunning)
                {
                    deathTimer.Start();
                }
                if (deathTimer.ElapsedMilliseconds > CelesteBotManager.PLAYER_DEATH_TIME_BEFORE_RESET * 1000)
                {
                    Dead = true;
                }
                return;
            }
            UpdateVision();
            Look();
            Think();
            /*need to incorporate y here, maybe dist to goal here as well*/
            if ((player.Speed.X == 0 || player.BottomCenter.X <= MaxPlayerPos.X) && !player.JustRespawned)
            {
                if (!timer.IsRunning)
                {
                    timer.Start();
                }
            } else
            {
                timer.Reset(); // Resets TimeWhileStuck if it starts moving again!
            }
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Time: " + timer.ElapsedMilliseconds + " Thresh: " + (timer.ElapsedMilliseconds / 1000.0) + " ? " + CelesteBotInteropModule.Settings.TimeStuckThreshold);
            if (timer.ElapsedMilliseconds / 1000.0 > CelesteBotInteropModule.Settings.TimeStuckThreshold)
            {
                // Kill the player because it hasn't moved for awhile
                Dead = true;
                // Actual reset happens in CelesteBotInteropModule
            }

            Lifespan++;
            AverageSpeed += player.Speed.LengthSquared() / (float)Lifespan;
            if (player.BottomCenter.X > MaxPlayerPos.X || (player.BottomCenter.Y < MaxPlayerPos.Y && player.BottomCenter.X >= MaxPlayerPos.X))
            {
                MaxPlayerPos = player.BottomCenter;
            }
            
            //LastPlayerPos = player.BottomCenter;
        }
        public void SetupVision()
        {
            try
            {
                //TileFinder.TilesOffset = Celeste.Celeste.Scene.Entities.FindFirst<SolidTiles>().Center; // Thanks KDT#7539!
                TileFinder.SetupOffset();
            } catch (NullReferenceException e)
            {
                // The Scene hasn't been created yet.
            }
        }
        // 1 for tile (walls), -1 for entities (moving platforms, etc.)
        // Might add more ex: -2 = dashblox, ... or new Nodes indicating type of entity/tile along with input box
        private void UpdateVision()
        {
            int visionX = CelesteBotManager.VISION_2D_X_SIZE;
            int visionY = CelesteBotManager.VISION_2D_Y_SIZE;
            int underYIndex = visionY / 2 + 1;
            int underXIndex = visionX / 2;
            try
            {
                Level level = (Level)Celeste.Celeste.Scene;
            } catch (InvalidCastException e)
            {
                // This means we tried to cast a LevelExit to a Level. It basically means we are dead.
                //Dead = true;
                // Wait for the timer to expire before actually resetting
                return;
            }

            Vector2 tileUnder = TileFinder.GetTileXY(new Vector2(player.X, player.Y+4));
            //Logger.Log(CelesteBotInteropModule.ModLogKey, "Tile Under Player: (" + tileUnder.X + ", " + tileUnder.Y + ")");
            //Logger.Log(CelesteBotInteropModule.ModLogKey, "(X,Y) Under Player: (" + player.X + ", " + (player.Y + 4) + ")");
            // 1 = Air, 2 = Wall, 4 = Entity
            int[,] outInts = new int[visionY, visionX];
            for (int i = 0; i < visionY; i++)
            {
                for (int j = 0; j < visionX; j++)
                {
                    int temp = TileFinder.IsWallAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 2 : 1;
                    if (temp == 1)
                    {
                        temp = TileFinder.IsEntityAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 4 : 1;
                    }
                    outInts[i, j] = temp;
                }
            }
            Vision2D = outInts;
        }
        void Look()
        {
            if (player == null)
            {
                // We are waiting for the death timer to expire
                return;
            }
            // Updates vision array with proper values each frame
            /*
            Inputs: PlayerX, PlayerY, PlayerXSpeed, PlayerYSpeed, <INPUTS FROM VISUALIZATION OF GAME>
            IT IS ALSO POSSIBLE THAT X AND Y ARE UNNEEDED, AS THE VISUALIZATION INPUTS MAY BE ENOUGH
            Outputs: U, D, L, R, Jump, Dash, Climb
            If any of the outputs are above 0.7, apply them when returning controller output
            */

            for (int i = 0; i < CelesteBotManager.VISION_2D_X_SIZE; i++)
            {
                for (int j = 0; j < CelesteBotManager.VISION_2D_Y_SIZE; j++)
                {
                    Vision[i * CelesteBotManager.VISION_2D_Y_SIZE + j] = Vision2D[j, i];
                }
            }
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.BottomCenter.X;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE + 1] = player.BottomCenter.Y;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE + 2] = player.Speed.X;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE + 3] = player.Speed.Y;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE + 4] = player.CanDash ? 1 : 0;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE + 5] = CelesteBotManager.Normalize(player.Stamina, -1, 120);
        }
        // Updates controller inputs based on neural network output
        void Think()
        {
            if (player == null)
            {
                // We are waiting for the death timer to expire
                return;
            }
            //get the output of the neural network
            Actions = Brain.FeedForward(Vision);
            CelesteBotInteropModule.inputPlayer.UpdateData(new InputData(Actions)); // Updates inputs to reflect neural network results
            string test = "Attempted Actions: [";
            for (int i = 0; i < Actions.Length; i++)
            {
                test += Actions[i] + ", ";
            }
            test += "]";
            Logger.Log(CelesteBotInteropModule.ModLogKey, test);
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Attempted Input: " + new InputData(Actions));
            // Need to convert actions float values into controller inputs here.
            // Then needs to return controller inputs so that the player can move
        }
        // Clones CelestePlayer
        public CelestePlayer Clone()
        {
            CelestePlayer outp = new CelestePlayer();
            outp.Replay = false;
            //outp.Fitness = Fitness;
            outp.Gen = Gen;
            outp.Brain = Brain.Clone();
            return outp;
        }
        // Clones for replaying
        public CelestePlayer CloneForReplay()
        {
            CelestePlayer outp = new CelestePlayer();
            outp.ReplayActions = (ArrayList)ReplayActions.Clone();
            outp.Replay = true;
            outp.Fitness = Fitness;
            outp.Gen = Gen;
            outp.Brain = Brain.Clone();
            outp.Name = Name;
            outp.SpeciesName = SpeciesName;
            return outp;
        }
        // Calculates fitness
        public void CalculateFitness()
        {
            // The closer it gets to the goal the better.
            // The faster it gets to the goal (or overall faster it travels) the better.
            if (player == null)
            {
                return;
            }
            // The further it gets to the goal the better, the lifespan decreases.
            if (!Replay)
            {
                Fitness = (((MaxPlayerPos - startPos).Length()) + AverageSpeed / 1000);
                // Could also create a fitness hash, using Levels as keys, and create Vector2's representing goal fitness locations
            }
            // MODIFY!
        }
        // Getter method for fitness (rarely used)
        public float GetFitness()
        {
            if (!Replay) {
                CalculateFitness();
            }
            return Fitness;
        }
        // Crossover function - less fit parent is parent2
        public CelestePlayer Crossover(CelestePlayer parent2)
        {
            CelestePlayer child = new CelestePlayer();

            child.Brain = Brain.Crossover(parent2.Brain);
            child.Brain.GenerateNetwork();

            return child;
        }
        public override string ToString()
        {
            string outp = "P<Name:" + Name;
            outp += ", speciesName:" + SpeciesName;
            outp += ", gen:" + Gen;
            outp += ", fitness:" + Fitness;
            outp += ", replay:" + Replay;
            outp += ", ACTIONS:`";
            foreach (float[] f in ReplayActions)
            {
                outp += "<";
                for (int i = 0; i < f.Length; i++)
                {
                    outp += f[i] + ", ";
                }
                outp = outp.Substring(0, outp.Length - 2); // Removes ", " at end
                outp += ">, ";
            }
            outp = outp.Substring(0, outp.Length - 2); // Removes ", " at end
            outp += ", " + Brain + ">";
            return outp;
        }
        public static CelestePlayer PlayerFromString(String str)
        {
            try
            {
                str = str.Split(new string[] { "P<" }, StringSplitOptions.None)[1];
                String name = str.Split(new string[] { "Name:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0];
                String speciesName = str.Split(new string[] { "speciesName:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0];
                int gen = Convert.ToInt32(str.Split(new string[] { "gen:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                float fitness = (float)Convert.ToDouble(str.Split(new string[] { "fitness:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                int bestScore = Convert.ToInt32(str.Split(new string[] { "bestScore:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                bool replay = Convert.ToBoolean(str.Split(new string[] { "replay:" }, StringSplitOptions.None)[1].Split(new string[] { ", " }, StringSplitOptions.None)[0]);
                String forActions = str.Split(new string[] { ", ACTIONS:`" }, StringSplitOptions.None)[1].Split(new string[] { ", GENOME" }, StringSplitOptions.None)[0];
                ArrayList replayActions = new ArrayList();
                while (forActions.Contains(">"))
                {
                    String singleton = forActions.Split(new string[] { "<" }, StringSplitOptions.None)[1].Split(new string[] { ">" }, StringSplitOptions.None)[0];
                    String[] arr = singleton.Split(new string[] { ", " }, StringSplitOptions.None); // Gets all of the floats individually
                    if (arr.Length == 0)
                    {
                        arr = new String[] { singleton };
                    }
                    int len = arr.Length;
                    float[] floats = new float[len];
                    for (int i = 0; i < len; i++)
                    {
                        floats[i] = (float)Convert.ToDouble(arr[i]);
                    }
                    replayActions.Add(floats);
                    forActions = forActions.Substring(forActions.IndexOf(">") + 1, forActions.Length);
                }
                Genome brain = Genome.GenomeFromString(str);
                CelestePlayer outp = new CelestePlayer();
                outp.ReplayActions = (ArrayList)replayActions.Clone();
                outp.Replay = false;
                outp.Fitness = fitness;
                outp.Gen = gen;
                outp.Brain = brain.Clone();
                outp.Name = name;
                outp.SpeciesName = speciesName;
                return outp;
            }
            catch (Exception e)
            {
                return null;
            }
        }
    }


}
