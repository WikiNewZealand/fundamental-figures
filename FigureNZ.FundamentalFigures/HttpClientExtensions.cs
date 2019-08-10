using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FigureNZ.FundamentalFigures
{
    public static class HttpClientExtensions
    {
        public static async Task<string> DownloadHttpFileAsync(this HttpClient client, Uri uri, string destination)
        {
            HttpFile httpFile = await client.GetHttpFileAsync(uri);

            uri = httpFile.Uri;
            string csvFile = Path.Combine(destination, httpFile.FileName);

            if (File.Exists(csvFile))
            {
                Console.WriteLine($"Found '{Path.GetFileName(csvFile)}'");
                return csvFile;
            }

            Console.WriteLine($"Downloading '{Path.GetFileName(csvFile)}' from '{uri}'");

            string directory = Path.GetDirectoryName(csvFile);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream stream = new FileStream(csvFile, FileMode.Create))
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                if (!response.IsSuccessStatusCode)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Downloading '{Path.GetFileName(csvFile)}' from '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'");
                    Console.WriteLine();
                    Console.ResetColor();

                    return null;
                }

                (await response.Content.ReadAsStreamAsync()).CopyTo(stream);
            }

            return csvFile;
        }

        public static Task<HttpResponseMessage> HeadAsync(this HttpClient client, Uri uri)
        {
            return client.SendAsync(new HttpRequestMessage(HttpMethod.Head, uri));
        }

        public static async Task<HttpFile> GetHttpFileAsync(this HttpClient client, Uri uri)
        {
            using (HttpResponseMessage response = await client.HeadAsync(uri))
            {
                if (response.IsSuccessStatusCode)
                {
                    return new HttpFile
                    {
                        Uri = uri,
                        FileName = response.Content.Headers.ContentDisposition.FileName
                    };
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}', checking dataset for 302 Redirect");
                Console.ResetColor();

                // We'll try the URL of the data table and see if it has a redirect to a new URL
                // - Remove /download segment, trim trailing slash because Figure.NZ does not expect it
                UriBuilder ub = new UriBuilder(uri)
                {
                    Path = Path.Combine(uri.Segments.Take(uri.Segments.Length - 1).ToArray()).TrimEnd('/')
                };

                // - Replace existing URI
                uri = ub.Uri;
            }

            // - Try again
            using (HttpResponseMessage response = await client.GetAsync(uri))
            {
                if (response.StatusCode != HttpStatusCode.Redirect)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'");
                    Console.ResetColor();

                    throw new ArgumentException($"Response for '{uri}' failed with '{response.StatusCode}: {response.ReasonPhrase}'", nameof(uri));
                    // return null;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"Response for '{uri}' succeeded with '{response.StatusCode}: {response.ReasonPhrase} {response.Headers.Location}'");
                Console.ResetColor();

                // - Append /download to the redirect location
                UriBuilder ub = new UriBuilder(response.Headers.Location);
                ub.Path = Path.Combine(response.Headers.Location.AbsolutePath, "download");

                // - Replace existing URI
                uri = ub.Uri;
            }

            // Recurse through the pipeline again, in case this one requires a redirect too
            return await client.GetHttpFileAsync(uri);
        }
    }
}
