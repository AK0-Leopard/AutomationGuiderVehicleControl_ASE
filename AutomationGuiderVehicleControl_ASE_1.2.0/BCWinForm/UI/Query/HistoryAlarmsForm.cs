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
    public partial class HistoryAlarmsForm : Form
    {
        BCMainForm mainform;
        BindingSource cmsMCS_bindingSource = new BindingSource();
        List<ALARM> alarmList = null;
        List<ALARMObjToShow> alarmShowList = null;
        public HistoryAlarmsForm(BCMainForm _mainForm)
        {
            InitializeComponent();
            dgv_alarms.AutoGenerateColumns = false;
            mainform = _mainForm;

            dgv_alarms.DataSource = cmsMCS_bindingSource;

            m_StartDTCbx.Value = DateTime.Today;
            m_EndDTCbx.Value = DateTime.Now;
            List<string> device_ids = new List<string>();
            device_ids.Add("");
            var vhs = mainform.BCApp.SCApplication.VehicleBLL.cache.loadAllVh();
            List<string> vh_ids = vhs.Select(v => v.VEHICLE_ID).ToList();
            var chargers = mainform.BCApp.SCApplication.UnitBLL.OperateCatch.loadUnits();
            List<string> charger_ids = chargers.Select(c => c.UNIT_ID).ToList();
            device_ids.AddRange(vh_ids);
            device_ids.AddRange(charger_ids);
            m_EqptIDCbx.DataSource = device_ids;
        }



        const int MAX_HCMD_MCS_QUERY_COUNT = 500;
        DateTime preStartDateTime = DateTime.MinValue;
        DateTime preEndDateTime = DateTime.MinValue;
        private async void updateAlarms()
        {
            DateTime start_time = m_StartDTCbx.Value;
            DateTime end_time = m_EndDTCbx.Value;
            string alarm_code = m_AlarmCodeTbl.Text;
            string device_id = m_EqptIDCbx.Text;
            string cst_id = txt_cstID.Text;
            if (preStartDateTime != start_time || preEndDateTime != end_time)
            {
                try
                {
                    tableLayoutPanel6.Enabled = false;
                    int count = 0;
                    await Task.Run(() =>
                    {
                        count = mainform.BCApp.SCApplication.AlarmBLL.GetAlarmCount(start_time, end_time);
                    });
                    if (count > MAX_HCMD_MCS_QUERY_COUNT)
                    {
                        MessageBox.Show(this, $"Alarm query 數量超過:{MAX_HCMD_MCS_QUERY_COUNT}，請重新調整搜尋區間。"
                                            , "Alarm Query"
                                            , MessageBoxButtons.OK
                                            , MessageBoxIcon.Information);
                        return;
                    }

                    await Task.Run(() =>
                    {
                        var alarm = mainform.BCApp.SCApplication.AlarmBLL.GetAlarms(start_time, end_time);
                        alarmList = alarm.ToList();
                        alarmList.ForEach(hv => sc.Common.SCUtility.TrimAllParameter(hv));
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
                var alarm_list_temp = alarmList.ToList();

                await Task.Run(() =>
                {

                    if (alarm_list_temp != null && alarm_list_temp.Count > 0)
                    {

                        if (!SCUtility.isEmpty(device_id))
                        {
                            alarm_list_temp = alarm_list_temp.Where(cmd => SCUtility.isMatche(cmd.EQPT_ID, device_id)).ToList();
                        }
                        if (!SCUtility.isEmpty(alarm_code))
                        {
                            alarm_list_temp = alarm_list_temp.Where(cmd => cmd.ALAM_CODE != null && cmd.ALAM_CODE.Contains(alarm_code)).ToList();
                        }
                        if (!SCUtility.isEmpty(cst_id))
                        {
                            alarm_list_temp = alarm_list_temp.Where(cmd =>
                            {
                                foreach (var c in cmd.RelatedCSTIDs)
                                {
                                    if (c.Contains(cst_id))
                                    {
                                        return true;
                                    }
                                }
                                return false;
                            }).ToList();
                        }
                        alarmShowList = alarm_list_temp.Select(cmd => new ALARMObjToShow(cmd)).ToList();
                    }
                    else
                    {
                        alarmShowList = new List<ALARMObjToShow>();
                    }

                });
                dgv_alarms.DataSource = alarmShowList;
                dgv_alarms.Refresh();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                //tableLayoutPanel6.Enabled = true;
            }
        }


        //private void updateAlarms()
        //{
        //    DateTime start_time = m_StartDTCbx.Value;
        //    DateTime end_time = m_EndDTCbx.Value;
        //    var alarms = mainform.BCApp.SCApplication.AlarmBLL.GetAlarms(start_time, end_time);
        //    if (alarms != null && alarms.Count > 0)
        //    {
        //        string alarm_code = m_AlarmCodeTbl.Text;
        //        string device_id = m_EqptIDCbx.Text;
        //        string cst_id = m_EqptIDCbx.Text;
        //        if (!SCUtility.isEmpty(alarm_code))
        //        {
        //            alarms = alarms.Where(alarm => SCUtility.isMatche(alarm.ALAM_CODE, alarm_code)).ToList();
        //        }
        //        if (!SCUtility.isEmpty(device_id))
        //        {
        //            alarms = alarms.Where(alarm => SCUtility.isMatche(alarm.EQPT_ID, device_id)).ToList();
        //        }
        //        if (!SCUtility.isEmpty(device_id))
        //        {
        //            alarms = alarms.Where(alarm => alarm.CMD_ID_1.Contains()).ToList();
        //        }
        //        alarmShowList = alarms.Select(alarm => new ALARMObjToShow(alarm)).ToList();
        //        cmsMCS_bindingSource.DataSource = alarmShowList;
        //        dgv_TransferCommand.Refresh();
        //    }
        //}

        private void TransferCommandQureyListForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            mainform.removeForm(this.Name);
        }

        private void btnlSearch_Click(object sender, EventArgs e)
        {
            updateAlarms();
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
                await Task.Run(() => xlsx = helper.Export(alarmShowList));
                if (xlsx != null)
                    xlsx.SaveAs(filename);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Warn(ex, "Exception");
            }
        }

        private void m_AlarmCodeTbl_TextChanged(object sender, EventArgs e)
        {
            updateAlarms();
        }

        private void txt_cstID_TextChanged(object sender, EventArgs e)
        {
            updateAlarms();
        }

        private void m_EqptIDCbx_SelectedValueChanged(object sender, EventArgs e)
        {
            updateAlarms();
        }
    }
}
