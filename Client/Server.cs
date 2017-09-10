using SharedData;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Security;
using System.Threading.Tasks;

namespace Client
{
    public class Server
    {
        private ConcurrentDictionary<short, object> ObjectsQueue { get; set; }
            = new ConcurrentDictionary<short, object>();
        private short ID = 1;

        private Socket Socket { get; set; }
        private Task _Receiving = null;
        private bool IsConnected { get; set; }
        private Server(IPEndPoint point)
        {
            Socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            Socket.Connect(point);
            IsConnected = true;
            _Receiving = Task.Run(Receiving);
        }
        public static Server Connect(IPEndPoint ip)
        {
            try
            {
                Server serv = new Server(ip);
                return serv;
            }
            catch
            {
                return null;
            }
        }
        /// <summary>
        ///Handshake with server
        /// </summary>
        /// <returns>string formatted as "{0};{1}". {0} - server id, {1} - soft version</returns>
        public Response doHandshake()
        {
            var a = Task.Run(doHandshakeAsync);
            a.Wait();
            return a.Result;
        }
        /// <summary>
        ///Handshake with server
        /// </summary>
        /// <returns>string formatted as "{0};{1}". {0} - server id, {1} - soft version</returns>
        public async Task<Response> doHandshakeAsync()
        {
            Request req = new Request(ID++, Commands.Handshake);
            return await Send(req);
        }

        /// <summary>
        /// Common action
        /// </summary>
        /// <param name="count">requested count of random bytes</param>
        /// <returns>random bytes</returns>
        public Response doCommon(byte count)
        {
            var a = Task.Run(() => doCommonAsync(count));
            a.Wait();
            return a.Result;
        }
        /// <summary>
        /// Common action
        /// </summary>
        /// <param name="count">requested count of random bytes</param>
        /// <returns>random bytes</returns>
        public async Task<Response> doCommonAsync(byte count)
        {
            Request req = new Request(ID++, Commands.Common, count);
            return await Send(req);
        }

        /// <summary>
        /// Send single ping request
        /// </summary>
        /// <returns>simply response</returns>
        public Response doPingRequest()
        {
            var a = Task.Run(doPingRequestAsync);
            a.Wait();
            return a.Result;
        }
        /// <summary>
        /// Send single ping request
        /// </summary>
        /// <returns>simply response</returns>
        public async Task<Response> doPingRequestAsync()
        {
            Request req = new Request(ID++, Commands.Ping);
            return await Send(req);
        }

        /// <summary>
        /// Checking connection to server
        /// </summary>
        /// <returns>time of all request and delivered percent</returns>
        public Tuple<TimeSpan, double> CheckPing()
        {
            var a = Task.Run(CheckPingAsync);
            a.Wait();
            return a.Result;
        }
        /// <summary>
        /// Checking connection to server
        /// </summary>
        /// <returns>time of all request and delivered percent</returns>
        public async Task<Tuple<TimeSpan, double>> CheckPingAsync()
        {
            DateTime st = DateTime.Now;
            int lost = 0;
            for (int i = 0; i < 10; i++)
            {
                Request req = new Request(ID++, Commands.Ping);
                Response res = await Send(req);
                if (res == null)
                    lost++;
            }
            DateTime end = DateTime.Now;
            return new Tuple<TimeSpan, double>(end - st, 100.0 - lost * 10.0);
        }

        /// <summary>
        /// Say to server that connection can been closed
        /// </summary>
        /// <returns>simply response</returns>
        public Response Goodbye()
        {
            var a = Task.Run(() => Send(new Request(ID++, Commands.Goodbye)));
            a.Wait();
            return a.Result;
        }

        /// <summary>
        /// Sending request
        /// </summary>
        /// <param name="req">request</param>
        /// <returns>Response of server</returns>
        private async Task<Response> Send(Request req)
        {
            ObjectsQueue.TryAdd(req.Id, req);

            byte[] estart = BitConverter.GetBytes((int)Edges.Start);
            byte[] eend = BitConverter.GetBytes((int)Edges.End);
            byte[] mess = req.GetBytes();
            byte[] outp = new byte[mess.Length + 8];

            estart.CopyTo(outp, 0);
            mess.CopyTo(outp, 4);
            eend.CopyTo(outp, outp.Length - 4);

            Socket.Send(outp);
            req.Sended = DateTime.Now;
            return await WaitFor(req.Id);
        }

        private async Task<Response> WaitFor(short id)
        {
            int times = 0;
            while (ObjectsQueue.ContainsKey(id))
            {
                object ob = null;
                ObjectsQueue.TryGetValue(id, out ob);
                if (ob is Response)
                    return ob as Response;
                else
                    times++;
                if (times >= 100)
                    return new Response(id, ResponseCode.Timeout);
                await Task.Delay(50);
            }
            return new Response(id, ResponseCode.Error);
        }

        private async Task Receiving()
        {
            byte[] buffer = new byte[0];
            while (IsConnected)
            {
                int size;
                while ((size = Socket.Available) == 0)
                {
                    await Task.Delay(50);
                }
                byte[] a = new byte[size];
                Socket.Receive(a);
                var pr = new byte[a.Length + buffer.Length];
                buffer.CopyTo(pr, 0);
                a.CopyTo(pr, buffer.Length);
                int st_to_bu = 0;
                int st_req = -1;

                byte[] estart = BitConverter.GetBytes((int)Edges.Start);
                byte[] eend = BitConverter.GetBytes((int)Edges.End);
                for (int i = 0; i < pr.Length; i++)
                {
                    if (i < pr.Length - 3
                        && pr[i] == estart[0]
                        && pr[i + 1] == estart[1]
                        && pr[i + 2] == estart[2]
                        && pr[i + 3] == estart[3])
                    {
                        st_req = i + 4;
                        i += 4;
                    }
                    else if (i < pr.Length - 3
                        && pr[i] == eend[0]
                        && pr[i + 1] == eend[1]
                        && pr[i + 2] == eend[2]
                        && pr[i + 3] == eend[3])
                    {
                        byte[] outp = new byte[i - st_req];
                        Array.Copy(pr, st_req, outp, 0, outp.Length);
                        Response res = Response.GetResponse(outp);
                        res.Received = DateTime.Now;

                        object ob = null;
                        ObjectsQueue.TryGetValue(res.Id, out ob);
                        res.Request = ob as Request;

                        ObjectsQueue.TryUpdate(res.Id, res, ob);
                        i += 4;
                        st_to_bu = i;
                    }
                }
                if (st_to_bu < pr.Length)
                {
                    buffer = new byte[pr.Length - st_to_bu];
                    Array.Copy(pr, st_to_bu, buffer, 0, buffer.Length);
                }
                else
                {
                    buffer = new byte[0];
                }
            }
        }
    }
}
