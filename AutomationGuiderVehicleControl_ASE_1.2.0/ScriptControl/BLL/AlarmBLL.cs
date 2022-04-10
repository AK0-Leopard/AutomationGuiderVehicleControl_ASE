//*********************************************************************************
//      AlarmBLL.cs
//*********************************************************************************
// File Name: AlarmBLL.cs
// Description: 業務邏輯：Alarm
//
//(c) Copyright 2014, MIRLE Automation Corporation
//
// Date          Author         Request No.    Tag     Description
// ------------- -------------  -------------  ------  -----------------------------
//**********************************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECS;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.sc.Data;
using Newtonsoft.Json;

namespace com.mirle.ibg3k0.sc.BLL
{
    /// <summary>
    /// Class AlarmBLL.
    /// </summary>
    public class AlarmBLL
    {
        const int CONTROL_REPORT_ALARM_CODE_MIN = 10000;
        const int CONTROL_REPORT_ALARM_CODE_MAX = 20000;
        public static bool IsReportByControl(string alarmCode)
        {
            bool is_success = int.TryParse(alarmCode, out int i_alarm_code);
            if (!is_success)
            {
                return false;
            }

            if (i_alarm_code >= CONTROL_REPORT_ALARM_CODE_MIN &&
                i_alarm_code <= CONTROL_REPORT_ALARM_CODE_MAX)
            {
                return true;
            }
            return false;
        }
        //public const string VEHICLE_ALARM_HAPPEND = "00000";
        public const string VEHICLE_LONG_TIME_INACTION_0 = "10000";
        public const string VEHICLE_CAN_NOT_SERVICE = "10001";
        public const string VEHICLE_BATTERY_LEVEL_IS_LOW = "10002";
        public const string VEHICLE_CAN_NOT_FIND_THE_COUPLER_TO_CHARGING = "10003";
        //public const string SEND_TRAN_CMD_TO_VEHICLE_FAIL_0_1 = "10004";
        public const string VEHICLE_BATTERY_LEVEL_IS_MIDDLE = "10005";

        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE1_ST01 = "10101";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE1_ST02 = "10102";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE2_ST01 = "10103";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE2_ST02 = "10104";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE2_ST03 = "10105";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LOOP_ST01 = "10106";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_STOCK_ST01 = "10107";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST01 = "10108";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST02 = "10109";
        public const string AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST03 = "10110";

        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE1_ST01 = "10111";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE1_ST02 = "10112";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE2_ST01 = "10113";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE2_ST02 = "10114";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE2_ST03 = "10115";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LOOP_ST01 = "10116";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_STOCK_ST01 = "10117";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST01 = "10118";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST02 = "10119";
        public const string AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST03 = "10120";

        public const string AGVC_TRAN_COMMAND_IN_QUEUE_TIME_OUT = "10121";

        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_OHBC_LINE1 = "10131";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_OHBC_LINE2 = "10132";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_OHBC_LINE3 = "10133";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_OHBC_LOOP = "10134";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_STK01 = "10135";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_STK02 = "10136";
        public const string AGVC_WITH_LCS_REDIS_CONNECTION_FAIL_STK03 = "10137";


        public const string AGVC_CHARGER_HP_NOT_SAFETY = "10201";

        /// <summary>
        /// The sc application
        /// </summary>
        private SCApplication scApp = null;
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// The alarm DAO
        /// </summary>
        private AlarmDao alarmDao = null;
        /// <summary>
        /// The line DAO
        /// </summary>
        private LineDao lineDao = null;
        /// <summary>
        /// The alarm RPT cond DAO
        /// </summary>
        private AlarmRptCondDao alarmRptCondDao = null;
        /// <summary>
        /// The alarm map DAO
        /// </summary>
        private AlarmMapDao alarmMapDao = null;
        private MainAlarmDao mainAlarmDao = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="AlarmBLL"/> class.
        /// </summary>
        public AlarmBLL()
        {

        }

        /// <summary>
        /// Starts the specified sc application.
        /// </summary>
        /// <param name="scApp">The sc application.</param>
        public void start(SCApplication scApp)
        {
            this.scApp = scApp;
            alarmDao = scApp.AlarmDao;
            lineDao = scApp.LineDao;
            alarmRptCondDao = scApp.AlarmRptCondDao;
            alarmMapDao = scApp.AlarmMapDao;
            mainAlarmDao = scApp.MainAlarmDao;
        }

        #region Alarm Map
        //public AlarmMap getAlarmMap(string eqpt_real_id, string alarm_id)
        //{
        //    DBConnection conn = null;
        //    AlarmMap alarmMap = null;
        //    try
        //    {
        //        conn = scApp.getDBConnection();
        //        conn.BeginTransaction();

        //        alarmMap = alarmMapDao.getAlarmMap(conn, eqpt_real_id, alarm_id);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Warn("getAlarmMap Exception!", ex);
        //    }
        //    return alarmMap;
        //}
        public List<ALARMRPTCOND> loadAllAlarmRptCond()
        {
            List<ALARMRPTCOND> alarm_rpt_conds = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarm_rpt_conds = alarmRptCondDao.loadAllRptCond(con);
            }
            return alarm_rpt_conds;
        }

        public bool isReportAlarmReport2MCS(string eqID, string alarmID, bool isDeviceTpye = false)
        {
            try
            {
                string device_type = eqID;
                if (!isDeviceTpye)
                {
                    var vh = scApp.VehicleBLL.cache.getVehicle(eqID);
                    if (vh != null)
                    {
                        device_type = vh.NODE_ID;
                    }
                    //else
                    //{
                    //    AUNIT charger = scApp.UnitBLL.OperateCatch.getUnit(eqID);
                    //    if (charger != null)
                    //    {
                    //        string eq_id_temp = charger.EQPT_ID;
                    //        var eq = scApp.EqptBLL.OperateCatch.GetEqpt(eq_id_temp);
                    //        if (eq != null)
                    //        {
                    //            device_type = eq.NODE_ID;
                    //        }
                    //    }
                    //}
                }
                var alarm_report_conds = scApp.getCommObjCacheManager().getAlarmReportConds();
                if (alarm_report_conds == null) return true;
                var alarm_report_cond = alarm_report_conds.Where(cond => SCUtility.isMatche(cond.EQPT_ID, device_type) &&
                                                                        SCUtility.isMatche(cond.ALAM_CODE, alarmID))
                                                          .FirstOrDefault();
                if (alarm_report_cond == null) return true;
                return SCUtility.isMatche(alarm_report_cond.ENABLE_FLG, SCAppConstants.YES_FLAG);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return true;
            }
        }

        public string getAlarmReportCondUserID(string deviceID, string alarmID)
        {
            try
            {
                var alarm_report_conds = scApp.getCommObjCacheManager().getAlarmReportConds();
                if (alarm_report_conds == null) return "";
                var alarm_report_cond = alarm_report_conds.Where(cond => SCUtility.isMatche(cond.EQPT_ID, deviceID) &&
                                                                        SCUtility.isMatche(cond.ALAM_CODE, alarmID))
                                                          .FirstOrDefault();
                if (alarm_report_cond == null) return "";
                return alarm_report_cond.USER_ID;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return "";
            }
        }
        public string getAlarmReportCondReason(string deviceID, string alarmID)
        {
            try
            {
                var alarm_report_conds = scApp.getCommObjCacheManager().getAlarmReportConds();
                if (alarm_report_conds == null) return "";
                var alarm_report_cond = alarm_report_conds.Where(cond => SCUtility.isMatche(cond.EQPT_ID, deviceID) &&
                                                                        SCUtility.isMatche(cond.ALAM_CODE, alarmID))
                                                          .FirstOrDefault();
                if (alarm_report_cond == null) return "";
                return alarm_report_cond.REASON;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return "";
            }
        }
        public string getAlarmReportCondDisableTime(string deviceID, string alarmID)
        {
            try
            {
                var alarm_report_conds = scApp.getCommObjCacheManager().getAlarmReportConds();
                if (alarm_report_conds == null) return "";
                var alarm_report_cond = alarm_report_conds.Where(cond => SCUtility.isMatche(cond.EQPT_ID, deviceID) &&
                                                                        SCUtility.isMatche(cond.ALAM_CODE, alarmID))
                                                          .FirstOrDefault();
                if (alarm_report_cond == null) return "";
                var disable_dateTime = alarm_report_cond.DISABLE_TIME.HasValue ?
                                       alarm_report_cond.DISABLE_TIME.Value.ToString(SCAppConstants.DateTimeFormat_22) :
                                       "";
                return disable_dateTime;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                return "";
            }
        }

        public List<AlarmMap> loadAlarmMaps()
        {
            List<AlarmMap> alarmMaps = alarmMapDao.loadAlarmMaps();
            return alarmMaps;
        }
        #endregion Alarm Map

        public (bool isExist, AlarmMap map) tryGetChargerAlarmMap(string errorCode)
        {
            var alarm_maps = alarmMapDao.loadAlarmMaps();
            var alarm_map = alarm_maps.Where(map => map.EQPT_REAL_ID.Contains("Charger") && SCUtility.isMatche(map.ALARM_ID, errorCode)).FirstOrDefault();
            return (alarm_map != null, alarm_map);
        }

        object lock_obj_alarm = new object();
        public ALARM setAlarmReport(string eq_id, string error_code, string errorDesc)
        {
            return setAlarmReport("", eq_id, error_code, errorDesc, new List<string>());
        }
        //public ALARM setAlarmReport(string nodeID, string eq_id, string error_code, string errorDesc, string cmd_id_1, string cmd_id_2)
        public ALARM setAlarmReport(string nodeID, string eq_id, string error_code, string errorDesc, List<string> effectTranIDs)
        {
            lock (lock_obj_alarm)
            {
                if (IsAlarmExist(eq_id, error_code)) return null;
                //AlarmMap alarmMap = alarmMapDao.getAlarmMap(eq_id, error_code);
                AlarmMap alarmMap = null;
                if (!SCUtility.isEmpty(nodeID))
                    alarmMap = alarmMapDao.getAlarmMap(nodeID, error_code);
                else
                    alarmMap = alarmMapDao.getAlarmMap(eq_id, error_code);

                var effect_tran_ids = tryGetTransferCommandIDs(effectTranIDs);

                string strNow = BCFUtility.formatDateTime(DateTime.Now, SCAppConstants.TimestampFormat_19);
                ALARM alarm = new ALARM()
                {
                    EQPT_ID = eq_id,
                    RPT_DATE_TIME = DateTime.Now,
                    ALAM_CODE = error_code,
                    ALAM_LVL = alarmMap == null ? E_ALARM_LVL.None : alarmMap.ALARM_LVL,
                    ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                    ALAM_DESC = alarmMap == null ? errorDesc : alarmMap.ALARM_DESC,
                    CMD_ID_1 = effect_tran_ids.TranCmdId1,
                    CMD_ID_2 = effect_tran_ids.TranCmdId2,
                    CMD_ID_3 = effect_tran_ids.TranCmdId3,
                    CMD_ID_4 = effect_tran_ids.TranCmdId4
                };
                if (SCUtility.isEmpty(alarm.ALAM_DESC))
                {
                    alarm.ALAM_DESC = $"Unknow error:{error_code}";
                }
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    alarmDao.insertAlarm(con, alarm);
                    CheckSetAlarm();
                }
                return alarm;
            }
        }
        (string TranCmdId1, string TranCmdId2, string TranCmdId3, string TranCmdId4) tryGetTransferCommandIDs(List<string> effectCmdIDs)
        {
            if (effectCmdIDs == null || effectCmdIDs.Count == 0) return ("", "", "", "");
            string tran_cmd_1 = "";
            string tran_cmd_2 = "";
            string tran_cmd_3 = "";
            string tran_cmd_4 = "";
            int effect_cmd_id_count = effectCmdIDs.Count;
            try
            {
                for (int i = 0; i < effect_cmd_id_count; i++)
                {
                    switch (i)
                    {
                        case 0:
                            tran_cmd_1 = effectCmdIDs[i];
                            break;
                        case 1:
                            tran_cmd_2 = effectCmdIDs[i];
                            break;
                        case 2:
                            tran_cmd_3 = effectCmdIDs[i];
                            break;
                        case 3:
                            tran_cmd_4 = effectCmdIDs[i];
                            break;
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            return (tran_cmd_1, tran_cmd_2, tran_cmd_3, tran_cmd_4);
        }


        public void setAlarmReport2Redis(ALARM alarm)
        {
            if (alarm == null) return;
            string hash_field = $"{alarm.EQPT_ID}_{alarm.ALAM_CODE}";
            scApp.getRedisCacheManager().AddTransactionCondition(StackExchange.Redis.Condition.HashNotExists(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field));
            scApp.getRedisCacheManager().HashSetAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field, JsonConvert.SerializeObject(alarm));
        }

        public List<ALARM> getCurrentAlarmsFromRedis()
        {
            List<ALARM> alarms = new List<ALARM>();
            var redis_values_alarms = scApp.getRedisCacheManager().HashValuesProductOnlyAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM).Result;
            foreach (string redis_value_alarm in redis_values_alarms)
            {
                ALARM alarm_obj = (ALARM)JsonConvert.DeserializeObject(redis_value_alarm, typeof(ALARM));
                alarms.Add(alarm_obj);
            }
            return alarms;
        }
        public List<ALARM> getCurrentChargerAlarmsFromRedis()
        {
            List<ALARM> alarms = new List<ALARM>();
            var redis_values_alarms = scApp.getRedisCacheManager().HashValuesProductOnlyAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM).Result;
            foreach (string redis_value_alarm in redis_values_alarms)
            {
                ALARM alarm_obj = (ALARM)JsonConvert.DeserializeObject(redis_value_alarm, typeof(ALARM));
                if (alarm_obj.EQPT_ID.Contains("Charger"))
                    alarms.Add(alarm_obj);
            }
            return alarms;
        }

        public List<ALARM> GetAlarms(DateTime startTime, DateTime endTime)
        {
            List<ALARM> alarm = null;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarm = alarmDao.getAlarms(con, startTime, endTime);
            }
            return alarm;
        }
        public int GetAlarmCount(DateTime startTime, DateTime endTime)
        {
            int count = 0;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                count = alarmDao.getAlarmsCount(con, startTime, endTime);
            }
            return count;
        }

        public List<ALARM> getCurrentAlarms()
        {
            List<ALARM> alarms = new List<ALARM>();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadSetAlarm(con);
            }
            return alarms;
        }
        public List<ALARM> getCurrentErrorAlarms()
        {
            List<ALARM> alarms = new List<ALARM>();
            //using (DBConnection_EF con = new DBConnection_EF())
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                alarms = alarmDao.loadSetErrorAlarm(con);
            }
            return alarms;
        }


        public ALARM resetAlarmReport(string eq_id, string error_code)
        {
            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALARM alarm = alarmDao.getSetAlarm(con, eq_id, error_code);
                    if (alarm != null)
                    {
                        alarm.ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
                        alarm.CLEAR_DATE_TIME = DateTime.Now;
                        alarmDao.updateAlarm(con, alarm);
                        CheckSetAlarm();
                    }
                    return alarm;
                }
            }
        }

        public void resetAlarmReport2Redis(ALARM alarm)
        {
            if (alarm == null) return;
            string hash_field = $"{alarm.EQPT_ID.Trim()}_{alarm.ALAM_CODE.Trim()}";
            //scApp.getRedisCacheManager().AddTransactionCondition(StackExchange.Redis.Condition.HashExists(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field));
            scApp.getRedisCacheManager().HashDeleteAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, hash_field);
        }


        public List<ALARM> resetAllAlarmReport(string eq_id, bool isResetByVh = false)
        {
            List<ALARM> alarms = null;
            lock (lock_obj_alarm)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    alarms = alarmDao.loadSetAlarm(con, eq_id);

                    if (alarms != null)
                    {
                        foreach (ALARM alarm in alarms.ToList())
                        {
                            if (isResetByVh)
                            {
                                if (IsReportByControl(SCUtility.Trim(alarm.ALAM_CODE, true)))
                                {
                                    alarms.Remove(alarm);
                                    continue;
                                }
                            }
                            //if (SCUtility.isMatche(alarm.ALAM_CODE, VEHICLE_ALARM_HAPPEND))
                            //{
                            //    alarms.Remove(alarm);
                            //    continue;
                            //}

                            alarm.ALAM_STAT = ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
                            alarm.CLEAR_DATE_TIME = DateTime.Now;
                            alarmDao.updateAlarm(con, alarm);
                        }
                        //CheckSetAlarm();
                    }
                }
            }
            return alarms;
        }



        public void resetAllAlarmReport2Redis(string vh_id)
        {
            var current_all_alarm = scApp.getRedisCacheManager().HashKeys(SCAppConstants.REDIS_KEY_CURRENT_ALARM);
            var vh_all_alarm = current_all_alarm.Where(redisKey => ((string)redisKey).Contains(vh_id)).ToArray();
            scApp.getRedisCacheManager().HashDeleteAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM, vh_all_alarm);
        }
        private bool IsAlarmExist(string eq_id, string code)
        {
            bool isExist = false;
            using (DBConnection_EF con = DBConnection_EF.GetUContext())
            {
                isExist = alarmDao.getSetAlarmCountByEQAndCode(con, eq_id, code) > 0;
            }
            return isExist;
        }
        public bool IsReportToHost(string code)
        {
            return true;
        }

        public AlarmMap GetAlarmMap(string objID, string errorCode)
        {
            AlarmMap alarmMap = alarmMapDao.getAlarmMap(objID, errorCode);
            return alarmMap;
        }

        public bool hasAlarmExist()
        {
            var redis_values_alarms = scApp.getRedisCacheManager().HashValuesProductOnlyAsync(SCAppConstants.REDIS_KEY_CURRENT_ALARM).Result;
            if (redis_values_alarms.Count() > 0)
            {
                return true;
            }
            return false;
        }



        public bool enableAlarmReport(string eqID, string alarm_id, Boolean isEnable, string userID = "", string reason = "")
        {
            bool isSuccess = true;
            try
            {
                string enable_flag = (isEnable ? SCAppConstants.YES_FLAG : SCAppConstants.NO_FLAG);

                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALARMRPTCOND cond = null;
                    DateTime? disable_time = null;
                    if (isEnable)
                    {
                        disable_time = null;
                    }
                    else
                    {
                        disable_time = DateTime.Now;
                    }
                    cond = alarmRptCondDao.getRptCond(con, eqID, alarm_id);
                    if (cond != null)
                    {
                        cond.ENABLE_FLG = enable_flag;
                        cond.USER_ID = userID;
                        cond.REASON = reason;
                        cond.DISABLE_TIME = disable_time;
                        alarmRptCondDao.updateRptCond(con, cond);
                    }
                    else
                    {
                        cond = new ALARMRPTCOND()
                        {
                            EQPT_ID = eqID,
                            ALAM_CODE = alarm_id,
                            ENABLE_FLG = enable_flag,
                            USER_ID = userID,
                            REASON = reason,
                            DISABLE_TIME = disable_time
                        };
                        alarmRptCondDao.insertRptCond(con, cond);
                    }
                }
                scApp.getCommObjCacheManager().RefreshAlarmReportCond();
            }
            catch (Exception ex)
            {
                isSuccess = false;
                logger.Error(ex, "Exception");
            }
            return isSuccess;
        }

        public List<AlarmMap> loadAlarmMaps(string eqObject)
        {
            List<AlarmMap> alarmMaps = alarmMapDao.loadAlarmMapsByEQRealID(eqObject);
            return alarmMaps;
        }


        public string onMainAlarm(string mAlarmCode, params object[] args)
        {
            MainAlarm mainAlarm = mainAlarmDao.getMainAlarmByCode(mAlarmCode);
            bool isAlarm = false;
            string msg = string.Empty;
            try
            {
                if (mainAlarm != null)
                {
                    isAlarm = mainAlarm.CODE.StartsWith("A");
                    msg = string.Format(mainAlarm.DESCRIPTION, args);
                    if (isAlarm)
                    {
                        msg = string.Format("[{0}]{2}", mainAlarm.CODE, Environment.NewLine, msg);
                        BCFApplication.onErrorMsg(msg);
                    }
                    else
                    {
                        msg = string.Format("[{0}]{2}", mainAlarm.CODE, Environment.NewLine, msg);
                        BCFApplication.onWarningMsg(msg);
                    }
                }
                else
                {
                    logger.Warn(string.Format("LFC alarm/warm happen, but no defin remark code:[{0}] !!!", mAlarmCode));
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            return msg;
        }

        object lock_obj_alarm_happen = new object();
        public void CheckSetAlarm()
        {
            lock (lock_obj_alarm_happen)
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    ALINE line = scApp.getEQObjCacheManager().getLine();
                    List<ALARM> alarmLst = alarmDao.loadSetAlarm(con);

                    if (alarmLst != null && alarmLst.Count > 0)
                    {
                        line.IsAlarmHappened = true;
                    }
                    else
                    {
                        line.IsAlarmHappened = false;
                    }
                }
            }
        }



    }
}
