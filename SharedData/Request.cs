using System;
using System.Collections.Generic;
using System.Text;

namespace SharedData
{
    public class Request
    {
        public Request(short id, Commands command, params byte[] body)
        {
            Command = command;
            Id = id;
            Body = body;
        }
        
        public Commands Command { get; set; }
        public byte[] Body { get; set; }
        public short Id { get; set; }
        public DateTime? Sended { get; set; }

        public byte[] GetBytes()
        {
            byte[] bytes = new byte[(Body?.Length ?? 0) + 3];
            bytes[0] = Convert.ToByte(Id / 256);
            bytes[1] = Convert.ToByte(Id % 256);
            bytes[2] = (byte)Command;
            if (Body != null && Body.Length > 0)
            {
                Body.CopyTo(bytes, 3);
            }
            return bytes;
        }

        public static Request GetRequest(byte[] b)
        {
            if (b == null || b.Length < 3)
                return null;
            var res = new Request(Convert.ToInt16(b[0] * 256 + b[1]), (Commands)b[2]);
            if (b.Length > 3)
            {
                var a = new byte[b.Length - 3];
                Array.Copy(b, 3, a, 0, a.Length);
                res.Body = a;
            }
            return res;
        }
    }
}
