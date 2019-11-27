using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace AMNSpeedTesting
{
    public class AMNSpeedTest 
    {
        public String BaseURL { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
        public String LoginPage { get; set; }
        public int TestPageLoadTimeCount { get; set; } = 1;
        public List<String> ExceptionList { get; set; } = new List<string>();
        public Dictionary<String, AMNHttpResponse> UrlCollections { get; set; } = new Dictionary<string, AMNHttpResponse>();
        private HttpContent GetContentLogin() {
            return new StringContent($"UserName={UserName}&Password={Password}", Encoding.UTF8, "application/x-www-form-urlencoded");
        }
        public void CrawlerAllPage(Action<AMNHttpResponse> actionAfterFetch = null) {
            Uri uri = new Uri(BaseURL);
            CookieContainer cookieContainer = new CookieContainer();
            using (HttpClientHandler handler = new HttpClientHandler() { CookieContainer = cookieContainer })
            {
                using (HttpClient client = new HttpClient(handler) { BaseAddress = uri })
                {
                    HttpResponseMessage message = client.PostAsync(LoginPage, GetContentLogin()).Result;
                    String element = message.Content.ReadAsStringAsync().Result;
                    ProcessCrawler(client, element, actionAfterFetch);
                }
            }
        }

        private void ProcessCrawler(HttpClient client, String elementHtml, Action<AMNHttpResponse> actionAfterFetch = null) {
            MatchCollection matches = Regex.Matches(elementHtml, @"\b\S(([a-zA-Z0-9]+\/)+).[a-zA-Z0-9]+\S\b");
            if (matches == null || matches.Count == 0) return;
            foreach (Match match in matches)
            {
                String valueURL = match.Value;
                bool skiped = false;
                foreach (String e in ExceptionList)
                {
                    if (valueURL.Contains(e))
                    {
                        skiped = true;
                        break;
                    }
                }
                if (skiped) continue;

                if (UrlCollections.ContainsKey(valueURL)) continue;
                UrlCollections.Add(valueURL, new AMNHttpResponse() { PathURL = valueURL });

                // REQUEST HERE
                HttpResponseMessage response = null;
                for (int i = 0; i < TestPageLoadTimeCount; i++)
                {
                    DateTime startRequest = DateTime.Now;
                    response = client.GetAsync(valueURL).Result;
                    String statusCode = response.StatusCode.ToString();
                    DateTime endRequest = DateTime.Now;
                    UrlCollections[valueURL].StatusCode = response.StatusCode;
                    UrlCollections[valueURL].ResponseMessage = response;
                    UrlCollections[valueURL].CurrentPageLoadTime.Add((endRequest - startRequest));
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        break;
                    }
                }
                // END REQUEST

                if (actionAfterFetch != null) {
                    actionAfterFetch.Invoke(UrlCollections[valueURL]);
                }
                
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    ProcessCrawler(client, response.Content.ReadAsStringAsync().Result, actionAfterFetch);
                }
            }
        }
    }
}
