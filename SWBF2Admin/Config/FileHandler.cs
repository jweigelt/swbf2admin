/*
 * This file is part of SWBF2Admin (https://github.com/jweigelt/swbf2admin). 
 * Copyright(C) 2017, 2018  Jan Weigelt <jan@lekeks.de>
 *
 * SWBF2Admin is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.

 * SWBF2Admin is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
 * GNU General Public License for more details.

 * You should have received a copy of the GNU General Public License
 * along with SWBF2Admin. If not, see<http://www.gnu.org/licenses/>.
 */
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
    public class FileHandler
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
        public void CreateDirectoryStructure(string fileName)
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
        /// Deserializes a XML file and returns the object
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        private T ReadXmlFile<T>(string fileName = "")
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (fileName.Equals("")) fileName = GetFileInfo<T>().FileName;

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
        /// <para>If no filename is specified, the filename will be read from a ConfigFileInfo Attribute</para>
        /// <para>If ConfigFileInfo also provides a template path, the template will be unpacked and written to disk.</para>
        /// </summary>
        /// <param name="fileName">relative or absolute path</param>
        /// <param name="obj">object to be serialized</param>
        private void WriteXmlFile<T>(T obj, string fileName)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(T));
            if (fileName.Equals("")) fileName = GetFileInfo<T>().FileName;

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
            if (fileName == "") fileName = GetFileInfo<T>().FileName;
            CreateDirectoryStructure(fileName);
            WriteXmlFile<T>((T)Activator.CreateInstance(typeof(T)), fileName);
        }

        /// <summary>
        /// Unpacks the resource specified in ConfigFileInfo
        /// </summary>
        public void UnpackConfigDefault<T>()
        {
            ConfigFileInfo info = GetFileInfo<T>();
            if (info.HasTemplate)
                UnpackResource(info.FileName, info.Template);
            else
                WriteConfigDefault<T>(info.FileName);
        }

        /// <summary>
        /// Reads a config file from disk, deserializes it and returns the object
        /// <para>If the file doesn't exist yet, a default configuration will be written and read afterwards.</para>
        /// <para>
        /// A template can be defined in the given T. If not defined, a default configuration will be created using the
        /// T's standard constructor
        /// </para>
        /// </summary>
        public T ReadConfig<T>(string fileName = "", string template = "")
        {
            if (fileName == "")
            {
                if (!File.Exists(GetFileInfo<T>().FileName))
                    UnpackConfigDefault<T>();
            }
            else
            {
                if (!File.Exists(fileName))
                {
                    if (template == "")
                    {
                        WriteConfigDefault<T>(fileName);
                    }
                    else
                    {
                        UnpackResource(fileName, template);
                    }
                }
            }
            return ReadXmlFile<T>(fileName);
        }

        /// <summary>
        /// Gets ConfigFileInfo object from type
        /// </summary>
        private ConfigFileInfo GetFileInfo<T>()
        {
            ConfigFileInfo[] info = (ConfigFileInfo[])typeof(T).GetCustomAttributes(typeof(ConfigFileInfo), false);
            if (info.Length > 0) return info[0];
            throw new Exception("No [ConfigFileInfo] attribute specified and no filename given.");
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
            if(!File.Exists(fileName)) CreateDirectoryStructure(fileName);
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

        public DirectoryInfo[] GetDirectories(string fileName)
        {
            fileName = ParseFileName(fileName);
            string[] dirs = Directory.GetDirectories(fileName);
            DirectoryInfo[] res = new DirectoryInfo[dirs.Length];
            for (int i = 0; i < dirs.Length; i++) res[i] = new DirectoryInfo(dirs[i]);
            return res;
        }

        public FileInfo[] GetFiles(string fileName, string exp = "*")
        {
            fileName = ParseFileName(fileName);
            string[] files = Directory.GetFiles(fileName, exp);
            FileInfo[] res = new FileInfo[files.Length];
            for (int i = 0; i < files.Length; i++) res[i] = new FileInfo(files[i]);
            return res;
        }


        public FileStream OpenStream(string fileName)
        {

            fileName = ParseFileName(fileName);
            try
            {
                return File.Open(fileName, FileMode.Open);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed to open stream \"{0}\" ({1})", fileName, e.Message);
                throw e;
            }
        }

        public void WriteBytes(FileStream stream, long address, byte[] buf, int offset = 0, int len = -1)
        {
            if (len < 0) len = buf.Length;
            try
            {
                stream.Position = address;
                stream.Write(buf, offset, len);
            }
            catch (Exception e)
            {
                Logger.Log(LogLevel.Error, "Failed write to {0} ({1}) \"{2}\"", address.ToString(), stream.Name, e.Message);
                throw e;
            }
        }

        public bool FileExists(string fileName)
        {
            fileName = ParseFileName(fileName);
            return File.Exists(fileName);
        }

        public bool DirectoryExists(string fileName)
        {
            fileName = ParseFileName(fileName);
            return Directory.Exists(fileName);
        }
    }
}