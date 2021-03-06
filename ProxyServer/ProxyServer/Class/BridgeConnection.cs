﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ProxyServer.Class
{
    public class BridgeConnection
    {
        public static int CAPACITY { get; } = 5242880;
        public Thread ProcessThread { get; set; }
        public int Timeout { get; set; }
        public string Name { get; set; }
        public Proxy Proxy { get; set; }
        public Client SocketClient { get; set; }
        public SWebClient WebClient { get; set; }

        public BridgeConnection()
        {
            SocketClient = new Client(this);

            WebClient = new SWebClient(this);
            Timeout = 0;

            Process();
        }

        public BridgeConnection(Socket client)
        {
            SocketClient = new Client(this, client);

            WebClient = new SWebClient(this);

            Timeout = 0;

            Process();
        }   

        public void Process()
        {
            SocketClient.Receive();
        }

        
        public void Close()
        {
            WebClient.Close();
            SocketClient.Close();
            if (Proxy.Instance == null)
            {
                Console.WriteLine("dsaabadzxzxzxzxzxzx");
            }
            Proxy.Instance.Remove(Name);
        }

        private void ReceivedClientCallback(object sender, ReceivedEventArgs e)
        {
            ProcessClientMessage(e.Data);
        }

        private void ReceivedServerCallback(object sender, ReceivedEventArgs e)
        {
            Console.WriteLine("Received from server: {0}\n", e.Data);
        }

        public void ProcessClientMessage(string message)
        {
            HttpMessage httpMessage = new HttpMessage(message);
            httpMessage.Resolve();
            switch (httpMessage.Method)
            {
                case HttpMethod.CONNECT:
                    Close();
                    break;
                case HttpMethod.GET:
                    if (Proxy.Instance.CheckBlackList(httpMessage.GetHost()))
                    {
                        Send403();
                        Close();
                        return;
                    }

                    Console.WriteLine("[Response] - " + message);

                    Console.WriteLine("Query: " + httpMessage.HttpUri.Query);

                    WebClient.IsCache = httpMessage.IsCache();
                    
                    WebClient.Connect(httpMessage.HttpUri);
                    WebClient.Content = message;
                    SendGetRequest(message);

                    break;
                case HttpMethod.POST:
                    Console.WriteLine(httpMessage.GetHost());
                    if (Proxy.Instance.CheckBlackList(httpMessage.GetHost()))
                    {
                        Send403();
                        Close();
                        return;
                    }

                    WebClient.Connect(httpMessage.HttpUri);
                    SendPostRequest(message);
                    break;
            }

        }

        public void ProcessServerMessage(string message)
        {
            HttpRespone respone = new HttpRespone(message);
            respone.Resolve();

            if (respone.StatusCode == HttpStatusCode.NotModified)
            {
                byte[] bytes;
                bytes = Proxy.Instance.GetCache(WebClient.Content);

                if (bytes != null)
                {
                    SocketClient.Send(bytes, 0, bytes.Length);
                }
                else
                {

                }

                return;
            }
            else
            {
                if (WebClient.IsCache)
                {
                    HttpCache cache = new HttpCache();
                }
            }

            SocketClient.Send(message);
        }

        public void ProcessServerMessage(byte[] bytes, int start, int length)
        {
            StringBuilder message = new StringBuilder();
            message.Append(Encoding.ASCII.GetString(bytes, start, length));
            
            HttpRespone respone = new HttpRespone(message.ToString());
            respone.Resolve();

            try
            {
                if (respone.StatusCode == HttpStatusCode.NotModified)
                {
                    byte[] bytesCache;
                    bytesCache = Proxy.Instance.GetCache(WebClient.Content);

                    if (bytes != null)
                    {
                        SocketClient.Send(bytesCache, 0, bytes.Length);
                    }
                    else
                    {

                    }

                    return;
                }
                else
                {
                    if (WebClient.IsCache)
                    {
                        HttpCache cache = new HttpCache();
                        cache.Content = bytes;
                        cache.Host = WebClient.Address.Host;
                        cache.Header = WebClient.Content;
                        cache.SaveFile();
                        Proxy.Instance.Caches.Add(cache);
                    }
                }
            }
            catch
            {
                SocketClient.Send(bytes, start, length);
                return;
            }

            SocketClient.Send(bytes, start, length);
        }

        private void ResponseConnect()
        {
            string response = "HTTP/1.1 200 OK\r\n";
            SocketClient.Send(response);
            Console.WriteLine(response);
        }

        private void SendGetRequest(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            WebClient.Send(message);
        }

        private void SendGetRequest(HttpMessage message)
        {
            string request = "GET http://soha.vn/ HTTP/1.1" +
                "Host: soha.vn\r\n" +
                "User-agent: Mozilla/5.0\r\n\r\n";
            WebClient.Send(request);
            return;
        }

        private void SendPostRequest(string message)
        {
            WebClient.Send(message);
        }
        private void Send403()
        {
            string response = "HTTP/1.1 403 Not Found\r\n" +
            "Content-Type: text/html\r\n\r\n" +
                "<!DOCTYPE html>\r\n" +
                                "<html>\r\n" +
                                "<h1>403 You cant go further</h1>\r\n" +
                                "<html>\r\n";
            SocketClient.Send(response);
        }
    }
}
