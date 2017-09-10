using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            IPAddress ipaddress = new IPAddress(new byte[] { 127, 0, 0, 1 });
            IPEndPoint point = new IPEndPoint(ipaddress, 100);

            Console.WriteLine("Trying to connect to server...");

            Server server = Server.Connect(point);

            if (server == null)
            {
                Console.WriteLine("Can't connect to server.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Connected to server.");

            Console.WriteLine("Initializing handshaking...");
            var r1 = server.doHandshake();
            var r1answer = new String(Encoding.Unicode.GetChars(r1.Answer));
            Console.WriteLine("Result code: {0}. Answer is: {1}", r1.ResultCode, r1answer);

            Console.WriteLine("Requesting common 3 random bytes...");
            var r2 = server.doCommon(3);
            Console.WriteLine("Result code: {0}. Count of received bytes: {1}", r2.ResultCode, r2.Answer?.Length ?? 0);

            Console.WriteLine("Ping...");
            var r3 = server.CheckPing();
            Console.WriteLine("Time: {0}. Delivered:{1}% (10 packages)", r3.Item1, r3.Item2);

            Console.WriteLine("Telling Goodbye...");
            var r4 = server.Goodbye();
            Console.WriteLine("Result: {0}", r4.ResultCode);

            Console.WriteLine("Trying to send request after closing...");
            var r5 = server.doCommon(3);
            Console.WriteLine("Result code: {0}.", r5.ResultCode);

            Console.WriteLine("Trying to connect to server second time...");

            server = Server.Connect(point);

            if (server == null)
            {
                Console.WriteLine("Can't connect to server.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Connected to server.");

            Console.WriteLine("Initializing second handshaking...");
            var r6 = server.doHandshake();
            var r6answer = new String(Encoding.Unicode.GetChars(r6.Answer));
            Console.WriteLine("Result code: {0}. Answer is: {1}", r6.ResultCode, r6answer);

            Console.WriteLine("Telling Goodbye...");
            var r7 = server.Goodbye();
            Console.WriteLine("Result: {0}", r7.ResultCode);


            Console.WriteLine("Trying to connect to unavaliable server...");

            server = Server.Connect(null);

            if (server != null)
            {
                Console.WriteLine("Oops, you are connected to unavaliable server.");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("You can't connect to server.");

            Console.WriteLine("All tests are passed. Congratulations!");

            Console.ReadKey();
        }
    }
}
