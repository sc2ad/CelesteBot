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
        public static float ACTION_THRESHOLD = 0.7f;
        public static float RE_RANDOMIZE_WEIGHT_CHANCE = 0.2f;
        public static double WEIGHT_MUTATION_CHANCE = 0.8;
        public static double ADD_CONNECTION_CHANCE = 0.1;
        public static double ADD_NODE_CHANCE = 0.01;
        
        public static int VISION_2D_X_SIZE = 5;
        public static int VISION_2D_Y_SIZE = 5;
        public static int INPUTS = VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 5;
        public static int OUTPUTS = 6;

        public static Color GENE_POSITIVE_COLOR = Color.Red;
        public static Color GENE_NEGATIVE_COLOR = Color.Blue;
        public static int NODE_RADIUS = 10;
        public static Vector2 NODE_LABEL_SCALE = new Vector2(0.2f, 0.2f);
        public static Vector2 TEXT_OFFSET = new Vector2(7, 7);

        private static string activeText = "Vision:\n";
        private static Vector2 FontScale = new Vector2(0.4f, 0.4f);
        private static Vector2 textPos = new Vector2(20f, 30f);

        public static string ActiveText { get => activeText; set => activeText = value; }

        public static bool Cutscene = false;

        public static void Draw()
        {
            //Monocle.Engine.Draw(gameTime);
            int viewWidth = Engine.ViewWidth;
            int viewHeight = Engine.ViewHeight;

            Monocle.Draw.SpriteBatch.Begin();
            try
            {
                Monocle.Draw.Rect(textPos.X - 10, textPos.Y - 8, viewWidth - 20f, 40f, Color.Black * 0.8f);
                ActiveFont.Draw(
                    ActiveText,
                    new Vector2(textPos.X, textPos.Y),
                    Vector2.Zero,
                    FontScale,
                    Color.White);
                if (CelesteBotInteropModule.DrawPlayer)
                {
                    DrawPlayer(CelesteBotInteropModule.tempPlayer);
                }
            }
            catch (NullReferenceException e)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Failed to draw text: " + ActiveText);
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
                foreach (Node n in p.Brain.network)
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
                double dy = (double)h / (double)a.Count;
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
                Color color = temp.Weight > 0 ? GENE_POSITIVE_COLOR : GENE_NEGATIVE_COLOR; // Sets color of gene. 
                Vector2 fromLoc = new Vector2(-1, -1);
                Vector2 toLoc = new Vector2(-1, -1);
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
                if (fromLoc.X < 0 && toLoc.X < 0)
                {
                    continue;
                }
                Monocle.Draw.Line(fromLoc, toLoc, color);
            }

            // Draws all of the Nodes in order
            foreach (ArrayList a in nodes2d)
            {
                foreach (Node n in a)
                {
                    Monocle.Draw.Circle(n.DrawPos, NODE_RADIUS, Color.White, 100);
                    ActiveFont.Draw(Convert.ToString(n.Id), new Vector2(n.DrawPos.X - TEXT_OFFSET.X, n.DrawPos.Y - TEXT_OFFSET.Y), Vector2.Zero, NODE_LABEL_SCALE, Color.White);
                }
            }
        }
    }
}