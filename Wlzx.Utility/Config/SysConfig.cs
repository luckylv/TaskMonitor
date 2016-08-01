using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wlzx.Utility
{
    /// <summary>
    /// 缓存系统所有配置信息，以键值对形式存在
    /// </summary>
    /// <remarks>
    /// 系统相关配置信息都应该通过此类的静态属性读取
    /// </remarks>
    /// <example>
    /// 获取连接字符串 SysConfig.SqlConnect
    /// </example>
    /// <summary>
    /// 系统的配置
    /// </summary>


    public class SysConfig
    {
        /// <summary>
        /// 数据库连接字符串信息
        /// </summary>
        [PathMap(Key = "SqlConnect")]
        public static string SqlConnect { get; set; }

        /// <summary>
        /// 联通SMS数据库连接字符串信息
        /// </summary>
        [PathMap(Key = "LTsmsConnect")]
        public static string LTsmsConnect { get; set; }

        /// <summary>
        /// 移动SMS数据库连接字符串信息
        /// </summary>
        [PathMap(Key = "YDsmsConnect")]
        public static string YDsmsConnect { get; set; }

        /// <summary>
        /// 邮件信息配置
        /// </summary>
        [PathMap(Key="MailInfo")]
        public static string MailInfo { get; set; }
        /// <summary>
        /// 邮件信息配置
        /// </summary>
        [PathMap(Key="WebPort")]
        public static int WebPort { get; set; }
    }

    /// <summary>
    /// 配置文件标注
    /// </summary>
    public class PathMapAttribute : Attribute
    {
        /// <summary>
        /// 键
        /// </summary>
        public string Key;
        /// <summary>
        /// xmlPath路径前缀
        /// </summary>
        public string Xmlpath = @"/configuration/add";
        /// <summary>
        /// 是否需要对该值进行DES解密
        /// </summary>
        public bool IsDecrypt = false;
    }
}
