using System;
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

            //SmsHelper.SendMessage("13958006233", "测试一下");
            QuartzHelper.InitScheduler();
            QuartzHelper.StartScheduler();
            
            try
            {
                //string url = string.Format("http://127.0.0.1:{0}", SysConfig.WebPort);
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
