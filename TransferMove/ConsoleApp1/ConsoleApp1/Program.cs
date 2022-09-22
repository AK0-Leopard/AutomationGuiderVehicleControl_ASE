using Microsoft.Extensions.Configuration;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
    class Program
    {
        static Quartz.IScheduler Scheduler { get; set; }
        public static com.mirle.ibg3k0.sc.Common.RedisCacheManager RedisCacheManager { get; set; }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //RedisCacheManager = new com.mirle.ibg3k0.sc.Common.RedisCacheManager("MoveTransfer");

            SettingApp();
            SetConnectionString();

            initScheduler();
            Console.ReadKey();
        }

        public static void SettingApp()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var config = builder.Build();
            string s_interval_min = config["IntervalTime_min"];
            int.TryParse(s_interval_min, out IntervalTime_min);
            Console.WriteLine($"interval time:{s_interval_min}");
        }
        public static string SourceTableConnectionString = "";
        public static string TargetTableConnectionString = "";
        public static int IntervalTime_min = 10;
        public static void SetConnectionString()
        {
            var builder = new ConfigurationBuilder()
                          .SetBasePath(Directory.GetCurrentDirectory())
                          .AddJsonFile("appsettings.json");
            var config = builder.Build();
            //SourceTableConnectionString = config.GetConnectionString("SourceTableConnectionString");
            TargetTableConnectionString = config.GetConnectionString("TargetTableConnectionString");
            Console.WriteLine($"SourceTableConnectionString:{SourceTableConnectionString}");
            Console.WriteLine($"TargetTableConnectionString:{TargetTableConnectionString}");


        }
        static async void initScheduler()
        {
            StdSchedulerFactory factory = new StdSchedulerFactory();

            Scheduler = await factory.GetScheduler();
            // 建立 Job
            IJobDetail job = JobBuilder.Create<MoveTransfer>()
                .WithIdentity("job1", "group1")
                .Build();
            // 建立 Trigger，每秒跑一次
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("trigger1", "group1")
                .StartNow()
                .WithSimpleSchedule(x => x
                    .WithIntervalInMinutes(IntervalTime_min)
                    .RepeatForever())
                .Build();
            // 加入 ScheduleJob 中
            await Scheduler.ScheduleJob(job, trigger);

            // 啟動
            await Scheduler.Start();
        }

    }
}
