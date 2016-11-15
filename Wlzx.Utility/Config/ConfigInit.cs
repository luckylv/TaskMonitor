using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Reflection;
using System.IO;
using System.Diagnostics;

namespace Wlzx.Utility
{
    /// <summary>
    /// 配置文件信息初始化,为了解决团队开发中,每个人的config文件不一致,而需要修改app.config或者web.config。
    /// 下次获取最新又覆盖了本地修改的相关配置问题，通过添加本地配置文件格式为app_Local.config开解决此问题
    /// </summary>
    public class ConfigInit
    {
        /// <summary>
        /// 服务器配置文件地址
        /// </summary>
        private static readonly string ConfigPath = FileHelper.GetAbsolutePath("Config/Config.config");

        /// <summary>
        /// 本地配置文件地址
        /// </summary>
        private static readonly string ConfigLocalPath = FileHelper.GetAbsolutePath("Config/Config_Local.config");

        private static BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.SetProperty;
        
        private static XmlDocument doc = null;

        /// <summary>
        /// 初始化配置信息
        /// </summary>
        public static  void InitConfig()
        {
            try
            {
                doc = new XmlDocument();
                if (!File.Exists(ConfigPath))
                {
                    return;
                }
                Type type = typeof(SysConfig);
                PropertyInfo[] Props = type.GetProperties(flags); //获取类的所有属性
                PathMapAttribute PathMap = null;
                foreach(var prop in Props)
                {
                    PathMap = GetMyAttribute<PathMapAttribute>(prop, false); //每个属性获取它的自定义特性
                    if (PathMap != null)
                    {
                        //根据XML中该属性的特性中的key代表的value,给类的每个属性设值
                        prop.SetValue(null, Convert.ChangeType(GetConfigValue(PathMap), prop.PropertyType), null);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 通过键读取配置文件信息
        /// </summary>
        /// <param name="PathMap">自定义属性信息</param>
        /// <returns>值</returns>
        private static string GetConfigValue(PathMapAttribute PathMap)
        {
            if (!File.Exists(ConfigPath))
            {
                return "";
            }

            string path = GetXmlPath(PathMap.Key, PathMap.Xmlpath);//根据特性的key获取配置文件中相应设置
            XmlNode node = null;
            XmlAttribute attr = null;
            try
            {
                //如果存在本地配置
                if (File.Exists(ConfigLocalPath))
                {
                    doc.Load(ConfigLocalPath);
                    node = doc.SelectSingleNode(path);
                    if (node != null)
                    {
                        attr = node.Attributes["value"];//根据KEY获取相应的value
                        if (attr == null)
                        {
                            throw new Exception("本地配置文件设置异常，节点" + PathMap.Key + "，没有相应的value属性，请检查!");
                        }
                        return GetRealValue(attr.Value, PathMap.IsDecrypt);//以string类型返回为相应key中的value
                    }
                }
                //读取服务器配置
                doc.Load(ConfigPath);
                node = doc.SelectSingleNode(path);
                if (node!=null)
                {
                    attr = node.Attributes["value"];
                    if (attr == null)
                    {
                        throw new Exception("服务器配置文件设置异常，节点" + PathMap.Key + "，没有相应的value属性，请检查!");
                    }
                    return GetRealValue(attr.Value, PathMap.IsDecrypt);
                }
            }
            catch (Exception ex)
            {
                
                throw ex;
            }
            return "";
        }

        /// <summary>
        /// 获取xmlpath全路径
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="XmlPath">xmlpath路径前缀</param>
        /// <returns>xmlpath全路径</returns>
        private static string GetXmlPath(string key,string XmlPath)
        {
            return string.Format("{0}[@key='{1}']", XmlPath, key);
        }


        /// <summary>
        /// 返回MemberInfo对象指定类型的Attribute
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        private static T GetMyAttribute<T>(MemberInfo m,bool inherit) where T:Attribute
        {
            T[] array = m.GetCustomAttributes(typeof(T), inherit) as T[];
            if (array.Length == 1)
                return array[0];

            if (array.Length > 1)
                throw new InvalidProgramException(string.Format("方法{0}不能同时指定多次[{1}]", m.Name, typeof(T)));
            return default(T);
        }

        /// <summary>
        /// 获取配置文件真实值
        /// </summary>
        /// <param name="value">原始值</param>
        /// <param name="IsDecrypt">是否需要解密</param>
        /// <returns>真实值</returns>
        private static string GetRealValue(string value, bool IsDecrypt)
        {
            if (IsDecrypt)
            {
                return DESEncrypt.Decrypt(value);
            }
            else
            {
                return value;
            }
        }


        /// <summary>
        /// 连接远程共享路径 
        /// </summary>
        /// <param name="remoteHost">remote server IP or machinename.domain name</param>
        /// <param name="shareName">share name</param>
        /// <param name="userName">user name of remote share access account</param>
        /// <param name="passWord">password of remote share access account</param>
        /// <returns>connect result</returns>        
        public static bool Connect(string remoteHost, string shareName, string userName, string passWord)
        {
            bool Flag = false;
            Process proc = new Process();
            string dosLine="";
            try
            {
                proc.StartInfo.FileName = "cmd.exe";
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardInput = true;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.CreateNoWindow = true;
                proc.Start();
                dosLine = @"net use \\" + remoteHost + @"\" + shareName + " /User:" + userName + " " + passWord + " /PERSISTENT:YES";
                proc.StandardInput.WriteLine(dosLine);
                proc.StandardInput.WriteLine("exit");
                while (!proc.HasExited)
                {
                    proc.WaitForExit(1000);
                }
                
                string errormsg = proc.StandardError.ReadToEnd();
                proc.StandardError.Close();
                if (String.IsNullOrEmpty(errormsg))
                {
                    Flag = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteErrorAndC("映射" + dosLine + "错误", ex);
                throw ex;
            }
            finally
            {
                proc.Close();
                proc.Dispose();
            }
            return Flag;
        }

    }
}
