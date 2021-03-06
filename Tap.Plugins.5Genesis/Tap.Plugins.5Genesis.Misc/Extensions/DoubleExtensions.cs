﻿// Author:      Bruno Garcia Garcia <bgarcia@lcc.uma.es>
// Copyright:   Copyright 2019-2020 Universidad de Málaga (University of Málaga), Spain
//
// This file is part of the 5GENESIS project. The 5GENESIS project is funded by the European Union’s
// Horizon 2020 research and innovation programme, grant agreement No 815178.
//
// This file cannot be modified or redistributed. This header cannot be removed.

using System;

namespace Tap.Plugins._5Genesis.Misc.Extensions
{
    public static class DoubleExtensions
    {
        public static DateTime ToDateTime(this double timestamp)
        {
            DateTimeOffset offset = DateTimeOffset.FromUnixTimeMilliseconds((long)(timestamp * 1000));
            return offset.UtcDateTime;
        }
    }
}
