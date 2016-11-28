﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Wlzx.Utility;

namespace Wlzx.Task.TaskSet
{
    /// <summary>
    /// 发送消息任务
    /// </summary>
    ///<remarks>DisallowConcurrentExecution属性标记任务不可并行，要是上一任务没运行完即使到了运行时间也不会运行</remarks>
    [DisallowConcurrentExecution]
    public class SendMessageJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            string TName = "";
            try
            {
                DateTime start = DateTime.Now;
                TName=context.JobDetail.JobDataMap.Get("TaskName").ToString();
                //LogHelper.TaskWriteLog("------------------发送信息任务开始执行 " + start.ToString("yyyy-MM-dd HH:mm:ss") + " BEGIN-----------------------------", TName);
                //取出所有当前待发送的消息
                List<Message> listWait = SQLHelper.ToList<Message>(strSQL2);
                
                bool isSucess = false;   
                if (listWait == null || listWait.Count == 0)
                {
                    LogHelper.TaskWriteLog("当前没有等待发送的消息!", TName);
                    //TaskLog.SendMessageLogInfo.WriteLogE("当前没有等待发送的消息!");
                }
                else
                {
                    foreach (Message item in listWait)
                    {
                        //检查此短信是否发过
                        Message itemtemp = item;
                        CheckIsSend(ref itemtemp);//先进行发送检查
                        if(string.IsNullOrWhiteSpace(itemtemp.Receiver))
                        {
                            MessageHelper.RemoveMessageWithoutHis(itemtemp.MessageGuid);//此条信息所有收信人在一天内均已发过,且超过3次
                            LogHelper.TaskWriteLog("接收者:" + item.Receiver + "，内容:" + itemtemp.Content + "超过" + SysConfig.SmsMax.ToString() + "次，未发出报警信息", "未发出报警信息");
                        }
                        else
                        {
                            LogHelper.TaskWriteLog("接收者:" + item.Receiver + "，内容:" + itemtemp.Content, "发出报警信息");
                            isSucess = MessageHelper.SendMessage(itemtemp);
                            //LogHelper.TaskWriteLog(string.Format("接收人:{0},类型:{1},内容:“{2}”的消息发送{3}", item.Receiver, item.Type.ToString(), item.Content, isSucess ? "成功" : "失败"), TName);
                        }
                    }
                }
                DateTime end = DateTime.Now;
                //LogHelper.TaskWriteLog("\r\n\r\n------------------发送信息任务完成:" + end.ToString("yyyy-MM-dd HH:mm:ss") + ",本次共耗时(分):" + (end - start).TotalMinutes + " END------------------------\r\n\r\n\r\n\r\n", TName);
            }
            catch (Exception ex)
            {
                JobExecutionException e2 = new JobExecutionException(ex);
                //TaskLog.SendMessageLogError.WriteLogE("发送信息任务异常", ex);
                LogHelper.TaskWriteLog("发送信息任务异常", TName,"error",ex);

                //1.立即重新执行任务 
                e2.RefireImmediately = true;
            }
        }


        ///// <summary>
        ///// 取待发送消息(与历史表进行对比,超过3分钟最长的第一条)
        ///// </summary>
        //private static readonly string strSQL = @"
        //        SELECT MessageGuid,Receiver,Content,Subject,Type,CreatedOn FROM (       
        //         SELECT *,ROW_NUMBER() OVER ( PARTITION BY Receiver ORDER BY Interval DESC ) AS RowNum FROM 
        //         (
        //          SELECT A.*,DATEDIFF(MINUTE,ISNULL(B.SendOn,'1900-01-01'),A.CreatedOn) AS Interval FROM dbo.p_Message AS A
        //          LEFT JOIN 
        //          (       
        //           SELECT  
        //            Receiver ,SendOn,
        //            ROW_NUMBER() OVER ( PARTITION BY Receiver ORDER BY SendOn DESC ) AS Num
        //           FROM    dbo.p_MessageHistory 
        //          )AS B
        //          ON A.Receiver = B.Receiver AND B.Num=1
        //         )AS C
        //        )AS D
        //        WHERE D.RowNum=1";

        /// <summary>
        /// 取带发送消息(与数据库时间进行对比,超过3分钟最长的第一条)
        /// </summary>
        //private static readonly string strSQL1 = @"
        //    SELECT MessageGuid,Receiver,Content,Subject,Type,CreatedOn FROM (
        //     SELECT  * ,
        //       ROW_NUMBER() OVER ( PARTITION BY Receiver ORDER BY CreatedOn DESC ) AS RowNum
        //     FROM    dbo.p_Message
        //     WHERE   DATEDIFF(MINUTE, CreatedOn, GETDATE()) > 3
        //    )AS A
        //    WHERE A.RowNum=1";

        /// <summary>
        /// 取出p_Message表里面所有数据进行发送
        /// </summary>
        private static readonly string strSQL2 = @"SELECT MessageGuid,Receiver,Content,Subject,Type,CreatedOn FROM dbo.p_Message ";

        /// <summary>
        /// 检查p_Message表相同内容和收件人的信息是否需要发送,已发过3次，则将Receiver清空不报警
        /// </summary>
        public void CheckIsSend(ref Message message)
        {
            //#region 设置向前搜索的起始时间起点
            //DateTime oneDayStart = DateTime.Now;//初始化检索时间起点
            ////当时间为0点到9点之间，获取昨天9点的时间
            //if(DateTime.Now>=DateTime.Parse(DateTime.Now.ToShortDateString() + " 0:00:00") && DateTime.Now<=DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00"))
            //{
            //    oneDayStart = DateTime.Parse(DateTime.Now.AddDays(-1).ToShortDateString() + " 9:00:00");
            //}
            //else//当时间为9点到24点之间，获取当天9点的时间
            //{
            //    oneDayStart = DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00");
            //}
            //#endregion

            DateTime oneDayStart = TaskHelper.GetDayStart();
            
            //将收信人提取
            string[] Rcvers = message.Receiver.Split(',');
            foreach (string Rcver in Rcvers)
            {
                List<MessageHistory> listHistory = SQLHelper.ToList<MessageHistory>(strSQLHistory, new { Receiver = Rcver, Content = message.Content.Trim(), SendOn = oneDayStart });
                if (listHistory.Count < SysConfig.SmsMax)//少于报警3次则报警
                {
                    if(listHistory.Count>0)//一次以上
                    {
                        //未超过10，20，30等整数时间则不发送报警
                        if ((DateTime.Now - listHistory[0].SendOn).Minutes < listHistory.Count * SysConfig.SmsSpan) 
                        {
                            string newrev;
                            if (message.Receiver.Contains(Rcver.Trim() + ","))   //该号码不在末尾
                            {
                                newrev = message.Receiver.Replace(Rcver.Trim() + ",", "");
                            }
                            else
                            {
                                newrev = message.Receiver.Replace(Rcver.Trim(), "");//该号码在末尾
                            }
                            message.Receiver = newrev; //删除此收信人
                        }
                        else
                        {
                            message.Content = "第" + listHistory.Count.ToString() + "次提醒:" + message.Content;
                        }
                    }
                }
                else//超过报警3次，则不报
                {
                    //不报警
                    string newrev;
                    if (message.Receiver.Contains(Rcver.Trim() + ","))   //该号码不在末尾
                    {
                        newrev = message.Receiver.Replace(Rcver.Trim() + ",", "");
                    }
                    else
                    {
                        newrev = message.Receiver.Replace(Rcver.Trim(), "");//该号码在末尾
                    }
                    message.Receiver = newrev;//删除此收信人
                }
            }
        }

        /// <summary>
        /// 取出p_MessageHistory表里面所有已发送的数据
        /// </summary>
        private static readonly string strSQLHistory = @"SELECT [MessageGuid],[Receiver],[Type],[Content],[Subject],[CreatedOn],[SendOn],[Remark],[FromType],[FkGUID] FROM dbo.p_MessageHistory where Receiver=@Receiver and Content=@Content and SendOn>@SendOn order by SendOn";
    }
}
