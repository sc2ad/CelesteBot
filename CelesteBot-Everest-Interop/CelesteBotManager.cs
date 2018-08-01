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
        private static string activeText = "Vision:\n";
        private static Vector2 FontScale = new Vector2(0.4f, 0.4f);
        private static Vector2 textPos = new Vector2(20f, 30f);

        public static string ActiveText { get => activeText; set => activeText = value; }


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
    }
}