﻿using System.Drawing;
using System.Windows.Forms;


namespace com.mirle.ibg3k0.bc.winform.UI.Components
{
    public partial class uctlPortNew : UserControl
    {
        #region "Internal Variable"

        private string m_sPortName;
        private string m_sNote;
        private string m_sAddress;
        private int m_iLocX;
        private int m_iLocY;
        private int m_iSizeW;
        private int m_iSizeH;
        private Color m_clrColor;

#pragma warning disable CS0169 // 欄位 'uctlPortNew.m_iStatus' 從未使用過
        private int m_iStatus;
#pragma warning restore CS0169 // 欄位 'uctlPortNew.m_iStatus' 從未使用過

        #endregion	/* Internal Variable */
        #region "Property"

        /// <summary>
        /// Object Name
        /// </summary>
        public string p_PortName
        {
            get { return (m_sPortName); }
            set
            {
                m_sPortName = value;
            }
        }
        public string p_Note
        {
            get { return (m_sNote); }
            set
            {
                m_sNote = value;
            }
        }

        public string p_Address
        {
            get { return (m_sAddress); }
            set
            {
                m_sAddress = value;
            }
        }

        public int p_LocX
        {
            get { return (m_iLocX); }
            set
            {
                m_iLocX = value;
                _ChangePortImage();
            }
        }

        public int p_LocY
        {
            get { return (m_iLocY); }
            set
            {
                m_iLocY = value;
                _ChangePortImage();
            }
        }

        public int p_SizeW
        {
            get { return (m_iSizeW); }
            set
            {
                m_iSizeW = value;
                this.Width = value;
                _ChangePortImage();
            }
        }

        public int p_SizeH
        {
            get { return (m_iSizeH); }
            set
            {
                m_iSizeH = value;
                this.Height = value;
                _ChangePortImage();
            }
        }

        public Color p_Color
        {
            get { return (m_clrColor); }
            set
            {
                m_clrColor = value;
                _ChangePortImage();
            }
        }




        #endregion	/* Property */


        public uctlPortNew()
        {
            InitializeComponent();
        }

        private void _ChangePortImage()
        {
            this.Left = this.m_iLocX - (this.Width / 2);
            this.Top = this.m_iLocY - (this.Height / 2);


            this.lblPort.BackColor = m_clrColor;

            string display_text = $"{sc.Common.SCUtility.Trim(m_sPortName, true)}\r\n({sc.Common.SCUtility.Trim(m_sNote, true)})";
            lblPort.Text = display_text;
            //this.lblPort.Text = m_sPortName.Trim();

        }

    }
    public partial class uctlPortNew : UserControl
    {
        private Color errorColor = Common.BCUtility.ConvStr2Color("FFFF88C2");

        private sc.AUNIT UNIT;
        public uctlPortNew(sc.AUNIT unit)
        {
            InitializeComponent();
            UNIT = unit;
        }
        public void refreshColor()
        {
            if (UNIT == null) return;
            Color judge_color = Color.Empty;
            if (UNIT.IsAbnormalHappend ||
                UNIT.coupler1HPSafety == sc.App.SCAppConstants.CouplerHPSafety.NonSafety)
            {
                judge_color = errorColor;
            }
            else
            {
                judge_color = m_clrColor;
            }

            if (this.lblPort.BackColor != judge_color)
            {
                this.lblPort.BackColor = judge_color;
            }
        }


    }

}
