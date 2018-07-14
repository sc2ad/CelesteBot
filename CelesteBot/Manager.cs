using Celeste;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot
{
    public class Manager
    {
        private static KeyboardState kbState;
        private static bool IsKeyDown(Keys key)
        {
            return kbState.IsKeyDown(key);
        }
        public static Vector2 GetXYCenterFromTile(Vector2 tPos)
        {
            int offsetX = -428;
            int offsetY = -244;
            int width = 8, height = 8;
            return new Vector2(offsetX + tPos.X * width, offsetY + tPos.Y * height);
        }
        public static Vector2 GetXYTopLeftFromTile(Vector2 tPos)
        {
            int offsetX = -432;
            int offsetY = -248;
            int width = 8, height = 8;
            return new Vector2(offsetX + tPos.X * width, offsetY + tPos.Y * height);
        }
        public static Vector2 GetTilePosFromXYCenter(Vector2 realPos)
        {
            int offsetX = -428;
            int offsetY = -244;
            int width = 8, height = 8;
            return new Vector2((int)((realPos.X - offsetX) / width), (int)((realPos.Y - offsetY) / height));
        }
        public static Vector2 GetTilePosFromXYTopLeft(Vector2 realPos)
        {
            int offsetX = -432;
            int offsetY = -248;
            int width = 8, height = 8;
            return new Vector2((int)((realPos.X - offsetX) / width), (int)((realPos.Y - offsetY) / height));
        }
        public static Vector2 GetTileUnderPlayer()
        {
            Player player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
            Vector2 playerPos = player.Position; // This is the bottom center
            playerPos = new Vector2(playerPos.X, playerPos.Y + 4);

            return GetTilePosFromXYCenter(playerPos); // This is to account for offset
        }
        public static Vector2 GetTileInFrontOfPlayer()
        {
            Player player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
            Vector2 playerPos = player.Position; // This is the bottom center

            if (player.Facing > 0)
            {
                playerPos = new Vector2(playerPos.X + 8, playerPos.Y - 4); // This is hacky but probably works
                // Facing Right
            }
            else
            {
                playerPos = new Vector2(playerPos.X - 8, playerPos.Y - 4); // This is hacky but probably works
                // Facing Left
            }

            return GetTilePosFromXYCenter(playerPos);
        }
        public static bool IsTile(Vector2 tile)
        {
            Level celesteLevel = Celeste.Celeste.Scene as Level;
            SolidTiles tiles = celesteLevel.SolidTiles;
            MTexture[,] tileArray = tiles.Tiles.Tiles.ToArray();
            return tileArray[(int)tile.X, (int)tile.Y] != null;
        }
        public static void PutEntitiesToFile()
        {
            Level celesteLevel = Celeste.Celeste.Scene as Level;
            SolidTiles tiles = celesteLevel.SolidTiles;

            string readableTextures = "Center of all tiles (?): " + tiles.Center + "\n";
            try
            {
                readableTextures += "Tile(s?) are at Position: " + tiles.Tiles.Position + " with tile (w, h): (" + tiles.Tiles.TileWidth + ", " + tiles.Tiles.TileHeight + ")\n";
                System.Collections.Generic.IEnumerator<Component> enummer = tiles.GetEnumerator();
                enummer.Reset();
                for (int i = 0; i < 100000; i++)
                {
                    enummer.MoveNext();
                    readableTextures += "Texture (?) at Position: " + enummer.Current.Entity.Position + " and Center: " + enummer.Current.Entity.Center + " has tag: " + enummer.Current.Entity.Tag + "\n";
                }
            }
            catch (Exception)
            {
                readableTextures += "Enummer has completed!";
            }
            /*
            MTexture[,] textures = tiles.Tiles.Tiles.ToArray();
            string readableTextures = "";
            for (int i = 0; i < textures.Length; i++)
            {
                for (int j = 0; j < textures.Length; j++)
                {
                    try
                    {
                        readableTextures += "Texture at: " + textures[i, j].Center + " looks like (as a string): " + textures[i, j].ToString() + "\n";
                    } catch (IndexOutOfRangeException e)
                    {
                        readableTextures += "Texture at index: (" + i + ", " + j + ") is out of bounds!\n";
                    }
                }
                
            }
            */
            string text = tiles.Tiles.ToString() + "\n";
            text += tiles.Tiles.Tiles.GetSegment(1, 1).ToString() + "\n";
            text += tiles.Tiles.Tiles.ToArray().ToString() + "\n";
            MTexture[,] tileArray = tiles.Tiles.Tiles.ToArray();
            System.Collections.IEnumerator enummer2 = tiles.Tiles.Tiles.ToArray().GetEnumerator();
            enummer2.Reset();
            string arrayText = "";

            //System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\tiles.txt", text);
            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\readableTextures.txt", readableTextures);
            //System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\textures.txt", arrayText);

            EntityList entities = Celeste.Celeste.Scene.Entities;/*Tracker.GetEntities<Entity>();*/

            Vector2 tileLocationUnderPlayer = GetTileUnderPlayer();
            Vector2 tileLocationInFront = GetTileInFrontOfPlayer();

            Player player = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
            Vector2 playerPos = player.Position;

            string toWrite = "Tile under player does not exist! err: " + tileLocationUnderPlayer + " with player pos: " + playerPos;
            if (IsTile(tileLocationUnderPlayer))
            {
                // This should be the tile X,Y under the player. Save to file.
                toWrite = "Tile under player has Center: " + GetXYCenterFromTile(tileLocationUnderPlayer) + " and Tile Loc: " + tileLocationUnderPlayer;
                toWrite += "\nWith Player pos: " + playerPos;
            }

            if (IsTile(tileLocationInFront))
            {
                toWrite += "\nTile in front of player has Center: " + GetXYCenterFromTile(tileLocationInFront) + " and Tile Loc: " + tileLocationInFront;
                toWrite += "\nWith Player facing: " + (player.Facing == (Facings)1 ? "Right" : "Left");
            }
            else
            {
                toWrite += "\nTile in front of player does not exist! err: " + tileLocationInFront + " with player pos: " + playerPos + " facing: " + (player.Facing == (Facings)1 ? "Right" : "Left");
            }


            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\info.txt", toWrite);


            string text2 = "";
            string readableText = "";
            for (int i = 0; i < entities.Count; i++)
            {
                text += entities[i].ToString() + "\n";
                readableText += "Entity at Position: " + entities[i].Position + " and Center: " + entities[i].Center + " has tag: " + entities[i].Tag + "\n";
            }
            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\entities.txt", text2);
            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\readableEntities.txt", readableText);
        }
        private static GamePadState GetGamePadState()
        {
            GamePadState currentState = MInput.GamePads[0].CurrentState;
            for (int i = 0; i < 4; i++)
            {
                currentState = GamePad.GetState((PlayerIndex)i);
                if (currentState.IsConnected)
                {
                    break;
                }
            }
            return currentState;
        }
        public static GamePadState CalculateInputs()
        {
            kbState = Keyboard.GetState();

            GamePadDPad pad = new GamePadDPad(
                ButtonState.Released,
                ButtonState.Released,
                IsKeyDown(Keys.OemQuotes) ? ButtonState.Pressed : ButtonState.Released,
                IsKeyDown(Keys.OemBackslash) ? ButtonState.Pressed : ButtonState.Released);
            GamePadThumbSticks sticks = new GamePadThumbSticks(new Vector2(0, 0), new Vector2(0, 0));
            GamePadState padState = new GamePadState(
                sticks,
                new GamePadTriggers(0, 0),
                new GamePadButtons(
                    IsTile(GetTileInFrontOfPlayer()) ? Buttons.A : (Buttons)0
                    | (Buttons)0
                    | (Buttons)0
                    | (Buttons)0
                    | (Buttons)0
                    | (Buttons)0
                    | (Buttons)0
                ),
                pad
            );
            return padState;
        }
        public static void UpdateInputs()
        {
            kbState = Keyboard.GetState();
            GamePadState padState = GetGamePadState();

            if (IsKeyDown(Keys.OemBackslash) || IsKeyDown(Keys.OemQuotes))
            {
                padState = CalculateInputs();

                bool found = false;
                for (int i = 0; i < 4; i++)
                {
                    //MInput.GamePads[i].Update();
                    if (MInput.GamePads[i].Attached)
                    {
                        found = true;
                        MInput.GamePads[i].CurrentState = padState;
                    }
                }

                if (!found)
                {
                    MInput.GamePads[0].CurrentState = padState;
                    MInput.GamePads[0].Attached = true;
                }
                MInput.UpdateVirtualInputs();
                PutEntitiesToFile();
                return;
            }
            for (int i = 0; i < 4; i++)
            {
                if (MInput.GamePads[i].Attached)
                {
                    MInput.GamePads[i].CurrentState = padState;
                }
            }
            MInput.UpdateVirtualInputs();
            MInput.UpdateVirtualInputs();
        }
    }
}
