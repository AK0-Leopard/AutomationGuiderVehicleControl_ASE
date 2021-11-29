using com.mirle.ibg3k0.sc;
using com.mirle.ibg3k0.sc.Common;
using com.mirle.ibg3k0.sc.ObjectRelay;
using com.mirle.ibg3k0.sc.ProtocolFormat.OHTMessage;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace com.mirle.ibg3k0.bc.winform.UI
{
    public partial class HistoryTransferCommand : Form
    {
        BCMainForm mainform;
        List<HVTRANSFER> hvTran = null;
        List<HCMD_MCSObjToShow> showHCMD_MCSList = null;

        public HistoryTransferCommand(BCMainForm _mainForm)
        {
            InitializeComponent();
            dgv_TransferCommand.AutoGenerateColumns = false;
            mainform = _mainForm;
            showHCMD_MCSList = new List<HCMD_MCSObjToShow>();
            dgv_TransferCommand.DataSource = showHCMD_MCSList;

            var device_ids = new List<string>();
            device_ids.Add("");

            var vh_ids = _mainForm.BCApp.SCApplication.VehicleBLL.cache.loadAllVh().Select(v => v.VEHICLE_ID);

            device_ids.AddRange(vh_ids);
            m_EqptIDCbx.DataSource = device_ids;
            m_StartDTCbx.Value = DateTime.Today;
            m_EndDTCbx.Value = DateTime.Now;
        }

        const int MAX_HCMD_MCS_QUERY_COUNT = 500;
        DateTime preStartDateTime = DateTime.MinValue;
        DateTime preEndDateTime = DateTime.MinValue;
        private async void updateHCMD_MCS()
        {
            DateTime start_time = m_StartDTCbx.Value;
            DateTime end_time = m_EndDTCbx.Value;
            string device_id = m_EqptIDCbx.Text;
            string cst_id = txt_CSTID.Text;
            string lot_id = txt_LotID.Text;
            if (preStartDateTime != start_time || preEndDateTime != end_time)
            {
                try
                {
                    tableLayoutPanel6.Enabled = false;
                    int count = 0;
                    await Task.Run(() =>
                    {
                        count = mainform.BCApp.SCApplication.CMDBLL.getByInsertTimeEndTimeCount(start_time, end_time);
                    });
                    if (count > MAX_HCMD_MCS_QUERY_COUNT)
                    {
                        MessageBox.Show(this, $"HCMD_MCS query 數量超過:{MAX_HCMD_MCS_QUERY_COUNT}，請重新調整搜尋區間。"
                                            , "HCMD MCS Query"
                                            , MessageBoxButtons.OK
                                            , MessageBoxIcon.Information);
                        return;
                    }

                    await Task.Run(() =>
                    {
                        var hcmd_mcs = mainform.BCApp.SCApplication.CMDBLL.loadByInsertTimeEndTime(start_time, end_time);
                        hvTran = hcmd_mcs.ToList();
                        hvTran.ForEach(hv => sc.Common.SCUtility.TrimAllParameter(hv));
                    });
                    preStartDateTime = start_time;
                    preEndDateTime = end_time;
                }
                catch (Exception ex)
                {
                    return;
                }
                finally
                {
                    tableLayoutPanel6.Enabled = true;
                }
            }

            try
            {
                //tableLayoutPanel6.Enabled = false;
                var cmd_mcs_temp = hvTran.ToList();

                await Task.Run(() =>
                 {

                     if (hvTran != null && hvTran.Count > 0)
                     {

                         if (!SCUtility.isEmpty(device_id))
                         {
                             cmd_mcs_temp = cmd_mcs_temp.Where(cmd => SCUtility.isMatche(cmd.VH_ID, device_id)).ToList();
                         }
                         if (!SCUtility.isEmpty(cst_id))
                         {
                             cmd_mcs_temp = cmd_mcs_temp.Where(cmd => cmd.CARRIER_ID != null && cmd.CARRIER_ID.Contains(cst_id)).ToList();
                         }
                         if (!SCUtility.isEmpty(lot_id))
                         {
                             cmd_mcs_temp = cmd_mcs_temp.Where(cmd => cmd.LOT_ID != null && cmd.LOT_ID.Contains(lot_id)).ToList();
                         }
                         showHCMD_MCSList = cmd_mcs_temp.Select(cmd => new HCMD_MCSObjToShow(mainform.BCApp.SCApplication, cmd)).ToList();
                     }
                     else
                     {
                         showHCMD_MCSList = new List<HCMD_MCSObjToShow>();
                     }

                 });
                dgv_TransferCommand.DataSource = cmd_mcs_temp;
                dgv_TransferCommand.Refresh();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //tableLayoutPanel6.Enabled = true;
            }
        }


        private void TransferCommandQureyListForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mainform.removeForm(this.Name);
        }

        private void btnlSearch_Click(object sender, EventArgs e)
        {
            updateHCMD_MCS();
        }

        private async void m_exportBtn_Click(object sender, EventArgs e)
        {
            try
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.Filter = "Alarm files (*.xlsx)|*.xlsx";
                if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK || bcf.Common.BCFUtility.isEmpty(dlg.FileName))
                {
                    return;
                }
                string filename = dlg.FileName;
                //建立 xlxs 轉換物件
                Common.XSLXHelper helper = new Common.XSLXHelper();
                //取得轉為 xlsx 的物件
                ClosedXML.Excel.XLWorkbook xlsx = null;
                await Task.Run(() => xlsx = helper.Export(showHCMD_MCSList));
                if (xlsx != null)
                    xlsx.SaveAs(filename);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Exception");
            }
        }

        private void m_EqptIDCbx_SelectedIndexChanged(object sender, EventArgs e)
        {
            updateHCMD_MCS();
        }

        private void txt_boxID_TextChanged(object sender, EventArgs e)
        {
            updateHCMD_MCS();
        }

        private void txt_CSTID_TextChanged(object sender, EventArgs e)
        {
            updateHCMD_MCS();
        }

        private void txt_LotID_TextChanged(object sender, EventArgs e)
        {
            updateHCMD_MCS();
        }

        private void dgv_TransferCommand_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {

        }

        private void HistoryCMD_MCS_Load(object sender, EventArgs e)
        {
            Type dgv = dgv_TransferCommand.GetType();
            System.Reflection.PropertyInfo info = dgv.GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            info.SetValue(dgv_TransferCommand, true, null);
        }
    }
}
