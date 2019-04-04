using Celeste;
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
    public enum Entity : int
    {
        Unset = 0,
        Air = 1,
        Tile = 2,
        Other = 3,
        IntroCrusher = 4,
        JumpthruPlatform = 5,
        Spring = 6,
        Strawberry = 7,
        Spikes = 8,
        ZipMover = 9,
        CrumblePlatform = 10,
        DashBlock = 11,
        FakeWall = 12,
        Refill = 13,
        ChangeRespawnTrigger = 14,
        FallingBlock = 15,
    }
    public class TileFinder
    {
        private static Level celesteLevel;
        private static SolidTiles tiles;
        public static MTexture[,] tileArray;

        public static Vector2 TilesOffset = new Vector2(0,0); // TilesOffset for SolidsData offset

        private static int framesToCacheWipe = CelesteBotManager.ENTITY_CACHE_UPDATE_FRAMES;
        private static Level cachedLevel;
        private static Vector2 cacheOffset = new Vector2(CelesteBotManager.TILE_2D_X_CACHE_SIZE / 2, CelesteBotManager.TILE_2D_Y_CACHE_SIZE / 2);
        private static Entity[,] tileCache = new Entity[CelesteBotManager.TILE_2D_X_CACHE_SIZE, CelesteBotManager.TILE_2D_Y_CACHE_SIZE];
        private static Entity[,] entityCache = new Entity[CelesteBotManager.TILE_2D_X_CACHE_SIZE, CelesteBotManager.TILE_2D_Y_CACHE_SIZE];

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
            try
            {
                celesteLevel = (Level)Celeste.Celeste.Scene;
                tiles = celesteLevel.SolidTiles;
                tileArray = tiles.Tiles.Tiles.ToArray();
            } catch (NullReferenceException e)
            {
                // level does not exist
            } catch (InvalidCastException e)
            {
                // level does not exist
            }
            TilesOffset = min;
        }
        public static Level GetCelesteLevel()
        {
            if (celesteLevel != null)
            {
                return celesteLevel;
            }
            try
            {
                celesteLevel = (Level)Celeste.Celeste.Scene;
            }
            catch (NullReferenceException e)
            {
                // level does not exist
            }
            catch (InvalidCastException e)
            {
                // level does not exist
            }
            return celesteLevel;
        }
        public static Vector2 GetTileXY(Vector2 realPos)
        {
            int tileW = 8, tileH = 8;
            return new Vector2((int)Math.Floor((realPos.X - TilesOffset.X) / tileW), (int)Math.Floor((realPos.Y - TilesOffset.Y) / tileH));
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
                Vector2 tile = GetTileXY(realPos);
                if (tileArray[(int)tile.X, (int)tile.Y] != null)
                {
                    if (!tileArray[(int)tile.X, (int)tile.Y].Equals("tilesets/scenery"))
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
                
                if (tileArray[(int)tile.X, (int)tile.Y] != null)
                {
                    if (!tileArray[(int)tile.X, (int)tile.Y].Equals("tilesets/scenery"))
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
        public static bool IsSpikeAtTile(Vector2 tile)
        {
            try
            {

                EntityList entities = Celeste.Celeste.Scene.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].CollidePoint(RealFromTile(tile)) && entities[i].Collidable)
                    {
                        try
                        {
                            Spikes s = (Spikes)entities[i];
                            return true;
                        } catch (InvalidCastException e)
                        {
                            // Not a Spike at this tile
                            return false;
                        }
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
        public static bool IsEntityAtTile(Vector2 tile)
        {
            try
            {
                EntityList entities = Celeste.Celeste.Scene.Entities;

                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i].CollidePoint(RealFromTile(tile)) && entities[i].Collidable)
                    {
                        return true;
                    }
                }
                return false;
            } catch (NullReferenceException e)
            {
                return false;
            }
        }
        public static Entity GetEntityAtTile(Vector2 tile)
        {
            if(cachedLevel != celesteLevel)
            {
                cachedLevel = celesteLevel;
                tileCache = new Entity[tileCache.GetLength(0), tileCache.GetLength(1)];
            }

            int xind = (int)(cacheOffset.X + tile.X);
            int yind = (int)(cacheOffset.Y + tile.Y);
            if (xind < 0 || xind >= tileCache.GetLength(0) || yind < 0 || yind >= tileCache.GetLength(1))
            {
                MoveCache((int)tile.X, (int)tile.Y);
                xind = (int)(cacheOffset.X + tile.X);
                yind = (int)(cacheOffset.Y + tile.Y);
            }
            
            if (tileCache[xind, yind] == Entity.Unset)
            {
                EntityList entities = Celeste.Celeste.Scene.Entities;
                Vector2 real = RealFromTile(tile);
                for (int i = 0; i < entities.Count; i++)
                {
                    if (entities[i] is SolidTiles && entities[i].Collidable && entities[i].CollideRect(new Rectangle((int)real.X, (int)real.Y, 8, 8)))
                    {
                        tileCache[xind, yind] = Entity.Tile;
                    }
                }
                if (tileCache[xind, yind] == Entity.Unset)
                {
                    tileCache[xind, yind] = Entity.Air;
                }
            }
            
            if(tileCache[xind, yind] == Entity.Tile)
            {
                //Logger.Log(LogLevel.Debug, "BOT_TEST", cache[xind, yind, 0].ToString() + " at " + xind.ToString() + ", " + yind.ToString() + ", 0");
                return tileCache[xind, yind];
            }
            else if(entityCache[xind, yind] != Entity.Unset)
            {
                //Logger.Log(LogLevel.Debug, "BOT_TEST", cache[xind, yind, 1].ToString() + " at " + xind.ToString() + ", " + yind.ToString() + ", 1");
                return entityCache[xind, yind];
            }
            else
            {
                //Logger.Log(LogLevel.Debug, "BOT_TEST", cache[xind, yind, 0].ToString() + " at " + xind.ToString() + ", " + yind.ToString() + ", 0");
                return tileCache[xind, yind];
            }
        }
        public static void UpdateGrid()
        {
            try
            {
                celesteLevel = (Level)Celeste.Celeste.Scene;
            }
            catch(Exception e)
            {
                return;
            }
            if(tiles != celesteLevel.SolidTiles)
            {
                tiles = celesteLevel.SolidTiles;
                tileArray = tiles.Tiles.Tiles.ToArray();
            }

        }
        public static void CacheEntities()
        {
            if(framesToCacheWipe > 0)
            {
                framesToCacheWipe--;
                return;
            }

            entityCache = new Entity[tileCache.GetLength(0), tileCache.GetLength(1)];

            EntityList entities = Celeste.Celeste.Scene.Entities;
            for (int i = 0; i < entities.Count; i++)
            {
                if(entities[i].Collidable && entities[i].Collider != null && !(entities[i] is SolidTiles) && !(entities[i] is Player))
                {
                    //Logger.Log(LogLevel.Debug, "BOT_TEST", entities[i].GetType().ToString());
                    Entity type;
                    if (!Enum.TryParse<Entity>(entities[i].GetType().ToString().Substring(8), out type)) {
                        type = Entity.Other;
                        Logger.Log(LogLevel.Debug, "celeste-bot", "Entity of type " + entities[i].GetType() + " not handled, add it to the Entity enum in TileFinder.cs");
                    }
                    Rectangle rect = entities[i].Collider.Bounds;
                    //Logger.Log(LogLevel.Debug, "BOT_TEST", rect.Left.ToString() + " " + rect.Right.ToString() + " " + rect.Bottom.ToString() + " " + rect.Top.ToString());
                    int j = rect.Left;
                    if(j % 8 == 0)
                    {
                        j += 1;
                    }
                    for (; j < rect.Right; j += 8)
                    {
                        int k = rect.Top;
                        if (k % 8 == 0)
                        {
                            k += 1;
                        }
                        for (; k < rect.Bottom; k += 8)
                        {
                            //Logger.Log(LogLevel.Debug, "BOT_TEST", j.ToString() + " " + k.ToString());
                            Vector2 tile = GetTileXY(new Vector2(j, k));
                            int xind = (int)(cacheOffset.X + tile.X);
                            int yind = (int)(cacheOffset.Y + tile.Y);
                            if (xind < 0 || xind >= tileCache.GetLength(0) || yind < 0 || yind >= tileCache.GetLength(1))
                            {
                                MoveCache((int)tile.X, (int)tile.Y);
                                xind = (int)(cacheOffset.X + tile.X);
                                yind = (int)(cacheOffset.Y + tile.Y);
                            }
                            entityCache[xind, yind] = type;
                            //Logger.Log(LogLevel.Debug, "BOT_TEST", cache[xind, yind, 1].ToString() + " cached at " + xind.ToString() + ", " + yind.ToString() + ", 1");
                            //Logger.Log(LogLevel.Debug, "BOT_TEST", cache[xind, yind, 0].ToString() + " cached at " + xind.ToString() + ", " + yind.ToString() + ", 0");
                        }
                    }
                }

                framesToCacheWipe = CelesteBotManager.ENTITY_CACHE_UPDATE_FRAMES;
            }
        }
        public static void ScaleCache()
        {
            Entity[,] newTileCache = new Entity[2 * tileCache.GetLength(0), 2 * tileCache.GetLength(1)];
            Entity[,] newEntityCache = new Entity[2 * tileCache.GetLength(0), 2 * tileCache.GetLength(1)];
            if (newTileCache.GetLength(0) > CelesteBotManager.TILE_2D_X_CACHE_SIZE || newTileCache.GetLength(1) > CelesteBotManager.TILE_2D_Y_CACHE_SIZE)
            {
                return;
            }

            int xoff = (int)cacheOffset.X;
            int yoff = (int)cacheOffset.Y;

            for(int i = 0; i < tileCache.GetLength(0); i++)
            {
                for(int j = 0; j < tileCache.GetLength(1); j++)
                {
                    newTileCache[xoff + i, yoff + j] = tileCache[i, j];
                    newEntityCache[xoff + i, yoff + j] = entityCache[i, j];
                }
            }

            tileCache = newTileCache;
            entityCache = newEntityCache;
            cacheOffset *= 2;
        }
        public static void MoveCache(int x, int y)
        {
            Entity[,] newTileCache = new Entity[tileCache.GetLength(0), tileCache.GetLength(1)];
            Entity[,] newEntityCache = new Entity[tileCache.GetLength(0), tileCache.GetLength(1)];

            int xoff = (int)cacheOffset.X;
            int yoff = (int)cacheOffset.Y;
            int newXoff = tileCache.GetLength(0) / 2 - x;
            int newYoff = tileCache.GetLength(1) / 2 - y;
            int shiftx = newXoff - xoff;
            int shifty = newYoff - yoff;

            for (int i = 0; i < tileCache.GetLength(0) && shiftx + i < tileCache.GetLength(0); i++)
            {
                if(shiftx + i < 0)
                {
                    continue;
                }
                for (int j = 0; j < tileCache.GetLength(1) && shifty + j < tileCache.GetLength(1); j++)
                {
                    if (shifty + j < 0)
                    {
                        continue;
                    }

                    newTileCache[shiftx + i, shifty + j] = tileCache[i, j];
                    newEntityCache[shiftx + i, shifty + j] = entityCache[i, j];
                }
            }

            tileCache = newTileCache;
            entityCache = newEntityCache;
            cacheOffset = new Vector2(newXoff, newYoff);
        }
        //public static MTexture[,] GetSplicedTileArray(int visionX, int visionY)
        //{
        //    int underYIndex = visionY / 2 + 1;
        //    int underXIndex = visionX / 2;
        //    ArrayS
        //}
    }
}
