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

        public Assembly CelesteBotDLL;
        public static string CelesteBotDLLPath { get; protected set; }
        public Type Manager;

        public static string ModLogKey = "celeste-bot";

        private static State state = State.None;
        [Flags]
        private enum State
        {
            None = 0,
            Running = 1,
            Disabled = 2
        }
        private static KeyboardState kbState; // For handling the bot enabling/disabling (state changes)

        private static bool IsKeyDown(Keys key)
        {
            return kbState.IsKeyDown(key);
        }

        public CelesteBotInteropModule()
        {
            Instance = this;
        }

        private bool SetupPath()
        {
            CelesteBotDLLPath = Path.Combine(Everest.PathGame, "CelesteBot.dll");
            if (!File.Exists(CelesteBotDLLPath))
            {
                return false;
            }
            return true;
        }

        public override void Load()
        {
            if (!SetupPath())
            {
                Logger.Log(ModLogKey, "CelesteBot.dll not found in game directory. CelesteBot-Everest-Interop not loading.");
            }

            Logger.Log(ModLogKey, "Loading CelesteBot.dll...");
            try
            {
                using (Stream stream = File.OpenRead(CelesteBotDLLPath))
                {
                    MonoModder old = Everest.Relinker.Modder;
                    Everest.Relinker.Modder = null;
                    using (MonoModder modder = Everest.Relinker.Modder)
                    {

                        modder.MethodRewriter += PatchAddonsMethod;
                        Type proxies = typeof(CelesteProxies);
                        foreach (MethodInfo proxy in proxies.GetMethods())
                        {
                            CelesteProxyAttribute attrib = proxy.GetCustomAttribute<CelesteProxyAttribute>();
                            if (attrib == null)
                                continue;
                            modder.RelinkMap[attrib.FindableID] = new RelinkMapEntry(proxies.FullName, proxy.GetFindableID(withType: false));
                        }

                        Logger.Log(ModLogKey, "Created relink map.");

                        CelesteBotDLL = Everest.Relinker.GetRelinkedAssembly(new EverestModuleMetadata
                        {
                            PathDirectory = Everest.PathGame,
                            Name = Metadata.Name,
                            DLL = CelesteBotDLLPath
                        }, stream,
                        checksumsExtra: new string[] {
                            Everest.Relinker.GetChecksum(Metadata)
                        }, prePatch: _ => {
                            Assembly interop = Assembly.GetExecutingAssembly();
                            AssemblyName interopName = interop.GetName();
                            AssemblyNameReference interopRef = new AssemblyNameReference(interopName.Name, interopName.Version);
                            modder.Module.AssemblyReferences.Add(interopRef);
                            string interopPath = Everest.Relinker.GetCachedPath(Metadata);
                            modder.DependencyCache[interopRef.Name] =
                            modder.DependencyCache[interopRef.FullName] = MonoModExt.ReadModule(interopPath, modder.GenReaderParameters(false, interopPath));
                            modder.MapDependency(modder.Module, interopRef);
                        });

                        modder.RelinkModuleMap = new Dictionary<string, ModuleDefinition>();
                        modder.RelinkMap = new Dictionary<string, object>();

                    }
                    Everest.Relinker.Modder = old;
                }
            }
            catch (Exception ex)
            {
                Logger.Log(ModLogKey, "Failed loading new/modified CelesteBot.dll");
                Logger.LogDetailed(ex);
            }
            if (CelesteBotDLL == null)
            {
                Logger.Log(ModLogKey, "Failed loading new/modified CelesteBot.dll (null CelesteBotDLL)");
            }

            Manager = CelesteBotDLL.GetType("CelesteBot.Manager");
            if (Manager == null)
            {
                Logger.Log(ModLogKey, "Failed to load Manager type from CelesteBot.dll!");
            }
            Type selfType = GetType();

            detourUpdateInputs = new Detour(
                typeof(CelesteBotInteropModule).GetMethod("UpdateInputs"),
                Manager.GetMethod("UpdateInputs"));

            detourDraw = new Detour(
                typeof(CelesteBotInteropModule).GetMethod("Draw"),
                Manager.GetMethod("Draw"));

            detourResetText = new Detour(
                typeof(CelesteBotInteropModule).GetMethod("ResetActiveText"),
                Manager.GetMethod("ResetActiveText"));

            On.Monocle.Engine.Update += Engine_Update;
            On.Monocle.MInput.Update += MInput_Update;
            On.Monocle.Engine.Draw += Engine_Draw;

            originalUpdate = (detourGameUpdate = new Detour(
                typeof(Game).GetMethod("Update", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
                typeof(CelesteBotInteropModule).GetMethod("GameUpdate")
                )).GenerateTrampoline<delegateGameUpdate>();

            state = State.Disabled;
        }
        public override void Unload()
        {
            if (CelesteBotDLL == null)
            {
                return;
            }

            detourGameUpdate.Undo();
            detourGameUpdate.Free();
            On.Monocle.Engine.Update -= Engine_Update;
            On.Monocle.MInput.Update -= MInput_Update;
            On.Monocle.Engine.Draw -= Engine_Draw;
            detourGameUpdate.Undo();
            detourGameUpdate.Free();
            Logger.Log(ModLogKey, "Unload successful");
        }

        private TypeDefinition engineTypeDef;
        private MethodDefinition engineGetScene;
        public void PatchAddonsMethod(MonoModder modder, MethodDefinition method)
        {
            if (!method.HasBody)
            {
                return;
            }
            if (engineTypeDef == null)
            {
                engineTypeDef = modder.FindType("Monocle.Engine")?.Resolve();
                if (engineTypeDef == null) return;
            }
            if (engineGetScene == null)
            {
                engineGetScene = engineTypeDef.FindMethod("Monocle.Scene get_Scene()");
                if (engineGetScene == null) return;
            }

            Mono.Collections.Generic.Collection<Instruction> instrs = method.Body.Instructions;
            ILProcessor il = method.Body.GetILProcessor();

            for (int instri = 0; instri < instrs.Count; instri++)
            {
                Instruction instr = instrs[instri];

                // Replace ldfld Engine::scene with ldsfld Engine::Scene.
                if (instr.OpCode == OpCodes.Ldfld && (instr.Operand as FieldReference)?.FullName == "Monocle.Scene Monocle.Engine::scene")
                {

                    // Pop the loaded instance.
                    instrs.Insert(instri, il.Create(OpCodes.Pop));
                    instri++;

                    // Replace the field load with a property getter call.
                    instr.OpCode = OpCodes.Call;
                    instr.Operand = engineGetScene;
                }
            }
        }

        public static Detour detourUpdateInputs;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateInputs()
        {
            // This gets relinked to CelesteBot.Manager.UpdateInputs
            throw new Exception("Failed relinking UpdateInputs!");
        }
        public static Detour detourDraw;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Draw()
        {
            throw new Exception("Failed relinking Engine_Draw!");
        }
        public static Detour detourResetText;

        public static void ResetActiveText(string reset)
        {
            throw new Exception("Failed relinking ResetActiveText!");
        }

        public static void Engine_Draw(On.Monocle.Engine.orig_Draw original, Engine self, GameTime time)
        {
            original(self, time);
            if (state == State.Running || Settings.DrawAlways) {
                Draw();
            }
        }

        public static void Engine_Update(On.Monocle.Engine.orig_Update original, Engine self, GameTime time)
        {
            if (!Settings.Enabled)
            {
                original(self, time);
                return;
            }
            original(self, time); // placeholder
            // Execute the bot here! (if it needs engine updates... who knows?)
        }

        public static void MInput_Update(On.Monocle.MInput.orig_Update original)
        {
            kbState = Keyboard.GetState();
            if (!Settings.Enabled)
            {
                original();
                return;
            }
            if (IsKeyDown(Keys.OemBackslash) || IsKeyDown(Keys.OemQuotes))
            {
                state = State.Running;
            } else
            {
                state = State.Disabled;
                if (!Settings.DrawAlways)
                {
                    ResetActiveText("Vision:\n");
                }
            }
            
            if (state == State.Disabled)
            {
                original();
            }
            
            UpdateInputs();
        }

        public static Detour detourGameUpdate;
        public delegate void delegateGameUpdate(Game self, GameTime time);
        public static delegateGameUpdate originalUpdate;
        public static void GameUpdate(Game self, GameTime time)
        {
            // Do something here if needed?
            originalUpdate(self, time);
        }
    }
}
