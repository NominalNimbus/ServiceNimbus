/* 
 * This project is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/
 * Any copyright is dedicated to the NominalNimbus.
 * https://github.com/NominalNimbus 
*/

using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using Com.Lmax.Api.Internal.Xml;

namespace Com.Lmax.Api.Internal
{
    public class HttpInvoker : IHttpInvoker
    {
        [ThreadStatic] private static XmlStructuredWriter _writer;
        private readonly string _userAgent;

        static HttpInvoker()
        {
            ServicePointManager.Expect100Continue = false;
            ServicePointManager.UseNagleAlgorithm = false;
            ServicePointManager.DefaultConnectionLimit = 5;
        }

        public HttpInvoker() : 
            this("")
        {
        }

        public HttpInvoker(string clientIdentifier)
        {
            _userAgent = "LMAX .Net API, version: " + Assembly.GetExecutingAssembly().GetName().Version + ", id: " +
                         clientIdentifier;
        }

        public virtual Response Invoke(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, out string sessionId)
        {
            const string delimiter = "; ";
            HttpWebRequest webRequest = CreateWebRequest(baseUri, request);
            setUserAgent(webRequest);
            WriteRequest(webRequest, request);

            HttpWebResponse webResponse = ReadResponse(webRequest, xmlParser, handler);
            try
            {
                string[] cookies = webResponse.Headers.GetValues("Set-Cookie");
                if (null != cookies)
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (string cookie in cookies)
                    {
                        builder.Append(ExtractCookiePair(cookie));
                        builder.Append(delimiter);
                    }
                    builder.Remove(builder.Length - delimiter.Length, delimiter.Length);
                    sessionId = builder.ToString();
                }
                else
                {
                    sessionId = null;
                }
            }
            finally
            {
                webResponse.Close();
            }

            return new Response(webResponse.StatusCode);
        }

        public virtual Response PostInSession(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, string sessionId)
        {
            HttpWebRequest webRequest = SendRequest(baseUri, request, sessionId);

            HttpWebResponse webResponse = ReadResponse(webRequest, xmlParser, handler);
            HttpStatusCode httpStatusCode = webResponse.StatusCode;
            webResponse.Close();
            return new Response(httpStatusCode);
        }

        public Response GetInSession(string baseUri, IRequest request, IXmlParser xmlParser, Handler handler, string sessionId)
        {
            if (null == sessionId)
            {
                throw new ArgumentException("'sessionId' must not be null");
            }

            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(baseUri + request.Uri);
            setUserAgent(webRequest);
            webRequest.Method = "GET";
            webRequest.Accept = "text/xml";
            webRequest.Headers.Add("Cookie", sessionId);

            HttpWebResponse webResponse = ReadResponse(webRequest, xmlParser, handler);
            HttpStatusCode httpStatusCode = webResponse.StatusCode;
            webResponse.Close();
            return new Response(httpStatusCode);
        }

        public IConnection Connect(Uri uri, string sessionId)
        {
            HttpWebRequest webRequest = CreateBinaryGetRequest(uri);
            webRequest.Headers.Add("Cookie", sessionId);
            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                return new Connection(webRequest, webResponse);

            }
            catch (WebException e)
            {
                throw new UnexpectedHttpStatusCodeException(((HttpWebResponse)e.Response)?.StatusCode ?? HttpStatusCode.RequestTimeout);
            }

        }

        public IConnection Connect(string baseUri, IRequest request, string sessionId)
        {
            HttpWebRequest webRequest = SendRequest(baseUri, request, sessionId);
            try
            {
                HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();
                return new Connection(webRequest, webResponse);
                
            } 
            catch (WebException e)
            {
                throw new UnexpectedHttpStatusCodeException(((HttpWebResponse)e.Response)?.StatusCode ?? HttpStatusCode.RequestTimeout);
            }
        }

        private static XmlStructuredWriter Writer
        {
            get { return _writer ?? (_writer = new XmlStructuredWriter()); }
        }

        private HttpWebRequest SendRequest(string baseUri, IRequest request, string sessionId)
        {
            if (null == sessionId)
            {
                throw new ArgumentException("'sessionId' must not be null");
            }

            HttpWebRequest webRequest = CreateWebRequest(baseUri, request);
            webRequest.Headers.Add("Cookie", sessionId);

            setUserAgent(webRequest);

            WriteRequest(webRequest, request);
            return webRequest;
        }

        private HttpWebResponse ReadResponse(HttpWebRequest webRequest, IXmlParser xmlParser, Handler handler)
        {
            HttpWebResponse webResponse = (HttpWebResponse)webRequest.GetResponse();

            using (StreamReader reader = new StreamReader(webResponse.GetResponseStream(), new UTF8Encoding()))
            {
                xmlParser.Parse(reader, new SaxContentHandler(handler));
            }

            return webResponse;
        }

        private static void WriteRequest(HttpWebRequest webRequest, IRequest request)
        {
            try
            {
                request.WriteTo(Writer);

                using (Stream oStreamOut = webRequest.GetRequestStream())
                {
                    Writer.WriteTo(oStreamOut);
                }
            }
            finally
            {
                Writer.Reset();
            }
        }

        private HttpWebRequest CreateBinaryGetRequest(Uri uri)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(uri);
            webRequest.Method = "GET";
            webRequest.Accept = "*/*";
            setUserAgent(webRequest);
            return webRequest;
        }

        private HttpWebRequest CreateWebRequest(string baseUri, IRequest request)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(baseUri + request.Uri);
            webRequest.Method = "POST";
            webRequest.ContentType = "text/xml";
            webRequest.Accept = "text/xml";
            return webRequest;
        }

        private void setUserAgent(HttpWebRequest webRequest)
        {
            webRequest.UserAgent = _userAgent;
        }

        private string ExtractCookiePair(string cookie)
        {
            int index = cookie.IndexOf(';');
            return (index != -1) ? cookie.Substring(0, index) : cookie;
        }
    }
}
