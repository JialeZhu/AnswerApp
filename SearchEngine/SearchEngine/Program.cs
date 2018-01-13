//#define DEBUG

using System;
using System.Drawing;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SearchEngine
{

    class Program
    {
        const string workspace = @"D:\Workspace\AnswerApp\";

        /// <summary>
        /// The number of choices in each question
        /// </summary>
        static int choices = 3;
        
        static int gameCode = 0; // Which game? 0 - 冲顶大会 1 - 芝士超人 2 - 百万英雄 3 - zhihu
        
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
#if DEBUG
            gameCode = 1;
            string imageFilePath = @"D:\Workspace\AnswerApp\screenshot13-13.png";
#else
            gameCode = int.Parse(args[1]);
            string imageFilePath = args[0];
#endif
            
            string question = string.Empty;
            string[] option;
            
            if (gameCode == 3) // zhihu
            {
                ZhihuImageProcess(imageFilePath, out question, out option);
            }
            else
            {
                Basic3ChoicesImageProcess(imageFilePath, out question, out option);
            }

            Console.WriteLine("Question: {0}", question);
            Console.WriteLine("A: {0}", option[0]);
            Console.WriteLine("B: {0}", option[1]);
            Console.WriteLine("C: {0}", option[2]);
            if (gameCode == 3) Console.WriteLine("D: {0}", option[3]);

            bool noflag = false;
            if (question.Contains("不"))
            {
                noflag = true;
                question = question.Replace("不", "");
                Console.WriteLine("不字反转启动");
            }

            //string keyword = getKeyword(question);
            //Console.WriteLine("Got keyword {0}", keyword);
            double[] rate = new double[choices];

            var searcher = new BingSearch();

            // show search result
            if (args.Length >= 3)
            {
                var count = searcher.SearchCounterAsync(question, true).Result;
            }

            var numerators = new Task<double>[choices];
            var denominators = new Task<double>[choices];

            for (int i = 0; i < choices; i++)
            {
                numerators[i] = searcher.SearchCounterAsync(question + " " + "\"" + option[i] + "\"");
                denominators[i] = searcher.SearchCounterAsync("\"" + option[i] + "\"");
            }

            for (int i = 0; i < choices; i++)
            {
                double a = numerators[i].Result;
                double b = denominators[i].Result;
                
                if (b < 1)
                {
                    b = 100000000000;
                }
                
                rate[i] = a / b;
                Console.WriteLine("Rating for choice {0}: {1} / {2} = {3}", i + 1, a, b, rate[i]);
            }

            int ans = noflag ? Utils.GetSmallestIndex(rate) : Utils.GetBiggestIndex(rate);
            Console.WriteLine("推荐答案 {0}: {1}", Convert.ToChar('A' + ans), option[ans]);
            return ans;
        }

        static void Basic3ChoicesImageProcess(string imageFilePath, out string question, out string[] option)
        {
            option = new string[10];
            choices = 3;

            string questionPic = PicUtils.SplitPic(imageFilePath, gameCode, workspace);
            // Execute the REST API call.
            Console.WriteLine("OCR ...");

            var ocrTool = new BingOCR();
            string jsonString = ocrTool.MakeOCRRequestAsync(questionPic).Result;
            Console.WriteLine("OCR finished");
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
            JArray text = jo["regions"][0]["lines"] as JArray;

            int lines = text.Count;
            string allText = "";
            for (int i = 0; i < lines; i++)
            {
                allText += WordUtils.words2string(text[i]);
            }

            int questionMarkIndex = Math.Max(allText.IndexOf("？"), allText.IndexOf("?"));
            question = allText.Substring(0, questionMarkIndex);
            // cut off the question number
            while (question[0] >= '0' && question[0] <= '9')
            {
                question = question.Substring(1, question.Length - 1);
            }
            
            for (int i = 0; i < choices; i++)
            {
                option[i] = WordUtils.words2string(text[lines - choices + i]);
            }
        }

        static void ZhihuImageProcess(string imageFilePath, out string question, out string[] option)
        {
            option = new string[10];
            choices = 4;

            Bitmap pic = (Bitmap)Image.FromFile(imageFilePath);
            var questionPic = pic.Clone(
            new Rectangle(50, 550, 950, 300),
            pic.PixelFormat
            );

            var picUtil = new PicUtils();

            string questionPath = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
            questionPic.Save(questionPath, pic.RawFormat);
            var questionAsync = picUtil.getAllStringInPicAsync(questionPath);

            var optionAsync = new Task<string>[choices];

            for (int i = 0; i < choices; i++)
            {
                var temp = pic.Clone(
                new Rectangle(200, 950 + i * 200, 700, 175),
                pic.PixelFormat
                );
                string tempPic = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                temp.Save(tempPic, pic.RawFormat);
                optionAsync[i] = picUtil.getAllStringInPicAsync(tempPic);
            }

            question = questionAsync.Result;

            for (int i = 0; i < choices; i++)
            {
                option[i] = optionAsync[i].Result;
            }
        }
    }
}
