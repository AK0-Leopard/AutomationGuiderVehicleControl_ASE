//------------------------------------------------------------------------------
// <auto-generated>
//    這個程式碼是由範本產生。
//
//    對這個檔案進行手動變更可能導致您的應用程式產生未預期的行為。
//    如果重新產生程式碼，將會覆寫對這個檔案的手動變更。
// </auto-generated>
//------------------------------------------------------------------------------

namespace com.mirle.ibg3k0.sc
{
    using System;
    using System.Collections.Generic;
    
    public partial class ALARM
    {
        public string EQPT_ID { get; set; }
        public System.DateTime RPT_DATE_TIME { get; set; }
        public string ALAM_CODE { get; set; }
        public E_ALARM_LVL ALAM_LVL { get; set; }
        public com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage.ErrorStatus ALAM_STAT { get; set; }
        public string ALAM_DESC { get; set; }
        public Nullable<System.DateTime> CLEAR_DATE_TIME { get; set; }
        public string CMD_ID_1 { get; set; }
        public string CMD_ID_2 { get; set; }
    }
}
