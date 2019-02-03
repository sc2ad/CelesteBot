using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CelesteBot_Everest_Interop
{
    /// <summary>
    /// This class is used to represent various states of the player/game.
    /// These QStates can be added/updated in the QTable in order to create a functioning Q-Learning AI.
    /// </summary>
    public class QState
    {
        public CelestePlayer player;
        /// <summary>
        /// Constructs a state using the player.
        /// </summary>
        /// <param name="player">The player that the state should be created from</param>
        public QState(CelestePlayer player)
        {
            this.player = player;
        }
        // Compares their visions
        public override bool Equals(object obj)
        {
            try
            {
                return player.Vision == ((QState)obj).player.Vision;
            } catch (InvalidCastException e)
            {
                // Oh well...
            }
            return base.Equals(obj);
        }
    }
}
