using System;
namespace SWBF2Admin.Structures.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property)]

    class ConfigSection : Attribute
    {
        public const int GENERAL = 1 << 0;
        public const int GENERAL_KEEPDEFAULT = 1 << 1;
        public const int GAME = 1 << 2;
        public const int MAPS = 1 << 3;

        public int Type { get; set; }
        private bool canUpdate;
        private bool needsReload;

        public ConfigSection(int type, bool canUpdate, bool needsReload)
        {
            Type = type;
            this.canUpdate = canUpdate;
            this.needsReload = needsReload;
        }
        public ConfigSection(int type)
        {
            Type = type;
            canUpdate = needsReload = false;
        }
    }
}