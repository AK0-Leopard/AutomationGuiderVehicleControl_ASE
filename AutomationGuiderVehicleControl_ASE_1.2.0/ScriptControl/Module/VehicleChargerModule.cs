using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.BLL;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace com.mirle.ibg3k0.sc.Module
{
    public class VehicleChargerModule
    {
        const string DEVICE_NAME = "AGVC";
        //public const int THE_LONGEST_FULLY_CHARGED_TIME_INTERVAL_MIN = 15;
        private NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private VehicleBLL vehicleBLL = null;
        private Service.VehicleService vehicleService = null;
        private Service.LineService lineService = null;
        private SegmentBLL segmentBLL = null;
        private AddressesBLL addressesBLL = null;
        private GuideBLL guideBLL = null;
        private CMDBLL cmdBLL = null;
        private EqptBLL eqptBLL = null;
        private UnitBLL unitBLL = null;
        private CommObjCacheManager commObjCacheManager = null;
        public VehicleChargerModule()
        {

        }
        public void start(SCApplication app)
        {
            vehicleBLL = app.VehicleBLL;
            vehicleService = app.VehicleService;
            segmentBLL = app.SegmentBLL;
            addressesBLL = app.AddressesBLL;
            guideBLL = app.GuideBLL;
            cmdBLL = app.CMDBLL;
            eqptBLL = app.EqptBLL;
            unitBLL = app.UnitBLL;
            commObjCacheManager = app.getCommObjCacheManager();
            lineService = app.LineService;

            var vhs = app.getEQObjCacheManager().getAllVehicle();
            foreach (AVEHICLE vh in vhs)
            {
                vh.CommandComplete += Vh_CommandComplete;
                vh.BatteryLevelChange += Vh_BatteryLevelChange;
            }

            //註冊各個Coupler的Status變化，在有其中一個有變化的時候要通知AGV目前所有coupler的狀態
            List<AUNIT> chargers = unitBLL.OperateCatch.loadUnits();
            foreach (AUNIT charger in chargers)
            {
                charger.CouplerStatusChanged += Charger_CouplerStatusChanged;
                charger.CouplerHPSafetyChaged += Charger_CouplerHPSafetyChaged;
            }
        }

        private void Charger_CouplerHPSafetyChaged(object sender, SCAppConstants.CouplerHPSafety e)
        {
            try
            {
                if (DebugParameter.isPassCouplerHPSafetySignal)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                       Data: $"pass coupler hp safey signal,flag:{DebugParameter.isPassCouplerHPSafetySignal}");
                    return;
                }
                AUNIT charger = sender as AUNIT;
                if (charger == null)
                {
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                       Data: $"charger is null");
                    return;
                }
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: $"Coupler hp safyte has changed,charger id:{charger.UNIT_ID} hp safety:{e}");
                var couplers = addressesBLL.cache.LoadCouplerAddresses(charger.UNIT_ID);
                var vhs = vehicleBLL.cache.loadAllVh();
                switch (e)
                {
                    case SCAppConstants.CouplerHPSafety.NonSafety:
                        lineService.ProcessAlarmReport(charger.UNIT_ID, AlarmBLL.AGVC_CHARGER_HP_NOT_SAFETY, ErrorStatus.ErrSet, $"Coupler position not safety.");
                        break;
                    case SCAppConstants.CouplerHPSafety.Safyte:
                        lineService.ProcessAlarmReport(charger.UNIT_ID, AlarmBLL.AGVC_CHARGER_HP_NOT_SAFETY, ErrorStatus.ErrReset, $"Coupler position not safety.");
                        break;
                }
                foreach (var coupler in couplers)
                {
                    string coupler_adr_id = coupler.ADR_ID;
                    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                       Data: $"Coupler hp safyte has changed,coupler adr id:{coupler_adr_id}, start check has vh can pass...");
                    foreach (var vh in vhs)
                    {
                        if (vh.isTcpIpConnect &&
                            (vh.MODE_STATUS == VHModeStatus.AutoRemote ||
                             vh.MODE_STATUS == VHModeStatus.AutoCharging ||
                             vh.MODE_STATUS == VHModeStatus.AutoCharging)
                           )
                        {
                            string vh_cur_adr_id = vh.CUR_ADR_ID;
                            bool is_walkable = guideBLL.IsRoadWalkable(coupler_adr_id, vh_cur_adr_id);
                            if (is_walkable)
                            {
                                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                                   Data: $"Coupler hp safyte has changed,coupler adr id:{coupler_adr_id} vh current adr:{vh_cur_adr_id}, is walkable start pause/continue action");
                                string vh_id = vh.VEHICLE_ID;
                                PauseType pauseType = PauseType.Normal;
                                PauseEvent pauseEvent = PauseEvent.Pause;
                                if (e == SCAppConstants.CouplerHPSafety.Safyte)
                                {
                                    pauseEvent = PauseEvent.Continue;
                                }
                                else
                                {
                                    pauseEvent = PauseEvent.Pause;
                                }
                                //Task.Run(() =>
                                //{
                                //    try
                                //    {
                                //        vehicleService.Send.Pause(vh_id, pauseEvent, pauseType);
                                //    }
                                //    catch (Exception ex)
                                //    {
                                //        logger.Error(ex, "Exception:");
                                //    }
                                //});
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }

        }


        object charger_status_notify_lock_obj = new object();
        private void Charger_CouplerStatusChanged(object sender, EventArgs e)
        {
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: $"Start sysc coupler status...");
                lock (charger_status_notify_lock_obj)
                {
                    vehicleService.doDataSyscAllVh();
                }
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: $"End sysc coupler status.");
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
            }
        }

        private void Vh_BatteryLevelChange(object sender, BatteryLevel e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            try
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: $"Battery Level Change,Current Level:{e}.low level:{AVEHICLE.BATTERYLEVELVALUE_LOW},Hight level:{AVEHICLE.BATTERYLEVELVALUE_HIGH}",
                   VehicleID: vh.VEHICLE_ID);
                switch (e)
                {
                    case BatteryLevel.Full:
                        vehicleBLL.updataVehicleLastFullyChargerTime(vh.VEHICLE_ID);
                        break;
                    case BatteryLevel.Low:
                        //if (vh.MODE_STATUS == VHModeStatus.Manual || vh.MODE_STATUS == VHModeStatus.None)
                        //{
                        //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                        //       Data: $"vh:{vh.VEHICLE_ID} current mode is:{vh.MODE_STATUS} can't change to auto charge.",
                        //       VehicleID: vh.VEHICLE_ID);
                        //    return;
                        //}
                        //else
                        //{
                        //    LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                        //       Data: $"vh:{vh.VEHICLE_ID} current mode is:{vh.MODE_STATUS} change to auto charge.",
                        //       VehicleID: vh.VEHICLE_ID);
                        //}
                        //vehicleService.changeVhStatusToAutoCharging(vh.VEHICLE_ID);

                        if (vh.MODE_STATUS == VHModeStatus.AutoRemote)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                               Data: $"vh:{vh.VEHICLE_ID} current mode is:{vh.MODE_STATUS} change to auto charge.",
                               VehicleID: vh.VEHICLE_ID);
                            vehicleService.changeVhStatusToAutoCharging(vh.VEHICLE_ID);
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                               Data: $"vh:{vh.VEHICLE_ID} current mode is:{vh.MODE_STATUS} can't change to auto charge.",
                               VehicleID: vh.VEHICLE_ID);
                        }
                        break;
                }
                if (e == BatteryLevel.Low)
                {
                    lineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_BATTERY_LEVEL_IS_LOW, ErrorStatus.ErrSet, $"vehicle:{vh.VEHICLE_ID} is in low battery status");
                }
                else
                {
                    lineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_BATTERY_LEVEL_IS_LOW, ErrorStatus.ErrReset, $"vehicle:{vh.VEHICLE_ID} is in low battery status");
                    lineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_FIND_THE_COUPLER_TO_CHARGING, ErrorStatus.ErrReset, $"vehicle:{vh.VEHICLE_ID} can't find coupler to charging");
                }
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: ex,
                   VehicleID: vh?.VEHICLE_ID);
            }
        }

        private void Vh_CommandComplete(object sender, CompleteStatus e)
        {
            AVEHICLE vh = sender as AVEHICLE;
            try
            {
                string cur_segment_id = vh.CUR_SEG_ID;
                string cur_section_id = vh.CUR_SEC_ID;
                string cur_adr_id = vh.CUR_ADR_ID;
                string vh_id = vh.VEHICLE_ID;

                if (e == CompleteStatus.MoveToCharger)
                {
                    //if (vh.BatteryLevel == BatteryLevel.Low)
                    if (vh.MODE_STATUS == VHModeStatus.AutoCharging)
                    {
                        RoadControl(vh_id, cur_adr_id, false);
                    }
                }
                else
                {
                    //if (vh.BatteryLevel == BatteryLevel.Low)
                    if (vh.MODE_STATUS == VHModeStatus.AutoCharging)
                    {
                        AADDRESS vh_on_address = addressesBLL.cache.GetAddress(cur_adr_id);
                        if (vh_on_address.IsCoupler)
                        {
                            RoadControl(vh_id, cur_adr_id, false);
                        }
                        else
                        {
                            askVhToCharging(vh);
                        }
                    }
                }
                lineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_FIND_THE_COUPLER_TO_CHARGING, ErrorStatus.ErrReset, $"vehicle:{vh.VEHICLE_ID} can't find coupler to charging");
            }
            catch (Exception ex)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: ex,
                   VehicleID: vh?.VEHICLE_ID);
            }
        }
        private bool canCreatAvoidCommand(AVEHICLE reservedVh)
        {
            return reservedVh.isTcpIpConnect &&
                        (reservedVh.MODE_STATUS == VHModeStatus.AutoRemote || reservedVh.MODE_STATUS == VHModeStatus.AutoCharging) &&
                        reservedVh.ACT_STATUS == VHActionStatus.NoCommand &&
                        !cmdBLL.isCMD_OHTCQueueByVh(reservedVh.VEHICLE_ID) &&
                        !cmdBLL.HasCMD_MCSInQueue();
        }

        private void RoadControl(string vh_id, string cur_adr_id, bool isEnable)
        {
            ICpuplerType couplerAddress = addressesBLL.cache.GetAddress(cur_adr_id) as ICpuplerType;
            if (couplerAddress == null)
            {
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Warn, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                   Data: $"CouplerAddress not exist.current adr id {cur_adr_id}",
                   VehicleID: vh_id);
                return;
            }
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                     Data: $"Excute cpupler address:{cur_adr_id} of road contol," +
                     $"segment ids:{string.Join(",", couplerAddress.TrafficControlSegment)},is ban:{isEnable} ",
                     VehicleID: vh_id);

            foreach (string segment in couplerAddress.TrafficControlSegment)
            {
                if (isEnable)
                    vehicleService.doEnableDisableSegment(segment, E_PORT_STATUS.InService);
                else
                    vehicleService.doEnableDisableSegment(segment, E_PORT_STATUS.OutOfService);
            }
            guideBLL.clearAllDirGuideQuickSearchInfo();
        }

        private (bool isSuccess, string msg) askVhToCharging(AVEHICLE vh)
        {
            string vh_current_address = vh.CUR_ADR_ID;
            AADDRESS current_adr = addressesBLL.cache.GetAddress(vh.CUR_ADR_ID);
            if (current_adr != null &&
                current_adr is CouplerAddress && (current_adr as CouplerAddress).IsWork(unitBLL))
            {
                ICpuplerType cpupler = (current_adr as CouplerAddress);
                string meg = $"ask vh:{vh.VEHICLE_ID} to charging. but it is already in charger:{cpupler.ChargerID} ,cpupler num:{cpupler.CouplerNum}";
                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                         Data: meg,
                         VehicleID: vh.VEHICLE_ID);
                return (false, meg);
            }
            bool is_need_to_long_charge = vh.IsNeedToLongCharge();
            string best_coupler_adr = findBestCoupler(vh_current_address, is_need_to_long_charge);
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                     Data: $"ask vh:{vh.VEHICLE_ID} to charging. coupler adr:{best_coupler_adr} ",
                     VehicleID: vh.VEHICLE_ID);
            if (!SCUtility.isEmpty(best_coupler_adr))
            {
                bool is_success = vehicleService.Command.MoveToCharge(vh.VEHICLE_ID, best_coupler_adr);
                return (is_success, "");
            }
            else
            {
                lineService.ProcessAlarmReport(vh, AlarmBLL.VEHICLE_CAN_NOT_FIND_THE_COUPLER_TO_CHARGING, ErrorStatus.ErrSet, $"vehicle:{vh.VEHICLE_ID} can't find coupler to charging");
                return (false, $"vehicle:{vh.VEHICLE_ID} can't find coupler to charging");
            }
        }
        public void askVhToChargerForWait(AVEHICLE vh)
        {
            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                     Data: $"ask vh:{vh.VEHICLE_ID} to charger idle.",
                     VehicleID: vh.VEHICLE_ID);
            askVhToCharging(vh);
        }

        public void askVhToChargerForWaitByManual(string vhID)
        {
            CMDBLL.CommandCheckResult check_result =
                CMDBLL.getOrSetCallContext<CMDBLL.CommandCheckResult>(CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
            try
            {
                AVEHICLE vh = vehicleBLL.cache.getVehicle(vhID);

                LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                         Data: $"ask vh:{vh.VEHICLE_ID} to charger idle.",
                         VehicleID: vh.VEHICLE_ID);
                var ask_result = askVhToCharging(vh);
                check_result.Result.AppendLine(ask_result.msg);
                check_result.IsSuccess &= ask_result.isSuccess;
            }
            catch (Exception ex)
            {
                logger.Error(ex, "Exception:");
                check_result.Result.AppendLine(ex.ToString());
                check_result.IsSuccess = false;
            }
        }



        private string findBestCoupler(string vh_current_address, bool isNeedToLongCharge)
        {
            string best_coupler_adr = string.Empty;
            //List<CouplerAddress> coupler_addresses = addressesBLL.cache.GetCouplerAddresses().ToList();
            List<CouplerAddress> coupler_addresses = addressesBLL.cache.GetEnableCouplerAddresses(unitBLL).ToList();
            //1.先計算過一次各個Address與Target Adr的距離後，再進行權重與距離的排序
            try
            {
                coupler_addresses.ForEach(coupler_address => coupler_address.setDistanceWithTargetAdr(guideBLL, vh_current_address));
                //coupler_addresses.Sort();
                if (isNeedToLongCharge)
                {
                    coupler_addresses.Sort(CouplerCompareForLongCharge);
                }
                else
                {
                    coupler_addresses.Sort(CouplerCompareForNormalCharge);
                }
            }
            catch { }
            List<KeyValuePair<string, int>> all_coupler_adr_and_distance = new List<KeyValuePair<string, int>>();
            foreach (CouplerAddress adr in coupler_addresses)
            {
                string coupler_adr = adr.ADR_ID;
                //1.確定路段是可以通的
                if (!guideBLL.IsRoadWalkable(vh_current_address, coupler_adr))
                {
                    continue;
                }
                //2.確認沒有車子在上面
                //if (!adr.hasVh(vehicleBLL) &&
                if (!adr.hasChargingVh(vehicleBLL) &&
                    !adr.hasVhGoing(vehicleBLL))
                {
                    best_coupler_adr = adr.ADR_ID;
                    break;
                }
            }
            return best_coupler_adr;
        }


        public int CouplerCompareForLongCharge(CouplerAddress coupler1, CouplerAddress coupler2)
        {
            int result;
            if (coupler1.Priority == coupler2.Priority && coupler1.DistanceWithTargetAdr == coupler2.DistanceWithTargetAdr)
            {
                result = 0;
            }
            else
            {
                if (coupler1.Priority > coupler2.Priority)
                {
                    result = -1;
                }
                else if (coupler1.Priority < coupler2.Priority)
                {
                    result = 1;
                }
                else if (coupler1.Priority == coupler2.Priority && coupler1.DistanceWithTargetAdr < coupler2.DistanceWithTargetAdr)
                {
                    result = -1;
                }
                else
                {
                    result = 1;
                }
            }
            return result;
        }

        public int CouplerCompareForNormalCharge(CouplerAddress coupler1, CouplerAddress coupler2)
        {
            int result;
            if (coupler1.Priority == coupler2.Priority && coupler1.DistanceWithTargetAdr == coupler2.DistanceWithTargetAdr)
            {
                result = 0;
            }
            else
            {
                if (coupler1.Priority > coupler2.Priority)
                {
                    result = 1;
                }
                else if (coupler1.Priority < coupler2.Priority)
                {
                    result = -1;
                }
                else if (coupler1.Priority == coupler2.Priority && coupler1.DistanceWithTargetAdr > coupler2.DistanceWithTargetAdr)
                {
                    result = 1;
                }
                else
                {
                    result = -1;
                }
            }
            return result;
        }


        public void CouplerTimingCheck()
        {
            //List<CouplerAddress> coupler_addresses = addressesBLL.cache.GetCouplerAddresses();
            List<CouplerAddress> coupler_addresses = addressesBLL.cache.GetEnableCouplerAddresses(unitBLL);
            foreach (CouplerAddress coupler_address in coupler_addresses)
            {
                if (coupler_address.hasVh(vehicleBLL))
                {
                    AVEHICLE ChargingVh = vehicleBLL.cache.getVhOnAddress(coupler_address.ADR_ID);

                    //1.如果N分鐘前，都沒有充飽過，就要等他充飽以後才可以再叫他回去工作
                    //if (ChargingVh.LAST_FULLY_CHARGED_TIME.HasValue &&
                    //   DateTime.Now > ChargingVh.LAST_FULLY_CHARGED_TIME?.AddMinutes(SystemParameter.TheLongestFullyChargedIntervalTime_Mim))
                    //{
                    //    if (ChargingVh.BatteryLevel == BatteryLevel.Full
                    //        && ChargingVh.MODE_STATUS == VHModeStatus.AutoCharging)
                    //    {
                    //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                    //                 Data: $"ask vh:{ChargingVh.VEHICLE_ID} recover to auto remmote " +
                    //                 $"and unban segment id:{string.Join(",", coupler_address.TrafficControlSegment)} ",
                    //                 VehicleID: ChargingVh.VEHICLE_ID);
                    //        vehicleService.changeVhStatusToAutoRemote(ChargingVh.VEHICLE_ID);
                    //        RoadControl(ChargingVh.VEHICLE_ID, ChargingVh.CUR_ADR_ID, true);
                    //    }
                    //    else
                    //    {
                    //        LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                    //                 Data: $"ChargingVh:{ChargingVh},Not yet charging condition." +
                    //                       $"a.charged time interval(min){SystemParameter.TheLongestFullyChargedIntervalTime_Mim}" +
                    //                       $"b.current battrty level:{ChargingVh.BatteryLevel} (need to be full)",
                    //                 VehicleID: ChargingVh.VEHICLE_ID);
                    //    }
                    //}
                    //else
                    {
                        //if (ChargingVh.BatteryLevel > BatteryLevel.Low &&
                        //    ChargingVh.MODE_STATUS == VHModeStatus.AutoCharging)
                        //if (ChargingVh.BatteryLevel > BatteryLevel.High &&
                        if (ChargingVh.BatteryLevel >= BatteryLevel.High &&
                            ChargingVh.MODE_STATUS == VHModeStatus.AutoCharging)
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                                     Data: $"ask vh:{ChargingVh.VEHICLE_ID} recover to auto remmote " +
                                     $"and unban segment id:{string.Join(",", coupler_address.TrafficControlSegment)} ",
                                     VehicleID: ChargingVh.VEHICLE_ID);
                            vehicleService.changeVhStatusToAutoRemote(ChargingVh.VEHICLE_ID);
                            RoadControl(ChargingVh.VEHICLE_ID, ChargingVh.CUR_ADR_ID, true);
                        }
                        else
                        {
                            LogHelper.Log(logger: logger, LogLevel: LogLevel.Debug, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                                     Data: $"ChargingVh:{ChargingVh},Not yet charging condition." +
                                           $"a.current battrty level:{ChargingVh.BatteryLevel} (need to be above High)",
                                     VehicleID: ChargingVh.VEHICLE_ID);
                        }
                    }
                }
            }

            //找出所有低電量的VH
            //List<AVEHICLE> low_level_battrty_vh = vehicleBLL.cache.loadLowBattrtyVh();
            //if (low_level_battrty_vh != null)
            //{
            //    foreach (AVEHICLE low_level_vh in low_level_battrty_vh)
            //    {
            //        AADDRESS current_adr = addressesBLL.cache.GetAddress(low_level_vh.CUR_ADR_ID);
            //        //確定它是在Auto charge mode 且 已經沒有在執行命令 且 目前所在的Addres不在充電站上 
            //        if (low_level_vh.MODE_STATUS == VHModeStatus.AutoCharging &&
            //            low_level_vh.ACT_STATUS == VHActionStatus.NoCommand &&
            //            !SCUtility.isEmpty(low_level_vh.CUR_ADR_ID) &&
            //            !(current_adr is CouplerAddress))
            //        {
            //            LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
            //                     Data: $"ask vh:{low_level_vh.VEHICLE_ID} to charging by timer. ",
            //                     VehicleID: low_level_vh.VEHICLE_ID);
            //            askVhToCharging(low_level_vh);
            //        }
            //    }
            //}

            //找出目前線內的VH，如果Mode = AutoCharging、No command的狀態且不在Coupler
            //1.如果處於低電位的話，則將他派送至Coupler，進行充電
            //2.如果是高於低電位的話，則將它切換回AutoRemote
            List<AVEHICLE> vhs = vehicleBLL.cache.loadAllVh();
            foreach (AVEHICLE vh in vhs)
            {
                if (SCUtility.isEmpty(vh.CUR_ADR_ID)) continue;
                AADDRESS current_adr = addressesBLL.cache.GetAddress(vh.CUR_ADR_ID);
                if (vh.BatteryLevel == BatteryLevel.Low)
                {
                    //確定它是在Auto charge mode 且 已經沒有在執行命令 且 目前所在的Addres不在充電站上 
                    if (vh.MODE_STATUS == VHModeStatus.AutoCharging &&
                        vh.ACT_STATUS == VHActionStatus.NoCommand &&
                        (!(current_adr is CouplerAddress) ||
                        //((current_adr is CouplerAddress) && !(current_adr as CouplerAddress).IsEnable))
                        ((current_adr is CouplerAddress) && !(current_adr as CouplerAddress).IsWork(unitBLL)))
                       )
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                                 Data: $"ask vh:{vh.VEHICLE_ID} to charging by timer. ",
                                 VehicleID: vh.VEHICLE_ID);
                        askVhToCharging(vh);
                    }
                }
                else if (vh.BatteryLevel > BatteryLevel.Low)
                {
                    if (vh.MODE_STATUS == VHModeStatus.AutoCharging &&
                        vh.ACT_STATUS == VHActionStatus.NoCommand &&
                        (!(current_adr is CouplerAddress) ||
                        //((current_adr is CouplerAddress) && !(current_adr as CouplerAddress).IsEnable))
                        //((current_adr is CouplerAddress) && !addressesBLL.cache.IsCouplerWork(current_adr as CouplerAddress, unitBLL)))
                        ((current_adr is CouplerAddress) && !(current_adr as CouplerAddress).IsWork(unitBLL)))
                       )
                    {
                        LogHelper.Log(logger: logger, LogLevel: LogLevel.Info, Class: nameof(VehicleChargerModule), Device: DEVICE_NAME,
                                 Data: $"ask vh:{vh.VEHICLE_ID} recover to auto remmote by timer",
                                 VehicleID: vh.VEHICLE_ID);
                        vehicleService.changeVhStatusToAutoRemote(vh.VEHICLE_ID);
                    }
                }
            }
        }

    }
}
