// #define DEBUG

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
        // **********************************************
        // *** Update or verify the following values. ***
        // **********************************************

        // Replace the accessKey string value with your valid access key.
        const string BingSearchAccessKey = "12024f47def94ec4a8e0d818376751c4";

        const string OCRAccessKey = "c746bd5de72e4bac883fd54d6195229e";

        const string workspace = @"D:\Workspace\AnswerApp\";

        /// <summary>
        /// The number of choices in each question
        /// </summary>
        const int choices = 3;

        // The question background pixel for floodfill
        static int[] startX = new int[] { 200, 200 };
        static int[] startY = new int[] { 200, 200 };
        
        // Cut head for no need info
        static int[] topChop = new int[] { 200, 200 };
        static int[] bottomChop = new int[] { 0, 100 };

        static int gameCode = 0; // Which game? 0 - CDDH 1 - ZSCR 2 - BWYX

        // Used to return search results including relevant headers
        struct SearchResult
        {
            public String jsonResult;
            public Dictionary<String, String> relevantHeaders;
        }

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

        static int Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
#if DEBUG
            gameCode = 1;
            string imageFilePath = @"D:\Workspace\AnswerApp\screenshot17-01.png";
#else
            gameCode = int.Parse(args[1]);
            string imageFilePath = args[0];
#endif
            string questionPic = SplitPic(imageFilePath);

            // Execute the REST API call.
            string jsonString = MakeOCRRequest(questionPic).Result;
            JObject jo = (JObject)JsonConvert.DeserializeObject(jsonString);
            JArray text = jo["regions"][0]["lines"] as JArray;

            string question = string.Empty;
            string[] option = new string[choices];
            int lines = text.Count;
            for (int i = 0; i < lines - choices; i++)
            {
                question += words2string(text[i]);
            }

            while (question[0] >= '0' && question[0] <= '9')
            {
                question = question.Substring(1, question.Length - 1);
            }

            for (int i = 0; i < choices; i++)
            {
                option[i] = words2string(text[lines - choices + i]);
            }
            Console.WriteLine("Question: {0}", question);
            Console.WriteLine("A: {0}", option[0]);
            Console.WriteLine("B: {0}", option[1]);
            Console.WriteLine("C: {0}", option[2]);

            double[] rate = new double[choices];

            for (int i = 0; i < choices; i++)
            {
                double a = SearchCounter(question + " " + option[i]);
                double b = SearchCounter(option[i]);
                
                if (b < 1)
                {
                    b = 100000000000000;
                }

                rate[i] = a / b;
                Console.WriteLine("Rating for choice {0}: {1} / {2} = {3}", i + 1, a, b, rate[i]);
            }

            return GetBiggestIndex(rate);
        }

        static int GetBiggestIndex(double[] rate)
        {
            double biggest = 0;
            int res = 0;
            for (int i = 0; i < rate.Length; i++)
            {
                if (rate[i] > biggest)
                {
                    biggest = rate[i];
                    res = i;
                }
            }

            return res;
        }

        static string words2string(JToken text)
        {
            string res = "";
            JArray words = text["words"] as JArray;
            foreach (var token in words)
            {
                res += token["text"].ToString();
            }

            return res;
        }

        /// <summary>
        /// Spilt the question part as seperate picture
        /// </summary>
        /// <param name="picname">The original full screenshot</param>
        /// <returns>The question part screen</returns>
        static string SplitPic(string picname)
        {
            Console.WriteLine("Going to process picture: {0}", picname);
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
                    var nx = nowpix.x + dx[i];
                    var ny = nowpix.y + dy[i];
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

        /// <summary>
        /// Search term
        /// </summary>
        /// <param name="searchTerm">The term to search</param>
        /// <returns>Result count, -1 if meet error</returns>
        static double SearchCounter(string searchTerm)
        {
            if (BingSearchAccessKey.Length == 32)
            {
                //Console.WriteLine("Searching the Web for: " + searchTerm);
                SearchResult result = BingWebSearch(searchTerm);

                //Console.WriteLine("\nResult count:\n");
                int start = result.jsonResult.IndexOf("totalEstimatedMatches") + 24;
                int end = result.jsonResult.IndexOf("value") - 3;
                if (end < 0) // no result
                {
                    return 0;
                }

                int count = int.Parse(result.jsonResult.Substring(start, end - start));
                //Console.WriteLine(count);
                //Console.WriteLine(JsonPrettyPrint(result.jsonResult));
                return count;
            }
            else
            {
                Console.WriteLine("Invalid Bing Search API subscription key!");
                return -1;
            }
        }

        /// <summary>
        /// Performs a Bing Web search and return the results as a SearchResult.
        /// </summary>
        static SearchResult BingWebSearch(string searchQuery)
        {
            // Verify the endpoint URI.  At this writing, only one endpoint is used for Bing
            // search APIs.  In the future, regional endpoints may be available.  If you
            // encounter unexpected authorization errors, double-check this value against
            // the endpoint for your Bing Web search instance in your Azure dashboard.
            const string uriBase = "https://api.cognitive.microsoft.com/bing/v7.0/search";

            // Construct the URI of the search request
            var uriQuery = uriBase 
                + "?q=" + Uri.EscapeDataString(searchQuery) 
                + "&setLang=zh" 
                + "&count=1"
                + "&mkt=en-US"
                + "&responseFilter=Webpages";

            // Perform the Web request and get the response
            WebRequest request = HttpWebRequest.Create(uriQuery);
            request.Headers["Ocp-Apim-Subscription-Key"] = BingSearchAccessKey;
            HttpWebResponse response = (HttpWebResponse)request.GetResponseAsync().Result;
            string json = new StreamReader(response.GetResponseStream()).ReadToEnd();

            // Create result object for return
            var searchResult = new SearchResult()
            {
                jsonResult = json,
                relevantHeaders = new Dictionary<String, String>()
            };

            // Extract Bing HTTP headers
            foreach (String header in response.Headers)
            {
                if (header.StartsWith("BingAPIs-") || header.StartsWith("X-MSEdge-"))
                    searchResult.relevantHeaders[header] = response.Headers[header];
            }

            return searchResult;
        }

        /// <summary>
        /// Gets the text visible in the specified image file by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        static async Task<string> MakeOCRRequest(string imageFilePath)
        {
            // Replace or verify the region.
            //
            // You must use the same region in your REST API call as you used to obtain your subscription keys.
            // For example, if you obtained your subscription keys from the westus region, replace 
            // "westcentralus" in the URI below with "westus".
            //
            // NOTE: Free trial subscription keys are generated in the westcentralus region, so if you are using
            // a free trial subscription key, you should not need to change this region.
            const string uriBase = "https://eastus.api.cognitive.microsoft.com/vision/v1.0/ocr";

            HttpClient client = new HttpClient();

            // Request headers.
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", OCRAccessKey);

            // Request parameters.
            string requestParameters = "language=unk&detectOrientation=true";

            // Assemble the URI for the REST API Call.
            string uri = uriBase + "?" + requestParameters;

            HttpResponseMessage response;

            // Request body. Posts a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(imageFilePath);

            using (ByteArrayContent content = new ByteArrayContent(byteData))
            {
                // This example uses content type "application/octet-stream".
                // The other content types you can use are "application/json" and "multipart/form-data".
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                // Execute the REST API call.
                response = await client.PostAsync(uri, content);

                // Get the JSON response.
                string contentString = await response.Content.ReadAsStringAsync();

                return contentString;
            }
        }
        
        /// <summary>
        /// Returns the contents of the specified file as a byte array.
        /// </summary>
        /// <param name="imageFilePath">The image file to read.</param>
        /// <returns>The byte array of the image data.</returns>
        static byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }

        /// <summary>
        /// Formats the given JSON string by adding line breaks and indents.
        /// </summary>
        /// <param name="json">The raw JSON string to format.</param>
        /// <returns>The formatted JSON string.</returns>
        static string JsonPrettyPrint(string json)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            json = json.Replace(Environment.NewLine, "").Replace("\t", "");

            StringBuilder sb = new StringBuilder();
            bool quote = false;
            bool ignore = false;
            int offset = 0;
            int indentLength = 3;

            foreach (char ch in json)
            {
                switch (ch)
                {
                    case '"':
                        if (!ignore) quote = !quote;
                        break;
                    case '\'':
                        if (quote) ignore = !ignore;
                        break;
                }

                if (quote)
                    sb.Append(ch);
                else
                {
                    switch (ch)
                    {
                        case '{':
                        case '[':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', ++offset * indentLength));
                            break;
                        case '}':
                        case ']':
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', --offset * indentLength));
                            sb.Append(ch);
                            break;
                        case ',':
                            sb.Append(ch);
                            sb.Append(Environment.NewLine);
                            sb.Append(new string(' ', offset * indentLength));
                            break;
                        case ':':
                            sb.Append(ch);
                            sb.Append(' ');
                            break;
                        default:
                            if (ch != ' ') sb.Append(ch);
                            break;
                    }
                }
            }

            return sb.ToString().Trim();
        }
    }
}
