using Nancy.Hosting.Self;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using Wlzx.Utility;

namespace Wlzx.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            AdminRun.Run();
            ConfigInit.InitConfig();
            QuartzHelper.InitScheduler();
            QuartzHelper.StartScheduler();


            //SmsHelper.SendMessage("13958006233", "测试一下");
            //MessageHelper.AddMessage("13958006233", "测试内容3", "测试标题", "主进程调试", Guid.Empty);

            //Dictionary<string, Dictionary<string, string>> warningStrs = new Dictionary<string, Dictionary<string, string>>();

            //if (warningStrs.Keys.Count==0)
            //    LogHelper.WriteLogC("warningStrs为null");

            

            //if (SysConfig.PerWarn)   //使用夜间到达率报警
            //{
            //    if (TaskHelper.TimeInSpan(DateTime.Now.AddHours(6), SysConfig.PerWarnStart, SysConfig.PerWarnEnd)) //在夜间
            //    {
            //        double PerReach = (1.0 - (double)1 / (double)108)*100 ;  //计算到达率
                    

            //        if (PerReach > SysConfig.PerValue)  //高于阈值
            //        {
            //            LogHelper.WriteLogC("在晚上，高于阈值");
            //        }
            //        else
            //        {
            //            LogHelper.WriteLogC("在晚上，低于阈值");
            //            //warnWord += kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "报警,到达率为" + PerReach.ToString("f2");
            //            //LogHelper.WriteLogAndC(kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "报警,到达率为" + PerReach.ToString("f2"));
            //            //LogHelper.TaskWriteLog(kvp.Key.Substring(0, kvp.Key.IndexOf("监控")) + "报警,到达率为" + PerReach.ToString("f2"), "报警信息");
            //        }

            //    }
            //    else //在白天
            //    {
            //        LogHelper.WriteLogC("在白天");
                    
            //        //warnWord += onew + ";";
            //        //LogHelper.WriteLogAndC("添加了报警" + onew + "，当前已报" + warnnum + "次，最大" + SysConfig.SmsMax);
            //        //LogHelper.TaskWriteLog("添加了报警" + onew + "，当前已报" + warnnum + "次，最大" + SysConfig.SmsMax, "报警信息");
            //    }
            //}


            
            try
            {
                string url = string.Format("http://127.0.0.1:{0}", SysConfig.WebPort);
                //启动站点
                //using(NancyHost host=Startup)
                LogHelper.WriteLogAndC("程序已启动,按任意键退出");
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
