﻿// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain
//
// This file is part of the 5GENESIS project. The 5GENESIS project is funded by the European Union’s
// Horizon 2020 research and innovation programme, grant agreement No 815178.
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.IO.Compression;
using Newtonsoft.Json.Linq;

using Tap.Plugins._5Genesis.Misc.Extensions;

namespace Tap.Plugins._5Genesis.RemoteAgents.Instruments
{
    public class iPerfResult
    {
        public double Jitter { get; set; }

        public double PacketLoss { get; set; }

        public double Throughput { get; set; }

        public double Timestamp { get; set; }

        public DateTime DateTime {
            get { return Timestamp.ToDateTime(); }
        }
    }
}
