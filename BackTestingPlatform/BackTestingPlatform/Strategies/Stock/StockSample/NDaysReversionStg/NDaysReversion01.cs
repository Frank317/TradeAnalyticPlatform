﻿using Autofac;
using BackTestingPlatform.Core;
using BackTestingPlatform.DataAccess;
using BackTestingPlatform.DataAccess.Futures;
using BackTestingPlatform.DataAccess.Option;
using BackTestingPlatform.DataAccess.Stock;
using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Model.Option;
using BackTestingPlatform.Model.Positions;
using BackTestingPlatform.Model.Signal;
using BackTestingPlatform.Model.Stock;
using BackTestingPlatform.Transaction;
using BackTestingPlatform.Transaction.MinuteTransactionWithSlip;
using BackTestingPlatform.Utilities;
using BackTestingPlatform.Utilities.Option;
using BackTestingPlatform.Utilities.TimeList;
using NLog;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BackTestingPlatform.Strategies.Stock.StockSample;
using BackTestingPlatform.Strategies.Stock.StockSample01;
using BackTestingPlatform.Utilities.Common;
using BackTestingPlatform.AccountOperator.Minute;
using System.Windows.Forms;
using BackTestingPlatform.Charts;

namespace BackTestingPlatform.Strategies.Stock.StockSample
{
    public class NDaysReversion01
    {
        static Logger log = LogManager.GetCurrentClassLogger();
        private DateTime startDate, endDate;
        public NDaysReversion01(int start, int end)
        {
            startDate = Kit.ToDate(start);
            endDate = Kit.ToDate(end);
        }
        //回测参数设置
        private double initialCapital = 10000000;
        private double slipPoint = 0.000;
        private static int contractTimes = 100;

        //策略参数设定
        private int period = 1;//应用周期
        private int NDays = 6 * 1;//5分钟级别
        private int lengthOfBackLooking = 120;//回看周期
        private double toleranceDegree = 0.01;//容忍度，允许破位的幅度
        string targetVariety = "510050.SH";
        /// <summary>
        /// 50ETF择时策略测试，N-Days Reversion
        /// </summary>
        public void compute()
        {
            log.Info("开始回测(回测期{0}到{1})", Kit.ToInt_yyyyMMdd(startDate), Kit.ToInt_yyyyMMdd(endDate));

            ///账户初始化
            //初始化positions
            SortedDictionary<DateTime, Dictionary<string, PositionsWithDetail>> positions = new SortedDictionary<DateTime, Dictionary<string, PositionsWithDetail>>();
            //初始化Account信息
            BasicAccount myAccount = new BasicAccount();
            myAccount.totalAssets = initialCapital;
            myAccount.freeCash = myAccount.totalAssets;
            //记录历史账户信息
            List<BasicAccount> accountHistory = new List<BasicAccount>();
            //记录benchmark数据
            List<double> benchmark = new List<double>();

            ///数据准备
            //交易日信息
            List<DateTime> tradeDays = DateUtils.GetTradeDays(startDate, endDate);
            //50etf分钟数据准备，取全回测期的数据存放于data
            Dictionary<string, List<KLine>> data = new Dictionary<string, List<KLine>>();
            foreach (var tempDay in tradeDays)
            {
                var ETFData = Platforms.container.Resolve<StockMinuteRepository>().fetchFromLocalCsvOrWindAndSave(targetVariety, tempDay);
                if (!data.ContainsKey(targetVariety))
                    data.Add(targetVariety, ETFData.Cast<KLine>().ToList());
                else
                    data[targetVariety].AddRange(ETFData.Cast<KLine>().ToList());
            }

            //频率转换测试
            List<KLine> data_5min = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Minutely", 5);
            List<KLine> data_15min = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Minutely", 15);
            //List<KLine> data_1Day = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Daily", 1);
            //List<KLine> data_1Month = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Monthly", 1);
            // List<KLine> data_1Week = MinuteFrequencyTransferUtils.MinuteToNPeriods(data[targetVariety], "Weekly", 1);

            int indexOfNow = -1;//记录整个data的索引，一分钟K线上的索引
            int indexOf5min = 0;//五分钟K线上的索引
            int indexOf15min = 0;//十五分钟K线上的索引

            ///回测循环
            //回测循环--By Day
            foreach (var day in tradeDays)
            {
                //取出当天的数据
                Dictionary<string, List<KLine>> dataToday = new Dictionary<string, List<KLine>>();
                foreach (var variety in data)
                {
                    dataToday.Add(variety.Key, data[variety.Key].FindAll(s => s.time.Year == day.Year && s.time.Month == day.Month && s.time.Day == day.Day));
                }

                int index = 0;
                //交易开关设置，控制day级的交易开关
                bool tradingOn = true;//总交易开关
                bool openingOn = true;//开仓开关
                bool closingOn = true;//平仓开关

                //是否为回测最后一天
                bool isLastDayOfBackTesting = day.Equals(endDate);

                //回测循环 -- By Minute
                //不允许在同一根1minBar上开平仓
                while (index < 240)
                {
                    int nextIndex = index + 1;
                    //更新索引值
                    indexOfNow++;
                    //N分钟k线的当前索引值是当前时间的之前第一个完整的k线，为避免data snooping不取当前时间的索引
                    indexOf5min = index / 5;
                    indexOf15min = index / 15;
                    
                    DateTime now = TimeListUtility.IndexToMinuteDateTime(Kit.ToInt_yyyyMMdd(day), index);
                    Dictionary<string, MinuteSignal> signal = new Dictionary<string, MinuteSignal>();
                    DateTime next = new DateTime();
                    myAccount.time = now;
                    //int indexOfNow = data[targetVariety].FindIndex(s => s.time == now);
                    double nowClose = dataToday[targetVariety][index].close;
                    //五分钟反转点
                 //   double nowDownReversionPoint = ComputeReversionPoint.findDownReversionPoint(data_5min, NDays, lengthOfBackLooking);
                    double nowUpReversionPoint = 0;

                    //实际操作从第一个回望期后开始
                    if (indexOfNow < lengthOfBackLooking - 1)
                    {
                        index = nextIndex;
                        continue;
                    }

                    try
                    {
                        //持仓查询，先平后开
                        //若当前有持仓 且 允许平仓
                        //是否是空仓,若position中所有品种volum都为0，则说明是空仓     
                        bool isEmptyPosition = positions.Count != 0 ? positions[positions.Keys.Last()].Values.Sum(x => Math.Abs(x.volume)) == 0 : true;
                        //若当前有持仓且允许交易
                        if (!isEmptyPosition && closingOn)
                        {
                            ///平仓条件
                            /// （1）若当前为 回测结束日 或 tradingOn 为false，平仓
                            /// （2）若当前下穿下反转点*（1-容忍度），平多                    
                            //（1）若当前为 回测结束日 或 tradingOn 为false，平仓
                            if (isLastDayOfBackTesting || tradingOn == false)
                                next = MinuteCloseAllPositonsWithSlip.closeAllPositions(dataToday, ref positions, ref myAccount, now: now, slipPoint: slipPoint);
                            //（2）若当前下穿下反转点*（1-容忍度），平多
                   //         else if (data[targetVariety][indexOfNow - 1].close >= nowDownReversionPoint * (1 - toleranceDegree) && nowClose < nowDownReversionPoint * (1 - toleranceDegree))
                                next = MinuteCloseAllPositonsWithSlip.closeAllPositions(dataToday, ref positions, ref myAccount, now: now, slipPoint: slipPoint);
                        }
                        //空仓 且可交易 可开仓
                        else if (isEmptyPosition && tradingOn && openingOn)
                        {
                            ///开仓条件
                            /// 可用资金足够，且出现上反转信号
                            double nowFreeCash = myAccount.freeCash;
                            //开仓量，满仓梭哈
                            double openVolume = Math.Truncate(nowFreeCash / data[targetVariety][indexOfNow].close / contractTimes) * contractTimes;
                            //若剩余资金至少购买一手 且 出上反转信号 开仓
                            if (openVolume >= 1 && data[targetVariety][indexOfNow - 1].close <= nowUpReversionPoint * (1 + toleranceDegree) && nowClose > nowUpReversionPoint * (1 + toleranceDegree))
                            {
                                MinuteSignal openSignal = new MinuteSignal() { code = targetVariety, volume = openVolume, time = now, tradingVarieties = "stock", price = dataToday[targetVariety][index].close, minuteIndex = index };
                                signal.Add(targetVariety, openSignal);
                                next = MinuteTransactionWithSlip.computeMinuteOpenPositions(signal, dataToday, ref positions, ref myAccount, slipPoint: slipPoint, now: now);
                                //当天买入不可卖出
                                closingOn = false;
                            }
                        }

                        //账户信息更新
                        AccountUpdatingForMinute.computeAccountUpdating(ref myAccount, positions, now, dataToday);
                    }

                    catch (Exception)
                    {
                        throw;
                    }
                    nextIndex = Math.Max(nextIndex, TimeListUtility.MinuteToIndex(next));
                    index = nextIndex;
                }
                //账户信息记录By Day            
                //用于记录的临时账户
                BasicAccount tempAccount = new BasicAccount();
                tempAccount.time = myAccount.time;
                tempAccount.freeCash = myAccount.freeCash;
                tempAccount.margin = myAccount.margin;
                tempAccount.positionValue = myAccount.positionValue;
                tempAccount.totalAssets = myAccount.totalAssets;
                accountHistory.Add(tempAccount);
                //抓取benchmark
                benchmark.Add(dataToday[targetVariety].Last().close);

                //显示当前信息
                Console.WriteLine("Time:{0,-8:F},netWorth:{1,-8:F3}", day, myAccount.totalAssets / initialCapital);
            }

            //遍历输出到console   
            /*
            foreach (var account in accountHistory)
                Console.WriteLine("time:{0,-8:F}, netWorth:{1,-8:F3}\n", account.time, account.totalAssets / initialCapital);
             */
            //策略绩效统计及输出
            PerformanceStatisics myStgStats = new PerformanceStatisics();
            myStgStats = PerformanceStatisicsUtils.compute(accountHistory, positions, benchmark.ToArray());

            //统计指标在console 上输出
            Console.WriteLine("--------Strategy Performance Statistics--------\n");
            Console.WriteLine(" netProfit:{0,-3:F} \n totalReturn:{1,-3:F} \n anualReturn:{2,-3:F} \n anualSharpe :{3,-3:F} \n winningRate:{4,-3:F} \n PnLRatio:{5,-3:F} \n maxDrawDown:{6,-3:F} \n maxProfitRatio:{7,-3:F} \n informationRatio:{8,-3:F} \n alpha:{9,-3:F} \n beta:{10,-3:F} \n averageHoldingRate:{11,-3:F} \n", myStgStats.netProfit, myStgStats.totalReturn, myStgStats.anualReturn, myStgStats.anualSharpe, myStgStats.winningRate, myStgStats.PnLRatio, myStgStats.maxDrawDown, myStgStats.maxProfitRatio, myStgStats.informationRatio, myStgStats.alpha, myStgStats.beta, myStgStats.averageHoldingRate);
            Console.WriteLine("-----------------------------------------------\n");

            //画图
            Dictionary<string, double[]> line = new Dictionary<string, double[]>();
            double[] netWorth = accountHistory.Select(a => a.totalAssets / initialCapital).ToArray();
            line.Add("NetWorth", netWorth);

            //benchmark净值
            List<double> netWorthOfBenchmark = benchmark.Select(x => x / benchmark[0]).ToList();
            line.Add("50ETF", netWorthOfBenchmark.ToArray());

            string[] datestr = accountHistory.Select(a => a.time.ToString("yyyyMMdd")).ToArray();
            Application.Run(new PLChart(line, datestr));
            /*
            //将accountHistory输出到csv
            var resultPath = ConfigurationManager.AppSettings["CacheData.ResultPath"] + "accountHistory.csv";
            var dt = DataTableUtils.ToDataTable(accountHistory);          // List<MyModel> -> DataTable
            CsvFileUtils.WriteToCsvFile(resultPath, dt);	// DataTable -> CSV File
           */

            Console.ReadKey();
        }


    }
}
