﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientNode
{
    class Singal
    {
        int time;
        int port;
        STM1 stm1;

        public Singal(int time, int port, STM1 stm1)
        {
            this.time = time;
            this.port = port;
            this.stm1 = stm1;
        }

    }
}
