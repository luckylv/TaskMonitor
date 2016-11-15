using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Wlzx.Utility;

namespace Wlzx.Task.Utils
{
    public class TaskLog
    {
        /// <summary>
        /// 发送消息任务普通日志
        /// </summary>
        public static LogHelper SendMessageLogInfo = new LogHelper("SendMessageJob", "info");

        /// <summary>
        /// 发送消息任务异常日志
        /// </summary>
        public static LogHelper SendMessageLogError = new LogHelper("SendMessageJob", "error");

        /// <summary>
        /// 外部启动任务普通日志
        /// </summary>
        public static LogHelper AutoRunLogInfo = new LogHelper("AutoRunJob", "info");

        /// <summary>
        /// 外部启动任务异常日志
        /// </summary>
        public static LogHelper AutoRunLogError = new LogHelper("AutoRunJob", "error");

        /// <summary>
        /// 外部启动任务普通日志
        /// </summary>
        public static LogHelper IpProxyLogInfo = new LogHelper("IpProxyJob", "info");

        /// <summary>
        /// 外部启动任务异常日志
        /// </summary>
        public static LogHelper IpProxyLogError = new LogHelper("IpProxyJob", "error");

        /// <summary>
        /// 自动站监控任务普通日志
        /// </summary>
        public static LogHelper AwsMonitorLogInfo = new LogHelper("AwsMonitorJob", "info");

        /// <summary>
        /// 自动站监控任务异常日志
        /// </summary>
        public static LogHelper AwsMonitorLogError = new LogHelper("AwsMonitorJob", "error");


    }
}