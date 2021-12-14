using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HsmTool
{
    public class HmacClient : DelegatingHandler
    {
        private SigningAlgorithm alName = SigningAlgorithm.HmacSHA256;
        private ApiCre  cred =  null;
        public enum SigningAlgorithm
        {
            HmacSHA1,
            HmacSHA256
        }

        public HmacClient(string api, string secret)
        {
            InnerHandler = new HttpClientHandler();
            cred = new ApiCre() { 
                AppId = api,
                AppSecret =  secret
            };

        }

        public const string RfcFormat = @"ddd, dd MMM yyyy HH:mm:ss \G\M\T";

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage res = null;
            string resCont = string.Empty;
            string reqUri = request.RequestUri.AbsoluteUri.ToLower();
            string reqUriMethod = request.Method.Method;
            string reqTime = Convert.ToUInt64((DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc))).TotalSeconds).ToString();
            string nonc = Guid.NewGuid().ToString("N");
            var dateNow = DateTime.UtcNow.ToString(RfcFormat);
            resCont = await request.Content.ReadAsStringAsync();
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0}\n", reqUriMethod);
            sb.AppendFormat("{0}\n", request.RequestUri.Scheme);
            sb.AppendFormat("{0}\n", request.RequestUri.Host.ToString() + ":" + request.RequestUri.Port.ToString());
            sb.AppendFormat("{0}\n", request.RequestUri.LocalPath);
            sb.AppendFormat("{0}\n", request.Content.Headers.ContentType);
            sb.AppendFormat("{0}\n", cred.AppId);
            sb.AppendFormat("{0}\n", nonc);
            sb.AppendFormat("{0}\n", dateNow);
            sb.AppendFormat("{0}\n", resCont);
            byte[] sign = Encoding.UTF8.GetBytes(sb.ToString());
            var secKey = Convert.FromBase64String(cred.AppSecret);
            KeyedHashAlgorithm keyed = new HMACSHA256();
            try
            {
                keyed.Key = secKey;
                byte[] signByte = keyed.ComputeHash(sign);
                string dig = Convert.ToBase64String(signByte);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(alName.ToString(), $"{cred.AppId}:{nonc}:{dig}:{reqTime}");
                request.Headers.Add("Date", dateNow);
            }
            catch (Exception)
            {

                throw;
            }
            finally
            {
                keyed.Clear();
            }
            res = await base.SendAsync(request, cancellationToken);
            return res;
        }
    }
    public class HttpClientFactory
    {
        public static HttpClient CreateHttpClient(string url, string appId, string apiSecret)
        {
            HttpClient client = new HttpClient(new HmacClient(appId, apiSecret));
            var uri = new Uri(url);
            client.BaseAddress = uri;
            return client;
        }
    } 
    
    public class ApiCre
    {
        public string AppId { get; set; }

        public string AppSecret { get; set; }
    }
    public class XmlSignedData
    {
        public string base64xmlsigned { get; set; }
        public int status { get; set; }
        public string description { get; set; }
    }
}
