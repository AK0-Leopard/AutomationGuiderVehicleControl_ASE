using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using DocumentFormat.OpenXml.Bibliography;
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
        protected NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        protected SCApplication scApp = null;
        private TransferBLL transferBLL = null;
        private CarrierBLL carrierBLL = null;
        private SysExcuteQualityBLL sysExcuteQualityBLL = null;
        private ReportBLL reportBLL = null;
        private LineBLL lineBLL = null;
        private CMDBLL cmdBLL = null;
        protected ALINE line = null;
        ITranAssigner NormalTranAssigner;
        ITranAssigner SwapTranAssigner;
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
            NormalTranAssigner = new TranAssignerNormal(scApp);
            SwapTranAssigner = new TransferAssignerSwap(scApp);

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

                //var carrier_info = transfer.GetCarrierInfo();
                var carrier_info = transfer.GetCarrierInfo(scApp.VehicleBLL);
                var sys_excute_quality_info = transfer.GetSysExcuteQuality(scApp.VehicleBLL);
                transferBLL.db.transfer.add(transfer);
                if (transfer.TRANSFERSTATE == E_TRAN_STATUS.Queue)
                {
                    carrierBLL.db.addOrUpdate(carrier_info);
                    //sysExcuteQualityBLL.addSysExcuteQuality(sys_excute_quality_info);
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
                    if (IsInTransferTime(mcs_cmd))
                    {
                        scApp.ReportBLL.newReportTransferCancelFailed(transferID, null);
                        return false;
                    }
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
                    if (IsInTransferTime(mcs_cmd))
                    {
                        scApp.ReportBLL.newReportTransferAbortFailed(transferID, null);
                        return false;
                    }
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

        private bool IsInTransferTime(ATRANSFER mcs_cmd)
        {
            //如果在Load中就回傳TRUE
            if (mcs_cmd.COMMANDSTATE >= ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_ARRIVE &&
                mcs_cmd.COMMANDSTATE <= ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE)
            {
                return true;
            }
            //如果在Unload中就回傳TRUE
            if (mcs_cmd.COMMANDSTATE >= ATRANSFER.COMMAND_STATUS_BIT_INDEX_UNLOAD_ARRIVE)
            {
                return true;
            }
            return false;
        }

        private long syncTranCmdPoint = 0;


        public virtual void ScanByVTransfer_v2()
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

                    Task.Run(() => queueTimeOutCheck(un_finish_trnasfer));
                    if (un_finish_trnasfer == null || un_finish_trnasfer.Count == 0) return;
                    if (DebugParameter.CanAutoRandomGeneratesCommand ||
                        (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        List<VTRANSFER> excuting_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE > E_TRAN_STATUS.Queue &&
                                                                tr.TRANSFERSTATE <= E_TRAN_STATUS.Transferring &&
                                                                !SCUtility.isEmpty(tr.VH_ID)).
                                                    ToList();
                        List<VTRANSFER> in_queue_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                    ToList();

                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) after_on_the_way_cehck_result =
                           checkAfterOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        if (after_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle_V2(after_on_the_way_cehck_result.bestSuitabletransfer,
                                                        after_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        //1.確認Source port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，如果有的話就優先將該命令派給該vh
                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) before_on_the_way_cehck_result =
                            checkBeforeOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        if (before_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferToVehicle_V2(before_on_the_way_cehck_result.bestSuitabletransfer,
                                                        before_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        try
                        {
                            //如果是在找Source非EQ Port的命令時，要用Port Priority做排序來幫助雙車(6/9)在跑時，如果370過於忙碌，都沒有車子可以去服務450的問題
                            var source_not_agv_st_in_queue_transfer = in_queue_transfer.Where(tran => !(tran.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType))
                                                                 .OrderByDescending(tran => tran.PORT_PRIORITY)
                                                                 .ToList();

                            foreach (VTRANSFER first_waitting_excute_mcs_cmd in source_not_agv_st_in_queue_transfer)
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
                                    //需確認是否已經有其他車在搬送同SourcePort的CST，且還沒到Transferring
                                    scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                    var check_has_some_source_vh_excuting = findOtherSameSourcePortCmdExcuteAndVhCanService(hostsource, excuting_transfer);
                                    if (check_has_some_source_vh_excuting.hasFind)
                                    {
                                        bestSuitableVh = check_has_some_source_vh_excuting.vh;
                                    }
                                    else
                                    {
                                        bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                                        if (bestSuitableVh != null)
                                        {
                                            var check_has_orther_vhs_in_block_result = HasOtherVhInTargetBlockOrWillGoTo(bestSuitableVh.VEHICLE_ID, from_adr);
                                            if (check_has_orther_vhs_in_block_result.hasOtherVh)
                                            {
                                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                              Data: $"Try find the vh:[{bestSuitableVh.VEHICLE_ID}] to block load cst , but has other vh in this block (Finial).",
                                                              XID: first_waitting_excute_mcs_cmd.ID);
                                                List<string> other_in_block_vhs = check_has_orther_vhs_in_block_result.vhs;
                                                foreach (string vh_id in other_in_block_vhs)
                                                {
                                                    AVEHICLE in_block_vh = scApp.VehicleBLL.cache.getVehicle(vh_id);
                                                    if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, in_block_vh))
                                                    {
                                                        bool is_success = AssignTransferToVehicle_V2(first_waitting_excute_mcs_cmd,
                                                                                                     in_block_vh);
                                                        if (is_success)
                                                        {
                                                            scApp.VehicleService.Command.Scan();
                                                            return;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                                      Data: $"In block vh:[{bestSuitableVh.VEHICLE_ID}] not ready to assign transfer command.",
                                                                      XID: first_waitting_excute_mcs_cmd.ID);
                                                    }
                                                }
                                                continue;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                    bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                                    if (bestSuitableVh.IsError ||
                                        bestSuitableVh.MODE_STATUS != VHModeStatus.AutoRemote)
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                                                 $"but it error happend or not auto remote.",
                                           VehicleID: bestSuitableVh.VEHICLE_ID);
                                        continue;
                                    }
                                }



                                if (bestSuitableVh != null)
                                {
                                    if (AssignTransferToVehicle_V2(first_waitting_excute_mcs_cmd, bestSuitableVh))
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
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Exception");
                        }
                        //1.確認是否有要回AGV Station的命令
                        //2.有的話要確認一下，是否已有預約成功
                        //3.預約成功後則看該Station是否已經可以讓AGV執行Double Unload。
                        //4.確認是否有車子已經準備服務或正在過去
                        //3-1.有，
                        //3-2.無，則直接下達Move指令先移過去等待
                        var check_result = checkAndFindReserveSuccessUnloadToAGVStationTransfer(un_finish_trnasfer);
                        if (check_result.isFind)
                        {
                            foreach (var tran_group_by_agvstation in check_result.tranGroupsByAGVStation)
                            {
                                AGVStation reserving_unload_agv_station = tran_group_by_agvstation.Key;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"agv station:{reserving_unload_agv_station.getAGVStationID()},is reserve success .check can start first assign this group...");
                                List<VTRANSFER> transfer_group = tran_group_by_agvstation.ToList();

                                List<VTRANSFER> tran_excuting_in_group = transfer_group.
                                                                Where(tran => tran.TRANSFERSTATE > E_TRAN_STATUS.Queue).
                                                                ToList();
                                List<VTRANSFER> tran_queue_in_group = transfer_group.
                                                              Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                              OrderBy(tran => tran.CARRIER_INSER_TIME).
                                                              ToList();


                                var try_find_carrier_on_vh_result = tryFindAssignOnVhCarrier(tran_queue_in_group);
                                if (try_find_carrier_on_vh_result.hasFind)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"find has carrier of vh:{try_find_carrier_on_vh_result.hasCarrierOfVh.VEHICLE_ID}, start assign command to vh");

                                    bool is_success = AssignTransferToVehicle_V2(try_find_carrier_on_vh_result.tran,
                                                                                 try_find_carrier_on_vh_result.hasCarrierOfVh);
                                    if (is_success)
                                        return;
                                }

                                //如果該Group已經有準備被執行/執行中的命令時，則代表該AGV Station已經有到vh去服務了，
                                //而等待被執行/執行中只有一筆且那一筆已經是Initial的時候(代表已經成功下給車子)
                                //就可以再以這一筆當出發點找出它鄰近的一筆再下給車子
                                if (tran_excuting_in_group.Count > 0)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"agv station:{reserving_unload_agv_station.getAGVStationID()} has cmd excute. can't start first assign");

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
                                    //var find_result = FindNearestVhAndCommand(tran_queue_in_group);
                                    (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) find_result =
                                        default((bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer));

                                    find_result = FindVhAndCommand(tran_queue_in_group);

                                    if (find_result.isFind)
                                    {
                                        bool is_success = AssignTransferToVehicle_V2(find_result.nearestTransfer,
                                                                                  find_result.nearestVh);
                                        if (is_success)
                                            return;
                                        //continue;
                                    }
                                }
                            }
                        }


                        //(bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) after_on_the_way_cehck_result =
                        //    checkAfterOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        //if (after_on_the_way_cehck_result.isFind)
                        //{
                        //    if (AssignTransferToVehicle_V2(after_on_the_way_cehck_result.bestSuitabletransfer,
                        //                                after_on_the_way_cehck_result.bestSuitableVh))
                        //    {
                        //        scApp.VehicleService.Command.Scan();
                        //        return;
                        //    }
                        //}

                        ////1.確認Source port是否有其他可以接收命令的vh正在準備前往Load相同Group的port，如果有的話就優先將該命令派給該vh
                        //(bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) before_on_the_way_cehck_result =
                        //    checkBeforeOnTheWay_V2(in_queue_transfer, excuting_transfer);
                        //if (before_on_the_way_cehck_result.isFind)
                        //{
                        //    if (AssignTransferToVehicle_V2(before_on_the_way_cehck_result.bestSuitabletransfer,
                        //                                before_on_the_way_cehck_result.bestSuitableVh))
                        //    {
                        //        scApp.VehicleService.Command.Scan();
                        //        return;
                        //    }
                        //}


                        //var not_agv_st_in_queue_transfer = in_queue_transfer.Where(tran => !(tran.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType))
                        //                                     .ToList();

                        //foreach (VTRANSFER first_waitting_excute_mcs_cmd in not_agv_st_in_queue_transfer)
                        //{
                        //    string hostsource = first_waitting_excute_mcs_cmd.HOSTSOURCE;
                        //    string hostdest = first_waitting_excute_mcs_cmd.HOSTDESTINATION;
                        //    string from_adr = string.Empty;
                        //    string to_adr = string.Empty;
                        //    AVEHICLE bestSuitableVh = null;
                        //    E_VH_TYPE vh_type = E_VH_TYPE.None;

                        //    //確認 source 是否為Port
                        //    bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                        //    if (source_is_a_port)
                        //    {
                        //        //需確認是否已經有其他車在搬送同SourcePort的CST，且還沒到Transferring
                        //        scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                        //        var check_has_some_source_vh_excuting = findOtherSameSourcePortCmdExcuteAndVhCanService(hostsource, excuting_transfer);
                        //        if (check_has_some_source_vh_excuting.hasFind)
                        //        {
                        //            bestSuitableVh = check_has_some_source_vh_excuting.vh;
                        //        }
                        //        else
                        //        {
                        //            bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                        //            if (HasOtherVhInTargetBlockOrWillGoTo(bestSuitableVh.VEHICLE_ID, from_adr))
                        //            {
                        //                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                        //                              Data: $"Try find the vh:[{bestSuitableVh.VEHICLE_ID}] to block load cst , but has other vh in this block (Finial).",
                        //                              XID: first_waitting_excute_mcs_cmd.ID);
                        //                continue;
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                        //        bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                        //        if (bestSuitableVh.IsError ||
                        //            bestSuitableVh.MODE_STATUS != VHModeStatus.AutoRemote)
                        //        {
                        //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                        //               Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                        //                     $"but it error happend or not auto remote.",
                        //               VehicleID: bestSuitableVh.VEHICLE_ID);
                        //            continue;
                        //        }
                        //    }



                        //    if (bestSuitableVh != null)
                        //    {
                        //        if (AssignTransferToVehicle_V2(first_waitting_excute_mcs_cmd, bestSuitableVh))
                        //        {
                        //            scApp.VehicleService.Command.Scan();
                        //            return;
                        //        }
                        //        else
                        //        {
                        //            //CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                        //            //LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                        //            //   Data: $"Assign transfer command fail.transfer id:{first_waitting_excute_mcs_cmd.ID}",
                        //            //   Details: check_result.ToString(),
                        //            //   XID: check_result.Num);
                        //        }
                        //    }
                        //}

                        //foreach (VTRANSFER queue_tran in in_queue_transfer)
                        //{
                        //    int AccumulateTime_minute = 5;
                        //    int current_time_priority = (int)((DateTime.Now - queue_tran.CMD_INSER_TIME).TotalMinutes * AccumulateTime_minute);
                        //    if (current_time_priority != queue_tran.TIME_PRIORITY)
                        //    {
                        //        int change_priority = current_time_priority - queue_tran.TIME_PRIORITY;
                        //        int new_sum_priority = queue_tran.PRIORITY_SUM + change_priority;
                        //        scApp.CMDBLL.updateCMD_MCS_TimePriority(queue_tran.ID, current_time_priority, new_sum_priority);
                        //    }
                        //}
                        //}
                        //else
                        //{
                        //LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: string.Empty,
                        //              Data: $"Not find idle car...");
                        //bool has_mcs_cmd_time_out_in_queue = in_queue_transfer.Where(tran => tran.IsQueueTimeOut).Count() > 0;
                        //if (has_mcs_cmd_time_out_in_queue)
                        //{
                        //    scApp.LineService.ProcessAlarmReport("AGVC", AlarmBLL.AGVC_TRAN_COMMAND_IN_QUEUE_TIME_OUT, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                        //                $"AGVC has trnasfer commmand in queue over time:{SystemParameter.TransferCommandQueueTimeOut_mSec}ms");
                        //}
                        //else
                        //{
                        //    scApp.LineService.ProcessAlarmReport("AGVC", AlarmBLL.AGVC_TRAN_COMMAND_IN_QUEUE_TIME_OUT, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                        //                $"AGVC has trnasfer commmand in queue over time:{SystemParameter.TransferCommandQueueTimeOut_mSec}ms");
                        //}
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
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }

        protected (bool hasFind, AVEHICLE vh) findOtherSameSourcePortCmdExcuteAndVhCanService(string sourcePort, List<VTRANSFER> excuteVTran)
        {
            if (excuteVTran == null || excuteVTran.Count == 0) return (false, null);
            var other_excute_v_trans = excuteVTran.Where(tran => SCUtility.isMatche(tran.HOSTSOURCE, sourcePort)).ToList();
            if (other_excute_v_trans == null || other_excute_v_trans.Count == 0) return (false, null);
            foreach (var tran in other_excute_v_trans)
            {
                if (tran.TRANSFERSTATE >= E_TRAN_STATUS.Transferring) continue;
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(tran.VH_ID);
                if (vh == null) continue;
                if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, vh))
                {
                    return (true, vh);
                }
            }
            return (false, null);
        }

        DateTime LastNotifyMCSCommandExcuteTimeout = DateTime.MinValue;
        int NotifyMCSCommandExcuteTimeoutInterval_min = 2;
        protected void queueTimeOutCheck(List<VTRANSFER> un_finish_trnasfer)
        {
            try
            {
                bool has_mcs_cmd_time_out_excute = un_finish_trnasfer.Where(tran => tran.IsExcuteTimeOut).Count() > 0;
                if (has_mcs_cmd_time_out_excute)
                {
                    scApp.LineService.ProcessAlarmReport("AGVC", AlarmBLL.AGVC_TRAN_COMMAND_IN_QUEUE_TIME_OUT, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                                $"AGVC has trnasfer commmand in queue over time:{SystemParameter.TransferCommandExcuteTimeOut_mSec}ms");
                    if (DateTime.Now > LastNotifyMCSCommandExcuteTimeout.AddMinutes(NotifyMCSCommandExcuteTimeoutInterval_min))
                    {
                        LastNotifyMCSCommandExcuteTimeout = DateTime.Now;
                        transferBLL.web.mcsCommandExcuteTimeOutNotify();
                    }
                }
                else
                {
                    scApp.LineService.ProcessAlarmReport("AGVC", AlarmBLL.AGVC_TRAN_COMMAND_IN_QUEUE_TIME_OUT, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                                $"AGVC has trnasfer commmand in queue over time:{SystemParameter.TransferCommandExcuteTimeOut_mSec}ms");
                }
            }
            catch { }
        }

        protected (bool hasFind, AVEHICLE hasCarrierOfVh, VTRANSFER tran) tryFindAssignOnVhCarrier(List<VTRANSFER> tran_queue_in_group)
        {
            foreach (var tran in tran_queue_in_group)
            {
                string hostsource = tran.HOSTSOURCE;
                string from_adr = string.Empty;
                bool source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(hostsource);
                //if (!source_is_a_port) continue;
                if (!source_is_a_port)
                {
                    var bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                    if (bestSuitableVh == null ||
                        bestSuitableVh.IsError ||
                        bestSuitableVh.MODE_STATUS != VHModeStatus.AutoRemote)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                           Data: $"Has transfer command:{SCUtility.Trim(tran.ID, true)} for vh:{hostsource}" +
                                 $"but it error happend or not auto remove or not this object.");
                        continue;
                    }
                    return (true, bestSuitableVh, tran);
                }
            }
            return (false, null, null);
        }

        protected (bool isFind, IEnumerable<IGrouping<AGVStation, VTRANSFER>> tranGroupsByAGVStation)
            checkAndFindReserveSuccessUnloadToAGVStationTransfer(List<VTRANSFER> unfinish_transfer)
        {
            var target_is_agv_stations = unfinish_transfer.
                                         Where(vtran => vtran.IsTargetPortAGVStation(scApp.EqptBLL));
            if (target_is_agv_stations.Count() == 0) { return (false, null); }
            target_is_agv_stations = target_is_agv_stations.OrderByDescending(tran => tran.PORT_PRIORITY).ToList();
            var target_is_agv_station_groups = target_is_agv_stations.
                                               GroupBy(tran => tran.getTragetPortEQ(scApp.EqptBLL) as AGVStation).
                                               ToList();
            foreach (var target_is_agv_station in target_is_agv_station_groups.ToList())
            {
                var agv_station = target_is_agv_station.Key;
                if (!agv_station.IsReservation)
                {
                    target_is_agv_station_groups.Remove(target_is_agv_station);
                    var group_tran_ids = target_is_agv_station.ToList().Select(tran => tran.ID);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                       Data: $"agv station:{agv_station.getAGVStationID()},not reserve success ,remove it.group tran ids:{string.Join(",", group_tran_ids)}");
                }
            }
            return (target_is_agv_station_groups.Count != 0, target_is_agv_station_groups);
        }


        protected (bool isFind, VTRANSFER nearestTransfer) FindNearestTransferBySourcePort(VTRANSFER firstTrnasfer, List<VTRANSFER> transfers)
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
                    //var result = scApp.GuideBLL.getGuideInfo(first_tran_from_adr_id, second_tran_from_adr_id);
                    var result = scApp.GuideBLL.IsRoadWalkable(first_tran_from_adr_id, second_tran_from_adr_id, out int totalCost);
                    //double total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                    //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                    double total_section_distance = 0;
                    //if (result.isSuccess)
                    if (result)
                    {
                        //total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                        //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        total_section_distance = totalCost;
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
        protected (bool isFind, VTRANSFER nearestTransfer) FindNearestTransferByTargetPort(VTRANSFER firstTrnasfer, List<VTRANSFER> inQueueTransfers)
        {
            VTRANSFER nearest_transfer = null;
            double minimum_cost = double.MaxValue;
            try
            {
                string first_tran_dest_port_id = SCUtility.Trim(firstTrnasfer.HOSTDESTINATION);
                bool first_tran_dest_is_port = scApp.PortStationBLL.OperateCatch.IsExist(first_tran_dest_port_id);
                if (!first_tran_dest_is_port) return (false, null);
                string first_tran_dest_adr_id = "";
                scApp.MapBLL.getAddressID(first_tran_dest_port_id, out first_tran_dest_adr_id);
                foreach (var tran in inQueueTransfers)
                {
                    string second_tran_source_port_id = tran.HOSTSOURCE;
                    bool second_tran_source_is_a_port = scApp.PortStationBLL.OperateCatch.IsExist(second_tran_source_port_id);
                    if (!second_tran_source_is_a_port) continue;

                    string second_tran_source_adr_id = string.Empty;
                    scApp.MapBLL.getAddressID(second_tran_source_port_id, out second_tran_source_adr_id);

                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"Start calculation distance, command id:{tran.ID.Trim()} command source port:{tran.HOSTDESTINATION?.Trim()}," +
                                        $"first transfer of dest port:{first_tran_dest_port_id} , prepare sencond source port:{second_tran_source_port_id}...",
                                  XID: tran.ID);
                    //var result = scApp.GuideBLL.getGuideInfo(first_tran_dest_adr_id, second_tran_source_adr_id);
                    var result = scApp.GuideBLL.IsRoadWalkable(first_tran_dest_adr_id, second_tran_source_adr_id, out int totalCost);
                    //double total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                    //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                    double total_section_distance = 0;
                    if (result)
                    {
                        //total_section_distance = result.guideSectionIds != null && result.guideSectionIds.Count > 0 ?
                        //                                scApp.SectionBLL.cache.GetSectionsDistance(result.guideSectionIds) : 0;
                        total_section_distance = totalCost;
                    }
                    else
                    {
                        total_section_distance = double.MaxValue;
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"Start calculation distance, command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                        $"first transfer of source port:{first_tran_dest_port_id} , prepare sencond port:{second_tran_source_port_id},distance:{total_section_distance}",
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

        const int TRAN_COMMAND_HIGH_PRIORITY_CONST = 99;
        public (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) FindVhAndCommand(List<VTRANSFER> transfers)
        {
            List<AVEHICLE> idle_vhs = scApp.VehicleBLL.cache.loadAllVh().ToList();
            scApp.VehicleBLL.cache.filterCanNotExcuteTranVh(ref idle_vhs, scApp.CMDBLL, E_VH_TYPE.None);
            //return FindVhAndCommandOrderByTransfer(idle_vhs, transfers);

            (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) findResult =
                default((bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer));
            var over_high_priority_tran = transfers.Where(tran => tran.PRIORITY_SUM >= TRAN_COMMAND_HIGH_PRIORITY_CONST)
                                                   .OrderByDescending(tran => tran.PRIORITY_SUM);
            //1.如果搬送命令有已經大於99的，就要從大於99的group來搜尋搬送的命令
            //2.如果還沒有大於99的就直接依照目前的遠近來選擇
            if (over_high_priority_tran.Count() > 0)
            {
                findResult = FindVhAndCommandOrderByTransfer(idle_vhs, over_high_priority_tran.ToList());
            }

            if (findResult.isFind)
            {
                return findResult;
            }
            else
            {
                return FindVhAndCommandOrderbyDistance(idle_vhs, transfers);
            }

        }
        private (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) FindVhAndCommandOrderByTransfer(List<AVEHICLE> vhs, List<VTRANSFER> transfers)
        {
            try
            {
                foreach (var tran in transfers)
                {
                    foreach (var vh in vhs)
                    {
                        string hostsource = tran.HOSTSOURCE;
                        string from_adr = string.Empty;

                        scApp.MapBLL.getAddressID(hostsource, out from_adr);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Start try find vh , command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} ...",
                                      XID: tran.ID);
                        //var result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, from_adr);
                        var result = scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, from_adr);
                        if (result)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                          Data: $"Find the vh success , command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                                $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr}.",
                                          XID: tran.ID);
                            return (true,
                                    vh,
                                    tran);
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                          Data: $"Find the vh fail continue check next vh, command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                                $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr}.",
                                          XID: tran.ID);
                        }
                    }
                }
                return (false,
                        null,
                        null);
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                   Data: ex);
                return (false,
                        null,
                        null);
            }
        }
        private (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) FindVhAndCommandOrderbyDistance(List<AVEHICLE> vhs, List<VTRANSFER> transfers)
        {
            AVEHICLE nearest_vh = null;
            VTRANSFER nearest_transfer = null;
            double minimum_cost = double.MaxValue;
            try
            {
                foreach (var tran in transfers)
                {
                    foreach (var vh in vhs)
                    {
                        string hostsource = tran.HOSTSOURCE;
                        string from_adr = string.Empty;

                        scApp.MapBLL.getAddressID(hostsource, out from_adr);
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Start try find vh , command id:{tran.ID.Trim()} command source port:{tran.HOSTSOURCE?.Trim()}," +
                                            $"vh:{vh.VEHICLE_ID} current adr:{vh.CUR_ADR_ID},from adr:{from_adr} ...",
                                      XID: tran.ID);
                        //var result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, from_adr);
                        bool result = scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, from_adr, out int totalCost);
                        double total_section_distance = 0;
                        if (result)
                        {
                            //total_section_distance = result.totalCost;
                            total_section_distance = totalCost;
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
                            if (HasOtherVhInTargetBlockOrWillGoTo(vh.VEHICLE_ID, from_adr).hasOtherVh)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                              Data: $"Try find the vh:[{vh.VEHICLE_ID}] to block load cst , but has other vh in this block.",
                                              XID: tran.ID);
                                continue;
                            }
                            nearest_transfer = tran;
                            nearest_vh = vh;
                            minimum_cost = total_section_distance;
                        }
                    }
                    if (minimum_cost < double.MaxValue && nearest_transfer != null && nearest_vh != null)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"command id:{nearest_transfer.ID.Trim()} ," +
                                            $"find the vh:{nearest_vh.VEHICLE_ID} to service",
                                      XID: tran.ID);
                        return (true,
                                nearest_vh,
                                nearest_transfer);
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
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(CMDBLL), Device: string.Empty,
                   Data: ex);
                nearest_vh = null;
                nearest_transfer = null;
            }
            return (nearest_vh != null && nearest_transfer != null,
                    nearest_vh,
                    nearest_transfer);
        }

        protected (bool hasOtherVh, List<string> vhs) HasOtherVhInTargetBlockOrWillGoTo(string vhID, string targetAdr)
        {
            var section = scApp.SectionBLL.cache.GetSectionsByAddress(targetAdr).FirstOrDefault();
            if (section == null) return (false, new List<string>());
            string sec_id = SCUtility.Trim(section.SEC_ID, true);
            var block_control_check_result = scApp.getCommObjCacheManager().IsBlockControlSection(sec_id);
            if (block_control_check_result.isBlockControlSec)
            {
                var block_sec_ids = block_control_check_result.enhanceInfo.EnhanceControlSections;
                var vhs = scApp.VehicleBLL.cache.loadAllVh();
                var other_in_block_vh = vhs.
                    Where(vh => (block_sec_ids.Contains(SCUtility.Trim(vh.CUR_SEC_ID, true)))
                                 && !SCUtility.isMatche(vh.VEHICLE_ID, vhID)).
                    ToList();
                if (other_in_block_vh != null && other_in_block_vh.Count() > 0)
                    return (true, other_in_block_vh.Select(vh => vh.VEHICLE_ID).ToList());

                ALINE line = scApp.getEQObjCacheManager().getLine();
                var acmds = line.CurrentExcuteCommand;
                if (acmds != null)
                {
                    var orther_vh_excute_in_block_cmd = acmds.Where(cmd => block_sec_ids.Contains(cmd.TragetSection(scApp.SectionBLL))
                                                                         && !SCUtility.isMatche(cmd.VH_ID, vhID)).ToList();
                    if (orther_vh_excute_in_block_cmd != null && orther_vh_excute_in_block_cmd.Count > 0)
                        return (true, orther_vh_excute_in_block_cmd.Select(cmd => SCUtility.Trim(cmd.ID, true)).ToList());
                }
            }
            return (false, new List<string>());
        }
        /// <summary>
        /// 尋找可以一起搬出agv Station的命令
        /// </summary>
        /// <param name="inQueueTransfers"></param>
        /// <param name="excutingTransfers"></param>
        /// <returns></returns>
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkBeforeOnTheWay_V2(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒Load Complete
            //2.接著再去找目前在Queue命令中，Host source port是有相同EQ的
            //3.找到後即可將兩筆命令進行配對
            List<VTRANSFER> can_excute_before_on_the_way_tran = excutingTransfers.
                                                                Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE).
                                                                ToList();
            //List<VTRANSFER> can_excute_before_on_the_way_tran = excutingTransfers;
            var in_queue_transfers_traget_not_agvstation =
                inQueueTransfers.Where(tran => !(tran.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType));
            foreach (var tran in can_excute_before_on_the_way_tran)
            {
                string excute_tran_eq_id = SCUtility.Trim(tran.getSourcePortEQID(scApp.PortStationBLL));
                var same_eq_ports = in_queue_transfers_traget_not_agvstation.
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
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkAfterOnTheWay_V2(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            //1.找出正在執行的命令中，且他的命令是還沒Load Complete
            //2.接著再去找目前在Queue命令中，目的地是有相同EQ的
            //3.找到後再從中找出一筆離執行中最近的命令，即可將兩筆命令進行配對
            List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.
                                                    Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE).
                                                    ToList();
            //List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers;
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

                var excute_tran_eq = tran.getTragetPortEQ(scApp.EqptBLL);
                if (excute_tran_eq is IAGVStationType)
                {
                    if (!(excute_tran_eq as IAGVStationType).IsReservation)
                    {
                        continue;
                    }
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
        public bool AssignTransferToVehicle(ATRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh, string forceAssignStPort)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            bool is_force_assign_st_port = !SCUtility.isEmpty(forceAssignStPort);
            if (is_force_assign_st_port)
            {
                APORTSTATION port_station = scApp.PortStationBLL.OperateCatch.getPortStation(forceAssignStPort);
                SCAppConstants.EqptType eq_type = port_station.GetEqptType(scApp.EqptBLL);
                if (eq_type != SCAppConstants.EqptType.AGVStation)
                {
                    CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                    check_result.Result.AppendLine($"port station:{port_station.PORT_ID} can't force assign.it type:{eq_type}");
                    //todo log...
                    return false;
                }
                assign_cmd.DESTINATION = SCUtility.Trim(port_station.ADR_ID, true);
                assign_cmd.DESTINATION_PORT = SCUtility.Trim(port_station.PORT_ID, true);
            }
            else
            {
                //var destination_info = checkAndRenameDestinationPortIfAGVStationReady(assign_cmd);
                var destination_info = checkAndRenameDestinationPortIfAGVStation(assign_cmd);
                if (destination_info.checkSuccess)
                {
                    assign_cmd.DESTINATION = destination_info.destinationAdrID;
                    assign_cmd.DESTINATION_PORT = destination_info.destinationPortID;
                }
                else
                {
                    CMDBLL.CommandCheckResult check_result = CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                    check_result.Result.AppendLine($" vh:{assign_cmd.VH_ID} creat command to db unsuccess. destination port :{SCUtility.Trim(assign_cmd.DESTINATION_PORT, true)} not ready");
                    //todo log...
                    return false;
                }
            }

            is_success = is_success && scApp.CMDBLL.checkCmd(assign_cmd);
            if (is_success)
            {
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
                            check_result.IsSuccess = false;
                        }
                    }
                }
            }
            return is_success;
        }

        private bool AssignTransferToVehicle_V2(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            //var destination_info = checkAndRenameDestinationPortIfAGVStationReady(assign_cmd);
            (bool checkSuccess, string destinationPortID, string destinationAdrID) destination_info =
                default((bool checkSuccess, string destinationPortID, string destinationAdrID));
            //if (DebugParameter.isNeedCheckPortReady)
            //    destination_info = checkAndRenameDestinationPortIfAGVStationReady(assign_cmd);
            //else
            //    destination_info = checkAndRenameDestinationPortIfAGVStationAuto(assign_cmd);
            destination_info = checkAndRenameDestinationPortIfAGVStation(assign_cmd);
            if (destination_info.checkSuccess)
            {
                assign_cmd.DESTINATION = destination_info.destinationAdrID;
                assign_cmd.DESTINATION_PORT = destination_info.destinationPortID;
            }
            else
            {
                //暫時針對有指定vh的才進行預先移動
                if (assign_cmd.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType)
                {
                    var sgv_station = assign_cmd.getTragetPortEQ(scApp.EqptBLL) as IAGVStationType;
                    if (SCUtility.isEmpty(sgv_station.BindingVh))
                        return false;
                }
                scApp.VehicleService.Command.preMoveToSourcePort(bestSuitableVh, assign_cmd);
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
        public (bool checkSuccess, string destinationPortID, string destinationAdrID) checkAndRenameDestinationPortIfAGVStation(ACMD assignCmd)
        {
            if (assignCmd.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType)
            {
                IAGVStationType unload_agv_station = assignCmd.getTragetPortEQ(scApp.EqptBLL) as IAGVStationType;
                //bool is_ready_double_port = unload_agv_station.IsReadyDoubleUnload;
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                   Data: $"check agv station:{unload_agv_station.getAGVStationID()},IsCheckPortReady:{unload_agv_station.IsCheckPortReady}");
                bool is_ready = false;
                List<APORTSTATION> port_stations = new List<APORTSTATION>();
                if (unload_agv_station.IsCheckPortReady)
                {
                    is_ready = unload_agv_station.IsReadySingleUnload;
                    port_stations = unload_agv_station.loadReadyAGVStationPort();
                }
                else
                {
                    is_ready = unload_agv_station.HasPortAuto;
                    port_stations = unload_agv_station.loadAutoAGVStationPorts();
                }
                if (!is_ready)
                {
                    return (false, "", "");
                }
                foreach (var port in port_stations)
                {
                    if (DebugParameter.isNeedCheckPortUpDateTime &&
                        port.Timestamp < unload_agv_station.ReservedSuccessTime)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(TransferService), Device: DEVICE_NAME_AGV,
                           Data: $"port id:[{port.PORT_ID}] not update ready, update time:[{port.Timestamp.ToString(SCAppConstants.DateTimeFormat_23)}]," +
                                 $"last reserved success time:[{unload_agv_station.ReservedSuccessTime.ToString(SCAppConstants.DateTimeFormat_23)}]");
                        continue;
                    }
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
                return (true, assignCmd.DESTINATION_PORT, assignCmd.DESTINATION);
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
        public (bool isSuccess, string result) tryInstallCarrierInVehicle(string vhID, string vhLocation, string carrierID)
        {
            try
            {
                //1.需確認該Carrier原本是不再車上車，且車上要有CST存在
                //1.1.如果原本就在車上就忽略該次的動作
                //1.2.如果原本不再車上則要將他Install到車上並且上報給CS
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
                    if (SCUtility.isMatche(check_has_carrier_on_location_result.onVhCarrier.ID, carrierID))
                    {
                        return (false, $"Location:{vhLocation} is already carrier:{check_has_carrier_on_location_result.onVhCarrier.ID} exist.");
                    }
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
        public (bool isSuccess, string result) ForceRemoveCarrierInVehicleByOP(string carrierID)
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
        public (bool isSuccess, string result) ForceRemoveCarrierInVehicleByAGV(string vhID, AGVLocation agvLocation, string carrierID)
        {
            try
            {
                AVEHICLE vh = scApp.VehicleBLL.cache.getVehicle(vhID);
                string location_real_id = vh.getLoctionRealID(agvLocation);

                var check_has_carrier_on_agv_loction = carrierBLL.db.hasCarrierOnVhLocation(location_real_id);
                if (!check_has_carrier_on_agv_loction.has)
                {
                    return (false, $"No carrier: on vh:{vhID} location:{agvLocation}");
                }
                var on_vh_of_carrier = check_has_carrier_on_agv_loction.onVhCarrier;
                carrierBLL.db.updateLocationAndState(on_vh_of_carrier.ID, "", E_CARRIER_STATE.OpRemove);
                scApp.ReportBLL.newReportCarrierRemoved
                    (vh.Real_ID, SCUtility.Trim(on_vh_of_carrier.ID, true), SCUtility.Trim(location_real_id, true), null);
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

        public void ScanByVTransfer_v3()
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

                    Task.Run(() => queueTimeOutCheck(un_finish_trnasfer));
                    if (un_finish_trnasfer == null || un_finish_trnasfer.Count == 0) return;
                    if (DebugParameter.CanAutoRandomGeneratesCommand ||
                        (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                    {
                        List<VTRANSFER> excuting_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE > E_TRAN_STATUS.Queue &&
                                                                tr.TRANSFERSTATE <= E_TRAN_STATUS.Transferring &&
                                                                !SCUtility.isEmpty(tr.VH_ID)).
                                                    ToList();
                        List<VTRANSFER> in_queue_transfer = un_finish_trnasfer.
                                                    Where(tr => tr.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                    ToList();

                        (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) before_on_the_way_cehck_result =
                           checkBeforeOnTheWay(in_queue_transfer, excuting_transfer);
                        if (before_on_the_way_cehck_result.isFind)
                        {
                            if (AssignTransferCommmand(before_on_the_way_cehck_result.bestSuitabletransfer,
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
                            if (AssignTransferCommmand(after_on_the_way_cehck_result.bestSuitabletransfer,
                                                       after_on_the_way_cehck_result.bestSuitableVh))
                            {
                                scApp.VehicleService.Command.Scan();
                                return;
                            }
                        }

                        //用來搜尋第一筆從AGV St.出來的命令
                        try
                        {
                            //如果是在找Source非EQ Port的命令時，要用Port Priority做排序來幫助雙車(6/9)在跑時，如果370過於忙碌，都沒有車子可以去服務450的問題
                            var traget_not_agv_st_in_queue_transfer = in_queue_transfer.Where(tran => !(tran.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType))
                                                                 .OrderByDescending(tran => tran.PORT_PRIORITY)
                                                                 .ToList();

                            foreach (VTRANSFER first_waitting_excute_mcs_cmd in traget_not_agv_st_in_queue_transfer)
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
                                    //需確認是否已經有其他車在搬送同SourcePort的CST，且還沒到Transferring
                                    scApp.MapBLL.getAddressID(hostsource, out from_adr, out vh_type);
                                    var check_has_some_source_vh_excuting = findOtherSameSourcePortCmdExcuteAndVhCanService(hostsource, excuting_transfer);
                                    if (check_has_some_source_vh_excuting.hasFind)
                                    {
                                        bestSuitableVh = check_has_some_source_vh_excuting.vh;
                                    }
                                    else
                                    {
                                        bestSuitableVh = scApp.VehicleBLL.cache.findBestSuitableVhStepByStepFromAdr(scApp.GuideBLL, scApp.CMDBLL, from_adr, vh_type);
                                        if (bestSuitableVh != null)
                                        {
                                            var check_has_orther_vhs_in_block_result = HasOtherVhInTargetBlockOrWillGoTo(bestSuitableVh.VEHICLE_ID, from_adr);
                                            if (check_has_orther_vhs_in_block_result.hasOtherVh)
                                            {
                                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                              Data: $"Try find the vh:[{bestSuitableVh.VEHICLE_ID}] to block load cst , but has other vh in this block (Finial).",
                                                              XID: first_waitting_excute_mcs_cmd.ID);
                                                List<string> other_in_block_vhs = check_has_orther_vhs_in_block_result.vhs;
                                                foreach (string vh_id in other_in_block_vhs)
                                                {
                                                    AVEHICLE in_block_vh = scApp.VehicleBLL.cache.getVehicle(vh_id);
                                                    if (scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, in_block_vh))
                                                    {
                                                        bool is_success = AssignTransferCommmand(first_waitting_excute_mcs_cmd,
                                                                                                 in_block_vh);
                                                        if (is_success)
                                                        {
                                                            scApp.VehicleService.Command.Scan();
                                                            return;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                                                      Data: $"In block vh:[{bestSuitableVh.VEHICLE_ID}] not ready to assign transfer command.",
                                                                      XID: first_waitting_excute_mcs_cmd.ID);
                                                    }
                                                }
                                                continue;
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByRealID(hostsource);
                                    bestSuitableVh = scApp.VehicleBLL.cache.getVehicleByLocationRealID(hostsource);
                                    if (bestSuitableVh.IsError ||
                                        bestSuitableVh.MODE_STATUS != VHModeStatus.AutoRemote)
                                    {
                                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                           Data: $"Has transfer command:{SCUtility.Trim(first_waitting_excute_mcs_cmd.ID, true)} for vh:{bestSuitableVh.VEHICLE_ID}" +
                                                 $"but it error happend or not auto remote.",
                                           VehicleID: bestSuitableVh.VEHICLE_ID);
                                        continue;
                                    }
                                }



                                if (bestSuitableVh != null)
                                {
                                    if (AssignTransferCommmand(first_waitting_excute_mcs_cmd, bestSuitableVh))
                                    {
                                        scApp.VehicleService.Command.Scan();
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error(ex, "Exception");
                        }
                        //1.確認是否有要回AGV Station的命令
                        //2.有的話要確認一下，是否已有預約成功
                        //3.預約成功後則看該Station是否已經可以讓AGV執行Double Unload。
                        //4.確認是否有車子已經準備服務或正在過去
                        //3-1.有，
                        //3-2.無，則直接下達Move指令先移過去等待
                        var check_result = checkAndFindReserveSuccessUnloadToAGVStationTransfer(un_finish_trnasfer);
                        if (check_result.isFind)
                        {
                            foreach (var tran_group_by_agvstation in check_result.tranGroupsByAGVStation)
                            {
                                AGVStation reserving_unload_agv_station = tran_group_by_agvstation.Key;
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                   Data: $"agv station:{reserving_unload_agv_station.getAGVStationID()},is reserve success .check can start first assign this group...");
                                List<VTRANSFER> transfer_group = tran_group_by_agvstation.ToList();

                                List<VTRANSFER> tran_excuting_in_group = transfer_group.
                                                                Where(tran => tran.TRANSFERSTATE > E_TRAN_STATUS.Queue).
                                                                ToList();
                                List<VTRANSFER> tran_queue_in_group = transfer_group.
                                                              Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).
                                                              OrderBy(tran => tran.CARRIER_INSER_TIME).
                                                              ToList();


                                var try_find_carrier_on_vh_result = tryFindAssignOnVhCarrier(tran_queue_in_group);
                                if (try_find_carrier_on_vh_result.hasFind)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"find has carrier of vh:{try_find_carrier_on_vh_result.hasCarrierOfVh.VEHICLE_ID}, start assign command to vh");

                                    bool is_success = AssignTransferCommmand(try_find_carrier_on_vh_result.tran,
                                                                             try_find_carrier_on_vh_result.hasCarrierOfVh);
                                    if (is_success)
                                        return;
                                }

                                //如果該Group已經有準備被執行/執行中的命令時，則代表該AGV Station已經有到vh去服務了，
                                //而等待被執行/執行中只有一筆且那一筆已經是Initial的時候(代表已經成功下給車子)
                                //就可以再以這一筆當出發點找出它鄰近的一筆再下給車子
                                if (tran_excuting_in_group.Count > 0)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleService), Device: DEVICE_NAME_AGV,
                                       Data: $"agv station:{reserving_unload_agv_station.getAGVStationID()} has cmd excute. can't start first assign");

                                }
                                else
                                {
                                    //var find_result = FindNearestVhAndCommand(tran_queue_in_group);
                                    (bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer) find_result =
                                        default((bool isFind, AVEHICLE nearestVh, VTRANSFER nearestTransfer));

                                    find_result = FindVhAndCommand(tran_queue_in_group);

                                    if (find_result.isFind)
                                    {
                                        bool is_success = AssignTransferCommmand(find_result.nearestTransfer,
                                                                                 find_result.nearestVh);
                                        if (is_success)
                                            return;
                                        //continue;
                                    }
                                }
                            }
                        }

                        foreach (VTRANSFER queue_tran in in_queue_transfer)
                        {
                            //int AccumulateTime_minute = 5;
                            int AccumulateTime_minute = SystemParameter.TransferCommandTimePriorityIncrement;
                            int current_time_priority = ((int)((DateTime.Now - queue_tran.CMD_INSER_TIME).TotalMinutes) * AccumulateTime_minute);
                            if (current_time_priority > queue_tran.TIME_PRIORITY)
                            {
                                //int change_priority = current_time_priority - queue_tran.TIME_PRIORITY;
                                //int new_sum_priority = queue_tran.PRIORITY_SUM + change_priority;
                                //scApp.CMDBLL.updateCMD_MCS_TimePriority(queue_tran.ID, current_time_priority, new_sum_priority);
                                updateTranTimePriority(queue_tran, current_time_priority);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncTranCmdPoint, 0);
                }
            }
        }
        public bool updateTranTimePriority(VTRANSFER tran, int timePriority)
        {
            try
            {
                int new_sum_priority = tran.PORT_PRIORITY + tran.PRIORITY + timePriority;
                return scApp.CMDBLL.updateCMD_MCS_TimePriority(tran.ID, timePriority, new_sum_priority);
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
                return false;
            }
        }

        /// <summary>
        /// 選出可以順便帶走的命令
        ///1.找出正在執行的命令中，且他的命令是還沒到達Load Complete
        ///2.接著再去找目前在Queue命令中，目的地是是有相同AGV St./EQ的
        ///3.找到後即可將兩筆命令進行配對
        /// </summary>
        /// <param name="inQueueTransfers"></param>
        /// <param name="excutingTransfers"></param>
        /// <returns></returns>
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkBeforeOnTheWay(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;

            //List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.
            //                                        Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE).
            //                                        ToList();
            List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.
                                                    Where(tr => tr.COMMANDSTATE < ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE ||
                                                                (tr.COMMANDSTATE == ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE &&
                                                                 SCUtility.isMatche(tr.PAUSEFLAG, TransferBLL.TRANSFER_PAUSE_FLAG))
                                                          ).
                                                    ToList();

            foreach (var excute_tran in can_excute_after_on_the_way_tran)
            {
                string source = excute_tran.HOSTSOURCE;
                string dest = excute_tran.HOSTDESTINATION;
                string best_suitable_vh_id = SCUtility.Trim(excute_tran.VH_ID, true);
                best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);

                if (!scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh, excute_tran.GetTransferDir()))
                {
                    best_suitable_vh = null;
                    continue;
                }

                var excute_tran_eq = excute_tran.getTragetPortEQ(scApp.EqptBLL);
                if (excute_tran_eq is IAGVStationType)
                {
                    var agv_st = (excute_tran_eq as IAGVStationType);
                    if (!agv_st.IsReservation)
                    {
                        continue;
                    }
                    if (agv_st.TransferMode == E_AGVStationTranMode.MoreOut) //當AGV Station為MoreOut時，每次僅能帶回一顆CST，
                                                                             //因此若已經有令命在搬送代表已經有一顆準備回去了
                    {
                        continue;
                    }
                }
                string excute_tran_eq_id = SCUtility.Trim(excute_tran.getTragetPortNodeID(scApp.PortStationBLL, scApp.EqptBLL));
                //var same_eq_ports = inQueueTransfers.
                //                    Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getTragetPortEQID(scApp.PortStationBLL),
                //                                                              excute_tran_eq_id)).
                //                    ToList();
                var same_eq_ports = inQueueTransfers.
                                    Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getTragetPortNodeID(scApp.PortStationBLL, scApp.EqptBLL),
                                                                              excute_tran_eq_id)).
                                    ToList();

                var check_result = FindNearestTransferBySourcePort(excute_tran, same_eq_ports);
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
        /// <summary>
        /// 尋找可以命令結束後，可順便一起帶走的CST
        /// 1.找出尚未結束的命令且目標為AGV st的
        /// 2.再找出Queue命令-Source port = Excute命令-Target port
        /// 3.找到後確認AGV還可接收該Type的命令時，即可下達
        /// </summary>
        /// <param name="inQueueTransfers"></param>
        /// <param name="excutingTransfers"></param>
        /// <returns></returns>
        private (bool isFind, AVEHICLE bestSuitableVh, VTRANSFER bestSuitabletransfer) checkAfterOnTheWay(List<VTRANSFER> inQueueTransfers, List<VTRANSFER> excutingTransfers)
        {
            AVEHICLE best_suitable_vh = null;
            VTRANSFER best_suitable_transfer = null;
            bool is_success = false;
            List<VTRANSFER> can_excute_after_on_the_way_tran = excutingTransfers.Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType
                                                                                            && tran.COMMANDSTATE > ATRANSFER.COMMAND_STATUS_BIT_INDEX_LOAD_COMPLETE).ToList();

            foreach (var excute_tran in can_excute_after_on_the_way_tran)
            {
                string source = excute_tran.HOSTSOURCE;
                string dest = excute_tran.HOSTDESTINATION;
                string best_suitable_vh_id = SCUtility.Trim(excute_tran.VH_ID, true);
                best_suitable_vh = scApp.VehicleBLL.cache.getVehicle(best_suitable_vh_id);

                if (!scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, best_suitable_vh, CMDBLL.CommandTranDir.OutAGVStation))
                {
                    best_suitable_vh = null;
                    continue;
                }

                string excute_tran_target_node_id = SCUtility.Trim(excute_tran.getTragetPortNodeID(scApp.PortStationBLL, scApp.EqptBLL));
                var same_node_tran = inQueueTransfers.
                                    Where(in_queue_tran => SCUtility.isMatche(in_queue_tran.getSourcePortNodeID(scApp.PortStationBLL, scApp.EqptBLL),
                                                                              excute_tran_target_node_id)).
                                    ToList();

                var check_result = FindNearestTransferByTargetPort(excute_tran, same_node_tran);
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


        bool AssignTransferCommmand(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            var tran_assigner = GetTranAssigner(bestSuitableVh.VEHICLE_TYPE);
            return tran_assigner.AssignTransferToVehicle(waittingExcuteMcsCmd, bestSuitableVh);
        }
        ITranAssigner GetTranAssigner(E_VH_TYPE vhType)
        {
            switch (vhType)
            {
                case E_VH_TYPE.Swap:
                    return SwapTranAssigner;
                default:
                    return NormalTranAssigner;
            }

        }

    }

    interface ITranAssigner
    {
        bool AssignTransferToVehicle(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh);
    }

    public class TranAssignerNormal : ITranAssigner
    {
        SCApplication scApp = null;
        protected NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        public TranAssignerNormal(SCApplication _scApp)
        {
            scApp = _scApp;
        }
        public bool AssignTransferToVehicle(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);
            //var destination_info = checkAndRenameDestinationPortIfAGVStationReady(assign_cmd);
            (bool checkSuccess, string destinationPortID, string destinationAdrID) destination_info =
                default((bool checkSuccess, string destinationPortID, string destinationAdrID));
            //if (DebugParameter.isNeedCheckPortReady)
            //    destination_info = checkAndRenameDestinationPortIfAGVStationReady(assign_cmd);
            //else
            //    destination_info = checkAndRenameDestinationPortIfAGVStationAuto(assign_cmd);
            destination_info = scApp.TransferService.checkAndRenameDestinationPortIfAGVStation(assign_cmd);
            if (destination_info.checkSuccess)
            {
                assign_cmd.DESTINATION = destination_info.destinationAdrID;
                assign_cmd.DESTINATION_PORT = destination_info.destinationPortID;
            }
            else
            {
                //暫時針對有指定vh的才進行預先移動
                if (assign_cmd.getTragetPortEQ(scApp.EqptBLL) is IAGVStationType)
                {
                    var sgv_station = assign_cmd.getTragetPortEQ(scApp.EqptBLL) as IAGVStationType;
                    if (SCUtility.isEmpty(sgv_station.BindingVh))
                        return false;
                }
                scApp.VehicleService.Command.preMoveToSourcePort(bestSuitableVh, assign_cmd);
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
    }

    public class TransferAssignerSwap : ITranAssigner
    {
        SCApplication scApp = null;
        protected NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public TransferAssignerSwap(SCApplication _scApp)
        {
            scApp = _scApp;
        }
        public bool AssignTransferToVehicle(VTRANSFER waittingExcuteMcsCmd, AVEHICLE bestSuitableVh)
        {
            bool is_success = true;
            ACMD assign_cmd = waittingExcuteMcsCmd.ConvertToCmd(scApp.PortStationBLL, scApp.SequenceBLL, bestSuitableVh);

            //is_success = is_success && scApp.CMDBLL.checkCmdSwap(assign_cmd);
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
    }


}
