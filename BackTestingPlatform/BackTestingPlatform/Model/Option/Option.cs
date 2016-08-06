﻿using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Model.Futures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.Model.Option
{
    /// <summary>
    /// 期权 基础信息
    /// </summary>
    public class OptionInfo
    {
        public string optionCode { get; set; }
        public string optionName { get; set; }
        public string executeType { get; set; }
        public double strike { get; set; }
        public string optionType { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }

        //public Dictionary<DateTime, string> IHcodes;
 
    }



    public class OptionTickData 
    {

        public DateTime time;
        public double lastPrice;
        public PositionData[] ask, bid;
        public OptionGreek greek;
        public StockTickData underlyingStock;
        public FuturesTickData underlyingFutures;

    }

    public struct OptionGreek
    {
        public double sigma, delta, gamma, vega, theta;
    }

    public struct OptionMinuteData
    {
        public DateTime time;
        public double open, high, low, close, volume, amount;
    }

}
