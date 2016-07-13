using CsharpHttpHelper;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

namespace Wlzx.Utility
{
    /// <summary>
    /// 短信发送类
    /// </summary>
    public class SmsHelper
    {

        private static string LTinsertSQL = @"INSERT INTO [ShortMsg_New].[dbo].[SendSms]
                                                ([phoneNumber]
                                                ,[smsContent]
                                                ,[smsTime]
                                                ,[smsUser]
                                                ,[staTime]
                                                ,[endTime]
                                                ,[status]
                                                ,[extno]
                                                ,[resultCode]
                                                ,[resultDesc]
                                                ,[failList])
                            VALUES(@phoneNumber,@smsContent,@smsTime,7,null,null,0,null,null,null,null)";

        private static string LTqueryIdSQL = @"Select MAX(smsIndex) AS ID FROM [ShortMsg_New].[dbo].[SendSms]";

        private static string LTqueryStatus = @"Select * FROM [ShortMsg_New].[dbo].[SendSms] where smsIndex=@smsId";
        /// <summary>
        /// 发送短信
        /// </summary>
        /// <param name="receiver">短信接收人手机号码（多号码用,分隔）</param>
        /// <param name="content">短信内容</param>
        /// <returns>发送状态</returns>
        public static SMSCode SendMessage(string receiver, string content)
        {
            try
            {
                //添加邮件接收人地址
                string[] receivers = receiver.Split(new char[] { ',' });
                foreach (string phone in receivers)
                {
                    //若发送失败，则重发此条短信一次
                    if (SendOneMessage(phone, content)!=SMSCode.Success)
                    {
                        SendOneMessage(phone, content);
                    }
                }
                return SMSCode.Success;

                #region "老代码"
                ////创建Httphelper对象
                //HttpHelper http = new HttpHelper();
                ////创建Httphelper参数对象
                //HttpItem item = new HttpItem()
                //{
                //    URL = string.Format("{0}?phone={1}&content=验证码:{2}", SmsAPI, receiver, content),//URL     必需项    
                //    Method = "get",//可选项 默认为Get   
                //    ContentType = "text/plain"//返回类型    可选项有默认值 ,
                //};
                //item.Header.Add("apikey", Apikey);
                ////请求的返回值对象
                //HttpResult result = http.GetHtml(item);
                //JObject jo = JObject.Parse(result.Html);
                //JToken value = null;
                //if (jo.TryGetValue("result", out value))
                //{
                //    return EnumHelper.IntToEnum<SMSCode>(Convert.ToInt32(value.ToString()));
                //}
                //return SMSCode.SystemBusy;


                #endregion
            }
            catch (Exception ex)
            {
                LogHelper.WriteError("短信发送出错", ex);
                LogHelper.WriteLogC("短信发送出错"+ex.Message);
                return SMSCode.Exception;
            }
        }

        public static SMSCode SendOneMessage(string oneReceiver, string content)
        {
            try
            {
                //插入一条发送记录
                DbHelper.ExecuteNonQuery(SysConfig.LTsmsConnect, LTinsertSQL, new { phoneNumber = oneReceiver, smsContent = content, smsTime = DateTime.Now.ToString() });
                //取出该条记录ID号
                string smsId = DbHelper.ExecuteScalar<string>(SysConfig.LTsmsConnect, LTqueryIdSQL, null);

                //等待2秒首次查询发送状态
                Thread.Sleep(2000);
                LTsmsUtil current = DbHelper.Single<LTsmsUtil>(SysConfig.LTsmsConnect, LTqueryStatus, new { smsId = smsId });
                if (current == null)
                {
                    string logstr = "发送短信错误，错误原因：第1次无法检索短信发送状态，请检查数据库连接";
                    LogHelper.WriteLogC(logstr);
                    LogHelper.WriteError(logstr);
                    return SMSCode.Exception;
                }

                //若无状态则每隔1秒查一次状态，共5次
                int checkTime = 0;
                while (checkTime < 5 && string.IsNullOrEmpty(current.resultCode))
                {
                    Thread.Sleep(1000);
                    current = DbHelper.Single<LTsmsUtil>(SysConfig.LTsmsConnect, LTqueryStatus, new { smsId = smsId });
                    if (current == null)
                    {
                        string logstr = "发送短信错误，错误原因：第" + checkTime.ToString() + "次无法检索到短信发送状态，请检查数据库连接";
                        LogHelper.WriteLogC(logstr);
                        LogHelper.WriteError(logstr);
                        return SMSCode.Exception;
                    }
                }
                //5次内检索到状态
                if (checkTime < 5)
                {   //状态为0则发送成功
                    if (Convert.ToInt32(current.resultCode) == 0)
                    {
                        string logstr = "成功发送短信至" + current.phoneNumber + "，内容:" + current.smsContent;
                        LogHelper.WriteLogC(logstr);
                        LogHelper.WriteLog(logstr);
                        return SMSCode.Success;
                    }
                    else//状态为非0则发送失败
                    {
                        string logstr = "发送短信失败，未到达" + current.phoneNumber + "，内容:" + current.smsContent + "，失败原因：" + current.resultDesc;
                        LogHelper.WriteLogC(logstr);
                        LogHelper.WriteError(logstr);
                        return SMSCode.Fail;
                    }
                }
                else//5次内还未检索到状态
                {
                    string logstr = "发送短信失败，未到达" + current.phoneNumber + "，内容:" + current.smsContent + "，失败原因：5次检索未取得短信发送状态";
                    LogHelper.WriteLogC(logstr);
                    LogHelper.WriteError(logstr);
                    return SMSCode.Fail;
                }
       
            }
            catch (Exception ex)
            {
                LogHelper.WriteError("短信发送出错", ex);
                LogHelper.WriteLogC("短信发送出错" + ex.Message);
                return SMSCode.Exception;
            }
        }

    }



    /// <summary>
    /// 联通短信记录实体
    /// </summary>
    public class LTsmsUtil
    {
        /// <summary>
        /// 短信发送ID
        /// </summary>
        public decimal smsIndex { get; set; }

        /// <summary>
        /// 发送手机号
        /// </summary>
        public string phoneNumber { get; set; }

        /// <summary>
        /// 发送内容
        /// </summary>
        public string smsContent { get; set; }

        /// <summary>
        /// 短信发送时间
        /// </summary>
        public DateTime smsTime { get; set; }

        /// <summary>
        /// 短信发送状态
        /// </summary>
        public string resultCode { get; set; }

        /// <summary>
        /// 短信发送状态描述
        /// </summary>
        public string resultDesc { get; set; }
    }


    /// <summary>
    /// 请求结果Code枚举
    /// </summary>
    public enum SMSCode
    {
        #region "验证码短信接口调用方错误码"

        /// <summary>
        /// 短信发送失败
        /// </summary>
        [Description("短信发送失败")]
        Fail = -1,

        /// <summary>
        /// 短信发送成功
        /// </summary>
        [Description("短信发送成功")]
        Success = 0,

        /// <summary>
        /// 发送状态记录采集失败，结果为空
        /// </summary>
        [Description("发送记录采集失败，结果为空")]
        Exception = 1,

        #endregion
    }
}
