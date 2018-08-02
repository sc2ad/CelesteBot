﻿using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    class TileFinder
    {
        public static Vector2 TilesOffset = new Vector2(0,0); // TilesOffset for SolidsData offset
        public static void GetAllEntities()
        {
            EntityList entities = Celeste.Celeste.Scene.Entities;
            string readableText = "";
            for (int i = 0; i < entities.Count; i++)
            {
                //text += entities[i].ToString() + "\n";
                readableText += "Entity at Position: " + entities[i].Position + " and Center: " + entities[i].Center + " has tag: " + entities[i].Tag + " with collision: " + entities[i].Collidable + "\n";
            }
            //System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\entities.txt", text2);
            System.IO.File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Celeste\readableEntities.txt", readableText);
            Logger.Log(CelesteBotInteropModule.ModLogKey, "All Entities stored in: readableEntities.txt");
        }
        public static void SetupOffset()
        {
            Vector2 min = new Vector2(0, 0);
            EntityList list = Celeste.Celeste.Scene.Entities;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].BottomCenter.X < min.X)
                {
                    min.X = list[i].BottomCenter.X;
                }
                if (list[i].BottomCenter.Y < min.Y)
                {
                    min.Y = list[i].BottomCenter.Y;
                }
            }
            TilesOffset = min;
        }
        public static Vector2 GetTileXY(Vector2 realPos)
        {
            int tileW = 8, tileH = 8;
            return new Vector2((int)((realPos.X - TilesOffset.X) / tileW), (int)((realPos.Y - TilesOffset.Y) / tileH));
        }
        public static Vector2 RealFromTile(Vector2 tile)
        {
            int width = 8, height = 8;
            return new Vector2(TilesOffset.X + tile.X * width, TilesOffset.Y + tile.Y * height);
        }
        public static bool IsTileAtReal(Vector2 realPos)
        {
            try
            {
                Level celesteLevel = (Level)Celeste.Celeste.Scene;
                SolidTiles tiles = celesteLevel.SolidTiles;
                MTexture[,] tileArray = tiles.Tiles.Tiles.ToArray();
                Vector2 tile = GetTileXY(realPos);
                return tileArray[(int)tile.X, (int)tile.Y] != null;
            }
            catch (NullReferenceException e)
            {
                return false;
            }
            catch (IndexOutOfRangeException e)
            {
                return false;
            }
        }
        public static bool IsWallAtReal(Vector2 realPos)
        {
            try
            {
                Level celesteLevel = (Level)Celeste.Celeste.Scene;
                SolidTiles tiles = celesteLevel.SolidTiles;
                MTexture[,] tileArray = tiles.Tiles.Tiles.ToArray();
                Vector2 tile = GetTileXY(realPos);
                if (tileArray[(int)tile.X, (int)tile.Y] != null)
                {
                    if (!tileArray[(int)tile.X, (int)tile.Y].Equals("tilesets/scenery"))
                    {
                        return true;
                    }
                }
                EntityList entities = Celeste.Celeste.Scene.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].CollidePoint(realPos))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (NullReferenceException e)
            {
                return false;
            }
            catch (IndexOutOfRangeException e)
            {
                return false;
            }
        }
        public static bool IsWallAtTile(Vector2 tile)
        {
            try
            {
                Level celesteLevel = (Level)Celeste.Celeste.Scene;
                SolidTiles tiles = celesteLevel.SolidTiles;
                MTexture[,] tileArray = tiles.Tiles.Tiles.ToArray();
                if (tileArray[(int)tile.X, (int)tile.Y] != null)
                {
                    if (!tileArray[(int)tile.X, (int)tile.Y].Equals("tilesets/scenery"))
                    {
                        return true;
                    }
                }
                EntityList entities = Celeste.Celeste.Scene.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].CollidePoint(RealFromTile(tile)))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch (NullReferenceException e)
            {
                return false;
            }
            catch (IndexOutOfRangeException e)
            {
                return false;
            }
        }
    }
}