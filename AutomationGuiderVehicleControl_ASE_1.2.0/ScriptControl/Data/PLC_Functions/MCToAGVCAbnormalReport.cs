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
    }



}
