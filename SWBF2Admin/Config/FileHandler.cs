using SWBF2Admin.Utility;
using System;
using System.IO;
using System.Xml.Serialization;
using System.Reflection;

namespace SWBF2Admin.Config
{
    /// <summary>
    /// class handling file I/O
    /// </summary>
    class FileHandler
    {
        /// <summary>
        /// Resolves relative filename and returns full path
        /// Absolute paths remain unaffected
        /// </summary>
        /// <param name="fileName">reltive path</param>
        public string ParseFileName(string fileName)
        {
            return fileName.Replace("./", Directory.GetCurrentDirectory() + "/");
        }

        /// <summary>
        /// Takes a file path and recursively creates a directory structure
        /// if it doesn't exist yet
        /// </summary>
        /// <param name="fileName">(absolute) path to file</param>
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

        /// <summary>
        /// Gets the FILE_NAME constant from a given class
        /// </summary>
        /// <param name="t">class type</param>
        private string GetFileName(Type t)
        {
            string fileName = (string)t.GetField("FILE_NAME").GetValue(null);
            fileName = ParseFileName(fileName);
            return fileName;
        }

        /// <summary>
        /// Gets the RESOURCE_NAME constant from a given class,
        /// returns string.Empty if RESOURCE_NAME is not defined 
        /// </summary>
        /// <param name="t">class type</param>
        private string GetResourceName(Type t)
        {
            FieldInfo fi = t.GetField("RESOURCE_NAME");
            return (fi == null ? string.Empty : (string)fi.GetValue(null));
        }

        /// <summary>
        /// Deserializes a XML file and returns the object
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
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

        /// <summary>
        /// Serializes an object and saves it to a XML-file
        /// <para>If no filename is specified, the FILE_NAME constant will be read from the given type.</para>
        /// <para>If a resource path is specified using the RESOURCE_NAME constant, the matching resource will be unpacked.</para>
        /// <para>If RESOURCE_NAME is not defined, a standard object will be generated using the type's default constuctor.</para>
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        /// <param name="obj">object to be serialized</param>
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

        /// <summary>
        /// Unpacks a resource from the binary and writes it to disk
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        /// <param name="resourceName">(absolute) resource path</param>
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

        /// <summary>
        /// Generates a object using the T's default constructor, serializes to XML
        /// and writes it to disk
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        public void WriteConfigDefault<T>(string fileName = "")
        {
            if (fileName.Equals("")) fileName = GetFileName(typeof(T));
            CreateDirectoryStructure(fileName);
            WriteXmlFile<T>(fileName, (T)Activator.CreateInstance(typeof(T)));
        }

        /// <summary>
        /// Unpacks the resource specified in RESOURCE_NAME and writes it to FILE_NAME 
        /// </summary>
        public void UnpackConfigDefault<T>()
        {
            string res = GetResourceName(typeof(T));
            string file = GetFileName(typeof(T));
            if (res == string.Empty)
                WriteConfigDefault<T>(file);
            else
                UnpackResource(file, res);
        }

        /// <summary>
        /// Reads a config file from disk, deserializes it and returns the object
        /// FILE_NAME has to be defined in the given T
        /// <para>If the file doesn't exist yet, a default configuration will be written and read afterwards.</para>
        /// <para>
        /// RESOURCE_NAME can be defined in the given T, if not defined, a default configuration will be created using the
        /// T's standard constructor
        /// </para>
        /// </summary>
        public T ReadConfig<T>()
        {
            if (!File.Exists(GetFileName(typeof(T))))
            {
                UnpackConfigDefault<T>();
            }
            return ReadXmlFile<T>();
        }

        /// <summary>
        /// Reads a file from disk and returns it as a string
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        public string ReadFileText(string fileName)
        {
            fileName = ParseFileName(fileName);
            return File.ReadAllText(fileName);
        }

        /// <summary>
        /// Writes a string to disk.
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        public void WriteFileText(string fileName, string text)
        {
            fileName = ParseFileName(fileName);
            File.WriteAllText(fileName, text);
        }

        /// <summary>
        /// Reads a file from disk and returns it as a byte-array
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        public byte[] ReadFileBytes(string fileName)
        {
            fileName = ParseFileName(fileName);
            return File.ReadAllBytes(fileName);
        }
    }
}