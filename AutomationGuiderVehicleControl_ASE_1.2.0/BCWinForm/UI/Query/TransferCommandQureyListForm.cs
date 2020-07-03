using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;
using com.mirle.ibg3k0.bc.winform.Common;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class TransferCommandQureyListForm : Form
    {
        BCMainForm mainform;
        BindingSource cmsMCS_bindingSource = new BindingSource();
        List<TRANSFERObjToShow> cmdMCSshowList = null;
        int selection_index = -1;
        public TransferCommandQureyListForm(BCMainForm _mainForm)
        {
            InitializeComponent();
            dgv_TransferCommand.AutoGenerateColumns = false;
            mainform = _mainForm;

            dgv_TransferCommand.DataSource = cmsMCS_bindingSource;

            List<string> lstVh = new List<string>();
            lstVh.Add(string.Empty);
            lstVh.AddRange(mainform.BCApp.SCApplication.VehicleBLL.cache.loadAllVh().Select(vh => vh.VEHICLE_ID).ToList());
            string[] allVh = lstVh.ToArray();
            BCUtility.setComboboxDataSource(cmb_force_assign, allVh);

        }

        private void updateTransferCommand()
        {
            //var mcs_commands = mainform.BCApp.SCApplication.CMDBLL.loadUnfinishedTransfer();
            ALINE line = mainform.BCApp.SCApplication.getEQObjCacheManager().getLine();
            var transfers = line.CurrentExcuteTransferCommand;
            cmdMCSshowList = transfers.
                Select(mcs_cmd => new TRANSFERObjToShow(mainform.BCApp.SCApplication.PortStationBLL, mcs_cmd)).
                ToList();
            cmsMCS_bindingSource.DataSource = cmdMCSshowList;
            dgv_TransferCommand.Refresh();
        }

        private void btn_refresh_Click(object sender, EventArgs e)
        {
            selection_index = -1;
            updateTransferCommand();
        }

        private async void btn_cancel_abort_Click(object sender, EventArgs e)
        {
            try
            {
                if (selection_index == -1) return;
                btn_cancel_abort.Enabled = false;
                var mcs_cmd = cmdMCSshowList[selection_index];
                CancelActionType cnacel_type = default(CancelActionType);
                if (mcs_cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Transferring)
                {
                    cnacel_type = CancelActionType.CmdCancel;
                }
                else if (mcs_cmd.TRANSFERSTATE < sc.E_TRAN_STATUS.Canceling)
                {
                    cnacel_type = CancelActionType.CmdAbort;
                }
                else
                {
                    MessageBox.Show($"Command ID:{mcs_cmd.CMD_ID.Trim()} can't excute cancel / abort,\r\ncurrent state:{mcs_cmd.TRANSFERSTATE}", "Cancel / Abort command fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                await Task.Run(() => mainform.BCApp.SCApplication.TransferService.AbortOrCancel(mcs_cmd.CMD_ID, cnacel_type));
                updateTransferCommand();
            }
            catch { }
            finally
            {
                btn_cancel_abort.Enabled = true;
            }
        }

        private void dgv_TransferCommand_SelectionChanged(object sender, EventArgs e)
        {
            if (dgv_TransferCommand.SelectedRows.Count > 0)
                selection_index = dgv_TransferCommand.SelectedRows[0].Index;
        }

        private async void btn_force_finish_Click(object sender, EventArgs e)
        {
            try
            {
                if (selection_index == -1) return;
                btn_force_finish.Enabled = false;
                var mcs_cmd = cmdMCSshowList[selection_index];

                ACMD cmd = mainform.BCApp.SCApplication.CMDBLL.GetCommandByTransferCmdID(mcs_cmd.CMD_ID);
                ATRANSFER transfer = mainform.BCApp.SCApplication.CMDBLL.GetTransferByID(mcs_cmd.CMD_ID);
                if (transfer == null)
                {
                    MessageBox.Show($"Transfer cmd ID:{SCUtility.Trim(mcs_cmd.CMD_ID, true)} not exist.", "Check command fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                CarrierLocationChooseForm carrierLocationChooseForm = new CarrierLocationChooseForm(mainform.BCApp.SCApplication, transfer);
                System.Windows.Forms.DialogResult result = carrierLocationChooseForm.ShowDialog(this);
                if (result != DialogResult.OK) return;
                string finial_carrier_location = carrierLocationChooseForm.GetChooseLocation();
                CompleteStatus finish_complete_status = carrierLocationChooseForm.GetCompleteStatus();

                await Task.Run(() =>
                {
                    try
                    {
                        //mainform.BCApp.SCApplication.VehicleService.Command.Finish(cmd.ID, CompleteStatus.ForceFinishByOp);
                        //todo 需要在review一下 kevin
                        ACARRIER carrier = mainform.BCApp.SCApplication.CarrierBLL.db.getCarrier(mcs_cmd.CARRIER_ID);

                        bool is_in_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.
                        IsVehicleLocationExistByLocationRealID(finial_carrier_location);
                        E_CARRIER_STATE finial_carrier_state =
                        is_in_vh ? E_CARRIER_STATE.Installed : E_CARRIER_STATE.Complete;
                        if (carrier != null)
                        {
                            //如果原本是在車上，但後來變成不在車上要上報Remove
                            //如果原本不在車在，後來變成在車上要在報Install
                            bool source_is_in_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.
                            IsVehicleLocationExistByLocationRealID(carrier.LOCATION);
                            if (is_in_vh && !source_is_in_vh)
                            {
                                AVEHICLE intall_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.getVehicleByLocationRealID(finial_carrier_location);
                                if (intall_vh != null)
                                    mainform.BCApp.SCApplication.ReportBLL.newReportCarrierInstalled
                                    (intall_vh.Real_ID, mcs_cmd.CARRIER_ID, finial_carrier_location, null);
                            }
                            else if (!is_in_vh && source_is_in_vh)
                            {
                                AVEHICLE remove_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.getVehicleByLocationRealID(carrier.LOCATION);
                                if (remove_vh != null)
                                    mainform.BCApp.SCApplication.ReportBLL.newReportCarrierRemoved
                                    (remove_vh.Real_ID, mcs_cmd.CARRIER_ID, finial_carrier_location, null);
                            }
                            mainform.BCApp.SCApplication.CarrierBLL.db.updateLocationAndState
                            (transfer.CARRIER_ID, finial_carrier_location, finial_carrier_state);
                        }

                        if (cmd != null)
                        {
                            mainform.BCApp.SCApplication.VehicleService.Command.Finish(cmd.ID, finish_complete_status);
                        }
                        else
                        {
                            mainform.BCApp.SCApplication.TransferService.FinishTransferCommand(transfer.ID, finish_complete_status);
                        }
                    }
                    catch { }
                }
                );
                updateTransferCommand();
            }
            catch (Exception ex) { }
            finally
            {
                btn_force_finish.Enabled = true;
            }
        }

        private void TransferCommandQureyListForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mainform.removeForm(this.Name);
        }

        private async void btn_force_assign_Click(object sender, EventArgs e)
        {
            string selected_vh_id = cmb_force_assign.Text;
            try
            {
                if (selection_index == -1)
                {
                    MessageBox.Show("Please select transfer command. ", "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                btn_force_finish.Enabled = false;
                var mcs_cmd = cmdMCSshowList[selection_index];
                AVEHICLE excute_cmd_of_vh = mainform.BCApp.SCApplication.VehicleBLL.cache.getVehicle(selected_vh_id);
                ATRANSFER transfer = mainform.BCApp.SCApplication.CMDBLL.GetTransferByID(mcs_cmd.CMD_ID);
                sc.BLL.CMDBLL.CommandCheckResult check_result_info = null;
                await Task.Run(() =>
                {
                    try
                    {
                        mainform.BCApp.SCApplication.TransferService.AssignTransferToVehicle(transfer, excute_cmd_of_vh);
                        check_result_info = sc.BLL.CMDBLL.getCallContext<sc.BLL.CMDBLL.CommandCheckResult>
                                                                           (sc.BLL.CMDBLL.CALL_CONTEXT_KEY_WORD_OHTC_CMD_CHECK_RESULT);
                    }
                    catch { }
                }
                );
                updateTransferCommand();
                if (check_result_info != null && !check_result_info.IsSuccess)
                {
                    MessageBox.Show(check_result_info.ToString(), "Command create fail.", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                {
                    MessageBox.Show("OK", "Command create success.", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch { }
            finally
            {
                btn_force_finish.Enabled = true;
            }
        }

        private void cmb_force_assign_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
