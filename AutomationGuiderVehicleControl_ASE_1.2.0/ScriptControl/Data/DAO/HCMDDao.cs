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
    public class HCMDDao
    {
        public void AddByBatch(DBConnection_EF con, List<HCMD> hcmds)
        {
            con.HCMD.AddRange(hcmds);
            con.SaveChanges();
        }


        public void DeleteByBatch(DBConnection_EF con, DateTime deleteBeforeTime)
        {
            string sdelete_before_time = deleteBeforeTime.ToString(SCAppConstants.DateTimeFormat_22);
            string sql = "DELETE [HCMD] WHERE [CMD_INSER_TIME] < {0}";
            con.Database.ExecuteSqlCommand(sql, sdelete_before_time);
        }

    }

}
