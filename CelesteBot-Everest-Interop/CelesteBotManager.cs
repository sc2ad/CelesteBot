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
        public static float ACTION_THRESHOLD = 0.55f; // The value that must be surpassed for the output to be accepted
        public static float RE_RANDOMIZE_WEIGHT_CHANCE = 0.2f; // The chance for the weight to be re-randomized
        public static double WEIGHT_MUTATION_CHANCE = 0.65f; // The chance for a weight to be mutated
        public static double ADD_CONNECTION_CHANCE = 0.55f; // The chance for a new connection to be added
        public static double ADD_NODE_CHANCE = 0.15f; // The chance for a new node to be added

        public static double WEIGHT_MAXIMUM = 3; // Max magnitude a weight can be (+- this number)
        
        public static int VISION_2D_X_SIZE = 5; // X Size of the Vision array
        public static int VISION_2D_Y_SIZE = 5; // Y Size of the Vision array
        public static int TILE_2D_X_CACHE_SIZE = 1000;
        public static int TILE_2D_Y_CACHE_SIZE = 1000;
        public static int ENTITY_CACHE_UPDATE_FRAMES = 10;
        public static int FAST_MODE_MULTIPLIER = 10;
        public static int INPUTS = VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 6;
        public static int OUTPUTS = 6;

        // Moving Fitness Parameters
        public static float UPDATE_TARGET_THRESHOLD = 8; // Pixels in distance between the fitness target and the current position before considering it "reached"

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
        public static double PLAYER_DEATH_TIME_BEFORE_RESET = 4; // How many seconds after a player dies should the next player be created and the last one deleted

        // Paths/Prefixes
        public static string ORGANISM_PATH = @"organismNames.txt";
        public static string SPECIES_PATH = @"speciesNames.txt";
        public static string CHECKPOINT_FILE_PATH = @"Checkpoints";
        public static string CHECKPOINT_FILE_PREFIX = @"Checkpoints\checkpoint";
        public static string QTableSavePath = @"QTable.tbl";

        public static bool Cutscene = false;

        // Q Learning Variables
        public static QTable qTable;
        public static QState LastQState;
        public static InputData LastQAction;
        public static double LastQReward;
        public static int QIterations = 0;
        public static double MaxQReward = 0;
        public static int QMaxRewardIteration = 0;

        // Q Learning Settings
        public static double QLearningRate { get { return CelesteBotInteropModule.Settings.QLearningRate / 100.0; } set { } }
        public static double QGamma { get { return CelesteBotInteropModule.Settings.QGamma / 100.0; } set { } }
        public static double QEpsilon = CelesteBotInteropModule.Settings.MaxQEpsilon / 100.0;
        public static int QGraphIterations { get { return CelesteBotInteropModule.Settings.QGraphIterations; } set { } }

        public static void Initialize()
        {
            ACTION_THRESHOLD = (float)(Convert.ToDouble(CelesteBotInteropModule.Settings.ActionThreshold) / 100.0); // The value that must be surpassed for the output to be accepted
            RE_RANDOMIZE_WEIGHT_CHANCE = (float)(Convert.ToDouble(CelesteBotInteropModule.Settings.ReRandomizeWeightChance) / 100.0); // The chance for the weight to be re-randomized
            WEIGHT_MUTATION_CHANCE = (float)(Convert.ToDouble(CelesteBotInteropModule.Settings.MutateWeight) / 100.0); // The chance for a weight to be mutated
            ADD_CONNECTION_CHANCE = (float)(Convert.ToDouble(CelesteBotInteropModule.Settings.AddConnectionChance) / 100.0); // The chance for a new connection to be added
            ADD_NODE_CHANCE = (float)(Convert.ToDouble(CelesteBotInteropModule.Settings.AddNodeChance) / 100.0); // The chance for a new node to be added

            WEIGHT_MAXIMUM = CelesteBotInteropModule.Settings.WeightMaximum; // Max magnitude a weight can be (+- this number)

            VISION_2D_X_SIZE = CelesteBotInteropModule.Settings.XVisionSize; // X Size of the Vision array
            VISION_2D_Y_SIZE = CelesteBotInteropModule.Settings.YVisionSize; // Y Size of the Vision array
            TILE_2D_X_CACHE_SIZE = CelesteBotInteropModule.Settings.XMaxCacheSize; // X Size of max cache size
            TILE_2D_Y_CACHE_SIZE = CelesteBotInteropModule.Settings.YMaxCacheSize; // Y Size of max cache size
            ENTITY_CACHE_UPDATE_FRAMES = CelesteBotInteropModule.Settings.EntityCacheUpdateFrames; // Frames between updating entity cache
            FAST_MODE_MULTIPLIER = CelesteBotInteropModule.Settings.FastModeMultiplier; // speed multiplier for fast mode
            INPUTS = VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 6;
            OUTPUTS = 6;

            UPDATE_TARGET_THRESHOLD = CelesteBotInteropModule.Settings.UpdateTargetThreshold;

            GENE_POSITIVE_COLOR = Color.DarkGreen;
            GENE_NEGATIVE_COLOR = Color.Red;
            THICKNESS_SCALE = 5; // How much the thickness increases per increase of 1 in the weight when drawing genes
            NODE_RADIUS = 10;
            NODE_LABEL_SCALE = new Vector2(0.2f, 0.2f);
            TEXT_OFFSET = new Vector2(7, 7);
            // Graphing Parameters
            SavedBestFitnesses = new ArrayList();

            // POPULATION PARAMETERS
            EXTINCTION_SAVE_TOP = 5; // How many species to save when a mass extinction occurs
            //POPULATION_SIZE = 50;

            PLAYER_GRACE_BUFFER = 160; // How long between restarts should the next player be created, some arbitrary number of frames
            PLAYER_DEATH_TIME_BEFORE_RESET = 4; // How many seconds after a player dies should the next player be created and the last one deleted
    }

        public static void Draw()
        {
            //Monocle.Engine.Draw(gameTime);
            int viewWidth = Engine.ViewWidth;
            int viewHeight = Engine.ViewHeight;

            Monocle.Draw.SpriteBatch.Begin();
            try
            {
                if (!CelesteBotInteropModule.FitnessAppendMode)
                {
                    DrawStandard(CelesteBotInteropModule.CurrentPlayer);
                } else
                {
                    DrawAppendMode();
                    Monocle.Draw.SpriteBatch.End();
                    return;
                }
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
                if (CelesteBotInteropModule.DrawTarget)
                {
                    DrawFitnessTarget(CelesteBotInteropModule.CurrentPlayer);
                }
                if (CelesteBotInteropModule.DrawRewardGraph)
                {
                    DrawRewardGraph();
                }
            }
            catch (NullReferenceException e)
            {
                // The game has yet to finish loading, just don't draw for now.
            }
            Monocle.Draw.SpriteBatch.End();
        }
        // WHAT IF: I ONLY UPDATE PREVIOUS STATES' QTABLE DATA
        // BECAUSE I CANT PREDICT THE NEXT FRAME/STATE!
        public static void UpdateQTable()
        {
            QEpsilon = CelesteBotInteropModule.Settings.MinQEpsilon / 100.0 + (CelesteBotInteropModule.Settings.MaxQEpsilon / 100.0 - CelesteBotInteropModule.Settings.MinQEpsilon / 100.0) * Math.Exp(-CelesteBotInteropModule.Settings.QEpsilonDecay / 10000.0 * QIterations);
            // Reuses Generations/Fitness for the Graph
            // Hacky
            if (CelesteBotInteropModule.CurrentPlayer.Fitness > CelesteBotInteropModule.population.BestFitness)
            {
                CelesteBotInteropModule.population.BestFitness = CelesteBotInteropModule.CurrentPlayer.Fitness;
                CelesteBotInteropModule.population.Gen = QIterations;
            }
            if (CelesteBotInteropModule.CurrentPlayer.Fitness > 0)
            {
                SavedBestFitnesses.Add(CelesteBotInteropModule.CurrentPlayer.Fitness);
            }
            if (SavedBestFitnesses.Count > CelesteBotInteropModule.Settings.FramesToSaveForRewardGraph)
            {
                SavedBestFitnesses.RemoveAt(0);
            }
            // What is the current state?
            QState current = new QState(CelesteBotInteropModule.CurrentPlayer);
            // What is the current action?
            CelesteBotInteropModule.CurrentPlayer.CalculateQAction();
            InputData action = CelesteBotInteropModule.inputPlayer.Data;
            // Calculate the reward for that action
            // But I can't calculate the reward for an action that hasn't happened yet!
            // Instead, calculate the reward for the action that already happened.
            double reward = CelesteBotInteropModule.CurrentPlayer.CalculateReward();
            if (LastQState == null)
            {
                // This is the first frame. We cannot look forward because we cannot predict frames
                // So we just need to update lazily (update the last ones as opposed to the current ones)
                // If the last is null, then we should immediately exit because no updating of the QTable happens
                LastQState = current;
                LastQAction = action;
                LastQReward = reward;
                return;
            }
            
            if (qTable != null)
            {
                qTable.Update(LastQState, LastQAction, QFunction(qTable, LastQState, current, LastQAction, LastQReward));
            }
            if (reward > -10000)
            {
                CelesteBotInteropModule.CurrentPlayer.Rewards.Add(reward);
            }
            if (CelesteBotInteropModule.CurrentPlayer.Rewards.Count > CelesteBotInteropModule.Settings.FramesToSaveForRewardGraph)
            {
                CelesteBotInteropModule.CurrentPlayer.Rewards.RemoveAt(0);
            }
            if (reward > MaxQReward)
            {
                MaxQReward = reward;
                QMaxRewardIteration = QIterations;
            }
            LastQState = current;
            LastQAction = action;
            LastQReward = reward;
        }
        private static double QFunction(QTable table, QState state, QState newState, InputData action, double value) {
            return table.GetValue(state, action) + QLearningRate * (value + QGamma * table.GetMax(newState) - table.GetValue(state, action));
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

            if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                x = 1400;
                w = 200;
                Monocle.Draw.Rect(x, y, w, h, Color.Black * 0.8f); // Draws background

                // Not even going to bother, just draw output

                double dy = h / (double)(OUTPUTS + 1);

                InputData data = CelesteBotInteropModule.inputPlayer.Data;

                if (data == QTable.GetAction(qTable.GetMaxActionIndex(LastQState)))
                {
                    ActiveFont.Draw("Not Guessing!\nReward: " + String.Format("{0:0.000}", qTable.GetMax(LastQState)), new Vector2(x + w / 2 - 60, y + 30), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
                }

                for (int i = 0; i < OUTPUTS; i++)
                {
                    string outputLabel = "";
                    Color c = Color.White;

                    double value = 0;

                    switch (i)
                    {
                        case 0:
                            outputLabel = "Left/Right";
                            value = data.MoveX;
                            break;
                        case 1:
                            outputLabel = "Up/Down";
                            value = data.MoveY;
                            break;
                        case 2:
                            outputLabel = "Jump";
                            value = data.Jump ? 1 : 0;
                            break;
                        case 3:
                            outputLabel = "Dash";
                            value = data.Dash ? 1 : 0;
                            break;
                        case 4:
                            outputLabel = "Grab";
                            value = data.Grab ? 1 : 0;
                            break;
                        case 5:
                            outputLabel = "LongJump";
                            value = data.LongJumpValue;
                            break;
                    }
                    if (value > 0)
                    {
                        c = Color.DarkGreen;
                    }
                    else if (value < 0)
                    {
                        c = Color.DarkRed;
                    }

                    Monocle.Draw.Circle(x + NODE_RADIUS*2, (int)(y + dy * (i+1)), 10, c, 3, 30);
                    ActiveFont.Draw(outputLabel, new Vector2(x + NODE_RADIUS*3 + 5, (int)(y + dy * (i+1)) - NODE_RADIUS), Vector2.Zero, NODE_LABEL_SCALE * 2, c);
                }
                return;
            }

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
                    Node temp = (Node)a[j];
                    if (i == 0 && j >= VISION_2D_X_SIZE * VISION_2D_Y_SIZE)
                    {
                        //dy = (double)h / (double)(a.Count + 1 - VISION_2D_X_SIZE * VISION_2D_Y_SIZE);
                        drawY = y + (int)((h * (j - VISION_2D_X_SIZE * VISION_2D_Y_SIZE + 1)) / (double)(a.Count + 1 - VISION_2D_X_SIZE * VISION_2D_Y_SIZE));
                    }
                    else
                    {
                        drawY += (int)dy;
                    }
                    // Handles drawing of 2D Vision array
                    if (temp.Id < VISION_2D_X_SIZE * VISION_2D_Y_SIZE)
                    {
                        // This Node is an input, 2d vision node
                        try
                        {
                            Vector2 renderPos = p.player.BottomCenter;

                            renderPos -= TileFinder.GetCelesteLevel().Camera.Position;
                            renderPos *= 6f;

                            double tileWidth = 48;
                            double tileHeight = 48;

                            Vector2 pos = new Vector2((float)(renderPos.X + (-VISION_2D_X_SIZE / 2 + temp.Id % VISION_2D_X_SIZE) * tileWidth), (float)(renderPos.Y + (-VISION_2D_Y_SIZE / 2 + temp.Id / VISION_2D_Y_SIZE - 0.5) * tileHeight));
                            temp.DrawPos = pos;
                            temp.DrawRadius = 2 * NODE_RADIUS;
                        }
                        catch (NullReferenceException e)
                        {
                            // Player DNE.
                            // Draw the positions in a standard way.
                            temp.DrawPos = new Vector2(drawX, drawY);
                            temp.DrawRadius = NODE_RADIUS;
                        }
                    }
                    else
                    {
                        temp.DrawPos = new Vector2(drawX, drawY);
                    }
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
            Dictionary<int, string> Labels = new Dictionary<int, string>();

            ArrayList outputNodes = (ArrayList)nodes2d[nodes2d.Count - 1];
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
                        outputLabel = "LongJump";
                        break;
                }
                Labels.Add(n.Id, outputLabel);
            }
            ArrayList biases = (ArrayList)nodes2d[0];
            for (int i = VISION_2D_X_SIZE * VISION_2D_Y_SIZE; i < biases.Count; i++)
            {
                Node n = (Node)biases[i];
                string label = "";
                switch (i - VISION_2D_X_SIZE * VISION_2D_Y_SIZE)
                {
                    case 0:
                        label = "X";
                        break;
                    case 1:
                        label = "Y";
                        break;
                    case 2:
                        label = "Vx";
                        break;
                    case 3:
                        label = "Vy";
                        break;
                    case 4:
                        label = "Can Dash?";
                        break;
                    case 5:
                        label = "Stamina";
                        break;
                    case 6:
                        label = "Bias";
                        break;
                }
                Labels.Add(n.Id, label);
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
                            case 8:
                                // Spike
                                color = Color.Blue;
                                thickness = 3;
                                break;
                        }
                        //color *= 0.7f; // Transparency
                    } else
                    {
                        if (Math.Abs(n.OutputValue) < 1 && (n.Id < INPUTS || n.Id > INPUTS + OUTPUTS - 1))
                        {
                            Vector3 greenest = Color.DarkGreen.ToVector3();
                            Vector3 redest = Color.DarkRed.ToVector3();
                            Vector3 colorDelta = greenest - redest;
                            color = new Color(redest + colorDelta/2 + colorDelta/2 * n.OutputValue);
                        }
                    }
                    //Monocle.Draw.Circle(n.DrawPos, NODE_RADIUS, color, 0, 100);
                    if (n.Id < VISION_2D_X_SIZE * VISION_2D_Y_SIZE)
                    {
                        thickness = 6;
                    }
                    //Monocle.Draw.Circle(n.DrawPos, n.DrawRadius, color, thickness, 100);
                    Monocle.Draw.Rect(n.DrawPos.X, n.DrawPos.Y, n.DrawRadius, n.DrawRadius, color);
                    //ActiveFont.Draw(Convert.ToString(n.Id), new Vector2(n.DrawPos.X - TEXT_OFFSET.X, n.DrawPos.Y - TEXT_OFFSET.Y), Vector2.Zero, NODE_LABEL_SCALE, color);
                    if (Labels.ContainsKey(n.Id))
                    {
                        // Draw Labels
                        ActiveFont.Draw(Labels[n.Id], new Vector2(n.DrawPos.X + NODE_RADIUS + 5, n.DrawPos.Y - NODE_RADIUS), Vector2.Zero, NODE_LABEL_SCALE * 2, color);
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
            if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                xInterval = w / CelesteBotInteropModule.Settings.FramesToSaveForRewardGraph;
            }
            float maxFitness = 0;
            foreach (float i in SavedBestFitnesses)
            {
                if (i > maxFitness)
                {
                    maxFitness = i;
                }
            }
            if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                maxFitness = CelesteBotInteropModule.population.BestFitness;
            }
            float yInterval = (float)(h / (maxFitness));
            float yBuffer = 10;

            Monocle.Draw.Rect(x, y, w, h, Color.Black * 0.8f); // Draws background

            Monocle.Draw.Line(new Vector2(x, y), new Vector2(x, y + h), Color.White);
            Monocle.Draw.Line(new Vector2(x, y + h), new Vector2(x + w, y + h), Color.White);

            ActiveFont.Draw("Fitness/Time", new Vector2(x + w / 2 - 100, y - 50), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
            ActiveFont.Draw(String.Format("{0:0.0000}", maxFitness), new Vector2(x - 75, y + 10), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
            if (SavedBestFitnesses.Count >= 2)
            {
                for (int i = 0; i < SavedBestFitnesses.Count - 1; i++)
                {
                    float fitness = (float)SavedBestFitnesses[i];
                    float fitness2 = (float)SavedBestFitnesses[i + 1];
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, y + h - yInterval * fitness + yBuffer), new Vector2(x + xInterval * (i + 1), y + h - yInterval * fitness2 + yBuffer), Color.White);
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, y + h + 3), new Vector2(x + xInterval * i, y + h - 3), Color.White);
                    if (CelesteBotInteropModule.LearningStyle == LearningStyle.NEAT)
                    {
                        ActiveFont.Draw(Convert.ToString(CelesteBotInteropModule.population.Gen - SavedBestFitnesses.Count + i), new Vector2(x + xInterval * i, y + h + 15), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
                    }
                }
                Monocle.Draw.Line(new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h + 3), new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h - 3), Color.White);
                ActiveFont.Draw(Convert.ToString(CelesteBotInteropModule.population.Gen - 1), new Vector2(x + xInterval * (SavedBestFitnesses.Count - 1), y + h + 15), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
                Monocle.Draw.Line(new Vector2(x - 3, y + yBuffer), new Vector2(x + 3, y + yBuffer), Color.White);
            }
        }

        public static void DrawRewardGraph()
        {
            if (CelesteBotInteropModule.LearningStyle != LearningStyle.Q)
            {
                return;
            }
            int x = 500;
            int y = 500;
            int w = 600;
            int h = 300;

            float xInterval = w / CelesteBotInteropModule.Settings.FramesToSaveForRewardGraph;
            double maxReward = MaxQReward;
            float yInterval = (float)(h / (maxReward));
            float yBuffer = 10;

            Monocle.Draw.Rect(x, y, w, h, Color.Black * 0.8f);

            Monocle.Draw.Line(new Vector2(x, y), new Vector2(x, y + h), Color.White);
            Monocle.Draw.Line(new Vector2(x, y + h), new Vector2(x + w, y + h), Color.White);

            ActiveFont.Draw("Reward/Time", new Vector2(x + w / 2 - 100, y - 50), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
            ActiveFont.Draw(String.Format("{0:0.000}", maxReward), new Vector2(x - 75, y + 10), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);

            if (CelesteBotInteropModule.CurrentPlayer.Rewards.Count >= 2)
            {
                for (int i = 0; i < CelesteBotInteropModule.CurrentPlayer.Rewards.Count - 1; i++)
                {
                    double reward1 = CelesteBotInteropModule.CurrentPlayer.Rewards[i];
                    double reward2 = CelesteBotInteropModule.CurrentPlayer.Rewards[i + 1];
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, (float)(y + h - yInterval * reward1 + yBuffer)), new Vector2(x + xInterval * (i + 1), (float)(y + h - yInterval * reward2 + yBuffer)), Color.White);
                    Monocle.Draw.Line(new Vector2(x + xInterval * i, y + h + 3), new Vector2(x + xInterval * i, y + h - 3), Color.White);
                }
                Monocle.Draw.Line(new Vector2(x + xInterval * (CelesteBotInteropModule.CurrentPlayer.Rewards.Count - 1), y + h + 3), new Vector2(x + xInterval * (CelesteBotInteropModule.CurrentPlayer.Rewards.Count - 1), y + h - 3), Color.White);
                ActiveFont.Draw(Convert.ToString(CelesteBotInteropModule.CurrentPlayer.Rewards.Count - 1), new Vector2(x + xInterval * (CelesteBotInteropModule.CurrentPlayer.Rewards.Count - 1), y + h + 15), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
                Monocle.Draw.Line(new Vector2(x - 3, y + yBuffer), new Vector2(x + 3, y + yBuffer), Color.White);
            }
        }

        public static void DrawFitnessTarget(CelestePlayer player)
        {
            Vector2 target = player.Target;
            if (target == null)
            {
                return;
            }
            Vector2 renderPos = target;

            renderPos -= TileFinder.GetCelesteLevel().Camera.Position;
            renderPos *= 6f;

            Monocle.Draw.Circle(renderPos, CelesteBotInteropModule.Settings.UpdateTargetThreshold, Color.Yellow, 20);
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
            Monocle.Draw.Rect(0f, 0f, 600f, 60f, Color.Black * 0.8f);
            if (CelesteBotInteropModule.LearningStyle == LearningStyle.NEAT)
            {
                if (CelesteBotInteropModule.population.Gen == p.Gen)
                {
                    ActiveFont.Draw("Gen: " + p.Gen + " Species: " + p.SpeciesName + " Organism (" + (CelesteBotInteropModule.population.CurrentIndex + 1) + "/" + CelesteBotInteropModule.population.Pop.Count + "): " + p.Name, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
                }
                else
                {
                    ActiveFont.Draw("Gen: " + CelesteBotInteropModule.population.Gen + " Species: " + p.SpeciesName + " Organism (" + (CelesteBotInteropModule.population.CurrentIndex + 1) + "/" + CelesteBotInteropModule.population.Pop.Count + "): " + p.Name, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
                }
            } else if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                ActiveFont.Draw("QLearningRate: " + QLearningRate + " QGamma: " + QGamma + " QEpsilon: " + QEpsilon + " QIterations: " + QIterations + " Max Reward At: " + QMaxRewardIteration, new Vector2(3, 0), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            }
            ActiveFont.Draw("Selected Target: " + p.Target, new Vector2(3, 30), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
        }
        public static void DrawDetails(CelestePlayer p)
        {
            Monocle.Draw.Rect(0f, 90f, 600f, 30f, Color.Black * 0.8f);
            ActiveFont.Draw("(X: " + p.player.BottomCenter.X + ", Y: " + p.player.BottomCenter.Y + "), (Vx: " + p.player.Speed.X + ", Vy: " + p.player.Speed.Y + "), Dashes: " + p.player.Dashes + ", Stamina: " + p.player.Stamina, new Vector2(3,90), Vector2.Zero, new Vector2(0.4f, 0.4f), Color.White);
        }
        public static void DrawBestFitness()
        {
            Monocle.Draw.Rect(0f, 60f, 600f, 30f, Color.Black * 0.8f);
            if (CelesteBotInteropModule.LearningStyle == LearningStyle.NEAT)
            {
                ActiveFont.Draw("Best Fitness: " + CelesteBotInteropModule.population.BestFitness, new Vector2(3, 60), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            } else if (CelesteBotInteropModule.LearningStyle == LearningStyle.Q)
            {
                ActiveFont.Draw("Best Fitness: " + CelesteBotInteropModule.population.BestFitness + " At: " + CelesteBotInteropModule.population.Gen + " QStateCount: " + qTable.GetStateCount() + " Actions: " + QTable.GetActionCount(), new Vector2(3, 60), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            }
        }
        public static void DrawAppendMode()
        {
            try
            {
                Player player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
                Level level = (Level)Celeste.Celeste.Scene;

                Monocle.Draw.Rect(0f, 60f, 600f, 30f, Color.Black * 0.8f);
                ActiveFont.Draw(level.Session.MapData.Filename + "_"+ level.Session.Level + ": [" + player.BottomCenter.X + ", " + player.BottomCenter.Y + ", " + player.Speed.X + ", " + player.Speed.Y + "]", new Vector2(3, 60), Vector2.Zero, new Vector2(0.45f, 0.45f), Color.White);
            }
            catch (Exception e)
            {
                // Pass
            }
        }
    }
}