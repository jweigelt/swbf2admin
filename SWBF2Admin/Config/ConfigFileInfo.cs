using System;
namespace SWBF2Admin.Config
{
    [AttributeUsage(System.AttributeTargets.Class)]
    class ConfigFileInfo : Attribute
    {
        public virtual bool HasTemplate { get { return (Template == null); } }
        public string FileName { get; set; }
        public string Template { get; set; }

        public ConfigFileInfo(string fileName, string template)
        {
            FileName = fileName;
            Template = template;
        }

        public ConfigFileInfo(string fileName)
        {
            FileName = fileName;
            Template = null;
        }

    }
}
