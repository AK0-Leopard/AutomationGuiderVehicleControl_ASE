using Quartz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class MoveTransfer : IJob
    {

        TargetModels.HTRANSFER GetTargetLastTran()
        {
            TargetModels.HTRANSFER last_target_tran = null;
            using (var cbx = new TargetModels.TargetTableDbContext())
            {
                //last_target_tran = cbx.HTRANSFERs.OrderByDescending(tran => tran.CMD_INSER_TIME).FirstOrDefault();
                last_target_tran = cbx.HTRANSFERs.OrderByDescending(tran => tran.CMD_FINISH_TIME).FirstOrDefault();
            }
            return last_target_tran;
        }
        List<SourceModels.HTRANSFER> GetHTRANSFERs(DateTime startSearchTime)
        {
            List<SourceModels.HTRANSFER> source_trans = new List<SourceModels.HTRANSFER>();
            using (var cbx = new SourceModels.SourceTableDbContext())
            {
                //source_trans = cbx.HTRANSFERs.Where(tran => tran.CMD_INSER_TIME > startSearchTime).ToList();
                source_trans = cbx.HTRANSFERs.Where(tran => tran.CMD_FINISH_TIME > startSearchTime).ToList();
            }
            return source_trans;
        }
        const string HTRANSFER_LIST = "HTRANSFER_LIST";
        List<SourceModels.HTRANSFER> GetHTRANSFERsByRedis()
        {
            var string_list_htran = Program.RedisCacheManager.ListRange(HTRANSFER_LIST);
            var htrans = string_list_htran.Select(s => SourceModels.HTRANSFER.ToObject(s)).ToList();
            return htrans;
        }
        const string HTRANSFER_TXT_PATH = @"C:\LogFiles\AGVC\RecordHTransfer";
        List<SourceModels.HTRANSFER> GetHTRANSFERsByTxT(DateTime startSeacherTime)
        {
            // Open the file to read from.
            DirectoryInfo di = new DirectoryInfo(HTRANSFER_TXT_PATH);
            var all_record_htran_files = di.GetFiles("*RecordHTransfer*").OrderByDescending(path => path.Name);
            List<SourceModels.HTRANSFER> all_new_htransfer = new List<SourceModels.HTRANSFER>();
            foreach (var paths in all_record_htran_files)
            {
                Console.WriteLine($"開始查詢路徑:{paths.Name}的資料...");
                var readText = File.ReadLines(paths.FullName).ToList();
                int total_row = readText.Count();
                var trans = readText.Select(t => Newtonsoft.Json.JsonConvert.DeserializeObject<SourceModels.HTRANSFER>(t)).ToList();
                var filter_trans = trans.Where(tran => tran.CMD_FINISH_TIME > startSeacherTime).ToList();
                int total_filter_trans_count = filter_trans.Count();

                if (total_filter_trans_count == 0)
                {
                    Console.WriteLine($"查詢路徑:{paths.Name}的資料完成,已無需更新的資料。");
                    break;
                }
                else
                {
                    Console.WriteLine($"查詢路徑:{paths.Name}的資料完成,尚有未查詢完成的資料...。");
                    all_new_htransfer.AddRange(filter_trans);
                }
                System.Threading.SpinWait.SpinUntil(() => false, 500);
            }
            return all_new_htransfer;
        }

        void inserToTargetHTransfer(List<TargetModels.HTRANSFER> trans)
        {
            using (var cbx = new TargetModels.TargetTableDbContext())
            {
                cbx.HTRANSFERs.AddRange(trans);
                cbx.SaveChanges();
            }
        }

        public Task Execute_old(IJobExecutionContext context)
        {
            Console.WriteLine("Start move transfer command...");
            DateTime start_search_time;
            var last_tran = GetTargetLastTran();

            if (last_tran != null && last_tran.CMD_FINISH_TIME.HasValue)
            {
                //start_search_time = last_tran.CMD_INSER_TIME;
                start_search_time = last_tran.CMD_FINISH_TIME.Value;
                Console.WriteLine($"target db最後一筆命令時間:{start_search_time}");
            }
            else
            {
                start_search_time = DateTime.Now.AddMinutes(-5);
                Console.WriteLine($"無找到最後一筆命令，採用前五分鐘前時間:{start_search_time}");
            }
            var source_trans = GetHTRANSFERs(start_search_time);
            Console.WriteLine($"開始搬移命令，數量:{source_trans.Count}");
            if (source_trans == null || source_trans.Count == 0) return Task.CompletedTask;
            var conver_to_target_trans = source_trans.Select(tran => tran.ToTargetTransfer()).ToList();
            inserToTargetHTransfer(conver_to_target_trans);
            Console.WriteLine($"命令搬移完成。");
            return Task.CompletedTask;
        }

        private long SyncPoint = 0;
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Entry...");
            if (System.Threading.Interlocked.Exchange(ref SyncPoint, 1) == 0)
            {
                try
                {
                    Console.WriteLine("Start move transfer command...");
                    DateTime start_search_time = DateTime.MinValue;
                    var last_tran = GetTargetLastTran();

                    if (last_tran != null && last_tran.CMD_FINISH_TIME.HasValue)
                    {
                        //start_search_time = last_tran.CMD_INSER_TIME;
                        start_search_time = last_tran.CMD_FINISH_TIME.Value;
                        Console.WriteLine($"target db最後一筆命令時間:{start_search_time}");
                    }
                    else
                    {
                        Console.WriteLine($"無找到最後一筆命令，採用最小時間開始找:{start_search_time}");
                    }

                    var source_trans = GetHTRANSFERsByTxT(start_search_time);
                    Console.WriteLine($"開始搬移命令，數量:{source_trans.Count}");

                    if (source_trans == null || source_trans.Count == 0) return Task.CompletedTask;
                    var conver_to_target_trans = source_trans.Select(tran => tran.ToTargetTransfer()).ToList();
                    inserToTargetHTransfer(conver_to_target_trans);
                    Console.WriteLine($"命令搬移完成。");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref SyncPoint, 0);
                }
            }
            Console.WriteLine("Leave");
            return Task.CompletedTask;
        }
    }

}