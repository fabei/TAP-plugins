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
using System.Text;
using System.ComponentModel;
using Keysight.Tap;

using Tap.Plugins._5Genesis.InfluxDB.ResultListeners;

namespace Tap.Plugins._5Genesis.InfluxDB.Steps
{
    [Display("Set Experiment ID", Groups: new string[] { "5Genesis", "InfluxDB" })]
    public class SetExperimentIdStep : TestStep
    {
        #region Settings
        
        [Display("InfluxDB", Order: 1.0)]
        public InfluxDbResultListener ResultListener { get; set; }

        [Display("Experiment ID", Order: 1.1)]
        public string ExperimentId { get; set; }

        #endregion
        public SetExperimentIdStep() { }


        public override void Run()
        {
            if (string.IsNullOrWhiteSpace(ExperimentId))
            {
                Log.Error("Cannot set ExperimentId to an empty string.");
            }
            else
            {
                Log.Info($"Setting ExperimentId to {ExperimentId} ({ResultListener.Name})");
                ResultListener.ExperimentId = this.ExperimentId;
            }
        }
    }
}
