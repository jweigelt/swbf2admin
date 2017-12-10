using System;
namespace SWBF2Admin.Runtime.Announce
{
    class AnnounceEventArgs : EventArgs
    {
        /// <summary>Announce object to be broadcasted</summary>
        private Announce announce;
        public AnnounceEventArgs(Announce announce)
        {
            this.announce = announce;
        }

        /// <summary>Gets the announce-message. Variables are parsed if required.</summary>
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