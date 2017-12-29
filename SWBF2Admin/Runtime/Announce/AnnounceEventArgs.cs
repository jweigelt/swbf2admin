using System;
namespace SWBF2Admin.Runtime.Announce
{
    class AnnounceEventArgs : EventArgs
    {
        /// <summary>
        /// Announce object to be broadcasted
        /// </summary>
        public Announce Announce { get; }
        public AnnounceEventArgs(Announce announce)
        {
            Announce = announce;
        }
    }
}