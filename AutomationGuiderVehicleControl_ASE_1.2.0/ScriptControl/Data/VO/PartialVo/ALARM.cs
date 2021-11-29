using System;
using System.Collections.Generic;

namespace com.mirle.ibg3k0.sc
{
    public partial class ALARM
    {

        public List<string> RelatedCSTIDs
        {
            get
            {
                List<string> related_cst_ids = new List<string>();
                var try_get_releated_cst_id_by_cmd = getRelatedCSTIDByCmd(CMD_ID_1);
                if (try_get_releated_cst_id_by_cmd.hasRelatedCSTID)
                {
                    related_cst_ids.Add(try_get_releated_cst_id_by_cmd.RelatedCSTID);
                }
                try_get_releated_cst_id_by_cmd = getRelatedCSTIDByCmd(CMD_ID_2);
                if (try_get_releated_cst_id_by_cmd.hasRelatedCSTID)
                {
                    related_cst_ids.Add(try_get_releated_cst_id_by_cmd.RelatedCSTID);
                }
                try_get_releated_cst_id_by_cmd = getRelatedCSTIDByCmd(CMD_ID_3);
                if (try_get_releated_cst_id_by_cmd.hasRelatedCSTID)
                {
                    related_cst_ids.Add(try_get_releated_cst_id_by_cmd.RelatedCSTID);
                }
                try_get_releated_cst_id_by_cmd = getRelatedCSTIDByCmd(CMD_ID_4);
                if (try_get_releated_cst_id_by_cmd.hasRelatedCSTID)
                {
                    related_cst_ids.Add(try_get_releated_cst_id_by_cmd.RelatedCSTID);
                }
                return related_cst_ids;
            }
        }
        private (bool hasRelatedCSTID, string RelatedCSTID) getRelatedCSTIDByCmd(string cmdID)
        {
            try
            {
                if (sc.Common.SCUtility.isEmpty(cmdID)) return (false, "");
                if (!cmdID.Contains("-")) return (false, "");
                return (true, cmdID.Split('-')[0]);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception:");
                return (false, "");
            }
        }
    }

}
