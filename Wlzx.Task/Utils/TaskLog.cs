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
    }
}
