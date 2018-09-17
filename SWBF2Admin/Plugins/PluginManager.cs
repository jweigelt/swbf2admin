using SWBF2Admin.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace SWBF2Admin.Plugins
{
    public class PluginManager : ComponentBase
    {
        private List<Assembly> pluginAssemblies;
        private List<SWBF2AdminPlugin> pluginObjects;

        const string DIR_PLUGINS = "./plugins";

        public PluginManager(AdminCore core) : base(core)
        {
            pluginAssemblies = new List<Assembly>();
            pluginObjects = new List<SWBF2AdminPlugin>();
        }

        private void loadPlugin(FileInfo f)
        {
            Logger.Log(LogLevel.Verbose, "[PI ] Loading plugin {0}", f.Name);
            try
            {
                AssemblyName an = AssemblyName.GetAssemblyName(f.FullName);
                Assembly assembly = Assembly.Load(an);
                if (assembly != null)
                    pluginAssemblies.Add(assembly);
                else throw new Exception("Assembly not found.");

                Type[] types = assembly.GetTypes();
                foreach (Type t in types)
                {
                    if (!t.IsAbstract && t.IsSubclassOf(typeof(SWBF2AdminPlugin)))
                    {
                        pluginObjects.Add((SWBF2AdminPlugin)Activator.CreateInstance(t, Core));
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Warning, "[PI ] Failed to load plugin \"{0}\" - disabling it ({1})", f.Name, e.Message);
            }
        }

        public override void OnInit()
        {
            if (!Core.Files.DirectoryExists(DIR_PLUGINS))
                Core.Files.CreateDirectoryStructure(DIR_PLUGINS + "/");

            FileInfo[] plugins = Core.Files.GetFiles(DIR_PLUGINS, "*.dll");
            foreach (FileInfo f in plugins)
            {
                //TODO: fix
                if(f.Name.StartsWith("SWBF2Admin")) { 
                    loadPlugin(f);
                }
            }
            base.OnInit();
        }
    }
}