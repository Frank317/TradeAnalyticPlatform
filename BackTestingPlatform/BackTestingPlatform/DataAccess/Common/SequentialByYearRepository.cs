﻿using BackTestingPlatform.Model.Common;
using BackTestingPlatform.Utilities;
using BackTestingPlatform.Utilities.Common;
using NLog;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.DataAccess
{
    /// <summary>
    /// 按每年存取时间序列数据的Repository
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SequentialByYearRepository<T> : SequentialRepository<T> where T : Sequential, new()
    {
        const string PATH_KEY = "CacheData.Path.SequentialByYear";
        static Logger log = LogManager.GetCurrentClassLogger();


        /// <summary>
        /// 尝试从Wind获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        protected abstract List<T> readFromWind(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null);


        /// <summary>
        /// 尝试从默认MSSQL源获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        protected abstract List<T> readFromDefaultMssql(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null);


        /// <summary>
        /// 尝试从本地csv文件获取数据,可能会抛出异常
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param>
        /// <param name="dateEnd">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> readFromLocalCsv(string code, DateTime date1, DateTime date2, string tag = null, IDictionary<string, object> options = null)
        {
            var filePath = _buildCacheDataFilePath(code, date1, date2, tag);
            return readFromLocalCsv(filePath);
        }

        /// <summary>
        /// 尝试从本地csv文件，Wind获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsv(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, true, false, false, false);
        }

        /// <summary>
        /// 尝试从Wind获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromWind(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, false, true, false, false);
        }

        /// <summary>
        /// 尝试从默认MSSQL源获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromMssql(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, false, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，Wind获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrWindAndSave(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, true, true, false, true);
        }
        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssql(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, true, false, true, false);
        }

        /// <summary>
        /// 先后尝试从本地csv文件，默认MSSQL数据库源获取数据。若无本地csv，则保存到CacheData文件夹。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromLocalCsvOrMssqlAndSave(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, true, false, true, true);
        }

        /// <summary>
        /// 尝试Wind获取数据。然后将数据覆盖保存到CacheData文件夹。
        /// 采用逐年保存的方式，每年对应一个csv文件。
        /// </summary>
        /// <param name="code">代码，如股票代码，期权代码</param>
        /// <param name="dateStart">开始时间，包含本身</param> 
        /// <param name="dateEnd">结束时间，包含本身</param>        
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="options">其他选项</param>
        /// <returns></returns>
        public List<T> fetchFromWindAndSave(string code, DateTime dateStart, DateTime dateEnd, string tag = null, IDictionary<string, object> options = null)
        {
            return fetch1(code, dateStart, dateEnd, tag, options, false, true, false, true);
        }

        /// <summary>
        /// 获取某一时间段的所有数据，以及相关写操作。
        /// 将时间段分割成整年的单位分别读取。
        /// </summary>
        /// <param name="code"></param>
        /// <param name="dateStart"></param>
        /// <param name="dateEnd"></param>
        /// <param name="tag"></param>
        /// <param name="options"></param>
        /// <param name="tryCsv"></param>
        /// <param name="tryWind"></param>
        /// <param name="tryMssql0"></param>
        /// <param name="saveToCsv"></param>
        /// <returns></returns>
        private List<T> fetch1(string code, DateTime dateStart, DateTime dateEnd, string tag, IDictionary<string, object> options, bool tryCsv, bool tryWind, bool tryMssql0, bool saveToCsv)
        {
            int year0 = dateStart.Year, year2 = dateEnd.Year;
            List<T> result = new List<T>();
            if (year0 < year2)
            {
                var year0_1231 = new DateTime(year0, 12, 31);
                var year2_0101 = new DateTime(year2, 1, 1);

                var year0all = fetch0(code, year0, tag, options, tryCsv, tryWind, tryMssql0, saveToCsv);
                result.AddRange(SequentialUtils.GetRange(year0all, dateStart, year0_1231));

                for (int y = year0 + 1; y < year2; y++)
                {
                    var year1all = fetch0(code, y, tag, options, tryCsv, tryWind, tryMssql0, saveToCsv);
                    result.AddRange(year1all);
                }

                var year2all = fetch0(code, year2, tag, options, tryCsv, tryWind, tryMssql0, saveToCsv);
                result.AddRange(SequentialUtils.GetRange(year2all, year2_0101, dateEnd));
            }
            else
            {
                var year0all = fetch0(code, year0, tag, options, tryCsv, tryWind, tryMssql0, saveToCsv);
                return SequentialUtils.GetRange(year0all, dateStart, dateEnd);
            }

            return result;
        }

        /// <summary>
        /// 获取某一整年（1月1日-12月31日）的数据，以及相关写操作
        /// </summary>
        /// <param name="code"></param>
        /// <param name="year"></param>
        /// <param name="tag"></param>
        /// <param name="options"></param>
        /// <param name="tryCsv"></param>
        /// <param name="tryWind"></param>
        /// <param name="tryMssql0"></param>
        /// <param name="saveToCsv"></param>
        /// <returns></returns>
        private List<T> fetch0(string code, int year, string tag, IDictionary<string, object> options, bool tryCsv, bool tryWind, bool tryMssql0, bool saveToCsv)
        {
            if (tag == null) tag = typeof(T).ToString();
            List<T> result = null;
            bool csvHasData = false;
            var date1 = new DateTime(year, 1, 1);
            var date2 = new DateTime(year, 12, 31);

            log.Debug("正在获取{0}数据列表(code={1},year={2})...", Kit.ToShortName(tag), code,year);
            if (tryCsv)
            {
                //尝试从csv获取                
                string pathThisYear;
                if (year == DateTime.Now.Year)
                {
                    //如果year为今年，可读取的文件路径不固定
                    var path = _buildCacheDataFilePath(code, year + "0101", year + "*", tag);
                    var dirPath = Path.GetDirectoryName(path);
                    var fileName = Path.GetFileName(path);
                    if (Directory.Exists(dirPath))
                    {
                        pathThisYear = Directory.EnumerateFiles(dirPath, fileName).FirstOrDefault();
                    }
                    else
                    {
                        pathThisYear = null;
                    }
                }
                else
                {
                    pathThisYear = _buildCacheDataFilePath(code, date1, date2, tag);
                }
                try
                {
                    log.Debug("尝试从csv文件{1}获取{0}...", code, Kit.ToShortName(pathThisYear));
                    //result返回空集表示本地csv文件中没有数据，null表示本地csv不存在
                    result = readFromLocalCsv(pathThisYear);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从csv文件{0}获取失败！", Kit.ToShortName(pathThisYear));
                }
                if (result != null) csvHasData = true;
            }
            if (result == null && tryWind)
            {
                //尝试从Wind获取
                log.Debug("尝试从Wind获取{0}...", code);
                try
                {
                    result = readFromWind(code, date1, date2, tag, options);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从Wind获取失败！");
                }
            }
            if (result == null && tryMssql0)
            {
                try
                {
                    //尝试从默认MSSQL源获取
                    log.Debug("尝试从默认MSSQL源获取{0}...", code);
                    result = readFromDefaultMssql(code, date1, date2, tag, options);
                }
                catch (Exception e)
                {
                    log.Error(e, "尝试从默认MSSQL源获取失败！");
                }

            }
            if (saveToCsv)
            {  

                if (!csvHasData && result != null && result.Count()>0)
                {   //如果数据不是从csv获取的，可保存至本地，存为csv文件

                    //删除所有非完整年度数据的csv文件（非1231结尾的文件名）
                    var path = _buildCacheDataFilePath(code, year + "0101", year + "*", tag);
                    var dirPath = Path.GetDirectoryName(path);
                    var fileName = Path.GetFileName(path);
                    if (Directory.Exists(dirPath))
                    {
                        var pathsToDel = Directory.EnumerateFiles(dirPath, fileName).Where(fn => !fn.EndsWith("1231.csv"));
                        foreach (var p in pathsToDel)
                        {
                            File.Delete(p);
                            log.Info("删除了文件:{0}", Kit.ToShortName(p));
                        }
                    }
                    

                    //如果是year为今年，csv文件的取名以今天为结尾，表示非完整年度数据
                    var d2 = (year == DateTime.Now.Year) ? DateTime.Now : date2;
                    var pathToSave = _buildCacheDataFilePath(code, date1, d2, tag);
                    log.Debug("正在保存到本地csv文件...");
                    saveToLocalCsv(pathToSave, result);

                }
            }
            if (result != null && result.Count()>0)
            {
                log.Info("获取{3}数据{0}(year={1})成功.共{2}行.", Kit.ToShortName(tag), year, result.Count, code);
            }
            else
            {
                log.Info("获取{2}数据{0}(year={1})失败.无有效数据.", Kit.ToShortName(tag), year,code);
            }
            return result;
        }


        /// <summary>
        /// 将数据以csv文件的形式保存到CacheData文件夹下的预定路径。
        /// 保存一整年的数据（1月1日-12月31日）。
        /// 默认不可以保存今年的数据，因为可能数据不全。
        /// </summary>
        /// <param name="data">要保存的数据</param>
        /// <param name="code">代码</param>
        /// <param name="year">年</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="appendMode">是否为追加的文件尾部模式，否则是覆盖模式</param>
        /// <param name="canSaveThisYear">是否可以保存今年的数据，默认不可以</param>
        [Obsolete]
        public void saveToLocalCsv(IList<T> data, string code, int year, string tag = null, bool appendMode = false, bool canSaveThisYear = false)
        {
            if (!canSaveThisYear && year >= DateTime.Now.Year)
            {
                log.Debug("今年的{0}数据不保存，请在今年后保存。", Kit.ToShortName(tag));
                return;
            }
            var path = _buildCacheDataFilePath(code, new DateTime(year, 1, 1), new DateTime(year, 12, 31), tag);
            saveToLocalCsv(path, data, appendMode);
        }

        /// <summary>
        /// 将数据以csv文件的形式保存到CacheData文件夹下的预定路径。
        /// 保存指定时间段的数据到同一个CSV文件。
        /// </summary>
        /// <param name="data">要保存的数据</param>
        /// <param name="code">代码</param>
        /// <param name="date1">开始时间，包含本身</param>
        /// <param name="date2">结束时间，包含本身</param>
        /// <param name="tag">读写文件路径前缀，若为空默认为类名</param>
        /// <param name="appendMode">是否为追加的文件尾部模式，否则是覆盖模式</param>
        /// <param name="canSaveThisYear">是否可以保存今年的数据，默认不可以</param>
        [Obsolete]
        private void saveToLocalCsv(IList<T> data, string code, DateTime date1, DateTime date2, string tag = null, bool appendMode = false)
        {
            var path = _buildCacheDataFilePath(code, date1, date2, tag);
            saveToLocalCsv(path, data, appendMode);
        }

   

        private static string _buildCacheDataFilePath(string code, DateTime date1, DateTime date2, string tag)
        {
            return _buildCacheDataFilePath(code, date1.ToString("yyyyMMdd"), date2.ToString("yyyyMMdd"), tag);
        }

        private static string _buildCacheDataFilePath(string code, string date1, string date2, string tag)
        {
            if (tag == null) tag = typeof(T).ToString();
            return FileUtils.GetCacheDataFilePath(PATH_KEY, new Dictionary<string, string>
            {
                ["{tag}"] = tag,
                ["{code}"] = code,
                ["{date1}"] = date1,
                ["{date2}"] = date2
            });
        }
    }
}
