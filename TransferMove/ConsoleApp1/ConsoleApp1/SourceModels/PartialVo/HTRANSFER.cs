using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp1.SourceModels
{
    public partial class HTRANSFER
    {
        public TargetModels.HTRANSFER ToTargetTransfer()
        {
            return new TargetModels.HTRANSFER()
            {
                ID = this.ID,
                CARRIER_ID = this.CARRIER_ID,
                LOT_ID = this.LOT_ID,
                TRANSFERSTATE = this.TRANSFERSTATE,
                COMMANDSTATE = this.COMMANDSTATE,
                HOSTSOURCE = this.HOSTSOURCE,
                HOSTDESTINATION = this.HOSTDESTINATION,
                PRIORITY = this.PRIORITY,
                CHECKCODE = this.CHECKCODE,
                PAUSEFLAG = this.PAUSEFLAG,
                CMD_INSER_TIME = this.CMD_INSER_TIME,
                CMD_START_TIME = this.CMD_START_TIME,
                CMD_FINISH_TIME = this.CMD_FINISH_TIME,
                TIME_PRIORITY = this.TIME_PRIORITY,
                PORT_PRIORITY = this.PORT_PRIORITY,
                REPLACE = this.REPLACE,
                PRIORITY_SUM = this.PRIORITY_SUM,
                EXCUTE_CMD_ID = this.EXCUTE_CMD_ID,
                RESULT_CODE = this.RESULT_CODE,
                EXE_TIME = getDifTime_Sec(CMD_START_TIME, CMD_FINISH_TIME),
                T_STEMP = DateTime.Now,
                ID_TIME = getID_Time(this.ID)
            };
        }
        static string getID_Time(string ID)
        {
            if (ID.Contains('-'))
            {
                var split = ID.Split('-');
                return split[1].Trim();
            }
            else
            {
                return "";
            }
        }
        static int getDifTime_Sec(DateTime? startTime, DateTime? endTime)
        {
            if (!startTime.HasValue || !endTime.HasValue) return 0;
            return (int)((endTime.Value - startTime.Value).TotalSeconds);
        }
        static public HTRANSFER ToObject(string json)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<HTRANSFER>(json);
        }
    }
}
