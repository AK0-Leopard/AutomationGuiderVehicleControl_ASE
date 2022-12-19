using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.DAO
{
    public class HTransferDao
    {
        public void AddByBatch(DBConnection_EF con, List<HTRANSFER> cmd_ohtcs)
        {
            con.HTRANSFER.AddRange(cmd_ohtcs);
            con.SaveChanges();
        }
        public void DeleteByBatch(DBConnection_EF con, DateTime deleteBeforeTime)
        {
            string sdelete_before_time = deleteBeforeTime.ToString(SCAppConstants.DateTimeFormat_22);
            string sql = "DELETE [HTRANSFER] WHERE [CMD_INSER_TIME] < {0}";
            con.Database.ExecuteSqlCommand(sql, sdelete_before_time);
        }

        public List<ObjectRelay.HCMD_MCSObjToShow> loadLast24Hours(DBConnection_EF con)
        {
            return new List<ObjectRelay.HCMD_MCSObjToShow>();
            //DateTime query_time = DateTime.Now.AddHours(-24);
            //var query = from cmd in con.HTRANSFER
            //            where cmd.CMD_INSER_TIME > query_time
            //            orderby cmd.CMD_INSER_TIME descending
            //            select new ObjectRelay.HCMD_MCSObjToShow() { hvTran = cmd };
            //return query.ToList();
        }
        public List<HVTRANSFER> loadByInsertTimeEndTime(DBConnection_EF con, DateTime startTime, DateTime endTime)
        {
            var query = from cmd in con.HVTRANSFER
                        where cmd.CMD_INSER_TIME >= startTime && cmd.CMD_INSER_TIME <= endTime
                        orderby cmd.CMD_START_TIME descending
                        select cmd;
            return query.ToList();
        }
        public int getByInsertTimeEndTimeCount(DBConnection_EF con, DateTime startTime, DateTime endTime)
        {
            var query = from cmd in con.HVTRANSFER
                        where cmd.CMD_INSER_TIME >= startTime && cmd.CMD_INSER_TIME <= endTime
                        select cmd;
            return query.Count();
        }



    }

}
