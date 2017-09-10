using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Configuration;
using System;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            Constants.SERVER_ID = ConfigurationManager.AppSettings["server_id"] ?? "UNDEFINED";
            Constants.SOFT_VERSION = ConfigurationManager.AppSettings["soft_version"] ?? "0.0.0.0";

            IPAddress ipaddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint point = new IPEndPoint(ipaddress, 100);

            List<Client> _Clients = new List<Client>();
            Socket socket = new Socket(SocketType.Stream, ProtocolType.Tcp);

            socket.Bind(point);
            socket.Listen(10);

            Task.Run(Waiting);
            Console.ReadKey();

            async Task Waiting()
            {
                while (true)
                {
                    var s = socket.Accept();
                    Client client = new Client(s);
                    client.Start();
                    _Clients.Add(client);
                }
            }
        }
    }
}
