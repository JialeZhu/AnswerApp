using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    struct pixel
    {
        public int x;
        public int y;

        public pixel(int x, int y) : this()
        {
            this.x = x;
            this.y = y;
        }
    }

    class PicUtils
    {
        public string getAllStringInPic(string path)
        {
            string res = "";
            Console.WriteLine("OCR ...");
            var ocrTool = new BingOCR();
            string jsonString = ocrTool.MakeOCRRequest(path).Result;
            Console.WriteLine("OCR finished");
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
            JArray text = jo["regions"][0]["lines"] as JArray;

            int lines = text.Count;
            for (int i = 0; i < lines; i++)
            {
                res += WordUtils.words2string(text[i]);
            }

            return res;
        }

        /// <summary>
        /// Spilt the question part as seperate picture
        /// </summary>
        /// <param name="picname">The original full screenshot</param>
        /// <returns>The question part screen</returns>
        public static string SplitPic(string picname, int gameCode, string workspace)
        {
            /// <summary>
            /// Flood fill coex
            /// </summary>f
            const int coex = 5;

            // The question background pixel for floodfill
            int[] startX = new int[] { 200, 200, 200 };
            int[] startY = new int[] { 200, 200, 200 };

            // Cut head for no need info
            int[] topChop = new int[] { 200, 200, 200 };
            int[] bottomChop = new int[] { 0, 100, 0 };

            Console.WriteLine("Going to process psicture: {0}", picname);
            Bitmap pic = (Bitmap)Image.FromFile(picname);

            int backgroundX = startX[gameCode];
            int backgroundY = startY[gameCode];

            int questionTop = 2000;
            int questionBottom = 0;
            int questionLeft = 2000;
            int questionRight = 0;

            int[] dx = new int[] { 1, 0, -1, 0 };
            int[] dy = new int[] { 0, 1, 0, -1 };
            var baseColor = pic.GetPixel(backgroundX, backgroundY);
            Queue<pixel> q = new Queue<pixel>();
            HashSet<int> visited = new HashSet<int>();
            q.Enqueue(new pixel(backgroundX, backgroundY));
            visited.Add(trans(backgroundX, backgroundY));

            while (q.Count > 0)
            {
                var nowpix = q.Dequeue();
                if (nowpix.x > questionRight) questionRight = nowpix.x;
                if (nowpix.x < questionLeft) questionLeft = nowpix.x;
                if (nowpix.y > questionBottom) questionBottom = nowpix.y;
                if (nowpix.y < questionTop) questionTop = nowpix.y;
                for (int i = 0; i < 4; i++)
                {
                    var nx = nowpix.x + dx[i] * coex;
                    var ny = nowpix.y + dy[i] * coex;
                    if (!(nx >= 0 && nx < pic.Width && ny >= 0 && ny < pic.Height)) continue;
                    var nextpix = new pixel(nx, ny);
                    if (!visited.Contains(trans(nextpix.x, nextpix.y))
                        && distance(pic.GetPixel(nextpix.x, nextpix.y), baseColor) < 5)
                    {
                        visited.Add(trans(nextpix.x, nextpix.y));
                        q.Enqueue(nextpix);
                    }
                }
            }

            questionTop += topChop[gameCode]; // chop more
            questionBottom -= bottomChop[gameCode];

            var questionPic = pic.Clone(
                new Rectangle(questionLeft, questionTop, questionRight - questionLeft, questionBottom - questionTop),
                pic.PixelFormat
                );

            string questionPath = workspace + "QuestionSnap" + DateTime.Now.ToString().Replace(':', '-').Replace('/', '-') + ".png";
            questionPic.Save(questionPath, pic.RawFormat);
            Console.WriteLine("Spilted");
            return questionPath;
        }

        static int distance(Color a, Color b)
        {
            return Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);
        }

        static int trans(int x, int y)
        {
            return x * 10000 + y;
        }

    }
}
