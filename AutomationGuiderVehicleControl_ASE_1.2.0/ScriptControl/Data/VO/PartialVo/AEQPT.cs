﻿using com.mirle.ibg3k0.bcf.App;
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
        #endregion MCharger
        public virtual SCAppConstants.EqptType Type { get; set; }

        private AlarmHisList alarmHisList = new AlarmHisList();
        public VIDCollection VID_Collection;

        public List<AUNIT> UnitList;

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
        public string RemoveURI { get; set; }
        public string getAGVStationID()
        {
            return this.EQPT_ID;
        }
        public bool IsReservation { get; set; }
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
        public List<APORTSTATION> getAGVStationPorts()
        {
            if (portStationList == null) return null;
            return portStationList.
                   Where(port_station => port_station.IsInPutMode && port_station.PortReady).
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
    }


    public interface IAGVStationType
    {
        string RemoveURI { get; set; }
        string getAGVStationID();
        List<APORTSTATION> loadReadyAGVStationPort();
        bool IsReservation { get; set; }
        bool IsTransferUnloadExcuting { get; set; }
        bool IsReadyDoubleUnload { get; }
        List<APORTSTATION> getAGVStationPorts();
    }


}
