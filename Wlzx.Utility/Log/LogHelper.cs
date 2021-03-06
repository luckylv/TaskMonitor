﻿using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Wlzx.Utility
{
    /// <summary>
    /// 使用LOG4NET记录日志的功能，在WEB.CONFIG里要配置相应的节点
    /// </summary>
    public class LogHelper
    {
        //log4net日志专用
        public static readonly log4net.ILog loginfo = log4net.LogManager.GetLogger("loginfo");
        public static readonly log4net.ILog logerror = log4net.LogManager.GetLogger("logerror");
        public static readonly log4net.ILog logcs = log4net.LogManager.GetLogger("logconsole");

        public static void SetConfig()
        {
            log4net.Config.XmlConfigurator.Configure();
        }

        public static void SetConfig(FileInfo configFile)
        {
            log4net.Config.XmlConfigurator.Configure(configFile);
        }
        /// <summary>
        /// 普通的文件记录日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteLog(string info)
        {
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }

        /// <summary>
        /// 普通的文件记录日志和窗口输出日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteLogAndC(string info)
        {
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
            if (logcs.IsInfoEnabled)
            {
                logcs.Info(info);
            }
        }

        /// <summary>
        /// Console窗口输出日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteLogC(string info)
        {
            if (logcs.IsInfoEnabled)
            {
                logcs.Info(info);
            }
        }
        ///// <summary>
        ///// 错误日志
        ///// </summary>
        ///// <param name="info"></param>
        ///// <param name="se"></param>
        //public static void WriteLog(string info, Exception se)
        //{
        //    if (logerror.IsErrorEnabled)
        //    {
        //        logerror.Error(info, se);
        //    }
        //}

        /// <summary>
        /// 错误日志2
        /// </summary>
        /// <param name="info"></param>
        /// <param name="se"></param>
        public static void WriteError(string info, Exception se)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, se);
            }
        }
        /// <summary>
        /// 错误日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteError(string info)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, null);
            }
        }

        /// <summary>
        /// 输出错误日志和窗口日志
        /// </summary>
        /// <param name="info"></param>
        public static void WriteErrorAndC(string info)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, null);
            }
            if (logcs.IsInfoEnabled)
            {
                logcs.Info(info);
            }
        }

        /// <summary>
        /// 输出错误日志和窗口日志2
        /// </summary>
        /// <param name="info"></param>
        /// <param name="se"></param>
        public static void WriteErrorAndC(string info, Exception se)
        {
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, se);
            }
            if (logcs.IsInfoEnabled)
            {
                logcs.Info(info, se);
            }
        }

        /// <summary>
        /// 缓存职责对应的日志Log
        /// </summary>
        private static Hashtable s_LogDict = Hashtable.Synchronized(new Hashtable(10240));

        /// <summary>
        /// 职责名称
        /// </summary>
        private string repositoryName;

        /// <summary>
        /// 日志级别
        /// </summary>
        private string level;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="repositoryName">职责名称</param>
        /// <remarks>解决任务和日志对应问题</remarks>
        public LogHelper(string repositoryName, string level)
        {
            this.repositoryName = repositoryName;
            this.level = level;
        }

        public ILog GetLogger()
        {
            return GetLogger(repositoryName, level);
        }

        /// <summary>
        /// 单独文件的普通文件记录日志
        /// </summary>
        /// <param name="info"></param>
        public void WriteLogE(string info)
        {
            ILog loginfo = GetLogger(repositoryName, level);
            if (loginfo.IsInfoEnabled)
            {
                loginfo.Info(info);
            }
        }

        ///// <summary>
        ///// 单独文件的错误日志
        ///// </summary>
        ///// <param name="info"></param>
        //public void WriteLogE(string info, Exception ex)
        //{
        //    ILog logerror = GetLogger(repositoryName, level);
        //    if (logerror.IsErrorEnabled)
        //    {
        //        logerror.Error(info, ex);
        //    }
        //}

        /// <summary>
        /// 单独文件的普通文件记录日志
        /// </summary>
        /// <param name="info">日志内容</param>
        /// <param name="repositoryName">任务名</param>
        /// <param name="level">日志级别，如info,error</param>
        public static void TaskWriteLog(string info, string repositoryName, string level = "info", Exception ex=null)
        {
            ILog loginfo = GetLogger(repositoryName, level);
            //Console.WriteLine("要运行" + repositoryName + "  " + level + "   " + info + "  " + level +"  "+ String.Equals(level, Level.Info.ToString(), StringComparison.CurrentCultureIgnoreCase).ToString());

            if (String.Equals(level, Level.Info.ToString(), StringComparison.CurrentCultureIgnoreCase))
            {
                if (loginfo.IsInfoEnabled)
                {
                    loginfo.Info(info);
                }
            }
            else
            {
                if (loginfo.IsErrorEnabled)
                {
                    loginfo.Error(info, ex);
                }
            }

            
        }

        /// <summary>
        /// 单独文件的错误日志
        /// </summary>
        /// <param name="info"></param>
        public void WriteLogE(string info, Exception ex)
        {
            ILog logerror = GetLogger(repositoryName, level);
            if (logerror.IsErrorEnabled)
            {
                logerror.Error(info, ex);
            }
        }

        #region "动态根据职责名称创建日志文件,方便一个任务的日志记录在相应的任务文件里面"

        /// <summary>
        /// 获取ILog对象
        /// </summary>
        /// <param name="repositoryName">职责名称,如果不存在则创建</param>
        /// <returns>ILog</returns>
        public static ILog GetLogger(string repositoryName, string level)
        {
            if (string.IsNullOrEmpty(repositoryName) || string.IsNullOrEmpty(level))
            {
                return loginfo;
            }
            repositoryName = GetRepositoryNameName(repositoryName, level);
            ILog log = s_LogDict[repositoryName] as ILog;
            if (log == null)
            {
                ILoggerRepository repository = null;
                //未找到则创建，多线程下很有可能创建时，就存在了
                try
                {
                    repository = LogManager.CreateRepository(repositoryName);
                    
                }
                catch (Exception)
                {
                    repository = LogManager.GetRepository(repositoryName);
                }

                if (string.IsNullOrEmpty(level))
                {
                    level = "all";
                }
                level = level.ToLower();
                switch (level)
                {
                    case "all":
                        repository.Threshold = Level.All;
                        break;
                    case "debug":
                        repository.Threshold = Level.Debug;
                        break;
                    case "info":
                        repository.Threshold = Level.Info;
                        break;
                    case "warn":
                        repository.Threshold = Level.Warn;
                        break;
                    case "error":
                        repository.Threshold = Level.Error;
                        break;
                    case "fatal":
                        repository.Threshold = Level.Fatal;
                        break;
                    case "off":
                        repository.Threshold = Level.Off;
                        break;
                    default:
                        repository.Threshold = Level.All;
                        break;
                }
                LoadFileAppender(repository);
                log = LogManager.GetLogger(repositoryName, GetLevelName(repository.Threshold));
                s_LogDict[repositoryName] = log;
            }
            return log;
        }

        private static string GetLevelName(Level code)
        {
            return string.Format("Log{0}", code.ToString()).ToLower();
        }

        private static string GetRepositoryNameName(string repositoryName, string code)
        {
            return string.Format("{0}-{1}", repositoryName, code).ToLower();
        }

        /// <summary>
        /// 使用文本记录信息
        /// </summary>
        /// <Author>焰尾迭</Author>
        /// <date>2015-09-22</date>
        private static void LoadFileAppender(ILoggerRepository repository)
        {
            string txtLogPath = FileHelper.GetAbsolutePath(string.Format("/Logs/{0}.log", repository.Name));
            RollingFileAppender fileAppender = new RollingFileAppender();
            fileAppender.Name = "LogFileAppender";
            fileAppender.File = txtLogPath;
            fileAppender.AppendToFile = true;
            fileAppender.MaxSizeRollBackups = 20;
            //fileAppender.MaximumFileSize = "1MB";
            fileAppender.RollingStyle = log4net.Appender.RollingFileAppender.RollingMode.Date;

            PatternLayout patternLayout = new PatternLayout();
            patternLayout.ConversionPattern = "%d{yyyy-MM-dd HH:mm:ss} %m%n";
            patternLayout.ActivateOptions();
            fileAppender.Layout = patternLayout;

            //选择UTF8编码，确保中文不乱码。
            fileAppender.Encoding = Encoding.UTF8;
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(repository, fileAppender);
        }


        #endregion
    }
}
