using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wlzx.Utility.Quartz
{
    /// <summary>
    /// 监控任务执行参数
    /// </summary>
    public class MonitorParam
    {
        /// <summary>
        /// 共享目录IP
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 共享文件夹名称
        /// </summary>
        public string ShareName { get; set; }

        /// <summary>
        /// 登录用户名
        /// </summary>
        public string User { get; set; }

        /// <summary>
        /// 登录密码
        /// </summary>
        public string Pw { get; set; }

        /// <summary>
        /// 日志文件名模板,用DTFormat代替日期部分
        /// </summary>
        public string LogFile { get; set; }

        /// <summary>
        /// 文件名中的日期格式设定
        /// </summary>
        public string DTFormat { get; set; }

        /// <summary>
        /// 子文件夹中设定,可为空
        /// </summary>
        public string ShareSub { get; set; }

        /// <summary>
        /// 子文件夹中的日期格式设定,可为空
        /// </summary>
        public string SubDTFormat { get; set; }

        /// <summary>
        /// 向上搜索时间跨度,单位分钟
        /// </summary>
        public string RunTimeSPMin { get; set; }

        /// <summary>
        /// 入库的表名称
        /// </summary>
        public string DBTable { get; set; }

        /// <summary>
        /// 单次匹配起点正则表达式
        /// </summary>
        public string RegexStart { get; set; }

        /// <summary>
        /// 匹配单条记录正则表达式
        /// </summary>
        public string RegexOne { get; set; }
    }
}
