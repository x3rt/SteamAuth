﻿namespace SteamAuth
{
    using System;
    using System.Net;

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();

        public CookieCollection ResponseCookies { get; set; } = new CookieCollection();

        protected override WebRequest GetWebRequest(Uri address)
        {
            HttpWebRequest request = (HttpWebRequest)base.GetWebRequest(address);
            request.CookieContainer = CookieContainer;
            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            HttpWebResponse response = (HttpWebResponse)base.GetWebResponse(request);
            ResponseCookies = response.Cookies;
            return response;
        }
    }
}