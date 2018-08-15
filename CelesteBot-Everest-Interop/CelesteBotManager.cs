using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CelesteBot_Everest_Interop
{
    public class CelesteBotManager
    {
        public static float ACTION_THRESHOLD = 0.55f;
        public static float RE_RANDOMIZE_WEIGHT_CHANCE = 0.2f;
        public static double WEIGHT_MUTATION_CHANCE = 0.65;
        public static double ADD_CONNECTION_CHANCE = 0.55;
        public static double ADD_NODE_CHANCE = 0.15;

        public static double WEIGHT_MAXIMUM = CelesteBotInteropModule.Settings.WeightMaximum; // Max magnitude a weight can be (+- this number)
        
        public static int VISION_2D_X_SIZE = 5;
        public static int VISION_2D_Y_SIZE = 5;
        public static int INPUTS = VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 6;
        public static int OUTPUTS = 6;

        public static Color GENE_POSITIVE_COLOR = Color.DarkGreen;
        public static Color GENE_NEGATIVE_COLOR = Color.Red;
        public static double THICKNESS_SCALE = 5; // How much the thickness increases per increase of 1 in the weight when drawing genes
        public static int NODE_RADIUS = 10;
        public static Vector2 NODE_LABEL_SCALE = new Vector2(0.2f, 0.2f);
        public static Vector2 TEXT_OFFSET = new Vector2(7, 7);
        // Graphing Parameters
        public static ArrayList SavedBestFitnesses = new ArrayList();

        // POPULATION PARAMETERS
        public static int EXTINCTION_SAVE_TOP = 5; // How many species to save when a mass extinction occurs
        //public static int POPULATION_SIZE = 50;

        public static int PLAYER_GRACE_BUFFER = 160; // How long between restarts should the next player be created, some arbitrary number of frames
        public static double PLAYER_DEATH_TIME_BEFORE_RESET = 2.5; // How many seconds after a player dies should the next player be created and the last one deleted

        // Paths/Prefixes
        public static string ORGANISM_PATH = @"organismNames.txt";
        public static string SPECIES_PATH = @"speciesNames.txt";
        public static string CHECKPOINT_FILE_PATH = @"Checkpoints";
        public static string CHECKPOINT_FILE_PREFIX = @"Checkpoints\checkpoint";

        public static bool Cutscene = false;

        public static void Draw()
        {
            //Monocle.Engine.Draw(gameTime);
            int viewWidth = Engine.ViewWidth;
            int viewHeight = Engine.ViewHeight;

            Monocle.Draw.SpriteBatch.Begin();
            try
            {
                DrawStandard(CelesteBotInteropModule.CurrentPlayer);
                if (CelesteBotInteropModule.DrawPlayer)
                {
                    DrawPlayer(CelesteBotInteropModule.CurrentPlayer);
                }
                if (CelesteBotInteropModule.DrawFitness)
                {
                    DrawFitness(CelesteBotInteropModule.CurrentPlayer);
                }
                if (CelesteBotInteropModule.DrawDetails)
                {
                    DrawDetails(CelesteBotInteropModule.CurrentPlayer);
                }
                if (CelesteBotInteropModule.DrawBestFitness)
                {
                    DrawBestFitness();
                }
                if (CelesteBotInteropModule.DrawGraph)
                {
                    DrawGraph();
                }
            }
            catch (NullReferenceException e)
            {
                // The game has yet to finish loading, just don't draw text for now.
            }
            Monocle.Draw.SpriteBatch.End();
        }
        public static bool CompleteCutsceneSkip(InputPlayer inputPlayer)
        {
            InputData thisFrame = new InputData();
            // If the last frame contains an escape, make this frame contain a menu down
            if (inputPlayer.Data.ESC)
            {
                thisFrame.MenuDown = true;
            }
            // If the last frame contains a menu down, make this frame contain a menu confirm
            else if (inputPlayer.Data.MenuDown)
            {
                thisFrame.MenuConfirm = true;
            }
            else
            {
                // This means we are done with handling a cutscene.
                // Just make sure we are playing again!
                return false;
            }
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Completing Cutscene Skip with inputs: " + thisFrame + " and Cutscene: "+Cutscene);
            inputPlayer.UpdateData(thisFrame);
            return true;
        }
        public static bool CheckForCutsceneSkip(InputPlayer inputPlayer)
        {
            // three inputs need to be done to successfully skip a cutscene.
            // esc --> menu down --> menu confirm
            try
            {
                if (Cutscene)
                {
                    Cutscene = CompleteCutsceneSkip(inputPlayer);
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "After Cutscene skip: " + Cutscene);
                    return true; // even if it returned false last time, still skip
                }
                try
                {
                    Level level = (Level)Celeste.Celeste.Scene;
                    if (level.InCutscene)
                    {
                        Logger.Log(CelesteBotInteropModule.ModLogKey, "Entered Cutscene! With Cutscene: "+Cutscene);
                        Cutscene = true;
                        InputData newFrame = new InputData();
                        newFrame.ESC = true;
                        inputPlayer.UpdateData(newFrame);
                        return true;
                    }
                } catch (InvalidCastException e)
                {
                    // Game still hasn't finished loading...
                }
            } catch (NullReferenceException e)
            {
                // Level or Player hasn't been setup yet. Just continue on for now.
            }
            return false;
        }
        public static bool CompleteRestart(InputPlayer inputPlayer)
        {
            if (inputPlayer.LastData.QuickRestart)
            {
                InputData temp = new InputData();
                temp.MenuConfirm = true;
                inputPlayer.UpdateData(temp);
                return true;
            }
            return false;
        }

        public static float Normalize(float value, float min, float max)
        {
            return (value - (max - min)/2) / ((max - min) / 2);
        }
        public static void DrawPlayer(CelestePlayer p)
        {
            int x = 800;
            int y = 100;
            int w = 1200;
            int h = 800;

            Monocle.Draw.Rect(x, y, w, h, Color.Black * 0.8f); // Draws background

            ArrayList nodes2d = new ArrayList();
            for (int i = 0; i < p.Brain.Layers; i++)
            {
                ArrayList nodesInLayer = new ArrayList();
                foreach (Node n in p.Brain.Nodes)
                {
                    if (n.Layer == i)
                    {
                        nodesInLayer.Add(n);
                    }
                }
                nodes2d.Add(nodesInLayer);
            }

            double dx = (double)w / (double)(p.Brain.Layers+1);
            x += (int)dx;
            // Sets drawing positions for all Nodes
            for (int i = 0; i < nodes2d.Count; i++)
            {
                int drawX = x + (int)(dx * i);
                ArrayList a = (ArrayList)nodes2d[i];
                double dy = (double)h / (double)(a.Count+1);
                int drawY = y;
                for (int j = 0; j < a.Count; j++)
                {
                    drawY += (int)dy;
                    Node temp = (Node)a[j];
                    temp.DrawPos = new Vector2(drawX, drawY);
                    a[j] = temp;
                }
            }

            // Draws all genes between Node positions
            for (int i = 0; i < p.Brain.Genes.Count; i++)
            {
                GeneConnection temp = (GeneConnection)p.Brain.Genes[i];
                if (!temp.Enabled)
                {
                    continue;
                }
                Color color = temp.Weight > 0 ? GENE_POSITIVE_COLOR : GENE_NEGATIVE_COLOR; // Sets color of gene. 
                Vector2 fromLoc = new Vector2(10000, 10000);
                Vector2 toLoc = new Vector2(10000, 10000);
                int thickness = (int)(Math.Abs(temp.Weight) * THICKNESS_SCALE) + 1;
                // Finds Node positions that match Ids of the GeneConnection
                foreach (ArrayList a in nodes2d)
                {
                    foreach (Node n in a)
                    {
                        if (n.Id == temp.FromNode.Id)
                        {
                            fromLoc = n.DrawPos;
                        }
                        else if (n.Id == temp.ToNode.Id)
                        {
                            toLoc = n.DrawPos;
                        }
                    }
                }
                if (fromLoc.X == 10000 && fromLoc.Y == 10000)
                {
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Could not find Node: " + temp.FromNode.ToString() + " in Nodes array!");
                    //continue;
                }
                if (toLoc.X == 10000 && fromLoc.Y == 10000)
                {
                    Logger.Log(CelesteBotInteropModule.ModLogKey, "Could not find Node: " + temp.ToNode.ToString() + " in Nodes array!");
                    //continue;
                }
                Monocle.Draw.Line(fromLoc, toLoc, color, thickness);
            }
            ArrayList outputNodes = (ArrayList)nodes2d[nodes2d.Count - 1];
            Dictionary<int, string> OutputLabels = new Dictionary<int, string>();
            for (int i = 0; i < outputNodes.Count; i++)
            {
                Node n = (Node)outputNodes[i];
                string outputLabel = "";
                switch (i)
                {
                    case 0:
                        outputLabel = "Left/Right";
                        break;
                    case 1:
                        outputLabel = "Up/Down";
                        break;
                    case 2:
                        outputLabel = "Jump";
                        break;
                    case 3:
                        outputLabel = "Dash";
                        break;
                    case 4:
                        outputLabel = "Grab";
                        break;
                    case 5:
                        outputLabel = "Talk";
                        break;
                }
                OutputLabels.Add(n.Id, outputLabel);
            }

            // Draws all of the Nodes in order
            foreach (ArrayList a in nodes2d)
            {
                foreach (Node n in a)
                {
                    float thickness = 1;
                    Color color = Color.White;
                    float highVal = 0;
                    float lowVal = 0;
                    if (n.Layer == p.Brain.Layers-1)
                    {
                        // This is an output Node. Need to check the threshold instead.
                        highVal = ACTION_THRESHOLD;
                        lowVal = -ACTION_THRESHOLD; // Never red when outputting (except for x, and y)
                    }
                    if (n.OutputValue > highVal)
                    {
                        thickness = 3;
                        color = Color.DarkGreen;
                    } else if (n.OutputValue < lowVal)
                    {
                        thickness = 3;
                        color = Color.DarkRed;
                    }
                    if (n.Id < VISION_2D_X_SIZE * VISION_2D_Y_SIZE)
                    {
                        // This is a vision input node.
                        switch (n.OutputValue)
                        {
                            default:
                            case 1:
                                // Air
                                color = Color.White;
                                thickness = 1;
                                break;
                            case 2:
                                // Walkable Ground
                                color = Color.DarkGreen;
                                thickness = 3;
                                break;
                            case 4:
                                // Collidable Entity
                                color = Color.DarkRed;
                                thickness = 3;
                                break;
                        }
                    } else
                    {
                        if (Math.Abs(n.OutputValue) < 1 && (n.Id < INPUTS || n.Id > INPUTS + OUTPUTS - 1))
                        {
                            Vector3 greenest = Color.DarkGreen.ToVector3();
                            Vector3 redest = Color.DarkRed.ToVector3();
                            Vector3 colorDelta = greenest - redest;
                            color = new Color(redest + colorDelta - colorDelta * n.OutputValue);
                        }
                    }
                    Monocle.Draw.Circle(n.DrawPos, NODE_RADIUS, color, thickness, 100);
                    ActiveFont.Draw(Convert.ToString(n.Id), new Vector2(n.DrawPos.X - TEXT_OFFSET.X, n.DrawPos.Y - TEXT_OFFSET.Y), Vector2.Zero, NODE_LABEL_SCALE, color);
                    if (n.Layer == p.Brain.Layers-1)
                    {
                        // Draw Output IDs
                        ActiveFont.Draw(OutputLabels[n.Id], new Vector2(n.DrawPos.X + NODE_RADIUS+5, n.DrawPos.Y - NODE_RADIUS), Vector2.Zero, NODE_LABEL_SCALE * 2, color);
                    }
                }
            }
        }

        public static void DrawGraph()
        {
            int x = 500;
            int y = 100;
            int w = 600;
            int h = 300;
            float xInterval = w / CelesteBotInteropModule.Settings.GenerationsToSaveForGraph;
            float maxFitness = 0;
            foreach (float i in SavedBestFitnesses)
            {
                if (i > maxFitness)
                {
                    maxFitness = i;
                }
            }
            float yInterval = (h / (maxFitness + 1));

            Monocle.Draw.Rect(x, y, w, h, Color.Black * 0.8f); // Draws background

            Monocle.Draw.Line(new Vector2(x, y), new Vector2(x, y + h), Color.White);
            Monocle.Draw.Line(new Vector2(x, y + h), new Vector2(x + w, y + h), Color.White);

            ActiveFont.Draw(Convert.ToString(maxFitness), new Vector2(x - 30, y + 10), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
            if (SavedBestFitnesses.Count >= 2)
            {
                for (int i = 0; i < SavedBestFitnesses.Count-1; i++)
                {
                    float fitness = (float)SavedBestFitnesses[i];
                    float fitness2 = (float)SavedBestFitnesses[i + 1];
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, y + h - yInterval * fitness), new Vector2(x + xInterval * (i+1), y + h - yInterval * fitness2), Color.White);
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, y + h + 3), new Vector2(x + xInterval * i, y + h - 3), Color.White);
                    ActiveFont.Draw(Convert.ToString(CelesteBotInteropModule.population.Gen - SavedBestFitnesses.Count + i), new Vector2(x + xInterval * i, y + h + 15), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
                }
                Monocle.Draw.Line(new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h + 3), new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h - 3), Color.White);
                ActiveFont.Draw(Convert.ToString(CelesteBotInteropModule.population.Gen - 1), new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h + 15), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
            }
        }

        static Dictionary<string, int> orgHash = new Dictionary<string, int>();
        static Dictionary<string, int> speciesHash = new Dictionary<string, int>();
        private static void FillHash(Dictionary<string, int> h, string path)
        {
            string[] strings = System.IO.File.ReadAllLines(path);
            foreach (string s in strings)
            {
                h.Add(s, 0);
            }
        }
        public static void FillOrganismHash(string path)
        {
            FillHash(orgHash, path);
        }
        public static void FillSpeciesHash(string path)
        {
            FillHash(speciesHash, path);
        }
        private static string GetUniqueName(Dictionary<string, int> dict)
        {
            Random rand = new Random(Guid.NewGuid().GetHashCode());
            List<string> list = new List<string>(dict.Keys);
            if (list.Count == 0)
            {
                return ""; // No values or keys
            }
            int index = rand.Next(list.Count);
            string key = list[index];
            string outp = key + dict[key];
            dict[key]++;
            return outp;
        }
        public static string GetUniqueOrganismName()
        {
            return GetUniqueName(orgHash);
        }

        public static string GetUniqueSpeciesName()
        {
            return GetUniqueName(speciesHash);
        }

        // These should all contain variables in the near future

        public static void DrawFitness(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 30f, 600f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw(Convert.ToString(p.GetFitness()), new Vector2(3,30), Vector2.Zero, new Vector2(0.5f, 0.5f), Color.White);
        }
        public static void DrawStandard(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 0f, 600f, 30f, Color.Black * 0.8f);
            if (CelesteBotInteropModule.population.Gen == p.Gen)
            {
                ActiveFont.Draw("Gen: " + p.Gen + " Species: " + p.SpeciesName + " Organism (" + (CelesteBotInteropModule.population.CurrentIndex + 1) + "/" + CelesteBotInteropModule.population.Pop.Count + "): " + p.Name, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            } else
            {
                ActiveFont.Draw("Gen: " + CelesteBotInteropModule.population.Gen + " Species: " + p.SpeciesName + " Organism (" + (CelesteBotInteropModule.population.CurrentIndex + 1) + "/" + CelesteBotInteropModule.population.Pop.Count + "): " + p.Name, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            }
        }
        public static void DrawDetails(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 90f, 600f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw("(X: " + p.player.BottomCenter.X + ", Y: " + p.player.BottomCenter.Y + "), (Vx: " + p.player.Speed.X + ", Vy: " + p.player.Speed.Y + "), Dashes: " + p.player.Dashes + ", Stamina: " + p.player.Stamina, new Vector2(3,90), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
        }
        public static void DrawBestFitness()
        {
            Monocle.Draw.Rect(0f, 60f, 600f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw("Best Fitness: " + CelesteBotInteropModule.population.BestFitness, new Vector2(3, 60), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
        }
    }
}