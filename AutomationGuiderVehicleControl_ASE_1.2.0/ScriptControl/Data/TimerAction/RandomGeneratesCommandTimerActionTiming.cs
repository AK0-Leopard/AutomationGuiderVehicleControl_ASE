// ***********************************************************************
// Assembly         : ScriptControl
// Author           : 
// Created          : 03-31-2016
//
// Last Modified By : 
// Last Modified On : 03-24-2016
// ***********************************************************************
// <copyright file="BCSystemStatusTimer.cs" company="">
//     Copyright ©  2014
// </copyright>
// <summary></summary>
// ***********************************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using com.mirle.ibg3k0.bcf.Data.TimerAction;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.Data.DAO;
using com.mirle.ibg3k0.sc.Data.SECS;
using NLog;

namespace com.mirle.ibg3k0.sc.Data.TimerAction
{
    /// <summary>
    /// Class BCSystemStatusTimer.
    /// </summary>
    /// <seealso cref="com.mirle.ibg3k0.bcf.Data.TimerAction.ITimerAction" />
    public class RandomGeneratesCommandTimerActionTiming : ITimerAction
    {
        /// <summary>
        /// The logger
        /// </summary>
        private static Logger logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// The sc application
        /// </summary>
        protected SCApplication scApp = null;

        public Dictionary<int, List<TranTask>> dicTranTaskSchedule = null;
        public int MCS_TaskIndex = 0;//420


        /// <summary>
        /// Initializes a new instance of the <see cref="BCSystemStatusTimer"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="intervalMilliSec">The interval milli sec.</param>
        public RandomGeneratesCommandTimerActionTiming(string name, long intervalMilliSec)
            : base(name, intervalMilliSec)
        {

        }
        /// <summary>
        /// Initializes the start.
        /// </summary>
        public override void initStart()
        {
            scApp = SCApplication.getInstance();
            //dicTranTaskSchedule = scApp.CMDBLL.loadTranTaskSchedule_24Hour();

        }
        /// <summary>
        /// Timer Action的執行動作
        /// </summary>
        /// <param name="obj">The object.</param>
        private long syncPoint = 0;
        public void doProcessOld(object obj)
        {
            if (!DebugParameter.CanAutoRandomGeneratesCommand) return;
            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            {
                try
                {

                    TimingGeneratesCommands();
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

        private void TimingGeneratesCommands()
        {
            try
            {
                List<TranTask> lstTranTask = null;
                logger.Trace($"MCS Index:{MCS_TaskIndex}.");
                if (dicTranTaskSchedule.TryGetValue(MCS_TaskIndex, out lstTranTask))
                {
                    if (lstTranTask != null)//只做24小時
                    {
                        foreach (TranTask task in lstTranTask)
                        {
                            string cst_id = task.CSTID;
                            string priority = task.Priority;
                            string source = task.SourcePort;
                            string destination = task.DestinationPort;
                            Task.Run(() => sendTranCmd(cst_id, priority, source, destination));
                            SpinWait.SpinUntil(() => false, 100);
                        }
                    }
                }
                MCS_TaskIndex++;
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception");
            }

        }

        public void sendTranCmd(string carrier_id, string priority, string source_port, string destn_port)
        {
            string cmdID = DateTime.Now.ToString("yyyyMMddHHmmssfffff");
            logger.Trace($"Creat command,CMD ID:{cmdID}, CST ID:{carrier_id},Priority:{priority},Source:{source_port},Destination:{destn_port}.");

            //scApp.CMDBLL.doTransferCommand(cmdID, priority, "0", carrier_id, source_port, destn_port, SECSConst.HCACK_Confirm);
            //SpinWait.SpinUntil(() => false, 5000);
            //scApp.CMDBLL.checkMCSTransferCommand();
            //scApp.CMDBLL.checkMCSTransferCommand_New();
            scApp.TransferService.Scan();

        }

        Random rnd_Index = new Random(Guid.NewGuid().GetHashCode());
        public void doProcess1(object obj)
        {

            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            {
                try
                {
                    if (!DebugParameter.CanAutoRandomGeneratesCommand)
                    {
                        agvStations = null;
                        return;
                    }
                    List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
                    foreach (AVEHICLE vh in vhs)
                    {
                        if (vh.isTcpIpConnect &&
                            vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
                            vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
                            !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
                            //scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign)
                            scApp.CMDBLL.canAssignCmdNew(vh, E_CMD_TYPE.Move).canAssign)
                        {
                            //找一份目前儲位的列表
                            if (agvStations == null || agvStations.Count == 0)
                                agvStations = scApp.PortStationBLL.OperateCatch.loadAllPortStation().ToList();
                            //如果取完還是空的 就跳出去
                            if (agvStations == null || agvStations.Count == 0)
                                return;
                            //刪除目前Vh所在的port位置
                            foreach (var port_station in agvStations.ToList())
                            {
                                if (SCUtility.isMatche(port_station.ADR_ID, vh.CUR_ADR_ID))
                                {
                                    agvStations.Remove(port_station);
                                }
                            }

                            //隨機找出讓車子移至下一個Port的地方
                            int task_RandomIndex = rnd_Index.Next(agvStations.Count - 1);
                            APORTSTATION p = agvStations[task_RandomIndex];
                            bool isSuccess = true;
                            if (scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, p.ADR_ID))
                            {
                                isSuccess &= scApp.VehicleService.Command.Move(vh.VEHICLE_ID, p.ADR_ID).isSuccess;
                            }
                            agvStations.Remove(p);
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

        public void doProcessTemp(object obj)
        {

            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            {
                try
                {
                    if (!DebugParameter.CanAutoRandomGeneratesCommand)
                    {
                        agvStations = null;
                        return;
                    }
                    List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
                    foreach (AVEHICLE vh in vhs)
                    {
                        if (vh.isTcpIpConnect &&
                            vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
                            vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
                            !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
                            //scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign)
                            scApp.CMDBLL.canAssignCmdNew(vh, E_CMD_TYPE.Move).canAssign)
                        {
                            //找一份目前儲位的列表
                            if (agvStations == null || agvStations.Count == 0)
                                agvStations = scApp.PortStationBLL.OperateCatch.loadAllPortStation().ToList();
                            //如果取完還是空的 就跳出去
                            if (agvStations == null || agvStations.Count == 0)
                                return;
                            APORTSTATION has_cst_port_station = agvStations.Where(port => !SCUtility.isEmpty(port.CST_ID)).FirstOrDefault();
                            if (has_cst_port_station == null)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                                   Data: $"No find has cst port");
                                return;
                            }
                            else
                            {
                                agvStations.Remove(has_cst_port_station);
                            }
                            //隨機找出讓車子移至下一個Port的地方
                            int task_RandomIndex = rnd_Index.Next(agvStations.Count - 1);
                            APORTSTATION p = agvStations[task_RandomIndex];
                            bool isSuccess = true;
                            if (scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, has_cst_port_station.ADR_ID) &&
                                scApp.GuideBLL.IsRoadWalkable(has_cst_port_station.ADR_ID, p.ADR_ID))
                            {
                                isSuccess &= scApp.VehicleService.Command.Loadunload(vh.VEHICLE_ID,
                                    has_cst_port_station.CST_ID,
                                    has_cst_port_station.ADR_ID, p.ADR_ID,
                                    has_cst_port_station.PORT_ID, p.PORT_ID);
                                agvStations.Remove(p);
                            }
                            else
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                                   Data: $"Has path not enable");
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
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

        List<APORTSTATION> wait_unload_agv_station = null;
        public void doProcessTemp2(object obj)
        {

            //if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            //{
            //    try
            //    {
            //        if (!DebugParameter.CanAutoRandomGeneratesCommand)
            //        {
            //            wait_unload_agv_station = null;
            //            return;
            //        }


            //        updatePortStatusByRedis();

            //        List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
            //        foreach (AVEHICLE vh in vhs)
            //        {
            //            if (vh.isTcpIpConnect &&
            //                vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
            //                vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
            //                !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
            //                scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign)
            //            {
            //                //找出目前的AGVStation
            //                if (wait_unload_agv_station == null || wait_unload_agv_station.Count == 0)
            //                    wait_unload_agv_station = scApp.PortStationBLL.OperateCatch.loadAGVPortStationCanUnload();
            //                //如果取完還是空的 就跳出去
            //                if (wait_unload_agv_station == null || wait_unload_agv_station.Count == 0)
            //                {
            //                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
            //                       Data: $"No find can unload agv station");
            //                    return;
            //                }

            //                APORTSTATION wait_out_agv_station = scApp.PortStationBLL.OperateCatch.getAGVPortStationWaitOutOK();

            //                if (wait_out_agv_station == null)
            //                {
            //                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
            //                       Data: $"No find status:[wait out] agv station");
            //                    return;
            //                }

            //                //隨機找出讓車子移至下一個Port的地方
            //                int task_RandomIndex = rnd_Index.Next(wait_unload_agv_station.Count - 1);
            //                APORTSTATION can_unload_agv_station = wait_unload_agv_station[task_RandomIndex];

            //                bool isSuccess = true;
            //                if (scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, wait_out_agv_station.ADR_ID) &&
            //                    scApp.GuideBLL.IsRoadWalkable(wait_out_agv_station.ADR_ID, can_unload_agv_station.ADR_ID))
            //                {
            //                    isSuccess &= scApp.VehicleService.Command.Loadunload(vh.VEHICLE_ID,
            //                        wait_out_agv_station.CST_ID,
            //                        wait_out_agv_station.ADR_ID, can_unload_agv_station.ADR_ID,
            //                        wait_out_agv_station.PORT_ID, can_unload_agv_station.PORT_ID);
            //                }
            //                else
            //                {
            //                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
            //                       Data: $"Can't find the path.");
            //                }
            //                wait_unload_agv_station.Remove(can_unload_agv_station);
            //            }
            //        }
            //    }
            //    catch (Exception ex)
            //    {
            //        logger.Error(ex, "Exception");
            //    }
            //    finally
            //    {
            //        System.Threading.Interlocked.Exchange(ref syncPoint, 0);
            //    }
            //}
        }
        public async override void doProcess(object obj)
        {

            if (System.Threading.Interlocked.Exchange(ref syncPoint, 1) == 0)
            {
                try
                {
                    if (!DebugParameter.CanAutoRandomGeneratesCommand)
                    {
                        agvStations = null;
                        return;
                    }

                    switch (DebugParameter.CycleRunType)
                    {
                        case DebugParameter.CycleRunTestType.Normal:
                            CycleRunTest();
                            break;
                        case DebugParameter.CycleRunTestType.IgnorePortStatus:
                            CycleRunIgnorePortStatus();
                            break;
                        case DebugParameter.CycleRunTestType.OnlyMove:
                            //CycleRunOnlyMove();
                            CycleRunOnlyMove();
                            break;
                        case DebugParameter.CycleRunTestType.MoveBySelectPort:
                            CycleRunOnlyMoveByZone();
                            break;
                    }

                    //updatePortStatusByRedis();
                    //List<AZONE> zones = scApp.ZoneBLL.cache.LoadZones();
                    //foreach (var zone in zones)
                    //{
                    //    Task<(bool is_success, object result)> find_idle_vh_result = findIdleVehicle();
                    //    Task<(bool is_success, object result)> find_can_unload_agv_station_result = findCanUnloadAGVStation(zone.ZONE_ID);
                    //    Task<(bool is_success, object result)> find_can_load_agv_station_result = findCanLoadAGVStation(zone.ZONE_ID);

                    //    var check_results = await Task.WhenAll(find_idle_vh_result, find_can_unload_agv_station_result, find_can_load_agv_station_result);
                    //    bool is_success = check_results.Where(result => result.is_success == false).Count() == 0;
                    //    if (is_success)
                    //    {
                    //        AVEHICLE idle_vh = check_results[0].result as AVEHICLE;
                    //        APORTSTATION can_unload_agv_station_port = check_results[1].result as APORTSTATION;
                    //        APORTSTATION can_load_agv_station_port = check_results[2].result as APORTSTATION;

                    //        if (scApp.GuideBLL.IsRoadWalkable(idle_vh.CUR_ADR_ID, can_load_agv_station_port.ADR_ID) &&
                    //            scApp.GuideBLL.IsRoadWalkable(can_load_agv_station_port.ADR_ID, can_unload_agv_station_port.ADR_ID))
                    //        {
                    //            scApp.VehicleService.Command.Loadunload(idle_vh.VEHICLE_ID,
                    //            can_load_agv_station_port.CST_ID,
                    //            can_load_agv_station_port.ADR_ID, can_unload_agv_station_port.ADR_ID,
                    //            can_load_agv_station_port.PORT_ID, can_unload_agv_station_port.PORT_ID);
                    //        }
                    //        else
                    //        {
                    //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                    //               Data: $"Can't find the path.");
                    //        }
                    //    }
                    //}
                }
                catch (Exception ex)
                {
                    logger.Error(ex, "Exception");
                }
                finally
                {
                    System.Threading.Interlocked.Exchange(ref syncPoint, 0);
                }
            }
        }

        private async void CycleRunTest()
        {
            scApp.PortStationBLL.updatePortStatusByRedis();
            List<AZONE> zones = scApp.ZoneBLL.cache.LoadZones();
            foreach (var zone in zones)
            {
                Task<(bool is_success, object result)> find_idle_vh_result = findIdleForCycleVehicle(zone.ZONE_ID);
                Task<(bool is_success, object result)> find_can_unload_agv_station_result = findCanUnloadAGVStation(zone.ZONE_ID);
                Task<(bool is_success, object result)> find_can_load_agv_station_result = findCanLoadAGVStation(zone.ZONE_ID);

                var check_results = await Task.WhenAll(find_idle_vh_result, find_can_unload_agv_station_result, find_can_load_agv_station_result);
                bool is_success = check_results.Where(result => result.is_success == false).Count() == 0;
                if (is_success)
                {
                    AVEHICLE idle_vh = check_results[0].result as AVEHICLE;
                    APORTSTATION can_unload_agv_station_port = check_results[1].result as APORTSTATION;
                    APORTSTATION can_load_agv_station_port = check_results[2].result as APORTSTATION;

                    if (scApp.GuideBLL.IsRoadWalkable(idle_vh.CUR_ADR_ID, can_load_agv_station_port.ADR_ID) &&
                        scApp.GuideBLL.IsRoadWalkable(can_load_agv_station_port.ADR_ID, can_unload_agv_station_port.ADR_ID))
                    {
                        scApp.VehicleService.Command.Loadunload(idle_vh.VEHICLE_ID,
                        can_load_agv_station_port.CassetteID,
                        can_load_agv_station_port.ADR_ID, can_unload_agv_station_port.ADR_ID,
                        can_load_agv_station_port.PORT_ID, can_unload_agv_station_port.PORT_ID);
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                           Data: $"Can't find the path.");
                    }
                }
            }
        }

        List<APORTSTATION> agvStations = null;
        private async void CycleRunIgnorePortStatus()
        {
            //List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();

            List<AZONE> zones = scApp.ZoneBLL.cache.LoadZones();
            foreach (var zone in zones)
            {
                Task<(bool is_success, object result)> find_idle_vh_result = findIdleForCycleVehicle(zone.ZONE_ID);
                Task<(bool is_success, object result)> find_has_cst_agv_station_result = findHasCSTAndInculdCycleTestAGVStation(zone.ZONE_ID);
                Task<(bool is_success, object result)> find_no_cst_agv_station_result = findNoCSTAndInculdCycleTestAGVStation(zone.ZONE_ID);
                var check_results = await Task.WhenAll(find_idle_vh_result, find_has_cst_agv_station_result, find_no_cst_agv_station_result);
                bool is_success = check_results.Where(result => result.is_success == false).Count() == 0;
                if (is_success)
                {
                    AVEHICLE idle_vh = check_results[0].result as AVEHICLE;
                    APORTSTATION has_cst_agv_station_port = check_results[1].result as APORTSTATION;
                    APORTSTATION no_cst_agv_station_port = check_results[2].result as APORTSTATION;

                    if (scApp.GuideBLL.IsRoadWalkable(idle_vh.CUR_ADR_ID, no_cst_agv_station_port.ADR_ID) &&
                        scApp.GuideBLL.IsRoadWalkable(no_cst_agv_station_port.ADR_ID, has_cst_agv_station_port.ADR_ID))
                    {
                        scApp.VehicleService.Command.Loadunload(idle_vh.VEHICLE_ID,
                        has_cst_agv_station_port.CST_ID,
                        has_cst_agv_station_port.ADR_ID, no_cst_agv_station_port.ADR_ID,
                        has_cst_agv_station_port.PORT_ID, no_cst_agv_station_port.PORT_ID);
                        no_cst_agv_station_port.TestTimes++;
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                           Data: $"Can't find the path.");
                    }
                }
            }


            //foreach (AVEHICLE vh in vhs)
            //{
            //    if (vh.isTcpIpConnect &&
            //        vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
            //        vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
            //        !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
            //        scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign)
            //    {
            //        //找一份目前儲位的列表
            //        if (agvStations == null || agvStations.Count == 0)
            //        {
            //            agvStations = scApp.PortStationBLL.OperateCatch.loadAGVPortStation().ToList();
            //            agvStations = agvStations.Where(port => port.IncludeCycleTest).ToList();
            //        }
            //        //如果取完還是空的 就跳出去
            //        if (agvStations == null || agvStations.Count == 0)
            //            return;
            //        APORTSTATION has_cst_port_station = agvStations.Where(port => !SCUtility.isEmpty(port.CST_ID)).FirstOrDefault();
            //        if (has_cst_port_station == null)
            //        {
            //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
            //               Data: $"No find has cst port");
            //            return;
            //        }
            //        else
            //        {
            //            agvStations.Remove(has_cst_port_station);
            //        }
            //        //隨機找出讓車子移至下一個Port的地方
            //        int task_RandomIndex = rnd_Index.Next(agvStations.Count - 1);
            //        APORTSTATION p = agvStations[task_RandomIndex];
            //        bool isSuccess = true;
            //        if (scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, has_cst_port_station.ADR_ID) &&
            //            scApp.GuideBLL.IsRoadWalkable(has_cst_port_station.ADR_ID, p.ADR_ID))
            //        {
            //            isSuccess &= scApp.VehicleService.Command.Loadunload(vh.VEHICLE_ID,
            //                has_cst_port_station.CST_ID,
            //                has_cst_port_station.ADR_ID, p.ADR_ID,
            //                has_cst_port_station.PORT_ID, p.PORT_ID);
            //            agvStations.Remove(p);
            //        }
            //        else
            //        {
            //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
            //               Data: $"Has path not enable");
            //        }
            //    }
            //}
        }

        private void CycleRunOnlyMove()
        {
            List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
            foreach (AVEHICLE vh in vhs)
            {
                if (vh.isTcpIpConnect &&
                    vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
                    vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
                    !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
                    //scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign)
                    scApp.CMDBLL.canAssignCmdNew(vh, E_CMD_TYPE.Move).canAssign)
                {
                    //找一份目前儲位的列表
                    if (agvStations == null || agvStations.Count == 0)
                        agvStations = scApp.PortStationBLL.OperateCatch.loadAllPortStation().ToList();
                    //如果取完還是空的 就跳出去
                    if (agvStations == null || agvStations.Count == 0)
                        return;
                    //刪除目前Vh所在的port位置
                    foreach (var port_station in agvStations.ToList())
                    {
                        if (SCUtility.isMatche(port_station.ADR_ID, vh.CUR_ADR_ID))
                        {
                            agvStations.Remove(port_station);
                        }
                    }

                    //隨機找出讓車子移至下一個Port的地方
                    int task_RandomIndex = rnd_Index.Next(agvStations.Count - 1);
                    APORTSTATION p = agvStations[task_RandomIndex];
                    bool isSuccess = true;
                    if (scApp.GuideBLL.IsRoadWalkable(vh.CUR_ADR_ID, p.ADR_ID))
                    {
                        isSuccess &= scApp.VehicleService.Command.Move(vh.VEHICLE_ID, p.ADR_ID).isSuccess;
                    }
                    agvStations.Remove(p);
                }
            }
        }
        private void CycleRunOnlyMoveByZone()
        {
            List<AZONE> zones = scApp.ZoneBLL.cache.LoadZones();
            foreach (var zone in zones)
            {
                (bool is_success, object result) find_idle_vh_result = findIdleForCycleVehicle(zone.ZONE_ID).Result;
                if (!find_idle_vh_result.is_success) continue;
                AVEHICLE find_vh = find_idle_vh_result.result as AVEHICLE;
                (bool is_success, object result) find_cycle_run_port = findInculdCycleTestPort(zone.ZONE_ID, find_vh.CUR_ADR_ID).Result;
                if (!find_cycle_run_port.is_success) continue;

                bool is_success = find_idle_vh_result.is_success && find_cycle_run_port.is_success;
                if (is_success)
                {
                    AVEHICLE idle_vh = find_idle_vh_result.result as AVEHICLE;
                    APORTSTATION choose_port = find_cycle_run_port.result as APORTSTATION;

                    if (scApp.GuideBLL.IsRoadWalkable(idle_vh.CUR_ADR_ID, choose_port.ADR_ID))
                    {
                        scApp.VehicleService.Command.Move(idle_vh.VEHICLE_ID, choose_port.ADR_ID);
                        choose_port.TestTimes++;
                    }
                    else
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                           Data: $"Can't find the path.");
                    }
                }
            }
        }


        private Task<(bool is_success, object result)> findCanLoadAGVStation(string zoneID)
        {
            var can_load_agv_station = scApp.PortStationBLL.OperateCatch.getAGVPortStationWaitOutOK(scApp.CMDBLL, zoneID);
            if (can_load_agv_station == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"No find can load agv station,{zoneID}");
            }
            return Task.FromResult((can_load_agv_station != null, (object)can_load_agv_station));
        }
        private Task<(bool is_success, object result)> findHasCSTAndInculdCycleTestAGVStation(string zoneID)
        {
            var agv_stations = scApp.PortStationBLL.OperateCatch.loadAGVPortStation(zoneID);
            APORTSTATION agv_station = null;
            if (agv_stations == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"No find can unload agv station,{zoneID}");
            }
            else
            {
                agv_station = agv_stations.Where(station => !SCUtility.isEmpty(station.CST_ID) && station.IncludeCycleTest).
                                           FirstOrDefault();
            }
            return Task.FromResult((agv_station != null, (object)agv_station));
        }

        private Task<(bool is_success, object result)> findCanUnloadAGVStation(string zoneID)
        {
            var can_unload_agv_station = scApp.PortStationBLL.OperateCatch.getAGVPortStationCanUnloadForCycleRun(scApp.CMDBLL, zoneID);
            if (can_unload_agv_station == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"No find can unload agv station,{zoneID}");
            }
            return Task.FromResult((can_unload_agv_station != null, (object)can_unload_agv_station));
        }
        private Task<(bool is_success, object result)> findNoCSTAndInculdCycleTestAGVStation(string zoneID)
        {
            var agv_stations = scApp.PortStationBLL.OperateCatch.loadAGVPortStation(zoneID);
            APORTSTATION agv_station = null;
            if (agv_stations == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"No find can unload agv station");
            }
            else
            {
                agv_station = agv_stations.Where(station => SCUtility.isEmpty(station.CST_ID) && station.IncludeCycleTest).
                                           OrderBy(station => station.TestTimes).
                                           FirstOrDefault();
            }
            return Task.FromResult((agv_station != null, (object)agv_station));
        }

        private Task<(bool is_success, object result)> findIdleForCycleVehicle(string zoneID)
        {
            List<AVEHICLE> vhs = scApp.VehicleBLL.cache.loadAllVh();
            var result = vhs.Where(vh => vh.isTcpIpConnect &&
                            !SCUtility.isEmpty(vh.CUR_ADR_ID) &&
                            //SCUtility.isMatche(vh.CUR_ZONE_ID, zoneID) &&
                            SCUtility.isMatche(vh.getZoneID(scApp.SectionBLL), zoneID) &&
                            vh.MODE_STATUS == ProtocolFormat.OHTMessage.VHModeStatus.AutoRemote &&
                            vh.ACT_STATUS == ProtocolFormat.OHTMessage.VHActionStatus.NoCommand &&
                            //scApp.CMDBLL.canAssignCmdNew(vh.VEHICLE_ID, E_CMD_TYPE.Move).canAssign).
                            scApp.CMDBLL.canAssignCmdNew(vh, E_CMD_TYPE.Move).canAssign).
                         FirstOrDefault();
            if (result == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"Can't find idle vh.,{zoneID}");
            }
            return Task.FromResult((result != null, (object)result));
        }

        private bool IsInZone(AVEHICLE vh, string zoneID)
        {
            string current_adr = SCUtility.Trim(vh.CUR_ADR_ID, true);
            if (SCUtility.isEmpty(current_adr)) return false;
            if (current_adr.StartsWith("9"))
            {
                current_adr = current_adr.Remove(0, 1);
                current_adr = "1" + current_adr;
            }
            var sections = scApp.SectionBLL.cache.GetSectionsByFromAddress(current_adr);
            //濾掉9開頭的路段
            sections = sections.
                       Where(sec => !sec.SEC_ID.StartsWith("9")).
                       ToList();
            return true;
        }

        private void updatePortStatusByRedis()
        {
            var ports_plc_info = scApp.PortStationBLL.redis.getCurrentPortsInfo();
            var port_stations = scApp.PortStationBLL.OperateCatch.loadAGVPortStation().ToList();
            foreach (var port_station in port_stations)
            {
                var port_plc_info = ports_plc_info.Where(port => SCUtility.isMatche(port.PortID, port_station.PORT_ID)).
                                                   FirstOrDefault();
                if (port_plc_info != null)
                {
                    port_station.SetPortInfo(port_plc_info);
                }
                else
                {
                    port_station.ResetPortInfo();
                }

            }
        }

        private Task<(bool is_success, object result)> findInculdCycleTestPort(string zoneID, string byPassAdrID)
        {
            var all_port_in_zone = scApp.PortStationBLL.OperateCatch.loadAllPortStation(zoneID);
            APORTSTATION port = null;
            if (all_port_in_zone == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(RandomGeneratesCommandTimerActionTiming), Device: string.Empty,
                   Data: $"No find any port,{zoneID}");
            }
            else
            {
                port = all_port_in_zone.Where(station => station.IncludeCycleTest &&
                                                                !SCUtility.isMatche(station.ADR_ID, byPassAdrID)).
                                        OrderBy(station => station.TestTimes).
                                        FirstOrDefault();
            }
            return Task.FromResult((port != null, (object)port));
        }

    }

}

