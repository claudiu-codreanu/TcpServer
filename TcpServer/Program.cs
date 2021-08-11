using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace TcpServer
{
    class Program
    {
        static Server2 m_server;
        internal static Dictionary<string, TcpClient> m_clients;

        static void Main(string[] args)
        {
            Console.WriteLine("TCP Server has started.");
            Settings settings = ParseCmdLineArgs(args);

            Thread t = new Thread(new ParameterizedThreadStart(DoWork));
            t.Start(settings);

            Console.WriteLine();
            Console.WriteLine("To stop the TCP server:\t\t\tstop![Enter]");
            Console.WriteLine("To send messages to session N:\t\tN: message goes here...[Enter]");

            Console.WriteLine();

            while(true)
            {
                string cmd = Console.ReadLine();

                if(cmd == "stop!")
                {
                    if (m_server != null)
                    {
                        m_server.StopListening();
                        m_clients.Clear();

                        m_server = null;
                        m_clients = null;
                    }

                    break;
                }


                Match m = Regex.Match(cmd, @"^([\d]+):\s([\w\W]+)$");

                if(m.Success)
                {
                    string session = m.Groups[1].Value;
                    string msg = m.Groups[2].Value;

                    if (!m_clients.ContainsKey(session))
                    {
                        Console.WriteLine(String.Format("Client session not found: {0}", session));
                    }
                    else
                    {
                        TcpClient client = m_clients[session];

                        if(File.Exists(msg))
                        {
                            msg = File.ReadAllText(msg);
                        }

                        //string file = @"C:\temp\stuff\misc\ResponseString.txt";
                        //string data = System.IO.File.ReadAllText(file);

                        m_server.WriteMessage(client, msg + "\r\n");
                       // m_server.WriteMessage(client, data);

                        Console.WriteLine("Message sent");
                    }

                    continue;
                }

                Console.WriteLine("Command not known");
            }
        }

        private static void DoWork(object data)
        {
            Settings settings = (Settings)data;

            //Server server = new Server(settings);
            m_server = new Server2(settings);
            m_clients = new Dictionary<string, TcpClient>();

            m_server.StartListening();
        }


        private static Settings ParseCmdLineArgs(string[] args)
        {
            Settings settings = new Settings();
            settings.Encoding = "ASCII";

            string key = "";

            for(var i = 0; i < args.Length; i++)
            {
                string arg = args[i];

                if(arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    key = arg.Replace("-", "").Replace("/", "").ToLower();
                    continue;
                }


                if(key.StartsWith("p"))
                {
                    settings.Port = int.Parse(arg);
                }
                else if(key.StartsWith("i"))
                {
                    settings.Ip = arg;
                }
                else if(key.StartsWith("e"))
                {
                    settings.Encoding = arg;
                }
            }

            return settings;
        }
    }
}
