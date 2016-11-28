using System;
using System.Text;
using Quartz;
using Wlzx.Utility;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wlzx.Utility.Quartz;

namespace Wlzx.Task.TaskSet
{
    /// <summary>
    /// 自动站传输软件日志监控任务
    /// </summary>
    ///<remarks>DisallowConcurrentExecution属性标记任务不可并行，要是上一任务没运行完即使到了运行时间也不会运行</remarks>
    [DisallowConcurrentExecution]
    public class AwsMonitorJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            string TName = "AWS";    //初始化任务名变量
            try
            {
                #region 该段为获取任务执行参数
                //获取任务执行参数,任务启动时会读取配置文件TaskConfig.xml节点TaskParam的值传递过来
                TName = context.JobDetail.JobDataMap.Get("TaskName").ToString();   //获取任务名称
                object objParam = context.JobDetail.JobDataMap.Get("TaskParam");
                MonitorParam Param = null;                      //TaskParam参数变量
                string logFileFull = "";  //日志文件完整路径
                if (objParam != null)  //成功获取任务参数
                {
                    Param = JsonConvert.DeserializeObject<MonitorParam>(objParam.ToString().Replace(@"\", @"\\"));//将参数\加入转义符后解析为\\
                    //获取文件完整路径
                    if (!String.IsNullOrWhiteSpace(Param.ShareSub))      //若映射目录没有下级子目录
                    {
                        if (!String.IsNullOrWhiteSpace(Param.SubDTFormat))
                        {
                            logFileFull = @"\\" + Param.Url + @"\" + Param.ShareName + @"\" + Param.ShareSub.ToString().Replace("SubDTFormat", DateTime.Now.ToString(Param.SubDTFormat.ToString())) + @"\" + Param.LogFile.Replace("DTFormat", DateTime.Now.ToString(Param.DTFormat));
                        }
                        else
                        {
                            logFileFull = @"\\" + Param.Url + @"\" + Param.ShareName + @"\" + Param.ShareSub.ToString() + @"\" + Param.LogFile.Replace("DTFormat", DateTime.Now.ToString(Param.DTFormat));
                        }
                    }
                    else
                    {
                        logFileFull = @"\\" + Param.Url + @"\" + Param.ShareName + @"\" + Param.LogFile.Replace("DTFormat", DateTime.Now.ToString(Param.DTFormat));
                    }
                }
                else
                {
                    LogHelper.WriteErrorAndC(TName + " 任务监控参数TaskParam获取失败,请检查数据库中该项设置！"); 
                    LogHelper.TaskWriteLog(TName + " 任务监控参数TaskParam获取失败,请检查数据库中该项设置！",TName,"error");  //单独任务日志中再写一次
                    return;
                }
                #endregion

                #region 该段为获取日志每次向前查找的时间点
                object LastRunTimeParam = context.JobDetail.JobDataMap.Get("LastRunTime");
                double RunTimeSp = 10;  //默认向上搜索时间为10分钟
                RunTimeSp = Convert.ToDouble(Param.RunTimeSPMin);   //从任务参数中获取指定的提前量
                DateTime RunAt = DateTime.Now;      //本次运行时间
                DateTime LastRunAt = DateTime.Now.AddHours(-2);  //上次运行时间，默认为前推2小时
                bool isTimeUp = false;   //向上搜索到达标志

                if (LastRunTimeParam == null && context.PreviousFireTimeUtc == null)  //如果无法从数据库和任务中获取上次运行时间，通常为任务刚建立时
                {
                    LastRunAt = RunAt.AddHours(-2);
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
                RunTimeSp = ts.TotalMinutes + RunTimeSp;    //获取最后一次运行时间+指定的提前量分钟，作为日志向上搜索时间
                #endregion

                LogHelper.WriteLogAndC("本次\"" + TName + "\"任务开始运行,搜索文件为：" + logFileFull + " ,上一次运行在" + LastRunAt.ToString() + " ,向上搜索至" + RunAt.AddMinutes(-RunTimeSp).ToString());
                LogHelper.TaskWriteLog("本次\"" + TName + "\"任务开始运行,搜索文件为：" + logFileFull + " ,上一次运行在" + LastRunAt.ToString() + " ,向上搜索至" + RunAt.AddMinutes(-RunTimeSp).ToString(), TName);

                string[] tt = File.ReadAllLines(logFileFull, Encoding.Default);  //读取整个日志文件
                long calstation = 0;   //更新记录计数器
                Regex regStart = new Regex(Param.RegexStart);
                Regex regOne = new Regex(Param.RegexOne);

                LogHelper.TaskWriteLog("regStart正则表达式为："+regStart.ToString(), TName);
                LogHelper.TaskWriteLog("regOne正则表达式为："+regOne.ToString(), TName);

                for (int i = tt.Length - 1; i >= 0; i--)
                {

                    if (regStart.IsMatch(tt[i].ToString()))    //正则表达式匹配
                    {
                        var startResult = regStart.Match(tt[i].ToString()).Groups;
                        LogHelper.TaskWriteLog(i + 1 + "行找到开始匹配，共需要检查" + startResult["upfiles"].ToString() + "条数据", TName);

                        i--;   //切换下一行
                        for (int j = 0; j < Convert.ToInt32(startResult["upfiles"].ToString()); j++)
                        {
                            if (regOne.IsMatch(tt[i - j].ToString()))
                            {
                                var result = regOne.Match(tt[i - j].ToString()).Groups;

                                /////////////////////////如果该检查时间早于设定checkTimeMin则停止
                                DateTime checkdt = Convert.ToDateTime(result["uptime"].ToString());
                                if (checkdt.AddMinutes(RunTimeSp) < RunAt)
                                {
                                    isTimeUp = true;
                                    break;
                                }
                                /////////////////////////////////////////////////////////////////

                                
                                //更新服务器信息
                                string UpdateServerSQL = @"UPDATE  " + Param.DBTable + " SET Server=@Server WHERE Station=@Station and Server<>@Server";
                                int svrChgNum = SQLHelper.ExecuteNonQuery(UpdateServerSQL, new { Server = Param.Url, Station = result["station"].ToString() });
                                if(svrChgNum>0)
                                {
                                    LogHelper.TaskWriteLog("更改了" + result["station"].ToString()+"的服务器信息", TName);
                                    LogHelper.WriteLogAndC("更改了" + result["station"].ToString() + "的服务器信息");
                                }


                                //更新数据
                                //string stationQuery = @"SELECT [Station],[Server],[ValidTime],[ValidNumber],[IsWarn] FROM " + Param.DBTable + " where Station=@Station and Server=@Server";
                                string stationQuery = @"SELECT [ValidTime] FROM " + Param.DBTable + " where Station=@Station and Server=@Server";

                                string stationQueryState = SQLHelper.ExecuteScalar<string>(stationQuery, new { Station = result["station"].ToString(), Server = Param.Url });

                                if (stationQueryState == null)
                                {
                                    string insertAWS = @"INSERT INTO " + Param.DBTable + " ([Station],[Server],[ValidTime],[ValidNumber],[IsWarn],[Warning],[AutoResume]) VALUES (@Station,@Server,@ValidTime,@ValidNumber,@isWarn,@Warning,@AutoResume)";
                                    //插入一条新站发送记录
                                    LogHelper.TaskWriteLog("插入一行数据: " + result["station"].ToString() + "   " + Param.Url + "   " + result["uptime"].ToString() + "   " + result["time"].ToString(), TName);
                                    calstation += SQLHelper.ExecuteNonQuery(insertAWS, new { Station = result["station"].ToString(), Server = Param.Url, ValidTime = Convert.ToDateTime(result["uptime"].ToString()), ValidNumber = result["time"].ToString(), isWarn = true, Warning = false, AutoResume=true });

                                }
                                else
                                {
                                    string updateAWS = @"UPDATE " + Param.DBTable + " SET [ValidTime] = '" + Convert.ToDateTime(result["uptime"].ToString()) +
                                                           @"',[ValidNumber] = '" + result["time"].ToString() +
                                                           @"',[Warning] = 'false'" + 
                                                           " WHERE Station='" + result["station"].ToString() + "' and ValidTime<'" + Convert.ToDateTime(result["uptime"].ToString()) + "' and Server='" + Param.Url + "'";
                                    
                                    if(Convert.ToDateTime(stationQueryState)<Convert.ToDateTime(result["uptime"].ToString()))
                                    {
                                        //更新一条发送数据
                                        LogHelper.TaskWriteLog("更新一行数据: " + result["station"].ToString() + "   " + Param.Url + "   " + result["uptime"].ToString() + "   " + result["time"].ToString(), TName);
                                    }
                                    calstation += SQLHelper.ExecuteNonQuery(updateAWS);
                                }
                            }
                        }
                        if (isTimeUp)
                        {
                            LogHelper.TaskWriteLog("中断处行号: " + (i+1).ToString(), TName);
                            break;
                        }
                        i = i - Convert.ToInt32(startResult["upfiles"].ToString()) + 1;
                    }
                }
                LogHelper.WriteLogAndC("本次\"" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + "\"任务运行结束,共更新数据：" + calstation.ToString() + "条");
                LogHelper.TaskWriteLog("本次\"" + context.JobDetail.JobDataMap.Get("TaskName").ToString() + "\"任务运行结束,共更新数据：" + calstation.ToString() + "条", TName);
            }
            catch (Exception ex)
            {
                JobExecutionException e2 = new JobExecutionException(ex);
                LogHelper.WriteError(TName+"任务监控任务异常", ex);
                LogHelper.TaskWriteLog(TName + "任务监控任务异常", TName, "error", ex);
                //1.立即重新执行任务 
                e2.RefireImmediately = true;
                //2 立即停止所有相关这个任务的触发器
                //e2.UnscheduleAllTriggers=true; 
            }
        }
    }


    
}
