using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    // Used to return search results including relevant headers
    struct SearchResult
    {
        public String jsonResult;
        public Dictionary<String, String> relevantHeaders;
    }
    
    class BingSearch
    {
        private const string BingSearchAccessKey = "12024f47def94ec4a8e0d818376751c4";

        /// <summary>
        /// Search term
        /// </summary>
        /// <param name="searchTerm">The term to search</param>
        /// <returns>Result count, -1 if meet error</returns>
        public double SearchCounter(string searchTerm, bool showRes = false)
        {
            searchTerm = searchTerm.Replace("\"", "").Replace("·", "");

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
                if (showRes) Console.WriteLine(Utils.JsonPrettyPrint(result.jsonResult));
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
        public SearchResult BingWebSearch(string searchQuery)
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
                + "&count=3"
                + "&mkt=zh-cn"
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
    }
}
