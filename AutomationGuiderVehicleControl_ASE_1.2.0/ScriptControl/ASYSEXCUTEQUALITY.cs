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
    
    public partial class ASYSEXCUTEQUALITY
    {
        public string CMD_ID_MCS { get; set; }
        public System.DateTime CMD_INSERT_TIME { get; set; }
        public Nullable<System.DateTime> CMD_START_TIME { get; set; }
        public Nullable<System.DateTime> CMD_FINISH_TIME { get; set; }
        public Nullable<E_CMD_STATUS> CMD_FINISH_STATUS { get; set; }
        public string VH_ID { get; set; }
        public string VH_START_SEC_ID { get; set; }
        public string SOURCE_ADR { get; set; }
        public int SEC_CNT_TO_SOURCE { get; set; }
        public int SEC_DIS_TO_SOURCE { get; set; }
        public string DESTINATION_ADR { get; set; }
        public int SEC_CNT_TO_DESTN { get; set; }
        public int SEC_DIS_TO_DESTN { get; set; }
        public double CMDQUEUE_TIME { get; set; }
        public double MOVE_TO_SOURCE_TIME { get; set; }
        public double TOTAL_BLOCK_TIME_TO_SOURCE { get; set; }
        public double TOTAL_OCS_TIME_TO_SOURCE { get; set; }
        public int TOTAL_BLOCK_COUNT_TO_SOURCE { get; set; }
        public int TOTAL_OCS_COUNT_TO_SOURCE { get; set; }
        public double MOVE_TO_DESTN_TIME { get; set; }
        public double TOTAL_BLOCK_TIME_TO_DESTN { get; set; }
        public double TOTAL_OCS_TIME_TO_DESTN { get; set; }
        public int TOTAL_BLOCK_COUNT_TO_DESTN { get; set; }
        public int TOTAL_OCS_COUNT_TO_DESTN { get; set; }
        public double TOTALPAUSE_TIME { get; set; }
        public double CMD_TOTAL_EXCUTION_TIME { get; set; }
        public int TOTAL_ACT_VH_COUNT { get; set; }
        public int PARKING_VH_COUNT { get; set; }
        public int CYCLERUN_VH_COUNT { get; set; }
        public int TOTAL_IDLE_VH_COUNT { get; set; }
    }
}
