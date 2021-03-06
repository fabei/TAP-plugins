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
using OpenTap;
using Newtonsoft.Json;

namespace Tap.Plugins._5Genesis.Prometheus.Instruments
{
    public class PrometheusReply
    {
        public HttpStatusCode Status { get; set; }

        public string StatusDescription { get; set; }

        public string Message
        {
            get
            {
                if (!string.IsNullOrEmpty(Content))
                {
                    dynamic json = JsonConvert.DeserializeObject(Content);
                    string status = json["status"].ToString();
                    if (status == "error")
                    {
                        string errorType = json["errorType"];
                        string message = json["error"];

                        return $"Error: {errorType} - {message}";
                    }
                    else { return status; }
                }
                else { return "<Reply has no Content>"; }
            }
        }

        public string Content { get; set; }

        public bool Success
        {
            get { return ((int)Status >= 200) && ((int)Status <= 299); }
        }

        public IEnumerable<ResultTable> Results
        {
            get
            {
                if (Success)
                {
                    dynamic json = JsonConvert.DeserializeObject(Content);
                    dynamic data = json["data"];
                    dynamic resultsList = data["result"];
                    foreach (dynamic result in resultsList)
                    {
                        yield return getResultTable(result);
                    }
                };
            }
        }

        private ResultTable getResultTable(dynamic result)
        {
            // Extract the available metadata from the "metric" dictionary
            Dictionary<string, string> metadata = new Dictionary<string, string>();
            foreach (var entry in result["metric"])
            {
                metadata[entry.Name] = entry.Value.ToString();
            }

            // Extract "values". 
            List<double> timestamps = new List<double>();
            List<string> datetimes = new List<string>();
            List<IConvertible> values = new List<IConvertible>();

            foreach (var point in result["values"])
            {
                double timestamp = double.Parse(point.First.ToString());
                DateTime datetime = DateTimeOffset.FromUnixTimeMilliseconds((long)(timestamp * 1000)).DateTime;
                timestamps.Add(timestamp);
                datetimes.Add(datetime.ToString(PrometheusInstrument.TimeFormat));
                values.Add(this.toIConvertible(point.Last.ToString()));
            }

            // Create columns for UNIX timestamp, local datetime and value
            string name = metadata.ContainsKey("__name__") ? metadata["__name__"] : "Prometheus result";
            ResultColumn timestampColumn = new ResultColumn("Timestamp", timestamps.ToArray());
            ResultColumn datetimesColumn = new ResultColumn("DateTime", datetimes.ToArray());
            ResultColumn valuesColumn = new ResultColumn(name, values.ToArray());

            // Create a column for each metadata value, repeated for every row
            List<ResultColumn> resultColumns = new List<ResultColumn>();
            foreach (var item in metadata)
            {
                ResultColumn column = new ResultColumn(item.Key, Enumerable.Repeat(item.Value, timestamps.Count).ToArray());
                resultColumns.Add(column);
            }

            resultColumns.AddRange(new ResultColumn[] { timestampColumn, datetimesColumn, valuesColumn });

            return new ResultTable(name, resultColumns.ToArray());
        }

        private IConvertible toIConvertible(string value)
        {
            if (long.TryParse(value, out long parsedLong)) { return parsedLong; }
            if (double.TryParse(value, out double parsedDouble)) { return parsedDouble; }
            if (bool.TryParse(value, out bool parsedBool)) { return parsedBool; }
            return value;
        }
    }
}
