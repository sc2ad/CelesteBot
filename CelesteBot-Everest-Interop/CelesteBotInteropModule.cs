using Celeste.Mod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Logger = Celeste.Mod.Logger;

namespace CelesteBot_Everest_Interop
{
    public class CelesteBotInteropModule : EverestModule
    {
        public static CelesteBotInteropModule Instance;

        public override Type SettingsType => typeof(CelesteBotModuleSettings);
        public static CelesteBotModuleSettings Settings => (CelesteBotModuleSettings)Instance._Settings;

        public static string ModLogKey = "celeste-bot";

        public static Population population;
        public static CelestePlayer CurrentPlayer;

        private static int buffer = 0; // The number of frames to wait when setting a new current player

        public static ArrayList innovationHistory = new ArrayList();

        public static bool DrawPlayer { get { return !ShowNothing && Settings.ShowPlayerBrain; } set { } }
        public static bool DrawFitness { get { return !ShowNothing && Settings.ShowPlayerFitness; } set { } }
        public static bool DrawDetails { get { return !ShowNothing && Settings.ShowDetailedPlayerInfo; } set { } }
        public static bool DrawBestFitness { get { return !ShowNothing && Settings.ShowBestFitness; } set { } }
        public static bool DrawGraph { get { return !ShowNothing && Settings.ShowGraph; } set { } }
        public static bool ShowNothing = false;

        public static bool ShowBest = false;
        public static bool RunBest = false;
        public static bool RunThroughSpecies = false;
        public static int UpToSpecies = 0;
        public static bool ShowBestEachGen = false;
        public static int UpToGen = 0;

        public static CelestePlayer SpeciesChamp;
        public static CelestePlayer GenPlayerTemp;

        private static State state = State.None;
        [Flags]
        private enum State
        {
            None = 0,
            Running = 1,
            Disabled = 2
        }
        private static KeyboardState kbState; // For handling the bot enabling/disabling (state changes)
        public static InputPlayer inputPlayer;

        private static bool IsKeyDown(Keys key)
        {
            return kbState.IsKeyDown(key);
        }

        public CelesteBotInteropModule()
        {
            Instance = this;
        }

        public override void Load()
        {
            On.Monocle.Engine.Draw += Engine_Draw;
            On.Monocle.Engine.Update += Engine_Update;
            On.Monocle.MInput.Update += MInput_Update;
            On.Celeste.Celeste.OnSceneTransition += OnScene_Transition;

            Logger.Log(ModLogKey, "Load successful");
        }
        public override void Initialize()
        {
            base.Initialize();

            // Hey, InputPlayer should be made to work without removing self when players die
            inputPlayer = new InputPlayer(Celeste.Celeste.Instance, new InputData()); // Blank InputData when constructing. Overwrite it when needing to update inputs
            Celeste.Celeste.Instance.Components.Add(inputPlayer);
            CelesteBotManager.FillOrganismHash(CelesteBotManager.ORGANISM_PATH);
            CelesteBotManager.FillSpeciesHash(CelesteBotManager.SPECIES_PATH);
            population = new Population(Settings.OrganismsPerGeneration); // Requires Restart
            //GeneratePlayer();
            CurrentPlayer = population.GetCurrentPlayer();
            
        }
        //public static void GeneratePlayer()
        //{
        //    CurrentPlayer = new CelestePlayer();
        //    CurrentPlayer.Brain.GenerateNetwork();
        //    CurrentPlayer.Brain.Mutate(innovationHistory);
        //}
        public override void Unload()
        {
            On.Monocle.Engine.Draw -= Engine_Draw;
            On.Monocle.Engine.Update -= Engine_Update;
            On.Monocle.MInput.Update -= MInput_Update;
            On.Celeste.Celeste.OnSceneTransition -= OnScene_Transition;
            Logger.Log(ModLogKey, "Unload successful");
        }

        public static void Engine_Draw(On.Monocle.Engine.orig_Draw original, Engine self, GameTime time)
        {
            original(self, time);
            if (state == State.Running || Settings.DrawAlways) {
                CelesteBotManager.Draw();
            }
        }

        private static void Reset(InputData temp)
        {
            temp.QuickRestart = true;
            buffer = CelesteBotManager.PLAYER_GRACE_BUFFER; // sets the buffer to desired wait time... magic
            inputPlayer.UpdateData(temp);
        }
        
        public static void MInput_Update(On.Monocle.MInput.orig_Update original)
        {
            if (!Settings.Enabled)
            {
                original();
                return;
            }
            try
            {
                Celeste.Player player = Celeste.Celeste.Scene.Tracker.GetEntity<Celeste.Player>();
                foreach (Entity e in Celeste.Celeste.Scene.Entities) {
                    Logger.Log(ModLogKey, e.Tag + " tag with: " + e.Center + " center");
                    if (e.TagCheck(4))
                    {
                        // This is (in theory) something that can be interacted with/talked to.
                        if (e.CollideCheck(player))
                        {
                            // If the player is touching it, we can pretend to talk (maybe)
                            InputData data = new InputData();
                            data.Talk = true;
                            inputPlayer.UpdateData(data);
                            Logger.Log(ModLogKey, "Attempted to talk to something!");
                            return;
                        }
                    }
                }
                if (player.StateMachine.State == 11 && !player.InControl && !player.OnGround() && inputPlayer.LastData.Dash != true) // this makes sure we retry
                {
                    // This means we are in the bird tutorial.
                    // Make us finish it right away.
                    InputData data = new InputData();
                    data.MoveX = 1;
                    data.MoveY = -1;
                    data.Dash = true;
                    inputPlayer.UpdateData(data);
                    Logger.Log(ModLogKey, "The player is in the dash cutscene, so we tried to get them out of it by dashing.");
                    return;
                }
                
            } catch (NullReferenceException e)
            {
                // level doesn't exist yet
            } catch (InvalidCastException e)
            {
                // still doesn't exist
            }
            if (CelesteBotManager.CompleteRestart(inputPlayer))
            {
                return;
            }
            // If in cutscene skip state, skip it the rest of the way.
            if (CelesteBotManager.CheckForCutsceneSkip(inputPlayer))
            {
                return;
            }
            if (CelesteBotManager.CompleteCutsceneSkip(inputPlayer))
            {
                return;
            }
            
            InputData temp = new InputData();
            
            
            kbState = Keyboard.GetState();

            if (IsKeyDown(Keys.Space))
            {
                ShowBest = !ShowBest;
            } else if (IsKeyDown(Keys.B))
            {
                RunBest = !RunBest;
                Reset(temp);
                return;
            } else if (IsKeyDown(Keys.S))
            {
                RunThroughSpecies = !RunThroughSpecies;
                UpToSpecies = 0;
                Species s = (Species)population.Species[0];
                CelestePlayer p = (CelestePlayer)s.Champ;
                SpeciesChamp = p.CloneForReplay();
                Reset(temp);
                return;
            } else if (IsKeyDown(Keys.G))
            {
                ShowBestEachGen = !ShowBestEachGen;
                UpToGen = 0;
                CelestePlayer p = (CelestePlayer)population.GenPlayers[0];
                GenPlayerTemp = p.CloneForReplay();
                Reset(temp);
                return;
            } else if (IsKeyDown(Keys.OemBackslash))
            {
                state = State.Running;
            } else if (IsKeyDown(Keys.OemQuotes))
            {
                Population test = Util.DeSerializeObject(CelesteBotManager.CHECKPOINT_FILE_PREFIX + "_" + Convert.ToString(Settings.CheckpointToLoad) + ".ckp");
                if (test != null) {
                    population = test;
                }
                Logger.Log(ModLogKey, "Loaded Population from: " + CelesteBotManager.CHECKPOINT_FILE_PREFIX + "_" + Convert.ToString(Settings.CheckpointToLoad) + ".ckp");
                Reset(temp);
                return;
            } else if (IsKeyDown(Keys.OemPeriod))
            {
                state = State.Disabled;
                temp.QuickRestart = true;
            } else if (IsKeyDown(Keys.OemComma))
            {
                state = State.Disabled;
                temp.ESC = true;
            } else if (IsKeyDown(Keys.OemQuestion))
            {
                state = State.Disabled;
                //GeneratePlayer();
            } else if (IsKeyDown(Keys.N))
            {
                ShowNothing = !ShowNothing;
            }
            if (state == State.Running)
            {
                if (buffer > 0)
                {
                    buffer--;
                    original();
                    inputPlayer.UpdateData(temp);
                    return;
                }
                if (ShowBestEachGen)
                {
                    if (!GenPlayerTemp.Dead)
                    {
                        CurrentPlayer = GenPlayerTemp;
                        GenPlayerTemp.Update();
                        if (GenPlayerTemp.Dead)
                        {
                            Reset(temp);
                            UpToGen++;
                            if (UpToGen >= population.GenPlayers.Count)
                            {
                                UpToGen = 0;
                                ShowBestEachGen = false;
                            }
                            else
                            {
                                GenPlayerTemp = (CelestePlayer)population.GenPlayers[UpToGen];
                            }
                        }
                    }
                    else
                    {
                        Reset(temp);
                        UpToGen = 0;
                        ShowBestEachGen = false;
                    }
                    original();
                    return;
                }
                else if (RunThroughSpecies)
                {
                    if (!SpeciesChamp.Dead)
                    {
                        CurrentPlayer = SpeciesChamp;
                        SpeciesChamp.Update();
                        if (SpeciesChamp.Dead)
                        {
                            Reset(temp);
                            UpToSpecies++;
                            if (UpToSpecies >= population.Species.Count)
                            {
                                UpToSpecies = 0;
                                RunThroughSpecies = false;
                            }
                            else
                            {
                                Species s = (Species)population.Species[UpToSpecies];
                                SpeciesChamp = s.Champ.CloneForReplay();
                            }
                        }
                    }
                    else
                    {
                        Reset(temp);
                        UpToSpecies = 0;
                        RunThroughSpecies = false;
                    }
                    original();
                    return;
                }
                else if (RunBest)
                {
                    if (!population.BestPlayer.Dead)
                    {
                        CurrentPlayer = population.BestPlayer;
                        population.BestPlayer.Update();
                        if (population.BestPlayer.Dead)
                        {
                            Reset(temp);
                            RunBest = false;
                            population.BestPlayer = population.BestPlayer.CloneForReplay();
                        }
                    }
                    else
                    {
                        Reset(temp);
                        RunBest = false;
                        population.BestPlayer = population.BestPlayer.CloneForReplay();
                    }
                    original();
                    return;
                }
                else
                {
                    if (!population.Done())
                    {

                        // Run the population till they die
                        population.UpdateAlive();
                        CurrentPlayer = population.GetCurrentPlayer();
                        if (CurrentPlayer.Dead)
                        {
                            temp.QuickRestart = true;
                            buffer = CelesteBotManager.PLAYER_GRACE_BUFFER; // sets the buffer to desired wait time... magic
                            if (CurrentPlayer.Fitness > population.BestFitness)
                            {
                                population.BestFitness = CurrentPlayer.Fitness;
                                population.BestPlayer = CurrentPlayer.CloneForReplay();
                            }
                            population.CurrentIndex++;
                            if (population.CurrentIndex >= population.Pop.Count)
                            {
                                Logger.Log(CelesteBotInteropModule.ModLogKey, "Population Current Index out of bounds, performing evolution...");
                                //inputPlayer.UpdateData(temp);
                                //original();
                                //return;
                            }
                            inputPlayer.UpdateData(temp);
                        }
                        original();
                        return;
                    }
                    else
                    {
                        // Do some checkpointing here maybe

                        if (population.Gen % Settings.CheckpointInterval == 0)
                        {
                            // Time to checkpoint!
                            // Lets save the population as-is into a binary file.
                            Directory.CreateDirectory(CelesteBotManager.CHECKPOINT_FILE_PATH);
                            Util.SerializeObject(population, CelesteBotManager.CHECKPOINT_FILE_PREFIX + "_" + population.Gen + ".ckp");
                            Logger.Log(ModLogKey, "Saved Population to: " + CelesteBotManager.CHECKPOINT_FILE_PREFIX + "_" + population.Gen + ".ckp");
                        }

                        float bFit = 0;
                        // Gets best fitness without looking at first (previous best) organism
                        for (int i = 1; i < population.Pop.Count; i++)
                        {
                            CelestePlayer p = (CelestePlayer)population.Pop[i];
                            if (p.GetFitness() > bFit)
                            {
                                bFit = p.GetFitness();
                            }
                        }
                        CelesteBotManager.SavedBestFitnesses.Add(bFit);
                        if (CelesteBotManager.SavedBestFitnesses.Count > Settings.GenerationsToSaveForGraph)
                        {
                            CelesteBotManager.SavedBestFitnesses.RemoveAt(0);
                        }
                        population.NaturalSelection();
                        
                    }
                }
            }
            inputPlayer.UpdateData(temp);
            original();
        }
        public static void Engine_Update(On.Monocle.Engine.orig_Update original, Engine self, GameTime gameTime)
        {
            //try
            //{
            //    if (CurrentPlayer.player.Dead)
            //    {
            //        CurrentPlayer.Dead = true;
            //        InputData data = new InputData();
            //        data.QuickRestart = true;
            //        inputPlayer.UpdateData(data);
            //    }
            //} catch (NullReferenceException e)
            //{
            //    // Player has not been setup yet
            //}
            original(self, gameTime);
        }
        public static void OnScene_Transition(On.Celeste.Celeste.orig_OnSceneTransition original, Celeste.Celeste self, Scene last, Scene next)
        {
            original(self, last, next);
            CurrentPlayer.SetupVision();
            TileFinder.GetAllEntities();
        }
    }
}
