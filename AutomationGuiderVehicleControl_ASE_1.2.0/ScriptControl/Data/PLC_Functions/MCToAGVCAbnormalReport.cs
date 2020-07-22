using com.mirle.ibg3k0.sc.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.PLC_Functions
{
    class MCToAGVCAbnormalReport : PLC_FunBase
    {
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE1")]
        public UInt16 ErrorCode_1;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE2")]
        public UInt16 ErrorCode_2;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE3")]
        public UInt16 ErrorCode_3;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE4")]
        public UInt16 ErrorCode_4;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE5")]
        public UInt16 ErrorCode_5;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE6")]
        public UInt16 ErrorCode_6;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE7")]
        public UInt16 ErrorCode_7;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE8")]
        public UInt16 ErrorCode_8;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE9")]
        public UInt16 ErrorCode_9;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE10")]
        public UInt16 ErrorCode_10;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE11")]
        public UInt16 ErrorCode_11;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE12")]
        public UInt16 ErrorCode_12;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE13")]
        public UInt16 ErrorCode_13;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE14")]
        public UInt16 ErrorCode_14;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE15")]
        public UInt16 ErrorCode_15;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE16")]
        public UInt16 ErrorCode_16;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE17")]
        public UInt16 ErrorCode_17;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE18")]
        public UInt16 ErrorCode_18;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE19")]
        public UInt16 ErrorCode_19;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_ERRORCODE20")]
        public UInt16 ErrorCode_20;
        [PLCElement(ValueName = "MCHARGER_TO_AGVC_ABNORMAL_CHARGING_REPORT_INDEX")]
        public UInt16 index;

        public List<string> loadCurrentHappendAlarms()
        {
            var alarms = new List<UInt16>();
            if (ErrorCode_1 != 0)
                alarms.Add(ErrorCode_1);
            if (ErrorCode_2 != 0)
                alarms.Add(ErrorCode_2);
            if (ErrorCode_3 != 0)
                alarms.Add(ErrorCode_3);
            if (ErrorCode_4 != 0)
                alarms.Add(ErrorCode_4);
            if (ErrorCode_5 != 0)
                alarms.Add(ErrorCode_5);
            if (ErrorCode_6 != 0)
                alarms.Add(ErrorCode_6);
            if (ErrorCode_7 != 0)
                alarms.Add(ErrorCode_7);
            if (ErrorCode_8 != 0)
                alarms.Add(ErrorCode_8);
            if (ErrorCode_9 != 0)
                alarms.Add(ErrorCode_9);
            if (ErrorCode_10 != 0)
                alarms.Add(ErrorCode_10);
            if (ErrorCode_11 != 0)
                alarms.Add(ErrorCode_11);
            if (ErrorCode_12 != 0)
                alarms.Add(ErrorCode_12);
            if (ErrorCode_13 != 0)
                alarms.Add(ErrorCode_13);
            if (ErrorCode_14 != 0)
                alarms.Add(ErrorCode_14);
            if (ErrorCode_15 != 0)
                alarms.Add(ErrorCode_15);
            if (ErrorCode_16 != 0)
                alarms.Add(ErrorCode_16);
            if (ErrorCode_17 != 0)
                alarms.Add(ErrorCode_17);
            if (ErrorCode_18 != 0)
                alarms.Add(ErrorCode_18);
            if (ErrorCode_19 != 0)
                alarms.Add(ErrorCode_19);
            if (ErrorCode_20 != 0)
                alarms.Add(ErrorCode_20);
            return alarms.Select(error_code => error_code.ToString()).ToList();
        }


    }


}
