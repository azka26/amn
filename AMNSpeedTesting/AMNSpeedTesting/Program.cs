using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.IO;

namespace AMNSpeedTesting
{
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
                String path = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, $"output_{t.UserName}_{patternOut}.csv");
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
