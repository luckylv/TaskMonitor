using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Wlzx.Utility;
using System.IO;
using System.Text.RegularExpressions;
using Wlzx.Task.Utils;
using Newtonsoft.Json;

namespace Wlzx.Task.TaskSet
{
    /// <summary>
    /// 自动站监控任务
    /// </summary>
    ///<remarks>DisallowConcurrentExecution属性标记任务不可并行，要是上一任务没运行完即使到了运行时间也不会运行</remarks>
    [DisallowConcurrentExecution]
    public class AwsMonitorJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                //获取任务执行参数,任务启动时会读取配置文件TaskConfig.xml节点TaskParam的值传递过来
                LogHelper.WriteLogC("本次任务开始运行");
                object objParam = context.JobDetail.JobDataMap.Get("TaskParam");
                object LastRunTimeParam = context.JobDetail.JobDataMap.Get("LastRunTime");
                double RunTimeSp=1;  //默认向上搜索时间为1
                DateTime RunAt=DateTime.Now;
                DateTime LastRunAt=DateTime.Now;

                if(context.PreviousFireTimeUtc==null)
                {
                    LastRunAt=(DateTime)LastRunTimeParam;
                    LogHelper.WriteLogC("PreviousFireTimeUtc is null");
                }
                else
                {
                    LastRunAt = context.PreviousFireTimeUtc.Value.LocalDateTime;
                    //context.PreviousFireTimeUtc.Value.DateTime.AddHours(8).ToString("yyyy-MM-dd HH:mm:ss")
                }
                    

                if (LastRunTimeParam != null)
                {
                    //LogHelper.WriteLog(string.Format("任务“{0}”启动成功,未来5次运行时间如下:", taskUtil.TaskName));
                    //List<DateTime> list = QuartzHelper.GetTaskeFireTime(CronExpressionStringParam.ToString(), 1);

                    TimeSpan ts = RunAt - LastRunAt;
                    RunTimeSp = ts.TotalMinutes + 1;    //获取最后一次运行时间+1分钟，作为日志向上搜索时间
                }

                if (objParam != null)
                {
                    
                    //JsonConvert.DeserializeObject<AwsParam>(objParam.ToString(),JsonSerializerSettings.)
                    AwsParam Param = JsonConvert.DeserializeObject<AwsParam>(objParam.ToString().Replace(@"\",@"\\"));//将参数加入转义符后解析
                    LogHelper.WriteLogC("参数为:LogUrl:" + Param.LogUrl + "   DTFormat:" +
                                                  Param.DTFormat + "   ServerID:" +
                                                  Param.ServerID + "   RegexStart:" +
                                                  Param.RegexStart + "   RegexOne:" +
                                                  Param.RegexOne + "   RunAt:" +
                                                  RunAt.ToString() + "   LastRun:" +
                                                  LastRunAt + "   RunTimeSpan:" +
                                                  RunTimeSp.ToString());


                }
                
                //LogHelper.WriteLog("测试任务，当前系统时间：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                double checkTimeMin = 20;
                bool isTimeUp = false;

                

                string[] tt = File.ReadAllLines(@"Z:\Log"+DateTime.Now.ToString("yyyyMMdd")+".txt", Encoding.Default);
                Regex regStart = new Regex(@"共上传(\d+)个文件所用时间为\s*(\d*\.\d*)秒");
                Regex regOne = new Regex(@"((\d{2}-?){3}\s+(\d{2}:?){3})\s+上传文件:\s+Z_SURF_I_(\w{5})-REG_(\d{14})_O_AWS_FTM.txt");
                for (int i = tt.Length - 1; i >= 0; i--)
                {
                    if (regStart.IsMatch(tt[i].ToString()))
                    {
                        var startResult = regStart.Match(tt[i].ToString()).Groups;
                        TaskLog.AwsMonitorLogInfo.WriteLogE(i + 1 + "行找到开始匹配，共需要插入" + startResult[1].ToString() + "条数据");

                        //LogHelper.WriteLog(i + 1 + "行找到开始匹配，共需要插入" + startResult[1].ToString() + "条数据");

                        i--;   //切换下一行
                        for (int j = 0; j < Convert.ToInt32(startResult[1].ToString()); j++)
                        {
                            if (regOne.IsMatch(tt[i - j].ToString()))
                            {
                                var result = regOne.Match(tt[i - j].ToString()).Groups;

                                /////////////////////////如果该检查时间早于设定checkTimeMin则停止
                                DateTime checkdt = Convert.ToDateTime(result[1].ToString());
                                if (checkdt.AddMinutes(checkTimeMin+1) < DateTime.Now)
                                {
                                    isTimeUp = true; 
                                    break;
                                }
                                /////////////////////////////////////////////////////////////////

                                TaskLog.AwsMonitorLogInfo.WriteLogE("更新一行数据: " + result[4].ToString() + "   " + result[1].ToString() + "   " + result[5].ToString());

                                //更新数据

                                string stationQuery = @"SELECT [Station],[ValidTime],[ValidNumber],[IsWarn] FROM [TaskManager].[dbo].[AwsMonitorTab] where Station=@Station";

                                string stationQueryState = SQLHelper.ExecuteScalar<string>(stationQuery, new { Station = result[4].ToString() });

                                if (stationQueryState == null)
                                {
                                    string insertAWS = @"INSERT INTO [TaskManager].[dbo].[AwsMonitorTab]
                                                              ([Station]
                                                              ,[ValidTime]
                                                              ,[ValidNumber]
                                                              ,[IsWarn])
                                                         VALUES(@Station,@ValidTime,@ValidNumber,@isWarn)";

                                    //插入一条发送记录
                                    //TaskLog.AwsMonitorLogInfo.WriteLogE("插入一行数据: " + result[4].ToString() + "   " + result[1].ToString() + "   " + result[5].ToString());
                                    SQLHelper.ExecuteNonQuery(insertAWS, new { Station = result[4].ToString(), ValidTime = Convert.ToDateTime(result[1].ToString()), ValidNumber = result[5].ToString(),isWarn=true });

                                }
                                else
                                {
                                    string updateAWS = @"UPDATE [TaskManager].[dbo].[AwsMonitorTab]
                                                          SET [ValidTime] = '"+Convert.ToDateTime(result[1].ToString())+
                                                           @"',[ValidNumber] = "+result[5].ToString()+
                                                          " WHERE Station='" + result[4].ToString() + "' and ValidTime<'" + Convert.ToDateTime(result[1].ToString()) + "'";
                                    //TaskLog.AwsMonitorLogInfo.WriteLogE(updateAWS);
//                                    string updateAWS1 = @"UPDATE [TaskManager].[dbo].[AwsMonitorTab]
//                                                          SET [ValidTime] = '@ValidTime'" +
//                                                           @",[ValidNumber] = @ValidNumber"+
//                                                          " WHERE Station='@Station' and ValidTime<'@ValidTime'";
                                    //更新一条数据
                                    SQLHelper.ExecuteNonQuery(updateAWS);
                                    //SQLHelper.ExecuteNonQuery(updateAWS1, new { Station = result[4].ToString(), ValidTime = Convert.ToDateTime(result[1].ToString()), ValidNumber = result[5].ToString()});
                                }



                                //LogHelper.WriteLog("更新一行数据: " + result[4].ToString() + "   " + result[1].ToString() + "   " + result[5].ToString());
                            }
                        }
                        if (isTimeUp)
                        {
                            TaskLog.AwsMonitorLogInfo.WriteLogE("中断处行号: "+i.ToString());
                            break;
                        }
                            

                        i = i - Convert.ToInt32(startResult[1].ToString()) + 1;
                    }
                }
            
            
            
            }
            catch (Exception ex)
            {
                JobExecutionException e2 = new JobExecutionException(ex);
                LogHelper.WriteLog("测试任务异常", ex);
                //1.立即重新执行任务 
                e2.RefireImmediately = true;
                //2 立即停止所有相关这个任务的触发器
                //e2.UnscheduleAllTriggers=true; 
            }
        }
    }


    /// <summary>
    /// 执行参数
    /// </summary>
    public class AwsParam
    {
        /// <summary>
        /// 日志文件路径,用DTFormat代替日期部分
        /// </summary>
        public string LogUrl { get; set; }

        /// <summary>
        /// 文件名中的日期格式设定
        /// </summary>
        public string DTFormat { get; set; }

        /// <summary>
        /// 服务器标示
        /// </summary>
        public string ServerID { get; set; }

        /// <summary>
        /// 单次匹配起点正则表达式
        /// </summary>
        public string RegexStart { get; set; }

        /// <summary>
        /// 匹配单条记录正则表达式
        /// </summary>
        public string RegexOne { get; set; }

        /// <summary>
        /// 每次检索的向前时间跨度（分钟）
        /// </summary>
        public int RunTimeSpan { get; set; }
    }
}
