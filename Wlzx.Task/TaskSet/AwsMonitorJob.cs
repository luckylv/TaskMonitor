using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Wlzx.Utility;
using System.IO;
using System.Text.RegularExpressions;
using Wlzx.Task.Utils;

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
                //object objParam = context.JobDetail.JobDataMap.Get("TaskParam");
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

                                string YDSMSState = DbHelper.ExecuteScalar<string>(SysConfig.SqlConnect, YDqueryStatus, new { smsId = smsId });





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
}
