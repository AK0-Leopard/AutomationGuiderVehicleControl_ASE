using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.DAO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace com.mirle.ibg3k0.sc.BLL
{
    public class TransferBLL
    {
        public const string TRANSFER_PAUSE_FLAG = "T";

        private static Logger logger = LogManager.GetCurrentClassLogger();
        public DB db = null;
        public Web web = null;
        public Redis redis = null;
        public TransferBLL()
        {
        }

        public void start(SCApplication scApp)
        {
            db = new DB(scApp);
            web = new Web(scApp.webClientManager);
            redis = new Redis(scApp.getRedisCacheManager());
        }

        public class DB
        {
            public Transfer transfer { get; private set; } = null;
            public VTransfer vTransfer { get; private set; } = null;
            public HTransfer hTransfer { get; private set; } = null;
            public DB(SCApplication scApp)
            {
                transfer = new Transfer(scApp);
                vTransfer = new VTransfer(scApp);
                hTransfer = new HTransfer(scApp);
            }
            public class Transfer
            {
                TransferDao tranDao = null;
                public Transfer(SCApplication scApp)
                {
                    tranDao = scApp.TransferDao;
                }
                #region Inster
                public void add(ATRANSFER transfer)
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        tranDao.add(con, transfer);
                    }
                }
                #endregion Inster
                #region update
                public bool updateTranStatus2InitialAndExcuteCmdID(string cmdID, string excuteCmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.TRANSFERSTATE = E_TRAN_STATUS.Initial;
                        cmd.CMD_START_TIME = DateTime.Now;
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_ENROUTE;
                        cmd.EXCUTE_CMD_ID = excuteCmdID;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }

                public bool updateTranStatus2Transferring(string cmdID, bool isWating)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.PAUSEFLAG = isWating ? TRANSFER_PAUSE_FLAG : "";
                        cmd.TRANSFERSTATE = E_TRAN_STATUS.Transferring;
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }


                public bool updateTranStatus2LoadArrivals(string cmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_ARRIVE;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }
                public bool updateTranStatus2Loading(string cmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOADING;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }

                public bool updateTranStatus2UnloadArrive(string cmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOAD_ARRIVE;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }
                public bool updateTranStatus2Unloading(string cmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOADING;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }
                public bool updateTranStatus2UnloadComplete(string cmdID)
                {
                    bool isSuccess = true;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        ATRANSFER cmd = tranDao.getByID(con, cmdID);
                        cmd.COMMANDSTATE = cmd.COMMANDSTATE | ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOAD_COMPLETE;
                        tranDao.update(con, cmd);
                    }
                    return isSuccess;
                }
                #endregion update
                #region Query
                public List<ATRANSFER> loadUnfinishedTransfer()
                {
                    //using (DBConnection_EF con = new DBConnection_EF())
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        return tranDao.loadACMD_MCSIsUnfinished(con);
                    }
                }

                #endregion Query
            }
            public class VTransfer
            {
                VTransferDao vtranDao = null;
                public VTransfer(SCApplication scApp)
                {
                    vtranDao = scApp.VTransferDao;
                }
                public VTRANSFER GetVTransferByTransferID(string transferID)
                {
                    VTRANSFER rtnVTransfer = null;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        rtnVTransfer = vtranDao.getVTransferByTransferID(con, transferID);
                    }
                    return rtnVTransfer;
                }
                public List<VTRANSFER> loadUnfinishedVTransfer()
                {
                    List<VTRANSFER> rtnVTransfers = null;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        rtnVTransfers = vtranDao.loadUnfinishedVTransfer(con);
                    }
                    return rtnVTransfers;

                }
                public (bool isStatusReady, bool isPausing) isTransferStatusReady(string cmdID, int status)
                {
                    VTRANSFER cmd = null;
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {
                        cmd = vtranDao.getVTransferByTransferID(con, cmdID);
                    }
                    if (cmd == null) return (false, false);
                    int check_value = cmd.COMMANDSTATE & status;
                    bool is_status_ready = check_value == status;
                    bool is_pausing = SCUtility.isMatche(cmd.PAUSEFLAG, TRANSFER_PAUSE_FLAG);

                    return (is_status_ready, is_pausing);
                }

                //public bool hasVTransferCommandUnfinished(string zoneID)
                //{
                //    List<VTRANSFER> vtrans = null;
                //    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                //    {
                //        vtrans = vtranDao.loadVTransferIsQueue(con);
                //    }
                //    if (vtrans == null || vtrans.Count == 0)
                //    {
                //        return false;
                //    }
                //    //List<VTRANSFER> vtrans_in_zone = vtrans.Where(tran=>tran);
                //}

            }
            public class HTransfer
            {
                HTransferDao htranDao = null;
                public HTransfer(SCApplication scApp)
                {
                    htranDao = scApp.HTransferDao;
                }
            }
        }

        public class Web
        {
            const string UNLOAD_CHECK_RESULT_OK = "OK";
            const string CHECK_IS_WAIT_RESULT_TRUE = "TRUE";
            const string TRANSFER_RECEIVE_CONST = "0";
            const string TRANSFER_TIME_OUT_CONST = "10";

            WebClientManager webClientManager;
            public Web(WebClientManager webClientManager)
            {
                this.webClientManager = webClientManager;
            }
            public void notifyNoUnloadTransferToAGVStation(IAGVStationType agvStation)
            {
                canExcuteUnloadTransferToAGVStation(agvStation, 0, false);
            }
            public bool canExcuteUnloadTransferToAGVStation(IAGVStationType agvStation, int unfinishCmdCount, bool isEmergency)
            {
                //if (DebugParameter.CanUnloadToAGVStationTest)
                //    return true;
                //else
                //    return false;
                string result = "";
                string url = "";
                try
                {
                    string agv_url = agvStation.RemoveURI;
                    url = SCUtility.Trim(agv_url, true);
                    string agv_station_id = agvStation.getAGVStationID();
                    string[] action_targets = new string[]
                    {
                    "TransferManagement",
                    "TransferCheck",
                    "AGVStation"
                    };
                    string[] param = new string[]
                    {
                    agv_station_id,
                    $"?{nameof(unfinishCmdCount)}={unfinishCmdCount}",
                    $"&{nameof(isEmergency)}={isEmergency}",
                    };
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to reserve agv station to unload,uri:{agv_url} station id:{agv_station_id} unfinishCmdCount:{unfinishCmdCount} isEmergency:{isEmergency}...");
                    result = webClientManager.GetInfoFromServer(agv_url, action_targets, param);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to reserve agv station to unload,uri:{agv_url} station id:{agv_station_id} unfinishCmdCount:{unfinishCmdCount} isEmergency:{isEmergency},result:{result}");
                    result = result.ToUpper();
                    agvStation.LastAskTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Exception:{url}");
                    throw ex;
                }
                //return SCUtility.isMatche(result, UNLOAD_CHECK_RESULT_OK);
                return result.Contains(UNLOAD_CHECK_RESULT_OK);
            }

            public (bool isCan, E_AGVStationTranMode tranMode) checkExcuteUnloadTransferToAGVStationStatus(IAGVStationType agvStation, int unfinishCmdCount, bool isEmergency)
            {
                //if (DebugParameter.CanUnloadToAGVStationTest)
                //    return true;
                //else
                //    return false;
                string result = "";
                string url = "";
                (bool isCan, E_AGVStationTranMode tranMode) parsing_result = (false, E_AGVStationTranMode.None);
                try
                {
                    string agv_url = agvStation.RemoveURI;
                    url = SCUtility.Trim(agv_url, true);
                    string agv_station_id = agvStation.getAGVStationID();
                    string[] action_targets = new string[]
                    {
                    "TransferManagement",
                    "TransferCheck",
                    "Swap",
                    "AGVStation"
                    };
                    string[] param = new string[]
                    {
                    agv_station_id,
                    $"?{nameof(unfinishCmdCount)}={unfinishCmdCount}",
                    $"&{nameof(isEmergency)}={isEmergency}",
                    };
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to reserve agv station to unload,uri:{agv_url} station id:{agv_station_id} unfinishCmdCount:{unfinishCmdCount} isEmergency:{isEmergency}...");
                    result = webClientManager.GetInfoFromServer(agv_url, action_targets, param);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to reserve agv station to unload,uri:{agv_url} station id:{agv_station_id} unfinishCmdCount:{unfinishCmdCount} isEmergency:{isEmergency},result:{result}");
                    result = result.ToUpper();
                    parsing_result = ParsingCheckAGVStationStatusResult(result);
                    agvStation.LastAskTime = DateTime.Now;
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Exception:{url}");
                    throw ex;
                }
                //return SCUtility.isMatche(result, UNLOAD_CHECK_RESULT_OK);



                return (parsing_result.isCan, parsing_result.tranMode);
            }

            private (bool isCan, E_AGVStationTranMode tranMode) ParsingCheckAGVStationStatusResult(string checkResult)
            {
                bool is_can = false;
                E_AGVStationTranMode tran_mode = default(E_AGVStationTranMode);
                if (!checkResult.Contains(","))
                {
                    return (false, E_AGVStationTranMode.None);
                }
                string[] s_result_array = checkResult.Split(',');
                string s_is_can = s_result_array[0];
                string s_tran_mode = s_result_array[1];
                string first_s_tran_mode_num = s_tran_mode.First().ToString();
                int i_tran_mode = 0;
                is_can = s_is_can.Contains(UNLOAD_CHECK_RESULT_OK);
                if (!int.TryParse(first_s_tran_mode_num, out i_tran_mode))
                {
                    return (is_can, E_AGVStationTranMode.MoreOut);
                }
                tran_mode = (E_AGVStationTranMode)i_tran_mode;
                if (!Enum.IsDefined(typeof(E_AGVStationTranMode), tran_mode))
                {
                    return (is_can, E_AGVStationTranMode.MoreOut);
                }
                return (is_can, tran_mode);
            }

            List<string> notify_urls = new List<string>()
            {
                //"http://stk01.asek21.mirle.com.tw:15000",
                 "http://agvc.asek21.mirle.com.tw:15000"
            };
            public void receiveMCSCommandNotify()
            {
                try
                {
                    string[] action_targets = new string[]
                    {
                    "weatherforecast"
                    };
                    string[] param = new string[]
                    {
                    TRANSFER_RECEIVE_CONST,
                    };
                    foreach (string notify_url in notify_urls)
                    {
                        string result = webClientManager.GetInfoFromServer(notify_url, action_targets, param);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
            }
            public void mcsCommandExcuteTimeOutNotify()
            {
                try
                {
                    string[] action_targets = new string[]
                    {
                    "weatherforecast"
                    };
                    string[] param = new string[]
                    {
                    TRANSFER_TIME_OUT_CONST,
                    };
                    foreach (string notify_url in notify_urls)
                    {
                        string result = webClientManager.GetInfoFromServer(notify_url, action_targets, param);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
            }

            public void preOpenAGVStationCover(IAGVStationType agvStation, string portID)
            {
                string result = "";
                string url = "";
                try
                {
                    string agv_url = agvStation.RemoveURI;
                    url = SCUtility.Trim(agv_url, true);
                    string agv_station_id = agvStation.getAGVStationID();
                    string[] action_targets = new string[]
                    {
                    "TransferManagement",
                    "PreOpenAGVStationCover",
                    "AGVStationPorts"
                    };
                    string[] param = new string[]
                    {
                    portID
                    };
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to pre open agv station cover,uri:{agv_url} station id:{agv_station_id} port id:{portID}...");
                    result = webClientManager.GetInfoFromServer(agv_url, action_targets, param);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to pre open agv station cover,uri:{agv_url} station id:{agv_station_id} port id:{portID} ,result:{result}");
                    result = result.ToUpper();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Exception:{url}");
                }
            }
            public bool checkIsNeedWaitForLoad(IAGVStationType agvStation,int waitTimeOut)
            {
                string result = "";
                string url = "";
                try
                {
                    //TransferManagement/TransferCheck/Swap/AGVStation/{agvStationName}
                    string agv_url = agvStation.RemoveURI;
                    url = SCUtility.Trim(agv_url, true);
                    string agv_station_id = agvStation.getAGVStationID();
                    string[] action_targets = new string[]
                    {
                    "TransferManagement",
                    "IsAnyAdditionalCstToBeTransfer",
                    "Swap",
                    "AGVStation"
                    };
                    string[] param = new string[]
                    {
                    agv_station_id
                    };
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to pre open agv station cover,uri:{agv_url} station id:{agv_station_id} ");
                    result = webClientManager.GetInfoFromServer(agv_url, action_targets, param, waitTimeOut);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferBLL), Device: "AGVC",
                       Data: $"Try to pre open agv station cover,uri:{agv_url} station id:{agv_station_id} ,result:{result}");
                    result = result.ToUpper();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, $"Exception:{url}");
                }
                return result.Contains(CHECK_IS_WAIT_RESULT_TRUE);
            }


        }

        public class Redis
        {
            TimeSpan timeOut_1hour = new TimeSpan(1, 0, 0);
            const string HTRANSFER_LIST = "HTRANSFER_LIST";
            RedisCacheManager redis;
            public Redis(RedisCacheManager _redis)
            {
                redis = _redis;
            }

            public void setHTransferInfos(List<ATRANSFER> transfers)
            {
                try
                {
                    var finish_cmd_mcs_list_string = transfers.Select(tran => tran.ToJson());
                    redis.ListRightPush(HTRANSFER_LIST, finish_cmd_mcs_list_string, timeOut_1hour);
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
            }

        }

    }
}
