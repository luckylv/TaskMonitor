using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Wlzx.Utility
{
    /// <summary>
    /// 使用SMTP发送邮件,需要添加发送人邮件 用户名密码进行认证才能发送成功
    /// </summary>
    public class MailHelper
    {
        /// <summary>
        /// 邮件服务配置信息
        /// </summary>
        private static string MailInfo = SysConfig.MailInfo;

        /// <summary>
        /// 配置信息
        /// </summary>
        private static MailConfig Config = null;

        /// <summary>
        /// SMTP客户端实例
        /// </summary>
        private static SmtpClient client = null;

        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="Receiver">邮件接收人</param>
        /// <param name="Subject">邮件主题</param>
        /// <param name="content">邮件内容</param>
        /// <returns>发送状态</returns>
        public static SMSCode SendMessage(string Receiver, string Subject, string content)
        {

            if (string.IsNullOrEmpty(Receiver) || string.IsNullOrEmpty(Subject)
                || string.IsNullOrEmpty(content))
            {
                throw new ArgumentNullException("SendMessage参数空异常！");
            }
            if (Config == null)
            {
                Config = JsonConvert.DeserializeObject<MailConfig>(MailInfo);
                if (string.IsNullOrEmpty(Config.SmtpHost) || string.IsNullOrEmpty(Config.SmtpPort) || string.IsNullOrEmpty(Config.SendMail) ||
                    string.IsNullOrEmpty(Config.UserName) || string.IsNullOrEmpty(Config.Password))
                {
                    throw new ArgumentNullException("配置文件节点MailInfo信息错误,相关信息为空！");
                }
            }
            if (client == null)
            {
                try
                {
                    client = new SmtpClient(Config.SmtpHost, Convert.ToInt32(Config.SmtpPort));
                    client.Credentials = new NetworkCredential(Config.UserName, Config.Password);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            try
            {
                MailMessage Message = new MailMessage();
                Message.From = new MailAddress(Config.SendMail);
                //添加邮件接收人地址
                string[] receivers = Receiver.Split(new char[] { ',' });
                Array.ForEach(receivers.ToArray(), ToMail => { Message.To.Add(ToMail); });
                Message.Subject = Subject;
                Message.Body = content;

                client.Send(Message);
                return SMSCode.Success;
            }
            catch (Exception ex)
            {
                LogHelper.WriteError("邮件发送失败", ex);
                return SMSCode.Exception;
            }
        }
    }

    /// <summary>
    /// 发送邮件服务配置信息
    /// </summary>
    internal sealed class MailConfig
    {
        /// <summary>
        /// smtp服务器地址
        /// </summary>
        public string SmtpHost { get; set; }

        /// <summary>
        /// smtp服务器端口
        /// </summary>
        public string SmtpPort { get; set; }

        /// <summary>
        ///发送邮箱地址
        /// </summary>
        public string SendMail { get; set; }

        /// <summary>
        ///发送邮箱地址
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// 发送邮箱用户名
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 发送邮箱密码
        /// </summary>
        public string Password { get; set; }
    }
}
