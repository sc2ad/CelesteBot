using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class InputNodes
    {
        // May eventually deprecate this class, as can just modify value directly
        public class MoveX : VirtualAxis.Node
        {
            public InputPlayer Player;
            public MoveX(InputPlayer player)
            {
                Player = player;
            }
            public override float Value => Player.Data.MoveX;
        }
        public class MoveY : VirtualAxis.Node
        {
            public InputPlayer Player;
            public MoveY(InputPlayer player)
            {
                Player = player;
            }
            public override float Value => Player.Data.MoveY;
        }
        public class Aim : VirtualJoystick.Node
        {
            public InputPlayer Player;
            public Aim(InputPlayer player)
            {
                Player = player;
            }
            public override Vector2 Value => Player.Data.Aim;
        }
        public class MountainAim : VirtualJoystick.Node
        {
            public InputPlayer Player;
            public MountainAim(InputPlayer player)
            {
                Player = player;
            }
            public override Vector2 Value => Player.Data.MountainAim;
        }
        
        public class Button : VirtualButton.Node
        {
            public InputPlayer Player;
            public int Mask;
            public Button(InputPlayer player, InputData.ButtonMask mask)
            {
                Player = player;
                Mask = (int) mask;
            }
            public override bool Check => !MInput.Disabled && (Player.Data.Buttons & Mask) == Mask;
            public override bool Pressed => !MInput.Disabled && (Player.Data.Buttons & Mask) == Mask && (Player.LastData.Buttons & Mask) == 0;
            public override bool Released => !MInput.Disabled && (Player.Data.Buttons & Mask) == 0 && (Player.LastData.Buttons & Mask) == Mask;
        }
    }
}
