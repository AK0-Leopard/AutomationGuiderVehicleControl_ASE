using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class VTRANSFER
    {
        public static ConcurrentDictionary<string, VTRANSFER> VTransferInfoList { get; private set; } = new ConcurrentDictionary<string, VTRANSFER>();
        public Stopwatch COUNTERDOWN_KEEP_TIME { get; private set; } = new Stopwatch();
        public string ServiceVhID { get; private set; }
        public ACMD ConvertToCmd(BLL.PortStationBLL portStationBLL, BLL.SequenceBLL sequenceBLL, AVEHICLE assignVehicle)
        {
            var source_port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTSOURCE);
            var desc_port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTDESTINATION);
            //如果HostSource port是與車子一樣的話，代表是要去進行Unload的動作
            bool source_is_a_port = portStationBLL.OperateCatch.IsExist(this.HOSTSOURCE);

            string host_source = source_is_a_port ? this.HOSTSOURCE : "";
            E_CMD_TYPE cmd_type = source_is_a_port ? E_CMD_TYPE.LoadUnload : E_CMD_TYPE.Unload; //如果Source不是Port的話，則代表是在車上

            string from_adr = source_port_station == null ? string.Empty : source_port_station.ADR_ID;
            string to_adr = desc_port_station == null ? string.Empty : desc_port_station.ADR_ID;
            return new ACMD()
            {
                ID = sequenceBLL.getCommandID(SCAppConstants.GenOHxCCommandType.Auto),
                TRANSFER_ID = this.ID,
                VH_ID = assignVehicle.VEHICLE_ID,
                CARRIER_ID = this.CARRIER_ID,
                CMD_TYPE = cmd_type,
                SOURCE = from_adr,
                DESTINATION = to_adr,
                PRIORITY = this.PRIORITY_SUM,
                CMD_INSER_TIME = DateTime.Now,
                CMD_STATUS = E_CMD_STATUS.Queue,
                SOURCE_PORT = host_source,
                DESTINATION_PORT = this.HOSTDESTINATION,
                COMPLETE_STATUS = ProtocolFormat.OHTMessage.CompleteStatus.Move
            };
        }

        public string getSourcePortGroupID(BLL.PortStationBLL portStationBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTSOURCE);
            if (port_station == null) return "";
            return SCUtility.Trim(port_station.GROUP_ID, true);
        }
        public string getSourcePortEQID(BLL.PortStationBLL portStationBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTSOURCE);
            if (port_station == null) return "";
            return SCUtility.Trim(port_station.EQPT_ID, true);
        }
        public AEQPT getSourcePortEQ(BLL.EqptBLL eqptBLL)
        {
            var eq = eqptBLL.OperateCatch.GetEqpt(this.HOSTSOURCE);
            if (eq == null) return null;
            return eq;
        }
        public string getSourcePortNodeID(BLL.PortStationBLL portStationBLL, BLL.EqptBLL eqptBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTSOURCE);
            if (port_station == null) return "";
            var eq = port_station.GetEqpt(eqptBLL);
            if (eq == null) return "";
            return SCUtility.Trim(eq.NODE_ID, true);
        }

        public string getTragetPortEQID(BLL.PortStationBLL portStationBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTDESTINATION);
            if (port_station == null) return "";
            return SCUtility.Trim(port_station.EQPT_ID, true);
        }

        public string getSourcePortAdrID(BLL.PortStationBLL portStationBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTSOURCE);
            if (port_station == null) return "";
            return SCUtility.Trim(port_station.ADR_ID, true);
        }
        public string getTragetPortAdrID(BLL.PortStationBLL portStationBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTDESTINATION);
            if (port_station == null) return "";
            return SCUtility.Trim(port_station.ADR_ID, true);
        }

        public AEQPT getTragetPortEQ(BLL.PortStationBLL portStationBLL, BLL.EqptBLL eqptBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTDESTINATION);
            if (port_station == null) return null;
            return port_station.GetEqpt(eqptBLL);
        }
        public string getTragetPortNodeID(BLL.PortStationBLL portStationBLL, BLL.EqptBLL eqptBLL)
        {
            var port_station = portStationBLL.OperateCatch.getPortStation(this.HOSTDESTINATION);
            if (port_station == null) return "";
            var eq = port_station.GetEqpt(eqptBLL);
            if (eq == null) return "";
            return SCUtility.Trim(eq.NODE_ID, true);
        }
        public AEQPT getTragetPortEQ(BLL.EqptBLL eqptBLL)
        {
            var eq = eqptBLL.OperateCatch.GetEqpt(this.HOSTDESTINATION);
            if (eq == null) return null;
            return eq;
        }

        public bool IsTargetPortAGVStation(BLL.EqptBLL eqptBLL)
        {
            var eq = eqptBLL.OperateCatch.GetEqpt(this.HOSTDESTINATION);
            if (eq == null) return false;
            return eq is IAGVStationType;
        }

        public bool IsSourceOnVh(BLL.VehicleBLL vehicleBLL)
        {
            var vh = vehicleBLL.cache.getVehicleByLocationRealID(HOSTSOURCE);
            return vh != null;
        }

        public BLL.CMDBLL.CommandTranDir GetTransferDir()
        {
            return BLL.CMDBLL.GetTransferDir(this);
        }

        public bool IsExcuteTimeOut
        {
            get
            {
                bool is_timeout = (TRANSFERSTATE >= E_TRAN_STATUS.Queue && TRANSFERSTATE <= E_TRAN_STATUS.Canceled) &&
                                    DateTime.Now > CMD_INSER_TIME.AddMilliseconds(SystemParameter.TransferCommandExcuteTimeOut_mSec);
                return is_timeout;
            }
        }

        public override string ToString()
        {
            return $"Command:{this.ID},source:{this.HOSTSOURCE},desc:{this.HOSTDESTINATION},inser time:{CMD_INSER_TIME.ToString()}";
        }

        public void setServiceVh(sc.App.SCApplication scApp)
        {
            try
            {
                string service_vh_id = scApp.GuideBLL.getServiceVhID(HOSTDESTINATION);
                ServiceVhID = service_vh_id;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception:");
            }
        }

        internal void put(VTRANSFER current_cmd)
        {
            ID = current_cmd.ID;
            LOT_ID = current_cmd.LOT_ID;
            CARRIER_ID = current_cmd.CARRIER_ID;
            TRANSFERSTATE = current_cmd.TRANSFERSTATE;
            COMMANDSTATE = current_cmd.COMMANDSTATE;
            HOSTSOURCE = current_cmd.HOSTSOURCE;
            HOSTDESTINATION = current_cmd.HOSTDESTINATION;
            PRIORITY = current_cmd.PRIORITY;
            CHECKCODE = current_cmd.CHECKCODE;
            CMD_INSER_TIME = current_cmd.CMD_INSER_TIME;
            CMD_START_TIME = current_cmd.CMD_START_TIME;
            CMD_FINISH_TIME = current_cmd.CMD_FINISH_TIME;
            TIME_PRIORITY = current_cmd.TIME_PRIORITY;
            PORT_PRIORITY = current_cmd.PORT_PRIORITY;
            REPLACE = current_cmd.REPLACE;
            PRIORITY_SUM = current_cmd.PRIORITY_SUM;
            RESULT_CODE = current_cmd.RESULT_CODE;
            EXCUTE_CMD_ID = current_cmd.EXCUTE_CMD_ID;
            CARRIER_INSER_TIME = current_cmd.CARRIER_INSER_TIME;
            CARRIER_LOCATION = current_cmd.CARRIER_LOCATION;
            CARRIER_INSTALLED_TIME = current_cmd.CARRIER_INSTALLED_TIME;
            CARRIER_READ_STATUS = current_cmd.CARRIER_READ_STATUS;
            VH_ID = current_cmd.VH_ID;
            COMPLETE_STATUS = current_cmd.COMPLETE_STATUS;
            PAUSEFLAG = current_cmd.PAUSEFLAG;
        }
    }

}
