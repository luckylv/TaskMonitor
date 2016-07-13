﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Wlzx.Utility
{
    public class AdminRun
    {
        /// <summary>
        /// 以管理员方式运行程序
        /// </summary>

        public static void Run()
        {
            /**
             * 当前用户是管理员的时候，直接启动应用程序
             * 如果不是管理员，则使用"启动对象"启动程序，以确保使用管理员身份运行
             */
            //获得当前登录的Windows用户标示
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            //判断当前登录用户是否为管理员
            //如果不是管理员，则以管理员方式运行
            if(!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                //创建启动对象
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.UseShellExecute = true;
                startInfo.WorkingDirectory = Environment.CurrentDirectory;
                startInfo.FileName = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                //设置启动动作,确保以管理员身份运行
                startInfo.Verb = "runas";
                try
                {
                    System.Diagnostics.Process.Start(startInfo);
                }
                catch (Exception ex)
                { 
                    throw ex;
                }
                // 退出当前非管理员进程
                System.Environment.Exit(0);
            }
        }
    }
}
