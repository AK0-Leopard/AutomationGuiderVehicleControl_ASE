﻿// ***********************************************************************
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
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.VO;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
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
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(AGVStationCheckTimerAction), Device: "AGVC",
                       Data: $"start check agv station:[{agv_station.getAGVStationID()}] status...");
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
                            //continue;
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
                            }
                            else
                            {
                                var cehck_has_vh_go_tran_reault = scApp.TransferService.FindNearestVhAndCommand(queue_target_port_tran);
                                //如果有命令還在Queue的話，則嘗試找看看能不能有車子來服務，有的話就可以去詢問看看
                                if (cehck_has_vh_go_tran_reault.isFind)
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
                            scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                            //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                            agv_station.IsReservation = false;
                        }
                    }
                    else
                    {
                        scApp.TransferBLL.web.canExcuteUnloadTransferToAGVStation(agv_station, current_excute_task, false);
                        //scApp.TransferBLL.web.notifyNoUnloadTransferToAGVStation(agv_station);
                        agv_station.IsReservation = false;
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
    }
}
