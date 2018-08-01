using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    public class InputData
    {
        public float MoveX;
        public float MoveY;
        public Vector2 Aim;
        public Vector2 MountainAim;
        public int Buttons;

        
        ///<summary>
        ///This constructor uses the actions float array to create virtual inputs from the actions.
        ///Refrenced from CelesteBotPlayer
        ///</summary>
        ///<param name="actions"actions array with length 6.</param>
        public InputData(float[] actions)
        {
            MoveX = Math.Abs(actions[0]) > CelesteBotManager.ACTION_THRESHOLD ? actions[0] : 0;
            MoveY = Math.Abs(actions[1]) > CelesteBotManager.ACTION_THRESHOLD ? actions[1] : 0;

            //this.ESC = actions[2] > CelesteBotManager.ACTION_THRESHOLD;
            //this.MenuConfirm = actions[3] > CelesteBotManager.ACTION_THRESHOLD;
            //this.MenuCancel = actions[4] > CelesteBotManager.ACTION_THRESHOLD;
            //this.QuickRestart = actions[5] > CelesteBotManager.ACTION_THRESHOLD;
            this.Jump = actions[2] > CelesteBotManager.ACTION_THRESHOLD;
            this.Dash = actions[3] > CelesteBotManager.ACTION_THRESHOLD;
            this.Grab = actions[4] > CelesteBotManager.ACTION_THRESHOLD;
            this.Talk = actions[5] > CelesteBotManager.ACTION_THRESHOLD;
        }

        public InputData() { }

        public bool ESC
        {
            get
            {
                return (Buttons & (int)ButtonMask.ESC) == (int)ButtonMask.ESC;
            }
            set
            {
                Buttons &= (int)~ButtonMask.ESC;
                if (value)
                    Buttons |= (int)ButtonMask.ESC;
            }
        }
        public bool MenuConfirm
        {
            get
            {
                return (Buttons & (int)ButtonMask.MenuConfirm) == (int)ButtonMask.MenuConfirm;
            }
            set
            {
                Buttons &= (int)~ButtonMask.MenuConfirm;
                if (value)
                    Buttons |= (int)ButtonMask.MenuConfirm;
            }
        }
        public bool MenuCancel
        {
            get
            {
                return (Buttons & (int)ButtonMask.MenuCancel) == (int)ButtonMask.MenuCancel;
            }
            set
            {
                Buttons &= (int)~ButtonMask.MenuCancel;
                if (value)
                    Buttons |= (int)ButtonMask.MenuCancel;
            }
        }
        public bool QuickRestart
        {
            get
            {
                return (Buttons & (int)ButtonMask.QuickRestart) == (int)ButtonMask.QuickRestart;
            }
            set
            {
                Buttons &= (int)~ButtonMask.QuickRestart;
                if (value)
                    Buttons |= (int)ButtonMask.QuickRestart;
            }
        }
        public bool Jump
        {
            get
            {
                return (Buttons & (int)ButtonMask.Jump) == (int)ButtonMask.Jump;
            }
            set
            {
                Buttons &= (int)~ButtonMask.Jump;
                if (value)
                    Buttons |= (int)ButtonMask.Jump;
            }
        }
        public bool Dash
        {
            get
            {
                return (Buttons & (int)ButtonMask.Dash) == (int)ButtonMask.Dash;
            }
            set
            {
                Buttons &= (int)~ButtonMask.Dash;
                if (value)
                    Buttons |= (int)ButtonMask.Dash;
            }
        }
        public bool Grab
        {
            get
            {
                return (Buttons & (int)ButtonMask.Grab) == (int)ButtonMask.Grab;
            }
            set
            {
                Buttons &= (int)~ButtonMask.Grab;
                if (value)
                    Buttons |= (int)ButtonMask.Grab;
            }
        }
        public bool Talk
        {
            get
            {
                return (Buttons & (int)ButtonMask.Talk) == (int)ButtonMask.Talk;
            }
            set
            {
                Buttons &= (int)~ButtonMask.Talk;
                if (value)
                    Buttons |= (int)ButtonMask.Talk;
            }
        }




        [Flags]
        public enum ButtonMask : int
        {
            ESC = 1 << 0,
            QuickRestart = 1 << 2,
            MenuConfirm = 1 << 3,
            MenuCancel = 1 << 4,
            Jump = 1 << 5,
            Dash = 1 << 6,
            Grab = 1 << 7,
            Talk = 1 << 8 // shouldn't really be needed
        }
    }
}
