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

        public List<ObjectRelay.HCMD_MCSObjToShow> loadLast24Hours(DBConnection_EF con)
        {
            DateTime query_time = DateTime.Now.AddHours(-24);
            var query = from cmd in con.HTRANSFER
                        where cmd.CMD_INSER_TIME > query_time
                        orderby cmd.CMD_INSER_TIME descending
                        select new ObjectRelay.HCMD_MCSObjToShow() { cmd_mcs = cmd };
            return query.ToList();
        }
        public List<HTRANSFER> loadByInsertTimeEndTime(DBConnection_EF con, DateTime insertTime, DateTime finishTime)
        {
            var query = from cmd in con.HTRANSFER
                        where cmd.CMD_START_TIME > insertTime && (cmd.CMD_FINISH_TIME != null && cmd.CMD_FINISH_TIME < finishTime)
                        orderby cmd.CMD_START_TIME descending
                        select cmd;
            return query.ToList();
        }

    }

}
