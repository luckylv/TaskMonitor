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
        //联通发送SQL
        private static string LTinsertSQL = @"INSERT INTO [ShortMsg_New].[dbo].[SendSms]
                                                ([phoneNumber]
                                                ,[smsContent]
                                                ,[smsTime]
                                                ,[smsUser]
                                                ,[status])
                            VALUES(@phoneNumber,@smsContent,getdate(),7,0)";
        //联通获取短信ID
        private static string LTqueryIdSQL = @"Select MAX(smsIndex) AS ID FROM [ShortMsg_New].[dbo].[SendSms]";
        //联通获取该ID发送状态
        private static string LTqueryStatus = @"Select * FROM [ShortMsg_New].[dbo].[SendSms] where smsIndex=@smsId";


        //移动发送SQL
        private static string YDinsertSQL = @"INSERT INTO [DB_CustomSMS].[dbo].[tbl_SMSendTask]
                                                ([CreatorID]
                                                ,[SmSendedNum]
                                                ,[OperationType]
                                                ,[SendType]
                                                ,[OrgAddr]
                                                ,[DestAddr]
                                                ,[SM_Content]
                                                ,[NeedStateReport]
                                                ,[ServiceID]
                                                ,[FeeType]
                                                ,[FeeCode]
                                                ,[MsgID]
                                                ,[SMType]
                                                ,[MessageID]
                                                ,[DestAddrType]
                                                ,[TaskStatus]
                                                ,[SendLevel]
                                                ,[SendState]
                                                ,[TryTimes]
                                                ,[SuccessID])
                                          VALUES
                                               ('0000',0,'API',1,'18600000',@phoneNumber,@smsContent,1,'TZJ0010101','01','0','0',0,'0',0,0,0,0,3,0)";
        
        //移动获取短信ID
        private static string YDqueryIdSQL = @"Select MAX(ID) AS ID FROM [DB_CustomSMS].[dbo].[tbl_SMSendTask]";

        //移动获取该ID发送状态
        private static string YDqueryStatus = @"Select TaskStatus FROM [DB_CustomSMS].[dbo].[tbl_SMSendTask] where ID=@smsId";

        //移动号码段
        private static string[] YDZone = new String[] { "134", "135", "136", "137", "138", "139", "150", "151", "152", "157", "158", "159", "182", "183", "187", "188", "147", "145" };
        
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
                //添加短信接收人地址
                string[] receivers = receiver.Split(new char[] { ',' });
                foreach (string phone in receivers)
                {
                    //若发送失败，则重发此条短信一次
                    if (YDSendOneMessage(phone, content) != SMSCode.Success)
                    {
                        YDSendOneMessage(phone, content);
                    }
                }
                return SMSCode.Success;

            }
            catch (Exception ex)
            {
                LogHelper.WriteError("短信发送出错", ex);
                LogHelper.WriteLogC("短信发送出错" + ex.Message);
                return SMSCode.Exception;
            }
        }

        /// <summary>
        /// 发送短信v2
        /// </summary>
        /// <param name="receiver">短信接收人手机号码（多号码用,分隔）</param>
        /// <param name="content">短信内容</param>
        /// <returns>发送状态</returns>
        public static SMSCode SendMessage(ref Message message)
        {
            try
            {
                return SMSCode.Success;

                //#region 占时注释，测试用
                ////添加短信接收人地址
                //string[] receivers = message.Receiver.Split(new char[] { ',' });

                //foreach (string phone in receivers)
                //{
                //    if(YDZone.Contains(phone.Substring(0, 3))) //属于移动号码
                //    {
                //        if (YDSendOneMessage(phone, message.Content) == SMSCode.Success)
                //        {
                //            string newrev;
                //            if (message.Receiver.Contains(phone + ","))   //该号码不在末尾
                //            {
                //                newrev = message.Receiver.Replace(phone + ",", "");
                //            }
                //            else
                //            {
                //                newrev = message.Receiver.Replace(phone, "");//该号码在末尾
                //            }
                //             message.Receiver = newrev;
                //        }
                //    }
                //    else
                //    {
                //        if (LTSendOneMessage(phone, message.Content) == SMSCode.Success)
                //        {
                //            string newrev;
                //            if (message.Receiver.Contains(phone + ","))
                //            {
                //                newrev = message.Receiver.Replace(phone + ",", "");
                //            }
                //            else
                //            {
                //                newrev = message.Receiver.Replace(phone, "");
                //            }
                //            message.Receiver = newrev;
                //        }
                //    } 
                //}

                //if (string.IsNullOrWhiteSpace(message.Receiver.Trim()))  //如果名单已清空，则表示全部发送成功
                //{
                //    return SMSCode.Success;
                //}
                //else                    //名单非空则表示有短信未发出
                //{
                //    return SMSCode.Fail;
                //}
                //#endregion

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
                LogHelper.WriteLogC("短信发送出错" + ex.Message);
                return SMSCode.Exception;
            }
        }

        /// <summary>
        /// 移动发送单条短信
        /// </summary>
        /// <param name="oneReceiver">短信接收人手机号码</param>
        /// <param name="content">短信内容</param>
        /// <returns>发送状态</returns>
        public static SMSCode YDSendOneMessage(string oneReceiver, string content)
        {
            try
            {
                //插入一条发送记录
                DbHelper.ExecuteNonQuery(SysConfig.YDsmsConnect, YDinsertSQL, new { phoneNumber = oneReceiver, smsContent = content });
                //取出该条记录ID号
                string smsId = DbHelper.ExecuteScalar<string>(SysConfig.YDsmsConnect, YDqueryIdSQL, null);
                //检查短信发送状态 
                string YDSMSState = DbHelper.ExecuteScalar<string>(SysConfig.YDsmsConnect, YDqueryStatus, new { smsId = smsId });

                //若无状态则每隔1秒查一次状态，共5次
                int checkTime = 0;
                while (checkTime < 5 && string.Equals(YDSMSState, "0"))
                {
                    Thread.Sleep(1000);
                    //取出该条状态
                    YDSMSState = DbHelper.ExecuteScalar<string>(SysConfig.YDsmsConnect, YDqueryStatus, new { smsId = smsId });
                    if (YDSMSState == null)
                    {
                        string logstr = "发送短信错误，错误原因：第" + checkTime.ToString() + "次无法检索到短信发送状态，请检查数据库连接";
                        LogHelper.TaskWriteLog(logstr, "发送信息任务", "error");
                        LogHelper.WriteErrorAndC(logstr);
                        return SMSCode.Exception;
                    }
                }

                //5次内检索到状态
                if (checkTime < 5)
                {   //状态为0则发送成功
                    //状态为1则发送成功
                    if (string.Equals(YDSMSState, "1"))
                    {
                        string logstr = "成功发送短信至" + oneReceiver + "，内容:" + content;
                        LogHelper.TaskWriteLog(logstr, "发送信息任务");
                        LogHelper.WriteLogAndC(logstr);
                        return SMSCode.Success;
                    }
                    else//状态为非1则发送失败
                    {
                        string logstr = "发送短信失败，未到达" + oneReceiver + "，内容:" + content + "，失败原因：获取短信状态为非1";
                        LogHelper.TaskWriteLog(logstr, "发送信息任务", "error");
                        LogHelper.WriteErrorAndC(logstr);
                        return SMSCode.Fail;
                    }
                }
                else//5次内还未检索到状态
                {
                    string logstr = "发送短信失败，未到达" + oneReceiver + "，内容:" + content + "，失败原因：5次检索未取得短信发送状态";
                    LogHelper.TaskWriteLog(logstr, "发送信息任务", "error");
                    LogHelper.WriteErrorAndC(logstr);
                    return SMSCode.Fail;
                }

            }
            catch (Exception ex)
            {
                LogHelper.TaskWriteLog("短信发送出错" + ex.Message, "发送信息任务", "error");
                LogHelper.WriteErrorAndC("短信发送出错", ex);
                return SMSCode.Exception;
            }
        }

        /// <summary>
        /// 联通发送单条短信
        /// </summary>
        /// <param name="oneReceiver">短信接收人手机号码</param>
        /// <param name="content">短信内容</param>
        /// <returns>发送状态</returns>
        public static SMSCode LTSendOneMessage(string oneReceiver, string content)
        {
            try
            {
                //插入一条发送记录
                DbHelper.ExecuteNonQuery(SysConfig.LTsmsConnect, LTinsertSQL, new { phoneNumber = oneReceiver, smsContent = content});
                //取出该条记录ID号
                string smsId = DbHelper.ExecuteScalar<string>(SysConfig.LTsmsConnect, LTqueryIdSQL, null);

                LTsmsUtil current = null;

                //若无状态则每隔1秒查一次状态，共5次
                int checkTime = 0;
                while (checkTime < 5 && string.IsNullOrEmpty(current.resultCode))
                {
                    Thread.Sleep(1000);
                    current = DbHelper.Single<LTsmsUtil>(SysConfig.LTsmsConnect, LTqueryStatus, new { smsId = smsId });
                    if (current == null)
                    {
                        string logstr = "发送短信错误，错误原因：第" + checkTime+1.ToString() + "次无法检索到短信发送状态，请检查数据库连接";
                        LogHelper.TaskWriteLog(logstr, "发送信息任务","error");
                        LogHelper.WriteErrorAndC(logstr);
                        return SMSCode.Exception;
                    }
                }
                //5次内检索到状态
                if (checkTime < 5)
                {   //状态为0则发送成功
                    if (Convert.ToInt32(current.resultCode) == 0)
                    {
                        string logstr = "成功发送短信至" + current.phoneNumber + "，内容:" + current.smsContent;
                        LogHelper.TaskWriteLog(logstr, "发送信息任务");
                        LogHelper.WriteLogAndC(logstr);
                        return SMSCode.Success;
                    }
                    else//状态为非0则发送失败
                    {
                        string logstr = "发送短信失败，未到达" + current.phoneNumber + "，内容:" + current.smsContent + "，失败原因：" + current.resultDesc;
                        LogHelper.TaskWriteLog(logstr, "发送信息任务", "error");
                        LogHelper.WriteErrorAndC(logstr);
                        return SMSCode.Fail;
                    }
                }
                else//5次内还未检索到状态
                {
                    string logstr = "发送短信失败，未到达" + current.phoneNumber + "，内容:" + current.smsContent + "，失败原因：5次检索未取得短信发送状态";
                    LogHelper.TaskWriteLog(logstr, "发送信息任务", "error");
                    LogHelper.WriteErrorAndC(logstr);
                    return SMSCode.Fail;
                }

            }
            catch (Exception ex)
            {
                LogHelper.TaskWriteLog("短信发送出错" + ex.Message, "发送信息任务", "error");
                LogHelper.WriteErrorAndC("短信发送出错", ex);
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
