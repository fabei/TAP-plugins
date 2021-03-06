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
using OpenTap;
using System.IO;
using System.Text.RegularExpressions;

namespace Tap.Plugins._5Genesis.Y1Demo.Steps
{
    public class MeasurementPoint
    {
        private static Regex regex = new Regex(@".*(KPI[12]_PERFORMANCE)\s*,\s*([\s\w]*)\s*,\s*(\d+)(\s*,\s*(\d+))?", RegexOptions.Compiled);

        public enum EKpi {ERR, KPI1, KPI2}
        public enum EMessage { TokenRequest, TokenGranted, Invite, Ok}

        public EKpi Kpi { get; private set; }

        public EMessage Message { get; private set; }

        public long Timestamp { get; private set; }

        public long Sequence { get; private set; }

        public MeasurementPoint(string line)
        {
            Match match = regex.Match(line);
            if (match.Success)
            {
                this.Kpi = match.Groups[1].Value.Contains("1") ? EKpi.KPI1 : EKpi.KPI2;

                switch (match.Groups[2].Value)
                {
                    case "TOKEN REQUEST": this.Message = EMessage.TokenRequest; break;
                    case "TOKEN GRANTED": this.Message = EMessage.TokenGranted; break;
                    case "INVITE": this.Message = EMessage.Invite; break;
                    case "200 OK": this.Message = EMessage.Ok; break;
                }

                this.Timestamp = long.Parse(match.Groups[3].Value);

                string maybeSequence = match.Groups[5].Value;
                if (!string.IsNullOrEmpty(maybeSequence))
                {
                    this.Sequence = long.Parse(maybeSequence);
                }
            }
        }

        public override string ToString()
        {
            return $"{Kpi} {Message} {Timestamp} {Sequence}";
        }
    }

    public class Point
    {
        public long Timestamp { get; private set; }
        public long Delay { get; private set; }
        public long UtcTimestamp { get; private set; }

        public Point(long timestamp, long delay)
        {
            Timestamp = timestamp;
            Delay = delay;
            DateTimeOffset local = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp);
            DateTimeOffset utc = TimeZoneInfo.ConvertTime(local, TimeZoneInfo.Local);
            UtcTimestamp = local.Subtract(utc.Offset).ToUnixTimeMilliseconds();
        }
    }

    public class KpiData
    {
        public int Total { get; set; }
        
        public List<Point> AccessTimes { get; set; }

        public List<Point> FailedTimes { get; set; }

        public KpiData()
        {
            AccessTimes = new List<Point>();
            FailedTimes = new List<Point>();
        }
    }

    [Display("Nemergent Parse Step", Groups: new string[] { "5Genesis", "Y1" }, 
        Description: "Parses Nemergent result files and publishes them as KPI")]
    public class NemergentParseStep : TestStep
    {
        #region Settings
        
        [FilePath(FilePathAttribute.BehaviorChoice.Open, "txt")]
        public string Kpi1File { get; set; }

        [FilePath(FilePathAttribute.BehaviorChoice.Open, "txt")]
        public string Kpi2File { get; set; }

        #endregion

        private KpiData AccessTime, E2ETime;

        public NemergentParseStep()
        {
            Kpi1File = string.Empty;
            Kpi2File = string.Empty;
        }

        public override void PrePlanRun()
        {
            base.PrePlanRun();
            AccessTime = new KpiData();
            E2ETime = new KpiData();
        }

        public override void Run()
        {
            getAccessTimes();
            
            getE2ETimes();

            publishResults();
        }

        public override void PostPlanRun()
        {
            AccessTime = null;
            E2ETime = null;
            base.PostPlanRun();
        }

        private void getAccessTimes()
        {
            bool requested = false;
            long requestTime = 0;

            foreach (MeasurementPoint point in getMeasurementPoints(Kpi1File))
            {
                if (point.Kpi == MeasurementPoint.EKpi.KPI1)
                {
                    switch (point.Message)
                    {
                        case MeasurementPoint.EMessage.TokenRequest:
                            if (requested) // There was a request without acknowledgement
                            {
                                AccessTime.FailedTimes.Add(new Point(requestTime, 0));
                            }
                            AccessTime.Total += 1;
                            requested = true;
                            requestTime = point.Timestamp;
                            break;
                        case MeasurementPoint.EMessage.TokenGranted:
                            if (requested)
                            {
                                AccessTime.AccessTimes.Add(new Point(requestTime, point.Timestamp - requestTime));
                            }
                            requested = false;
                            break;
                    }
                }
            }
        }

        private void getE2ETimes()
        {
            Dictionary<long, long> requests = new Dictionary<long, long>(); // Dictionary of <Sequence, Timestamp>

            foreach (MeasurementPoint point in getMeasurementPoints(Kpi2File))
            {
                if (point.Kpi == MeasurementPoint.EKpi.KPI2)
                {
                    switch (point.Message)
                    {
                        case MeasurementPoint.EMessage.Invite:
                            requests[point.Sequence] = point.Timestamp;
                            E2ETime.Total += 1;
                            break;
                        case MeasurementPoint.EMessage.Ok:
                            if (requests.ContainsKey(point.Sequence))
                            {
                                long startTime = requests[point.Sequence];
                                E2ETime.AccessTimes.Add(new Point(startTime, point.Timestamp - startTime));
                                requests.Remove(point.Sequence);
                            }
                            break;
                    }
                }
            }

            // Requests that never got an OK
            foreach (var pair in requests)
            {
                E2ETime.FailedTimes.Add(new Point(pair.Value, 0));
            }
        }

        private void publishResults()
        {
            if (AccessTime.Total != 0)
            {
                publishOne(AccessTime, "NEM_Access_Time_Success", "NEM_Access_Time_Failed", "NEM_Access_Time_Aggregated");
            }
            if (E2ETime.Total != 0)
            {
                publishOne(E2ETime, "NEM_E2E_Access_Success", "NEM_E2E_Access_Failed", "NEM_E2E_Access_Aggregated");
            }
        }

        private void publishOne(KpiData kpi, string successName, string failedName, string aggregatedName)
        {
            List<long> successTimestamps = new List<long>();
            List<long> delays = new List<long>();
            List<long> failedTimestamps = new List<long>();

            foreach (Point point in kpi.AccessTimes)
            {
                successTimestamps.Add(point.UtcTimestamp);
                delays.Add(point.Delay);
            }

            foreach (Point point in kpi.FailedTimes)
            {
                failedTimestamps.Add(point.UtcTimestamp);
            }

            // Aggregated values must be created near the rest of the results
            long aggregatedTimestamp = successTimestamps.Count != 0 ? successTimestamps[0] : failedTimestamps[0];

            Results.PublishTable(successName, new List<string>() { "Timestamp", "Delay" },
                successTimestamps.ToArray(), delays.ToArray());
            Results.PublishTable(failedName, new List<string>() { "Timestamp", "Zero" },
                failedTimestamps.ToArray(), new int[failedTimestamps.Count]);
            Results.Publish(aggregatedName, new List<string>() { "Timestamp", "Total", "Success", "Failed" },
                aggregatedTimestamp, kpi.Total, kpi.AccessTimes.Count, kpi.FailedTimes.Count);
        }

        private IEnumerable<MeasurementPoint> getMeasurementPoints(string file)
        {
            foreach (string line in File.ReadLines(file))
            {
                yield return new MeasurementPoint(line);
            }
        }
    }
}
