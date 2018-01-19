using System;

namespace SWBF2Admin.Gameserver
{
    class StartEventArgs : EventArgs
    {
        public bool Attached { get; }
        public StartEventArgs(bool attached)
        {
            Attached = attached;
        }
    }
}