using System;
using System.Collections.Generic;
using System.Text;

namespace es.dmoreno.utils.security
{
    public class Token
    {
        static public string generate(ConfigToken c)
        {
            int min = 0;
            int max = ConfigToken.Characters.Length;

            if (!c.Letters)
            {
                max = 10;
            }

            if (!c.Numbers)
            {
                min = 11;
            }

            int length = c.Length;

            if (length <= 0)
            {
                length = 20;
            }

            var r = new Random();
            string token = "";
            for (int i = 0; i < length; i++)
            {
                token += ConfigToken.Characters[r.Next(min, max)];
            }

            if (c.UpperCase)
            {
                token = token.ToUpper();
            }

            return token;
        }

        static public int getRandomNumber()
        {
            var r = new Random(DateTime.Now.Millisecond);
            return r.Next();
        }

        static public string getRandomID(bool with_brackets = true)
        {
            string id = getRandomNumber().ToString();

            if (with_brackets)
            {
                id = "[" + id + "]";
            }

            return id;
        }
    }
}
