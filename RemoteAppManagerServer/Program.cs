﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RemoteAppManagerServer
{
    class Program
    {
        AppManagerServer _server;
        static void Main(string[] args)
        {
            AppManagerServer server = new AppManagerServer();
            server.Listen();
        }
    }
}
