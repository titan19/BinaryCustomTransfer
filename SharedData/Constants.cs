using System;
using System.Collections.Generic;
using System.Text;

namespace SharedData
{
    public enum Edges
    {
        Start = 0xAFAFAF,
        End = 0xCDCDCD,
    }
    public enum Commands
    {
        Common = 0xAA,
        Handshake = 0xF1,
        Goodbye = 0x1F,
        Ping = 0x81,
    }
    public enum ResponseCode
    {
        OK = 0xA1,
        Error = 0xA2,
        WrongRequest = 0xA3,
        Timeout = 0xA4,
    }
    public enum CommonAnswers
    {
        Handshake = 0x0E,
        Goodbye = 0xE0,
        Ping = 0x7E,
    }
}
