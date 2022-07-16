using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.security
{
    public static class Base64
    {
        public static string Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            var result = System.Convert.ToBase64String(plainTextBytes);

            return result;
        }

        public static string Decode(string base64Text)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64Text);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }
}
