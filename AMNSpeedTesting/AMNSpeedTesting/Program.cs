using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;

namespace AMNSpeedTesting
{
    public class AMNHttpResponse { 
        public String PathURL { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public List<TimeSpan> CurrentPageLoadTime { get; set; } = new List<TimeSpan>();
    }

    public class AMNSpeedTest 
    {
        public String BaseURL { get; set; }
        public String UserName { get; set; }
        public String Password { get; set; }
        public String LoginPage { get; set; }
        public int TestPageLoadTimeCount { get; set; } = 1;
        public Dictionary<String, AMNHttpResponse> UrlCollections { get; set; } = new Dictionary<string, AMNHttpResponse>();
        private bool IsExistOnSamplePath(String path) {
            return false;
        }

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

        public List<String> ExceptionList { get; set; } = new List<string>();
        public void AddException(String value) {
            ExceptionList.Add(value);
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
    
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                AMNSpeedTest t = null;
                using (FileStream fs = new FileStream(Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "config", "crawler_config.json"), FileMode.Open))
                {
                    using (StreamReader rd = new StreamReader(fs))
                    {
                        String content = rd.ReadToEnd();
                        t = Newtonsoft.Json.JsonConvert.DeserializeObject<AMNSpeedTest>(content);
                    }
                }

                Console.WriteLine("Start Crawler");
                String patternOut = String.Format("{0:yyyyMMddHHmmss}", DateTime.Now);
                String path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, $"output_{patternOut}.csv");
                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                String results = "";
                t.CrawlerAllPage((p) =>
                {
                    String result = $"{p.PathURL};{p.StatusCode};";
                    List<String> counts = new List<string>();
                    foreach (TimeSpan t in p.CurrentPageLoadTime)
                    {
                        counts.Add(t.ToString());
                    }
                    result = result + String.Join(";", counts);
                    results += result + System.Environment.NewLine;
                    Console.WriteLine(result);
                    using (FileStream fs = new FileStream(path, FileMode.OpenOrCreate))
                    {
                        byte[] bytes = Encoding.ASCII.GetBytes(results);
                        fs.Write(bytes, 0, bytes.Length);
                    }

                    if (p.StatusCode == HttpStatusCode.OK)
                    {
                        String dirOut = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, patternOut, p.PathURL);
                        if (!Directory.Exists(dirOut))
                        {
                            Directory.CreateDirectory(dirOut);
                        }
                        dirOut = Path.Combine(dirOut, "out.html");
                        using (FileStream fs = new FileStream(dirOut, FileMode.OpenOrCreate))
                        {
                            byte[] bytes = Encoding.ASCII.GetBytes(p.ResponseMessage.Content.ReadAsStringAsync().Result);
                            fs.Write(bytes, 0, bytes.Length);
                        }
                    }
                });
                Console.WriteLine("Crawler Complete");
            }
            catch (Exception ex) {
                String outEx = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "error.log");
                using (FileStream fs = new FileStream(outEx, FileMode.OpenOrCreate))
                {
                    byte[] bytes = Encoding.ASCII.GetBytes(ex.Message);
                    fs.Write(bytes, 0, bytes.Length);
                }
            }
        }
    }
}
