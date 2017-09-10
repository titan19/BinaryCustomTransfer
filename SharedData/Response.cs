using System;
using System.Collections.Generic;
using System.Text;

namespace SharedData
{
    public class Response
    {
        public Response(short id, ResponseCode code, params byte[] answer)
        {
            Id = id;
            ResultCode = code;
            Answer = answer;
        }

        public ResponseCode ResultCode { get; set; }
        public byte[] Answer { get; set; }
        public short Id { get; set; }

        public DateTime? Received { get; set; }
        public Request Request { get; set; }
        public TimeSpan? FullTime {
            get {
                if(Received == null || 
                    Request == null || 
                    Request.Sended == null)
                return null;
                return Received - Request.Sended;
            }
        }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[(Answer?.Length ?? 0) + 3];
            bytes[0] = Convert.ToByte(Id / 256);
            bytes[1] = Convert.ToByte(Id % 256);
            bytes[2] = (byte)ResultCode;
            if (Answer != null && Answer.Length > 0)
            {
                Answer.CopyTo(bytes, 3);
            }
            return bytes;
        }

        public static Response GetResponse(byte[] b)
        {
            if (b == null || b.Length < 3)
                return null;
            Response res = new Response(Convert.ToInt16(b[0] * 256 + b[1]), (ResponseCode)b[2]);
            if (b.Length > 3)
            {
                var a = new byte[b.Length - 3];
                Array.Copy(b, 3, a, 0, a.Length);
                res.Answer = a;
            }
            return res;
        }
    }
}
