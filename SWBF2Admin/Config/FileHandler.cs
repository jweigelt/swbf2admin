using SWBF2Admin.Utility;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace SWBF2Admin.Config
{
    class FileHandler
    {
        public string ParseFileName(string fileName)
        {
            return fileName.Replace("./", Directory.GetCurrentDirectory() + "/");
        }
        private void CreateDirectoryStructure(string fileName)
        {
            int idx = 0;
            while ((idx = fileName.IndexOfAny(new char[] { '/', '\\' }, ++idx)) > 0)
            {
                string dir = fileName.Substring(0, idx);
                if (!Directory.Exists(dir))
                {
                    try
                    {
                        Directory.CreateDirectory(dir);
                        Logger.Log(LogLevel.Verbose, Log.FILE_DIRECTORY_CREATE, dir);
                    }
                    catch (Exception e)
                    {
                        Logger.Log(LogLevel.Error, Log.FILE_DIRECTORY_CREATE_ERROR, dir, e.ToString());
                        throw e;
                    }
                }
            }
        }
        private string GetFileName(Type t)
        {
            string fileName = (string)t.GetField("FILE_NAME").GetValue(null);
            fileName = ParseFileName(fileName);
            return fileName;
        }

        private string GetResourceName(Type t)
        {
            return (string)t.GetField("RESOURCE_NAME").GetValue(null);
        }

        private T ReadXmlFile<T>(string fileName = "")
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (fileName.Equals("")) fileName = GetFileName(typeof(T));

            try
            {
                using (StreamReader reader = new StreamReader(fileName))
                {
                    T obj = (T)serializer.Deserialize(reader);
                    Logger.Log(LogLevel.Verbose, Log.FILE_XML_PARSE, fileName, typeof(T).ToString());
                    return obj;
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Log.FILE_XML_PARSE_ERROR, fileName, typeof(T).ToString(), e.ToString());
                throw e;
            }
        }
        private void WriteXmlFile<T>(string fileName, T obj)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (fileName.Equals("")) fileName = GetFileName(typeof(T));

            try
            {
                using (StreamWriter writer = new StreamWriter(fileName))
                {
                    serializer.Serialize(writer, obj);
                    Logger.Log(LogLevel.Verbose, Log.FILE_XML_CREATE, fileName, typeof(T).ToString());
                }
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Log.FILE_XML_CREATE_ERROR, fileName, typeof(T).ToString(), e.ToString());
                throw e;
            }
        }

        private void UnpackResource(string fileName, string resourceName)
        {
            CreateDirectoryStructure(fileName);
            try
            {
                using (Stream stream = Assembly.GetCallingAssembly().GetManifestResourceStream(resourceName))
                {
                    using (FileStream fs = new FileStream(fileName, FileMode.Create))
                    {
                        stream.CopyTo(fs);
                    }
                }
                Logger.Log(LogLevel.Info, Log.FILE_DEFAULT_UNPACK, fileName);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, Log.FILE_DEFAULT_UNPACK_ERROR, fileName, e.ToString());
                throw e;
            }
        }

        public void WriteConfigDefault<T>(string fileName = "")
        {
            if (fileName.Equals("")) fileName = GetFileName(typeof(T));
            CreateDirectoryStructure(fileName);
            WriteXmlFile<T>(fileName, (T)Activator.CreateInstance(typeof(T)));
        }
        public void UnpackConfigDefault<T>()
        {
            UnpackResource(GetFileName(typeof(T)), GetResourceName(typeof(T)));
        }

        public T ReadConfig<T>()
        {
            if (!File.Exists(GetFileName(typeof(T))))
            {
                UnpackConfigDefault<T>();
            }
            return ReadXmlFile<T>();
        }

        public string ReadFileText(string fileName)
        {
            fileName = ParseFileName(fileName);
            return File.ReadAllText(fileName);
        }

        public void WriteFileText(string fileName, string text)
        {
            fileName = ParseFileName(fileName);
            File.WriteAllText(fileName, text);
        }


        public byte[] ReadFileBytes(string fileName)
        {
            fileName = ParseFileName(fileName);
            return File.ReadAllBytes(fileName);
        }


    }
}