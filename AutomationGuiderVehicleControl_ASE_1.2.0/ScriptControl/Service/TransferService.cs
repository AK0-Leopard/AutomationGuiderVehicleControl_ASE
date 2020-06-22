using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using static com.mirle.ibg3k0.sc.ALINE;
using static com.mirle.ibg3k0.sc.AVEHICLE;

namespace com.mirle.ibg3k0.sc.Service
{
    public class TransferService
    {
        NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private SCApplication scApp = null;
        private TransferBLL transferBLL = null;
        private CarrierBLL carrierBLL = null;
        private SysExcuteQualityBLL sysExcuteQualityBLL = null;
        private ReportBLL reportBLL = null;
        private LineBLL lineBLL = null;
        private CMDBLL cmdBLL = null;
        private ALINE line = null;
        public TransferService()
        {

        }
        public void start(SCApplication _app)
        {
            scApp = _app;
            reportBLL = _app.ReportBLL;
            lineBLL = _app.LineBLL;
            transferBLL = _app.TransferBLL;
            carrierBLL = _app.CarrierBLL;
            sysExcuteQualityBLL = _app.SysExcuteQualityBLL;
            cmdBLL = _app.CMDBLL;
            line = scApp.getEQObjCacheManager().getLine();

            line.addEventHandler(nameof(ConnectionInfoService), nameof(line.MCSCommandAutoAssign), PublishTransferInfo);


            initPublish(line);
        }
        private void initPublish(ALINE line)
        {
            PublishTransferInfo(line, null);
            //PublishOnlineCheckInfo(line, null);
            //PublishPingCheckInfo(line, null);
        }

        private void PublishTransferInfo(object sender, PropertyChangedEventArgs e)
        {
            try
            {
                ALINE line = sender as ALINE;
                if (sender == null) return;
                byte[] line_serialize = BLL.LineBLL.Convert2GPB_TransferInfo(line);
                scApp.getNatsManager().PublishAsync
                    (SCAppConstants.NATS_SUBJECT_TRANSFER, line_serialize);


                //TODO 要改用GPP傳送
                //var line_Serialize = ZeroFormatter.ZeroFormatterSerializer.Serialize(line);
                //scApp.getNatsManager().PublishAsync
                //    (string.Format(SCAppConstants.NATS_SUBJECT_LINE_INFO), line_Serialize);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }

        public bool Creat(ATRANSFER transfer)
        {
            try
            {
                var carrier_info = transfer.GetCarrierInfo();
                var sys_excute_quality_info = transfer.GetSysExcuteQuality(scApp.VehicleBLL);
                transferBLL.db.transfer.add(transfer);
                if (transfer.TRANSFERSTATE == E_TRAN_STATUS.Queue)
                {
                    carrierBLL.db.addOrUpdate(carrier_info);
                    sysExcuteQualityBLL.addSysExcuteQuality(sys_excute_quality_info);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return false;
            }
            return true;
        }

        public bool AbortOrCancel(string transferID, ProtocolFormat.OHTMessage.CancelActionType actType)
        {
            ATRANSFER mcs_cmd = scApp.CMDBLL.GetTransferByID(transferID);
            if (mcs_cmd == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                   Details: $"want to cancel/abort mcs cmd:{transferID},but cmd not exist.",
                   XID: transferID);
                return false;
            }
            bool is_success = true;
            switch (actType)
            {
                case ProtocolFormat.OHTMessage.CancelActionType.CmdCancel:
                    scApp.ReportBLL.newReportTransferCancelInitial(transferID, null);
                    if (mcs_cmd.TRANSFERSTATE == E_TRAN_STATUS.Queue)
                    {
                        scApp.CMDBLL.updateTransferCmd_TranStatus2Canceled(transferID);
                        scApp.ReportBLL.newReportTransferCancelCompleted(transferID, false, null);
                    }
                    //else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Initial && mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring)
                    else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Initial)
                    {
                        ACMD excute_cmd = scApp.CMDBLL.GetCommandByTransferCmdID(transferID);
                        bool has_cmd_excute = excute_cmd != null;
                        if (has_cmd_excute)
                        {
                            is_success = scApp.VehicleService.Send.Cancel(excute_cmd.VH_ID, excute_cmd.ID, ProtocolFormat.OHTMessage.CancelActionType.CmdCancel);
                            if (is_success)
                            {
                                scApp.CMDBLL.updateTransferCmd_TranStatus2Canceling(transferID);
                            }
                            else
                            {
                                scApp.ReportBLL.newReportTransferCancelFailed(transferID, null);
                            }
                        }
                        else
                        {
                            scApp.ReportBLL.newReportTransferCancelFailed(transferID, null);
                        }
                    }
                    //else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Transferring) //當狀態變為Transferring時，即代表已經是Load complete
                    //{
                    //    scApp.ReportBLL.newReportTransferCancelFailed(mcs_cmd.ID, null);
                    //}
                    break;
                case ProtocolFormat.OHTMessage.CancelActionType.CmdAbort:
                    scApp.ReportBLL.newReportTransferAbortInitial(transferID, null);
                    //if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Transferring)
                    if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Initial)
                    {
                        ACMD excute_cmd = scApp.CMDBLL.GetCommandByTransferCmdID(transferID);
                        bool has_cmd_excute = excute_cmd != null;
                        if (has_cmd_excute)
                        {
                            is_success = scApp.VehicleService.Send.Cancel(excute_cmd.VH_ID, excute_cmd.ID, ProtocolFormat.OHTMessage.CancelActionType.CmdAbort);
                            if (is_success)
                            {
                                scApp.CMDBLL.updateCMD_MCS_TranStatus2Aborting(transferID);
                            }
                            else
                            {
                                scApp.ReportBLL.newReportTransferAbortFailed(transferID, null);
                            }
                        }
                        else
                        {
                            scApp.ReportBLL.newReportTransferAbortFailed(transferID, null);
                        }
                    }
                    //else if (mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Queue && mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Transferring)
                    else
                    {
                        scApp.ReportBLL.newReportTransferAbortFailed(transferID, null);
                    }
                    break;
            }
            return is_success;
        }
        private long syncTranCmdPoint = 0;
        public void Scan()
        {
            if (System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;
                    //List<ATRANSFER> un_finish_trnasfer = scApp.TransferBLL.db.transfer.loadUnfinishedTransfer();
                    //line.CurrentExcuteTransferCommand = un_finish_trnasfer;
                    if (DebugParameter.CanAutoRandomGeneratesCommand ||
                        (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        //int idle_vh_count = scApp.VehicleBLL.cache.getVhCurrentStatusInIdleCount(scApp.CMDBLL);
                        List<ATRANSFER> ACMD_MCSs = scApp.CMDBLL.loadMCS_Command_Queue();
                        //List<ATRANSFER> ACMD_MCSs = un_finish_trnasfer.
                        //                            Where(tr => tr.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                        //                            ToList();


                        //if (idle_vh_count > 0)
                        //{
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                                      Data: $"Start process normal command search...");

                        //1.確認Source port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，如果有的話就優先將該命令派給該vh
                        //(AVEHICLE findVh, ATRANSFER tran) = checkBeforeOnTheWay(in_queue_transfer);

                        //2.確認Source port是否有其他可以接收命令的vh正在準備前往Unload相同Group的port，如果有的話就優先將該命令派給該vh


                        foreach (ATRANSFER first_waitting_excute_mcs_cmd in ACMD_MCSs)
                        {
                            string hostsource = first_waitting_excute_mcs_cmd.HOSTSOURCE;
                            string hostdest = first_waitting_excute_mcs_cmd.HOSTDESTINATION;
                            string from_adr = string.Empty;
                            string to_adr = string.Empty;
                            AVEHICLE bestSuitableVh = null;
                            E_VH_TYPE vh_type = E_VH_TYPE.None;

                            //如果目的的Port是AGV Station的話，則要判斷一下是否已經有相同且在執行的MCS命令
                            APORTSTATION port_station = scApp.PortStationBLL.OperateCatch.getPortStation(hostdest);
                            if (port_station.GetEqptType(scApp.EqptBLL) == SCAppConstants.EqptType.AGVStation)
                            {
                                int excute_dest_count =
                                    scApp.CMDBLL.getCMD_MCSIsUnfinishedCountByHostDsetination(hostdest);
                                if (excute_dest_count != 0)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} want to agv station port:{port_station.PORT_ID}" +
                                             $"but has orther transfer command excute for for this port.");
                                    continue;
                                }
                            }
                            //確認 source 是否為Port
                            bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                            if (source_is_a_port)
                            {
                                scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                            }
                            else
                            {
                                //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                                if (bestSuitableVh.IsError)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                                             $"but it error happend.",
                                       VehicleID: bestSuitableVh.VEHICLE_ID);
                                    continue;
                                }
                            }



                            if (bestSuitableVh != null)
                            {
                                if (AssignTransferToVehicle(first_waitting_excute_mcs_cmd, bestSuitableVh))
                                {
                                    scApp.VehicleService.Command.Scan();
                                    return;
                                }
                                else
                                {
                                    CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                                       Data: $"Assign transfer command fail.transfer id:{first_waitting_excute_mcs_cmd.ID}",
                                       Details: check_result.ToString(),
                                       XID: check_result.Num);
                                }
                            }
                        }
                        //}
                        //else
                        //{
                        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                        //              Data: $"Not find idle car...");

                        foreach (ATRANSFER waitting_excute_mcs_cmd in ACMD_MCSs)
                        {
                            int AccumulateTime_minute = 1;
                            int current_time_priority = (int)((DateTime.Now - waitting_excute_mcs_cmd.CMD_INSER_TIME).TotalMinutes / AccumulateTime_minute);
                            if (current_time_priority != waitting_excute_mcs_cmd.TIME_PRIORITY)
                            {
                                int change_priority = current_time_priority - waitting_excute_mcs_cmd.TIME_PRIORITY;
                                scApp.CMDBLL.updateCMD_MCS_TimePriority(waitting_excute_mcs_cmd, current_time_priority);
                                scApp.CMDBLL.updateCMD_MCS_PrioritySUM(waitting_excute_mcs_cmd, waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                            }
                        }
                        //}
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }
        public void ScanByVTransfer()
        {
            if (System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;
                    List<VTRANSFER> un_finish_trnasfer = scApp.TransferBLL.db.vTransfer.loadUnfinishedVTransfer();
                    line.CurrentExcuteTransferCommand = un_finish_trnasfer;
                    if (DebugParameter.CanAutoRandomGeneratesCommand ||
                        (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        //int idle_vh_count = scApp.VehicleBLL.cache.getVhCurrentStatusInIdleCount(scApp.CMDBLL);
                        //List<ATRANSFER> ACMD_MCSs = scApp.CMDBLL.loadMCS_Command_Queue();
                        List<VTRANSFER> in_queue_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                    ToList();
                        List<VTRANSFER> excuting_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE > E_TRAN_STATUS.Queue &&
                                                                tr.TRANSFERSTATE <= E_TRAN_STATUS.Transferring).
                                                    ToList();


                        //if (idle_vh_count > 0)
                        //{
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                                      Data: $"Start process normal command search...");

                        //1.確認Source port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，如果有的話就優先將該命令派給該vh
                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) before_on_the_way_cehck_result =
                            checkBeforeOnTheWay(in_queue_transfer, excuting_transfer);
                        if (before_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle(before_on_the_way_cehck_result.bestSuitabletransfer,
                                                        before_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) after_on_the_way_cehck_result =
                            checkAfterOnTheWay(in_queue_transfer, excuting_transfer);
                        if (after_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle(after_on_the_way_cehck_result.bestSuitabletransfer,
                                                        after_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        //2.確認Source port是否有其他可以接收命令的vh正在準備前往Unload相同Group的port，如果有的話就優先將該命令派給該vh


                        foreach (VTRANSFER first_waitting_excute_mcs_cmd in in_queue_transfer)
                        {
                            string hostsource = first_waitting_excute_mcs_cmd.HOSTSOURCE;
                            string hostdest = first_waitting_excute_mcs_cmd.HOSTDESTINATION;
                            string from_adr = string.Empty;
                            string to_adr = string.Empty;
                            AVEHICLE bestSuitableVh = null;
                            E_VH_TYPE vh_type = E_VH_TYPE.None;

                            //如果目的的Port是AGV Station的話，則要判斷一下是否已經有相同且在執行的MCS命令
                            APORTSTATION port_station = scApp.PortStationBLL.OperateCatch.getPortStation(hostdest);
                            if (port_station.GetEqptType(scApp.EqptBLL) == SCAppConstants.EqptType.AGVStation)
                            {
                                int excute_dest_count =
                                    scApp.CMDBLL.getCMD_MCSIsUnfinishedCountByHostDsetination(hostdest);
                                if (excute_dest_count != 0)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} want to agv station port:{port_station.PORT_ID}" +
                                             $"but has orther transfer command excute for for this port.");
                                    continue;
                                }
                            }
                            //確認 source 是否為Port
                            bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                            if (source_is_a_port)
                            {
                                scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                            }
                            else
                            {
                                //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                                if (bestSuitableVh.IsError)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                                             $"but it error happend.",
                                       VehicleID: bestSuitableVh.VEHICLE_ID);
                                    continue;
                                }
                            }



                            if (bestSuitableVh != null)
                            {
                                if (AssignTransferToVehicle(first_waitting_excute_mcs_cmd, bestSuitableVh))
                                {
                                    scApp.VehicleService.Command.Scan();
                                    return;
                                }
                                else
                                {
                                    CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                                       Data: $"Assign transfer command fail.transfer id:{first_waitting_excute_mcs_cmd.ID}",
                                       Details: check_result.ToString(),
                                       XID: check_result.Num);
                                }
                            }
                        }
                        //}
                        //else
                        //{
                        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                        //              Data: $"Not find idle car...");

                        //foreach (VTRANSFER waitting_excute_mcs_cmd in in_queue_transfer)
                        //{
                        //    int AccumulateTime_minute = 1;
                        //    int current_time_priority = (int)((DateTime.Now - waitting_excute_mcs_cmd.CMD_INSER_TIME).TotalMinutes / AccumulateTime_minute);
                        //    if (current_time_priority != waitting_excute_mcs_cmd.TIME_PRIORITY)
                        //    {
                        //        int change_priority = current_time_priority - waitting_excute_mcs_cmd.TIME_PRIORITY;
                        //        scApp.CMDBLL.updateCMD_MCS_TimePriority(waitting_excute_mcs_cmd, current_time_priority);
                        //        scApp.CMDBLL.updateCMD_MCS_PrioritySUM(waitting_excute_mcs_cmd, waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                        //    }
                        //}
                        //}
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }

        public void ScanByVTransfer_v2()
        {
            if (System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 1) == 0)
            {
                try
                {
                    if (scApp.getEQObjCacheManager().getLine().ServiceMode
                        != SCAppConstants.AppServiceMode.Active)
                        return;
                    List<VTRANSFER> un_finish_trnasfer = scApp.TransferBLL.db.vTransfer.loadUnfinishedVTransfer();
                    line.CurrentExcuteTransferCommand = un_finish_trnasfer;
                    if (DebugParameter.CanAutoRandomGeneratesCommand ||
                        (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        List<VTRANSFER> excuting_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE > E_TRAN_STATUS.Queue &&
                                                                tr.TRANSFERSTATE <= E_TRAN_STATUS.Transferring).
                                                    ToList();
                        List<VTRANSFER> in_queue_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                    ToList();
                        //1.確認是否有要回AGV Station的命令
                        //2.有的話要確認一下，是否已有預約成功
                        //3.預約成功後則看該Station是否已經可以讓AGV執行Double Unload。
                        //4.確認是否有車子已經準備服務或正在過去
                        //3-1.有，
                        //3-2.無，則直接下達Move指令先移過去等待
                        var check_result = checkAndFindReserveUnloadToAGVStationTransfer(un_finish_trnasfer);
                        if (check_result.isFind)
                        {
                            foreach (var tran_group_by_agvstation in check_result.tranGroupsByAGVStation)
                            {
                                AGVStation reserving_load_agv_station = tran_group_by_agvstation.Key;
                                List<VTRANSFER> transfer_group = tran_group_by_agvstation.ToList();

                                List<VTRANSFER> tran_excuting_in_group = transfer_group.
                                                                Where(tran => tran.TRANSFERSTATE > E_TRAN_STATUS.Queue).
                                                                ToList();
                                List<VTRANSFER> tran_queue_in_group = transfer_group.
                                                              Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                              ToList();
                                //如果該Group已經有準備被執行/執行中的命令時，則代表該AGV Station已經有到vh去服務了，
                                //而等待被執行/執行中只有一筆且那一筆已經是Initial的時候(代表已經成功下給車子)
                                //就可以再以這一筆當出發點找出它鄰近的一筆再下給車子
                                if (tran_excuting_in_group.Count > 0)
                                {
                                    //if (tran_excuting_in_group.Count == 1 &&
                                    //    tran_excuting_in_group[0].TRANSFERSTATE >= E_TRAN_STATUS.Initial)
                                    //{
                                    //    VTRANSFER excuteing_tran = tran_excuting_in_group[0];
                                    //    var find_result = FindNearestTransferBySourcePort
                                    //        (tran_excuting_in_group[0], tran_queue_in_group);
                                    //    if (find_result.isFind)
                                    //    {
                                    //        AVEHICLE excuting_tran_vh = scApp.VehicleBLL.cache.getVehicle(excuteing_tran.VH_ID);
                                    //        bool is_success = AssignTransferToVehicle(find_result.nearestTransfer,
                                    //                                                  excuting_tran_vh);
                                    //        if (is_success)
                                    //            continue;
                                    //    }
                                    //}
                                }
                                else
                                {
                                    var find_result = FindNearestVhAndCommand(tran_queue_in_group);
                                    if (find_result.isFind)
                                    {
                                        bool is_success = AssignTransferToVehicle(find_result.nearestTransfer,
                                                                                  find_result.nearestVh);
                                        if (is_success)
                                            return;
                                        //continue;
                                    }
                                }
                            }
                        }


                        //1.確認Source port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，如果有的話就優先將該命令派給該vh
                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) before_on_the_way_cehck_result =
                            checkBeforeOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        if (before_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle(before_on_the_way_cehck_result.bestSuitabletransfer,
                                                        before_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) after_on_the_way_cehck_result =
                            checkAfterOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        if (after_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle(after_on_the_way_cehck_result.bestSuitabletransfer,
                                                        after_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        in_queue_transfer = in_queue_transfer.Where(tran => !(tran.getTragetPortEQ(scApp.PortStationBLL, scApp.EqptBLL) is IAGVStationType))
                                                             .ToList();

                        foreach (VTRANSFER first_waitting_excute_mcs_cmd in in_queue_transfer)
                        {
                            string hostsource = first_waitting_excute_mcs_cmd.HOSTSOURCE;
                            string hostdest = first_waitting_excute_mcs_cmd.HOSTDESTINATION;
                            string from_adr = string.Empty;
                            string to_adr = string.Empty;
                            AVEHICLE bestSuitableVh = null;
                            E_VH_TYPE vh_type = E_VH_TYPE.None;

                            //確認 source 是否為Port
                            bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                            if (source_is_a_port)
                            {
                                scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                            }
                            else
                            {
                                //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                                if (bestSuitableVh.IsError)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                                             $"but it error happend.",
                                       VehicleID: bestSuitableVh.VEHICLE_ID);
                                    continue;
                                }
                            }



                            if (bestSuitableVh != null)
                            {
                                if (AssignTransferToVehicle(first_waitting_excute_mcs_cmd, bestSuitableVh))
                                {
                                    scApp.VehicleService.Command.Scan();
                                    return;
                                }
                                else
                                {
                                    //CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                                    //LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                                    //   Data: $"Assign transfer command fail.transfer id:{first_waitting_excute_mcs_cmd.ID}",
                                    //   Details: check_result.ToString(),
                                    //   XID: check_result.Num);
                                }
                            }
                        }
                        //}
                        //else
                        //{
                        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                        //              Data: $"Not find idle car...");

                        //foreach (VTRANSFER waitting_excute_mcs_cmd in in_queue_transfer)
                        //{
                        //    int AccumulateTime_minute = 1;
                        //    int current_time_priority = (int)((DateTime.Now - waitting_excute_mcs_cmd.CMD_INSER_TIME).TotalMinutes / AccumulateTime_minute);
                        //    if (current_time_priority != waitting_excute_mcs_cmd.TIME_PRIORITY)
                        //    {
                        //        int change_priority = current_time_priority - waitting_excute_mcs_cmd.TIME_PRIORITY;
                        //        scApp.CMDBLL.updateCMD_MCS_TimePriority(waitting_excute_mcs_cmd, current_time_priority);
                        //        scApp.CMDBLL.updateCMD_MCS_PrioritySUM(waitting_excute_mcs_cmd, waitting_excute_mcs_cmd.PRIORITY_SUM + change_priority);
                        //    }
                        //}
                        //}
                    }
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }

        private (bool isFind, IEnumerable<IGrouping<AGVStation, VTRANSFER>> tranGroupsByAGVStation)
            checkAndFindReserveUnloadToAGVStationTransfer(List<VTRANSFER> unfinish_transfer)
        {
            var target_is_agv_stations = unfinish_transfer.
                                         Where(vtran => vtran.IsTargetPortAGVStation(scApp.EqptBLL));
            if (target_is_agv_stations.Count() == 0) { return (false, null); }

            var target_is_agv_station_groups = target_is_agv_stations.
                                               GroupBy(tran => tran.getTragetPortEQ(scApp.EqptBLL) as AGVStation).
                                               ToList();
            foreach (var target_is_agv_station in target_is_agv_station_groups.ToList())
            {
                var agv_station = target_is_agv_station.Key;
                if (!agv_station.IsReservation)
                {
                    target_is_agv_station_groups.Remove(target_is_agv_station);
                }
            }
            return (target_is_agv_station_groups.Count != 0, target_is_agv_station_groups);
        }

        public (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) FindNearestVhAndCommand(List<VTRANSFER> transfers)
        {
            List<AVEHICLE> idle_vhs = scApp.VehicleBLL.cache.loadAllVh().ToList();
            scApp.VehicleBLL.cache.filterCanNotExcuteTranVh(ref idle_vhs, scApp.CMDBLL, E_VH_TYPE.None);
            return FindNearestVhAndCommand(idle_vhs, transfers);
        }

        private (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) FindNearestVhAndCommand(List<AVEHICLE> vhs, List<VTRANSFER> transfers)
        {
            AVEHICLE nearest_vh = null;
            VTRANSFER nearest_transfer = null;
            double minimum_cost = double.MaxValue;
            try
            {
                foreach (var vh in vhs)
                {
                    foreach (var tran in transfers)
                    {
                        string hostsource = tran.HOSTSOURCE;
                        string from_adr = string.Empty;
                        bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                        if (!source_is_a_port) continue;

                        scApp.MapBLL.getAddressID(hostsource, out from_adr);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Start calculation distance, command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} ...",
                                      XID: tran.ID);
                        var result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, from_adr);
                        //double total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                        //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        double total_section_distance = 0;
                        if (result.isSuccess)
                        {
                            //total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                            //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                            total_section_distance = result.totalCost;
                        }
                        else
                        {
                            total_section_distance = double.MaxValue;
                        }
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} distance:{total_section_distance}",
                                      XID: tran.ID);
                        if (total_section_distance < minimum_cost)
                        {
                            nearest_transfer = tran;
                            nearest_vh = vh;
                            minimum_cost = total_section_distance;
                        }
                    }
                }
                if (minimum_cost == double.MaxValue)
                {
                    nearest_transfer = null;
                    nearest_vh = null;
                }
            }
            catch (Exception ex)
            {
                nearest_vh = null;
                nearest_transfer = null;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                   Data: ex);
            }
            return (nearest_vh != null && nearest_transfer != null,
                    nearest_vh,
                    nearest_transfer);
        }
        private (bool isFind, VTRANSFER nearestTransfer) FindNearestTransferBySourcePort(VTRANSFER firstTrnasfer, List<VTRANSFER> transfers)
        {
            VTRANSFER nearest_transfer = null;
            double minimum_cost = double.MaxValue;
            try
            {
                string first_tran_source_port_id = SCUtility.Trim(firstTrnasfer.HOSTSOURCE);
                bool first_tran_source_is_port = scApp.PortStationBLL.OperateCatch.IsExist(first_tran_source_port_id);
                if (!first_tran_source_is_port) return (false, null);
                string first_tran_from_adr_id = "";
                scApp.MapBLL.getAddressID(first_tran_source_port_id, out first_tran_from_adr_id);
                foreach (var tran in transfers)
                {
                    string second_tran_source_port_id = tran.HOSTSOURCE;
                    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(second_tran_source_port_id);
                    if (!source_is_a_port) continue;

                    string second_tran_from_adr_id = string.Empty;
                    scApp.MapBLL.getAddressID(second_tran_source_port_id, out second_tran_from_adr_id);

                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"Start calculation distance, command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                        $"first transfer of source port:{first_tran_source_port_id} , prepare sencond port:{second_tran_source_port_id}...",
                                  XID: tran.ID);
                    var result = scApp.GuideBLL.getGuideInfo(first_tran_from_adr_id, second_tran_from_adr_id);
                    //double total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                    //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                    double total_section_distance = 0;
                    if (result.isSuccess)
                    {
                        //total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                        //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        total_section_distance = result.totalCost;
                    }
                    else
                    {
                        total_section_distance = double.MaxValue;
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"Start calculation distance, command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                        $"first transfer of source port:{first_tran_source_port_id} , prepare sencond port:{second_tran_source_port_id},distance:{total_section_distance}",
                                  XID: tran.ID);
                    if (total_section_distance < minimum_cost)
                    {
                        nearest_transfer = tran;
                        minimum_cost = total_section_distance;
                    }
                }
                if (minimum_cost == double.MaxValue)
                {
                    nearest_transfer = null;
                }
            }
            catch (Exception ex)
            {
                nearest_transfer = null;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                   Data: ex);
            }
            return (nearest_transfer != null && minimum_cost != double.MaxValue,
                    nearest_transfer);
        }

        private (bool isFind, List<VTRANSFER> portPriorityMaxCommands) checkPortPriorityMaxCommand(List<VTRANSFER> in_queue_transfer)
        {
            List<VTRANSFER> port_priority_max_command = new List<VTRANSFER>();
            foreach (VTRANSFER cmd in in_queue_transfer)
            {
                APORTSTATION source_port = scApp.getEQObjCacheManager().getPortStation(cmd.HOSTSOURCE);
                APORTSTATION destination_port = scApp.getEQObjCacheManager().getPortStation(cmd.HOSTDESTINATION);
                if (source_port != null && source_port.PRIORITY >= SystemParameter.PortMaxPriority)
                {
                    if (destination_port != null)
                    {
                        if (source_port.PRIORITY >= destination_port.PRIORITY)
                        {
                            cmd.PORT_PRIORITY = source_port.PRIORITY;
                        }
                        else
                        {
                            cmd.PORT_PRIORITY = destination_port.PRIORITY;
                        }
                    }
                    else
                    {
                        cmd.PORT_PRIORITY = source_port.PRIORITY;
                    }
                    port_priority_max_command.Add(cmd);
                    continue;
                }
                if (destination_port != null && destination_port.PRIORITY >= SystemParameter.PortMaxPriority)
                {
                    if (source_port != null)
                    {
                        if (destination_port.PRIORITY >= source_port.PRIORITY)
                        {
                            cmd.PORT_PRIORITY = destination_port.PRIORITY;
                        }
                        else
                        {
                            cmd.PORT_PRIORITY = source_port.PRIORITY;
                        }
                    }
                    else
                    {
                        cmd.PORT_PRIORITY = destination_port.PRIORITY;
                    }
                    port_priority_max_command.Add(cmd);
                    continue;
                }
            }

            if (port_priority_max_command.Count == 0)
            {
                port_priority_max_command = null;
            }
            else
            {
                port_priority_max_command = port_priority_max_command.OrderByDescending(cmd => cmd.PORT_PRIORITY).ToList();
            }
            return (port_priority_max_command != null, port_priority_max_command);
        }

        /// <summary>
        /// 確認在Queue中的Command-Source port是否有其他可以接收命令的vh正在準備前往Load相同EQ的port，
        /// 如果有的話就優先將該命令派給該vh
        /// </summary>
        /// <param name="inQueueTransfers"></param>
        /// <returns></returns>
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkBeforeOnTheWay(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒到達Load Arrive
            //2.接著再去找目前在Queue命令中，Host source port是有相同EQ的
            //3.找到後即可將兩筆命令進行配對
            List<VTRANSFER> can_excute_before_on_the_way_tran = excutingTransfers.
                                                                Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_ARRIVE).
                                                                ToList();

            foreach (var tran in can_excute_before_on_the_way_tran)
            {
                string excute_tran_eq_id = SCUtility.Trim(tran.getSourcePortEQID(scApp.PortStationBLL));

                best_suitable_transfer = inQueueTransfers.
                                         Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getSourcePortEQID(scApp.PortStationBLL),
                                                                                   excute_tran_eq_id)).
                                         FirstOrDefault();
                if (best_suitable_transfer != null)
                {
                    string best_suitable_vh_id = SCUtility.Trim(tran.VH_ID, true);
                    best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);
                    if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh))
                    {
                        break;
                    }
                    else
                    {
                        best_suitable_vh = null;
                        continue;
                    }
                }
            }
            is_success = best_suitable_vh != null && best_suitable_transfer != null;
            return (is_success, best_suitable_vh, best_suitable_transfer);
        }

        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkBeforeOnTheWay_V2(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒到達Load Arrive
            //2.接著再去找目前在Queue命令中，Host source port是有相同EQ的
            //3.找到後即可將兩筆命令進行配對
            //List<VTRANSFER> can_excute_before_on_the_way_tran = excutingTransfers.
            //                                                    Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_ARRIVE).
            //                                                    ToList();

            List<VTRANSFER> can_excute_before_on_the_way_tran = excutingTransfers;

            foreach (var tran in can_excute_before_on_the_way_tran)
            {
                string excute_tran_eq_id = SCUtility.Trim(tran.getSourcePortEQID(scApp.PortStationBLL));
                var same_eq_ports = inQueueTransfers.
                                    Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getSourcePortEQID(scApp.PortStationBLL),
                                                                              excute_tran_eq_id)).
                                    ToList();
                var check_result = FindNearestTransferBySourcePort(tran, same_eq_ports);
                //best_suitable_transfer = inQueueTransfers.
                //                         Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getSourcePortEQID(scApp.PortStationBLL),
                //                                                                   excute_tran_eq_id)).
                //                         FirstOrDefault();
                //if (best_suitable_transfer != null)
                if (check_result.isFind)
                {
                    best_suitable_transfer = check_result.nearestTransfer;
                    string best_suitable_vh_id = SCUtility.Trim(tran.VH_ID, true);
                    best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);
                    if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh))
                    {
                        break;
                    }
                    else
                    {
                        best_suitable_vh = null;
                        continue;
                    }
                }
            }
            is_success = best_suitable_vh != null && best_suitable_transfer != null;
            return (is_success, best_suitable_vh, best_suitable_transfer);
        }


        /// <summary>
        /// 確認Target port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，
        /// 如果有的話就優先將該命令派給該vh
        /// </summary>
        /// <param name="inQueueTransfers"></param>
        /// <returns></returns>
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkAfterOnTheWay(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒到達Load Arrive
            //2.接著再去找目前在Queue命令中，Host source port是有相同EQ的
            //3.找到後即可將兩筆命令進行配對
            List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.
                                                                Where(tr => tr.COMMANDSTATE >= ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE &&
                                                                            tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOAD_COMPLETE).
                                                                ToList();

            foreach (var tran in can_excute_after_on_the_way_tran)
            {
                string excute_tran_eq_id = SCUtility.Trim(tran.getTragetPortEQID(scApp.PortStationBLL));
                best_suitable_transfer = inQueueTransfers.
                                         Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getSourcePortEQID(scApp.PortStationBLL),
                                                                                   excute_tran_eq_id)).
                                         FirstOrDefault();
                if (best_suitable_transfer != null)
                {
                    string best_suitable_vh_id = SCUtility.Trim(tran.VH_ID, true);
                    best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);
                    if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh))
                    {
                        break;
                    }
                    else
                    {
                        best_suitable_vh = null;
                        continue;
                    }
                }
            }
            is_success = best_suitable_vh != null && best_suitable_transfer != null;
            return (is_success, best_suitable_vh, best_suitable_transfer);
        }
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkAfterOnTheWay_V2(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒到達Load Arrive
            //2.接著再去找目前在Queue命令中，Host source port是有相同EQ的
            //3.找到後即可將兩筆命令進行配對
            List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers;
            //List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.
            //                                                    Where(tr => tr.COMMANDSTATE >= ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE &&
            //                                                                tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOAD_COMPLETE).
            //                                                    ToList();

            foreach (var tran in can_excute_after_on_the_way_tran)
            {
                string best_suitable_vh_id = SCUtility.Trim(tran.VH_ID, true);
                best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);
                if (!scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh))
                {
                    best_suitable_vh = null;
                    continue;
                }

                string excute_tran_eq_id = SCUtility.Trim(tran.getTragetPortEQID(scApp.PortStationBLL));
                var same_eq_ports = inQueueTransfers.
                                    Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getTragetPortEQID(scApp.PortStationBLL),
                                                                              excute_tran_eq_id)).
                                    ToList();

                var check_result = FindNearestTransferBySourcePort(tran, same_eq_ports);
                if (check_result.isFind)
                {
                    best_suitable_transfer = check_result.nearestTransfer;
                    break;
                }
                else
                {
                    best_suitable_transfer = null;
                }
            }
            is_success = best_suitable_vh != null && best_suitable_transfer != null;
            return (is_success, best_suitable_vh, best_suitable_transfer);
        }

        public bool AssignTransferToVehicle(ATRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            is_success = is_success && scApp.CMDBLL.checkCmd(assign_cmd);
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    is_success = is_success && scApp.CMDBLL.addCmd(assign_cmd);
                    is_success = is_success && scApp.CMDBLL.updateTransferCmd_TranStatus2PreInitial(waittingExcuteMcsCmd.ID);
                    if (is_success)
                    {
                        tx.Complete();
                    }
                    else
                    {
                        CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                        check_result.Result.AppendLine($" vh:{assign_cmd.VH_ID} creat command to db unsuccess.");
                    }
                }
            }
            return is_success;
        }

        public bool AssignTransferToVehicle(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            is_success = is_success && scApp.CMDBLL.checkCmd(assign_cmd);
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    is_success = is_success && scApp.CMDBLL.addCmd(assign_cmd);
                    is_success = is_success && scApp.CMDBLL.updateTransferCmd_TranStatus2PreInitial(waittingExcuteMcsCmd.ID);
                    if (is_success)
                    {
                        tx.Complete();
                    }
                    else
                    {
                        CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                        check_result.Result.AppendLine($" vh:{assign_cmd.VH_ID} creat command to db unsuccess.");
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                           Data: $"Assign transfer command fail.transfer id:{waittingExcuteMcsCmd.ID}",
                           Details: check_result.ToString(),
                           XID: check_result.Num);
                    }
                }
            }
            return is_success;
        }
        public bool AssignTransferToVehicle_V2(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            var destination_info = checkAndRenameDestinationPortIfAGVStation(assign_cmd);
            if (destination_info.checkSuccess)
            {
                assign_cmd.DESTINATION = destination_info.destinationAdrID;
                assign_cmd.DESTINATION_PORT = destination_info.destinationPortID;
            }
            else
            {
                //todo log...
                return false;
            }
            is_success = is_success && scApp.CMDBLL.checkCmd(assign_cmd);
            using (TransactionScope tx = SCUtility.getTransactionScope())
            {
                using (DBConnection_EF con = DBConnection_EF.GetUContext())
                {
                    is_success = is_success && scApp.CMDBLL.addCmd(assign_cmd);
                    is_success = is_success && scApp.CMDBLL.updateTransferCmd_TranStatus2PreInitial(waittingExcuteMcsCmd.ID);
                    if (is_success)
                    {
                        tx.Complete();
                    }
                    else
                    {
                        CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                        check_result.Result.AppendLine($" vh:{assign_cmd.VH_ID} creat command to db unsuccess.");
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                           Data: $"Assign transfer command fail.transfer id:{waittingExcuteMcsCmd.ID}",
                           Details: check_result.ToString(),
                           XID: check_result.Num);
                    }
                }
            }
            return is_success;
        }
        private (bool checkSuccess, string destinationPortID, string destinationAdrID) checkAndRenameDestinationPortIfAGVStation(ACMD assignCmd)
        {
            if (assignCmd.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType)
            {
                IAGVStationType unload_agv_station = assignCmd.getTragetPortEQ(scApp.EqptBLL) as IAGVStationType;
                var ready_agv_station_port = unload_agv_station.loadReadyAGVStationPort();
                foreach (var port in ready_agv_station_port)
                {
                    bool has_command_excute = cmdBLL.hasExcuteCMDByDestinationPort(port.PORT_ID);
                    if (!has_command_excute)
                    {
                        return (true, port.PORT_ID, port.ADR_ID);
                    }
                }
                //todo log
                return (false, "", "");
            }
            else
            {
                return (true, assignCmd.DESTINATION, assignCmd.DESTINATION_PORT);
            }
        }

        public (bool isSuccess, string result) CommandShift(string transferID, string vhID)
        {
            return (false, ""); //todo kevin 需要實作 Command shift功能。

            try
            {
                bool is_success = false;
                string result = "";
                //1. Cancel命令
                ATRANSFER mcs_cmd = scApp.CMDBLL.GetTransferByID(transferID);
                if (mcs_cmd == null)
                {
                    result = $"want to cancel/abort mcs cmd:{transferID},but cmd not exist.";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Details: result,
                       XID: transferID);
                    is_success = false;
                    return (is_success, result);
                }
                //當命令還沒被初始化(即尚未被送下去)或者已經為Transferring時(已經將貨物載到車上)，則不能進Command shift的動作
                if (mcs_cmd.TRANSFERSTATE < E_TRAN_STATUS.Initial || mcs_cmd.TRANSFERSTATE >= E_TRAN_STATUS.Transferring)
                {
                    result = $"want to excute command shift mcs cmd:{transferID},but current transfer state is:{mcs_cmd.TRANSFERSTATE}, can't excute.";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Details: result,
                       XID: transferID);
                    is_success = false;
                    return (is_success, result);
                }


                ACMD excute_cmd = scApp.CMDBLL.GetCommandByTransferCmdID(transferID);
                bool has_cmd_excute = excute_cmd != null;
                if (!has_cmd_excute)
                {
                    result = $"want to excute command shift mcs cmd:{transferID},but current not vh in excute.";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Details: result,
                       XID: transferID);
                    is_success = false;
                    return (is_success, result);
                }
                bool btemp = scApp.VehicleService.Send.Cancel(excute_cmd.VH_ID, excute_cmd.ID, ProtocolFormat.OHTMessage.CancelActionType.CmdCancel);
                if (btemp)
                {
                    result = "OK";
                }
                else
                {
                    is_success = false;
                    result = $"Transfer command:[{transferID}] cancel failed.";
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Details: result,
                       XID: transferID);
                    return (is_success, result);
                }
                //2. Unassign Vehicle
                //3. 分派命令給新車(不能報command initial)
                //ATRANSFER ACMD_MCS = scApp.CMDBLL.GetTransferByID(mcs_id);
                //if (ACMD_MCS != null)
                //{
                //    bool check_result = true;
                //    result = "OK";
                //    //ACMD_MCS excute_cmd = ACMD_MCSs[0];
                //    string hostsource = ACMD_MCS.HOSTSOURCE;
                //    string hostdest = ACMD_MCS.HOSTDESTINATION;
                //    string from_adr = string.Empty;
                //    string to_adr = string.Empty;
                //    AVEHICLE vh = null;
                //    E_VH_TYPE vh_type = E_VH_TYPE.None;
                //    E_CMD_TYPE cmd_type = default(E_CMD_TYPE);

                //    //確認 source 是否為Port
                //    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                //    if (source_is_a_port)
                //    {
                //        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                //        vh = scApp.VehicleBLL.cache.getVehicle(vh_id);
                //        cmd_type = E_CMD_TYPE.LoadUnload;
                //    }
                //    else
                //    {
                //        result = "Source must be a port.";
                //        return false;
                //    }
                //    scApp.MapBLL.getAddressID(hostdest, out to_adr);
                //    if (vh != null)
                //    {
                //        if (vh.ACT_STATUS != VHActionStatus.Commanding)
                //        {
                //            bool temp = AssignMCSCommand2Vehicle(ACMD_MCS, cmd_type, vh);
                //            if (!temp)
                //            {
                //                result = "Assign command to vehicle failed.";
                //                return false;
                //            }
                //        }
                //        else
                //        {
                //            result = "Vehicle already have command.";
                //            return false;

                //        }

                //    }
                //    else
                //    {
                //        result = $"Can not find vehicle:{vh_id}.";
                //        return false;
                //    }
                //    return true;
                //}
                //else
                //{
                //    result = $"Can not find command:{mcs_id}.";
                //    return false;
                //}
            }
            finally
            {
                //System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
            }
        }
        public (bool isSuccess, string result) FinishTransferCommand(string cmdID, CompleteStatus completeStatus)
        {
            try
            {
                scApp.CMDBLL.updateCMD_MCS_TranStatus2Complete(cmdID, completeStatus);
                VTRANSFER vtran = cmdBLL.getVCMD_MCSByID(cmdID);
                scApp.ReportBLL.newReportTransferCommandForceFinish(vtran, completeStatus, null);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return (false, ex.ToString());
            }
            return (true, $"Force finish mcs command sucess.");
        }

        public (bool isSuccess, string result) ForceInstallCarrierInVehicle(string vhID, string vhLocation, string carrierID)
        {
            try
            {
                //1.需確認該Location目前是有貨的
                //3.需確認該Location目前是沒有帳的
                //2.需確認該Carrier目前沒有在線內
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                Location location_info = vh.CarrierLocation.
                                            Where(loc => SCUtility.isMatche(loc.ID, vhLocation)).
                                            FirstOrDefault();
                if (!location_info.HAS_CST)
                {
                    return (false, $"Location:{vhLocation} no carrier exist.");
                }

                var check_has_carrier_on_location_result = carrierBLL.db.hasCarrierOnVhLocation(vhLocation);
                if (check_has_carrier_on_location_result.has)
                {
                    return (false, $"Location:{vhLocation} is already carrier:{check_has_carrier_on_location_result.onVhCarrier.ID} exist.");
                }
                var check_has_carrier_in_line_result = carrierBLL.db.hasCarrierInLine(carrierID);
                if (check_has_carrier_in_line_result.has)
                {
                    return (false, $"Carrier:{carrierID} is already in line current location in:{SCUtility.Trim(check_has_carrier_in_line_result.inLineCarrier.LOCATION, true)}.");
                }

                ACARRIER carrier = new ACARRIER()
                {
                    ID = carrierID,
                    LOT_ID = "",
                    INSER_TIME = DateTime.Now,
                    INSTALLED_TIME = DateTime.Now,
                    LOCATION = vhLocation,
                    STATE = ProtocolFormat.OHTMessage.E_CARRIER_STATE.Installed
                };
                carrierBLL.db.addOrUpdate(carrier);
                scApp.ReportBLL.newReportCarrierInstalled(vh.Real_ID, carrierID, vhLocation, null);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return (false, ex.ToString());
            }
            return (true, $"Install carrier:{carrierID} in location:{vhLocation} success.");
        }
        public (bool isSuccess, string result) ForceRemoveCarrierInVehicle(string carrierID)
        {
            try
            {
                var check_has_carrier_in_line_result = carrierBLL.db.hasCarrierInLine(carrierID);
                if (!check_has_carrier_in_line_result.has)
                {
                    return (false, $"Carrier:{carrierID} is not in AGVC system.");
                }

                carrierBLL.db.updateLocationAndState(carrierID, string.Empty, ProtocolFormat.OHTMessage.E_CARRIER_STATE.OpRemove);
                string current_location = check_has_carrier_in_line_result.inLineCarrier.LOCATION;
                var location_of_vh = scApp.VehicleBLL.cache.getVehicleByLocationID(current_location);
                if (location_of_vh != null)
                    scApp.ReportBLL.newReportCarrierRemoved
                        (location_of_vh.Real_ID, SCUtility.Trim(carrierID, true), SCUtility.Trim(current_location, true), null);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return (false, ex.ToString());
            }
            return (true, $"Remove carrier:{carrierID} is success.");
        }
        public (bool isSuccess, string result) processIDReadFailAndMismatch(string commandCarrierID, CompleteStatus completeStatus)
        {
            var check_has_carrier_in_line_result = carrierBLL.db.hasCarrierInLine(commandCarrierID);
            if (!check_has_carrier_in_line_result.has)
            {
                return (false, $"Carrier:{commandCarrierID} is not in AGVC system.");
            }
            E_CARRIER_STATE carrier_state = E_CARRIER_STATE.None;
            switch (completeStatus)
            {
                case CompleteStatus.IdmisMatch:
                    carrier_state = E_CARRIER_STATE.IdMismatch;
                    break;
                case CompleteStatus.IdreadFailed:
                    carrier_state = E_CARRIER_STATE.IdReadFail;
                    break;
            }
            //將原本的帳移除
            ACARRIER remove_carrier = check_has_carrier_in_line_result.inLineCarrier;
            carrierBLL.db.updateLocationAndState(remove_carrier.ID, string.Empty, carrier_state);
            var location_of_vh = scApp.VehicleBLL.cache.getVehicleByLocationID(remove_carrier.LOCATION);
            scApp.ReportBLL.newReportCarrierRemoved
                (location_of_vh.Real_ID, SCUtility.Trim(remove_carrier.ID, true), SCUtility.Trim(remove_carrier.LOCATION, true), null);

            //建入Rename後的帳
            ACARRIER install_carrier = new ACARRIER()
            {
                ID = remove_carrier.RENAME_ID,
                LOT_ID = remove_carrier.LOT_ID,
                INSER_TIME = DateTime.Now,
                INSTALLED_TIME = DateTime.Now,
                LOCATION = remove_carrier.LOCATION,
                STATE = E_CARRIER_STATE.Installed
            };
            carrierBLL.db.addOrUpdate(install_carrier);
            scApp.ReportBLL.newReportCarrierInstalled(location_of_vh.Real_ID, install_carrier.ID, install_carrier.LOCATION, null);
            return (true, $"process[{completeStatus}] success,remove carrier:{commandCarrierID} install:{install_carrier.ID}");
        }



    }
}
