using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using SharedData;
using System.Text;

namespace Server
{
    public class Client
    {
        Socket Connection { get; set; }
        Task Task { get; set; }
        bool IsWorking { get; set; }
        private object _Locker { get; } = new object();

        public Client(Socket socket)
        {
            Connection = socket;
        }

        public bool Start()
        {
            IsWorking = true;
            if (Task == null)
            {
                Task = Task.Run(Receiving);
                return true;
            }
            return false;
        }

        private async Task<Response> GetAnswer(Request req)
        {
            Response res = null;
            if (req.Command == Commands.Handshake)
            {
                string answer = string.Format("{0};{1}", Constants.SERVER_ID, Constants.SOFT_VERSION);
                res = new Response(
                    req.Id,
                    ResponseCode.OK,
                    Encoding.Unicode.GetBytes(answer)
                );
            }
            else if (req.Command == Commands.Goodbye)
            {
                res = new Response(
                    req.Id,
                    ResponseCode.OK,
                    (byte)CommonAnswers.Goodbye
                );

                IsWorking = false;
            }
            else if (req.Command == Commands.Ping)
            {
                res = new Response(
                    req.Id,
                    ResponseCode.OK,
                    (byte)CommonAnswers.Ping
                );
            }
            else if (req.Command == Commands.Common)
            {
                Random rand = new Random();
                res = new Response(req.Id, ResponseCode.OK);
                if (req.Body.Length > 0)
                {
                    byte[] ar = new byte[req.Body[0]];
                    rand.NextBytes(ar);
                    res.Answer = ar;
                }
            }
            if (res != null)
                return res;
            return new Response(0, ResponseCode.WrongRequest);
        }

        private bool IsFeedbackNow = false;
        private async Task Feedback(Request req)
        {
            IsFeedbackNow = true;
            Response res = await GetAnswer(req);

            byte[] estart = BitConverter.GetBytes((int)Edges.Start);
            byte[] eend = BitConverter.GetBytes((int)Edges.End);
            byte[] mess = res.GetBytes();
            byte[] outp = new byte[mess.Length + 8];

            estart.CopyTo(outp, 0);
            mess.CopyTo(outp, 4);
            eend.CopyTo(outp, outp.Length - 4);

            lock (_Locker)
            {
                Connection.Send(outp);
            }
            IsFeedbackNow = false;
        }

        private async Task Receiving()
        {
            byte[] buffer = new byte[0];
            while (IsWorking)
            {
                int size;
                while ((size = Connection.Available) == 0)
                {
                    await Task.Delay(50);
                }

                byte[] a = new byte[size];
                Connection.Receive(a);
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
                        byte[] request = new byte[i - st_req];
                        Array.Copy(pr, st_req, request, 0, request.Length);

                        if (request != null && request.Length >= 3)
                        {
                            Request req = Request.GetRequest(request);
                            var t = Feedback(req);
                        }

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
            while(IsFeedbackNow)
            {
                await Task.Delay(15);
            }
            Connection.Close();
        }
    }
}
