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
            //LogHelper.WriteLogC(DateTime.Now.ToString("MM月dd日HH:mm").Trim());
            
            QuartzHelper.InitScheduler();
            QuartzHelper.StartScheduler();


            //SmsHelper.SendMessage("13958006233", "测试一下");
            //MessageHelper.AddMessage("13958006233", "测试内容3", "测试标题", "主进程调试", Guid.Empty);
            
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
