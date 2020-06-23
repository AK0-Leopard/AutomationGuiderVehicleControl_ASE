﻿using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.PLC_Functions;
using com.mirle.ibg3k0.sc.Data.VO;
using com.mirle.ibg3k0.sc.Module;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using com.mirle.ibg3k0.sc.RouteKit;
using Google.Protobuf.Collections;
using KingAOP;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using static com.mirle.ibg3k0.sc.App.SCAppConstants;

namespace com.mirle.ibg3k0.sc.Service
{
    public class DeadLockEventArgs : EventArgs
    {
        public AVEHICLE Vehicle1;
        public AVEHICLE Vehicle2;
        public DeadLockEventArgs(AVEHICLE vehicle1, AVEHICLE vehicle2)
        {
            Vehicle1 = vehicle1;
            Vehicle2 = vehicle2;
        }
    }

    public class VehicleService : IDynamicMetaObjectProvider
    {
        static Logger logger = LogManager.GetCurrentClassLogger();
        static SCApplication scApp = null;
        public const string DEVICE_NAME_AGV = "AGV";
        public SendProcessor Send { get; private set; }
        public ReceiveProcessor Receive { get; private set; }
        public CommandProcessor Command { get; private set; }
        public AvoidProcessor Avoid { get; private set; }
        public class SendProcessor
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            CMDBLL cmdBLL = null;
            VehicleBLL vehicleBLL = null;
            ReportBLL reportBLL = null;
            TransferBLL transferBLL = null;
            GuideBLL guideBLL = null;
            public SendProcessor(SCApplication scApp)
            {
                cmdBLL = scApp.CMDBLL;
                vehicleBLL = scApp.VehicleBLL;
                reportBLL = scApp.ReportBLL;
                guideBLL = scApp.GuideBLL;
                transferBLL = scApp.TransferBLL;
            }
            //todo kevin 要重新整理SendMessage_ID_31的功能
            #region ID_31 TransferCommand 
            public bool Command(AVEHICLE assignVH, ACMD cmd)
            {
                bool isSuccess = ProcSendTransferCommandToVh(assignVH, cmd);
                if (isSuccess)
                {
                    Task.Run(() => vehicleBLL.web.commandSendCompleteNotify(assignVH.VEHICLE_ID));
                }
                return isSuccess;
            }
            public bool CommandHome(string vhID, string cmdID)
            {
                return sendMessage_ID_31_TRANS_REQUEST(vhID, cmdID, CommandActionType.Home, "",
                                                fromAdr: "", destAdr: "",
                                                loadPort: "", unloadPort: "");
            }
            private bool ProcSendTransferCommandToVh(AVEHICLE assignVH, ACMD cmd)
            {
                SCUtility.TrimAllParameter(cmd);
                bool isSuccess = true;
                string vh_id = assignVH.VEHICLE_ID;
                CommandActionType active_type = cmdBLL.convertECmdType2ActiveType(cmd.CMD_TYPE);
                bool isTransferCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                try
                {
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (var tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            isSuccess &= cmdBLL.updateCommand_OHTC_StatusByCmdID(vh_id, cmd.ID, E_CMD_STATUS.Execution);
                            //isSuccess &= vehicleBLL.updateVehicleExcuteCMD(cmd.VH_ID, cmd.ID, cmd.TRANSFER_ID);
                            if (isTransferCmd)
                            {
                                isSuccess &= transferBLL.db.transfer.updateTranStatus2InitialAndExcuteCmdID(cmd.TRANSFER_ID, cmd.ID);
                                isSuccess &= reportBLL.newReportBeginTransfer(cmd.TRANSFER_ID, reportqueues);
                                reportBLL.insertMCSReport(reportqueues);
                            }

                            if (isSuccess)
                            {
                                isSuccess &= sendMessage_ID_31_TRANS_REQUEST
                                    (cmd.VH_ID, cmd.ID, active_type, cmd.CARRIER_ID,
                                     cmd.SOURCE, cmd.DESTINATION,
                                     cmd.SOURCE_PORT, cmd.DESTINATION_PORT);
                            }
                            if (isSuccess)
                            {
                                tx.Complete();
                            }
                        }
                    }
                    if (isSuccess)
                    {
                        reportBLL.newSendMCSMessage(reportqueues);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exection:");
                    isSuccess = false;
                }
                return isSuccess;
            }
            private bool sendMessage_ID_31_TRANS_REQUEST(string vhID, string cmd_id, CommandActionType activeType, string cst_id,
                                                         string fromAdr, string destAdr,
                                                         string loadPort, string unloadPort)
            {
                //TODO 要在加入Transfer Command的確認 scApp.CMDBLL.TransferCommandCheck(activeType,) 
                bool isSuccess = true;
                string reason = string.Empty;
                ID_131_TRANS_RESPONSE receive_gpp = null;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);
                if (isSuccess)
                {
                    ID_31_TRANS_REQUEST send_gpp = new ID_31_TRANS_REQUEST()
                    {
                        CmdID = cmd_id,
                        CommandAction = activeType,
                        CSTID = cst_id ?? string.Empty,
                        LoadAdr = fromAdr ?? string.Empty,
                        DestinationAdr = destAdr ?? string.Empty,
                        LoadPortID = loadPort ?? string.Empty,
                        UnloadPortID = unloadPort ?? string.Empty
                    };

                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                    isSuccess = vh.send_Str31(send_gpp, out receive_gpp, out reason);
                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
                }
                if (isSuccess)
                {
                    int reply_code = receive_gpp.ReplyCode;
                    if (reply_code != 0)
                    {
                        isSuccess = false;
                        bcf.App.BCFApplication.onWarningMsg(string.Format("發送命令失敗,VH ID:{0}, CMD ID:{1}, Reason:{2}",
                                                                  vhID,
                                                                  cmd_id,
                                                                  reason));
                    }
                    //vh.NotifyVhExcuteCMDStatusChange();
                    vh.onExcuteCommandStatusChange();
                }
                else
                {
                    bcf.App.BCFApplication.onWarningMsg(string.Format("發送命令失敗,VH ID:{0}, CMD ID:{1}, Reason:{2}",
                                              vhID,
                                              cmd_id,
                                              reason));
                    StatusRequest(vhID, true);
                }
                return isSuccess;
            }
            #endregion ID_31 TransferCommand
            #region ID_35 Carrier Rename
            public bool CarrierIDRename(string vh_id, string newCarrierID, string oldCarrierID)
            {
                bool isSuccess = true;
                AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
                ID_135_CST_ID_RENAME_RESPONSE receive_gpp;
                ID_35_CST_ID_RENAME_REQUEST send_gpp = new ID_35_CST_ID_RENAME_REQUEST()
                {
                    OLDCSTID = oldCarrierID ?? string.Empty,
                    NEWCSTID = newCarrierID ?? string.Empty,
                };
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                isSuccess = vh.send_Str35(send_gpp, out receive_gpp);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_35 Carrier Rename
            #region ID_37 Cancel
            public bool Cancel(string vhID, string cmd_id, CancelActionType actType)
            {
                var vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                bool isSuccess = false;
                ID_37_TRANS_CANCEL_REQUEST stSend;
                ID_137_TRANS_CANCEL_RESPONSE stRecv;
                stSend = new ID_37_TRANS_CANCEL_REQUEST()
                {
                    CmdID = cmd_id,
                    CancelAction = actType
                };
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, stSend);
                isSuccess = vh.send_Str37(stSend, out stRecv);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, stRecv, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_37 Cancel
            #region ID_41 ModeChange
            public bool ModeChange(string vh_id, OperatingVHMode mode)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vh_id);
                ID_141_MODE_CHANGE_RESPONSE receive_gpp;
                ID_41_MODE_CHANGE_REQ sned_gpp = new ID_41_MODE_CHANGE_REQ()
                {
                    OperatingVHMode = mode
                };
                SCUtility.RecodeReportInfo(vh_id, 0, sned_gpp);
                isSuccess = vh.send_S41(sned_gpp, out receive_gpp);
                SCUtility.RecodeReportInfo(vh_id, 0, receive_gpp, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_41 ModeChange
            #region ID_43 StatusRequest
            public bool StatusRequest(string vhID, bool isSync = false)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);
                ID_143_STATUS_RESPONSE statusResponse;
                (isSuccess, statusResponse) = sendMessage_ID_43_STATUS_REQUEST(vhID);
                if (isSync && isSuccess)
                {
                    isSuccess = PorcessSendStatusRequestResponse(isSuccess, vh, statusResponse);
                }
                return isSuccess;
            }
            private bool PorcessSendStatusRequestResponse(bool isSuccess, AVEHICLE vh, ID_143_STATUS_RESPONSE statusReqponse)
            {
                scApp.VehicleBLL.setAndPublishPositionReportInfo2Redis(vh.VEHICLE_ID, statusReqponse);
                uint batteryCapacity = statusReqponse.BatteryCapacity;
                VHModeStatus modeStat = scApp.VehicleBLL.DecideVhModeStatus(vh.VEHICLE_ID, statusReqponse.ModeStatus, batteryCapacity);
                VHActionStatus actionStat = statusReqponse.ActionStatus;
                VhPowerStatus powerStat = statusReqponse.PowerStatus;
                string cmd_id_1 = statusReqponse.CmdId1;
                string cmd_id_2 = statusReqponse.CmdId2;
                string cst_id_l = statusReqponse.CstIdL;
                string cst_id_r = statusReqponse.CstIdR;
                VhChargeStatus chargeStatus = statusReqponse.ChargeStatus;
                VhStopSingle reserveStatus = statusReqponse.ReserveStatus;
                VhStopSingle obstacleStat = statusReqponse.ObstacleStatus;
                VhStopSingle blockingStat = statusReqponse.BlockingStatus;
                VhStopSingle pauseStat = statusReqponse.PauseStatus;
                VhStopSingle errorStat = statusReqponse.ErrorStatus;
                VhLoadCSTStatus load_cst_status_l = statusReqponse.HasCstL;
                VhLoadCSTStatus load_cst_status_r = statusReqponse.HasCstR;
                bool has_cst_l = load_cst_status_l == VhLoadCSTStatus.Exist;
                bool has_cst_r = load_cst_status_r == VhLoadCSTStatus.Exist;
                string[] will_pass_section_id = statusReqponse.WillPassGuideSection.ToArray();
                int obstacleDIST = statusReqponse.ObstDistance;
                string obstacleVhID = statusReqponse.ObstVehicleID;
                int steeringWheel = statusReqponse.SteeringWheel;
                bool hasdifferent = vh.BATTERYCAPACITY != batteryCapacity ||
                                    vh.MODE_STATUS != modeStat ||
                                    vh.ACT_STATUS != actionStat ||
                                    SCUtility.isMatche(vh.CMD_ID_1, cmd_id_1) ||
                                    SCUtility.isMatche(vh.CMD_ID_2, cmd_id_2) ||
                                    SCUtility.isMatche(vh.CST_ID_L, cst_id_l) ||
                                    SCUtility.isMatche(vh.CST_ID_R, cst_id_r) ||
                                    vh.ChargeStatus != chargeStatus ||
                                    vh.RESERVE_PAUSE != reserveStatus ||
                                    vh.OBS_PAUSE != obstacleStat ||
                                    vh.BLOCK_PAUSE != blockingStat ||
                                    vh.CMD_PAUSE != pauseStat ||
                                    vh.ERROR != errorStat ||
                                    vh.HAS_CST_L != has_cst_l ||
                                    vh.HAS_CST_R != has_cst_r ||
                                    !SCUtility.isMatche(vh.PredictSections, will_pass_section_id)
                                    ;

                if (hasdifferent)
                {
                    scApp.VehicleBLL.cache.updateVehicleStatus(scApp.CMDBLL, vh.VEHICLE_ID,
                                                         cst_id_l, cst_id_r, modeStat, actionStat, chargeStatus,
                                                         blockingStat, pauseStat, obstacleStat, VhStopSingle.Off, errorStat, reserveStatus,
                                                         has_cst_l, has_cst_r,
                                                         cmd_id_1, cmd_id_2,
                                                         batteryCapacity, will_pass_section_id);
                }

                if (modeStat != vh.MODE_STATUS)
                {
                    vh.onModeStatusChange(modeStat);
                }
                if (errorStat != vh.ERROR)
                {
                    vh.onErrorStatusChange(errorStat);
                }

                return isSuccess;
            }
            private (bool isSuccess, ID_143_STATUS_RESPONSE statusResponse) sendMessage_ID_43_STATUS_REQUEST(string vhID)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);
                ID_143_STATUS_RESPONSE statusResponse = null;
                ID_43_STATUS_REQUEST send_gpp = new ID_43_STATUS_REQUEST()
                {
                    SystemTime = DateTime.Now.ToString(SCAppConstants.TimestampFormat_16)
                };
                SCUtility.RecodeReportInfo(vhID, 0, send_gpp);
                isSuccess = vh.send_S43(send_gpp, out statusResponse);
                SCUtility.RecodeReportInfo(vhID, 0, statusResponse, isSuccess.ToString());
                return (isSuccess, statusResponse);
            }
            #endregion ID_43 StatusRequest
            #region ID_45 PowerOperatorChange
            public bool PowerOperatorChange(string vhID, OperatingPowerMode mode)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);
                ID_145_POWER_OPE_RESPONSE receive_gpp;
                ID_45_POWER_OPE_REQ sned_gpp = new ID_45_POWER_OPE_REQ()
                {
                    OperatingPowerMode = mode
                };
                isSuccess = vh.send_S45(sned_gpp, out receive_gpp);
                return isSuccess;
            }
            #endregion ID_45 PowerOperatorChange
            #region ID_51 Avoid
            public (bool is_success, string result) Avoid(string vh_id, string avoidAddress)
            {
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vh_id);
                List<string> guide_segment_ids = null;
                List<string> guide_section_ids = null;
                List<string> guide_address_ids = null;
                int total_cost = 0;
                bool is_success = true;
                string result = "";
                string vh_current_address = SCUtility.Trim(vh.CUR_ADR_ID, true);
                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Start vh:{vh_id} avoid script,avoid to address:{avoidAddress}...",
                       VehicleID: vh_id,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);

                    if (SCUtility.isEmpty(vh.CMD_ID_1) && SCUtility.isEmpty(vh.CMD_ID_2))
                    {
                        is_success = false;
                        result = $"vh:{vh_id} not excute ohtc command.";
                    }
                    if (!vh.IsReservePause)
                    {
                        is_success = false;
                        result = $"vh:{vh_id} current not in reserve pause.";
                    }
                    if (is_success)
                    {
                        int current_find_count = 0;
                        int max_find_count = 10;
                        List<string> need_by_pass_sec_ids = new List<string>();

                        do
                        {
                            //確認下一段Section，是否可以預約成功
                            string next_walk_section = "";
                            string next_walk_address = "";


                            //(is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                            //    scApp.GuideBLL.getGuideInfo_New2(vh_current_section, vh_current_address, avoidAddress);
                            (is_success, guide_segment_ids, guide_section_ids, guide_address_ids, total_cost) =
                                guideBLL.getGuideInfo(vh_current_address, avoidAddress, need_by_pass_sec_ids);
                            next_walk_section = guide_section_ids[0];
                            next_walk_address = guide_address_ids[0];

                            if (is_success)
                            {

                                var reserve_result = scApp.ReserveBLL.askReserveSuccess(scApp.SectionBLL, vh_id, next_walk_section, next_walk_address);
                                if (!reserve_result.isSuccess &&
                                    SCUtility.isMatche(vh.CanNotReserveInfo.ReservedVhID, reserve_result.reservedVhID))
                                {
                                    is_success = false;
                                    need_by_pass_sec_ids.Add(next_walk_section);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"find the avoid path ,but section:{next_walk_section} is reserved for vh:{reserve_result.reservedVhID}" +
                                             $"add to need by pass sec ids,current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                                       VehicleID: vh.VEHICLE_ID,
                                       CST_ID_L: vh.CST_ID_L,
                                       CST_ID_R: vh.CST_ID_R);
                                }
                                else
                                {
                                    is_success = true;
                                }
                            }
                            if (current_find_count++ > max_find_count)
                            {
                                is_success = false;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find the avoid path ,but over times:{max_find_count}",
                                   VehicleID: vh.VEHICLE_ID,
                                   CST_ID_L: vh.CST_ID_L,
                                   CST_ID_R: vh.CST_ID_R);
                                break;
                            }
                        } while (!is_success);

                        string vh_current_section = SCUtility.Trim(vh.CUR_SEC_ID, true);
                        if (is_success)
                        {
                            is_success = sendMessage_ID_51_AVOID_REQUEST(vh_id, avoidAddress, guide_section_ids.ToArray(), guide_address_ids.ToArray());
                            if (!is_success)
                            {
                                result = $"send avoid to vh fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                            }
                        }
                        else
                        {
                            result = $"find avoid path fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.";
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       Details: $"AvoidRequest fail.vh:{vh_id}, vh current adr:{vh_current_address} ,avoid address:{avoidAddress}.",
                       VehicleID: vh_id);
                }
                return (is_success, result);
            }
            private bool sendMessage_ID_51_AVOID_REQUEST(string vh_id, string avoidAddress, string[] guideSection, string[] guideAddresses)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vh_id);
                ID_151_AVOID_RESPONSE receive_gpp;
                ID_51_AVOID_REQUEST send_gpp = new ID_51_AVOID_REQUEST();
                send_gpp.DestinationAdr = avoidAddress;
                send_gpp.GuideSections.AddRange(guideSection);
                send_gpp.GuideAddresses.AddRange(guideAddresses);

                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                isSuccess = vh.send_Str51(send_gpp, out receive_gpp);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_51 Avoid
            #region ID_39 Pause
            public bool Pause(string vhID, PauseEvent pause_event, PauseType pauseType)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);
                ID_139_PAUSE_RESPONSE receive_gpp;
                ID_39_PAUSE_REQUEST send_gpp = new ID_39_PAUSE_REQUEST()
                {
                    PauseType = pauseType,
                    EventType = pause_event
                };
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                isSuccess = vh.send_Str39(send_gpp, out receive_gpp);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_39 Pause
            #region ID_71 Teaching
            public bool Teaching(string vh_id, string from_adr, string to_adr)
            {
                bool isSuccess = false;
                AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
                ID_171_RANGE_TEACHING_RESPONSE receive_gpp;
                ID_71_RANGE_TEACHING_REQUEST send_gpp = new ID_71_RANGE_TEACHING_REQUEST()
                {
                    FromAdr = from_adr,
                    ToAdr = to_adr
                };
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, send_gpp);
                isSuccess = vh.send_Str71(send_gpp, out receive_gpp);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, 0, receive_gpp, isSuccess.ToString());
                return isSuccess;
            }
            #endregion ID_71 Teaching
            #region ID_91 Alamr Reset
            public bool AlarmReset(string vh_id)
            {
                bool isSuccess = false;
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vh_id);
                ID_191_ALARM_RESET_RESPONSE receive_gpp;
                ID_91_ALARM_RESET_REQUEST sned_gpp = new ID_91_ALARM_RESET_REQUEST()
                {

                };
                isSuccess = vh.send_S91(sned_gpp, out receive_gpp);
                if (isSuccess)
                {
                    isSuccess = receive_gpp?.ReplyCode == 0;
                }
                return isSuccess;
            }
            #endregion ID_91 Alamr Reset
        }
        public class ReceiveProcessor : IDynamicMetaObjectProvider
        {
            Logger logger = LogManager.GetCurrentClassLogger();
            CMDBLL cmdBLL = null;
            VehicleBLL vehicleBLL = null;
            ReportBLL reportBLL = null;
            GuideBLL guideBLL = null;
            VehicleService service = null;
            public ReceiveProcessor(VehicleService _service)
            {
                cmdBLL = scApp.CMDBLL;
                vehicleBLL = scApp.VehicleBLL;
                reportBLL = scApp.ReportBLL;
                guideBLL = scApp.GuideBLL;
                service = _service;
            }
            #region ID_132 TransferCompleteReport
            [ClassAOPAspect]
            public void CommandCompleteReport(string tcpipAgentName, BCFApplication bcfApp, AVEHICLE vh, ID_132_TRANS_COMPLETE_REPORT recive_str, int seq_num)
            {
                scApp.VehicleBLL.setAndPublishPositionReportInfo2Redis(vh.VEHICLE_ID, recive_str);

                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);
                string cmd_id = recive_str.CmdID;
                int travel_dis = recive_str.CmdDistance;
                CompleteStatus completeStatus = recive_str.CmpStatus;
                string cur_sec_id = recive_str.CurrentSecID;
                string cur_adr_id = recive_str.CurrentAdrID;
                string cur_cst_id = recive_str.CSTID;
                string vh_id = vh.VEHICLE_ID.ToString();
                string finish_cmd_id = "";
                //using (TransactionScope tx = SCUtility.getTransactionScope())
                //{
                //    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                //    {
                bool is_success = true;
                var finish_result = service.Command.Finish(cmd_id, completeStatus, travel_dis);
                is_success = is_success && finish_result.isSuccess;
                is_success = is_success && reply_ID_32_TRANS_COMPLETE_RESPONSE(vh, seq_num, finish_cmd_id, finish_result.transferID);
                if (is_success)
                {
                    //tx.Complete();
                    vehicleBLL.doInitialVhCommandInfo(vh_id);
                    scApp.VehicleBLL.cache.resetWillPassSectionInfo(vh_id);
                }
                else
                {
                    return;
                }
                //    }
                //}

                //vh.NotifyVhExcuteCMDStatusChange();
                vh.onExcuteCommandStatusChange();
                vh.onCommandComplete(completeStatus);
                sendCommandCompleteEventToNats(vh.VEHICLE_ID, recive_str);
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
                vh.VhAvoidInfo = null;

                if (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.PAUSING)
                {
                    List<ATRANSFER> cmd_mcs_lst = scApp.CMDBLL.loadUnfinishedTransfer();
                    if (cmd_mcs_lst.Count == 0)
                    {
                        scApp.LineService.TSCStateToPause();
                    }
                }
                tryAskVh2ChargerIdle(vh);
            }

            /// <summary>
            /// 如果等待時間超過了"MAX_WAIT_COMMAND_TIME"，
            /// 就可以讓車子回去充電站待命了。
            /// </summary>
            /// <param name="vh"></param>
            const int MAX_WAIT_COMMAND_TIME = 10000;
            private void tryAskVh2ChargerIdle(AVEHICLE vh)
            {
                string vh_id = vh.VEHICLE_ID;
                SpinWait.SpinUntil(() => false, 3000);
                bool has_cmd_excute = SpinWait.SpinUntil(() => scApp.CMDBLL.cache.hasCmdExcute(vh_id), MAX_WAIT_COMMAND_TIME);
                if (!has_cmd_excute)
                {
                    scApp.VehicleChargerModule.askVhToChargerForWait(vh);
                }
            }



            private bool reply_ID_32_TRANS_COMPLETE_RESPONSE(AVEHICLE vh, int seq_num, string finish_cmd_id, string finish_fransfer_cmd_id)
            {
                ID_32_TRANS_COMPLETE_RESPONSE send_str = new ID_32_TRANS_COMPLETE_RESPONSE
                {
                    ReplyCode = 0,
                    WaitTime = DebugParameter.CommandCompleteWaitTime
                };
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    TranCmpResp = send_str
                };
                Boolean resp_cmp = vh.sendMessage(wrapper, true);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, finish_cmd_id, finish_fransfer_cmd_id, resp_cmp.ToString());
                return resp_cmp;
            }


            private void sendCommandCompleteEventToNats(string vhID, ID_132_TRANS_COMPLETE_REPORT recive_str)
            {
                byte[] arrayByte = new byte[recive_str.CalculateSize()];
                recive_str.WriteTo(new Google.Protobuf.CodedOutputStream(arrayByte));
                scApp.getNatsManager().PublishAsync
                    (string.Format(SCAppConstants.NATS_SUBJECT_VH_COMMAND_COMPLETE_0, vhID), arrayByte);
            }

            #endregion ID_132 TransferCompleteReport
            #region ID_134 TransferEventReport (Position)
            [ClassAOPAspect]
            public void PositionReport(BCFApplication bcfApp, AVEHICLE vh, ID_134_TRANS_EVENT_REP receiveStr, int current_seq_num)
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, current_seq_num, receiveStr);
                int pre_position_seq_num = vh.PrePositionSeqNum;
                bool need_process_position = checkPositionSeqNum(current_seq_num, pre_position_seq_num);
                vh.PrePositionSeqNum = current_seq_num;
                if (!need_process_position)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleBLL), Device: Service.VehicleService.DEVICE_NAME_AGV,
                       Data: $"The vehicles updata position report of seq num is old,by pass this one.old seq num;{pre_position_seq_num},current seq num:{current_seq_num}",
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                    return;
                }

                scApp.VehicleBLL.setAndPublishPositionReportInfo2Redis(vh.VEHICLE_ID, receiveStr);

                EventType eventType = receiveStr.EventType;
                string current_adr_id = SCUtility.isEmpty(receiveStr.CurrentAdrID) ? string.Empty : receiveStr.CurrentAdrID;
                string current_sec_id = SCUtility.isEmpty(receiveStr.CurrentSecID) ? string.Empty : receiveStr.CurrentSecID;
                ASECTION sec_obj = scApp.SectionBLL.cache.GetSection(current_sec_id);
                string current_seg_id = sec_obj == null ? string.Empty : sec_obj.SEG_NUM;
                string last_adr_id = vh.CUR_ADR_ID;
                string last_sec_id = vh.CUR_SEC_ID;
                uint sec_dis = receiveStr.SecDistance;
            }
            const int TOLERANCE_SCOPE = 50;
            private const ushort SEQNUM_MAX = 999;
            private bool checkPositionSeqNum(int currnetNum, int preNum)
            {

                int lower_limit = preNum - TOLERANCE_SCOPE;
                if (lower_limit >= 0)
                {
                    //如果該次的Num介於上次的值減去容錯值(TOLERANCE_SCOPE = 50) 至 上次的值
                    //就代表是舊的資料
                    if (currnetNum > (lower_limit) && currnetNum < preNum)
                    {
                        return false;
                    }
                }
                else
                {
                    //如果上次的值減去容錯值變成負的，代表要再由SENDSEQNUM_MAX往回推
                    lower_limit = SEQNUM_MAX + lower_limit;
                    if (currnetNum > (lower_limit) && currnetNum < preNum)
                    {
                        return false;
                    }
                }
                return true;
            }
            #endregion ID_134 TransferEventReport (Position)
            #region ID_136 TransferEventReport
            [ClassAOPAspect]
            public void TranEventReport(BCFApplication bcfApp, AVEHICLE vh, ID_136_TRANS_EVENT_REP recive_str, int seq_num)
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   seq_num: seq_num,
                   Data: recive_str,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);

                EventType eventType = recive_str.EventType;
                string current_adr_id = recive_str.CurrentAdrID;
                string current_sec_id = recive_str.CurrentSecID;
                string carrier_id = recive_str.CSTID;
                string last_adr_id = vh.CUR_ADR_ID;
                string last_sec_id = vh.CUR_SEC_ID;
                string req_block_id = recive_str.RequestBlockID;
                string excute_cmd_id = recive_str.CmdID;
                var reserveInfos = recive_str.ReserveInfos;
                BCRReadResult bCRReadResult = recive_str.BCRReadResult;

                switch (eventType)
                {
                    case EventType.ReserveReq:
                        if (DebugParameter.testRetryReserveReq) return;
                        TranEventReport_PathReserveReq(bcfApp, vh, seq_num, reserveInfos, excute_cmd_id);
                        break;
                    case EventType.LoadArrivals:
                        if (DebugParameter.testRetryLoadArrivals) return;
                        TranEventReport_LoadArrivals(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.LoadComplete:
                        if (DebugParameter.testRetryLoadComplete) return;
                        TranEventReport_LoadComplete(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.UnloadArrivals:
                        if (DebugParameter.testRetryUnloadArrivals) return;
                        TranEventReport_UnloadArrive(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.UnloadComplete:
                        if (DebugParameter.testRetryUnloadComplete) return;
                        TranEventReport_UnloadComplete(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.Vhloading:
                        if (DebugParameter.testRetryVhloading) return;
                        TranEventReport_Loading(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.Vhunloading:
                        if (DebugParameter.testRetryVhunloading) return;
                        TranEventReport_Unloading(bcfApp, vh, seq_num, eventType, excute_cmd_id);
                        break;
                    case EventType.Bcrread:
                        if (DebugParameter.testRetryBcrread) return;
                        TranEventReport_BCRRead(bcfApp, vh, seq_num, eventType, carrier_id, bCRReadResult, excute_cmd_id);
                        break;
                    default:
                        replyTranEventReport(bcfApp, eventType, vh, seq_num, excute_cmd_id);
                        break;
                }
            }
            private void TranEventReport_LoadArrivals(BCFApplication bcfApp, AVEHICLE vh, int seqNum
                                                    , EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process report {eventType}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                vh.LastLoadCompleteCommandID = cmdID;
                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                if (isTranCmd)
                {
                    scApp.TransferBLL.db.transfer.updateTranStatus2LoadArrivals(cmd.TRANSFER_ID);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"do report {eventType} to mcs.",
                               VehicleID: vh.VEHICLE_ID,
                               CST_ID_L: vh.CST_ID_L,
                               CST_ID_R: vh.CST_ID_R);
                            bool isCreatReportInfoSuccess = scApp.ReportBLL.newReportLoadArrivals(cmd.TRANSFER_ID, reportqueues);
                            if (!isCreatReportInfoSuccess)
                            {
                                return;
                            }
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                        }
                        Boolean resp_cmp = replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                        if (resp_cmp)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                    scApp.SysExcuteQualityBLL.updateSysExecQity_ArrivalSourcePort(cmd.TRANSFER_ID);
                }
                else
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }

                scApp.VehicleBLL.doLoadArrivals(vh.VEHICLE_ID);
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
            }
            private void TranEventReport_LoadComplete(BCFApplication bcfApp, AVEHICLE vh, int seqNum
                                                    , EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Process report {eventType}",
                           VehicleID: vh.VEHICLE_ID,
                           CST_ID_L: vh.CST_ID_L,
                           CST_ID_R: vh.CST_ID_R);
                scApp.MapBLL.getPortID(vh.CUR_ADR_ID, out string port_id);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                vh.LastLoadCompleteCommandID = cmdID;
                updateCarrierInVehicleLocation(vh, cmd, "");
                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                if (isTranCmd)
                {
                    string transfer_id = cmd.TRANSFER_ID;
                    scApp.TransferBLL.db.transfer.updateTranStatus2Transferring(transfer_id);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"do report {eventType} to mcs.",
                               VehicleID: vh.VEHICLE_ID,
                               CST_ID_L: vh.CST_ID_L,
                               CST_ID_R: vh.CST_ID_R);
                            bool isCreatReportInfoSuccess = scApp.ReportBLL.newReportLoadComplete(cmd.TRANSFER_ID, reportqueues);
                            if (!isCreatReportInfoSuccess)
                            {
                                return;
                            }
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                        }

                        Boolean resp_cmp = replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);

                        if (resp_cmp)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                }
                else
                {
                    //if (!SCUtility.isEmpty(cmd.CARRIER_ID))
                    //    scApp.ReportBLL.newReportLoadComplete(vh.Real_ID, cmd.CARRIER_ID, vh.Real_ID, null);
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }

                scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(cmd.SOURCE_PORT, string.Empty);
                scApp.VehicleBLL.doLoadComplete(vh.VEHICLE_ID);
            }

            private void updateCarrierInVehicleLocation(AVEHICLE vh, ACMD cmd, string readCarrierID)
            {
                var carrier_location = tryFindCarrierLocationOnVehicle(vh.VEHICLE_ID, cmd.CARRIER_ID, readCarrierID);
                if (carrier_location.isExist)
                {
                    scApp.CarrierBLL.db.updateLocationAndState
                        (cmd.CARRIER_ID, carrier_location.Location.ID, E_CARRIER_STATE.Installed);
                }
                else
                {
                    string location_id_r = vh.LocationRealID_R;
                    string location_id_l = vh.LocationRealID_L;
                    //在找不到在哪個CST時，要找自己的Table是否有該Vh carrier如果有就上報另一個沒carrier的
                    var check_has_carrier_on_location_result = scApp.CarrierBLL.db.hasCarrierOnVhLocation(location_id_l);
                    if (check_has_carrier_on_location_result.has)
                    {
                        scApp.CarrierBLL.db.updateLocationAndState
                            (cmd.CARRIER_ID, location_id_r, E_CARRIER_STATE.Installed);
                    }
                    else
                    {
                        scApp.CarrierBLL.db.updateLocationAndState
                            (cmd.CARRIER_ID, location_id_l, E_CARRIER_STATE.Installed);
                    }

                    //scApp.CarrierBLL.db.updateLocationAndState
                    //    (cmd.CARRIER_ID, "", E_CARRIER_STATE.Installed);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"vh:{vh.VEHICLE_ID} report load complete cst id:{SCUtility.Trim(cmd.CARRIER_ID, true)}, " +
                             $"but no find carrier in vh. location r cst id:{vh.CST_ID_R},location l cst id:{vh.CST_ID_L}",
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                }
            }

            private (bool isExist, AVEHICLE.Location Location) tryFindCarrierLocationOnVehicle(string vhID, string commandCarrierID, string readCarrierID)
            {
                (bool isExist, AVEHICLE.Location Location) location = (false, null);
                var vh = vehicleBLL.cache.getVehicle(vhID);
                bool is_exist = SpinWait.SpinUntil(() => vh.IsCarreirExist(commandCarrierID), 1000);
                if (is_exist)
                {
                    location = vh.getCarreirLocation(commandCarrierID);
                }
                else
                {
                    if (!SCUtility.isEmpty(readCarrierID))
                    {
                        is_exist = SpinWait.SpinUntil(() => vh.IsCarreirExist(readCarrierID), 1000);
                        if (is_exist)
                        {
                            location = vh.getCarreirLocation(readCarrierID);
                        }
                    }
                }
                return location;
            }

            private void TranEventReport_UnloadArrive(BCFApplication bcfApp, AVEHICLE vh, int seqNum
                                                    , EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Process report {eventType}",
                           VehicleID: vh.VEHICLE_ID,
                           CST_ID_L: vh.CST_ID_L,
                           CST_ID_R: vh.CST_ID_R);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                if (isTranCmd)
                {
                    scApp.TransferBLL.db.transfer.updateTranStatus2UnloadArrive(cmd.TRANSFER_ID);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {

                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"do report {eventType} to mcs.",
                               VehicleID: vh.VEHICLE_ID,
                               CST_ID_L: vh.CST_ID_L,
                               CST_ID_R: vh.CST_ID_R);
                            bool isCreatReportInfoSuccess = scApp.ReportBLL.newReportUnloadArrivals(cmd.TRANSFER_ID, reportqueues);
                            if (!isCreatReportInfoSuccess)
                            {
                                return;
                            }
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                        }

                        Boolean resp_cmp = replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);

                        if (resp_cmp)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                    scApp.SysExcuteQualityBLL.updateSysExecQity_ArrivalDestnPort(cmd.TRANSFER_ID);
                }
                else
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }

                scApp.VehicleBLL.doUnloadArrivals(vh.VEHICLE_ID, cmdID);
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
            }
            private void TranEventReport_UnloadComplete(BCFApplication bcfApp, AVEHICLE vh, int seqNum
                                                    , EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Process report {eventType}",
                           VehicleID: vh.VEHICLE_ID,
                           CST_ID_L: vh.CST_ID_L,
                           CST_ID_R: vh.CST_ID_R);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                if (isTranCmd)
                {
                    scApp.TransferBLL.db.transfer.updateTranStatus2UnloadComplete(cmd.TRANSFER_ID);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"do report {eventType} to mcs.",
                               VehicleID: vh.VEHICLE_ID,
                               CST_ID_L: vh.CST_ID_L,
                               CST_ID_R: vh.CST_ID_R);
                            bool isCreatReportInfoSuccess = true;
                            //if (!scApp.PortStationBLL.OperateCatch.IsEqPort(scApp.EqptBLL, cmd.DESTINATION_PORT))
                            isCreatReportInfoSuccess = scApp.ReportBLL.newReportUnloadComplete(cmd.TRANSFER_ID, reportqueues);

                            if (!isCreatReportInfoSuccess)
                            {
                                return;
                            }
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                        }
                        scApp.CarrierBLL.db.updateLocationAndState(cmd.CARRIER_ID, cmd.DESTINATION_PORT, E_CARRIER_STATE.Complete);

                        Boolean resp_cmp = replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);

                        if (resp_cmp)
                        {
                            tx.Complete();
                        }
                        else
                        {
                            return;
                        }
                    }
                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                }
                else
                {
                    scApp.CarrierBLL.db.updateLocationAndState(cmd.CARRIER_ID, cmd.DESTINATION_PORT, E_CARRIER_STATE.Complete);
                    //if (!SCUtility.isEmpty(cmd.CARRIER_ID))
                    //{
                    //    scApp.CarrierBLL.db.updateLocationAndState(cmd.CARRIER_ID, cmd.DESTINATION_PORT, E_CARRIER_STATE.Complete);
                    //    scApp.ReportBLL.newReportUnloadComplete(vh.Real_ID, cmd.CARRIER_ID, cmd.DESTINATION_PORT, null);
                    //}
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }
                scApp.VehicleBLL.doUnloadComplete(vh.VEHICLE_ID);
            }


            private void TranEventReport_BCRRead(BCFApplication bcfApp, AVEHICLE vh, int seqNum,
                                               EventType eventType, string readCarrierID, BCRReadResult bCRReadResult,
                                               string cmdID)
            {
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                string rename_carrier_id = string.Empty;
                ReplyActionType replyActionType = ReplyActionType.Continue;
                if (cmd == null)
                {
                    if (!SCUtility.isEmpty(vh.LastLoadCompleteCommandID))
                    {
                        cmd = scApp.CMDBLL.GetCMD_OHTCByID(vh.LastLoadCompleteCommandID);
                    }
                    if (cmd == null)
                    {
                        switch (bCRReadResult)
                        {
                            case BCRReadResult.BcrMisMatch:
                                replyActionType = ReplyActionType.CancelIdMisnatch;
                                rename_carrier_id = readCarrierID;
                                break;
                            case BCRReadResult.BcrReadFail:
                                replyActionType = ReplyActionType.CancelIdReadFailed;
                                string new_carrier_id =
                                    $"UNKF{vh.Real_ID.Trim()}{DateTime.Now.ToString(SCAppConstants.TimestampFormat_12)}";
                                rename_carrier_id = new_carrier_id;
                                break;
                            default:
                                replyActionType = ReplyActionType.Continue;
                                break;
                        }
                        replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID,
                            renameCarrierID: rename_carrier_id,
                            actionType: replyActionType);
                        return;
                    }
                }
                //updateCarrierInVehicleLocation(vh, cmd, readCarrierID);
                bool is_tran_cmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                switch (bCRReadResult)
                {
                    case BCRReadResult.BcrMisMatch:
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"BCR miss match happend,start abort command id:{cmd.ID.Trim()}",
                           VehicleID: vh.VEHICLE_ID,
                           CST_ID_L: vh.CST_ID_L,
                           CST_ID_R: vh.CST_ID_R);
                        if (DebugParameter.isContinueByIDReadFail)
                        {
                            rename_carrier_id = SCUtility.Trim(cmd.CARRIER_ID, true);
                            replyActionType = ReplyActionType.Continue;
                        }
                        else
                        {
                            rename_carrier_id = readCarrierID;
                            replyActionType = ReplyActionType.CancelIdMisnatch;
                        }
                        scApp.CarrierBLL.db.updateRenameID(cmd.CARRIER_ID, rename_carrier_id);

                        //todo kevin 要重新Review mismatch fail時候的流程
                        //todo kevin 要加入duplicate 的流程
                        replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID,
                            renameCarrierID: rename_carrier_id,
                            actionType: replyActionType);
                        //scApp.CarrierBLL.db.updateRenameID(cmd.CARRIER_ID, readCarrierID);
                        break;
                    case BCRReadResult.BcrReadFail:
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"BCR read fail happend,start abort command id:{cmd.ID.Trim()}",
                           VehicleID: vh.VEHICLE_ID,
                           CST_ID_L: vh.CST_ID_L,
                           CST_ID_R: vh.CST_ID_R);

                        //if (DebugParameter.isContinueByIDReadFail)
                        if (true)
                        {
                            rename_carrier_id = SCUtility.Trim(cmd.CARRIER_ID, true);
                            replyActionType = ReplyActionType.Continue;
                        }
                        else
                        {
                            string new_carrier_id =
                                $"UNKF{vh.Real_ID.Trim()}{DateTime.Now.ToString(SCAppConstants.TimestampFormat_12)}";
                            rename_carrier_id = new_carrier_id;
                            replyActionType = ReplyActionType.CancelIdReadFailed;
                        }
                        scApp.CarrierBLL.db.updateRenameID(cmd.CARRIER_ID, rename_carrier_id);

                        replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID,
                            renameCarrierID: rename_carrier_id,
                            actionType: replyActionType);
                        //scApp.CarrierBLL.db.updateRenameID(cmd.CARRIER_ID, new_carrier_id);
                        break;
                    case BCRReadResult.BcrNormal:
                        replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                        break;
                }
                //if (is_tran_cmd)
                //{
                //    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                //    scApp.ReportBLL.newReportCarrierIDReadReport(cmd.TRANSFER_ID, reportqueues);
                //    scApp.ReportBLL.insertMCSReport(reportqueues);
                //    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                //}
            }
            private void TranEventReport_Loading(BCFApplication bcfApp, AVEHICLE vh, int seqNum, EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process report {eventType}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                if (cmd == null)
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                    return;
                }
                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);

                if (isTranCmd)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"do report {eventType} to mcs.",
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                    scApp.TransferBLL.db.transfer.updateTranStatus2Loading(cmd.TRANSFER_ID);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            bool isSuccess = true;
                            scApp.ReportBLL.newReportLoading(cmd.TRANSFER_ID, reportqueues);
                            scApp.ReportBLL.insertMCSReport(reportqueues);

                            if (isSuccess)
                            {
                                if (replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID))
                                {
                                    tx.Complete();
                                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                                }
                            }
                        }
                    }
                }
                else
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }
                scApp.VehicleBLL.doLoading(vh.VEHICLE_ID);
            }
            private void TranEventReport_Unloading(BCFApplication bcfApp, AVEHICLE vh, int seqNum, EventType eventType, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process report {eventType}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                ACMD cmd = scApp.CMDBLL.GetCMD_OHTCByID(cmdID);
                if (cmd == null)
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                    return;
                }

                bool isTranCmd = !SCUtility.isEmpty(cmd.TRANSFER_ID);
                if (isTranCmd)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"do report {eventType} to mcs.",
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                    scApp.TransferBLL.db.transfer.updateTranStatus2Unloading(cmd.TRANSFER_ID);
                    List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                    using (TransactionScope tx = SCUtility.getTransactionScope())
                    {
                        using (DBConnection_EF con = DBConnection_EF.GetUContext())
                        {
                            bool isSuccess = true;
                            scApp.ReportBLL.newReportUnloading(cmd.TRANSFER_ID, reportqueues);
                            scApp.ReportBLL.insertMCSReport(reportqueues);
                            if (isSuccess)
                            {
                                if (replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID))
                                {
                                    tx.Complete();
                                    scApp.ReportBLL.newSendMCSMessage(reportqueues);
                                }
                            }
                        }
                    }
                }
                else
                {
                    replyTranEventReport(bcfApp, eventType, vh, seqNum, cmdID);
                }
                scApp.VehicleBLL.doUnloading(vh.VEHICLE_ID);

                //scApp.MapBLL.getPortID(vh.CUR_ADR_ID, out string port_id);
                scApp.PortBLL.OperateCatch.updatePortStationCSTExistStatus(cmd.DESTINATION_PORT, cmd.CARRIER_ID);
            }
            object reserve_lock = new object();
            private void TranEventReport_PathReserveReq(BCFApplication bcfApp, AVEHICLE vh, int seqNum, RepeatedField<ReserveInfo> reserveInfos, string cmdID)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process path reserve request,request path id:{reserveInfos.ToString()}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);


                lock (reserve_lock)
                {
                    var ReserveResult = scApp.ReserveBLL.IsReserveSuccessNew(vh.VEHICLE_ID, reserveInfos);
                    if (ReserveResult.isSuccess)
                    {
                        scApp.VehicleBLL.cache.ResetCanNotReserveInfo(vh.VEHICLE_ID);//TODO Mark check
                                                                                     //防火門機制要檢查其影響區域有沒要被預約了。
                                                                                     //if (scApp.getCommObjCacheManager().isSectionAtFireDoorArea(ReserveResult.reservedSecID))
                                                                                     //{
                                                                                     //    //Task. scApp.getCommObjCacheManager().sectionReserveAtFireDoorArea(reserveInfo.Value);
                                                                                     //    Task.Run(() => scApp.getCommObjCacheManager().sectionReserveAtFireDoorArea(ReserveResult.reservedSecID));
                                                                                     //}
                    }
                    else
                    {
                        string reserve_fail_section = reserveInfos[0].ReserveSectionID;
                        DriveDirction driveDirction = reserveInfos[0].DriveDirction;
                        ASECTION reserve_fail_sec_obj = scApp.SectionBLL.cache.GetSection(reserve_fail_section);
                        scApp.VehicleBLL.cache.SetUnsuccessReserveInfo(vh.VEHICLE_ID, new AVEHICLE.ReserveUnsuccessInfo(ReserveResult.reservedVhID, "", reserve_fail_section));
                        Task.Run(() => service.Avoid.tryNotifyVhAvoid(vh.VEHICLE_ID, ReserveResult.reservedVhID));
                    }
                    replyTranEventReport(bcfApp, EventType.ReserveReq, vh, seqNum, cmdID,
                                         reserveSuccess: ReserveResult.isSuccess,
                                         reserveInfos: reserveInfos);
                }
            }
            private bool replyTranEventReport(BCFApplication bcfApp, EventType eventType, AVEHICLE vh, int seq_num, string cmdID,
                                              bool reserveSuccess = true, bool canBlockPass = true, bool canHIDPass = true,
                                              string renameCarrierID = "", ReplyActionType actionType = ReplyActionType.Continue, RepeatedField<ReserveInfo> reserveInfos = null)
            {
                ID_36_TRANS_EVENT_RESPONSE send_str = new ID_36_TRANS_EVENT_RESPONSE
                {
                    EventType = eventType,
                    IsReserveSuccess = reserveSuccess ? ReserveResult.Success : ReserveResult.Unsuccess,
                    IsBlockPass = canBlockPass ? PassType.Pass : PassType.Block,
                    ReplyCode = 0,
                    RenameCarrierID = renameCarrierID,
                    ReplyAction = actionType,
                    CmdID = cmdID ?? string.Empty
                };
                if (reserveInfos != null)
                {
                    send_str.ReserveInfos.AddRange(reserveInfos);
                }
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    ImpTransEventResp = send_str
                };
                Boolean resp_cmp = vh.sendMessage(wrapper, true);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
                return resp_cmp;
            }
            #endregion ID_136 TransferEventReport
            #region ID_138 GuideInfoRequest
            [ClassAOPAspect]
            public void GuideInfoRequest(BCFApplication bcfApp, AVEHICLE vh, ID_138_GUIDE_INFO_REQUEST recive_str, int seq_num)
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   seq_num: seq_num,
                   Data: recive_str,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);
                var request_from_to_list = recive_str.FromToAdrList;

                List<GuideInfo> guide_infos = new List<GuideInfo>();
                foreach (FromToAdr from_to_adr in request_from_to_list)
                {
                    //var guide_info = scApp.GuideBLL.getGuideInfo(from_to_adr.From, from_to_adr.To);
                    var guide_info = CalculationPath(vh, from_to_adr.From, from_to_adr.To);

                    GuideInfo guide = new GuideInfo();
                    guide.FromTo = from_to_adr;
                    if (guide_info.isSuccess)
                    {
                        guide.GuideAddresses.AddRange(guide_info.guideAddressIds);
                        guide.GuideSections.AddRange(guide_info.guideSectionIds);
                        guide.Distance = (uint)guide_info.totalCost;
                    }
                    guide_infos.Add(guide);
                }

                bool is_success = reply_ID_38_TRANS_COMPLETE_RESPONSE(vh, seq_num, guide_infos);
                if (is_success && guide_infos.Count > 0)
                {
                    vh.VhAvoidInfo = null;
                    var shortest_path = guide_infos.OrderBy(info => info.Distance).First();
                    scApp.VehicleBLL.cache.setWillPassSectionInfo(vh.VEHICLE_ID, shortest_path.GuideSections.ToList());
                }
            }
            private (bool isSuccess, List<string> guideSegmentIds, List<string> guideSectionIds, List<string> guideAddressIds, int totalCost)
                CalculationPath(AVEHICLE vh, string fromAdr, string toAdr)
            {
                bool is_success = false;
                List<string> guide_segment_isd = null;
                List<string> guide_section_isd = null;
                List<string> guide_address_isd = null;
                int total_cost = 0;

                //bool is_after_avoid_complete = vh.VhAvoidInfo != null;
                List<string> by_pass_sections = new List<string>();
                var reserved_section_ids = scApp.ReserveBLL.GetCurrentReserveSectionList(vh.VEHICLE_ID);
                if (reserved_section_ids.Count > 0)
                    by_pass_sections.AddRange(reserved_section_ids);
                var vehicle_section_ids = scApp.VehicleBLL.cache.LoadVehicleCurrentSection(vh.VEHICLE_ID);
                if (vehicle_section_ids.Count > 0)
                    by_pass_sections.AddRange(vehicle_section_ids);
                (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost) =
                    CalculationPathAfterAvoid(vh, fromAdr, toAdr, by_pass_sections);
                //if (is_after_avoid_complete)
                //{
                //    (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost) =
                //        CalculationPathAfterAvoid(vh, fromAdr, toAdr);
                //}
                //else
                //{
                //    (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost) =
                //        scApp.GuideBLL.getGuideInfo(fromAdr, toAdr);
                //}
                return (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost);
            }

            private (bool isSuccess, List<string> guideSegmentIds, List<string> guideSectionIds, List<string> guideAddressIds, int totalCost)
                CalculationPathAfterAvoid(AVEHICLE vh, string fromAdr, string toAdr, List<string> needByPassSecIDs = null)
            {
                int current_find_count = 0;
                int max_find_count = 10;

                bool is_success = true;
                List<string> guide_segment_isd = null;
                List<string> guide_section_isd = null;
                List<string> guide_address_isd = null;
                int total_cost = 0;
                bool is_need_check_reserve_status = true;
                List<string> need_by_pass_sec_ids = new List<string>();
                if (needByPassSecIDs != null)
                {
                    need_by_pass_sec_ids.AddRange(needByPassSecIDs);
                }
                do
                {
                    //如果有找到路徑則確認一下段是否可以預約的到
                    if (current_find_count != max_find_count) //如果是最後一次的話，就不要在確認預約狀態了。
                    {
                        (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost)
                            = scApp.GuideBLL.getGuideInfo(fromAdr, toAdr, need_by_pass_sec_ids);
                        if (is_success)
                        {
                            //確認下一段Section，是否可以預約成功
                            string next_walk_section = "";
                            string next_walk_address = "";
                            if (guide_section_isd != null && guide_section_isd.Count > 0)
                            {
                                next_walk_section = guide_section_isd[0];
                                next_walk_address = guide_address_isd[0];
                            }

                            if (!SCUtility.isEmpty(next_walk_section)) //由於有可能找出來後，是剛好在原地
                            {
                                if (is_success)
                                {
                                    var reserve_result = scApp.ReserveBLL.askReserveSuccess
                                        (scApp.SectionBLL, vh.VEHICLE_ID, next_walk_section, next_walk_address);
                                    if (reserve_result.isSuccess)
                                    {
                                        is_success = true;
                                    }
                                    else
                                    {
                                        is_success = false;
                                        need_by_pass_sec_ids.Add(next_walk_section);
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"find the override path ,but section:{next_walk_section} is reserved for vh:{reserve_result.reservedVhID}" +
                                                 $"add to need by pass sec ids",
                                           VehicleID: vh.VEHICLE_ID);
                                    }

                                    //4.在準備送出前，如果是因Avoid完成所下的Over ride，要判斷原本block section是否已經可以預約到了，是才可以下給車子
                                    if (is_success && vh.VhAvoidInfo != null && is_need_check_reserve_status)
                                    {
                                        bool is_pass_before_blocked_section = true;
                                        if (guide_section_isd != null)
                                        {
                                            is_pass_before_blocked_section &= guide_section_isd.Contains(vh.VhAvoidInfo.BlockedSectionID);
                                        }
                                        if (is_pass_before_blocked_section)
                                        {
                                            //is_success = false;
                                            //string before_block_section_id = vh.VhAvoidInfo.BlockedSectionID;
                                            //need_by_pass_sec_ids.Add(before_block_section_id);

                                            //如果有則要嘗試去預約，如果等了20秒還是沒有釋放出來則嘗試別條路徑
                                            string before_block_section_id = vh.VhAvoidInfo.BlockedSectionID;
                                            if (!SpinWait.SpinUntil(() => scApp.ReserveBLL.TryAddReservedSection
                                            (vh.VEHICLE_ID, before_block_section_id, isAsk: true).OK, 15000))
                                            {
                                                is_success = false;
                                                need_by_pass_sec_ids.Add(before_block_section_id);
                                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                                   Data: $"wait more than 5 seconds,before block section id:{before_block_section_id} not release, by pass section:{before_block_section_id} find next path.current by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                                                   VehicleID: vh.VEHICLE_ID);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            ////如果在找不到路的時候，就把原本By pass的路徑給打開，然後再找一次
                            ////該次就不檢查原本預約不到的路是否已經可以過了，即使不能過也再下一次走看看
                            if (need_by_pass_sec_ids != null && need_by_pass_sec_ids.Count > 0)
                            {
                                is_success = false;
                                need_by_pass_sec_ids.Clear();
                                is_need_check_reserve_status = false;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find path fail vh:{vh.VEHICLE_ID}, current address:{vh.CUR_ADR_ID} ," +
                                   $" by pass section:{string.Join(",", need_by_pass_sec_ids)},clear all by pass section and then continue find override path.",
                                   VehicleID: vh.VEHICLE_ID);

                            }
                            else
                            {
                                //如果找不到路徑，則就直接跳出搜尋的Loop
                                is_success = false;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find path fail vh:{vh.VEHICLE_ID}, current address:{vh.CUR_ADR_ID} ," +
                                   $" by pass section{string.Join(",", need_by_pass_sec_ids)}",
                                   VehicleID: vh.VEHICLE_ID);
                                break;
                            }
                        }
                    }
                    else
                    {
                        (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost)
                            = scApp.GuideBLL.getGuideInfo(fromAdr, toAdr);
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"find the override path result:{is_success} vh:{vh.VEHICLE_ID} vh current address:{vh.CUR_ADR_ID} ," +
                       $". by pass section:{string.Join(",", need_by_pass_sec_ids)}",
                       VehicleID: vh.VEHICLE_ID);

                }
                while (!is_success && current_find_count++ <= max_find_count);
                return (is_success, guide_segment_isd, guide_section_isd, guide_address_isd, total_cost);
            }

            private bool reply_ID_38_TRANS_COMPLETE_RESPONSE(AVEHICLE vh, int seq_num, List<GuideInfo> guideInfos)
            {
                ID_38_GUIDE_INFO_RESPONSE send_str = new ID_38_GUIDE_INFO_RESPONSE();
                send_str.GuideInfoList.Add(guideInfos);
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    GuideInfoResp = send_str
                };
                Boolean resp_cmp = vh.sendMessage(wrapper, true);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
                return resp_cmp;
            }

            #endregion ID_138 GuideInfoRequest
            #region ID_144 StatusReport
            public void ReserveStopTest(string vhID, bool is_reserve_stop)
            {
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                scApp.VehicleBLL.cache.SetReservePause(vhID, is_reserve_stop ? VhStopSingle.On : VhStopSingle.Off);
            }
            public void CST_R_DisaplyTest(string vhID, bool hasCst)
            {
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                scApp.VehicleBLL.cache.SetCSTR(vhID, hasCst);
            }
            public void CST_L_DisaplyTest(string vhID, bool hasCst)
            {
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                scApp.VehicleBLL.cache.SetCSTL(vhID, hasCst);
            }
            [ClassAOPAspect]
            public void StatusReport(BCFApplication bcfApp, AVEHICLE vh, ID_144_STATUS_CHANGE_REP recive_str, int seq_num)
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   seq_num: seq_num,
                   Data: recive_str,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
                SCUtility.RecordReportInfo(vh.VEHICLE_ID, seq_num, recive_str);

                uint batteryCapacity = recive_str.BatteryCapacity;
                VHModeStatus modeStat = scApp.VehicleBLL.DecideVhModeStatus(vh.VEHICLE_ID, recive_str.ModeStatus, batteryCapacity);
                VHActionStatus actionStat = recive_str.ActionStatus;
                VhPowerStatus powerStat = recive_str.PowerStatus;
                string cmd_id_1 = recive_str.CmdId1;
                string cmd_id_2 = recive_str.CmdId2;
                string cst_id_l = recive_str.CstIdL;
                string cst_id_r = recive_str.CstIdR;
                VhChargeStatus chargeStatus = recive_str.ChargeStatus;
                VhStopSingle reserveStatus = recive_str.ReserveStatus;
                VhStopSingle obstacleStat = recive_str.ObstacleStatus;
                VhStopSingle blockingStat = recive_str.BlockingStatus;
                VhStopSingle pauseStat = recive_str.PauseStatus;
                VhStopSingle errorStat = recive_str.ErrorStatus;
                VhLoadCSTStatus load_cst_status_l = recive_str.HasCstL;
                VhLoadCSTStatus load_cst_status_r = recive_str.HasCstR;
                bool has_cst_l = load_cst_status_l == VhLoadCSTStatus.Exist;
                bool has_cst_r = load_cst_status_r == VhLoadCSTStatus.Exist;
                string[] will_pass_section_id = recive_str.WillPassGuideSection.ToArray();

                int obstacleDIST = recive_str.ObstDistance;
                string obstacleVhID = recive_str.ObstVehicleID;
                int steeringWheel = recive_str.SteeringWheel;
                bool hasdifferent = vh.BATTERYCAPACITY != batteryCapacity ||
                                    vh.MODE_STATUS != modeStat ||
                                    vh.ACT_STATUS != actionStat ||
                                    !SCUtility.isMatche(vh.CMD_ID_1, cmd_id_1) ||
                                    !SCUtility.isMatche(vh.CMD_ID_2, cmd_id_2) ||
                                    !SCUtility.isMatche(vh.CST_ID_L, cst_id_l) ||
                                    !SCUtility.isMatche(vh.CST_ID_R, cst_id_r) ||
                                    vh.ChargeStatus != chargeStatus ||
                                    vh.RESERVE_PAUSE != reserveStatus ||
                                    vh.OBS_PAUSE != obstacleStat ||
                                    vh.BLOCK_PAUSE != blockingStat ||
                                    vh.CMD_PAUSE != pauseStat ||
                                    vh.ERROR != errorStat ||
                                    vh.HAS_CST_L != has_cst_l ||
                                    vh.HAS_CST_R != has_cst_r ||
                                    !SCUtility.isMatche(vh.PredictSections, will_pass_section_id)
                                    ;
                if (hasdifferent)
                {
                    scApp.VehicleBLL.cache.updateVehicleStatus(scApp.CMDBLL, vh.VEHICLE_ID,
                                                         cst_id_l, cst_id_r, modeStat, actionStat, chargeStatus,
                                                         blockingStat, pauseStat, obstacleStat, VhStopSingle.Off, errorStat, reserveStatus,
                                                         has_cst_l, has_cst_r,
                                                         cmd_id_1, cmd_id_2,
                                                         batteryCapacity, will_pass_section_id);
                }
                if (modeStat != vh.MODE_STATUS)
                {
                    vh.onModeStatusChange(modeStat);
                }
                if (errorStat != vh.ERROR)
                {
                    vh.onErrorStatusChange(errorStat);
                }

                //  reply_status_event_report(bcfApp, eqpt, seq_num);
            }
            private bool reply_status_event_report(BCFApplication bcfApp, AVEHICLE vh, int seq_num)
            {
                ID_44_STATUS_CHANGE_RESPONSE send_str = new ID_44_STATUS_CHANGE_RESPONSE
                {
                    ReplyCode = 0
                };
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    StatusChangeResp = send_str
                };

                //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, eqpt.TcpIpAgentName, wrapper, true);
                Boolean resp_cmp = vh.sendMessage(wrapper, true);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                  seq_num: seq_num, Data: send_str,
                  VehicleID: vh.VEHICLE_ID,
                  CST_ID_L: vh.CST_ID_L,
                  CST_ID_R: vh.CST_ID_R);
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
                return resp_cmp;
            }
            #endregion ID_144 StatusReport
            #region ID_152 AvoidCompeteReport
            [ClassAOPAspect]
            public void AvoidCompleteReport(BCFApplication bcfApp, AVEHICLE vh, ID_152_AVOID_COMPLETE_REPORT recive_str, int seq_num)
            {
                if (scApp.getEQObjCacheManager().getLine().ServerPreStop)
                    return;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process Avoid complete report.vh current address:{vh.CUR_ADR_ID}, current section:{vh.CUR_SEC_ID}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);

                ID_52_AVOID_COMPLETE_RESPONSE send_str = null;
                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);
                send_str = new ID_52_AVOID_COMPLETE_RESPONSE
                {
                    ReplyCode = 0
                };
                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    AvoidCompleteResp = send_str
                };

                //Boolean resp_cmp = ITcpIpControl.sendGoogleMsg(bcfApp, tcpipAgentName, wrapper, true);
                Boolean resp_cmp = vh.sendMessage(wrapper, true);

                SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());

                //在避車完成之後，先清除掉原本已經預約的路徑，接著再將自己當下的路徑預約回來，確保不會被預約走
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh.VEHICLE_ID);
                //SpinWait.SpinUntil(() => false, 1000);
                var result = scApp.ReserveBLL.TryAddReservedSection(vh.VEHICLE_ID, vh.CUR_SEC_ID,
                                                                    sensorDir: Mirle.Hlts.Utils.HltDirection.None,
                                                                    forkDir: Mirle.Hlts.Utils.HltDirection.None);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(ReserveBLL), Device: "AGV",
                   Data: $"vh:{vh.VEHICLE_ID} reserve section:{vh.CUR_SEC_ID} after remove all reserved(avoid complete),result:{result.ToString()}",
                   VehicleID: vh.VEHICLE_ID);

            }
            #endregion ID_152 AvoidCompeteReport
            #region ID_172 RangeTeachingCompleteReport
            [ClassAOPAspect]
            public void RangeTeachingCompleteReport(string tcpipAgentName, BCFApplication bcfApp, AVEHICLE eqpt, ID_172_RANGE_TEACHING_COMPLETE_REPORT recive_str, int seq_num)
            {
                ID_72_RANGE_TEACHING_COMPLETE_RESPONSE response = null;
                response = new ID_72_RANGE_TEACHING_COMPLETE_RESPONSE()
                {
                    ReplyCode = 0
                };

                WrapperMessage wrapper = new WrapperMessage
                {
                    SeqNum = seq_num,
                    RangeTeachingCmpResp = response
                };
                Boolean resp_cmp = eqpt.sendMessage(wrapper, true);
                SCUtility.RecodeReportInfo(eqpt.VEHICLE_ID, seq_num, response, resp_cmp.ToString());
            }
            #endregion ID_172 RangeTeachingCompleteReport
            #region ID_194 AlarmReport
            [ClassAOPAspect]
            public void AlarmReport(BCFApplication bcfApp, AVEHICLE vh, ID_194_ALARM_REPORT recive_str, int seq_num)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                  seq_num: seq_num, Data: recive_str,
                  VehicleID: vh.VEHICLE_ID,
                  CST_ID_L: vh.CST_ID_L,
                  CST_ID_R: vh.CST_ID_R);
                try
                {
                    string node_id = vh.NODE_ID;
                    string eq_id = vh.VEHICLE_ID;
                    string err_code = recive_str.ErrCode;
                    string err_desc = recive_str.ErrDescription;
                    ErrorStatus status = recive_str.ErrStatus;
                    scApp.LineService.ProcessAlarmReport(vh, err_code, status, err_desc);
                    ID_94_ALARM_RESPONSE send_str = new ID_94_ALARM_RESPONSE
                    {
                        ReplyCode = 0
                    };
                    WrapperMessage wrapper = new WrapperMessage
                    {
                        SeqNum = seq_num,
                        AlarmResp = send_str
                    };
                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, recive_str);
                    Boolean resp_cmp = vh.sendMessage(wrapper, true);
                    SCUtility.RecodeReportInfo(vh.VEHICLE_ID, seq_num, send_str, resp_cmp.ToString());
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception:");
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                }
            }
            #endregion ID_194 AlarmReport
            public DynamicMetaObject GetMetaObject(Expression parameter)
            {
                return new AspectWeaver(parameter, this);
            }
        }
        public class CommandProcessor
        {
            private ALINE line = null;
            VehicleService service;
            public CommandProcessor(VehicleService _service, ALINE _line)
            {
                service = _service;
                line = _line;
            }

            //public bool Move(string vhID, string destination)
            public (bool isSuccess, ACMD moveCmd) Move(string vhID, string destination)
            {
                bool is_success = false;
                ACMD cmd_obj = null;
                is_success = scApp.CMDBLL.doCreatCommand(vhID, out cmd_obj, cmd_type: E_CMD_TYPE.Move, destination: destination);
                //return scApp.CMDBLL.doCreatCommand(vhID, cmd_type: E_CMD_TYPE.Move, destination: destination);
                return (is_success, cmd_obj);
            }
            public bool MoveToCharge(string vhID, string destination)
            {
                return scApp.CMDBLL.doCreatCommand(vhID, cmd_type: E_CMD_TYPE.Move_Charger, destination: destination);
            }
            public bool Load(string vhID, string cstID, string source, string sourcePortID)
            {
                return scApp.CMDBLL.doCreatCommand(vhID, carrier_id: cstID, cmd_type: E_CMD_TYPE.Load, source: source,
                                                   sourcePort: sourcePortID);
            }
            public bool Unload(string vhID, string cstID, string destination, string destinationPortID)
            {
                return scApp.CMDBLL.doCreatCommand(vhID, carrier_id: cstID, cmd_type: E_CMD_TYPE.Unload, destination: destination,
                                                   destinationPort: destinationPortID);
            }
            public bool Loadunload(string vhID, string cstID, string source, string destination, string sourcePortID, string destinationPortID)
            {
                return scApp.CMDBLL.doCreatCommand(vhID, carrier_id: cstID, cmd_type: E_CMD_TYPE.LoadUnload, source: source, destination: destination,
                                                   sourcePort: sourcePortID, destinationPort: destinationPortID);
            }


            public (bool isSuccess, string transferID) CommandInitialFail(ACMD initial_cmd)
            {
                bool is_success = true;
                string finish_fransfer_cmd_id = "";
                try
                {
                    if (initial_cmd != null)
                    {
                        string vh_id = initial_cmd.VH_ID;
                        string initial_cmd_id = initial_cmd.ID;
                        finish_fransfer_cmd_id = initial_cmd.TRANSFER_ID;
                        is_success = is_success && scApp.CMDBLL.updateCommand_OHTC_StatusToFinish(initial_cmd_id, CompleteStatus.CommandInitailFail);
                        bool isTransfer = !SCUtility.isEmpty(finish_fransfer_cmd_id);
                        if (isTransfer)
                        {
                            scApp.CarrierBLL.db.updateState(initial_cmd.CARRIER_ID, E_CARRIER_STATE.MoveError);
                            scApp.TransferService.FinishTransferCommand(finish_fransfer_cmd_id, CompleteStatus.CommandInitailFail);
                        }
                    }
                }
                catch (Exception ex)
                {
                    is_success = false;
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       Details: $"process commamd initial fail ,has exception happend.cmd id:{initial_cmd?.ID}");
                }
                return (is_success, finish_fransfer_cmd_id);
            }
            public (bool isSuccess, string transferID) Finish(string finish_cmd_id, CompleteStatus completeStatus, int totalTravelDis = 0)
            {
                ACMD cmd = scApp.CMDBLL.getExcuteCMD_OHTCByCmdID(finish_cmd_id);
                string finish_fransfer_cmd_id = "";
                string vh_id = "";
                //確認是否為尚未結束的Task
                bool is_success = true;
                if (cmd != null)
                {
                    carrierStateCheck(cmd, completeStatus);
                    vh_id = cmd.VH_ID;
                    finish_fransfer_cmd_id = cmd.TRANSFER_ID;
                    is_success = is_success && scApp.CMDBLL.updateCommand_OHTC_StatusToFinish(finish_cmd_id, completeStatus);
                    //再確認是否為Transfer command
                    //是的話
                    //1.要上報MCS
                    //2.要將該Transfer改為結束
                    bool isTransfer = !SCUtility.isEmpty(finish_fransfer_cmd_id);
                    if (isTransfer)
                    {
                        //if (scApp.PortStationBLL.OperateCatch.IsEqPort(scApp.EqptBLL, cmd.DESTINATION_PORT))
                        //scApp.ReportBLL.newReportUnloadComplete(cmd.TRANSFER_ID, null);

                        is_success = is_success && scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(finish_fransfer_cmd_id, completeStatus);
                        is_success = is_success && scApp.ReportBLL.ReportTransferResult2MCS(finish_fransfer_cmd_id, completeStatus);
                        is_success = is_success && scApp.SysExcuteQualityBLL.SysExecQityfinish(finish_fransfer_cmd_id, completeStatus, totalTravelDis);
                        if (completeStatus == CompleteStatus.IdmisMatch ||
                            completeStatus == CompleteStatus.IdreadFailed)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"start process:[{completeStatus}] script. finish cmd id:{finish_cmd_id}...",
                               VehicleID: vh_id);
                            var result = scApp.TransferService.processIDReadFailAndMismatch(cmd.CARRIER_ID, completeStatus);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"process:[{completeStatus}] script success:[{result.isSuccess}], result:[{result.result}]." +
                                     $" finish cmd id:{finish_cmd_id}",
                               VehicleID: vh_id);

                        }
                    }
                }
                return (is_success, finish_fransfer_cmd_id);
            }

            /// <summary>
            /// 在命令132上報結束時，確認一下當下Carrier的狀態，
            /// 如果不是在Installed的狀態時，一律當作是MoveError
            /// </summary>
            /// <param name="cmd"></param>
            /// <param name="completeStatus"></param>
            private void carrierStateCheck(ACMD cmd, CompleteStatus completeStatus)
            {
                if (cmd == null) return;
                string carrier_id = cmd.CARRIER_ID;
                try
                {
                    bool is_carrier_trnasfer = !SCUtility.isEmpty(carrier_id);
                    if (is_carrier_trnasfer)
                    {
                        switch (completeStatus)
                        {
                            case CompleteStatus.Abort:
                            case CompleteStatus.Cancel:
                            case CompleteStatus.InterlockError:
                            case CompleteStatus.VehicleAbort:
                                ACARRIER transfer_carrier = scApp.CarrierBLL.db.getCarrier(carrier_id);
                                if (transfer_carrier != null &&
                                    transfer_carrier.STATE != E_CARRIER_STATE.Installed)
                                {
                                    scApp.CarrierBLL.db.updateState(carrier_id, E_CARRIER_STATE.MoveError);
                                }
                                break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       Details: $"process carrier state fail. carrier id:{carrier_id}");
                }
            }

            private long cmd_SyncPoint = 0;
            //public void Scan()
            public void Scan_backup()
            {
                if (System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 1) == 0)
                {
                    try
                    {
                        if (scApp.getEQObjCacheManager().getLine().ServiceMode
                            != SCAppConstants.AppServiceMode.Active)
                            return;
                        List<ACMD> CMD_OHTC_Queues = scApp.CMDBLL.loadCMD_OHTCMDStatusIsQueue();
                        if (CMD_OHTC_Queues == null || CMD_OHTC_Queues.Count == 0)
                            return;
                        foreach (ACMD cmd in CMD_OHTC_Queues)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(CMDBLL), Device: string.Empty,
                               Data: $"Start process command ,id:{SCUtility.Trim(cmd.ID)},vh id:{SCUtility.Trim(cmd.VH_ID)},from:{SCUtility.Trim(cmd.SOURCE)},to:{SCUtility.Trim(cmd.DESTINATION)}");

                            string vehicle_id = cmd.VH_ID.Trim();
                            AVEHICLE assignVH = scApp.VehicleBLL.cache.getVehicle(vehicle_id);
                            if (!assignVH.isTcpIpConnect ||
                                !scApp.CMDBLL.canSendCmd(vehicle_id)) //todo kevin 需要確認是否要再判斷是否有命令的執行?
                            {
                                continue;
                            }

                            bool is_success = service.Send.Command(assignVH, cmd);
                            if (!is_success)
                            {
                                //Finish(cmd.ID, CompleteStatus.Cancel);
                                //Finish(cmd.ID, CompleteStatus.VehicleAbort);
                                CommandInitialFail(cmd);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Exection:");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 0);
                    }
                }
            }
            public void Scan()
            {
                if (System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 1) == 0)
                {
                    try
                    {
                        if (scApp.getEQObjCacheManager().getLine().ServiceMode
                            != SCAppConstants.AppServiceMode.Active)
                            return;
                        //List<ACMD> CMD_OHTC_Queues = scApp.CMDBLL.loadCMD_OHTCMDStatusIsQueue();
                        List<ACMD> unfinish_cmd = scApp.CMDBLL.loadUnfinishCmd();
                        line.CurrentExcuteCommand = unfinish_cmd;
                        if (unfinish_cmd == null || unfinish_cmd.Count == 0)
                            return;
                        List<ACMD> CMD_OHTC_Queues = unfinish_cmd.Where(cmd => cmd.CMD_STATUS == E_CMD_STATUS.Queue).ToList();
                        if (CMD_OHTC_Queues == null || CMD_OHTC_Queues.Count == 0)
                            return;
                        foreach (ACMD cmd in CMD_OHTC_Queues)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(CMDBLL), Device: string.Empty,
                               Data: $"Start process command ,id:{SCUtility.Trim(cmd.ID)},vh id:{SCUtility.Trim(cmd.VH_ID)},from:{SCUtility.Trim(cmd.SOURCE)},to:{SCUtility.Trim(cmd.DESTINATION)}");

                            string vehicle_id = cmd.VH_ID.Trim();
                            AVEHICLE assignVH = scApp.VehicleBLL.cache.getVehicle(vehicle_id);
                            if (!assignVH.isTcpIpConnect ||
                                !scApp.CMDBLL.canSendCmd(vehicle_id)) //todo kevin 需要確認是否要再判斷是否有命令的執行?
                            {
                                continue;
                            }

                            bool is_success = service.Send.Command(assignVH, cmd);
                            if (!is_success)
                            {
                                //Finish(cmd.ID, CompleteStatus.Cancel);
                                //Finish(cmd.ID, CompleteStatus.VehicleAbort);
                                CommandInitialFail(cmd);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Exection:");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 0);
                    }
                }
            }

            public void Scan_V2()
            {
                if (System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 1) == 0)
                {
                    try
                    {
                        if (scApp.getEQObjCacheManager().getLine().ServiceMode
                            != SCAppConstants.AppServiceMode.Active)
                            return;
                        List<ACMD> CMD_OHTC_Queues = scApp.CMDBLL.loadCMD_OHTCMDStatusIsQueue();
                        if (CMD_OHTC_Queues == null || CMD_OHTC_Queues.Count == 0)
                            return;
                        foreach (ACMD cmd in CMD_OHTC_Queues)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(CMDBLL), Device: string.Empty,
                               Data: $"Start process command ,id:{SCUtility.Trim(cmd.ID)},vh id:{SCUtility.Trim(cmd.VH_ID)},from:{SCUtility.Trim(cmd.SOURCE)},to:{SCUtility.Trim(cmd.DESTINATION)}");

                            string vehicle_id = cmd.VH_ID.Trim();
                            AVEHICLE assignVH = scApp.VehicleBLL.cache.getVehicle(vehicle_id);
                            if (!assignVH.isTcpIpConnect ||
                                !scApp.CMDBLL.canSendCmd(vehicle_id)) //todo kevin 需要確認是否要再判斷是否有命令的執行?
                            {
                                continue;
                            }
                            //1.如果目的地是AGV Station，則需要去看目的地的Port是否已經準備好了
                            //  如果還沒好，則需要先把這台AGV派至該命令的EQ端等待接收命令
                            bool is_agv_station_target_port = cmd.IsTargetPortAGVStation(scApp.PortStationBLL, scApp.EqptBLL);
                            if (is_agv_station_target_port)
                            {
                                var target_port_agv_station = cmd.getTragetPortEQ(scApp.PortStationBLL, scApp.EqptBLL) as AGVStation;
                                if (!target_port_agv_station.IsReadyDoubleUnload)
                                {
                                    preMoveToSourcePort(assignVH, cmd);
                                    continue;
                                }
                            }

                            bool is_success = service.Send.Command(assignVH, cmd);
                            if (!is_success)
                            {
                                //Finish(cmd.ID, CompleteStatus.Cancel);
                                //Finish(cmd.ID, CompleteStatus.VehicleAbort);
                                CommandInitialFail(cmd);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Exection:");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref cmd_SyncPoint, 0);
                    }
                }
            }

            /// <summary>
            /// 確認vh是否已經在準備要他去的Address上，如果還沒且
            /// </summary>
            /// <param name="assignVH"></param>
            /// <param name="cmd"></param>
            private void preMoveToSourcePort(AVEHICLE assignVH, ACMD cmd)
            {
                string vh_current_adr = assignVH.CUR_ADR_ID;
                string cmd_source_adr = cmd.SOURCE;
                //如果一樣 則代表已經在待命位上
                if (SCUtility.isMatche(vh_current_adr, cmd_source_adr)) return;
                var creat_result = service.Command.Move(assignVH.VEHICLE_ID, cmd.SOURCE);
                if (creat_result.isSuccess)
                {
                    bool is_success = service.Send.Command(assignVH, creat_result.moveCmd);
                    if (!is_success)
                    {
                        CommandInitialFail(cmd);
                    }
                }
            }
        }
        public class AvoidProcessor
        {
            VehicleService service;
            public AvoidProcessor(VehicleService _service)
            {
                service = _service;
            }
            public const string VehicleVirtualSymbol = "virtual";
            public void tryNotifyVhAvoid(string requestVhID, string reservedVhID)
            {
                if (System.Threading.Interlocked.Exchange(ref syncPoint_NotifyVhAvoid, 1) == 0)
                {
                    try
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Try to notify vh avoid...,requestVh:{requestVhID} reservedVh:{reservedVhID}",
                           VehicleID: requestVhID);
                        AVEHICLE reserved_vh = scApp.VehicleBLL.cache.getVehicle(reservedVhID);
                        AVEHICLE request_vh = scApp.VehicleBLL.cache.getVehicle(requestVhID);

                        //先確認是否可以進行趕車的確認，如果當前Reserved的車子狀態是
                        //1.發出Error的
                        //2.正在進行長充電的
                        //則要將來要得車子進行路徑Override
                        var check_can_creat_avoid_command = canCreatAvoidCommand(reserved_vh);
                        //if (canCreatAvoidCommand(reserved_vh))
                        if (check_can_creat_avoid_command.is_can)
                        {
                            string reserved_vh_current_section = reserved_vh.CUR_SEC_ID;

                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"start search section:{reserved_vh_current_section}",
                               VehicleID: requestVhID);

                            var findResult = findNotConflictSectionAndAvoidAddressNew(request_vh, reserved_vh, false);
                            if (!findResult.isFind)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find not conflict section fail. reserved section:{reserved_vh_current_section},",
                                   VehicleID: requestVhID);
                                return;
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"find not conflict section:{findResult.notConflictSection.SEC_ID}.avoid address:{findResult.avoidAdr}",
                                   VehicleID: requestVhID);
                            }

                            string avoid_address = findResult.avoidAdr;


                            if (!SCUtility.isEmpty(avoid_address))
                            {
                                //bool is_success = scApp.CMDBLL.doCreatCommand(reserved_vh.VEHICLE_ID, string.Empty, string.Empty,
                                //                                    E_CMD_TYPE.Move,
                                //                                    string.Empty,
                                //                                    avoid_address);
                                bool is_success = service.Command.Move(reserved_vh.VEHICLE_ID, avoid_address).isSuccess;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}, is success :{is_success}.",
                                   VehicleID: requestVhID);
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}, fail.",
                                   VehicleID: requestVhID);
                            }
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"Try to notify vh avoid,requestVh:{requestVhID} reservedVh:{reservedVhID}," +
                                     $"but reservedVh:{reservedVhID} status not ready." +
                                     $" isTcpIpConnect:{reserved_vh.isTcpIpConnect}" +
                                     $" MODE_STATUS:{reserved_vh.MODE_STATUS}" +
                                     $" ACT_STATUS:{reserved_vh.ACT_STATUS}" +
                                     $" result:{check_can_creat_avoid_command.result}",
                               VehicleID: requestVhID);

                            switch (check_can_creat_avoid_command.result)
                            {
                                case CAN_NOT_AVOID_RESULT.VehicleInError:
                                case CAN_NOT_AVOID_RESULT.VehicleInLongCharge:
                                    if (request_vh.IsReservePause)
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"Try to notify vh avoid fail,start over request of vh..., because reserved of status:{check_can_creat_avoid_command.result}," +
                                                 $" requestVh:{requestVhID} reservedVh:{reservedVhID}.",
                                           VehicleID: requestVhID);
                                        //todo 要實作要求避車的功能，在擋住路的
                                        scApp.VehicleService.Avoid.trydoAvoidCommandToVh(request_vh, reserved_vh);
                                    }
                                    break;
                                default:
                                    if (request_vh.IsReservePause && reserved_vh.IsReservePause)
                                    {
                                        //如果兩台車都已經Reserve Pause了，就不再透過這邊進行避車
                                        //而是透過Deadlock的Timer來解除。
                                    }
                                    else if (request_vh.IsReservePause)
                                    {
                                        if (IsBlockEachOrther(reserved_vh, request_vh))
                                        {
                                            if (scApp.VehicleService.Avoid.trydoAvoidCommandToVh(request_vh, reserved_vh))
                                            {
                                                SpinWait.SpinUntil(() => false, 15000);
                                            }
                                        }
                                        else
                                        {
                                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                               Data: $"request vh:{requestVhID} with reserved vh:{reservedVhID} of can't reserve info not same,don't excute Avoid",
                                               VehicleID: requestVhID);
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: ex,
                           Details: $"excute tryNotifyVhAvoid has exception happend.requestVh:{requestVhID},reservedVh:{reservedVhID}");
                    }
                    finally
                    {
                        System.Threading.Interlocked.Exchange(ref syncPoint_NotifyVhAvoid, 0);
                    }
                }
            }

            private bool IsBlockEachOrther(AVEHICLE reserved_vh, AVEHICLE request_vh)
            {
                return (reserved_vh.CanNotReserveInfo != null && request_vh.CanNotReserveInfo != null) &&
                        SCUtility.isMatche(reserved_vh.CanNotReserveInfo.ReservedVhID, request_vh.VEHICLE_ID) &&
                        SCUtility.isMatche(request_vh.CanNotReserveInfo.ReservedVhID, reserved_vh.VEHICLE_ID);
            }

            public bool trydoAvoidCommandToVh(AVEHICLE avoidVh, AVEHICLE willPassVh)
            {
                var find_avoid_result = findNotConflictSectionAndAvoidAddressNew(willPassVh, avoidVh, true);
                string blocked_section = avoidVh.CanNotReserveInfo.ReservedSectionID;
                string blocked_vh_id = avoidVh.CanNotReserveInfo.ReservedVhID;
                if (find_avoid_result.isFind)
                {
                    avoidVh.VhAvoidInfo = null;
                    var avoid_request_result = service.Send.Avoid(avoidVh.VEHICLE_ID, find_avoid_result.avoidAdr);
                    if (avoid_request_result.is_success)
                    {
                        avoidVh.VhAvoidInfo = new AVEHICLE.AvoidInfo(blocked_section, blocked_vh_id);
                    }
                    return avoid_request_result.is_success;
                }
                else
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"No find the can avoid address. avoid vh:{avoidVh.VEHICLE_ID} current adr:{avoidVh.CUR_ADR_ID}," +
                             $"will pass vh:{willPassVh.VEHICLE_ID} current adr:{willPassVh.CUR_ADR_ID}",
                       VehicleID: avoidVh.VEHICLE_ID,
                       CST_ID_L: avoidVh.CST_ID_L,
                       CST_ID_R: avoidVh.CST_ID_R);
                    return false;
                }
            }
            private long syncPoint_NotifyVhAvoid = 0;
            private enum CAN_NOT_AVOID_RESULT
            {
                Normal,
                VehicleInLongCharge,
                VehicleInError
            }
            private (bool is_can, CAN_NOT_AVOID_RESULT result) canCreatAvoidCommand(AVEHICLE reservedVh)
            {
                if (reservedVh.ACT_STATUS == VHActionStatus.NoCommand &&
                    reservedVh.IsOnCharge(scApp.AddressesBLL) &&
                    reservedVh.IsNeedToLongCharge())
                {
                    return (false, CAN_NOT_AVOID_RESULT.VehicleInLongCharge);
                }
                else if (reservedVh.IsError)
                {
                    return (false, CAN_NOT_AVOID_RESULT.VehicleInError);
                }
                else
                {
                    bool is_can = reservedVh.isTcpIpConnect &&
                           (reservedVh.MODE_STATUS == VHModeStatus.AutoRemote || reservedVh.MODE_STATUS == VHModeStatus.AutoCharging) &&
                           reservedVh.ACT_STATUS == VHActionStatus.NoCommand &&
                           !scApp.CMDBLL.isCMD_OHTCQueueByVh(reservedVh.VEHICLE_ID) &&
                           !scApp.CMDBLL.HasCMD_MCSInQueue();
                    return (is_can, CAN_NOT_AVOID_RESULT.Normal);
                }

            }
            private (bool isFind, ASECTION notConflictSection, string entryAdr, string avoidAdr) findNotConflictSectionAndAvoidAddressNew
                (AVEHICLE willPassVh, AVEHICLE findAvoidAdrOfVh, bool isDeadLock)
            {
                string will_pass_vh_cur_adr = willPassVh.CUR_ADR_ID;
                string find_avoid_vh_cur_adr = findAvoidAdrOfVh.CUR_ADR_ID;
                ASECTION find_avoid_vh_current_section = scApp.SectionBLL.cache.GetSection(findAvoidAdrOfVh.CUR_SEC_ID);
                //先找出哪個Address是距離即將到來的車子比較遠，即反方向
                string first_search_adr = findTheOppositeOfAddress(will_pass_vh_cur_adr, find_avoid_vh_current_section);

                (string next_address, ASECTION source_section) first_search_section_infos = (first_search_adr, find_avoid_vh_current_section);
                var searchResult = tryFindAvoidAddressByOneWay(willPassVh, findAvoidAdrOfVh, first_search_section_infos, false);
                if (!isDeadLock && !searchResult.isFind)
                {
                    string second_search_adr = SCUtility.isMatche(first_search_adr, find_avoid_vh_current_section.FROM_ADR_ID) ?
                        find_avoid_vh_current_section.TO_ADR_ID : find_avoid_vh_current_section.FROM_ADR_ID;

                    (string next_address, ASECTION source_section) second_search_section_infos = (second_search_adr, find_avoid_vh_current_section);
                    searchResult = tryFindAvoidAddressByOneWay(willPassVh, findAvoidAdrOfVh, second_search_section_infos, false);
                }
                return searchResult;
            }
            private string findTheOppositeOfAddress(string req_vh_cur_adr, ASECTION reserved_vh_current_section)
            {
                string opposite_address = "";
                int from_distance = 0;
                var from_adr_guide_result = scApp.GuideBLL.getGuideInfo(req_vh_cur_adr, reserved_vh_current_section.FROM_ADR_ID);
                if (from_adr_guide_result.isSuccess)
                {
                    from_distance = from_adr_guide_result.totalCost;
                }
                int to_distance = 0;
                var to_adr_guide_result = scApp.GuideBLL.getGuideInfo(req_vh_cur_adr, reserved_vh_current_section.TO_ADR_ID);
                if (to_adr_guide_result.isSuccess)
                {
                    to_distance = to_adr_guide_result.totalCost;
                }
                if (from_distance > to_distance)
                {
                    opposite_address = reserved_vh_current_section.FROM_ADR_ID;
                }
                else
                {
                    opposite_address = reserved_vh_current_section.TO_ADR_ID;
                }
                return opposite_address;
            }
            private (bool isFind, ASECTION notConflictSection, string entryAdr, string avoidAdr) tryFindAvoidAddressByOneWay
                (AVEHICLE willPassVh, AVEHICLE findAvoidAdrVh, (string next_address, ASECTION source_section) startSearchInfo, bool isForceCrossing)
            {
                int calculation_count = 0;
                int max_calculation_count = 20;

                List<(string next_address, ASECTION source_section)> next_search_infos =
                    new List<(string next_address, ASECTION source_section)>() { startSearchInfo };
                List<(string next_address, ASECTION source_section)> next_search_address_temp =
                    new List<(string, ASECTION)>();

                ASECTION not_conflict_section = null;
                string avoid_address = null;
                string orther_end_point = "";
                //string virtual_vh_id = "";
                List<string> virtual_vh_ids = new List<string>();

                try
                {
                    //在一開始的時候就先Set一台虛擬車在相同位置，防止找到鄰近的Address
                    var hlt_vh_obj = scApp.ReserveBLL.GetHltVehicle(findAvoidAdrVh.VEHICLE_ID);
                    string virtual_vh_id = $"{VehicleVirtualSymbol}_{findAvoidAdrVh.VEHICLE_ID}";
                    scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_id, "", hlt_vh_obj.X, hlt_vh_obj.Y, hlt_vh_obj.Angle, 0,
                        sensorDir: Mirle.Hlts.Utils.HltDirection.ForwardReverse,
                          forkDir: Mirle.Hlts.Utils.HltDirection.None);
                    virtual_vh_ids.Add(virtual_vh_id);
                    do
                    {
                        foreach (var search_info in next_search_infos.ToArray())
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"start search address:{search_info.next_address}",
                               VehicleID: willPassVh.VEHICLE_ID);
                            //next_search_address.Clear();
                            List<ASECTION> next_sections = scApp.SectionBLL.cache.GetSectionsByAddress(search_info.next_address);

                            //先把自己的Section移除
                            next_sections.Remove(search_info.source_section);
                            if (next_sections != null && next_sections.Count() > 0)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())}",
                                   VehicleID: willPassVh.VEHICLE_ID);
                                //過濾掉已經Disable的Segment
                                next_sections = next_sections.Where(sec => sec.IsActive(scApp.SegmentBLL)).ToList();
                                if (next_sections != null && next_sections.Count() > 0)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"next search section:{string.Join(",", next_sections.Select(sec => sec.SEC_ID).ToArray())} after filter not in active",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                }
                                else
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"search result is empty after filter not in active ,search adr:{search_info.next_address}",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                }
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"search result is empty,search adr:{search_info.next_address}",
                                   VehicleID: willPassVh.VEHICLE_ID);
                            }

                            //當找出兩段以上的Section時且他的Source為會與另一台vh前進路徑交錯的車，
                            //代表找到了叉路，因此要在入口加入一台虛擬車來幫助找避車路徑時確保不會卡住的點。
                            if (next_sections.Count >= 2 &&
                                hasCrossWithPredictSection(search_info.source_section.SEC_ID, willPassVh.WillPassSectionID))
                            //hasCrossWithPredictSection(search_info.source_section.SEC_ID, requestVh.PredictSections))
                            {
                                string virtual_vh_section_id = $"{virtual_vh_id}_{search_info.next_address}";
                                scApp.ReserveBLL.TryAddVehicleOrUpdate(virtual_vh_section_id, search_info.next_address);
                                virtual_vh_ids.Add(virtual_vh_section_id);
                                //scApp.ReserveBLL.ForceUpdateVehicle(virtual_vh_id, search_info.next_address);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"Add virtual in reserve system vh:{virtual_vh_section_id} in address id:{search_info.next_address}",
                                   VehicleID: findAvoidAdrVh.VEHICLE_ID);
                            }
                            foreach (ASECTION sec in next_sections)
                            {
                                //if (sec == search_info.source_section) continue;
                                orther_end_point = sec.GetOrtherEndPoint(search_info.next_address);
                                //如果跟目前找停車位的車子同一個點位時，代表找回到了原點，因此要把它濾掉。
                                if (SCUtility.isMatche(findAvoidAdrVh.CUR_ADR_ID, orther_end_point))
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} of orther end point:{orther_end_point} same with vh current address pass this section",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                    continue;
                                }
                                //if (!isForceCrossing)
                                //{
                                //if (requestVh.PredictSections != null && requestVh.PredictSections.Count() > 0)
                                if (willPassVh.WillPassSectionID != null && willPassVh.WillPassSectionID.Count() > 0)
                                {
                                    //if (requestVh.PredictSections.Contains(SCUtility.Trim(sec.SEC_ID)))
                                    if (willPassVh.WillPassSectionID.Contains(SCUtility.Trim(sec.SEC_ID)))
                                    {
                                        //next_search_address.Add(next_calculation_address);
                                        next_search_address_temp.Add((orther_end_point, sec));
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} is request_vh of will sections:{string.Join(",", willPassVh.WillPassSectionID)}.by pass it,continue find next address{orther_end_point}",
                                           VehicleID: willPassVh.VEHICLE_ID);
                                        continue;
                                    }
                                }
                                //}
                                //取得沒有相交的Section後，在確認是否該Orther end point是一個可以避車且不是R2000的任一端點，如果是的話就可以拿來作為一個避車點
                                AADDRESS orther_end_address = scApp.AddressesBLL.cache.GetAddress(orther_end_point);
                                //if (!orther_end_address.canAvoidVhecle)
                                if (!orther_end_address.canAvoidVehicle(scApp.SectionBLL))
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} of orther end point:{orther_end_point} is not can avoid address, continue find next address{orther_end_point}",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                    next_search_address_temp.Add((orther_end_point, sec));
                                    continue;
                                }
                                //找到以後嘗試去預約看看，確保該路徑是否還會干涉到該台VH
                                //還是有干涉到的話就繼續往下找
                                var reserve_check_result = scApp.ReserveBLL.TryAddReservedSection(findAvoidAdrVh.VEHICLE_ID, sec.SEC_ID, isAsk: true);
                                if (!reserve_check_result.OK &&
                                    !reserve_check_result.VehicleID.StartsWith(VehicleVirtualSymbol))
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} try to reserve fail,result:{reserve_check_result.Description}.",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                    if (isForceCrossing)
                                        next_search_address_temp.Add((orther_end_point, sec));
                                    else
                                    {
                                        AVEHICLE obstruct_vh = scApp.VehicleBLL.cache.getVehicle(reserve_check_result.VehicleID);
                                        if (obstruct_vh != null && !SCUtility.isMatche(sec.SEC_ID, obstruct_vh.CUR_SEC_ID))
                                        {
                                            next_search_address_temp.Add((orther_end_point, sec));
                                        }
                                    }
                                    continue;
                                }
                                else
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"sec id:{SCUtility.Trim(sec.SEC_ID)} try to reserve success,result:{reserve_check_result.Description}.",
                                       VehicleID: willPassVh.VEHICLE_ID);
                                }
                                not_conflict_section = sec;
                                avoid_address = orther_end_point;
                                return (true, not_conflict_section, search_info.next_address, avoid_address);
                            }
                        }
                        next_search_infos = next_search_address_temp.ToList();
                        next_search_address_temp.Clear();
                        calculation_count++;
                    } while (next_search_infos.Count() != 0 && calculation_count < max_calculation_count);
                }
                finally
                {
                    if (virtual_vh_ids != null && virtual_vh_ids.Count > 0)
                    {
                        foreach (string virtual_vh_id in virtual_vh_ids)
                        {
                            scApp.ReserveBLL.RemoveVehicle(virtual_vh_id);
                            //scApp.ReserveBLL.ForceUpdateVehicle(virtual_vh_id, 0, 0, 0);
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                               Data: $"remove virtual in reserve system vh:{virtual_vh_id} ",
                               VehicleID: findAvoidAdrVh.VEHICLE_ID);
                        }
                    }
                }
                return (false, null, null, null);
            }
            private bool hasCrossWithPredictSection(string checkSection, List<string> willPassSection)
            {
                if (willPassSection == null || willPassSection.Count() == 0) return false;
                if (SCUtility.isEmpty(checkSection)) return false;
                return willPassSection.Contains(SCUtility.Trim(checkSection));
            }
        }
        #region Event
        public event EventHandler<DeadLockEventArgs> DeadLockProcessFail;
        public void onDeadLockProcessFail(AVEHICLE vehicle1, AVEHICLE vehicle2)
        {
            SystemParameter.setAutoOverride(false);
            DeadLockProcessFail?.Invoke(this, new DeadLockEventArgs(vehicle1, vehicle2));
        }
        #endregion Event
        public void Start(SCApplication app)
        {
            scApp = app;
            Send = new SendProcessor(scApp);
            Receive = new ReceiveProcessor(this);
            Command = new CommandProcessor(this, scApp.getEQObjCacheManager().getLine());
            Avoid = new AvoidProcessor(this);
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();

            foreach (var vh in vhs)
            {
                vh.ConnectionStatusChange += (s1, e1) => PublishVhInfo(s1, ((AVEHICLE)s1).VEHICLE_ID);
                vh.ExcuteCommandStatusChange += (s1, e1) => PublishVhInfo(s1, e1);
                vh.VehicleStatusChange += (s1, e1) => PublishVhInfo(s1, e1);
                vh.VehiclePositionChange += (s1, e1) => PublishVhInfo(s1, e1);
                vh.ErrorStatusChange += (s1, e1) => Vh_ErrorStatusChange(s1, e1);


                vh.addEventHandler(nameof(VehicleService), nameof(vh.isTcpIpConnect), PublishVhInfo);
                vh.LocationChange += Vh_LocationChange;
                vh.SegmentChange += Vh_SegementChange;
                vh.LongTimeNoCommuncation += Vh_LongTimeNoCommuncation;
                vh.LongTimeInaction += Vh_LongTimeInaction;
                vh.LongTimeDisconnection += Vh_LongTimeDisconnection;
                vh.ModeStatusChange += Vh_ModeStatusChange;
                vh.SetupTimerAction();
            }
        }

        private void Vh_ExcuteCommandStatusChange(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #region Vh Event Handler
        private void Vh_ErrorStatusChange(object sender, VhStopSingle vhStopSingle)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            try
            {
                if (vhStopSingle == VhStopSingle.On)
                {
                    scApp.VehicleBLL.web.errorHappendNotify();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
            }
        }
        private void Vh_ModeStatusChange(object sender, VHModeStatus e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process vehicle mode change ,change to mode status:{e}",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);

                //如果他是變成manual mode的話，則需要報告無法服務的Alarm給 MCS
                if (e == VHModeStatus.AutoCharging ||
                    e == VHModeStatus.AutoLocal ||
                    e == VHModeStatus.AutoRemote)
                {
                    scApp.LineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrReset, $"vehicle cannot service");
                }
                else
                {
                    if (vh.IS_INSTALLED)
                        scApp.LineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
            }
        }
        private void Vh_LongTimeDisconnection(object sender, EventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            try
            {
                vh.stopVehicleTimer();
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"Process vehicle long time disconnection",
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);

                //要再上報Alamr Rerport給MCS
                if (vh.IS_INSTALLED)
                    scApp.LineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vh.VEHICLE_ID,
                   CST_ID_L: vh.CST_ID_L,
                   CST_ID_R: vh.CST_ID_R);
            }
        }
        private long syncPoint_ProcLongTimeInaction = 0;
        private void Vh_LongTimeInaction(object sender, string cmdID)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint_ProcLongTimeInaction, 1) == 0)
            {

                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"Process vehicle long time inaction, cmd id:{cmdID}",
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);

                    //當發生命令執行過久之後要將該筆命令改成Abormal end，如果該筆命令是MCS的Command則需要將命令上報給MCS作為結束
                    Command.Finish(cmdID, CompleteStatus.LongTimeInaction);
                    //要再上報Alamr Rerport給MCS
                    scApp.LineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_LONG_TIME_INACTION, ErrorStatus.ErrSet, $"vehicle long time inaction, cmd id:{cmdID}");
                }
                catch (Exception ex)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: ex,
                       VehicleID: vh.VEHICLE_ID,
                       CST_ID_L: vh.CST_ID_L,
                       CST_ID_R: vh.CST_ID_R);
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint_ProcLongTimeInaction, 0);
                }
            }
        }
        private void Vh_LongTimeNoCommuncation(object sender, EventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            if (vh == null) return;
            //當發生很久沒有通訊的時候，就會發送143去進行狀態的詢問，確保Control還與Vehicle連線著
            bool is_success = Send.StatusRequest(vh.VEHICLE_ID);
            if (!is_success)
            {

            }
        }
        private void Vh_LocationChange(object sender, LocationChangeEventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            ASECTION leave_section = scApp.SectionBLL.cache.GetSection(e.LeaveSection);
            ASECTION entry_section = scApp.SectionBLL.cache.GetSection(e.EntrySection);
            entry_section?.Entry(vh.VEHICLE_ID);
            leave_section?.Leave(vh.VEHICLE_ID);
            if (leave_section != null)
            {
                scApp.ReserveBLL.RemoveManyReservedSectionsByVIDSID(vh.VEHICLE_ID, leave_section.SEC_ID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh:{vh.VEHICLE_ID} leave section {entry_section.SEC_ID},remove reserved.",
                   VehicleID: vh.VEHICLE_ID);
            }
            scApp.VehicleBLL.cache.removeAlreadyPassedSection(vh.VEHICLE_ID, e.LeaveSection);

            //如果在進入該Section後，還有在該Section之前的Section沒有清掉的，就把它全部釋放
            if (entry_section != null)
            {
                List<string> current_resreve_section = scApp.ReserveBLL.loadCurrentReserveSections(vh.VEHICLE_ID);
                int current_section_index_in_reserve_section = current_resreve_section.IndexOf(entry_section.SEC_ID);
                if (current_section_index_in_reserve_section > 0)//代表不是在第一個
                {
                    for (int i = 0; i < current_section_index_in_reserve_section; i++)
                    {
                        scApp.ReserveBLL.RemoveManyReservedSectionsByVIDSID(vh.VEHICLE_ID, current_resreve_section[i]);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"vh:{vh.VEHICLE_ID} force release omission section {current_resreve_section[i]},remove reserved.",
                           VehicleID: vh.VEHICLE_ID);
                    }
                }
            }
        }
        private void Vh_SegementChange(object sender, SegmentChangeEventArgs e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            ASEGMENT leave_section = scApp.SegmentBLL.cache.GetSegment(e.LeaveSegment);
            ASEGMENT entry_section = scApp.SegmentBLL.cache.GetSegment(e.EntrySegment);
            //if (leave_section != null && entry_section != null)
            //{
            //    AADDRESS release_adr = FindReleaseAddress(leave_section, entry_section);
            //    release_adr?.Release(vh.VEHICLE_ID);
            //}
        }
        private void PublishVhInfo(object sender, EventArgs e)
        {
            try
            {
                //string vh_id = e.PropertyValue as string;
                //AVEHICLE vh = scApp.VehicleBLL.getVehicleByID(vh_id);
                AVEHICLE vh = sender as AVEHICLE;
                if (sender == null) return;
                byte[] vh_Serialize = BLL.VehicleBLL.Convert2GPB_VehicleInfo(vh);
                RecoderVehicleObjInfoLog(vh.VEHICLE_ID, vh_Serialize);

                scApp.getNatsManager().PublishAsync
                    (string.Format(SCAppConstants.NATS_SUBJECT_VH_INFO_0, vh.VEHICLE_ID.Trim()), vh_Serialize);

                scApp.getRedisCacheManager().ListSetByIndexAsync
                    (SCAppConstants.REDIS_LIST_KEY_VEHICLES, vh.VEHICLE_ID, vh.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }
        private void PublishVhInfo(object sender, string vhID)
        {
            try
            {
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                if (sender == null) return;
                byte[] vh_Serialize = BLL.VehicleBLL.Convert2GPB_VehicleInfo(vh);
                RecoderVehicleObjInfoLog(vhID, vh_Serialize);

                scApp.getNatsManager().PublishAsync
                    (string.Format(SCAppConstants.NATS_SUBJECT_VH_INFO_0, vh.VEHICLE_ID.Trim()), vh_Serialize);

                scApp.getRedisCacheManager().ListSetByIndexAsync
                    (SCAppConstants.REDIS_LIST_KEY_VEHICLES, vh.VEHICLE_ID, vh.ToString());
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }
        private static void RecoderVehicleObjInfoLog(string vh_id, byte[] arrayByte)
        {
            string compressStr = SCUtility.CompressArrayByte(arrayByte);
            dynamic logEntry = new JObject();
            logEntry.RPT_TIME = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
            logEntry.OBJECT_ID = vh_id;
            logEntry.RAWDATA = compressStr;
            logEntry.Index = "ObjectHistoricalInfo";
            var json = logEntry.ToString(Newtonsoft.Json.Formatting.None);
            json = json.Replace("RPT_TIME", "@timestamp");
            LogManager.GetLogger("ObjectHistoricalInfo").Info(json);
        }
        #endregion Vh Event Handler
        #region Send Message To Vehicle
        #region Data syne
        public bool HostBasicVersionReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            ID_101_HOST_BASIC_INFO_VERSION_RESPONSE receive_gpp = null;
            ID_1_HOST_BASIC_INFO_VERSION_REP sned_gpp = new ID_1_HOST_BASIC_INFO_VERSION_REP()
            {
                DataDateTimeYear = "2018",
                DataDateTimeMonth = "10",
                DataDateTimeDay = "25",
                DataDateTimeHour = "15",
                DataDateTimeMinute = "22",
                DataDateTimeSecond = "50",
                CurrentTimeYear = crtTime.Year.ToString(),
                CurrentTimeMonth = crtTime.Month.ToString(),
                CurrentTimeDay = crtTime.Day.ToString(),
                CurrentTimeHour = crtTime.Hour.ToString(),
                CurrentTimeMinute = crtTime.Minute.ToString(),
                CurrentTimeSecond = crtTime.Second.ToString()
            };
            isSuccess = vh.send_Str1(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool BasicInfoReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            ID_111_BASIC_INFO_RESPONSE receive_gpp = null;
            int travel_base_data_count = 1;
            int section_data_count = 0;
            int address_data_coune = 0;
            int scale_base_data_count = 1;
            int control_data_count = 1;
            int guide_base_data_count = 1;
            section_data_count = scApp.DataSyncBLL.getCount_ReleaseVSections();
            address_data_coune = scApp.MapBLL.getCount_AddressCount();
            ID_11_BASIC_INFO_REP sned_gpp = new ID_11_BASIC_INFO_REP()
            {
                TravelBasicDataCount = travel_base_data_count,
                SectionDataCount = section_data_count,
                AddressDataCount = address_data_coune,
                ScaleDataCount = scale_base_data_count,
                ContrlDataCount = control_data_count,
                GuideDataCount = guide_base_data_count
            };
            isSuccess = vh.send_S11(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool TavellingDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            DateTime crtTime = DateTime.Now;
            AVEHICLE_CONTROL_100 data = scApp.DataSyncBLL.getReleaseVehicleControlData_100(vh_id);

            ID_113_TAVELLING_DATA_RESPONSE receive_gpp = null;
            ID_13_TAVELLING_DATA_REP sned_gpp = new ID_13_TAVELLING_DATA_REP()
            {
                Resolution = (UInt32)data.TRAVEL_RESOLUTION,
                StartStopSpd = (UInt32)data.TRAVEL_START_STOP_SPEED,
                MaxSpeed = (UInt32)data.TRAVEL_MAX_SPD,
                AccelTime = (UInt32)data.TRAVEL_ACCEL_DECCEL_TIME,
                SCurveRate = (UInt16)data.TRAVEL_S_CURVE_RATE,
                OriginDir = (UInt16)data.TRAVEL_HOME_DIR,
                OriginSpd = (UInt32)data.TRAVEL_HOME_SPD,
                BeaemSpd = (UInt32)data.TRAVEL_KEEP_DIS_SPD,
                ManualHSpd = (UInt32)data.TRAVEL_MANUAL_HIGH_SPD,
                ManualLSpd = (UInt32)data.TRAVEL_MANUAL_LOW_SPD,
                TeachingSpd = (UInt32)data.TRAVEL_TEACHING_SPD,
                RotateDir = (UInt16)data.TRAVEL_TRAVEL_DIR,
                EncoderPole = (UInt16)data.TRAVEL_ENCODER_POLARITY,
                PositionCompensation = 0, //TODO 要填入正確的資料
                //FLimit = (UInt16)data.TRAVEL_F_DIR_LIMIT, //TODO 要填入正確的資料
                //RLimit = (UInt16)data.TRAVEL_R_DIR_LIMIT,
                KeepDistFar = (UInt32)data.TRAVEL_OBS_DETECT_LONG,
                KeepDistNear = (UInt32)data.TRAVEL_OBS_DETECT_SHORT,
            };
            isSuccess = vh.send_S13(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool AddressDataReport(string vh_id)
        {
            bool isSuccess = false;

            return isSuccess;
        }
        public bool ScaleDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            SCALE_BASE_DATA data = scApp.DataSyncBLL.getReleaseSCALE_BASE_DATA();

            ID_119_SCALE_DATA_RESPONSE receive_gpp = null;
            ID_19_SCALE_DATA_REP sned_gpp = new ID_19_SCALE_DATA_REP()
            {
                Resolution = (UInt32)data.RESOLUTION,
                InposArea = (UInt32)data.INPOSITION_AREA,
                InposStability = (UInt32)data.INPOSITION_STABLE_TIME,
                ScalePulse = (UInt32)data.TOTAL_SCALE_PULSE,
                ScaleOffset = (UInt32)data.SCALE_OFFSET,
                ScaleReset = (UInt32)data.SCALE_RESE_DIST,
                ReadDir = (UInt16)data.READ_DIR

            };
            isSuccess = vh.send_S19(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool ControlDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);

            CONTROL_DATA data = scApp.DataSyncBLL.getReleaseCONTROL_DATA();
            string rtnMsg = string.Empty;
            ID_121_CONTROL_DATA_RESPONSE receive_gpp;
            ID_21_CONTROL_DATA_REP sned_gpp = new ID_21_CONTROL_DATA_REP()
            {
                TimeoutT1 = (UInt32)data.T1,
                TimeoutT2 = (UInt32)data.T2,
                TimeoutT3 = (UInt32)data.T3,
                TimeoutT4 = (UInt32)data.T4,
                TimeoutT5 = (UInt32)data.T5,
                TimeoutT6 = (UInt32)data.T6,
                TimeoutT7 = (UInt32)data.T7,
                TimeoutT8 = (UInt32)data.T8,
                TimeoutBlock = (UInt32)data.BLOCK_REQ_TIME_OUT
            };
            isSuccess = vh.send_S21(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool GuideDataReport(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            AVEHICLE_CONTROL_100 data = scApp.DataSyncBLL.getReleaseVehicleControlData_100(vh_id);
            ID_123_GUIDE_DATA_RESPONSE receive_gpp;
            ID_23_GUIDE_DATA_REP sned_gpp = new ID_23_GUIDE_DATA_REP()
            {
                StartStopSpd = (UInt32)data.GUIDE_START_STOP_SPEED,
                MaxSpeed = (UInt32)data.GUIDE_MAX_SPD,
                AccelTime = (UInt32)data.GUIDE_ACCEL_DECCEL_TIME,
                SCurveRate = (UInt16)data.GUIDE_S_CURVE_RATE,
                NormalSpd = (UInt32)data.GUIDE_RUN_SPD,
                ManualHSpd = (UInt32)data.GUIDE_MANUAL_HIGH_SPD,
                ManualLSpd = (UInt32)data.GUIDE_MANUAL_LOW_SPD,
                LFLockPos = (UInt32)data.GUIDE_LF_LOCK_POSITION,
                LBLockPos = (UInt32)data.GUIDE_LB_LOCK_POSITION,
                RFLockPos = (UInt32)data.GUIDE_RF_LOCK_POSITION,
                RBLockPos = (UInt32)data.GUIDE_RB_LOCK_POSITION,
                ChangeStabilityTime = (UInt32)data.GUIDE_CHG_STABLE_TIME,
            };
            isSuccess = vh.send_S23(sned_gpp, out receive_gpp);
            isSuccess = isSuccess && receive_gpp.ReplyCode == 0;
            return isSuccess;
        }
        public bool doDataSysc(string vh_id)
        {
            bool isSyscCmp = false;
            DateTime ohtDataVersion = new DateTime(2017, 03, 27, 10, 30, 00);
            if (BasicInfoReport(vh_id) &&
                TavellingDataReport(vh_id) &&
                AddressDataReport(vh_id) &&
                ScaleDataReport(vh_id) &&
                ControlDataReport(vh_id) &&
                GuideDataReport(vh_id))
            {
                isSyscCmp = true;
            }
            return isSyscCmp;
        }
        public bool IndividualUploadRequest(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_161_INDIVIDUAL_UPLOAD_RESPONSE receive_gpp;
            ID_61_INDIVIDUAL_UPLOAD_REQ sned_gpp = new ID_61_INDIVIDUAL_UPLOAD_REQ()
            {

            };
            isSuccess = vh.send_S61(sned_gpp, out receive_gpp);
            //TODO Set info 2 DB
            if (isSuccess)
            {

            }
            return isSuccess;
        }
        public bool IndividualChangeRequest(string vh_id)
        {
            bool isSuccess = false;
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vh_id);
            ID_163_INDIVIDUAL_CHANGE_RESPONSE receive_gpp;
            ID_63_INDIVIDUAL_CHANGE_REQ sned_gpp = new ID_63_INDIVIDUAL_CHANGE_REQ()
            {
                OffsetGuideFL = 1,
                OffsetGuideRL = 2,
                OffsetGuideFR = 3,
                OffsetGuideRR = 4
            };
            isSuccess = vh.send_S63(sned_gpp, out receive_gpp);
            return isSuccess;
        }
        #endregion Data syne
        private (bool isSuccess, int total_code,
            List<string> guide_start_to_from_segment_ids, List<string> guide_start_to_from_section_ids, List<string> guide_start_to_from_address_ids,
            List<string> guide_to_dest_segment_ids, List<string> guide_to_dest_section_ids, List<string> guide_to_dest_address_ids)
            FindGuideInfo(string vh_current_address, string source_adr, string dest_adr, CommandActionType active_type, bool has_carray = false, List<string> byPassSectionIDs = null)
        {
            bool isSuccess = false;
            List<string> guide_start_to_from_segment_ids = null;
            List<string> guide_start_to_from_section_ids = null;
            List<string> guide_start_to_from_address_ids = null;
            List<string> guide_to_dest_segment_ids = null;
            List<string> guide_to_dest_section_ids = null;
            List<string> guide_to_dest_address_ids = null;
            int total_cost = 0;
            //1.取得行走路徑的詳細資料
            switch (active_type)
            {
                case CommandActionType.Loadunload:
                    if (!SCUtility.isMatche(vh_current_address, source_adr))
                    {
                        (isSuccess, guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, source_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = true;//如果相同 代表是在同一個點上
                    }
                    if (isSuccess && !SCUtility.isMatche(source_adr, dest_adr))
                    {
                        (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(source_adr, dest_adr, null);
                    }
                    break;
                case CommandActionType.Load:
                    if (!SCUtility.isMatche(vh_current_address, source_adr))
                    {
                        (isSuccess, guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, source_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = true; //如果相同 代表是在同一個點上
                    }
                    break;
                case CommandActionType.Unload:
                    if (!SCUtility.isMatche(vh_current_address, dest_adr))
                    {
                        (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, dest_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = true;//如果相同 代表是在同一個點上
                    }
                    break;
                case CommandActionType.Move:
                case CommandActionType.Movetocharger:
                    if (!SCUtility.isMatche(vh_current_address, dest_adr))
                    {
                        (isSuccess, guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids, total_cost)
                            = scApp.GuideBLL.getGuideInfo(vh_current_address, dest_adr, byPassSectionIDs);
                    }
                    else
                    {
                        isSuccess = false;
                    }
                    break;
            }
            return (isSuccess, total_cost,
                    guide_start_to_from_segment_ids, guide_start_to_from_section_ids, guide_start_to_from_address_ids,
                    guide_to_dest_segment_ids, guide_to_dest_section_ids, guide_to_dest_address_ids);
        }


        #endregion Send Message To Vehicle
        #region Vh connection / disconnention
        [ClassAOPAspect]
        public void Connection(BCFApplication bcfApp, AVEHICLE vh)
        {
            vh.isTcpIpConnect = true;
            vh.startVehicleTimer();

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Connection ! Begin synchronize with vehicle...",
               VehicleID: vh.VEHICLE_ID,
               CST_ID_L: vh.CST_ID_L,
               CST_ID_R: vh.CST_ID_R);
            VehicleInfoSynchronize(vh.VEHICLE_ID);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Connection ! End synchronize with vehicle.",
               VehicleID: vh.VEHICLE_ID,
               CST_ID_L: vh.CST_ID_L,
               CST_ID_R: vh.CST_ID_R);


            SCUtility.RecodeConnectionInfo
                (vh.VEHICLE_ID,
                SCAppConstants.RecodeConnectionInfo_Type.Connection.ToString(),
                vh.getDisconnectionIntervalTime(bcfApp));
        }
        /// <summary>
        /// 與Vehicle進行資料同步。(通常使用剛與Vehicle連線時)
        /// </summary>
        /// <param name="vh_id"></param>
        private void VehicleInfoSynchronize(string vh_id)
        {
            /*與Vehicle進行狀態同步*/
            Send.StatusRequest(vh_id, true);
            /*要求Vehicle進行Alarm的Reset*/
            Send.AlarmReset(vh_id);
        }
        [ClassAOPAspect]
        public void Disconnection(BCFApplication bcfApp, AVEHICLE vh)
        {
            vh.isTcpIpConnect = false;

            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
               Data: "Disconnection !",
               VehicleID: vh.VEHICLE_ID,
               CST_ID_L: vh.CST_ID_L,
               CST_ID_R: vh.CST_ID_R);
            SCUtility.RecodeConnectionInfo
                (vh.VEHICLE_ID,
                SCAppConstants.RecodeConnectionInfo_Type.Disconnection.ToString(),
                vh.getConnectionIntervalTime(bcfApp));
        }
        #endregion Vh Connection / disconnention
        #region Vehicle Install/Remove
        public (bool isSuccess, string result) Install(string vhID)
        {
            try
            {
                AVEHICLE vh_vo = scApp.VehicleBLL.cache.getVehicle(vhID);
                if (!vh_vo.isTcpIpConnect)
                {
                    string message = $"vh:{vhID} current not connection, can't excute action:Install";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }
                ASECTION current_section = scApp.SectionBLL.cache.GetSection(vh_vo.CUR_SEC_ID);
                if (current_section == null)
                {
                    string message = $"vh:{vhID} current section:{SCUtility.Trim(vh_vo.CUR_SEC_ID, true)} is not exist, can't excute action:Install";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }

                var ReserveResult = scApp.ReserveBLL.askReserveSuccess(scApp.SectionBLL, vhID, vh_vo.CUR_SEC_ID, vh_vo.CUR_ADR_ID);
                if (!ReserveResult.isSuccess)
                {
                    string message = $"vh:{vhID} current section:{SCUtility.Trim(vh_vo.CUR_SEC_ID, true)} can't reserved," +
                                     $" can't excute action:Install";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }

                scApp.VehicleBLL.updataVehicleInstall(vhID);
                if (vh_vo.MODE_STATUS == VHModeStatus.Manual)
                {
                    scApp.LineService.ProcessAlarmReport(vh_vo, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrSet, $"vehicle cannot service");
                }
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                scApp.ReportBLL.newReportVehicleInstalled(vh_vo.Real_ID, reportqueues);
                scApp.ReportBLL.newSendMCSMessage(reportqueues);
                return (true, "");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vhID);
                return (false, "");
            }
        }
        public (bool isSuccess, string result) Remove(string vhID)
        {
            try
            {
                //1.確認該VH 是否可以進行Remove
                //  a.是否為斷線狀態
                //2.將該台VH 更新成Remove狀態
                //3.將位置的資訊清空。(包含Reserve的路段、紅綠燈、Block)
                //4.上報給MCS
                AVEHICLE vh_vo = scApp.VehicleBLL.cache.getVehicle(vhID);

                //測試期間，暫時不看是否已經連線中
                //因為會讓車子在連線狀態下跑CycleRun
                //此時車子會是連線狀態但要把它Remove
                if (vh_vo.isTcpIpConnect)
                {
                    string message = $"vh:{vhID} current is connection, can't excute action:remove";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: message,
                       VehicleID: vhID);
                    return (false, message);
                }
                scApp.VehicleBLL.updataVehicleRemove(vhID);
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh id:{vhID} remove success. start release reserved control...",
                   VehicleID: vhID);
                scApp.ReserveBLL.RemoveAllReservedSectionsByVehicleID(vh_vo.VEHICLE_ID);
                scApp.ReserveBLL.RemoveVehicle(vh_vo.VEHICLE_ID);
                scApp.LineService.ProcessAlarmReport(vh_vo, AlarmBLL.VEHICLE_CAN_NOT_SERVICE, ErrorStatus.ErrReset, $"vehicle cannot service");
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: $"vh id:{vhID} remove success. end release reserved control.",
                   VehicleID: vhID);
                List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                scApp.ReportBLL.newReportVehicleRemoved(vh_vo.Real_ID, reportqueues);
                scApp.ReportBLL.newSendMCSMessage(reportqueues);
                return (true, "");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Data: ex,
                   VehicleID: vhID);
                return (false, "");
            }
        }
        #endregion Vehicle Install/Remove
        #region Specially Control
        public bool changeVhStatusToAutoRemote(string vhID)
        {
            scApp.VehicleBLL.cache.updataVehicleMode(vhID, VHModeStatus.AutoRemote);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.onVehicleStatusChange();
            return true;
        }
        public bool changeVhStatusToAutoLocal(string vhID)
        {
            scApp.VehicleBLL.cache.updataVehicleMode(vhID, VHModeStatus.AutoLocal);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.onVehicleStatusChange();
            return true;
        }
        public bool changeVhStatusToAutoCharging(string vhID)
        {
            scApp.VehicleBLL.cache.updataVehicleMode(vhID, VHModeStatus.AutoCharging);
            AVEHICLE vh = scApp.getEQObjCacheManager().getVehicletByVHID(vhID);
            vh?.onVehicleStatusChange();
            return true;
        }
        public void PauseAllVehicleByNormalPause()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            foreach (var vh in vhs)
            {
                Send.Pause(vh.VEHICLE_ID, PauseEvent.Pause, PauseType.Normal);
            }
        }
        public void ResumeAllVehicleByNormalPause()
        {
            List<AVEHICLE> vhs = scApp.getEQObjCacheManager().getAllVehicle();
            foreach (var vh in vhs)
            {
                Send.Pause(vh.VEHICLE_ID, PauseEvent.Continue, PauseType.Normal);
            }
        }
        #endregion Specially Control
        #region RoadService
        public (bool isSuccess, ASEGMENT segment) doEnableDisableSegment(string segment_id, E_PORT_STATUS port_status)
        {
            ASEGMENT segment = null;
            try
            {
                //List<APORTSTATION> port_stations = scApp.MapBLL.loadAllPortBySegmentID(segment_id);

                using (TransactionScope tx = SCUtility.getTransactionScope())
                {
                    using (DBConnection_EF con = DBConnection_EF.GetUContext())
                    {

                        switch (port_status)
                        {
                            case E_PORT_STATUS.InService:
                                segment = scApp.GuideBLL.unbanRouteTwoDirect(segment_id);
                                scApp.SegmentBLL.cache.EnableSegment(segment_id);
                                break;
                            case E_PORT_STATUS.OutOfService:
                                segment = scApp.GuideBLL.banRouteTwoDirect(segment_id);
                                scApp.SegmentBLL.cache.DisableSegment(segment_id);
                                break;
                        }
                        //foreach (APORTSTATION port_station in port_stations)
                        //{
                        //    scApp.MapBLL.updatePortStatus(port_station.PORT_ID, port_status);
                        //}
                        tx.Complete();
                    }
                }
                //List<AMCSREPORTQUEUE> reportqueues = new List<AMCSREPORTQUEUE>();
                //foreach (APORTSTATION port_station in port_stations)
                //{
                //    switch (port_status)
                //    {
                //        case E_PORT_STATUS.InService:
                //            scApp.ReportBLL.ReportPortInServeice(port_station.PORT_ID, reportqueues);
                //            break;
                //        case E_PORT_STATUS.OutOfService:
                //            scApp.ReportBLL.ReportPortOutServeice(port_station.PORT_ID, reportqueues);
                //            break;
                //    }
                //}
                //scApp.ReportBLL.sendMCSMessageAsyn(reportqueues);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
            return (segment != null, segment);
        }
        #endregion RoadService

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new AspectWeaver(parameter, this);
        }

    }
}