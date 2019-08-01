using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Aws4RequestSigner;
using System.Net.Http;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Xml;

namespace Utils
{
    public static class ScwObjectStorage
    {
        private static readonly string keyId = "YOUR KEY ID";
        private static readonly string secretAccessKey = "YOUR SECRET";
        private static readonly string region = "YOUR REGION fr-par for example";
        private static readonly string service = "s3";
        private static readonly string provider = "scw.cloud";

        public static async Task GetListObjectFromBucket(string bucketName)
        {
            var response = await CallWebservice(HttpMethod.Get, new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/"));
            var responseStr = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseStr);
        }

        public static async Task GetBucketVersion(string bucketName)
        {
            var response = await CallWebservice(HttpMethod.Get, new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/?location"));
            var responseStr = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseStr);
        }

        public static async Task<Dictionary<DateTime, string>> GetAllVersionsFromAnObject(string bucketName, string objectName)
        {
            Dictionary<DateTime, string> versions = new Dictionary<DateTime, string>();
            var response = await GetAllVersionsFromObjectName(bucketName, objectName);
            var responseStr = await response.Content.ReadAsStringAsync();
            XmlDocument myDoc = new XmlDocument();

            myDoc.LoadXml(responseStr);

            var q = myDoc.GetElementsByTagName("Version");

            foreach (XmlNode node in q)
            {
                string versionId = null;
                DateTime date = new DateTime();
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    if (node.ChildNodes[i].Name == "LastModified")
                        date = DateTime.Parse(node.ChildNodes[i].InnerText);
                    else if (node.ChildNodes[i].Name == "VersionId")
                        versionId = node.ChildNodes[i].InnerText;                  
                }
                versions.Add(date, versionId); 
            }

            return versions;
        }

        public static async Task<string> GetStringObjectByName(string bucketName, string objectName, string versionId = null)
        {
            var response = await GetObjectByName(bucketName, objectName, versionId);
            var responseStr = await response.Content.ReadAsStringAsync();
            return responseStr;
        }

        public static async Task<byte[]> GetByteObjectByName(string bucketName, string objectName, string versionId = null)
        {
            var response = await GetObjectByName(bucketName, objectName, versionId);
            var responseStr = await response.Content.ReadAsByteArrayAsync();
            return responseStr;
        }

        public static async Task<Stream> GetStreamObjectByName(string bucketName, string objectName, string versionId = null)
        {
            var response = await GetObjectByName(bucketName, objectName, versionId);
            var responseStr = await response.Content.ReadAsStreamAsync();
            return responseStr;
        }

        public static async Task<bool> AddObject(string bucketName, string objectName, byte[] bytes)
        {
            var response = await CallWebservice(HttpMethod.Put, new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/" + objectName), bytes);
            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> DeleteObject(string bucketName, string objectName)
        {
            var response = await CallWebservice(HttpMethod.Delete, new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/" + objectName));
            return response.IsSuccessStatusCode;
        }

        #region Private method
        private static async Task<HttpResponseMessage> CallWebservice(HttpMethod method, Uri uri, byte[] content = null)
        {
            var signer = new AWS4RequestSigner(keyId, secretAccessKey);
            var request = new HttpRequestMessage
            {
                Method = method,
                RequestUri = uri
            };

            if (content != null)
            {
                request.Content = new ByteArrayContent(content);
                string sha256Hash = Hash(content);
                request.Headers.Add("x-amz-content-sha256", sha256Hash);
            }

            request = await signer.Sign(request, service, region);

            var client = new HttpClient();
            var response = await client.SendAsync(request);
            return response;
        }

        private static async Task<HttpResponseMessage> GetObjectByName(string bucketName, string objectName, string specificVersion = null)
        {
            Uri uri = null;

            if (string.IsNullOrEmpty(specificVersion))
                uri = new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/" + objectName);
            else
                uri = new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/" + objectName + "?versionId=" + specificVersion);

            return await CallWebservice(HttpMethod.Get, uri);
        }

        private static async Task<HttpResponseMessage> GetAllVersionsFromObjectName(string bucketName, string objectName)
        {
            return await CallWebservice(HttpMethod.Get, new Uri("https://" + bucketName + "." + service + "." + region + "." + provider + "/?versions&prefix=" + objectName));
        }

        private static string Hash(byte[] bytesToHash)
        {
            SHA256 _sha256 = SHA256.Create();
            var result = _sha256.ComputeHash(bytesToHash);
            return ToHexString(result);
        }

        private static string ToHexString(byte[] array)
        {
            var hex = new StringBuilder(array.Length * 2);
            foreach (byte b in array)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }
        #endregion
    }
}
