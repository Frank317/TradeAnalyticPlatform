﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackTestingPlatform.Utilities
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using NLog;

    /// <summary>
    /// 
    /// 代码参考自 http://www.cnblogs.com/steed-zgf/archive/2012/01/06/2314559.html
    /// 有改动，支持多数据源(connectionString)
    /// </summary>
    public class SqlUtils
    {

        static Logger log = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 获取app.config里配置的ConnectionString
        /// 如果以“server=”开头，则原样返回
        /// </summary>
        /// <param name="connectionName"></param>
        /// <returns></returns>
        public static string GetConnectionString(string connectionName)
        {
            if (connectionName.StartsWith("server="))
                return connectionName;
            return ConfigurationManager.ConnectionStrings[connectionName].ToString();
        }
        /// <summary>
        /// 根据app.config里配置的连接名称打开连接
        /// </summary>
        /// <param name="connName">app.config里配置的连接名称</param>
        /// <returns>打开的连接</returns>
        public static SqlConnection openConnectionByConfigName(string connName)
        {
            string connStr = GetConnectionString(connName);
            return new SqlConnection(connStr);
        }


        #region 通过一次性的连接，执行查询，返回DataTable对象-----------------------


        /// <summary>
        /// 通过一次性的连接，执行查询，返回DataTable对象
        /// </summary>
        /// <param name="connStr">sql连接字符串，代表一个数据源</param>
        /// <param name="sql">sql语句</param>
        /// <returns>DataTable对象</returns>
        public static DataTable GetTable(string connStr, string sql)
        {
            return GetTable(connStr, sql, null);
        }

        /// <summary>
        /// 通过一次性的连接，执行查询，返回DataTable对象
        /// </summary>
        /// <param name="connStr">sql连接字符串，代表一个数据源</param>
        /// <param name="sql">sql语句</param>
        /// <param name="paramArr">参数数组</param>
        /// <returns>DataTable对象</returns>
        public static DataTable GetTable(string connStr, string sql, SqlParameter[] paramArr)
        {
            return GetTable(connStr, sql, paramArr, CommandType.Text);
        }

        /// <summary>
        /// 通过一次性的连接，执行查询，返回DataTable对象
        /// </summary>
        /// <param name="connStr">sql连接字符串，代表一个数据源</param>
        /// <param name="sql">sql语句</param>
        /// <param name="paramArr">参数数组</param>
        /// <param name="cmdType">Command类型</param>
        /// <returns>DataTable对象</returns>
        public static DataTable GetTable(string connStr, string sql, SqlParameter[] paramArr, CommandType cmdType)
        {
            return GetTable(connStr, sql, paramArr, cmdType, 0);
        }

        /// <summary>
        /// 通过一次性的连接，执行查询，返回DataTable对象
        /// </summary>
        /// <param name="connStr">sql连接字符串，代表一个数据源</param>
        /// <param name="sql">sql语句</param>
        /// <param name="paramArr">参数数组</param>
        /// <param name="cmdTimeout"></param>
        /// <returns>DataTable对象</returns>
        public static DataTable GetTable(string connStr, string sql, SqlParameter[] paramArr, int cmdTimeout)
        {
            return GetTable(connStr, sql, paramArr, CommandType.Text, cmdTimeout);
        }

        /// <summary>
        /// 通过一次性的连接，执行查询，返回DataTable对象
        /// </summary>
        /// <param name="connStr">sql连接字符串，代表一个数据源</param>
        /// <param name="sql">sql语句</param>
        /// <param name="paramArr">参数数组</param>
        /// <param name="cmdType">Command类型</param>
        /// <param name="cmdTimeout"></param>
        /// <returns>DataTable对象</returns>
        public static DataTable GetTable(string connStr, string sql, SqlParameter[] paramArr, CommandType cmdType,int cmdTimeout)
        {
            DataTable dt = new DataTable();
            log.Debug("Executing SQL Query: conn={0},SQL=[\n{1}\n]", connStr, sql);
            if (paramArr != null)
                log.Debug("params={0}", paramArr.ToString());
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                    da.SelectCommand.CommandType = cmdType;
                    if (cmdTimeout>0) da.SelectCommand.CommandTimeout = cmdTimeout;
                    if (paramArr != null)
                    {
                        da.SelectCommand.Parameters.AddRange(paramArr);
                    }
                    da.Fill(dt);
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }
            return dt;
        }


        #endregion


        #region 给定已可用的connection,执行查询-------------------------

        public static DataTable GetTable(SqlConnection conn, string sql, SqlParameter[] paramArr = null, CommandType commandType = CommandType.Text)
        {
            DataTable dt = new DataTable();

            SqlDataAdapter da = new SqlDataAdapter(sql, conn);
            da.SelectCommand.CommandType = commandType;
            if (paramArr != null)
            {
                da.SelectCommand.Parameters.AddRange(paramArr);
            }
            da.Fill(dt);

            return dt;
        }

        #endregion

        #region 执行查询，返回DataSet对象-------------------------




        public static DataSet GetDataSet(string connStr, string sql)
        {
            return GetDataSet(connStr, sql, null);
        }

        public static DataSet GetDataSet(string connStr, string sql, SqlParameter[] paramArr)
        {
            return GetDataSet(connStr, sql, paramArr, CommandType.Text);
        }
        /// <summary>
        /// 执行查询，返回DataSet对象
        /// </summary>
        /// <param name="connStr">sql连接字符串</param>
        /// <param name="sql">sql语句</param>
        /// <param name="paramArr">参数数组</param>
        /// <param name="commandType">Command类型</param>
        /// <returns>DataSet对象</returns>
        public static DataSet GetDataSet(string connStr, string sql, SqlParameter[] paramArr, CommandType commandType)
        {
            DataSet dt = new DataSet();
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlDataAdapter da = new SqlDataAdapter(sql, conn);
                da.SelectCommand.CommandType = commandType;
                if (paramArr != null)
                {
                    da.SelectCommand.Parameters.AddRange(paramArr);
                }
                da.Fill(dt);
            }
            return dt;
        }
        #endregion





        #region 执行非查询存储过程和SQL语句-----------------------------




        public static int ExcuteProc(string connStr, string procName)
        {
            return ExcuteSQL(connStr, procName, null, CommandType.StoredProcedure);
        }

        public static int ExcuteProc(string connStr, string procName, SqlParameter[] paras)
        {
            return ExcuteSQL(connStr, procName, paras, CommandType.StoredProcedure);
        }

        public static int ExcuteSQL(string connStr, string sql)
        {
            return ExcuteSQL(connStr, sql, null);
        }

        public static int ExcuteSQL(string connStr, string sql, SqlParameter[] paras)
        {
            return ExcuteSQL(connStr, sql, paras, CommandType.Text);
        }

        /// <summary>
        /// 执行非查询存储过程和SQL语句
        /// 增、删、改
        /// </summary>
        /// <param name="connStr">sql连接字符串</param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="paras">参数列表，没有参数填入null</param>
        /// <param name="cmdType">Command类型</param>
        /// <returns>返回影响行数</returns>
        public static int ExcuteSQL(string connStr, string sql, SqlParameter[] paras, CommandType cmdType)
        {
            int i = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = cmdType;
                if (paras != null)
                {
                    cmd.Parameters.AddRange(paras);
                }
                conn.Open();
                i = cmd.ExecuteNonQuery();
                conn.Close();
            }
            return i;

        }


        #endregion








        #region 执行查询返回第一行，第一列---------------------------------




        public static int ExcuteScalarSQL(string connStr, string sql)
        {
            return ExcuteScalarSQL(connStr, sql, null);
        }

        public static int ExcuteScalarSQL(string connStr, string sql, SqlParameter[] paras)
        {
            return ExcuteScalarSQL(connStr, sql, paras, CommandType.Text);
        }
        public static int ExcuteScalarProc(string connStr, string sql, SqlParameter[] paras)
        {
            return ExcuteScalarSQL(connStr, sql, paras, CommandType.StoredProcedure);
        }
        /// <summary>
        /// 执行SQL语句，返回第一行，第一列
        /// </summary>
        /// <param name="connStr"></param>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="paras">参数列表，没有参数填入null</param>
        /// <param name="cmdType"></param>
        /// <returns>返回影响行数</returns>
        public static int ExcuteScalarSQL(string connStr, string sql, SqlParameter[] paras, CommandType cmdType)
        {
            int i = 0;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = cmdType;
                if (paras != null)
                {
                    cmd.Parameters.AddRange(paras);
                }
                conn.Open();
                i = Convert.ToInt32(cmd.ExecuteScalar());
                conn.Close();
            }
            return i;

        }


        #endregion









        #region 查询获取单个值------------------------------------




        /// <summary>
        /// 调用不带参数的存储过程获取单个值
        /// </summary>
        /// <param name="ProcName"></param>
        /// <returns></returns>
        public static object GetObjectByProc(string connStr, string ProcName)
        {
            return GetObjectByProc(connStr, ProcName, null);
        }
        /// <summary>
        /// 调用带参数的存储过程获取单个值
        /// </summary>
        /// <param name="ProcName"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public static object GetObjectByProc(string connStr, string ProcName, SqlParameter[] paras)
        {
            return GetObject(connStr, ProcName, paras, CommandType.StoredProcedure);
        }
        /// <summary>
        /// 根据sql语句获取单个值
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public static object GetObject(string connStr, string sql)
        {
            return GetObject(connStr, sql, null);
        }
        /// <summary>
        /// 根据sql语句 和 参数数组获取单个值
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="paras"></param>
        /// <returns></returns>
        public static object GetObject(string connStr, string sql, SqlParameter[] paras)
        {
            return GetObject(connStr, sql, paras, CommandType.Text);
        }

        /// <summary>
        /// 执行SQL语句，返回首行首列
        /// </summary>
        /// <param name="sql">要执行的SQL语句</param>
        /// <param name="paras">参数列表，没有参数填入null</param>
        /// <returns>返回的首行首列</returns>
        public static object GetObject(string connStr, string sql, SqlParameter[] paras, CommandType cmdtype)
        {
            object o = null;
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.CommandType = cmdtype;
                if (paras != null)
                {
                    cmd.Parameters.AddRange(paras);

                }

                conn.Open();
                o = cmd.ExecuteScalar();
                conn.Close();
            }
            return o;

        }



        #endregion





        #region 查询获取DataReader------------------------------------




        /// <summary>
        /// 调用不带参数的存储过程，返回DataReader对象
        /// </summary>
        /// <param name="procName">存储过程名称</param>
        /// <returns>DataReader对象</returns>
        public static SqlDataReader GetReaderByProc(string connStr, string procName)
        {
            return GetReaderByProc(connStr, procName, null);
        }
        /// <summary>
        /// 调用带有参数的存储过程，返回DataReader对象
        /// </summary>
        /// <param name="procName">存储过程名</param>
        /// <param name="paras">参数数组</param>
        /// <returns>DataReader对象</returns>
        public static SqlDataReader GetReaderByProc(string connStr, string procName, SqlParameter[] paras)
        {
            return GetReader(connStr, procName, paras, CommandType.StoredProcedure);
        }
        /// <summary>
        /// 根据sql语句返回DataReader对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <returns>DataReader对象</returns>
        public static SqlDataReader GetReader(string connStr, string sql)
        {
            return GetReader(connStr, sql, null);
        }
        /// <summary>
        /// 根据sql语句和参数返回DataReader对象
        /// </summary>
        /// <param name="sql">sql语句</param>
        /// <param name="paras">参数数组</param>
        /// <returns>DataReader对象</returns>
        public static SqlDataReader GetReader(string connStr, string sql, SqlParameter[] paras)
        {
            return GetReader(connStr, sql, paras, CommandType.Text);
        }
        /// <summary>
        /// 查询SQL语句获取DataReader
        /// </summary>
        /// <param name="sql">查询的SQL语句</param>
        /// <param name="paras">参数列表，没有参数填入null</param>
        /// <returns>查询到的DataReader（关闭该对象的时候，自动关闭连接）</returns>
        public static SqlDataReader GetReader(string connStr, string sql, SqlParameter[] paras, CommandType cmdtype)
        {
            SqlDataReader sqldr = null;
            SqlConnection conn = new SqlConnection(connStr);
            SqlCommand cmd = new SqlCommand(sql, conn);
            cmd.CommandType = cmdtype;
            if (paras != null)
            {
                cmd.Parameters.AddRange(paras);
            }
            conn.Open();
            //CommandBehavior.CloseConnection的作用是如果关联的DataReader对象关闭，则连接自动关闭
            sqldr = cmd.ExecuteReader(CommandBehavior.CloseConnection);
            return sqldr;
        }



        #endregion




        #region 批量插入数据---------------------------------------------




        /// <summary>
        /// 往数据库中批量插入数据
        /// </summary>
        /// <param name="sourceDt">数据源表</param>
        /// <param name="targetTable">服务器上目标表</param>
        public static void BulkToDB(string connStr, DataTable sourceDt, string targetTable)
        {
            SqlConnection conn = new SqlConnection(connStr);
            SqlBulkCopy bulkCopy = new SqlBulkCopy(conn);   //用其它源的数据有效批量加载sql server表中
            bulkCopy.DestinationTableName = targetTable;    //服务器上目标表的名称
            bulkCopy.BatchSize = sourceDt.Rows.Count;   //每一批次中的行数

            try
            {
                conn.Open();
                if (sourceDt != null && sourceDt.Rows.Count != 0)
                    bulkCopy.WriteToServer(sourceDt);   //将提供的数据源中的所有行复制到目标表中
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                conn.Close();
                if (bulkCopy != null)
                    bulkCopy.Close();
            }

        }

        #endregion


    }

}
