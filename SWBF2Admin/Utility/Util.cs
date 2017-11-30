using System.Text;
using System.Security.Cryptography;

namespace SWBF2Admin.Utility
{
    class Util
    {
        public static byte[] StrToBytes(string str)
        {
            return Encoding.ASCII.GetBytes(str);
        }

        public static string BytesToStr(byte[] buf, int count)
        {
            return Encoding.ASCII.GetString(buf, 0, count);
        }

        public static string Md5(string s)
        {
            byte[] hash = null;
            StringBuilder b = new StringBuilder();
            using (MD5 md5 = MD5.Create()) hash = md5.ComputeHash(Encoding.ASCII.GetBytes(s));
            for (int i = 0; i < hash.Length; i++) b.Append(hash[i].ToString("X2"));
            return b.ToString().ToLower();
        }
    }
}