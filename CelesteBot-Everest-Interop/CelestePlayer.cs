using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class CelestePlayer
    {
        float fitness = -1; // Default so that it is known when fitness isn't calculated
        float unadjustedFitness;
        Genome brain;
        ArrayList replayActions = new ArrayList();
        int[,] Vision2D = new int[CelesteBotManager.VISION_2D_X_SIZE, CelesteBotManager.VISION_2D_Y_SIZE];
        float[] vision = new float[CelesteBotManager.INPUTS];
        float[] actions = new float[CelesteBotManager.OUTPUTS];
        int lifespan = 0;
        bool dead = false;
        bool replay = false;
        int gen = 0;
        String name;
        String speciesName = "Not yet defined";
        Player player;
        Vector2 startPos = new Vector2(0,0);

        public float Fitness { get => fitness; set => fitness = value; }
        public float UnadjustedFitness { get => unadjustedFitness; set => unadjustedFitness = value; }
        public Genome Brain { get => brain; set => brain = value; }
        public ArrayList ReplayActions { get => replayActions; set => replayActions = value; }
        public float[] Vision { get => vision; set => vision = value; }
        public float[] Actions { get => actions; set => actions = value; }
        public int Lifespan { get => lifespan; set => lifespan = value; }
        public bool Dead { get => dead; set => dead = value; }
        public bool Replay { get => replay; set => replay = value; }
        public int Gen { get => gen; set => gen = value; }
        public string Name { get => name; set => name = value; }
        public string SpeciesName { get => speciesName; set => speciesName = value; }

        public CelestePlayer()
        {
            Brain = new Genome(CelesteBotManager.INPUTS, CelesteBotManager.OUTPUTS);
            //Name = CelesteBotManager.GetUniqueOrganismName();

        }
        void move()
        {
            // Need to output controller inputs here.
            // The controller then does the actions to 'move' the player.
            Lifespan++;

        }
        public void Update()
        {
            if (player == null)
            {
                try
                {
                    player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
                } catch (NullReferenceException e)
                {
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Player has not been created yet, or is null for some other reason.");
                    return;
                }
                startPos = player.Center;
            }
            UpdateVision();
            
            
            // Also calculate live fitness here. The fitness should weight distance to goal, then time it takes to get there.
            // It might also be easier to apply some sort of fitness based off of velocity of character (that way the faster it goes, the higher the fitness it has)
            // But make sure that making it to the end is still highest priority.

            // Also update the various parameters the player has here.
            // Ex: Player x, Player y, Player vx, Player vy, vision (?)
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
        
        private void UpdateVision()
        {
            int visionX = CelesteBotManager.VISION_2D_X_SIZE;
            int visionY = CelesteBotManager.VISION_2D_Y_SIZE;
            int underYIndex = visionY / 2 + 1;
            int underXIndex = visionX / 2;

            Level level = (Level)Celeste.Celeste.Scene;

            Vector2 tileUnder = TileFinder.GetTileXY(new Vector2(player.X, player.Y+4));
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Tile Under Player: (" + tileUnder.X + ", " + tileUnder.Y + ")");
            Logger.Log(CelesteBotInteropModule.ModLogKey, "(X,Y) Under Player: (" + player.X + ", " + (player.Y + 4) + ")");
            int[,] outInts = new int[visionX, visionY];
            for (int i = 0; i < visionY; i++)
            {
                for (int j = 0; j < visionX; j++)
                {
                    int temp = TileFinder.IsWallAtTile(new Vector2(tileUnder.X - underXIndex + j, tileUnder.Y - underYIndex + i)) ? 1 : 0;
                    outInts[i, j] = temp;
                }
            }
            Vision2D = outInts;
        }
        void look()
        {
            if (player == null)
            {
                dead = true;
                return;
            }
            // Updates vision array with proper values each frame
            // LAST STEP!
            /*
            Inputs: PlayerX, PlayerY, PlayerXSpeed, PlayerYSpeed, <INPUTS FROM VISUALIZATION OF GAME>
            IT IS ALSO POSSIBLE THAT X AND Y ARE UNNEEDED, AS THE VISUALIZATION INPUTS MAY BE ENOUGH
            Outputs: U, D, L, R, Jump, Dash, Climb
            If any of the outputs are above 0.7, apply them when returning controller output
            */
            //Vision[0] = (float)x / ((float)width); // Normalize x and y to the width and height of the scene
            //Vision[1] = (float)y / ((float)height);
            //Vision[2] = Math.Abs(vx); // might not want abs?
            //Vision[3] = vy;
            for (int i = 0; i < CelesteBotManager.VISION_2D_X_SIZE; i++)
            {
                for (int j = 0; j < CelesteBotManager.VISION_2D_Y_SIZE; j++)
                {
                    Vision[i * CelesteBotManager.VISION_2D_Y_SIZE + j] = Vision2D[i, j];
                }
            }
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.BottomCenter.X;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.BottomCenter.Y;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.Speed.X;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.Speed.Y;
            Vision[CelesteBotManager.VISION_2D_Y_SIZE * CelesteBotManager.VISION_2D_X_SIZE] = player.CanDash ? 1 : 0;

        }
        // Updates controller inputs based on neural network output
        void think()
        {
            if (player == null)
            {
                dead = true;
                return;
            }
            //get the output of the neural network
            Actions = Brain.FeedForward(Vision);
            if (Replay)
            {
                //println(vision);
                for (int i = 0; i < Actions.Length; i++)
                {
                    if (Lifespan >= ReplayActions.Count)
                    {
                        Dead = true;
                        return;
                    }
                    float[] tArr = (float[])ReplayActions[Lifespan];
                    Actions[i] = tArr[i];
                }
            }

            CelesteBotInteropModule.inputPlayer.UpdateData(new InputData(Actions)); // Updates inputs to reflect neural network results
            // Need to convert actions float values into controller inputs here.
            // Then needs to return controller inputs so that the player can move
        }
        // Clones CelestePlayer
        CelestePlayer clone()
        {
            CelestePlayer outp = new CelestePlayer();
            outp.Replay = false;
            outp.Fitness = Fitness;
            outp.Gen = Gen;
            outp.Brain = Brain.Clone();
            return outp;
        }
        // Clones for replaying
        CelestePlayer cloneForReplay()
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
        void calculateFitness()
        {
            // The closer it gets to the goal the better.
            // The faster it gets to the goal (or overall faster it travels) the better.
            int dx = 1; // temp

            Fitness = (2 * dx) * (2 * dx) + Lifespan;
            // MODIFY!
        }
        // Getter method for fitness (rarely used)
        float getFitness()
        {
            if (Fitness < 0)
            {
                calculateFitness();
            }
            return Fitness;
        }
        // Crossover function - less fit parent is parent2
        CelestePlayer crossover(CelestePlayer parent2)
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
        public static CelestePlayer playerFromString(String str)
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
