using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace SearchEngine
{
    class BingOCR
    {

        const string OCRAccessKey = "c746bd5de72e4bac883fd54d6195229e";

        /// <summary>
        /// Gets the text visible in the specified image file by using the Computer Vision REST API.
        /// </summary>
        /// <param name="imageFilePath">The image file.</param>
        public async Task<string> MakeOCRRequestAsync(string imageFilePath)
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

            string requestParameters;
            // Request parameters.
            requestParameters = "language=unk&detectOrientation=true";
           
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
        private byte[] GetImageAsByteArray(string imageFilePath)
        {
            FileStream fileStream = new FileStream(imageFilePath, FileMode.Open, FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            return binaryReader.ReadBytes((int)fileStream.Length);
        }
    }
}
