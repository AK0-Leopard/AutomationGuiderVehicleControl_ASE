using com.mirle.ibg3k0.bc.winform.App;
using com.mirle.ibg3k0.bc.winform.Common;
using com.mirle.ibg3k0.bcf.App;
using com.mirle.ibg3k0.bcf.Common;
using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.App;
using com.mirle.ibg3k0.sc.Data;
using com.mirle.ibg3k0.sc.Data.SECS.AGVC;
using com.mirle.ibg3k0.sc.Data.ValueDefMapAction;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static com.mirle.ibg3k0.sc.App.SCAppConstants;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class DebugFormNew : Form
    {

        BCMainForm mainForm;
        BCApplication bcApp;
        List<RadioButton> radioButtons = new List<RadioButton>();
#pragma warning disable CS0414 // 已指派欄位 'DebugForm.blocked_queues'，但從未使用過其值。
        List<BLOCKZONEQUEUE> blocked_queues = null;
#pragma warning restore CS0414 // 已指派欄位 'DebugForm.blocked_queues'，但從未使用過其值。
        protected sc.Data.SECSDriver.IBSEMDriver mcsMapAction = null;
        APORTSTATION P11 = null;
        APORTSTATION P12 = null;
        APORTSTATION P13 = null;
        APORTSTATION P14 = null;
        List<APORTSTATION> agvPortStation = null;
        List<AGVStation> agvStations = null;
        AEQPT mCharger = null;

        public DebugFormNew(BCMainForm _mainForm)
        {
            InitializeComponent();
            mainForm = _mainForm;
            bcApp = mainForm.BCApp;

            numer_num_of_avoid_seg.Value = DebugParameter.NumberOfAvoidanceSegment;
            ck_check_port_is_ready.Checked = DebugParameter.isNeedCheckPortReady;

            cb_passCouplerStatus.Checked = DebugParameter.isPassCouplerStatus;
            cb_passCouplerHPSafetySingnal.Checked = DebugParameter.isPassCouplerHPSafetySignal;
            cb_needCheckPortUpdateTime.Checked = DebugParameter.isNeedCheckPortUpDateTime;


            List<string> lstVh = new List<string>();
            lstVh.Add(string.Empty);
            lstVh.AddRange(bcApp.SCApplication.VehicleBLL.cache.loadAllVh().Select(vh => vh.VEHICLE_ID).ToList());
            string[] allVh = lstVh.ToArray();
            BCUtility.setComboboxDataSource(cmb_tcpipctr_Vehicle, allVh);
            BCUtility.setComboboxDataSource(cmb_mcsReportTestVHID, allVh);

            List<AADDRESS> allAddress_obj = bcApp.SCApplication.MapBLL.loadAllAddress();
            string[] allAdr_ID = allAddress_obj.Select(adr => adr.ADR_ID).ToArray();

            List<ASECTION> sections = bcApp.SCApplication.SectionBLL.cache.GetSections();
            string[] allSec_ID = sections.Select(sec => sec.SEC_ID).ToArray();

            cmb_pauseEvent.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.PauseEvent));
            cmb_pauseType.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.PauseType))
                                           .Cast<sc.ProtocolFormat.OHTMessage.PauseType>()
                                           .Where(e => e == sc.ProtocolFormat.OHTMessage.PauseType.All ||
                                                       e == sc.ProtocolFormat.OHTMessage.PauseType.Normal)
                                           .ToList();
            cb_Abort_Type.DataSource = Enum.GetValues(typeof(sc.ProtocolFormat.OHTMessage.CancelActionType))
                                           .Cast<sc.ProtocolFormat.OHTMessage.CancelActionType>()
                                           .Where(e => e == sc.ProtocolFormat.OHTMessage.CancelActionType.CmdAbort ||
                                                       e == sc.ProtocolFormat.OHTMessage.CancelActionType.CmdCancel)
                                           .ToList();
            cmbUnloadVhType.DataSource = Enum.GetValues(typeof(E_VH_TYPE));

            mcsMapAction = SCApplication.getInstance().getEQObjCacheManager().getLine().getMapActionByIdentityKey(typeof(ASEMCSDefaultMapAction).Name) as ASEMCSDefaultMapAction;

            cb_Cache_data_Name.Items.Add("");
            cb_Cache_data_Name.Items.Add("ASECTION");
            dgv_cache_object_data.AutoGenerateColumns = false;
            dgv_cache_object_data_portstation.AutoGenerateColumns = false;
            dgv_AGVStationInfo.AutoGenerateColumns = false;
            P11 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("AASTK250P01");
            P12 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("AASTK250P02");
            P13 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("CASTK010P01");
            P14 = bcApp.SCApplication.getEQObjCacheManager().getPortStation("CASTK010P02");

            num_BatteryLowBoundaryValue.Value = AVEHICLE.BATTERYLEVELVALUE_LOW;
            num_BatteryHighBoundaryValue.Value = AVEHICLE.BATTERYLEVELVALUE_HIGH;
            numer_pre_open_agv_station_distance.Value = sc.App.SystemParameter.OpenAGVStationCoverDistance_mm;
            num_tran_cmd_queue_time_out_ms.Value = sc.App.SystemParameter.TransferCommandExcuteTimeOut_mSec;
            cb_by_pass_shelf_status.Checked = sc.App.SystemParameter.IsByPassAGVShelfStatus;
            num_vh_idle_time.Value = sc.App.SystemParameter.AllowVhIdleTime_ms;
            num_timePriorityIncrement.Value = sc.App.SystemParameter.TransferCommandTimePriorityIncrement;

            agvPortStation = bcApp.SCApplication.PortStationBLL.OperateCatch.loadAllPortStation();
            dgv_cache_object_data_portstation.DataSource = agvPortStation;

            agvStations = bcApp.SCApplication.EqptBLL.OperateCatch.loadAllAGVStation();
            dgv_AGVStationInfo.DataSource = agvStations;
            string[] agv_station_ids = agvStations.Select(station => station.EQPT_ID).ToArray();
            BCUtility.setComboboxDataSource(cmbStationPortID, agv_station_ids.ToArray());

            tabControl1.TabPages.RemoveAt(1);

            mCharger = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger");
            if (mCharger != null)
            {
                foreach (AUNIT unit in mCharger.UnitList)
                {
                    cb_Charger.Items.Add(unit.UNIT_ID);
                    cb_ChargerID.Items.Add(unit.UNIT_ID);
                }
            }
            for (int i = 0; i < 15; i++)
            {
                cb_PIOCoupler.Items.Add(i + 1);
            }
            initChargerValue();
            registerEvent();

            if (sc.Common.SCUtility.isMatche(bcApp.LoginUserID, "ADMIN"))
            {
                cb_by_pass_shelf_status.Visible = true;
                cb_PassSafetyCheckToPause.Enabled = true;
            }
        }

        #region Charger
        private void initChargerValue()
        {
            if (mCharger != null)
            {
                agvcAliveChange(mCharger);
                MChargerAbnormalIndexChange(mCharger);
                foreach (AUNIT unit in mCharger.UnitList)
                {
                    ChargerAliveChange(unit);
                    CurrentSupplyStatusBlockChange(unit);
                    ChargerStatusIndexChange(unit);
                    ChargerCurrentParameterindexChange(unit);
                    CouplerChargeInfoIndexChange(unit);
                    PIOIndexChange(unit);
                }
            }
        }
        private void registerEvent()
        {
            string Handler = this.Name;
            if (mCharger != null)
            {
                mCharger.addEventHandler(Handler, BCFUtility.getPropertyName(() => mCharger.AGVCAliveIndex),
                    (s1, e1) => agvcAliveChange(mCharger));

                mCharger.addEventHandler(Handler, BCFUtility.getPropertyName(() => mCharger.AbnormalReportIndex),
                    (s1, e1) => MChargerAbnormalIndexChange(mCharger));

                foreach (AUNIT unit in mCharger.UnitList)
                {
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerAlive),
                        (s1, e1) => ChargerAliveChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.CurrentSupplyStatusBlock),
                        (s1, e1) => CurrentSupplyStatusBlockChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerStatusIndex),
                        (s1, e1) => ChargerStatusIndexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.ChargerCurrentParameterIndex),
                        (s1, e1) => ChargerCurrentParameterindexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.CouplerChargeInfoIndex),
                        (s1, e1) => CouplerChargeInfoIndexChange(unit));
                    unit.addEventHandler(Handler, BCFUtility.getPropertyName(() => unit.PIOIndex),
                        (s1, e1) => PIOIndexChange(unit));

                }
            }
        }
        private void unregisterEvent()
        {
            if (mCharger != null)
            {
                mCharger.RemoveAllEvents();

                foreach (AUNIT unit in mCharger.UnitList)
                {
                    unit.RemoveAllEvents();
                }
            }
        }

        private void agvcAliveChange(AEQPT eqpt)
        {


            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_agvcAlive.Text = eqpt.AGVCAliveIndex.ToString();
            }), null);

        }
        private void MChargerAbnormalIndexChange(AEQPT eqpt)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_ErrorReportIndex.Text = eqpt.AbnormalReportIndex.ToString();
                tb_ErrorReportCode1.Text = eqpt.abnormalReportCode01.ToString();
                tb_ErrorReportCode2.Text = eqpt.abnormalReportCode02.ToString();
                tb_ErrorReportCode3.Text = eqpt.abnormalReportCode03.ToString();
                tb_ErrorReportCode4.Text = eqpt.abnormalReportCode04.ToString();
                tb_ErrorReportCode5.Text = eqpt.abnormalReportCode05.ToString();
                tb_ErrorReportCode6.Text = eqpt.abnormalReportCode06.ToString();
                tb_ErrorReportCode7.Text = eqpt.abnormalReportCode07.ToString();
                tb_ErrorReportCode8.Text = eqpt.abnormalReportCode08.ToString();
                tb_ErrorReportCode9.Text = eqpt.abnormalReportCode09.ToString();
                tb_ErrorReportCode10.Text = eqpt.abnormalReportCode10.ToString();
                tb_ErrorReportCode11.Text = eqpt.abnormalReportCode11.ToString();
                tb_ErrorReportCode12.Text = eqpt.abnormalReportCode12.ToString();
                tb_ErrorReportCode13.Text = eqpt.abnormalReportCode13.ToString();
                tb_ErrorReportCode14.Text = eqpt.abnormalReportCode14.ToString();
                tb_ErrorReportCode15.Text = eqpt.abnormalReportCode15.ToString();
                tb_ErrorReportCode16.Text = eqpt.abnormalReportCode16.ToString();
                tb_ErrorReportCode17.Text = eqpt.abnormalReportCode17.ToString();
                tb_ErrorReportCode18.Text = eqpt.abnormalReportCode18.ToString();
                tb_ErrorReportCode19.Text = eqpt.abnormalReportCode19.ToString();
                tb_ErrorReportCode20.Text = eqpt.abnormalReportCode20.ToString();
            }), null);
        }
        private void ChargerAliveChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerAlive.Text = unit.ChargerAlive.ToString();
            }), null);
        }
        private void CurrentSupplyStatusBlockChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_inputVoltage.Text = unit.inputVoltage.ToString();
                tb_chargeVoltage.Text = unit.chargeVoltage.ToString();
                textBox_chargeCurrent.Text = unit.chargeCurrent.ToString();
                textBox_chargePower.Text = unit.chargePower.ToString();
                tb_couplerChargeVoltage.Text = unit.couplerChargeVoltage.ToString();
                tb_couplerChargeCurrent.Text = unit.couplerChargeCurrent.ToString();
                textBox_couplerID.Text = unit.couplerID.ToString();
            }), null);
        }
        private void ChargerStatusIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerStatusReportIndex.Text = unit.ChargerStatusIndex.ToString();
                tb_Reserve.Text = unit.chargerReserve.ToString();
                tb_ConstantVoOutput.Text = unit.chargerConstantVoltageOutput.ToString();
                tb_ContantCurrentOutput.Text = unit.chargerConstantCurrentOutput.ToString();
                tb_HighInputVoProtect.Text = unit.chargerHighInputVoltageProtection.ToString();
                tb_LowInputVoProtect.Text = unit.chargerLowInputVoltageProtection.ToString();
                tb_HighOutputVoProtect.Text = unit.chargerHighOutputVoltageProtection.ToString();
                tb_HighOutputCurrentProtect.Text = unit.chargerHighOutputCurrentProtection.ToString();
                tb_OverheatProtect.Text = unit.chargerOverheatProtection.ToString();
                tb_RS485Status.Text = unit.chargerRS485Status.ToString();
                tb_CouplerStatus1.Text = unit.Coupler1Status.ToString();
                tb_CouplerStatus2.Text = unit.Coupler2Status.ToString();
                tb_CouplerStatus3.Text = unit.Coupler3Status.ToString();
                tb_Coupler1HPSafety.Text = unit.coupler1HPSafety.ToString();
                tb_Coupler2HPSafety.Text = unit.coupler2HPSafety.ToString();
                tb_Coupler3HPSafety.Text = unit.coupler3HPSafety.ToString();
            }), null);
        }
        private void ChargerCurrentParameterindexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_ChargerCurrentParameterSettingIndex.Text = unit.ChargerCurrentParameterIndex.ToString();
                tb_OutputVo.Text = unit.chargerOutputVoltage.ToString();
                tb_OutputCurrent.Text = unit.chargerOutputCurrent.ToString();
                tb_OverloadVo.Text = unit.chargerOverVoltage.ToString();
                tb_OverloadCurrent.Text = unit.chargerOverCurrent.ToString();
            }), null);
        }
        private void CouplerChargeInfoIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                if (unit.UNIT_ID != cb_ChargerID.Text) return;
                tb_CouplerChargeInfoReportIndex.Text = unit.CouplerChargeInfoIndex.ToString();
                tb_CouplerID.Text = unit.chargerCouplerID.ToString();
                tb_ChargeStartTime.Text = unit.chargerChargingStartTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_ChargeEndTime.Text = unit.chargerChargingEndTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_InputAh.Text = unit.chargerInputAH.ToString();
                tb_ChargeResult.Text = unit.chargerChargingResult.ToString();
            }), null);
        }
        private void PIOIndexChange(AUNIT unit)
        {
            Adapter.Invoke(new SendOrPostCallback((o1) =>
            {
                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }), null);
        }
        private void cb_ChargerID_SelectedIndexChanged(object sender, EventArgs e)
        {
            AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID(cb_ChargerID.Text);
            if (unit != null)
            {
                cb_PassSafetyCheckToPause.Checked = unit.isPassSafetySignalToSendPause;

                tb_ChargerAlive.Text = unit.ChargerAlive.ToString();
                tb_inputVoltage.Text = unit.inputVoltage.ToString();
                tb_chargeVoltage.Text = unit.chargeVoltage.ToString();
                textBox_chargeCurrent.Text = unit.chargeCurrent.ToString();
                textBox_chargePower.Text = unit.chargePower.ToString();
                tb_couplerChargeVoltage.Text = unit.couplerChargeVoltage.ToString();
                tb_couplerChargeCurrent.Text = unit.couplerChargeCurrent.ToString();
                textBox_couplerID.Text = unit.couplerID.ToString();

                tb_ChargerStatusReportIndex.Text = unit.ChargerStatusIndex.ToString();
                tb_Reserve.Text = unit.chargerReserve.ToString();
                tb_ConstantVoOutput.Text = unit.chargerConstantVoltageOutput.ToString();
                tb_ContantCurrentOutput.Text = unit.chargerConstantCurrentOutput.ToString();
                tb_HighInputVoProtect.Text = unit.chargerHighInputVoltageProtection.ToString();
                tb_LowInputVoProtect.Text = unit.chargerLowInputVoltageProtection.ToString();
                tb_HighOutputVoProtect.Text = unit.chargerHighOutputVoltageProtection.ToString();
                tb_HighOutputCurrentProtect.Text = unit.chargerHighOutputCurrentProtection.ToString();
                tb_OverheatProtect.Text = unit.chargerOverheatProtection.ToString();
                tb_RS485Status.Text = unit.chargerRS485Status.ToString();
                tb_CouplerStatus1.Text = unit.Coupler1Status.ToString();
                tb_CouplerStatus2.Text = unit.Coupler2Status.ToString();
                tb_CouplerStatus3.Text = unit.Coupler3Status.ToString();
                tb_Coupler1HPSafety.Text = unit.coupler1HPSafety.ToString();
                tb_Coupler2HPSafety.Text = unit.coupler2HPSafety.ToString();
                tb_Coupler3HPSafety.Text = unit.coupler3HPSafety.ToString();

                tb_ChargerCurrentParameterSettingIndex.Text = unit.ChargerCurrentParameterIndex.ToString();
                tb_OutputVo.Text = unit.chargerOutputVoltage.ToString();
                tb_OutputCurrent.Text = unit.chargerOutputCurrent.ToString();
                tb_OverloadVo.Text = unit.chargerOverVoltage.ToString();
                tb_OverloadCurrent.Text = unit.chargerOverCurrent.ToString();

                tb_CouplerChargeInfoReportIndex.Text = unit.CouplerChargeInfoIndex.ToString();
                tb_CouplerID.Text = unit.chargerCouplerID.ToString();
                tb_ChargeStartTime.Text = unit.chargerChargingStartTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_ChargeEndTime.Text = unit.chargerChargingEndTime.ToString("yyyy/MM/dd hh:mm:ss");
                tb_InputAh.Text = unit.chargerInputAH.ToString();
                tb_ChargeResult.Text = unit.chargerChargingResult;

                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }

        }
        private void cb_PIOCoupler_SelectedIndexChanged(object sender, EventArgs e)
        {
            AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID(cb_ChargerID.Text);
            if (unit != null)
            {
                tb_PIOIndex.Text = unit.PIOIndex.ToString();
                int x;
                bool result = int.TryParse(cb_PIOCoupler.Text, out x);
                if (result)
                {
                    if (unit.PIOInfos.Count >= x)
                    {
                        tb_PIOCouplerID.Text = unit.PIOInfos[x - 1].CouplerID.ToString();
                        tb_PIOHandshakeTime.Text = unit.PIOInfos[x - 1].Timestamp.ToString("yyyy/MM/dd hh:mm:ss");
                        tb_PIOSignal1.Text = unit.PIOInfos[x - 1].signal1.Replace(",", "").Replace("True", "1").Replace("False", "0");
                        tb_PIOSignal2.Text = unit.PIOInfos[x - 1].signal2.Replace(",", "").Replace("True", "1").Replace("False", "0");
                    }
                }
            }
        }
        #endregion Charger




        private void DebugForm_Load(object sender, EventArgs e)
        {
            DebugParameter.IsDebugMode = true;
            //refreshCouplerStatusUI();
        }

        private void DebugForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            DebugParameter.IsDebugMode = false;
            DebugParameter.IsCycleRun = false;
            DebugParameter.CanAutoRandomGeneratesCommand = false;

            //DebugParameter.isForcedPassReserve = false;
            DebugParameter.isForcedRejectReserve = false;
            DebugParameter.isContinueByIDReadFail = false;
            DebugParameter.testRetryReserveReq = false;
            DebugParameter.testRetryLoadArrivals = false;
            DebugParameter.testRetryLoadComplete = false;
            DebugParameter.testRetryUnloadArrivals = false;
            DebugParameter.testRetryUnloadComplete = false;
            DebugParameter.testRetryVhloading = false;
            DebugParameter.testRetryVhunloading = false;
            DebugParameter.testRetryBcrread = false;
            DebugParameter.CommandCheckForcePass = false;
            DebugParameter.CommandCompleteWaitTime = 0;

            mainForm.removeForm(typeof(DebugFormNew).Name);

            unregisterEvent();

        }




        AVEHICLE noticeCar = null;
        string vh_id = null;
        private async void cmb_Vehicle_SelectedIndexChanged(object sender, EventArgs e)
        {
            vh_id = cmb_tcpipctr_Vehicle.Text.Trim();

            noticeCar = bcApp.SCApplication.getEQObjCacheManager().getVehicletByVHID(vh_id);
            lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
            lbl_listening_status.Text = noticeCar?.IsTcpIpListening(bcApp.SCApplication.getBCFApplication()).ToString();

            string[] command_ids = null;
            await Task.Run(() =>
            {
                command_ids = bcApp.SCApplication.CMDBLL.loadUnfinishCmd(vh_id)?.Select(cmd => cmd.ID).ToArray();
            });
            BCUtility.setComboboxDataSource(cmb_command_ids, command_ids);
        }

        private void uctl_Btn1_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.HostBasicVersionReport);
            //asyExecuteAction(noticeCar.sned_S1);
        }
        private void uctl_SendFun11_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.CoplerInfosReport);
            //asyExecuteAction(noticeCar.sned_S11);
        }
        private void uctl_SendFun13_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.TavellingDataReport);
            //asyExecuteAction(noticeCar.sned_S13);
        }
        private void uctl_SendFun15_Click(object sender, EventArgs e)
        {
        }
        private void uctl_SendFun17_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.AddressDataReport);
            //asyExecuteAction(noticeCar.sned_S17);
        }

        private void uctl_SendFun19_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.ScaleDataReport);
            //asyExecuteAction(noticeCar.sned_S19);
        }

        private void uctl_SendFun21_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.ControlDataReport);
            //asyExecuteAction(noticeCar.sned_S21);
        }

        private void uctl_SendFun23_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.GuideDataReport);
            //asyExecuteAction(noticeCar.sned_S23);
        }

        private void asyExecuteAction(Func<string, bool> act)
        {
            Task.Run(() =>
            {
                act(vh_id);
            });
        }

        private void uctl_SendAllFun_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.doDataSysc);
            //asyExecuteAction(noticeCar.sned_ALL);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.IndividualUploadRequest);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.IndividualChangeRequest);
        }


        private void button5_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.Send.AlarmReset);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                bool isSuccess = bcApp.SCApplication.CMDBLL.forceUpdataCmdStatus2FnishByVhID(vh_id);
                if (isSuccess)
                {
                    var vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(vh_id);
                    vh.VehicleUnassign();
                    vh.onExcuteCommandStatusChange();
                }

            });
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                //bcApp.SCApplication.VehicleService.forceResetVHStatus(vh_id);
                bcApp.SCApplication.VehicleService.Send.StatusRequest(vh_id, true);
            });
        }

        private void cb_StartGenAntoCmd_CheckedChanged(object sender, EventArgs e)
        {

            CheckBox cb = sender as CheckBox;
            DebugParameter.CanAutoRandomGeneratesCommand = cb.Checked;

            //if (!cb.Checked)
            //{
            //    Task.Run(() =>
            //    {
            //        var mcs_cmds = bcApp.SCApplication.CMDBLL.loadMCS_Command_Queue();
            //        foreach (var cmd in mcs_cmds)
            //            bcApp.SCApplication.CMDBLL.updateCMD_MCS_TranStatus2Complete(cmd.ID, E_TRAN_STATUS.Canceled);
            //    });
            //}
        }


        private void btn_pause_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.PauseEvent pauseEvent;
            sc.ProtocolFormat.OHTMessage.PauseType pauseType;
            Enum.TryParse(cmb_pauseEvent.SelectedValue.ToString(), out pauseEvent);
            Enum.TryParse(cmb_pauseType.SelectedValue.ToString(), out pauseType);
            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.Send.Pause(vh_id, pauseEvent, pauseType);
            });

        }




        private void label17_Click(object sender, EventArgs e)
        {

        }



        private void radio_bitX_Click(object sender, EventArgs e)
        {
            (sender as RadioButton).Checked = !(sender as RadioButton).Checked;
        }




        private void button8_Click(object sender, EventArgs e)
        {
        }

        private void button9_Click(object sender, EventArgs e)
        {
            sc.ProtocolFormat.OHTMessage.CancelActionType type;
            Enum.TryParse(cb_Abort_Type.SelectedValue.ToString(), out type);

            Task.Run(() =>
            {
                bcApp.SCApplication.VehicleService.Send.Cancel(noticeCar.VEHICLE_ID, noticeCar.CMD_ID_1, sc.ProtocolFormat.OHTMessage.CancelActionType.CmdCancel);
                //noticeCar.sned_Str37(noticeCar.OHTC_CMD, type); //todo kevin 要填入Command id
            });

        }

        private void button10_Click(object sender, EventArgs e)
        {

        }

        private void button11_Click(object sender, EventArgs e)
        {
        }

        private void btn_blocked_sec_refresh_Click(object sender, EventArgs e)
        {
        }



        private void ck_test_carrierinterface_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isTestCarrierInterfaceError = ck_test_carrierinterface_error.Checked;
        }

        private void button8_Click_1(object sender, EventArgs e)
        {
        }



        private void ck_autoTech_Click(object sender, EventArgs e)
        {

        }

        private void btn_reset_teach_result_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                List<ASECTION> sections = bcApp.SCApplication.MapBLL.loadAllSection();
                foreach (var sec in sections)
                {
                    if (bcApp.SCApplication.MapBLL.resetSecTechingTime(sec.SEC_ID))
                    {
                        sec.LAST_TECH_TIME = null;
                    }

                }
            });
        }

        private void btn_cmd_override_test_Click(object sender, EventArgs e)
        {
            string vh_id = cmb_tcpipctr_Vehicle.Text;
            Task.Run(() =>
            {
            });
        }

        private void uctl_SendFun2_Click(object sender, EventArgs e)
        {
            //asyExecuteAction(bcApp.SCApplication.VehicleService.BasicInfoVersionReport);
            asyExecuteAction(bcApp.SCApplication.VehicleService.HostBasicVersionReport);


        }

        private void cb_Cache_data_Name_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected_name = (sender as ComboBox).SelectedItem as string;

        }



        private void dgv_cache_object_data_EditModeChanged(object sender, EventArgs e)
        {

        }


        #region MTL Test


        private void btn_mtl_dateTimeSync_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                DateTime dateTime = DateTime.Now;
                //DateTime dateTime = DateTime.Now.AddMilliseconds(int.Parse(numericUpDownForDateTimeAddMilliseconds.Value.ToString()));
                mtl_mapaction.DateTimeSyncCommand(dateTime);
            }
            );
        }
        #endregion MTL Test

        private void btn_mtl_message_download_Click(object sender, EventArgs e)
        {
            bool is_enable = cb_all_coupler_enable.Checked;
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerAllCouplerEnable(is_enable);
            }
            );
        }

        private void btn_mtl_vh_realtime_info_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToMChargerAllChargerChargingFinish();
            }
            );
        }

        private void btn_mtl_car_out_notify_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getEquipmentByEQPTID("MCharger").
                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.ChargerValueDefMapAction;
                mtl_mapaction.AGVCToMChargerAlarmReset();
            }
            );
        }


        private void btn_coulper_enable_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cb_Charger.Text))
            {
                MessageBox.Show("Please select charger.");
                return;
            }
            if (string.IsNullOrWhiteSpace(txt_charger_coupler_id.Text))
            {
                MessageBox.Show("Please enter coupler ID.");
                return;
            }

            string charger_id = cb_Charger.Text.Trim();
            string coupler_id = txt_charger_coupler_id.Text.Trim();
            if (!uint.TryParse(coupler_id, out uint i_coupler_id))
            {
                MessageBox.Show("Coupler ID could only be numbers.");
                return;
            }
            //uint.TryParse(coupler_id, out uint i_coupler_id);
            bool is_enable = cb_coupler_enable.Checked;
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", charger_id).
                getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction)) as
                com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerCouplerEnable(i_coupler_id, is_enable);
            });
        }


        private void button7_Click_1(object sender, EventArgs e)
        {
            dgv_cache_object_data.Refresh();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            dgv_cache_object_data.Refresh();
        }


        private void btn_Charger1ForceFinish_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cb_Charger.Text))
            {
                MessageBox.Show("Please select charger.");
            }
            string charger_id = cb_Charger.Text.Trim();
            Task.Run(() =>
            {
                var mtl_mapaction = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", charger_id)
                .getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction)) as
                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction;
                mtl_mapaction.AGVCToChargerForceStopCharging();
            }
            );
        }

        private void btn_Charger1ReadStatus_Click(object sender, EventArgs e)
        {
            //            AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID("Charger1");
            //            Task.Run(() =>
            //            {
            //                //AUNIT unit = bcApp.SCApplication.getEQObjCacheManager().getUnitByUnitID("Charger1");
            //                var mtl_mapaction = unit.
            //                    getMapActionByIdentityKey(nameof(com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction)) as
            //                    com.mirle.ibg3k0.sc.Data.ValueDefMapAction.SubChargerValueDefMapAction;
            //                mtl_mapaction.ChargerCurrentStatus();

            //                Adapter.BeginInvoke(new SendOrPostCallback((o1) =>
            //                {
            //                    textBox_inputVoltage.Text = unit.inputVoltage.ToString();
            //                    textBox_chargeVoltage.Text = unit.chargeVoltage.ToString();
            //                    textBox_chargeCurrent.Text = unit.chargeCurrent.ToString();
            //                    textBox_chargePower.Text = unit.chargePower.ToString();
            //                    textBox_couplerChargeVoltage.Text = unit.couplerChargeVoltage.ToString();
            //                    textBox_couplerChargeCurrent.Text = unit.couplerChargeCurrent.ToString();
            //                    textBox_couplerID.Text = unit.couplerID.ToString();
            //                }), null);

            //            }
            //);

        }
        //private void btn_CouplerStatusRefresh_Click(object sender, EventArgs e)
        //{
        //    refreshCouplerStatusUI();
        //}

        //private void refreshCouplerStatusUI()
        //{
        //    AUNIT unit1 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger1");
        //    AUNIT unit2 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger2");
        //    AUNIT unit3 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger3");
        //    AUNIT unit4 = bcApp.SCApplication.getEQObjCacheManager().getUnit("MCharger", "Charger4");

        //    textBox_C11_Status.Text = unit1.coupler1Status.ToString();
        //    textBox_C12_Status.Text = unit1.coupler2Status.ToString();
        //    textBox_C13_Status.Text = unit1.coupler3Status.ToString();
        //    textBox_C21_Status.Text = unit2.coupler1Status.ToString();
        //    textBox_C22_Status.Text = unit2.coupler2Status.ToString();
        //    textBox_C23_Status.Text = unit2.coupler3Status.ToString();
        //    textBox_C31_Status.Text = unit3.coupler1Status.ToString();
        //    textBox_C32_Status.Text = unit3.coupler2Status.ToString();
        //    textBox_C33_Status.Text = unit3.coupler3Status.ToString();
        //    textBox_C41_Status.Text = unit4.coupler1Status.ToString();
        //    textBox_C42_Status.Text = unit4.coupler2Status.ToString();
        //    textBox_C43_Status.Text = unit4.coupler3Status.ToString();
        //}

        private void btn_online_Click(object sender, EventArgs e)
        {
            SCApplication scApp = SCApplication.getInstance();
            scApp.LineService.OnlineRemoteWithHost();
        }

        private void btnPortEnableSet_Click(object sender, EventArgs e)
        {

        }

        private void num_batteryCapacity_ValueChanged(object sender, EventArgs e)
        {
            DebugParameter.BatteryCapacity = (uint)num_batteryCapacity.Value;
        }

        private void btn_auto_remote_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoRemote);

        }


        private void num_BatteryLowBoundaryValue_ValueChanged(object sender, EventArgs e)
        {
            AVEHICLE.SettingBatteryLevelLowBoundary((UInt16)num_BatteryLowBoundaryValue.Value);

        }

        private void num_BatteryHighBoundaryValue_ValueChanged(object sender, EventArgs e)
        {
            AVEHICLE.SettingBatteryLevelHighBoundary((UInt16)num_BatteryHighBoundaryValue.Value);
        }

        private void btn_refresf_portsation_info_Click(object sender, EventArgs e)
        {
            dgv_cache_object_data_portstation.Refresh();

        }


        private void numer_num_of_avoid_seg_ValueChanged(object sender, EventArgs e)
        {
            DebugParameter.NumberOfAvoidanceSegment = (int)numer_num_of_avoid_seg.Value;
        }

        private void btn_loadArrivals_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.LoadArrivals;
            McsReportEventTest(report_event);
        }

        private void McsReportEventTest(sc.ProtocolFormat.OHTMessage.EventType report_event,
            sc.ProtocolFormat.OHTMessage.BCRReadResult bCRReadResult = sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrNormal)
        {
            string cst_id = txt_mcsReportTestCstID.Text;
            string cmd_id = txt_mcsReportTestCmdID.Text;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            var id_136 = new sc.ProtocolFormat.OHTMessage.ID_136_TRANS_EVENT_REP()
            {
                EventType = report_event,
                CSTID = cst_id,
                BCRReadResult = bCRReadResult,
                CmdID = cmd_id
            };
            var bcfApp = bcApp.SCApplication.getBCFApplication();
            Task.Run(() =>
            {
                dynamic recive_processor = bcApp.SCApplication.VehicleService.Receive;
                //bcApp.SCApplication.VehicleService.Receive.TranEventReport(bcfApp, test_report_vh, id_136, 0);
                recive_processor.TranEventReport(bcfApp, test_report_vh, id_136, 0);
            });
        }

        private void btn_vhloading_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Vhloading;
            McsReportEventTest(report_event);
        }

        private void btn_loadComplete_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.LoadComplete;
            McsReportEventTest(report_event);
        }

        private void btn_unloadArrivals_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.UnloadArrivals;
            McsReportEventTest(report_event);
        }

        private void btn_vhunloading_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Vhunloading;
            McsReportEventTest(report_event);
        }

        private void btn_unloadComplete_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.UnloadComplete;
            McsReportEventTest(report_event);
        }




        private void btn_id_bcr_read_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event);
        }

        private void btn_bcrReadMismatch_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event, sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrMisMatch);

            //var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.IdmisMatch;
            //McsCommandCompleteTest(completeStatus);
        }

        private void btn_bcrReadError_Click(object sender, EventArgs e)
        {
            var report_event = sc.ProtocolFormat.OHTMessage.EventType.Bcrread;
            McsReportEventTest(report_event, sc.ProtocolFormat.OHTMessage.BCRReadResult.BcrReadFail);
            //var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.IdreadFailed;
            //McsCommandCompleteTest(completeStatus);
        }

        private void btn_cancel_cmp_Click(object sender, EventArgs e)
        {

        }
        private void btn_cancelFail_Click(object sender, EventArgs e)
        {

        }

        private void btn_interlockError_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.InterlockError;

            McsCommandCompleteTest(completeStatus);
        }

        private void McsCommandCompleteTest(sc.ProtocolFormat.OHTMessage.CompleteStatus completeStatus)
        {
            string cmd_id = txt_mcsReportTestCmdID.Text;
            string cst_id = txt_mcsReportTestCstID.Text;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            var id_132 = new sc.ProtocolFormat.OHTMessage.ID_132_TRANS_COMPLETE_REPORT()
            {
                CmdID = cmd_id,
                CSTID = cst_id,
                CmpStatus = completeStatus
            };
            var bcfApp = bcApp.SCApplication.getBCFApplication();
            Task.Run(() => bcApp.SCApplication.VehicleService.Receive.CommandCompleteReport("", bcfApp, test_report_vh, id_132, 0));
        }

        private void btn_alarmtSet_Click(object sender, EventArgs e)
        {
            string error_code = "-999";
            var error_status = sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrSet;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            Task.Run(() => bcApp.SCApplication.LineService.ProcessAlarmReport(test_report_vh, error_code, error_status, ""));
        }

        private void btn_alarmClear_Click(object sender, EventArgs e)
        {
            string error_code = "-999";
            var error_status = sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset;
            AVEHICLE test_report_vh = bcApp.SCApplication.VehicleBLL.cache.getVehicle(cmb_mcsReportTestVHID.Text);
            Task.Run(() => bcApp.SCApplication.LineService.ProcessAlarmReport(test_report_vh, error_code, error_status, ""));
        }

        private void btn_cmpIdMismatch_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.IdmisMatch;

            McsCommandCompleteTest(completeStatus);
        }

        private void btn_idReadError_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.IdreadFailed;

            McsCommandCompleteTest(completeStatus);
        }

        private void btn_cmp_vh_abort_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.VehicleAbort;

            McsCommandCompleteTest(completeStatus);
        }


        private void btn_auto_charge_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoCharging);
        }

        private void btn_auto_local_Click(object sender, EventArgs e)
        {
            asyExecuteAction(bcApp.SCApplication.VehicleService.changeVhStatusToAutoLocal);
        }

        private async void btn_changeToRemove_Click(object sender, EventArgs e)
        {
            try
            {
                if (!noticeCar.IS_INSTALLED)
                {
                    MessageBox.Show($"{vh_id} is removed ready!");
                    return;
                }
                btn_changeToRemove.Enabled = false;
                (bool isSuccess, string result) check_result = default((bool isSuccess, string result));
                await Task.Run(() => check_result = bcApp.SCApplication.VehicleService.Remove(vh_id));
                if (check_result.isSuccess)
                {
                    MessageBox.Show($"{vh_id} remove ok");
                }
                else
                {
                    MessageBox.Show($"{vh_id} remove fail.{Environment.NewLine}" +
                                    $"result:{check_result.result}");
                }
                lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
            }
            finally
            {
                btn_changeToRemove.Enabled = true;
            }
        }

        private async void btn_changeToInstall_Click(object sender, EventArgs e)
        {
            try
            {
                if (noticeCar.IS_INSTALLED)
                {
                    MessageBox.Show($"{vh_id} is install ready!");
                    return;
                }

                btn_changeToInstall.Enabled = false;
                (bool isSuccess, string result) check_result = default((bool isSuccess, string result));
                await Task.Run(() => check_result = bcApp.SCApplication.VehicleService.Install(vh_id));
                if (check_result.isSuccess)
                {
                    MessageBox.Show($"{vh_id} install ok");
                }
                else
                {
                    MessageBox.Show($"{vh_id} install fail.{Environment.NewLine}" +
                                    $"result:{check_result.result}");
                }
                lbl_install_status.Text = noticeCar?.IS_INSTALLED.ToString();
            }
            finally
            {
                btn_changeToInstall.Enabled = true;
            }
        }

        int COUPLER_TEST_INDEX = 0;
        private void btn_coupler_enable_Click(object sender, EventArgs e)
        {
            COUPLER_TEST_INDEX++;
            if (bcApp.SCApplication.getBCFApplication().tryGetReadValueEventstring("Unit", "Charger1", "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER1_STATUS", out bcf.Controller.ValueRead vr_status))
            {
                vr_status.Value = new int[] { 1 };
            }
            if (bcApp.SCApplication.getBCFApplication().tryGetReadValueEventstring("Unit", "Charger1", "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_INDEX", out bcf.Controller.ValueRead vr))
            {
                vr.Value = new int[] { COUPLER_TEST_INDEX };
            }

        }

        private void btn_coupler_error_Click(object sender, EventArgs e)
        {
            COUPLER_TEST_INDEX++;
            if (bcApp.SCApplication.getBCFApplication().tryGetReadValueEventstring("Unit", "Charger1", "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_COUPLER1_STATUS", out bcf.Controller.ValueRead vr_status))
            {
                vr_status.Value = new int[] { 3 };
            }
            if (bcApp.SCApplication.getBCFApplication().tryGetReadValueEventstring("Unit", "Charger1", "CHARGERX_TO_AGVC_CHARGER_STATUS_REPORT_INDEX", out bcf.Controller.ValueRead vr))
            {
                vr.Value = new int[] { COUPLER_TEST_INDEX };
            }
        }

        private void btn_set_adr_Click(object sender, EventArgs e)
        {

        }

        private void btn_cmp_vh_complete_Click(object sender, EventArgs e)
        {
            var completeStatus = sc.ProtocolFormat.OHTMessage.CompleteStatus.Loadunload;

            McsCommandCompleteTest(completeStatus);
        }

        private void cb_canUnloadToAGVStation_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.CanUnloadToAGVStationTest = cb_canUnloadToAGVStation.Checked;
        }


        private void btn_refresh_agvstationInfo_Click(object sender, EventArgs e)
        {
            dgv_AGVStationInfo.Refresh();
        }

        private void ck_check_port_is_ready_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isNeedCheckPortReady = ck_check_port_is_ready.Checked;
        }

        private void numer_pre_open_agv_station_distance_ValueChanged(object sender, EventArgs e)
        {
            sc.App.SystemParameter.setOpenAGVStationCoverDistance((int)numer_pre_open_agv_station_distance.Value);
        }

        private void cb_by_pass_shelf_status_CheckedChanged(object sender, EventArgs e)
        {
            sc.App.SystemParameter.setIsByPassAGVShelfStatus(cb_by_pass_shelf_status.Checked);
        }

        private void num_tran_cmd_queue_time_out_ms_ValueChanged(object sender, EventArgs e)
        {
            sc.App.SystemParameter.setTransferCommandQueueTimeOut_mSec((int)num_tran_cmd_queue_time_out_ms.Value);
        }

        private async void button11_Click_1(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                bcApp.SCApplication.LineService.ProcessAlarmReport("AGVC", "0", sc.ProtocolFormat.OHTMessage.ErrorStatus.ErrReset,
                            $"");
            });
        }

        private void lbl_listening_status_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && (ModifierKeys & Keys.Control) == Keys.Control)
            {
                btn_open_tcp_port.Visible = true;
                btn_close_tcp_port.Visible = true;
            }
        }

        private async void btn_open_tcp_port_Click(object sender, EventArgs e)
        {
            bool is_success = false;
            await Task.Run(() =>
            {
                is_success = bcApp.SCApplication.VehicleService.startVehicleTcpIpServer(vh_id);
            });
            MessageBox.Show(is_success ? "OK" : "NG");
        }

        private async void btn_close_tcp_port_Click(object sender, EventArgs e)
        {
            bool is_success = false;
            await Task.Run(() =>
            {
                is_success = bcApp.SCApplication.VehicleService.stopVehicleTcpIpServer(vh_id);
            });
            MessageBox.Show(is_success ? "OK" : "NG");
        }

        private void cb_passCouplerStatus_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isPassCouplerStatus = cb_passCouplerStatus.Checked;
        }

        private void cb_passCouplerHPSafetySingnal_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isPassCouplerHPSafetySignal = cb_passCouplerHPSafetySingnal.Checked;
        }

        private void num_vh_idle_time_ValueChanged(object sender, EventArgs e)
        {
            sc.App.SystemParameter.setAllowIdleTime_ms((int)num_vh_idle_time.Value);
        }

        private void cb_needCheckPortUpdateTime_CheckedChanged(object sender, EventArgs e)
        {
            DebugParameter.isNeedCheckPortUpDateTime = cb_needCheckPortUpdateTime.Checked;
        }

        private async void btnUpdateUnloadVhType_Click(object sender, EventArgs e)
        {
            try
            {
                string port_id = cmbStationPortID.Text;
                E_VH_TYPE vh_type;
                Enum.TryParse(cmbUnloadVhType.SelectedValue.ToString(), out vh_type);
                btnUpdateUnloadVhType.Enabled = false;
                await Task.Run(() => bcApp.SCApplication.PortStationService.doUpdatePortUnloadVhType(port_id, vh_type));
            }
            finally
            {
                btnUpdateUnloadVhType.Enabled = true;
            }
        }

        private void dgv_AGVStationInfo_SelectionChanged(object sender, EventArgs e)
        {

        }

        private void cmbStationPortID_SelectedIndexChanged(object sender, EventArgs e)
        {
            string port_id = cmbStationPortID.Text;
            var port_st_obj = bcApp.SCApplication.PortStationBLL.OperateCatch.getPortStation(port_id);
            cmbUnloadVhType.SelectedItem = port_st_obj.ULD_VH_TYPE;
        }

        private void button12_Click(object sender, EventArgs e)
        {
            lbl_guideTimes.Text = DebugParameter.GuideSearchTimes.ToString();
            lbl_guideQuickTimes.Text = DebugParameter.GuideQuickSearchTimes.ToString();
        }

        private void num_timePriorityIncrement_ValueChanged(object sender, EventArgs e)
        {
            sc.App.SystemParameter.setTransferCommandTimePriorityIncrement((int)num_timePriorityIncrement.Value);
        }

        private void cb_PassSafetyCheckToPause_CheckedChanged(object sender, EventArgs e)
        {
            string current_selected_charger_id = cb_ChargerID.Text;
            AUNIT charger = bcApp.SCApplication.UnitBLL.OperateCatch.getUnit(current_selected_charger_id);
            if (charger == null) return;
            charger.isPassSafetySignalToSendPause = cb_PassSafetyCheckToPause.Checked;
        }
    }
}
