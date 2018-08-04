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
        public static bool ShowNothing = false;

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
            population = new Population(CelesteBotManager.POPULATION_SIZE);
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
        
        public static void MInput_Update(On.Monocle.MInput.orig_Update original)
        {
            if (!Settings.Enabled)
            {
                original();
                return;
            }
            if (CelesteBotManager.CompleteRestart(inputPlayer))
            {
                return;
            }
            if (CelesteBotManager.CheckForCutsceneSkip(inputPlayer))
            {
                return;
            }
            if (CelesteBotManager.CompleteCutsceneSkip(inputPlayer))
            {
                return;
            }// test
            
            InputData temp = new InputData();
            
            // If in cutscene skip state, skip it the rest of the way.
            kbState = Keyboard.GetState();
            
            if (IsKeyDown(Keys.OemBackslash))
            {
                state = State.Running;
            } else if (IsKeyDown(Keys.OemQuotes))
            {
                state = State.Running;
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
                if (!population.Done())
                {
                    if (buffer > 0)
                    {
                        buffer--;
                        original();
                        inputPlayer.UpdateData(temp);
                        return;
                    }
                    // Run the population till they die
                    population.UpdateAlive();
                    CurrentPlayer = population.GetCurrentPlayer();
                    if (CurrentPlayer.Dead && CurrentPlayer.player.InControl && !CurrentPlayer.player.JustRespawned)
                    {
                        temp.QuickRestart = true;
                        buffer = CelesteBotManager.PLAYER_GRACE_BUFFER; // sets the buffer to desired wait time... magic
                        population.CurrentIndex++;
                        if (population.CurrentIndex >= population.Pop.Count)
                        {
                            Logger.Log(CelesteBotInteropModule.ModLogKey, "Population Current Index out of bounds, performing evolution...");
                            inputPlayer.UpdateData(temp);
                            return;
                        }
                        inputPlayer.UpdateData(temp);
                    }
                    original();
                    return;
                } else
                {
                    // Do some checkpointing here maybe
                    population.NaturalSelection();
                }
            }
            inputPlayer.UpdateData(temp);
            original();
        }
        public static void Engine_Update(On.Monocle.Engine.orig_Update original, Engine self, GameTime gameTime)
        {
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
