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
using Wlzx.Utility.Quartz;

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
                object objParam = context.JobDetail.JobDataMap.Get("TaskParam");
                object LastRunTimeParam = context.JobDetail.JobDataMap.Get("LastRunTime");
                double RunTimeSp=1;  //默认向上搜索时间为1
                DateTime RunAt = DateTime.Now;      //本次运行时间
                DateTime LastRunAt = DateTime.Now.AddDays(-1);  //上次运行时间
                string logFileFull = "";  //日志文件完整路径
                string SrvID = "";       //自动站服务器标示
                string DBTable = "";     //数据库表名
                bool isTimeUp = false;   //向上搜索到达标志
                MonitorParam Param = null;

                if (LastRunTimeParam == null && context.PreviousFireTimeUtc == null)
                {
                    LastRunAt = DateTime.Now.AddDays(-1);
                }
                else if (context.PreviousFireTimeUtc == null) //如果无法从任务本身获取上次运行时间
                {
                    LastRunAt = (DateTime)LastRunTimeParam;  //以任务参数方式从数据库中获取最后一次运行时间
                }
                else
                {
                    LastRunAt = context.PreviousFireTimeUtc.Value.LocalDateTime; //直接获取上次运行时间
                }


                TimeSpan ts = RunAt - LastRunAt;
                RunTimeSp = ts.TotalMinutes + 1;    //获取最后一次运行时间+1分钟，作为日志向上搜索时间

     
                if (objParam != null)  //成功获取任务参数
                {
                    Param = JsonConvert.DeserializeObject<MonitorParam>(objParam.ToString().Replace(@"\", @"\\"));//将参数\加入转义符后解析为\\
                    //LogHelper.WriteLogC("参数为:LogUrl:" + Param.LogUrl + "   DTFormat:" +
                    //                              Param.DTFormat + "   ServerID:" +
                    //                              Param.ServerID + "   RegexStart:" +
                    //                              Param.RegexStart + "   RegexOne:" +
                    //                              Param.RegexOne + "   RunAt:" +
                    //                              RunAt.ToString() + "   LastRun:" +
                    //                              LastRunAt + "   RunTimeSpan:" +
                    //                              RunTimeSp.ToString());

                    //if (!ConfigInit.Connect(Param.Url, Param.ShareName, Param.User, Param.Pw))
                    //{
                    //    LogHelper.WriteLogC("任务:" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + " 共享文件夹映射失败！");
                    //    LogHelper.WriteError("任务:" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + " 共享文件夹映射失败！");
                    //    return;
                    //}

                    //获取文件完整路径
                    logFileFull = @"\\" + Param.Url + @"\" + Param.ShareName + @"\" + Param.LogFile.Replace("DTFormat", DateTime.Now.ToString(Param.DTFormat));
                    SrvID = Param.Url;
                    DBTable = Param.DBTable;
                }

                if (Param == null)
                {
                    LogHelper.WriteLogC("参数为空");
                    return;
                }

                LogHelper.WriteLogC("本次\"" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + "\"任务开始运行,搜索文件为：" + logFileFull);
                LogHelper.WriteLog("本次\"" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + "\"任务开始运行,搜索文件为：" + logFileFull);
                TaskLog.AwsMonitorLogInfo.WriteLogE("本次\"" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + "\"任务开始运行,搜索文件为：" + logFileFull);

                string[] tt = File.ReadAllLines(logFileFull, Encoding.Default);
                Regex regStart = new Regex(Param.RegexStart);
                Regex regOne = new Regex(Param.RegexOne);
                for (int i = tt.Length - 1; i >= 0; i--)
                {
                    Console.WriteLine("1" + Param.RegexStart);
                    if (regStart.IsMatch(tt[i].ToString()))    //正则表达式匹配
                    {
                        Console.WriteLine("2" + Param.RegexOne);
                        var startResult = regStart.Match(tt[i].ToString()).Groups;
                        TaskLog.AwsMonitorLogInfo.WriteLogE(i + 1 + "行找到开始匹配，共需要插入" + startResult[1].ToString() + "条数据");

                        i--;   //切换下一行
                        for (int j = 0; j < Convert.ToInt32(startResult[1].ToString()); j++)
                        {
                            if (regOne.IsMatch(tt[i - j].ToString()))
                            {
                                var result = regOne.Match(tt[i - j].ToString()).Groups;
                                Console.WriteLine("3" + Param.RegexOne);
                                /////////////////////////如果该检查时间早于设定checkTimeMin则停止
                                DateTime checkdt = Convert.ToDateTime(result[1].ToString());
                                if (checkdt.AddMinutes(RunTimeSp) < DateTime.Now)
                                {
                                    Console.WriteLine("4" + Param.RegexOne);
                                    isTimeUp = true; 
                                    break;
                                }
                                /////////////////////////////////////////////////////////////////
                                Console.WriteLine("5" + Param.RegexOne);
                                //更新数据

                                string stationQuery = @"SELECT [Station],[Server],[ValidTime],[ValidNumber],[IsWarn] FROM " + DBTable + " where Station=@Station and Server=@Server";

                                string stationQueryState = SQLHelper.ExecuteScalar<string>(stationQuery, new { Station = result[4].ToString(), Server = SrvID });

                                if (stationQueryState == null)
                                {
                                    string insertAWS = @"INSERT INTO " + DBTable + " ([Station],[Server],[ValidTime],[ValidNumber],[IsWarn]) VALUES (@Station,@Server,@ValidTime,@ValidNumber,@isWarn)";

                                    LogHelper.WriteLogC(insertAWS);
                                    //插入一条新站发送记录
                                    TaskLog.AwsMonitorLogInfo.WriteLogE("插入一行数据: " + result[4].ToString() + "   " + result[1].ToString() + "   " + result[5].ToString());
                                    SQLHelper.ExecuteNonQuery(insertAWS, new { Station = result[4].ToString(), Server = SrvID, ValidTime = Convert.ToDateTime(result[1].ToString()), ValidNumber = result[5].ToString(), isWarn = true });

                                }
                                else
                                {
                                    string updateAWS = @"UPDATE " + DBTable + " SET [ValidTime] = '"+Convert.ToDateTime(result[1].ToString())+
                                                           @"',[ValidNumber] = "+result[5].ToString()+
                                                           " WHERE Station='" + result[4].ToString() + "' and ValidTime<'" + Convert.ToDateTime(result[1].ToString()) + "' and Server='" + SrvID + "'";
                                                          //" WHERE Station='" + result[4].ToString() + "' and ValidTime<'" + Convert.ToDateTime(result[1].ToString()) + "'";

                                    //更新一条发送数据
                                    TaskLog.AwsMonitorLogInfo.WriteLogE("更新一行数据: " + result[4].ToString() + "   " + result[1].ToString() + "   " + result[5].ToString());
                                    SQLHelper.ExecuteNonQuery(updateAWS);
                                }   
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
                LogHelper.WriteError("自动站监控任务异常", ex);
                //1.立即重新执行任务 
                e2.RefireImmediately = true;
                //2 立即停止所有相关这个任务的触发器
                //e2.UnscheduleAllTriggers=true; 
            }
        }
    }


    
}
