using System;
namespace SWBF2Admin.Runtime.Announce
{
    class AnnounceEventArgs : EventArgs
    {
        private Announce announce;
        public AnnounceEventArgs(Announce announce)
        {
            this.announce = announce;
        }

        public string GetMessage()
        {
            if (!announce.EnableParser)
            {
                return announce.Message;
            }
            else
            {
                //TODO: parse stuff
                return announce.Message;
            }
        }
    }
}