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
                    var v_trans = scApp.getEQObjCacheManager().getLine().CurrentExcuteTransferCommand;
                    var agv_stations = scApp.EqptBLL.OperateCatch.loadAllAGVStation();
                    foreach (var agv_station in agv_stations)
                    {
                        if (agv_station == null)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                               Data: $"Check agv station has null.");
                            continue;
                        }
                        Task.Run(() => agvStationCheck(v_trans, agv_station));
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

        private void agvStationCheck(List<VTRANSFER> v_trans, AGVStation agv_station)
        {
            if (System.Threading.Interlocked.Exchange(ref agv_station.syncPoint, 1) == 0)
            {
                try
                {
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
                        if (service_vh != null)
                        {
                            if (service_vh.HAS_CST_L)
                                current_excute_task++;
                            if (service_vh.HAS_CST_R)
                                current_excute_task++;
                        }
                    }
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"start check agv station:[{agv_station.getAGVStationID()}] status...");
                    if (v_trans != null && v_trans.Count > 0)
                    {
                        var unfinish_source_port_command = v_trans.
                                                           Where(tran => tran.getSourcePortEQ(scApp.EqptBLL) == agv_station).
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
                                var cehck_has_vh_go_tran_reault = scApp.TransferService.FindNearestVhAndCommand(queue_target_port_tran);
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
                                        scApp.TransferService.ScanByVTransfer_v2();
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
