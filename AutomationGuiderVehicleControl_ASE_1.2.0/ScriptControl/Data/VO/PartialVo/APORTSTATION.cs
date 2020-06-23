﻿using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.SystemClass.PortInfo;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class APORTSTATION : BaseEQObject
    {
        public string CST_ID { get; set; }
        public string ZONE_ID { get; set; }
        public string EQPT_ID { get; set; }
        public bool IncludeCycleTest { get; set; }
        public int TestTimes { get; set; }

        public override void doShareMemoryInit(BCFAppConstants.RUN_LEVEL runLevel)
        {
            foreach (IValueDefMapAction action in valueDefMapActionDic.Values)
            {
                action.doShareMemoryInit(runLevel);
            }
        }
        public AEQPT GetEqpt(EqptBLL eqptBLL)
        {
            return eqptBLL.OperateCatch.GetEqpt(EQPT_ID);
        }
        public SCAppConstants.EqptType GetEqptType(EqptBLL eqptBLL)
        {
            return eqptBLL.OperateCatch.GetEqptType(EQPT_ID);
        }
        public string GroupID
        {
            get
            {
                string[] temp = PORT_ID.Contains(":") ? PORT_ID.Split(':') : null;
                if (temp != null)
                {
                    return temp[0];
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public override string ToString()
        {
            return $"{PORT_ID} ({ADR_ID})";
        }
        public string PortAdrInfo
        {
            get { return $"{PORT_ID} ({ADR_ID})"; }
        }

    }

    public partial class APORTSTATION
    {
        PORT_INFO PortInfo = new PORT_INFO();
        public DateTime Timestamp
        {
            get
            {
                DateTime dateTime = DateTime.MinValue;
                DateTime.TryParseExact(PortInfo.Timestamp, SCAppConstants.TimestampFormat_17, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateTime);
                return dateTime;
            }
        }
        public bool IsInPutMode { get { return PortInfo.IsInputMode; } }
        public bool IsOutPutMode { get { return PortInfo.IsOutputMode; } }
        public bool PortReady { get { return PortInfo.AGVPortReady; } }
        public bool PortWaitOut { get { return PortInfo.PortWaitOut; } }
        public bool PortWaitIn { get { return PortInfo.PortWaitIn; } }
        public bool IsCSTPresence { get { return PortInfo.IsCSTPresence; } }
        public bool CSTPresenceMismatch { get { return PortInfo.CSTPresenceMismatch; } }
        public string CassetteID { get { return SCUtility.Trim(PortInfo.CassetteID, true); } }
        public void SetPortInfo(PORT_INFO newPortInfo)
        {
            PortInfo.Timestamp = newPortInfo.Timestamp;
            PortInfo.IsInputMode = newPortInfo.IsInputMode;
            PortInfo.IsOutputMode = newPortInfo.IsOutputMode;
            PortInfo.AGVPortReady = newPortInfo.AGVPortReady;
            PortInfo.PortWaitOut = newPortInfo.PortWaitOut;
            PortInfo.PortWaitIn = newPortInfo.PortWaitIn;
            PortInfo.IsCSTPresence = newPortInfo.IsCSTPresence;
            PortInfo.CSTPresenceMismatch = newPortInfo.CSTPresenceMismatch;
            PortInfo.CassetteID = newPortInfo.CassetteID;
        }
        public void ResetPortInfo()
        {
            PortInfo.IsInputMode = false;
            PortInfo.IsOutputMode = false;
            PortInfo.AGVPortReady = false;
            PortInfo.PortWaitOut = false;
            PortInfo.PortWaitIn = false;
            PortInfo.IsCSTPresence = false;
            PortInfo.CassetteID = "";
        }
    }



}