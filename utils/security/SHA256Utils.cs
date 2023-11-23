using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace es.dmoreno.utils.security
{
    public class SHA256Utils
    {
        static public string GetHash(string s)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] data = sha256.ComputeHash(Encoding.UTF8.GetBytes(s));
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < data.Length; i++)
                {
                    sBuilder.Append(data[i].ToString("x2"));
                }
                return sBuilder.ToString();
            }
        }
    }
}
