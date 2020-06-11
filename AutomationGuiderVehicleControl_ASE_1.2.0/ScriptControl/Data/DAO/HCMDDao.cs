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


    }

}
