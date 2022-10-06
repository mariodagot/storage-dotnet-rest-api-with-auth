namespace StorageRestApiAuth
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;

    internal static class Program
    {
        static string StorageAccountName = "YOURSTORAGEACCOUNTNAME";
        static string StorageAccountKey = "YOURSTORAGEACCOUNTKEY";
        static string StorageAccountContainer = "YOURSTORAGEACCOUNTCONTAINER";
        static string StorageAccountContainerBlob = "path/to/my/blob.txt";
        static string SampleContent = "This is sample text.";

        private static void Main()
        {
            // List the containers in a storage account.
            ListContainersAsyncREST(StorageAccountName, StorageAccountKey, CancellationToken.None).GetAwaiter().GetResult();

            // Create a blob file in a storage account.
            PutContainersAsyncREST(StorageAccountName, StorageAccountKey, StorageAccountContainer, StorageAccountContainerBlob, SampleContent, CancellationToken.None).GetAwaiter().GetResult();

            Console.WriteLine("Press any key to continue.");
            Console.ReadLine();
        }

        private static async Task PutContainersAsyncREST(string storageAccountName, string storageAccountKey, string storageAccountContainer, string storageAccountContainerBlob, string sampleContent, CancellationToken cancellationToken)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Construct the URI. This will look like this:
            //   https://myaccount.blob.core.windows.net/resource
            String uri = string.Format("https://{0}.blob.core.windows.net/{1}/{2}", storageAccountName, storageAccountContainer, storageAccountContainerBlob);

            // Set this to whatever payload you desire. Ours is null because 
            //   we're not passing anything in.
            Byte[] requestPayload = null;

            //Instantiate the request message with a null payload.
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, uri)
            { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {

                //int contentLength = Encoding.UTF8.GetByteCount(sampleContent);

                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2020-04-08");
                httpRequestMessage.Headers.Add("x-ms-blob-type", "BlockBlob");
                // If you need any additional headers, add them here before creating
                //   the authorization header. 

                httpRequestMessage.Content = new StringContent(sampleContent, Encoding.UTF8, "text/plain"); ;

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);

                // Send the request.
                using (HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
                {
                    // If successful (status code = 200), 
                    //   parse the XML response for the container names.
                    if (httpResponseMessage.StatusCode == HttpStatusCode.Created)
                    {
                        Console.WriteLine("Blob created!");
                    } else
                    {
                        Console.WriteLine("Error creating Blob");
                        using (TextReader textReader = new StreamReader(httpResponseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                        {
                            string line;
                            while ((line = textReader.ReadLine()) != null)
                                Console.WriteLine(line);
                        }
                        Console.WriteLine();
                    }
                }
            }
        }


        /// <summary>
        /// This is the method to call the REST API to retrieve a list of
        /// containers in the specific storage account.
        /// This will call CreateRESTRequest to create the request, 
        /// then check the returned status code. If it's OK (200), it will 
        /// parse the response and show the list of containers found.
        /// </summary>
        private static async Task ListContainersAsyncREST(string storageAccountName, string storageAccountKey, CancellationToken cancellationToken)
        {
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            // Construct the URI. This will look like this:
            //   https://myaccount.blob.core.windows.net/resource
            String uri = string.Format("https://{0}.blob.core.windows.net?comp=list", storageAccountName);

            // Set this to whatever payload you desire. Ours is null because 
            //   we're not passing anything in.
            Byte[] requestPayload = null;

            //Instantiate the request message with a null payload.
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri)
            { Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload) })
            {

                // Add the request headers for x-ms-date and x-ms-version.
                DateTime now = DateTime.UtcNow;
                httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
                httpRequestMessage.Headers.Add("x-ms-version", "2017-04-17");
                // If you need any additional headers, add them here before creating
                //   the authorization header. 

                // Add the authorization header.
                httpRequestMessage.Headers.Authorization = AzureStorageAuthenticationHelper.GetAuthorizationHeader(
                   storageAccountName, storageAccountKey, now, httpRequestMessage);

                // Send the request.
                using (HttpResponseMessage httpResponseMessage = await new HttpClient().SendAsync(httpRequestMessage, cancellationToken))
                {
                    // If successful (status code = 200), 
                    //   parse the XML response for the container names.
                    if (httpResponseMessage.StatusCode == HttpStatusCode.OK)
                    {
                        String xmlString = await httpResponseMessage.Content.ReadAsStringAsync();
                        XElement x = XElement.Parse(xmlString);
                        foreach (XElement container in x.Element("Containers").Elements("Container"))
                        {
                            Console.WriteLine("Container name = {0}", container.Element("Name").Value);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error Listing Containers");
                        using (TextReader textReader = new StreamReader(httpResponseMessage.Content.ReadAsStreamAsync().GetAwaiter().GetResult()))
                        {
                            string line;
                            while ((line = textReader.ReadLine()) != null)
                                Console.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
}
