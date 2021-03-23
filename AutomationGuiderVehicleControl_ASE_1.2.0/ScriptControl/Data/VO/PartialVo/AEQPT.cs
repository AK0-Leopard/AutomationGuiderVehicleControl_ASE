using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class AEQPT : BaseEQObject, IAlarmHisList, IVIDCollection
    {
        protected List<APORTSTATION> portStationList;
        public AEQPT()
        {
            eqptObjectCate = SCAppConstants.EQPT_OBJECT_CATE_EQPT;
        }

        #region MCharger
        private int agvcAliveIndex;
        public virtual int AGVCAliveIndex
        {
            get { return agvcAliveIndex; }
            set
            {
                if (agvcAliveIndex != value)
                {
                    agvcAliveIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.AGVCAliveIndex));
                }
            }
        }
        private int abnormalReportIndex;
        public virtual int AbnormalReportIndex
        {
            get { return abnormalReportIndex; }
            set
            {
                if (abnormalReportIndex != value)
                {
                    abnormalReportIndex = value;
                    OnPropertyChanged(BCFUtility.getPropertyName(() => this.AbnormalReportIndex));
                }
            }
        }
        public virtual int abnormalReportCode01 { get; set; }
        public virtual int abnormalReportCode02 { get; set; }
        public virtual int abnormalReportCode03 { get; set; }
        public virtual int abnormalReportCode04 { get; set; }
        public virtual int abnormalReportCode05 { get; set; }
        public virtual int abnormalReportCode06 { get; set; }
        public virtual int abnormalReportCode07 { get; set; }
        public virtual int abnormalReportCode08 { get; set; }
        public virtual int abnormalReportCode09 { get; set; }
        public virtual int abnormalReportCode10 { get; set; }
        public virtual int abnormalReportCode11 { get; set; }
        public virtual int abnormalReportCode12 { get; set; }
        public virtual int abnormalReportCode13 { get; set; }
        public virtual int abnormalReportCode14 { get; set; }
        public virtual int abnormalReportCode15 { get; set; }
        public virtual int abnormalReportCode16 { get; set; }
        public virtual int abnormalReportCode17 { get; set; }
        public virtual int abnormalReportCode18 { get; set; }
        public virtual int abnormalReportCode19 { get; set; }
        public virtual int abnormalReportCode20 { get; set; }
        #endregion MCharger
        public virtual SCAppConstants.EqptType Type { get; set; }

        private AlarmHisList alarmHisList = new AlarmHisList();
        public VIDCollection VID_Collection;

        public List<AUNIT> UnitList;

        public string proc_Formaat = "";
        public string recipe_Parameter_Format = "";

        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
            //對sub eqpt進行初始化
            List<AUNIT> subUnitList = SCApplication.getInstance().getEQObjCacheManager().getUnitListByEquipment(EQPT_ID);
            if (subUnitList != null)
            {
                foreach (AUNIT unit in subUnitList)
                {
                    unit.doShareMemoryInit(runLevel);
                }
            }
            List<APORT> subPortList = SCApplication.getInstance().getEQObjCacheManager().getPortListByEquipment(EQPT_ID);
            if (subPortList != null)
            {
                foreach (APORT port in subPortList)
                {
                    port.doShareMemoryInit(runLevel);
                }
            }
            List<ABUFFER> subBuffList = SCApplication.getInstance().getEQObjCacheManager().getBuffListByEquipment(EQPT_ID);
            if (subBuffList != null)
            {
                foreach (ABUFFER buff in subBuffList)
                {
                    buff.doShareMemoryInit(runLevel);
                }
            }
            //List<APORTSTATION> portStationList = SCApplication.getInstance().getEQObjCacheManager().getPortStationByEquipment(EQPT_ID);
            portStationList = SCApplication.getInstance().getEQObjCacheManager().getPortStationByEquipment(EQPT_ID);
            if (portStationList != null)
            {
                foreach (APORTSTATION portStation in portStationList)
                {
                    portStation.doShareMemoryInit(runLevel);
                }
            }
        }

        public virtual void resetAlarmHis(List<ALARM> AlarmHisList)
        {
            alarmHisList.resetAlarmHis(AlarmHisList);
        }

        public VIDCollection getVIDCollection()
        {
            return VID_Collection;
        }

        public string Process_Data_Format { get; set; }             //A0.11
        private com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler procDataConfigHandler;     //A0.11
        public com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler getProcessDataConfigHandler()
        {
            if (BCFUtility.isEmpty(Process_Data_Format))
            {
                return null;
            }
            if (procDataConfigHandler == null)
            {
                procDataConfigHandler =
                    new com.mirle.ibg3k0.sc.ConfigHandler.ProcessDataConfigHandler(Process_Data_Format);
            }
            return procDataConfigHandler;
        }
        #region PortStation
        public bool EQ_Ready { get; set; }
        public bool EQ_Error { get; set; }
        public bool EQ_Down { get; set; }
        public bool EQ_Disable { get; set; }
        #endregion PortStation
    }

    public class AGVStation : AEQPT, IAGVStationType
    {
        public string AddressID
        {
            get
            {
                if (portStationList == null) return "";
                var position = portStationList.
                       Where(port_station => port_station.PORT_ID.Contains("_ST0")).
                       FirstOrDefault();
                if (position == null) return "";
                return SCUtility.Trim(position.ADR_ID, true);
            }
        }
        public bool IsCheckPortReady { get; set; } = true;
        public string RemoveURI { get { return this.TcpIpAgentName; } }
        public string AGVStationID { get { return this.EQPT_ID; } }
        public string getAGVStationID()
        {
            return this.EQPT_ID;
        }

        private Stopwatch OutOfStockTimer = new Stopwatch();
        const int OUT_OF_STOCK_TIME_OUT_MS = 90000;
        public bool IsOutOfStockTimeOut
        {
            get { return OutOfStockTimer.ElapsedMilliseconds > OUT_OF_STOCK_TIME_OUT_MS; }
        }
        private bool isoutofstock;
        public bool IsOutOfStock
        {
            get { return isoutofstock; }
            set
            {
                if (isoutofstock != value)
                {
                    isoutofstock = value;
                    if (isoutofstock)
                    {
                        OutOfStockTimer.Start();
                    }
                    else
                    {
                        OutOfStockTimer.Reset();
                    }
                }
            }
        }

        public DateTime ReservedSuccessTime { get; set; } = DateTime.MinValue;
        private Stopwatch ReservedStopwatch = new Stopwatch();
        const int RESERVED_TIME_OUT_MS = 90000;
        public bool IsReservedTimeOut
        {
            get { return ReservedStopwatch.ElapsedMilliseconds > RESERVED_TIME_OUT_MS; }
        }
        private bool isreservation;
        public bool IsReservation
        {
            get
            {
                return isreservation;
            }
            set
            {
                if (isreservation != value)
                {
                    isreservation = value;
                    if (isreservation)
                    {
                        ReservedStopwatch.Start();
                        ReservedSuccessTime = DateTime.Now;
                    }
                    else
                    {
                        ReservedStopwatch.Reset();
                    }
                }
            }
        }
        public long syncPoint = 0;
        public bool IsTransferUnloadExcuting { get; set; }
        public bool IsReadyDoubleUnload
        {
            get
            {
                return portStationList != null &&
                       portStationList.
                       Where(port_station => port_station.IsInPutMode && port_station.PortReady).
                       Count() >= 2;
            }
        }
        public bool IsReadySingleUnload
        {
            get
            {
                return portStationList != null &&
                       portStationList.
                       Where(port_station => port_station.IsInPutMode && port_station.PortReady).
                       Count() >= 1;
            }
        }
        public bool HasPortAuto
        {
            get
            {
                return portStationList != null &&
                       portStationList.
                       Where(port_station => port_station.IsAutoMode).
                       Count() >= 1;
            }
        }

        public string BindingVh { get { return proc_Formaat; } }

        public bool IsVirtrueUse { get { return !SCUtility.isEmpty(this.TcpIpAgentName); } }

        public List<APORTSTATION> getAGVStationPorts()
        {
            if (portStationList == null) return null;
            return portStationList.
                   Where(port_station => !port_station.PORT_ID.Contains("_ST0")).
                   OrderByDescending(port_station => port_station.PORT_ID).
                   ToList();
        }
        public List<string> getAGVStationPortAdrIDs()
        {
            if (portStationList == null) return new List<string>();
            return portStationList.
                   Where(port_station => !port_station.PORT_ID.Contains("_ST0")).
                   Select(port_station => SCUtility.Trim(port_station.ADR_ID, true)).
                   ToList();
        }

        public List<APORTSTATION> getAGVStationReadyLoadPorts()
        {
            if (portStationList == null) return null;
            return portStationList.
                   Where(port_station => !port_station.PORT_ID.Contains("_ST0") && port_station.IsInPutMode && port_station.PortReady).
                   OrderByDescending(port_station => port_station.PORT_ID).
                   ToList();
        }
        public APORTSTATION getAGVVirtruePort()
        {
            if (portStationList == null) return null;
            return portStationList.
                   Where(port_station => port_station.PORT_ID.Contains("_ST0")).
                   FirstOrDefault();
        }
        public List<APORTSTATION> loadAutoAGVStationPorts()
        {
            if (portStationList == null) return null;
            return portStationList.
                   Where(port_station => !port_station.PORT_ID.Contains("_ST0") && port_station.IsAutoMode).
                   //Where(port_station => !port_station.PORT_ID.Contains("_ST0")).
                   OrderBy(port_station => port_station.PORT_ID).
                   ToList();
        }

        public List<APORTSTATION> loadReadyAGVStationPort()
        {
            if (portStationList == null)
            { return new List<APORTSTATION>(); }
            return portStationList.
                   Where(port_station => port_station.IsInPutMode && port_station.PortReady).
                   OrderBy(port_station => port_station.PORT_ID).
                   ToList();
        }
        public E_AGVStationTranMode TransferMode { get; set; }

        public E_AGVStationDeliveryMode DeliveryMode { get { return GetDeliveryMode(); } }

        E_AGVStationDeliveryMode GetDeliveryMode()
        {
            var port_station = getAGVVirtruePort();
            if (port_station == null)
            {
                return E_AGVStationDeliveryMode.Normal;
            }
            else
            {
                switch (port_station.ULD_VH_TYPE)
                {

                    case E_VH_TYPE.Swap:
                        return E_AGVStationDeliveryMode.Swap;
                    default:
                        return E_AGVStationDeliveryMode.Normal;
                }
            }
        }

        public void SetLastStartWaitingWaitOutTime()
        {
            LastStartWaitingWaitOutTime = DateTime.Now;
        }

        public void ResetLastStartWaitingWaitOutTime()
        {
            LastStartWaitingWaitOutTime = DateTime.MinValue;
        }

        //        E_AGVStationDeliveryMode GetDeliveryMode()
        //{
        //    if (SCUtility.isEmpty(recipe_Parameter_Format))
        //    {
        //        return E_AGVStationDeliveryMode.Normal;
        //    }
        //    int i_delivery_mode = 0;
        //    if (!int.TryParse(recipe_Parameter_Format, out i_delivery_mode))
        //    {
        //        return E_AGVStationDeliveryMode.Normal;
        //    }
        //    E_AGVStationDeliveryMode delivery_mode = (E_AGVStationDeliveryMode)i_delivery_mode;
        //    if (!Enum.IsDefined(typeof(E_AGVStationDeliveryMode), delivery_mode))
        //    {
        //        return E_AGVStationDeliveryMode.Normal;
        //    }
        //    return delivery_mode;
        //}

        private bool isemergency;
        public bool IsEmergency
        {
            get
            {
                return isemergency;
            }
            set
            {
                if (isemergency != value)
                {
                    isemergency = value;
                }
            }
        }
        public bool ForceEmergency { get; set; }
        public DateTime LastAskTime { get; set; }
        public string sLastAskTime { get { return LastAskTime.ToString(SCAppConstants.DateTimeFormat_19); } }
        public DateTime LastStartWaitingWaitOutTime { get; set; } = DateTime.MinValue;
        private int WATING_CST_WAIT_IN_TIME_SEC = 60;
        public bool isNeedWaitingCSTWaitIn => LastStartWaitingWaitOutTime.AddSeconds(WATING_CST_WAIT_IN_TIME_SEC) > DateTime.Now;
        public bool IsAskedWaitWaitOutCST => LastStartWaitingWaitOutTime != DateTime.MinValue;
        private Stopwatch lastnotifypreOpencovertime = new Stopwatch();
        public Stopwatch LastNotifyPreOpenCoverTime { get { return lastnotifypreOpencovertime; } }

        public void RestartLastNotifyPreOpenCoverTime()
        {
            LastNotifyPreOpenCoverTime.Restart();
        }

    }


    public interface IAGVStationType
    {
        bool IsCheckPortReady { get; set; }
        bool IsVirtrueUse { get; }
        string AddressID { get; }
        string RemoveURI { get; }
        string AGVStationID { get; }
        string getAGVStationID();
        List<APORTSTATION> loadReadyAGVStationPort();
        bool IsReservation { get; set; }
        bool IsTransferUnloadExcuting { get; set; }
        bool IsReadyDoubleUnload { get; }
        bool IsReadySingleUnload { get; }
        bool HasPortAuto { get; }
        APORTSTATION getAGVVirtruePort();
        List<APORTSTATION> getAGVStationPorts();
        List<string> getAGVStationPortAdrIDs();
        List<APORTSTATION> getAGVStationReadyLoadPorts();
        List<APORTSTATION> loadAutoAGVStationPorts();
        string BindingVh { get; }
        DateTime ReservedSuccessTime { get; set; }
        E_AGVStationTranMode TransferMode { get; set; }

        E_AGVStationDeliveryMode DeliveryMode { get; }
        bool IsEmergency { get; set; }
        bool ForceEmergency { get; set; }

        DateTime LastAskTime { get; set; }
        string sLastAskTime { get; }

        void SetLastStartWaitingWaitOutTime();
        void ResetLastStartWaitingWaitOutTime();
        DateTime LastStartWaitingWaitOutTime { get; set; }
        bool isNeedWaitingCSTWaitIn { get; }
        bool IsAskedWaitWaitOutCST { get; }
        Stopwatch LastNotifyPreOpenCoverTime { get; }
        void RestartLastNotifyPreOpenCoverTime();

    }

    public enum E_AGVStationDeliveryMode
    {
        Normal,
        Swap
    }


    public enum E_AGVStationTranMode
    {
        None,
        MoreIn,
        MoreOut
    }


}
