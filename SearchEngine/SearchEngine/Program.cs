//#define DEBUG

using System;
using System.Drawing;
using System.Text;
using System.Net;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing.Imaging;
using Newtonsoft.Json;
using System.Threading.Tasks;
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
        
        static int gameCode = 0; // Which game? 0 - CDDH 1 - ZSCR 2 - BWYX 3 - Zhihu
        
        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
#if DEBUG
            gameCode = 1;
            string imageFilePath = @"D:\Workspace\AnswerApp\screenshot13-17.png";
#else
            gameCode = int.Parse(args[1]);
            string imageFilePath = args[0];
#endif
            
            string question = string.Empty;
            string[] option = new string[10];

            var searcher = new BingSearch();

            if (gameCode == 3) // zhihu
            {
                choices = 4;
                Bitmap pic = (Bitmap)Image.FromFile(imageFilePath);
                var questionPic = pic.Clone(
                new Rectangle(50, 550, 950, 300),
                pic.PixelFormat
                );

                var picUtil = new PicUtils();

                string questionPath = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                questionPic.Save(questionPath, pic.RawFormat);
                question = picUtil.getAllStringInPic(questionPath);
                
                var temp = pic.Clone(
                new Rectangle(200, 950, 700, 175),
                pic.PixelFormat
                );
                string tempPic = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                temp.Save(tempPic, pic.RawFormat);
                option[0] = picUtil.getAllStringInPic(tempPic);

                temp = pic.Clone(
                new Rectangle(200, 1150, 700, 175),
                pic.PixelFormat
                );
                tempPic = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                temp.Save(tempPic, pic.RawFormat);
                option[1] = picUtil.getAllStringInPic(tempPic);

                temp = pic.Clone(
                new Rectangle(200, 1350, 700, 175),
                pic.PixelFormat
                );
                tempPic = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                temp.Save(tempPic, pic.RawFormat);
                option[2] = picUtil.getAllStringInPic(tempPic);

                temp = pic.Clone(
                new Rectangle(200, 1550, 700, 175),
                pic.PixelFormat
                );
                tempPic = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
                temp.Save(tempPic, pic.RawFormat);
                option[3] = picUtil.getAllStringInPic(tempPic);
            }
            else
            {
                string questionPic = PicUtils.SplitPic(imageFilePath, gameCode, workspace);
                // Execute the REST API call.
                Console.WriteLine("OCR ...");

                var ocrTool = new BingOCR();
                string jsonString = ocrTool.MakeOCRRequest(questionPic).Result;
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

                // show search result
                if (args.Length >= 3)
                {
                    searcher.SearchCounter(question, true);
                }

                for (int i = 0; i < choices; i++)
                {
                    option[i] = WordUtils.words2string(text[lines - choices + i]);
                }
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

            for (int i = 0; i < choices; i++)
            {
                double a = searcher.SearchCounter(question + " +" + "\"" + option[i] + "\"");
                double b = searcher.SearchCounter(option[i]);
                
                if (b < 1)
                {
                    b = 1000000000000;
                }

                //b = Math.Log(b, 1.01);
                rate[i] = a / b;
                Console.WriteLine("Rating for choice {0}: {1} / {2} = {3}", i + 1, a, b, rate[i]);
            }

            int ans = noflag ? Utils.GetSmallestIndex(rate) : Utils.GetBiggestIndex(rate);
            Console.WriteLine("推荐答案 {0}: {1}", Convert.ToChar('A' + ans), option[ans]);
            return ans;
        }
    }
}
