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
using System.Runtime.Serialization;
/*
 * The CelestePlayer class represents the player in the game and is located in the same file as the CelesteBotInteropModule. 
 * It contains a Brain property which represents its neural network, a Fitness property to keep track of its performance in
 * the game, InputData and Vision properties which are used to provide input to the neural network, and various other properties
 * and methods related to gameplay.
The Brain property is used to decide which actions to take based on the inputs given to it. The Population of players contains 
multiple instances of CelestePlayer. During gameplay, the current player is retrieved from the Population and manipulated using
the CurrentPlayer property.
 */
namespace CelesteBot_Everest_Interop
{
    [KnownType(typeof(CelestePlayer))]
    [DataContract]
    public class CelestePlayer : IDisposable
    {
        [DataMember]
        int[][] Vision2D = new int[CelesteBotManager.VISION_2D_X_SIZE][];
        
        public Player player;
        [DataMember]
        Vector2 startPos = new Vector2(0,0);

        [DataMember]
        public float Fitness = -1;
        [DataMember]
        public double LastPlayerPosition = 0;
        [DataMember]
        private float AverageSpeed = 0;
        [DataMember]
        private float AverageStamina = 110;
        [DataMember]
        public float UnadjustedFitness;
        [DataMember]
        public Genome Brain;
        [DataMember]
        public ArrayList ReplayActions = new ArrayList();
        [DataMember]
        public float[] Vision = new float[CelesteBotManager.INPUTS];
        [DataMember]
        public float[] Actions = new float[CelesteBotManager.OUTPUTS];
        [DataMember]
        public int Lifespan = 0;
        [DataMember]
        public bool Dead = false;
        [DataMember]
        public bool Replay = false;
        [DataMember]
        public int Gen = 0;
        [DataMember]
        private Stopwatch timer;
        [DataMember]
        private Stopwatch deathTimer;
        [DataMember]
        public string Name;
        [DataMember]
        public string SpeciesName = "Not yet defined";

        // Target Fitness and stuff
        [DataMember]
        public string FitnessPath = @"fitnesses.fit";
        [DataMember]
        public Dictionary<string, List<Vector2>> positionFitnesses;
        [DataMember]
        public Dictionary<string, List<Vector2>> velocityFitnesses;

        [DataMember]
        public Vector2 Target = Vector2.Zero;
        [DataMember]
        public int TargetsPassed = 0;

        [DataMember]
        private List<Vector2>.Enumerator enumForFitness;
        [DataMember]
        private List<string>.Enumerator enumForLevels;

        [DataMember]
        private Vector2 MaxPlayerPos = new Vector2(-10000, -10000);

        [DataMember]
        public bool VisionSetup = false;
        [DataMember]
        public List<double> Rewards;

        public CelestePlayer()
        {
            for (int i = 0; i < CelesteBotManager.VISION_2D_X_SIZE; i++)
            {
                Vision2D[i] = new int[CelesteBotManager.VISION_2D_Y_SIZE];
            }

            Brain = new Genome(CelesteBotManager.INPUTS, CelesteBotManager.OUTPUTS);
            Name = CelesteBotManager.GetUniqueOrganismName();
            timer = new Stopwatch();
            deathTimer = new Stopwatch();
            positionFitnesses = Util.GetPositionFitnesses(FitnessPath);
            velocityFitnesses = Util.GetVelocityFitnesses(FitnessPath);
            Rewards = new List<double>();
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
                if (deathTimer.ElapsedMilliseconds * CelesteBotInteropModule.FrameLoops > CelesteBotManager.PLAYER_DEATH_TIME_BEFORE_RESET * 1000)
                {
                    Dead = true;
                }
                //return;
            }
            UpdateVision();
            Look();
            Think();
            CalculateFitness();
            /*need to incorporate y here, maybe dist to goal here as well*/
            // Compare to distance to fitness target
            if (player.Speed.Length() == 0 || (player.BottomCenter - Target).Length() >= (MaxPlayerPos - Target).Length() && !player.JustRespawned)
            {
                if (!timer.IsRunning)
                {
                    timer.Start();
                }
            } else
            {
                timer.Reset(); // Resets TimeWhileStuck if it starts moving again!
            }
            if (timer.ElapsedMilliseconds * CelesteBotInteropModule.FrameLoops > CelesteBotInteropModule.Settings.TimeStuckThreshold * 1000 && !player.Dead && !deathTimer.IsRunning)
            {
                // Kill the player because it hasn't moved for awhile
                Dead = true;
                // Actual reset happens in CelesteBotInteropModule
            }

            Lifespan++;
            AverageSpeed += player.Speed.LengthSquared() / (float)Lifespan;
            AverageStamina += player.Stamina / (float)Lifespan;
            // Needs to be replaced with minimum distance position, instead
            if ((player.BottomCenter - Target).Length() < (MaxPlayerPos - Target).Length())
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
            VisionSetup = true;
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
            int[][] outInts = new int[visionX][];
            for (int i = 0; i < visionX; i++)
            {
                outInts[i] = new int[visionY];
            }
            //MTexture[,] tiles = TileFinder.GetSplicedTileArray(visionX, visionY);
            TileFinder.UpdateGrid();
            TileFinder.CacheEntities();
            for (int i = 0; i < visionY; i++)
            {
                for (int j = 0; j < visionX; j++)
                {
                    if (TileFinder.tileArray != null)
                    {
                        //if (TileFinder.tileArray[(int)(tileUnder.X - underXIndex + j), (int)(tileUnder.Y - underYIndex + i)] != null)
                        //{
                        //    Logger.Log(CelesteBotInteropModule.ModLogKey, TileFinder.tileArray[(int)(tileUnder.X - underXIndex + j), (int)(tileUnder.Y - underYIndex + i)].ToString());
                        //}
                    }
                    /*int temp = TileFinder.IsSpikeAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 8 : 1;
                    if (temp == 1)
                    {
                        temp = TileFinder.IsWallAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 2 : 1;
                    }
                    if (temp == 1)
                    {
                        temp = TileFinder.IsEntityAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 4 : 1;
                    }*/
                    outInts[j][i] = (int)TileFinder.GetEntityAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i));
                }
            }
            Vision2D = outInts;
        }
        // Returns the convolution of the given kernel over the vision2d array with stride stride
        //int[,] Convolve(int[,] vision2d, int[] kernel, int[] stride)
        //{

        //}
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
                    Vision[i * CelesteBotManager.VISION_2D_Y_SIZE + j] = Vision2D[j][i];
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
            if (CelesteBotInteropModule.LearningStyle == LearningStyle.NEAT)
            {
                if(deathTimer.IsRunning)
                {
                    Actions = new float[] { 0, 0, 0, 0, 0, 0};
                }
                else
                {
                    Actions = Brain.FeedForward(Vision);
                }
                InputData inp = new InputData(Actions);
                CelesteBotInteropModule.inputPlayer.UpdateData(inp); // Updates inputs to reflect neural network results
            } else if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                CalculateQAction();
            }
            //Logger.Log(CelesteBotInteropModule.ModLogKey, test);
            //Logger.Log(CelesteBotInteropModule.ModLogKey, "Attempted Input: " + new InputData(Actions));
            // Need to convert actions float values into controller inputs here.
            // Then needs to return controller inputs so that the player can move
        }
        // Calculates and Pushes an action according to QLearning
        public void CalculateQAction()
        {
            // Should we be exploring or exploiting?
            if (CelesteBotManager.QEpsilon <= new Random(Guid.NewGuid().GetHashCode()).NextDouble())
            {
                // Exploitation
                CelesteBotInteropModule.inputPlayer.UpdateData(QTable.GetAction(CelesteBotManager.qTable.GetMaxActionIndex(new QState(this))));
            } else
            {
                // Exploration
                CelesteBotInteropModule.inputPlayer.UpdateData(QTable.GetRandomAction());
            }
        }
        // Calculates a reward for QLearning
        public double CalculateReward()
        {
            if (player == null)
            {
                return 0;
            }
            //CalculateFitness();
            // DONT USE FITNESS, USE PLAYER POSITION INSTEAD OF MAX POSITION
            double quasiFitness = (player.BottomCenter - Target).Length();
            //double reward = quasiFitness * 10;
            if (LastPlayerPosition == 0)
            {
                LastPlayerPosition = quasiFitness;
            }
            double reward = -(quasiFitness - LastPlayerPosition) / (1.0 / 60.0) * 10;
            LastPlayerPosition = quasiFitness;
            
            // Maybe change reward to be dFitness/dt?
            return reward;
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
        private void UpdateTarget()
        {
            if (Target == Vector2.Zero)
            {
                // Enum does not exist yet, lets make it.
                Level level = TileFinder.GetCelesteLevel();
                try
                {
                    enumForFitness = positionFitnesses[level.Session.MapData.Filename + "_" + level.Session.Level + "_" + "0"].GetEnumerator();
                    enumForLevels = Util.GetRawLevelsInOrder(FitnessPath).GetEnumerator();
                    enumForLevels.MoveNext(); // Should always be one ahead of the current level/fitness
                    enumForFitness.MoveNext();
                    Target = enumForFitness.Current;
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Key: " + enumForLevels.Current + "==" + level.Session.MapData.Filename + "_" + level.Session.Level + "_" + "0" + " out: " + Target.ToString());
                } catch (KeyNotFoundException e)
                {
                    // In a level that doesn't have a valid fitness enumerator
                    Target = new Vector2(10000, 10000);
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Unknown Fitness Enumerator for: " + level.Session.MapData.Filename + "_" + level.Session.Level + "_" + "0");
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "With FitnessPath: " + enumForLevels);
                }
            }
            // Updates the target based off of the current position
            if ((player.BottomCenter - Target).Length() < CelesteBotManager.UPDATE_TARGET_THRESHOLD)
            {
                enumForFitness.MoveNext();
                //enumForLevels.MoveNext();
                if (enumForFitness.Current == null || enumForFitness.Current == Vector2.Zero)
                {
                    // We are at the end of the enumerator. Now is the tricky part: We need to move to the next fitness.
                    // We need to create an enumerator that we would use for the next level, but... how do we know the next level?
                    enumForLevels.MoveNext();
                    enumForFitness = positionFitnesses[enumForLevels.Current].GetEnumerator();
                    enumForFitness.MoveNext();
                }
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Updating Target Location: " + enumForFitness.Current);
                Target = enumForFitness.Current;
                // Use the enumerator to attempt to enumerate to next possible option. If it doesn't exist in this level (as in the enumerator is done) then use the next level's fitness
                TargetsPassed++; // Increase the targets we have passed, which should give us a large boost in fitness
            }
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
                // This is incredibly important to resolve!
                //Fitness = (((MaxPlayerPos - startPos).Length())/* + AverageSpeed / 10000 + 110 / AverageStamina*/);
                // Need to keep track of the times that I have entered a certain screen. Can really take care of this at a later time tho.
                UpdateTarget();
                //Fitness = 1000.0f/(player.BottomCenter - Target).LengthSquared() + TargetsPassed * CelesteBotInteropModule.Settings.TargetReachedRewardFitness;
                Fitness = 1000.0f / ((MaxPlayerPos - Target).LengthSquared() + 1) + TargetsPassed * CelesteBotInteropModule.Settings.TargetReachedRewardFitness;
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

        public void Dispose()
        {
            Vision2D = null;
            Vision = null;
            ReplayActions = null;
            Brain = null;
            Actions = null;
        }
    }


}
