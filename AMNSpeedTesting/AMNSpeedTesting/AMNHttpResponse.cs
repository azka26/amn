using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace AMNSpeedTesting
{
    public class AMNHttpResponse { 
        public String PathURL { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }
        public List<TimeSpan> CurrentPageLoadTime { get; set; } = new List<TimeSpan>();
    }
}
