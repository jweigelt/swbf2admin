using System;
using SWBF2Admin.Structures;

namespace SWBF2Admin.Runtime.Game
{
    public class GameClosedEventArgs : EventArgs
    {
        public GameInfo Game { get; }
        public GameClosedEventArgs(GameInfo game)
        {
            Game = game;
        }
    }
}
