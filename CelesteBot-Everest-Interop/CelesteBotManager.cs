using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;

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
                
            }
            catch (NullReferenceException e)
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Failed to draw text: " + ActiveText);
                // The game has yet to finish loading, just don't draw text for now.
            }
            Monocle.Draw.SpriteBatch.End();
        }
        public static void CheckForRestart()
        {

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
    }
}