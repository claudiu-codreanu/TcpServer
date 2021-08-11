using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Text;

namespace TcpServer
{
    class Server2
    {
        private Settings settings;
        private TcpListener server;
        private bool isListening;

        private string folder;
        private long sessionId;

        public Server2(Settings settings)
        {
            this.settings = settings;
            server = new TcpListener(settings.Ip.ToLower() == "any" ? IPAddress.Any : IPAddress.Parse(settings.Ip), settings.Port);
        }


        public void StartListening()
        {
            server.Start();
            isListening = true;

            while (isListening)
            {
                Console.WriteLine("TCP Server:\tWaiting for client connection");

                TcpClient client;

                try
                {
                    client = server.AcceptTcpClient();
                }
                catch
                {
                    // stopping from outside throws WSACancelBlockingCall
                    // just "swallow" it

                    continue;
                }

                long id = Interlocked.Increment(ref sessionId);
                Console.WriteLine($"TCP Server:\tClient connected, session id = {id}");

                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(new { TcpClient = client, SessionId = id });

                Thread.Sleep(1000);
            }


            Console.WriteLine("TCP Server:\tStopping");
            Console.WriteLine("TCP Server:\tBye bye!");
        }

        public void StopListening()
        {
            isListening = false;
            server.Stop();
        }

        private void HandleClient(dynamic data)
        {
            TcpClient client = (TcpClient)data.TcpClient;
            long id = data.SessionId;

            Program.m_clients.Add(id.ToString(), client);

            try
            {
                //WriteMessage(client, $"Hello, client {id}! Say something!\r\n");

                while (true)
                {
                    string msg = ReadNextMessage(client);

                    if (msg.ToLower() == "stop!")
                    {
                        client.Close();
                        StopListening();

                        return;
                    }

                    Console.WriteLine($"Session {id}:\tMessage received: {msg}");
                    //WriteMessage(client, $"You said '{msg.Trim()}'. Now say something else.\r\n");
                }
            }
            catch
            {
                Console.WriteLine($"Session {id}:\tClient disconnected");
                client.Close();

                Program.m_clients.Remove(id.ToString());
            }
        }


        private string ReadNextMessage(TcpClient client)
        {
            StringBuilder sb = new StringBuilder();

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            DateTime start = DateTime.Now;

            while (true)
            {
                int count = stream.Read(buffer, 0, buffer.Length);

                if (count == 0)
                {
                    DateTime current = DateTime.Now;

                    if (current.Subtract(start).TotalSeconds > 30)
                    {
                        // Read() keeps returning zero even if client has disconnected
                        // there's no reliable way to detect client disconnection
                        // assume 30 seconds of silence = disconnected (per Paul's recommendation)

                        throw new Exception("Client has disconnected");
                    }

                    Thread.Sleep(100);
                    continue;
                }

                string chunk = Encoding.UTF8.GetString(buffer, 0, count);
                sb.Append(chunk);

                if (sb.ToString().EndsWith("\r\n"))
                {
                    break;
                }
            }

            return sb.ToString().Trim();
        }


        internal void WriteMessage(TcpClient client, string msg)
        {
            NetworkStream stream = client.GetStream();

            byte[] resp = Encoding.UTF8.GetBytes(msg);
            stream.Write(resp, 0, resp.Length);
        }
    }
}
