using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.ObjectRelay
{
    public class TRANSFERObjToShow
    {
        //public static App.SCApplication app = App.SCApplication.getInstance();
        public BLL.PortStationBLL PortStationBLL = null;
        public BLL.EqptBLL EqptBLL = null;
        public BLL.VehicleBLL VehicleBLL = null;
        //public ATRANSFER cmd_mcs = null;
        public VTRANSFER vtrnasfer { get; private set; }

        public TRANSFERObjToShow()
        {
        }
        public TRANSFERObjToShow(BLL.PortStationBLL portStationBLL, BLL.EqptBLL eqptBLL, BLL.VehicleBLL vehicleBLL, VTRANSFER _cmd_mcs)
        {
            sc.Common.SCUtility.TrimAllParameter(_cmd_mcs);
            vtrnasfer = _cmd_mcs;
            PortStationBLL = portStationBLL;
            EqptBLL = eqptBLL;
            VehicleBLL = vehicleBLL;
        }
        public void setVTRANSFER(VTRANSFER _cmd_mcs)
        {
            sc.Common.SCUtility.TrimAllParameter(_cmd_mcs);
            vtrnasfer = _cmd_mcs;
        }

        //public TRANSFERObjToShow(BLL.VehicleBLL vehicleBLL, BLL.PortStationBLL portStationBLL, ATRANSFER _cmd_mcs)
        //{
        //    vtrnasfer = _cmd_mcs;
        //    VehicleBLL = vehicleBLL;
        //    PortStationBLL = portStationBLL;
        //}
        public string CMD_ID { get { return vtrnasfer.ID; } }
        public string CARRIER_ID { get { return vtrnasfer.CARRIER_ID; } }
        public string LOT_ID { get { return vtrnasfer.LOT_ID; } }
        public string VEHICLE_ID
        {
            get
            {
                if (!sc.Common.SCUtility.isEmpty(vtrnasfer.VH_ID))
                {
                    return vtrnasfer.VH_ID;
                }
                return vtrnasfer.ServiceVhID;
            }
        }

        public E_TRAN_STATUS TRANSFERSTATE { get { return vtrnasfer.TRANSFERSTATE; } }
        public string HOSTSOURCE
        {
            get
            {
                var portstation = PortStationBLL.OperateCatch.getPortStation(vtrnasfer.HOSTSOURCE);
                return portstation == null ? sc.Common.SCUtility.Trim(vtrnasfer.HOSTSOURCE, true) : portstation.ToString();
            }
        }
        public string HOSTDESTINATION
        {
            get
            {
                var portstation = PortStationBLL.OperateCatch.getPortStation(vtrnasfer.HOSTDESTINATION);
                return portstation == null ? sc.Common.SCUtility.Trim(vtrnasfer.HOSTDESTINATION, true) : portstation.ToString();
            }
        }

        public string REQUEST_REASON
        {
            get
            {
                try
                {
                    if (TRANSFERSTATE != E_TRAN_STATUS.Queue) return "";
                    bool is_service_vh_error_or_disconnection = checkServiceVhIsErrorOrDisConnection();
                    if (is_service_vh_error_or_disconnection)
                    {
                        return "車子故障或離線中";
                    }
                    var portstation = PortStationBLL.OperateCatch.getPortStation(vtrnasfer.HOSTDESTINATION);
                    if (portstation == null)
                        return "";
                    if (!portstation.IsVirtualAGVStation(EqptBLL))
                        return "";
                    var eq_st_obj = portstation.GetEqpt(EqptBLL) as AGVStation;
                    if (eq_st_obj == null)
                        return "";
                    return sc.Common.SCUtility.Trim(eq_st_obj.RequestReason, true);
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Exception:");
                    return "";
                }
            }
        }

        private bool checkServiceVhIsErrorOrDisConnection()
        {
            if (!sc.Common.SCUtility.isEmpty(VEHICLE_ID))
            {
                var vh = VehicleBLL.cache.getVehicle(VEHICLE_ID);
                if (vh != null)
                {
                    if (vh.IsError || !vh.isTcpIpConnect)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //public int PRIORITY { get { return cmd_mcs.PRIORITY; } }
        public int PRIORITY
        {
            get
            {
                int priority = vtrnasfer.PRIORITY_SUM > 99 ? 99 : vtrnasfer.PRIORITY_SUM;
                return priority;
            }
        }
        public System.DateTime CMD_INSER_TIME { get { return vtrnasfer.CMD_INSER_TIME; } }
        public Nullable<System.DateTime> CMD_START_TIME { get { return vtrnasfer.CMD_START_TIME; } }
        public Nullable<System.DateTime> CMD_FINISH_TIME { get { return vtrnasfer.CMD_FINISH_TIME; } }
        public int REPLACE { get { return vtrnasfer.REPLACE; } }

        public int CMD_PRIORITY { get { return vtrnasfer.PRIORITY; } }
        public int TIME_PRIORITY { get { return vtrnasfer.TIME_PRIORITY; } }
        public int PORT_PRIORITY { get { return vtrnasfer.PORT_PRIORITY; } }
        public int PRIORITY_SUM { get { return vtrnasfer.PRIORITY_SUM; } }
    }
    public class HCMD_MCSObjToShow
    {
        public App.SCApplication app = null;
        public HVTRANSFER hvTran = null;
        public HCMD_MCSObjToShow(App.SCApplication _app, HVTRANSFER _hvTran)
        {
            app = _app;
            hvTran = _hvTran;
        }
        public string ID { get { return hvTran.ID; } }
        public string CARRIER_ID { get { return hvTran.CARRIER_ID; } }
        public string LOT_ID { get { return hvTran.LOT_ID; } }
        public E_TRAN_STATUS TRANSFERSTATE { get { return hvTran.TRANSFERSTATE; } }
        public string HOSTSOURCE
        {
            get
            {
                var portstation = app.PortStationBLL.OperateCatch.getPortStation(hvTran.HOSTSOURCE);
                return portstation == null ? hvTran.HOSTSOURCE : portstation.ToString();
            }
        }
        public string HOSTDESTINATION
        {
            get
            {
                var portstation = app.PortStationBLL.OperateCatch.getPortStation(hvTran.HOSTDESTINATION);
                return portstation == null ? hvTran.HOSTDESTINATION : portstation.ToString();
            }
        }

        //public int PRIORITY { get { return cmd_mcs.PRIORITY; } }
        public int PRIORITY
        {
            get
            {
                int priority = hvTran.PRIORITY_SUM > 99 ? 99 : hvTran.PRIORITY_SUM;
                return priority;
            }
        }
        public string CHECKCODE { get { return hvTran.CHECKCODE; } }
        public string PAUSEFLAG { get { return hvTran.PAUSEFLAG; } }

        public System.DateTime CMD_INSER_TIME { get { return hvTran.CMD_INSER_TIME; } }
        public Nullable<System.DateTime> CMD_START_TIME { get { return hvTran.CMD_START_TIME; } }
        public Nullable<System.DateTime> CMD_FINISH_TIME { get { return hvTran.CMD_FINISH_TIME; } }
        public int TIME_PRIORITY { get { return hvTran.TIME_PRIORITY; } }
        public int PORT_PRIORITY { get { return hvTran.PORT_PRIORITY; } }
        public int REPLACE { get { return hvTran.REPLACE; } }
        public int PRIORITY_SUM { get { return hvTran.PRIORITY_SUM; } }
        public string VH_ID { get { return hvTran.VH_ID; } }

    }
}
