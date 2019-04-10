using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GrabTask
{
    public class CookieAwareWebClient : WebClient
    {
        public CookieAwareWebClient()
            : base()
        {
            CookieContainer = new CookieContainer();
            this.ResponseCookies = new CookieCollection();
        }

        public CookieContainer CookieContainer { get; private set; }
        public CookieCollection ResponseCookies { get; set; }

        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest request = base.GetWebRequest(address);

            HttpWebRequest webRequest = request as HttpWebRequest;
            if (webRequest != null)
            {
                webRequest.AllowAutoRedirect = true;
                webRequest.CookieContainer = CookieContainer;
            }

            return request;
        }

        protected override WebResponse GetWebResponse(WebRequest request)
        {
            var response = (HttpWebResponse)base.GetWebResponse(request);
            this.ResponseCookies = response.Cookies;
            return response;
        }
    }
}
