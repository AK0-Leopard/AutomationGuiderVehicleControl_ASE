using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace com.mirle.ibg3k0.sc.Data.DAO
{
    public class VTransferDao
    {
        public List<VTRANSFER> loadVTransferIsUnfinished(DBConnection_EF con)
        {
            var query = from cmd in con.VTRANSFER.AsNoTracking()
                        where cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Aborting
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        select cmd;

            return query.ToList();
        }

        public List<VTRANSFER> loadUnfinishedVTransfer(DBConnection_EF con)
        {
            var query = from cmd in con.VTRANSFER.AsNoTracking()
                        where cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && cmd.TRANSFERSTATE <= E_TRAN_STATUS.Aborting
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        orderby cmd.PRIORITY_SUM descending, cmd.CMD_INSER_TIME
                        select cmd;
            return query.ToList();
        }

        public List<VTRANSFER> loadVTransferIsQueue(DBConnection_EF con)
        {
            var query = from cmd in con.VTRANSFER.AsNoTracking()
                        where cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue
                        && cmd.CHECKCODE.Trim() == SECSConst.HCACK_Confirm
                        select cmd;

            return query.ToList();
        }

        public List<VTRANSFER> loadAllVTransfer(DBConnection_EF con)
        {
            var query = from cmd in con.VTRANSFER.AsNoTracking()
                        select cmd;
            return query.ToList();
        }

        //public IQueryable getQueryAllSQL(DBConnection_EF con)
        //{
        //    var query = from vacmd_mcs in con.VACMD_MCS
        //                select vacmd_mcs;
        //    return query;
        //}

        public VTRANSFER getVTransferByTransferID(DBConnection_EF con, String cmd_id)
        {
            var query = from cmd in con.VTRANSFER
                        where cmd.ID.Trim() == cmd_id.Trim()
                        select cmd;
            return query.SingleOrDefault();
        }

    }
}