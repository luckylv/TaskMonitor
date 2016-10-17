﻿using Quartz;
using Quartz.Impl;
using Quartz.Spi;
using Quartz.Impl.Matchers;
using Quartz.Impl.Triggers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Wlzx.Utility
{
    /// <summary>
    /// 任务调度处理帮助类
    /// </summary>
    public class QuartzHelper
    {
        private QuartzHelper() { }

        private static object obj = new object();

        /// <summary>
        /// 缓存任务所在程序集信息
        /// </summary>
        private static Dictionary<string, Assembly> AssemblyDict = new Dictionary<string, Assembly>();

        private static IScheduler scheduler = null;

        /// <summary>
        /// 初始化任务调度对象
        /// </summary>
        public static void InitScheduler()
        {
            try
            {
                lock(obj)
                {
                    if(scheduler==null)
                    {
                        NameValueCollection properties = new NameValueCollection();

                        properties["quartz.scheduler.instanceName"] = "ExampleDefaultQuartzScheduler";

                        properties["quartz.threadPool.type"] = "Quartz.Simpl.SimpleThreadPool, Quartz";

                        properties["quartz.threadPool.threadCount"] = "10";

                        properties["quartz.threadPool.threadPriority"] = "Normal";

                        properties["quartz.jobStore.misfireThreshold"] = "60000";

                        properties["quartz.jobStore.type"] = "Quartz.Simpl.RAMJobStore, Quartz";

                        ISchedulerFactory factory = new StdSchedulerFactory(properties);

                        scheduler = factory.GetScheduler();
                        scheduler.Clear();
                        LogHelper.WriteLog("任务调度初始化成功！");
                        Console.WriteLine("任务调度初始化成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("任务调度初始化失败！", ex);
            }
        }

        /// <summary>
        /// 启用任务调度
        /// 启动调度时会把任务表中状态为“执行中”的任务加入到任务调度队列中
        /// </summary>
        public static void StartScheduler()
        {
            try
            {
                if(!scheduler.IsStarted)
                {
                    //添加全局监听
                    scheduler.ListenerManager.AddTriggerListener(new CustomTriggerListener(),GroupMatcher<TriggerKey>.AnyGroup());
                    scheduler.Start();

                    List<TaskUtil> listTask=TaskHelper.ReadConfig().ToList<TaskUtil>();
                    if (listTask!=null&&listTask.Count>0)
                    {
                        foreach (TaskUtil taskUtil in listTask)
                        {
                            try
                            {
                                ScheduleJob(taskUtil);
                            }
                            catch(Exception e)
                            {
                                LogHelper.WriteLog(string.Format("任务“{0}”启动失败！",taskUtil.TaskName),e);
                            }
                        }
                    }
                    LogHelper.WriteLog("任务调度启动成功！");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("任务调度启动失败！",ex);
            }
        }

        /// <summary>
        /// 删除现有任务
        /// </summary>
        /// <param name="JobKey"></param>
        public static void DeleteJob(string JobKey)
        {
            JobKey jk = new JobKey(JobKey);
            if(scheduler.CheckExists(jk))
            {
                //任务已经存在则删除
                scheduler.DeleteJob(jk);
                LogHelper.WriteLog(string.Format("任务“{0}”已经删除", JobKey));
            }
        }

        /// <summary>
        /// 启用任务
        /// <param name="taskUtil">任务信息</param>
        /// <param name="isDeleteOldTask">是否删除原有任务</param>
        /// <returns>返回任务trigger</returns>
        /// </summary>
        public static void ScheduleJob(TaskUtil taskUtil,bool isDeleteOldTask=false)
        {
            if(isDeleteOldTask)
            {
                //按设置删除现有已存在任务
                DeleteJob(taskUtil.TaskID.ToString());
            }

            //验证是否正确的Cron表达式
            if (ValidExpression(taskUtil.CronExpressionString))
            {
                IJobDetail job = new JobDetailImpl(taskUtil.TaskID.ToString(), GetClassInfo(taskUtil.Assembly, taskUtil.Class));
                CronTriggerImpl trigger = new CronTriggerImpl();
                trigger.CronExpressionString = taskUtil.CronExpressionString;
                trigger.Name = taskUtil.TaskID.ToString();
                trigger.Description = taskUtil.TaskName;
                //添加任务执行参数
                job.JobDataMap.Add("TaskParam", taskUtil.TaskParam);
                job.JobDataMap.Add("LastRunTime", taskUtil.LastRunTime);
                scheduler.ScheduleJob(job, trigger);
                if(taskUtil.Status==TaskStatus.STOP)
                {
                    JobKey jk = new JobKey(taskUtil.TaskID.ToString());
                    scheduler.PauseJob(jk);
                }
                else
                {
                    LogHelper.WriteLog(string.Format("任务“{0}”启动成功,未来5次运行时间如下:", taskUtil.TaskName));
                    List<DateTime> list = GetTaskeFireTime(taskUtil.CronExpressionString, 5);
                    foreach (var time in list)
                    {
                        LogHelper.WriteLog(time.ToString());
                    }
                }
            }
            else
            {
                throw new Exception(taskUtil.CronExpressionString + "不是正确的Cron表达式,无法启动该任务!");
            }
        }

        /// <summary>
        /// 暂停任务
        /// </summary>
        /// <param name="JobKey"></param>
        public static void PauseJob(string JobKey)
        {
            JobKey jk = new JobKey(JobKey);
            if (scheduler.CheckExists(jk))
            {
                //任务已经存在则暂停任务
                scheduler.PauseJob(jk);
                LogHelper.WriteLog(string.Format("任务“{0}”已经暂停", JobKey));
            }
        }

        /// <summary>
        /// 恢复运行暂停的任务
        /// </summary>
        /// <param name="JobKey">任务key</param>
        public static void ResumeJob(string JobKey)
        {
            JobKey jk = new JobKey(JobKey);
            if (scheduler.CheckExists(jk))
            {
                //任务已经存在则恢复任务
                scheduler.ResumeJob(jk);
                LogHelper.WriteLog(string.Format("任务“{0}”恢复运行", JobKey));
            }
        }

        /// 获取类的属性、方法  
        /// </summary>  
        /// <param name="assemblyName">程序集</param>  
        /// <param name="className">类名</param>
        private static Type GetClassInfo(string assemblyName,string className)
        {
            try
            {
                assemblyName = FileHelper.GetAbsolutePath(assemblyName + ".dll");
                Assembly assembly = null;
                if (!AssemblyDict.TryGetValue(assemblyName,out assembly))
                {
                    assembly = Assembly.LoadFrom(assemblyName);
                    AssemblyDict[assemblyName] = assembly;
                }
                Type type = assembly.GetType(className, true, true);
                return type;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 停止任务调度
        /// </summary>
        public static void StopSchedule()
        {
            try
            {
                //判断调度是否已经关闭
                if(!scheduler.IsShutdown)
                {
                    scheduler.Shutdown(true);
                    LogHelper.WriteLog("任务调度停止！");
                }
            }
            catch (Exception ex)
            {
                LogHelper.WriteLog("任务调度停止失败！", ex);
            }
        }

        /// <summary>
        /// 校验字符串是否为正确的Cron表达式
        /// </summary>
        /// <param name="cronExpression">带校验表达式</param>
        /// <returns></returns>
        public static bool ValidExpression(string cronExpression)
        {
            return CronExpression.IsValidExpression(cronExpression);
        }

        /// <summary>
        /// 获取任务在未来周期内哪些时间会运行
        /// </summary>
        /// <param name="CronExpressionString">Cron表达式</param>
        /// <param name="numTimes">运行次数</param>
        /// <returns>运行时间段</returns>
        public static List<DateTime> GetTaskeFireTime(string CronExpressionString,int numTimes)
        {
            if(numTimes<0)
            {
                throw new Exception("参数numTimes值大于等于0");
            }

            //时间表达式
            ITrigger trigger = TriggerBuilder.Create().WithCronSchedule(CronExpressionString).Build();
            IList<DateTimeOffset> dates = TriggerUtils.ComputeFireTimes(trigger as IOperableTrigger, null, numTimes);
            List<DateTime> list = new List<DateTime>();
            foreach (DateTimeOffset dtf in dates)
            {
                list.Add(TimeZoneInfo.ConvertTimeFromUtc(dtf.DateTime, TimeZoneInfo.Local));
            }
            return list;
        }
    }
}
