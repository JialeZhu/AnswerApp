using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    class WordUtils
    {
        public static string words2string(JToken text)
        {
            string res = "";
            JArray words = text["words"] as JArray;
            foreach (var token in words)
            {
                res += token["text"].ToString();
            }

            return res;
        }

        public static string getQutoedStringArr(string question)
        {
            int head = 0;
            int tail = 0;
            for (int i = 0; i < question.Length; i++)
            {
                if (isQuotes(question[i]))
                {
                    head = i;
                    break;
                }
            }

            for (int i = question.Length - 1; i > 0; i--)
            {
                if (isQuotes(question[i]))
                {
                    tail = i;
                    break;
                }
            }

            return question.Substring(head + 1, tail - head - 1);
        }

        public static bool isQuotes(char x)
        {
            return x == '\"' || x == '\'' || x == '‘' || x == '“' || x == '”' || x == '’';
        }
    }
}
