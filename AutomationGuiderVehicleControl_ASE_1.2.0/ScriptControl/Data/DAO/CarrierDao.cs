using com.mirle.ibg3k0.sc.Data.SECS;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;

namespace com.mirle.ibg3k0.sc.Data.DAO
{
    public class CarrierDao
    {
        #region Inser
        public void add(DBConnection_EF con, ACARRIER carrier)
        {
            con.ACARRIER.Add(carrier);
            con.SaveChanges();
        }
        #endregion Inser
        #region Query
        public ACARRIER getByID(DBConnection_EF con, String carrierID)
        {
            var query = from db_obj in con.ACARRIER
                        where db_obj.ID == carrierID.Trim()
                        select db_obj;
            return query.FirstOrDefault();
        }
        public List<ACARRIER> loadCurrentInLineCarrier(DBConnection_EF con)
        {
            var query = from db_obj in con.ACARRIER
                        where db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.WaitIn ||
                              db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.Installed
                        select db_obj;
            return query.ToList();
        }
        public ACARRIER getInLineCarrierByLocationID(DBConnection_EF con, string location)
        {
            var query = from db_obj in con.ACARRIER
                        where (db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.WaitIn ||
                              db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.Installed) &&
                              db_obj.LOCATION == location
                        select db_obj;
            return query.FirstOrDefault();
        }
        public ACARRIER getInLineCarrierByCarrierID(DBConnection_EF con, string carrierID)
        {
            var query = from db_obj in con.ACARRIER
                        where (db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.WaitIn ||
                              db_obj.STATE == ProtocolFormat.OHTMessage.E_CARRIER_STATE.Installed) &&
                              db_obj.ID == carrierID
                        select db_obj;
            return query.FirstOrDefault();
        }
        #endregion Query
        #region update
        public void update(DBConnection_EF con, ACARRIER carrier)
        {
            con.SaveChanges();
        }
        #endregion update
        #region Delete
        public void delete(DBConnection_EF con, ACARRIER carrier)
        {
            con.ACARRIER.Remove(carrier);
            con.SaveChanges();
        }
        #endregion Delete

    }
}