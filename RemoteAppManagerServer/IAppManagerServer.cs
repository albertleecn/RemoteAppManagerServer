﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteAppManagerServer
{
    interface IAppManagerServer
    {
        void Listen();
        void AcceptCallback(IAsyncResult ar);
        void RequestProcesses();
        void RequestKillProcess(int processID);
        void RequestStartProcess(String process);
    }
}