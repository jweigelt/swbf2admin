using System.Xml.Serialization;
using SWBF2Admin.Utility;
namespace SWBF2Admin.Runtime.Announce
{
    public class Announce
    {
        /// <summary>
        /// Enables dynamic variable parsing
        /// </summary>
        [XmlAttribute]
        public bool EnableParser { get; set; }

        /// <summary>
        /// Message text which will be broadcasted
        /// </summary>
        [XmlAttribute]
        public string Message { get; set; }

        public string ParseMessage(AdminCore core)
        {
            if (!EnableParser) return Message;
            else
            {
                return Util.ParseVars(Message, core);
            }
        }
    }
}