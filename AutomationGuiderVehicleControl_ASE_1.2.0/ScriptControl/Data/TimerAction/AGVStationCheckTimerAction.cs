// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="EqptAliveCheck.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using com.mirle.ibg3k0.bcf.Controller;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    /// <summary>
    /// Class EqptAliveCheck.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.TimerAction.ITimerAction" />
    class AGVStationCheckTimerAction : ITimerAction
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;
        /// <summary>
        /// The line
        /// </summary>
        ALINE line = null;


        /// <summary>
        /// Initializes a new instance of the <see cref="EqptAliveCheck"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="intervalMilliSec">The interval milli sec.</param>
        public AGVStationCheckTimerAction(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }

        /// <summary>
        /// Initializes the start.
        /// </summary>
        public override void initStart()
        {
            scApp = SCApplication.getInstance();
            line = scApp.getEQObjCacheManager().getLine();
        }

        private long syncPoint = 0;
        /// <summary>
        /// Timer Action的執行動作
        /// </summary>
        /// <param name="obj">The object.</param>
        public override void doProcess(object obj)
        {
            if (scApp.getEQObjCacheManager().getLine().ServiceMode
                != SCAppConstants.AppServiceMode.Active)
                return;
            try
            {
                scApp.PortStationBLL.updatePortStatusByRedis();

                if (DebugParameter.CanAutoRandomGeneratesCommand ||
                (scApp.getEQObjCacheManager().getLine().SCStats == ALINE.TSCState.AUTO && scApp.getEQObjCacheManager().getLine().MCSCommandAutoAssign))
                {

                    //1.確認是否有要回AGV Station的命令
                    //2-1.有的話開始透過Web API跟對應的OHBC詢問是否可以開始搬送
                    //2-2.沒有，則持續跟OHBC通報目前沒有Queue的命令要過去
                    //3.如果有預約成功，則將該Station的is reservation = true
                    //var v_trans = line.CurrentExcuteTransferCommand;
                    var v_trans = scApp.getEQObjCacheManager().getLine().CurrentExcuteTransferCommand.ToList();
                    var agv_stations = scApp.EqptBLL.OperateCatch.loadAllAGVStation();
                    foreach (var agv_station in agv_stations)
                    {
                        if (agv_station == null)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                               Data: $"Check agv station has null.");
                            continue;
                        }
                        //testGuideInfo();


                        switch (agv_station.DeliveryMode)
                        {
                            case E_AGVStationDeliveryMode.Swap:
                                Task.Run(() => agvStationCheckForSwap(v_trans, agv_station));
                                break;
                            default:
                                bool is_Specify_service_vh = !SCUtility.isEmpty(agv_station.BindingVh);
                                if (is_Specify_service_vh)
                                    Task.Run(() => agvStationCheck(v_trans, agv_station));
                                else
                                    Task.Run(() => agvStationCheckNew(v_trans, agv_station));
                                break;
                        }


                        //if (v_trans != null)
                        //{
                        //    var unfinish_target_port_command = v_trans.
                        //                                       Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                        //                                       ToList();

                        //    var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        //    int excute_target_pott_count = excute_target_port_tran.Count();
                        //    if (excute_target_pott_count > 0)
                        //    {
                        //        agv_station.IsTransferUnloadExcuting = true;
                        //        if (excute_target_pott_count >= 2)
                        //        {
                        //            agv_station.IsReservation = false;
                        //        }
                        //        continue;
                        //    }
                        //    else
                        //    {
                        //        if (agv_station.IsTransferUnloadExcuting)
                        //        {
                        //            agv_station.IsTransferUnloadExcuting = false;
                        //            agv_station.IsReservation = false;
                        //        }
                        //    }

                        //    var queue_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).ToList();
                        //    if (queue_target_port_tran.Count() > 0)
                        //    {
                        //        if (agv_station.IsReservation)
                        //        {
                        //            //not thing...
                        //        }
                        //        else
                        //        {
                        //            var cehck_has_vh_go_tran_reault = scApp.TransferService.FindNearestVhAndCommand(queue_target_port_tran);
                        //            //如果有命令還在Queue的話，則嘗試找看看能不能有車子來服務，有的話就可以去詢問看看
                        //            if (cehck_has_vh_go_tran_reault.isFind)
                        //            {
                        //                int carry_cst = 0;
                        //                int current_excute_task = 0;
                        //                AVEHICLE service_vh = scApp.VehicleBLL.cache.getVehicle(agv_station.BindingVh);
                        //                if (service_vh != null)
                        //                {
                        //                    if (service_vh.HAS_CST_L)
                        //                        carry_cst++;
                        //                    if (service_vh.HAS_CST_R)
                        //                        carry_cst++;
                        //                }
                        //                current_excute_task = carry_cst + unfinish_target_port_command.Count();
                        //                //bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, unfinish_target_port_command.Count(), false);
                        //                bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                        //                if (is_reserve_success)
                        //                {
                        //                    agv_station.IsReservation = true;
                        //                    scApp.TransferService.ScanByVTransfer_v2();
                        //                }
                        //            }
                        //            else
                        //            {
                        //                //todo log...
                        //            }
                        //        }
                        //    }
                        //    else
                        //    {
                        //        //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                        //        agv_station.IsReservation = false;
                        //    }
                        //}
                        //else
                        //{
                        //    //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                        //    agv_station.IsReservation = false;
                        //}
                    }
                }

            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception");
            }
        }

        private void testGuideInfo()
        {
            var getAddresses = scApp.AddressesBLL.cache.GetAddresses();
            Random rnd_Index = new Random(Guid.NewGuid().GetHashCode());
            int task_RandomIndex1 = rnd_Index.Next(getAddresses.Count - 1);
            int task_RandomIndex2 = rnd_Index.Next(getAddresses.Count - 1);
            var addres1 = getAddresses[task_RandomIndex1];
            var addres2 = getAddresses[task_RandomIndex2];
            bool is_walkable = scApp.GuideBLL.IsRoadWalkable(addres1.ADR_ID, addres2.ADR_ID);

        }

        private void agvStationCheck(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            if (System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 1) == 0)
            {
                try
                {
                    agv_station.TransferMode = E_AGVStationTranMode.None;

                    int current_excute_task = 0;
                    AVEHICLE service_vh = scApp.VehicleBLL.cache.getVehicle(agv_station.BindingVh);
                    if (service_vh != null)
                    {
                        if (service_vh.IsError)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                               Data: $"vh:{service_vh.VEHICLE_ID} has error happend.pass this one ask agv station");
                            return;
                        }
                        if (service_vh.MODE_STATUS != ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                               Data: $"vh:{service_vh.VEHICLE_ID} not auto remote.pass this one ask agv station");
                            return;
                        }
                        //if (service_vh != null)
                        //{
                        if (v_trans != null && v_trans.Count > 0)
                        {
                            int current_vh_carrier_count = 0;
                            if (service_vh.HAS_CST_L)
                            {
                                bool cst_l_is_in_commanding = v_trans.Where(cmd => SCUtility.isMatche(cmd.CARRIER_ID, service_vh.CST_ID_L)).Count() > 0;
                                //如果身上的CST 不再執行的命令中，則需要用CST 來佔一個Command的位置
                                if (!cst_l_is_in_commanding)
                                {
                                    current_vh_carrier_count++;
                                }
                            }
                            if (service_vh.HAS_CST_R)
                            {
                                bool cst_r_is_in_commanding = v_trans.Where(cmd => SCUtility.isMatche(cmd.CARRIER_ID, service_vh.CST_ID_R)).Count() > 0;
                                //如果身上的CST 不再執行的命令中，則需要用CST 來佔一個Command的位置
                                if (!cst_r_is_in_commanding)
                                {
                                    current_vh_carrier_count++;
                                }
                            }
                            current_excute_task += current_vh_carrier_count;
                        }
                        else
                        {
                            if (service_vh.HAS_CST_L)
                                current_excute_task++;
                            if (service_vh.HAS_CST_R)
                                current_excute_task++;
                        }
                        //if (service_vh.HAS_CST_L)
                        //    current_excute_task++;
                        //if (service_vh.HAS_CST_R)
                        //    current_excute_task++;
                        //}
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"start check agv station:[{agv_station.getAGVStationID()}] status...");
                    if (v_trans != null && v_trans.Count > 0)
                    {
                        //var unfinish_source_port_command = v_trans.
                        //                                   Where(tran => tran.getSourcePortEQ(scApp.EqptBLL) == agv_station).
                        //                                   ToList();
                        var unfinish_source_port_command = v_trans.
                                                           Where(tran => SCUtility.isMatche(tran.getSourcePortEQID(scApp.PortStationBLL), agv_station.EQPT_ID)).
                                                           ToList();
                        var excute_source_port_tran = unfinish_source_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        int excute_source_pott_count = excute_source_port_tran.Count();
                        if (excute_source_pott_count > 0)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                               Data: $"agv station:[{agv_station.getAGVStationID()}] has out of stock command excute,pass ask reserve");
                            agv_station.IsReservation = false;
                            agv_station.IsTransferUnloadExcuting = false;
                            return;
                        }


                        var unfinish_target_port_command = v_trans.
                                                           Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                                                           ToList();

                        var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        int excute_target_pott_count = excute_target_port_tran.Count();
                        if (excute_target_pott_count > 0)
                        {
                            agv_station.IsTransferUnloadExcuting = true;
                            if (excute_target_pott_count >= 2)
                            {
                                agv_station.IsReservation = false;
                            }
                            string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                            scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                                        $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                            return;
                        }
                        else
                        {
                            if (agv_station.IsTransferUnloadExcuting)
                            {
                                agv_station.IsTransferUnloadExcuting = false;
                                agv_station.IsReservation = false;
                            }
                        }

                        var queue_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).ToList();
                        if (queue_target_port_tran.Count() > 0)
                        {
                            if (agv_station.IsReservation)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"agv station:[{agv_station.getAGVStationID()}] is reservation");
                                //not thing...
                                if (agv_station.IsReservedTimeOut)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"agv station:[{agv_station.getAGVStationID()}] is reserved time out, reset it and then ask again...");
                                    agv_station.IsReservation = false;
                                    string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                                    scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                                                                         $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                                }
                            }
                            else
                            {
                                bool has_source_on_vh = hasSourceIsVh(queue_target_port_tran);
                                //var cehck_has_vh_go_tran_reault = scApp.TransferService.FindNearestVhAndCommand(queue_target_port_tran);
                                var cehck_has_vh_go_tran_reault = scApp.TransferService.FindVhAndCommand(queue_target_port_tran);
                                //如果有命令還在Queue的話，則嘗試找看看能不能有車子來服務，有的話就可以去詢問看看
                                //if (cehck_has_vh_go_tran_reault.isFind)
                                if (cehck_has_vh_go_tran_reault.isFind || has_source_on_vh)
                                {
                                    //int current_excute_task = 0;
                                    current_excute_task += unfinish_target_port_command.Count();
                                    //bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, unfinish_target_port_command.Count(), false);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}] ...");
                                    bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}] ,reserve result{is_reserve_success}");
                                    if (is_reserve_success)
                                    {
                                        agv_station.IsReservation = true;
                                        //scApp.TransferService.ScanByVTransfer_v2();
                                        scApp.TransferService.ScanByVTransfer_v3();
                                    }
                                }
                                else
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}],but not vh can service it, pass ask process.");
                                }
                            }
                        }
                        else
                        {
                            bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                            //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                            agv_station.IsReservation = false;
                            checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, current_excute_task);
                        }
                    }
                    else
                    {
                        bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                        //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                        agv_station.IsReservation = false;
                        checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, current_excute_task);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 0);
                }
            }
        }
        private (bool hasVh, AVEHICLE canServiceVh) checkHasVhCanServiceAGVStation(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh().ToList();
            //scApp.VehicleBLL.cache.filterCanNotExcuteTranVh(ref idle_vhs, scApp.CMDBLL, E_VH_TYPE.None);
            foreach (var vh in vhs)
            {
                //bool can_assign_tran_cmd = scApp.VehicleBLL.cache.canAssignTransferCmd(scApp.CMDBLL, vh, true);
                bool can_assign_tran_cmd = vh.IsStanby(scApp.CMDBLL);
                if (!can_assign_tran_cmd)
                {
                    //但是AGV車上有一顆該Station的CST命令，就應該讓他去詢問
                    //if (v_trans != null && v_trans.Count > 0)
                    //{
                    //    var on_vh_cmd_and_is_go_to_this_st =
                    //        v_trans.Where(v_tran =>
                    //                      v_tran.TRANSFERSTATE == E_TRAN_STATUS.Queue &&
                    //                      (SCUtility.isMatche(v_tran.CARRIER_ID, vh.CST_ID_L) || SCUtility.isMatche(v_tran.CARRIER_ID, vh.CST_ID_R)) &&
                    //                      SCUtility.isMatche(v_tran.HOSTDESTINATION, agv_station.getAGVStationID()))
                    //                      .FirstOrDefault();
                    //    if (on_vh_cmd_and_is_go_to_this_st != null)
                    //    {
                    //        return true;
                    //    }
                    //}
                    continue;
                }

                //var path_check_result = scApp.GuideBLL.getGuideInfo(vh.CUR_ADR_ID, agv_station.AddressID);
                var is_walkable = scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, agv_station.AddressID);
                if (is_walkable)
                {

                    //確認身上沒有無命令的CST
                    if (hasNotCmdCstInVh(v_trans, vh.CST_ID_L))
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Has no transfer command of cst in vh:[{vh.VEHICLE_ID}] cst id:{vh.CST_ID_L},pass this one to service st.:{agv_station.getAGVStationID()}.");
                        continue;
                    }
                    if (hasNotCmdCstInVh(v_trans, vh.CST_ID_R))
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: string.Empty,
                                      Data: $"Has no transfer command of cst in vh:[{vh.VEHICLE_ID}] cst id:{vh.CST_ID_R},pass this one to service st.:{agv_station.getAGVStationID()}.");
                        continue;
                    }

                    if (v_trans != null && v_trans.Count > 0)
                    {
                        var on_vh_cmd_and_is_go_to_this_st =
                            v_trans.Where(v_tran =>
                                          v_tran.TRANSFERSTATE == E_TRAN_STATUS.Queue &&
                                          (SCUtility.isMatche(v_tran.CARRIER_ID, vh.CST_ID_L) || SCUtility.isMatche(v_tran.CARRIER_ID, vh.CST_ID_R)) &&
                                          SCUtility.isMatche(v_tran.HOSTDESTINATION, agv_station.getAGVStationID()))
                                          .FirstOrDefault();
                        if (on_vh_cmd_and_is_go_to_this_st != null)
                        {
                            return (true, vh);
                        }
                    }

                    return (true, vh);
                }
                else
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(CMDBLL), Device: string.Empty,
                                  Data: $"vh:[{vh.VEHICLE_ID}] current adr:[{vh.CUR_ADR_ID}] no path go to agv st.[{agv_station.getAGVStationID()}({agv_station.AddressID})],can not service it.");
                }
            }
            return (false, null);
        }
        /// <summary>
        /// 用來確認是否有無命令的CST停留在車子上
        /// </summary>
        /// <param name="v_trans"></param>
        /// <param name="cstID"></param>
        /// <returns></returns>
        private bool hasNotCmdCstInVh(List<VTRANSFER> v_trans, string cstID)
        {
            if (SCUtility.isEmpty(cstID)) return false;
            var tran_cmd_count = v_trans.Where(tran => SCUtility.isMatche(tran.CARRIER_ID, cstID)).Count();
            return tran_cmd_count == 0;
        }
        private void agvStationCheckNew(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            if (System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 1) == 0)
            {
                try
                {
                    agv_station.TransferMode = E_AGVStationTranMode.None;

                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"start check agv station:[{agv_station.getAGVStationID()}] status(not binding vh)...");
                    //1.是否有車子可以來服務這個port
                    //2.可以服務這個port的車，車上是否有都無命令的cst在車上=>有，對該port不進行詢問
                    var check_has_vh_can_service_result = checkHasVhCanServiceAGVStation(v_trans, agv_station);
                    if (!check_has_vh_can_service_result.hasVh)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: "AGVC",
                                      Data: $"No vh can service agv station:{agv_station.getAGVStationID()}, pass this one ask");
                        checkCanCloseReservationFlagWhenNoVhCanService(v_trans, agv_station);
                        return;
                    }

                    if (v_trans != null && v_trans.Count > 0)
                    {

                        var unfinish_target_port_command = v_trans.
                                                           Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                                                           ToList();

                        var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        int excute_target_pott_count = excute_target_port_tran.Count();
                        if (excute_target_pott_count > 0)
                        {
                            agv_station.IsTransferUnloadExcuting = true;
                            if (excute_target_pott_count >= 2)
                            {
                                agv_station.IsReservation = false;
                            }
                            string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                            scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                                        $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                            return;
                        }
                        else
                        {
                            if (agv_station.IsTransferUnloadExcuting)
                            {
                                agv_station.IsTransferUnloadExcuting = false;
                                agv_station.IsReservation = false;
                            }
                        }

                        var queue_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).ToList();
                        if (queue_target_port_tran.Count() > 0)
                        {
                            if (agv_station.IsReservation)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"agv station:[{agv_station.getAGVStationID()}] is reservation");
                                //not thing...
                                if (agv_station.IsReservedTimeOut)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"agv station:[{agv_station.getAGVStationID()}] is reserved time out, reset it and then ask again...");
                                    agv_station.IsReservation = false;
                                    string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                                    scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                                                                         $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                                }
                            }
                            else
                            {
                                int current_excute_task = 0;
                                current_excute_task = unfinish_target_port_command.Count();
                                //bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, unfinish_target_port_command.Count(), false);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}] ...");
                                bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}] ,reserve result{is_reserve_success}");
                                if (is_reserve_success)
                                {
                                    agv_station.IsReservation = true;
                                    //scApp.TransferService.ScanByVTransfer_v2();
                                    scApp.TransferService.ScanByVTransfer_v3();
                                }
                            }
                        }
                        else
                        {
                            bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, 0, false);
                            agv_station.IsReservation = false;
                            //checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, 0);
                        }
                    }
                    else
                    {
                        bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, 0, false);
                        agv_station.IsReservation = false;
                        //checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, 0);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 0);
                }
            }
        }

        private bool hasSourceIsVh(List<VTRANSFER> transfers)
        {
            if (transfers == null || transfers.Count == 0) return false;
            int count = transfers.Where(tran => tran.IsSourceOnVh(scApp.VehicleBLL)).Count();
            return count != 0;
        }


        private void checkIsNeedPreMoveToAGVStation(AGVStation agv_station, AVEHICLE serviceVh, bool is_reserve_success, int current_excute_task)
        {
            if (current_excute_task == 0 && !is_reserve_success)
            {
                agv_station.IsOutOfStock = true;
                if (serviceVh != null)
                {
                    var virtrueagv_station = agv_station.getAGVVirtruePort();
                    if (SCUtility.isMatche(serviceVh.CUR_ADR_ID, virtrueagv_station.ADR_ID)) return;
                    scApp.VehicleService.Command.Move(serviceVh.VEHICLE_ID, virtrueagv_station.ADR_ID);
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"agv station:[{agv_station.getAGVStationID()}] will cst out,service vh:{serviceVh.VEHICLE_ID} pre move to adr:{virtrueagv_station.ADR_ID}");
                }
            }
            else
            {
                agv_station.IsOutOfStock = false;
            }

            string stock_of_out_time_out_alarm_code = getAGVStationStockOfOutTimeOutCode(agv_station.getAGVStationID());
            if (agv_station.IsOutOfStockTimeOut)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                   Data: $"agv station:[{agv_station.getAGVStationID()}] is out of stock time out.");
                scApp.LineService.ProcessAlarmReport("AGVC", stock_of_out_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                                                     $"AGV Station:[{agv_station.getAGVStationID()}] out of stock timeout.");
            }
            else
            {
                scApp.LineService.ProcessAlarmReport("AGVC", stock_of_out_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                                                     $"AGV Station:[{agv_station.getAGVStationID()}] out of stock timeout.");
            }
        }

        private void agvStationCheckForSwap(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            if (System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 1) == 0)
            {
                try
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"start check agv station:[{agv_station.getAGVStationID()}] status(swap mode)...");
                    //1.是否有車子可以來服務這個port(沒有在執行移動命令以外的車子)
                    //2.可以服務這個port的車，車上是否有都無命令的cst在車上=>有，對該port不進行詢問
                    //bool has_vh_can_service = checkHasVhCanServiceAGVStation(v_trans, agv_station);
                    var check_has_vh_can_service_result = checkHasVhCanServiceAGVStation(v_trans, agv_station);
                    if (!check_has_vh_can_service_result.hasVh)
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: "AGVC",
                                      Data: $"No vh can service agv station:{agv_station.getAGVStationID()}, pass this one ask");

                        checkCanCloseReservationFlagWhenNoVhCanService(v_trans, agv_station);

                        return;
                    }

                    if (v_trans != null && v_trans.Count > 0)
                    {

                        var unfinish_target_port_command = v_trans.
                                                           Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                                                           ToList();

                        var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        int excute_target_pott_count = excute_target_port_tran.Count();
                        if (excute_target_pott_count > 0)
                        {
                            agv_station.IsTransferUnloadExcuting = true;

                            if (agv_station.DeliveryMode == E_AGVStationDeliveryMode.Swap &&
                                agv_station.TransferMode == E_AGVStationTranMode.MoreOut)
                            {
                                if (excute_target_pott_count >= 1)
                                {
                                    agv_station.IsReservation = false;
                                }
                            }
                            else
                            {
                                if (excute_target_pott_count >= 2)
                                {
                                    agv_station.IsReservation = false;
                                }
                            }

                            string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                            scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                                        $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                            return;
                        }
                        else
                        {
                            if (agv_station.IsTransferUnloadExcuting)
                            {
                                agv_station.IsTransferUnloadExcuting = false;
                                agv_station.IsReservation = false;
                                agv_station.TransferMode = E_AGVStationTranMode.None;
                            }
                        }


                        var queue_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE == E_TRAN_STATUS.Queue).ToList();
                        if (queue_target_port_tran.Count() > 0)
                        {
                            if (agv_station.IsReservation)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"agv station:[{agv_station.getAGVStationID()}] is reservation");
                                //not thing...
                                if (agv_station.IsReservedTimeOut)
                                {
                                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                       Data: $"agv station:[{agv_station.getAGVStationID()}] is reserved time out, reset it and then ask again...");
                                    agv_station.IsReservation = false;
                                    string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                                    scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrSet,
                                                                         $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                                }
                            }
                            else
                            {
                                int current_excute_task = 0;
                                current_excute_task = unfinish_target_port_command.Count();
                                //bool is_reserve_success = scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, unfinish_target_port_command.Count(), false);
                                bool is_need_emergency = isNeedEmergency(check_has_vh_can_service_result.canServiceVh, unfinish_target_port_command);
                                agv_station.IsEmergency = is_need_emergency;

                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}],need emergency:{is_need_emergency}, force emergency:{agv_station.ForceEmergency} ...");
                                var check_result = scApp.TransferBLL.web.checkExcuteUnloadTransferToAGVStationStatus(agv_station, current_excute_task, is_need_emergency || agv_station.ForceEmergency);
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                                   Data: $"start try to reserve agv station:[{agv_station.getAGVStationID()}] ,reserve result is can:{check_result.isCan} ,tran mode:{check_result.tranMode}");
                                if (check_result.isCan)
                                {
                                    agv_station.TransferMode = check_result.tranMode;
                                    agv_station.IsReservation = true;
                                    //scApp.TransferService.ScanByVTransfer_v2();
                                    scApp.TransferService.ScanByVTransfer_v3();
                                }
                                else
                                {
                                    agv_station.IsReservation = false;
                                    agv_station.TransferMode = E_AGVStationTranMode.None;
                                }
                            }
                        }
                        else
                        {
                            var check_result = scApp.TransferBLL.web.checkExcuteUnloadTransferToAGVStationStatus(agv_station, 0, false);
                            agv_station.IsReservation = false;
                            agv_station.TransferMode = E_AGVStationTranMode.None;

                            //checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, 0);
                        }
                    }
                    else
                    {
                        var check_result = scApp.TransferBLL.web.checkExcuteUnloadTransferToAGVStationStatus(agv_station, 0, false);
                        agv_station.IsReservation = false;
                        agv_station.TransferMode = E_AGVStationTranMode.None;

                        //checkIsNeedPreMoveToAGVStation(agv_station, service_vh, is_reserve_success, 0);
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 0);
                }
            }
        }

        private bool isNeedEmergency(AVEHICLE vh, List<VTRANSFER> queueTargetPortTran)
        {
            if (vh == null)
                return false;
            if (queueTargetPortTran == null || queueTargetPortTran.Count == 0)
                return false;
            if (!SystemParameter.IsByPassAGVShelfStatus)
            {
                if (vh.ShelfStatus_L == ProtocolFormat.OHTMessage.ShelfStatus.Disable)
                {
                    return true;
                }
                if (vh.ShelfStatus_R == ProtocolFormat.OHTMessage.ShelfStatus.Disable)
                {
                    return true;
                }
            }
            bool has_l_tran_cmd = queueTargetPortTran.Where(tran => SCUtility.isMatche(tran.HOSTSOURCE, vh.LocationRealID_L)).Count() > 0;
            bool has_r_tran_cmd = queueTargetPortTran.Where(tran => SCUtility.isMatche(tran.HOSTSOURCE, vh.LocationRealID_R)).Count() > 0;
            if (has_l_tran_cmd && has_r_tran_cmd)
            {
                return true;
            }
            else
            {
                return false;
            }


        }

        private void checkCanCloseReservationFlagWhenNoVhCanService(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            if (agv_station.IsReservation)
            {
                if (v_trans != null && v_trans.Count > 0)
                {
                    //var unfinish_target_port_command = v_trans.
                    //                                   Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                    //                                   ToList();
                    //if (unfinish_target_port_command.Count == 0)
                    //{
                    //    agv_station.IsReservation = false;
                    //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: "AGVC",
                    //                  Data: $"No vh can service agv station:{agv_station.getAGVStationID()} and no command is this one port, so close reservation. ");
                    //}
                    //var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                    //if (excute_target_port_tran >= 2)
                    //{
                    //    agv_station.IsReservation = false;
                    //}
                    var unfinish_target_port_command = v_trans.
                                                         Where(tran => tran.getTragetPortEQ(scApp.EqptBLL) == agv_station).
                                                         ToList();
                    if (unfinish_target_port_command.Count == 0)
                    {
                        agv_station.IsReservation = false;
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(CMDBLL), Device: "AGVC",
                                      Data: $"No vh can service agv station:{agv_station.getAGVStationID()} and no command is this one port, so close reservation. ");
                    }
                    else
                    {
                        var excute_target_port_tran = unfinish_target_port_command.Where(tran => tran.TRANSFERSTATE >= E_TRAN_STATUS.PreInitial).ToList();
                        int excute_target_pott_count = excute_target_port_tran.Count();
                        if (excute_target_pott_count > 0)
                        {
                            agv_station.IsTransferUnloadExcuting = true;
                            if (agv_station.DeliveryMode == E_AGVStationDeliveryMode.Swap &&
                                agv_station.TransferMode == E_AGVStationTranMode.MoreOut)
                            {
                                if (excute_target_pott_count >= 1)
                                {
                                    agv_station.IsReservation = false;
                                }
                            }
                            else
                            {
                                if (excute_target_pott_count >= 2)
                                {
                                    agv_station.IsReservation = false;
                                }
                            }
                            string reserved_time_out_alarm_code = getAGVStationReservedTimeOutCode(agv_station.getAGVStationID());
                            //scApp.LineService.ProcessAlarmReport("AGVC", reserved_time_out_alarm_code, ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                            //            $"AGV Station:[{agv_station.getAGVStationID()} reserved time out]");
                            return;
                        }
                        else
                        {
                            if (agv_station.IsTransferUnloadExcuting)
                            {
                                agv_station.IsTransferUnloadExcuting = false;
                                agv_station.IsReservation = false;
                            }
                        }
                    }
                }
                else
                {
                    agv_station.IsTransferUnloadExcuting = false;
                    agv_station.IsReservation = false;
                }


            }
            else
            {
                agv_station.IsTransferUnloadExcuting = false;
                agv_station.IsReservation = false;
            }
        }

        private string getAGVStationReservedTimeOutCode(string agvStationID)
        {
            switch (agvStationID)
            {
                case "B7_OHBLINE1_ST01":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE1_ST01;
                case "B7_OHBLINE1_ST02":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE1_ST02;
                case "B7_OHBLINE2_ST01":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE2_ST01;
                case "B7_OHBLINE2_ST02":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE2_ST02;
                case "B7_OHBLOOP_ST01":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LOOP_ST01;
                case "B7_STK01_ST01":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_STOCK_ST01;
                case "B7_OHBLINE3_ST01":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST01;
                case "B7_OHBLINE3_ST02":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST02;
                case "B7_OHBLINE3_ST03":
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE3_ST03;
                default:
                    return AlarmBLL.AGVC_AGVSTATION_RESERVED_TIME_OUT_LINE1_ST01;
            }
        }
        private string getAGVStationStockOfOutTimeOutCode(string agvStationID)
        {
            switch (agvStationID)
            {
                case "B7_OHBLINE1_ST01":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE1_ST01;
                case "B7_OHBLINE1_ST02":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE1_ST02;
                case "B7_OHBLINE2_ST01":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE2_ST01;
                case "B7_OHBLINE2_ST02":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE2_ST02;
                case "B7_OHBLOOP_ST01":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LOOP_ST01;
                case "B7_STK01_ST01":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_STOCK_ST01;
                case "B7_OHBLINE3_ST01":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST01;
                case "B7_OHBLINE3_ST02":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST02;
                case "B7_OHBLINE3_ST03":
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE3_ST03;
                default:
                    return AlarmBLL.AGVC_OUT_OF_STOCK_TIME_OUT_LINE1_ST01;
            }
        }

    }
}
