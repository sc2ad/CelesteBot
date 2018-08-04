using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;

namespace CelesteBot_Everest_Interop
{
    public class CelesteBotManager
    {
        public static float ACTION_THRESHOLD = 0.55f;
        public static float RE_RANDOMIZE_WEIGHT_CHANCE = 0.3f;
        public static double WEIGHT_MUTATION_CHANCE = 0.85;
        public static double ADD_CONNECTION_CHANCE = 0.3;
        public static double ADD_NODE_CHANCE = 0.02;

        public static double WEIGHT_MAXIMUM = 5; // Max magnitude a weight can be (+- this number)
        
        public static int VISION_2D_X_SIZE = 5;
        public static int VISION_2D_Y_SIZE = 5;
        public static int INPUTS = VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 5;
        public static int OUTPUTS = 6;

        public static Color GENE_POSITIVE_COLOR = Color.DarkGreen;
        public static Color GENE_NEGATIVE_COLOR = Color.Red;
        public static double THICKNESS_SCALE = 5; // How much the thickness increases per increase of 1 in the weight when drawing genes
        public static int NODE_RADIUS = 10;
        public static Vector2 NODE_LABEL_SCALE = new Vector2(0.2f, 0.2f);
        public static Vector2 TEXT_OFFSET = new Vector2(7, 7);

        // POPULATION PARAMETERS
        public static int EXTINCTION_SAVE_TOP = 5; // How many species to save when a mass extinction occurs
        public static int POPULATION_SIZE = 50;

        public static int PLAYER_GRACE_BUFFER = 120; // How long between restarts should the next player be created

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
        public static void DrawPlayer(CelestePlayer p)
        {
            int x = 100;
            int y = 200;
            int w = 600;
            int h = 600;

            Logger.Log(CelesteBotInteropModule.ModLogKey, p.ToString());

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
                    Monocle.Draw.Circle(n.DrawPos, NODE_RADIUS, color, thickness, 100);
                    ActiveFont.Draw(Convert.ToString(n.Id), new Vector2(n.DrawPos.X - TEXT_OFFSET.X, n.DrawPos.Y - TEXT_OFFSET.Y), Vector2.Zero, NODE_LABEL_SCALE, color);
                }
            }
        }

        // These should all contain variables in the near future

        public static void DrawFitness(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 30f, 500f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw(Convert.ToString(p.GetFitness()), new Vector2(3,30), Vector2.Zero, new Vector2(0.5f, 0.5f), Color.White);
        }
        public static void DrawStandard(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 0f, 500f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw("Gen: " + p.Gen + " Species: " + p.SpeciesName + " Organism: " + p.Name, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
        }
        public static void DrawDetails(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 60f, 500f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw("(X: " + p.player.BottomCenter.X + ", Y: " + p.player.BottomCenter.Y + "), (Vx: " + p.player.Speed.X + ", Vy: " + p.player.Speed.Y + "), Dashes: " + p.player.Dashes + ", Stamina: " + p.player.Stamina, new Vector2(3,60), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
        }
    }
}