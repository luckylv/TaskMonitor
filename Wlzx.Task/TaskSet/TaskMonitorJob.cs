using System;
using System.Text;
using Quartz;
using Wlzx.Utility;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Wlzx.Utility.Quartz;
using System.Data;
using System.Collections.Generic;

namespace Wlzx.Task.TaskSet
{
    /// <summary>
    /// 所有任务的监控轮询任务
    /// </summary>
    ///<remarks>DisallowConcurrentExecution属性标记任务不可并行，要是上一任务没运行完即使到了运行时间也不会运行</remarks>
    [DisallowConcurrentExecution]
    public class TaskMonitorJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            try
            {
                string sqlstr = @"SELECT [TaskID],[TaskName],[TaskParam],[CronExpressionString],[Assembly],[Class],[Status],[CreatedOn]
                                  ,[ModifyOn],[RecentRunTime],[LastRunTime],[CronRemark],[Remark]
                            FROM [TaskManager].[dbo].[p_Task]";
                DataTable dt = SQLHelper.FillDataTable(sqlstr);
                string ResumeStr = "";//报警站恢复字符串
                Dictionary<string, Dictionary<string, string>> warningStrs = new Dictionary<string, Dictionary<string, string>>();   //任务报警信息合集

                #region 查找每个任务的报警信息
                foreach (DataRow row in dt.Rows)
                {
                    //如果任务未在运行或者是"全局任务监控"，则跳过
                    if (!Convert.ToBoolean(row["Status"]) || String.Equals(row["TaskName"].ToString(), "全局任务监控", StringComparison.CurrentCultureIgnoreCase))   
                    {
                        continue;
                    }
                    else
                    {
                        string TaskName = row["TaskName"].ToString();   //任务名称

                        string TaskParam = row["TaskParam"].ToString();
                        MonitorParam Param = null;                      //TaskParam参数变量

                        if (TaskParam != null)  //成功获取任务参数
                        {
                            Param = JsonConvert.DeserializeObject<MonitorParam>(TaskParam.Replace(@"\", @"\\"));//将参数\加入转义符后解析为\\ 
                        }
                        else
                        {
                            LogHelper.WriteErrorAndC(TaskName + " 任务监控参数TaskParam获取失败,无法监控状态！");
                            return;
                        }

                        if (Param != null)   //TaskParam参数解析成功
                        {
                            #region 每日早上9：00恢复所有报警
                            if (context.PreviousFireTimeUtc != null)
                            {
                                if (context.PreviousFireTimeUtc.Value.LocalDateTime < DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00") && DateTime.Now > DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00"))
                                {
                                    string updateResumeState = @"UPDATE " + Param.DBTable + " SET [IsWarn] = 'true',[AutoResume]='true'";
                                    SQLHelper.ExecuteNonQuery(updateResumeState);
                                    LogHelper.WriteLogAndC(TaskName+"恢复了所有站报警");
                                    LogHelper.TaskWriteLog(TaskName + "恢复了所有站报警", TaskName);
                                }
                            }
                            #endregion

                            #region 获得本任务上一次的运行时间
                            DateTime? lastRunTime;
                            if(row["LastRunTime"]==null)
                            {
                                lastRunTime = DateTime.Now.AddHours(-2);//上次运行时间，如果从数据库获取不到，默认为前推2小时
                            }
                            else
                            {
                                lastRunTime = Convert.ToDateTime(row["LastRunTime"]);  //从数据库任务表中的LastRunTime获取
                            }
                            #endregion

                            //1.5倍RunTimeSPMin作为容错时间
                            DateTime checkTime = DateTime.Now.AddMinutes(-Convert.ToDouble(Param.RunTimeSPMin) / 2 * 3);  
                            
                            
                            if (lastRunTime < checkTime)    //上一次运行时间过远，等待最近时次运行后再监控
                            {
                                if (lastRunTime < DateTime.Now.AddHours(-6))
                                {
                                    LogHelper.WriteLogAndC(TaskName + " 任务已6小时未运行，请检查！");
                                    //发短信?
                                }
                            }
                            else
                            {
                                #region 对已恢复的数据还原报警开关
                                //判断任务参数中的自动恢复是否为true

                                string stationResumeQuery = @"SELECT [Station],[ValidTime] FROM " + Param.DBTable + " where ValidTime>='" + checkTime + "' and IsWarn='false' and AutoResume='true'";
                                DataTable dtResumeMiss = SQLHelper.FillDataTable(stationResumeQuery);
                                if (dtResumeMiss.Rows.Count > 0)
                                {
                                    string updateResumeState = @"UPDATE " + Param.DBTable + " SET [IsWarn] = 'true' WHERE ValidTime>='" + checkTime + "' and AutoResume='true'";
                                    SQLHelper.ExecuteNonQuery(updateResumeState);

                                    ResumeStr = TaskName.Substring(0, TaskName.IndexOf("监控"));
                                    foreach (DataRow rowResumeMiss in dtResumeMiss.Rows)
                                    {
                                        ResumeStr += rowResumeMiss["Station"].ToString().Trim() + ",";
                                            //ResumeStr += rowResumeMiss["Station"].ToString().Trim()+"|"+rowResumeMiss["ValidTime"].ToString().Trim();
                                    }
                                    ResumeStr = ResumeStr.Substring(0, ResumeStr.LastIndexOf(',')) + "恢复传输;";
                                    LogHelper.WriteLogAndC(ResumeStr);
                                    LogHelper.TaskWriteLog(ResumeStr, TaskName);
                                        //发短信?
                                }
                                

                                
                                #endregion

                                //发现信息并报警
                                string updateState = @"UPDATE " + Param.DBTable + " SET [Warning] = 'true' WHERE ValidTime<'" + checkTime + "' and IsWarn='true'";
                                int calstation = SQLHelper.ExecuteNonQuery(updateState);
                                
                                if (calstation > 0)
                                {
                                    Dictionary<string, string> TaskwarningStrs = new Dictionary<string, string>();   //任务报警信息合集
                                    string stationMissQuery = @"SELECT [Station],[ValidTime] FROM " + Param.DBTable + " where Warning='true' and IsWarn='true'";
                                    DataTable dtmiss = SQLHelper.FillDataTable(stationMissQuery);
                                    foreach (DataRow rowmiss in dtmiss.Rows)
                                    {
                                        TaskwarningStrs.Add(rowmiss["Station"].ToString().Trim(), Convert.ToDateTime(rowmiss["ValidTime"]).ToString().Trim());
                                    }

                                    string strMiss="";
                                    foreach (KeyValuePair<string, string> kvpsmall in TaskwarningStrs)
                                    {
                                        strMiss += kvpsmall.Key.Trim() + "|" + kvpsmall.Value.Trim();
                                    }
                                    warningStrs.Add(TaskName, TaskwarningStrs);
                                    LogHelper.WriteLogAndC(TaskName + "发现报警信息" + calstation.ToString() + "条:" + strMiss);
                                    LogHelper.TaskWriteLog(TaskName + "报警" + calstation.ToString() + "条:" + strMiss, "报警信息");
                                    //发短信?
                                }
                                else
                                {

                                }
                            }   
                        }
                    }
                }
                #endregion

                
                #region 将DIC的KEY转为一维数组处理
                List<string> warningStrsList = new List<string>();
                //遍历报警信息
                foreach (string onekey in warningStrs.Keys)
                {
                    warningStrsList.Add(onekey);
                    LogHelper.WriteLogAndC("加入了搜索任务:" + onekey);
                }
                

                for (int i = 0; i < warningStrsList.Count; i++)
                {
                    for (int j = i+1; j < warningStrsList.Count; j++)
                    {
                        LogHelper.WriteLogAndC("搜索任务为:" + warningStrsList[j].ToString());
                        string onesub=warningStrsList[i].Substring(0, warningStrsList[i].IndexOf("监控"));
                        string othersub=warningStrsList[j].Substring(0, warningStrsList[j].IndexOf("监控"));

                        if(String.Equals(onesub,othersub,StringComparison.CurrentCultureIgnoreCase))
                        {
                            //遍历某类监控一个任务中的所有报警信息,将Dic转为一维数组进行遍历时删除
                            foreach (string onekey in new List<string>(warningStrs[warningStrsList[i]].Keys)) 
                            {
                                //if (key == 1) ht.Remove(key);
                                LogHelper.WriteLogAndC("开始进入J行遍历，搜索台站为:" + onekey);

                                if (!warningStrs[warningStrsList[j]].ContainsKey(onekey))
                                {
                                    LogHelper.WriteLogAndC(warningStrsList[j] + "不包含报警单站:" + onekey + ",I中删除了单站:" + onekey);
                                    warningStrs[warningStrsList[i]].Remove(onekey);//有别的站不包含（已到报），则删除此站
                                }
                                else
                                {
                                    LogHelper.WriteLogAndC(warningStrsList[j] + "包含报警单站:" + onekey);
                                }
                            }

                            LogHelper.WriteLogAndC("删除了整行任务:" + warningStrsList[j].ToString());
                            warningStrs.Remove(warningStrsList[j]); //warningStrs遍历完后把J行删除
                            warningStrsList.RemoveAt(j);    //warningStrsList遍历完后把J行删除
                            j--;//回到当前行继续遍历
                        }   
                    }
                }
                #endregion

                string warnWord = "";

                warnWord += ResumeStr;

                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in warningStrs)
                {
                    //Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);

                    warnWord += kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "共" + kvp.Value.Count.ToString() + "条报警：" ;
                    foreach (KeyValuePair<string, string> kvpsmall in kvp.Value)
                    {
                        warnWord += kvpsmall.Key.Trim() + "|" + kvpsmall.Value.Trim();
                    }

                    warnWord += ";";  
                }


                if (!String.IsNullOrWhiteSpace(warnWord))
                {
                    LogHelper.WriteLogAndC("本次整合报警信息" + warnWord);
                    LogHelper.TaskWriteLog("本次整合报警信息" + warnWord, "报警信息");
                }
            }
            catch (Exception ex)
            {
                JobExecutionException e2 = new JobExecutionException(ex);
                LogHelper.WriteErrorAndC("全局任务监控异常", ex);
                //1.立即重新执行任务 
                e2.RefireImmediately = true;
                //2 立即停止所有相关这个任务的触发器
                //e2.UnscheduleAllTriggers=true; 
            }
	
            
            

        }

    }
}
