using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.bcf.Data.ValueDefMapAction;
using com.mirle.ibg3k0.bcf.Data.VO;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Data.VO.Interface;
using com.mirle.ibg3k0.sc.ObjectRelay;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc
{
    public partial class ACMD
    {

        public bool IsMoveCommand
        {
            get
            {
                return CMD_TYPE == E_CMD_TYPE.Move ||
                       CMD_TYPE == E_CMD_TYPE.Move_Charger;
            }
        }
        public HCMD ToHCMD()
        {
            return new HCMD()
            {
                ID = this.ID,
                VH_ID = this.VH_ID,
                CARRIER_ID = this.CARRIER_ID,
                CMD_TYPE = this.CMD_TYPE,
                SOURCE = this.SOURCE,
                DESTINATION = this.DESTINATION,
                PRIORITY = this.PRIORITY,
                CMD_START_TIME = this.CMD_START_TIME,
                CMD_END_TIME = this.CMD_END_TIME,
                CMD_PROGRESS = this.CMD_PROGRESS,
                INTERRUPTED_REASON = this.INTERRUPTED_REASON,
                ESTIMATED_TIME = this.ESTIMATED_TIME,
                ESTIMATED_EXCESS_TIME = this.ESTIMATED_EXCESS_TIME,
                TRANSFER_ID = this.TRANSFER_ID,
                CMD_INSER_TIME = this.CMD_INSER_TIME,
                SOURCE_PORT = this.SOURCE_PORT,
                DESTINATION_PORT = this.DESTINATION_PORT,
                CMD_STATUS = this.CMD_STATUS,
                COMPLETE_STATUS = this.COMPLETE_STATUS,
            };
        }
        public override string ToString()
        {
            return $"Command:{this.ID},vh id:{VH_ID},source:{this.SOURCE}({SOURCE_PORT}),desc:{this.DESTINATION}({DESTINATION_PORT}),inser time:{CMD_INSER_TIME.ToString()}";
        }

    }

}
