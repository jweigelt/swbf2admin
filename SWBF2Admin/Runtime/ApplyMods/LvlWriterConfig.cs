using System.Collections.Generic;
using SWBF2Admin.Config;
namespace SWBF2Admin.Runtime.ApplyMods
{
    [ConfigFileInfo(fileName: "./cfg/mods.xml", template: "SWBF2Admin.Resources.cfg.mods.xml")]
    public  class LvlWriterConfig
    {
        public List<LvlMod> Mods { get; set; } = new List<LvlMod>();
        public string LvlDir { get; set; } = "/data/_lvl_pc";
    }
}
