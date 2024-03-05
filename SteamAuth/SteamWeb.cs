﻿namespace SteamAuth
{
    using System;
    using System.Collections.Specialized;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;

    public class SteamWeb
    {
        public static string MobileAppUserAgent = "Dalvik/2.1.0 (Linux; U; Android 9; Valve Steam App Version/3)";

        public static async Task<string> GetRequest(string url, CookieContainer cookies)
        {
            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = MobileAppUserAgent;
                response = await wc.DownloadStringTaskAsync(url);
            }

            return response;
        }

        public static async Task<string> PostRequest(string url, CookieContainer cookies, NameValueCollection body)
        {
            if (body == null)
            {
                body = new NameValueCollection();
            }

            string response;
            using (CookieAwareWebClient wc = new CookieAwareWebClient())
            {
                wc.Encoding = Encoding.UTF8;
                wc.CookieContainer = cookies;
                wc.Headers[HttpRequestHeader.UserAgent] = MobileAppUserAgent;
                byte[] result = await wc.UploadValuesTaskAsync(new Uri(url), "POST", body);
                response = Encoding.UTF8.GetString(result);
            }

            return response;
        }
    }
}