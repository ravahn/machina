using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Machina
{
    interface IRawSocket
    {
        uint LocalIP
        { get; }
        uint RemoteIP
        { get; }
        void Create(uint localAddress, uint remoteAddress = 0);
        int Receive(out byte[] buffer);
        void Destroy();
    }
}
