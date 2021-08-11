using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Text;

namespace TcpServer
{
    class Server
    {
        private Settings settings;
        private TcpListener server;
        private bool isListening;

        private string folder;
        private long sessionId;

        public Server(Settings settings)
        {
            this.settings = settings;
            server = new TcpListener(settings.Ip.ToLower() == "any" ? IPAddress.Any : IPAddress.Parse(settings.Ip), settings.Port);
        }


        public void StartListening()
        {
            server.Start();
            isListening = true;

            while(isListening)
            {
                Console.WriteLine("TCP Server:\tWaiting for instrument connection");

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
                Console.WriteLine($"TCP Server:\tInstrument connected, session id = {id}");

                Thread t = new Thread(new ParameterizedThreadStart(HandleClient));
                t.Start(new { TcpClient = client, SessionId = id});

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

            try
            {
                while (true)
                {
                    string msg = ReadNextMessage(client);

                    if (msg.ToLower() == String.Format("{0}{1}{2}", "\x0B", "stop", "\x1C\r"))
                    {
                        client.Close();
                        StopListening();

                        return;
                    }

                    string inGuid = ExtractGuid(msg);
                    Console.WriteLine($"Session {id}:\tMessage received, GUID = {inGuid}");

                    string file = GetTempFileName();
                    byte[] fileContent = Encoding.UTF8.GetBytes(msg);

                    File.WriteAllBytes(file, fileContent);
                    Console.WriteLine($"Session {id}:\tMessage saved, file = {Path.GetFileName(file)}");

                    WriteAck(client, inGuid);
                    Console.WriteLine($"Session {id}:\tACK sent");
                }
            }
            catch
            {
                Console.WriteLine($"Session {id}:\tInstrument disconnected");
                client.Close();
            }
        }


        private string GetTempFileName()
        {
            if(String.IsNullOrEmpty(folder))
            {
                string loc = Assembly.GetExecutingAssembly().Location;
                string parent = Path.GetDirectoryName(loc);

                folder = Path.Combine(parent, "Data");
                Directory.CreateDirectory(folder);
            }

            string file = Guid.NewGuid().ToString().Replace("-", "") + ".txt";
            return Path.Combine(folder, file);
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

                    if(current.Subtract(start).TotalSeconds > 30)
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

                if (sb.ToString().EndsWith("\x1C\r"))
                {
                    break;
                }
            }

            return sb.ToString();
        }

        private string ExtractGuid(string msg)
        {
            string[] tokens = msg.Split('|');
            return tokens.Length > 9 ? tokens[9] : "";
        }


        private void WriteAck(TcpClient client, string inGuid)
        {
            NetworkStream stream = client.GetStream();
            string outGuid = Guid.NewGuid().ToString().Replace("-", "");

            string ack = String.Format("\v{0}{1}{2}{3}\r\x1C\r",
                                        @"MSH|^~\&|BigData EMR|Silver Hill Hospital|Abbott ID NOW||20190415204503||ACK^R01^ACK|",
                                        outGuid,
                                        "|Q|2.6\rMSA|AA|",
                                        inGuid);


            //string template = "\v" + @"MSH|^~\&|BigData EMR|Silver Hill Hospital|Abbott ID NOW||20190415204503||ACK^R01^ACK|" + outGuid +
            //                "|Q|2.6\rMSA|AA|" + inGuid + "\r\x1C\r";

            byte[] resp = Encoding.UTF8.GetBytes(ack);
            stream.Write(resp, 0, resp.Length);
        }
    }
}
