using Celeste;
using Celeste.Mod;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class InputPlayer : GameComponent
    {
        // Each Player class also contains an InputPlayer, which plays their input
        // Might change this to have only one...

        public InputData Data;
        public InputData LastData = new InputData();

        public InputPlayer(Game game, InputData Data) : base(game)
        {
            this.Data = Data;
            Everest.Events.Input.OnInitialize += HookInput;
            HookInput();
        }
        public void UpdateData(InputData newData)
        {
            LastData = Data;
            Data = newData;
        }
        public void HookInput()
        {
            Input.MoveX.Nodes.Add(new InputNodes.MoveX(this));
            Input.MoveY.Nodes.Add(new InputNodes.MoveY(this));
            Input.Aim.Nodes.Add(new InputNodes.Aim(this));
            Input.MountainAim.Nodes.Add(new InputNodes.MountainAim(this));

            Input.ESC.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.ESC));
            Input.QuickRestart.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.QuickRestart));
            Input.MenuConfirm.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.MenuConfirm));
            Input.MenuCancel.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.MenuCancel));
            Input.Jump.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.Jump));
            Input.Dash.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.Dash));
            Input.Grab.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.Grab));
            Input.Talk.Nodes.Add(new InputNodes.Button(this, InputData.ButtonMask.Talk));

            Logger.Log(CelesteBotInteropModule.ModLogKey, "Hooked Input with Nodes!");
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            Player p = Celeste.Celeste.Scene.Tracker.GetEntity<Player>();
            if (p.Dead || p == null) // Not sure if this works if quickRestarting
            {
                Logger.Log(CelesteBotInteropModule.ModLogKey, "Unloading Input because Player was either null or dead!");
                // Player is dead, let's remove for the next player
                Remove();
            }
        }
        public void Remove()
        {
            Everest.Events.Input.OnInitialize -= HookInput;
            Input.Initialize();
            Logger.Log(CelesteBotInteropModule.ModLogKey, "Unloading Input, (hopefully) returning input to player...");
            Game.Components.Remove(this);
        }
    }
}
