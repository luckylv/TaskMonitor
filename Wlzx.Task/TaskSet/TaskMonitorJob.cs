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
                string TopTaskName = context.JobDetail.JobDataMap.Get("TaskName").ToString();
                string sqlstr = @"SELECT [TaskID],[TaskName],[TaskParam],[CronExpressionString],[Assembly],[Class],[Status],[CreatedOn]
                                  ,[ModifyOn],[RecentRunTime],[LastRunTime],[CronRemark],[Remark]
                            FROM [dbo].[p_Task]";
                DataTable dt = SQLHelper.FillDataTable(sqlstr);
                string ResumeStr = "";//报警站恢复字符串
                Dictionary<string, Dictionary<string, string>> warningStrs = new Dictionary<string, Dictionary<string, string>>();   //任务报警信息合集
                Dictionary<string, int> stnums = new Dictionary<string, int>();   //任务报警台站个数


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
                            LogHelper.TaskWriteLog(TaskName + " 任务监控参数TaskParam获取失败,无法监控状态！", TaskName,"error");
                            return;
                        }

                        if (Param != null)   //TaskParam参数解析成功
                        {
                            #region 每日早上9：00恢复所有报警重置IsWarn和AutoResume
                            if (context.PreviousFireTimeUtc != null)
                            {
                                if (context.PreviousFireTimeUtc.Value.LocalDateTime < DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00") && DateTime.Now > DateTime.Parse(DateTime.Now.ToShortDateString() + " 9:00:00"))
                                {
                                    string updateResumeState = @"UPDATE " + Param.DBTable + " SET [IsWarn] = 'true',[AutoResume]='true'";
                                    SQLHelper.ExecuteNonQuery(updateResumeState);
                                    LogHelper.WriteLogAndC(TaskName+"恢复了所有站报警");
                                    LogHelper.TaskWriteLog(TaskName + "恢复了所有站报警", TopTaskName);
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
                                    LogHelper.TaskWriteLog(TaskName + " 任务已6小时未运行，请检查！", TopTaskName);
                                    continue;
                                    //发短信?
                                }
                                else
                                {
                                    LogHelper.WriteLogAndC("等待"+TaskName + " 任务运行后进行监控");
                                    LogHelper.TaskWriteLog("等待" + TaskName + " 任务运行后进行监控", TopTaskName);
                                    continue;
                                }
                            }
                            else
                            {
                                #region 计算每类监控的台站数
                                string stationNumQuery = @"SELECT COUNT(*) FROM " + Param.DBTable ;
                                stnums.Add(TaskName, SQLHelper.ExecuteScalar<int>(stationNumQuery));
                                //LogHelper.WriteLogAndC("任务名："+TaskName+"  查数sql："+stationNumQuery+"个数："+ddd);
                                #endregion

                                #region 对已恢复的数据还原报警开关
                                //判断任务参数中的自动恢复是否为true

                                string stationResumeQuery = @"SELECT [Station],[ValidTime] FROM " + Param.DBTable + " where ValidTime>='" + checkTime + "' and IsWarn='false' and AutoResume='true'";
                                DataTable dtResumeMiss = SQLHelper.FillDataTable(stationResumeQuery);
                                if (dtResumeMiss.Rows.Count > 0)
                                {
                                    string updateResumeState = @"UPDATE " + Param.DBTable + " SET [IsWarn] = 'true' WHERE ValidTime>='" + checkTime + "' and AutoResume='true'";
                                    int rsNum=SQLHelper.ExecuteNonQuery(updateResumeState);

                                    ResumeStr = TaskName.Substring(0, TaskName.IndexOf("监控")) + "共" + rsNum.ToString().Trim()+ "站:";
                                    int l = 0;
                                    foreach (DataRow rowResumeMiss in dtResumeMiss.Rows)
                                    {  
                                        if (l>=3)
                                        {
                                            ResumeStr += "等";    //省略3个意外的站明细
                                            break;
                                        }
                                        else if(l!=0)
                                        {
                                            ResumeStr += ",";
                                        }
                                        ResumeStr += rowResumeMiss["Station"].ToString().Trim();
                                        l++;
                                        //ResumeStr += rowResumeMiss["Station"].ToString().Trim()+"|"+rowResumeMiss["ValidTime"].ToString().Trim();
                                    }
                                    ResumeStr += "恢复传输;";
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
                                        TaskwarningStrs.Add(rowmiss["Station"].ToString().Trim(), Convert.ToDateTime(rowmiss["ValidTime"]).ToString("MM月dd日HH:mm").Trim());
                                    }

                                    string strMiss="";
                                    foreach (KeyValuePair<string, string> kvpsmall in TaskwarningStrs)
                                    {
                                        strMiss += kvpsmall.Key.Trim() + "|" + kvpsmall.Value.Trim();
                                    }
                                    //LogHelper.WriteLogAndC(TaskName + "发现报警信息" + calstation.ToString() + "条:" + strMiss);
                                    LogHelper.TaskWriteLog(TaskName + "发现报警" + calstation.ToString() + "条:" + strMiss, TopTaskName);

                                    warningStrs.Add(TaskName, TaskwarningStrs);
                                }
                                else
                                {
                                    warningStrs.Add(TaskName, new Dictionary<string, string>());  //未检测到缺战，插入空
                                }
                            }   
                        }
                    }
                }
                #endregion

                if (warningStrs.Keys.Count == 0)
                {
                    //LogHelper.WriteLogAndC("等待有任务运行后再进行监控");
                    return;
                }



                #region 将相同类型的报警进行合并
                //将DIC的KEY转为一维数组处理，
                List<string> warningStrsList = new List<string>();
                //遍历报警信息
                foreach (string onekey in warningStrs.Keys)
                {
                    warningStrsList.Add(onekey);
                    //LogHelper.WriteLogAndC("加入了搜索任务:" + onekey);
                }
                
                //将相同类型的监控进行合并
                for (int i = 0; i < warningStrsList.Count; i++)
                {
                    for (int j = i+1; j < warningStrsList.Count; j++)
                    {
                        //LogHelper.WriteLogAndC("搜索任务为:" + warningStrsList[j].ToString());
                        string onesub=warningStrsList[i].Substring(0, warningStrsList[i].IndexOf("监控"));
                        string othersub=warningStrsList[j].Substring(0, warningStrsList[j].IndexOf("监控"));

                        if(String.Equals(onesub,othersub,StringComparison.CurrentCultureIgnoreCase))
                        {
                            //遍历某类监控一个任务中的所有报警信息,将Dic转为一维数组进行遍历时删除
                            foreach (string onekey in new List<string>(warningStrs[warningStrsList[i]].Keys)) 
                            {
                                //if (key == 1) ht.Remove(key);
                                //LogHelper.WriteLogAndC("开始进入J行遍历，搜索台站为:" + onekey);

                                if (!warningStrs[warningStrsList[j]].ContainsKey(onekey))
                                {
                                    //LogHelper.WriteLogAndC(warningStrsList[j] + "不包含报警单站:" + onekey + ",I中删除了单站:" + onekey);
                                    warningStrs[warningStrsList[i]].Remove(onekey);//有别的站不包含（已到报），则删除此站
                                }
                                else
                                {
                                    //LogHelper.WriteLogAndC(warningStrsList[j] + "包含报警单站:" + onekey);
                                }
                            }

                            //LogHelper.WriteLogAndC("删除了整行任务:" + warningStrsList[j].ToString());
                            LogHelper.WriteLogAndC("完成了" + warningStrsList[i].ToString()+"和"+ warningStrsList[j].ToString()+"的合并检查");
                            LogHelper.TaskWriteLog("完成了" + warningStrsList[i].ToString() + "和" + warningStrsList[j].ToString() + "的合并检查", TopTaskName);
                            warningStrs.Remove(warningStrsList[j]); //warningStrs遍历完后把J行删除
                            warningStrsList.RemoveAt(j);    //warningStrsList遍历完后把J行删除
                            j--;//回到当前行继续遍历
                        }   
                    }
                }
                #endregion

                string warnWord = "";
                
                //加入台站恢复信息
                warnWord += ResumeStr;

                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in warningStrs)
                {
                    if (kvp.Value.Count == 0)  //只检查非零的报警
                        continue;

                    //Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                    DateTime oneDayStart = TaskHelper.GetDayStart();
                    string onew = "";
                    onew += kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "共" + kvp.Value.Count.ToString() + "条报警：";
                    #region 具体的报警台站信息
                    int ti = 0;
                    bool useData = false;
                    foreach (KeyValuePair<string, string> kvpsmall in kvp.Value)
                    {
                        string qOne= kvpsmall.Key.Trim() + "|" + kvpsmall.Value.Trim();
                        string qSQL = "SELECT COUNT(*) ct FROM dbo.p_MessageHistory where charindex(@ContentCmpl,Content)>0 and SendOn>@SendOn group by Receiver order by ct desc";
                        int oneWarnNum = SQLHelper.ExecuteScalar<int>(qSQL, new { ContentCmpl = qOne, SendOn = oneDayStart });

                        if (oneWarnNum < SysConfig.SmsMax)//某单站未超过报警阈值，则报警所有站
                            useData = true;

                        onew += qOne;
                        ti++;
                        if (ti >= 3)  //如果报警站数超过3个则省略
                        {
                            onew += "等";
                            break;
                        }
                        else
                        {
                            if (ti != kvp.Value.Count)  //末数不加","
                                onew += ",";
                        }
                    }

                    #endregion

                    #region 检查该类信息已报警次数

                    if (useData)  //有站小于报警次数，则报警
                    {
                        if (SysConfig.PerWarn)   //使用夜间到达率报警
                        {
                            if (TaskHelper.TimeInSpan(DateTime.Now, SysConfig.PerWarnStart, SysConfig.PerWarnEnd)) //在夜间
                            {
                                double PerReach = (1.0 - (double)kvp.Value.Count / (double)stnums[kvp.Key]) * 100;  //计算到达率

                                if (PerReach > SysConfig.PerValue)  //高于阈值
                                {
                                    LogHelper.TaskWriteLog("在夜间，缺报站数" + kvp.Value.Count.ToString() + "，总站数" + stnums[kvp.Key].ToString() + "到达率为" + PerReach.ToString("f2") + "%,高于阈值" + SysConfig.PerValue, TopTaskName);
                                }
                                else
                                {
                                    LogHelper.WriteLogAndC("在夜间，缺报站数" + kvp.Value.Count.ToString() + "，总站数" + stnums[kvp.Key].ToString() + "到达率为" + PerReach.ToString("f2") + "%,低于阈值" + SysConfig.PerValue);
                                    LogHelper.TaskWriteLog("在夜间，缺报站数" + kvp.Value.Count.ToString() + "，总站数" + stnums[kvp.Key].ToString() + "到达率为" + PerReach.ToString("f2") + "%,低于阈值" + SysConfig.PerValue, TopTaskName);
                                    warnWord += kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "报警,到达率为" + PerReach.ToString("f2") + ";";
                                }

                            }
                            else //在白天
                            {
                                warnWord += onew + ";";
                                LogHelper.WriteLogAndC("在白天,添加了报警" + onew);
                                LogHelper.TaskWriteLog("在白天,添加了报警" + onew, TopTaskName);
                            }
                        }
                        else                   //不使用到达率
                        {
                            warnWord += onew + ";";
                            LogHelper.WriteLogAndC("未使用到达率报警,添加了报警" + onew);
                            LogHelper.TaskWriteLog("未使用到达率报警,添加了报警" + onew, TopTaskName);
                        }
                    }
                    else  //是否需要每小时报警
                    {
                        //LogHelper.WriteLogAndC(onew + "在当天已被报警" + warnnum.ToString() + "次");
                        //LogHelper.TaskWriteLog(onew + "在当天已被报警" + warnnum.ToString() + "次", "报警信息");
                    }
                    #endregion
                }

                if (warnWord.EndsWith(";"))   //去除最后一个";"
                    warnWord=warnWord.Remove(warnWord.Length - 1);


                if (!String.IsNullOrWhiteSpace(warnWord))
                {
                    LogHelper.WriteLogAndC("本次发送整合报警：" + warnWord);
                    LogHelper.TaskWriteLog("本次发送整合报警：" + warnWord, TopTaskName);
                    //MessageHelper.AddMessage(SysConfig.SmsTels.Trim(), "本次整合" + warnWord, "报警标题", "监控轮询任务", Guid.Empty);
                    MessageHelper.AddMessage(SysConfig.SmsTels.Trim(), warnWord, "报警标题", "全局任务监控", Guid.Empty);
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
