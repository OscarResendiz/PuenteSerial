using System.IO.Ports;

namespace PuenteSerial
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null!;
        private ComboBox cboPortA = null!;
        private ComboBox cboBaudA = null!;
        private ComboBox cboParityA = null!;
        private ComboBox cboDataBitsA = null!;
        private ComboBox cboStopBitsA = null!;
        private TextBox txtSendA = null!;
        private Button btnSendA = null!;
        private CheckBox chkAppendLfA = null!;
        private ComboBox cboPortB = null!;
        private ComboBox cboBaudB = null!;
        private ComboBox cboParityB = null!;
        private ComboBox cboDataBitsB = null!;
        private ComboBox cboStopBitsB = null!;
        private TextBox txtSendB = null!;
        private Button btnSendB = null!;
        private CheckBox chkAppendLfB = null!;
        private Button btnStartStop = null!;
        private Button btnRefreshPorts = null!;
        private Button btnSaveLog = null!;
        private Button btnClearLog = null!;
        private CheckBox chkShowSpecialCharacters = null!;
        private Label lblStatus = null!;
        private RichTextBox rtbLog = null!;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            cboPortA = new ComboBox();
            cboBaudA = new ComboBox();
            cboParityA = new ComboBox();
            cboDataBitsA = new ComboBox();
            cboStopBitsA = new ComboBox();
            txtSendA = new TextBox();
            btnSendA = new Button();
            chkAppendLfA = new CheckBox();
            cboPortB = new ComboBox();
            cboBaudB = new ComboBox();
            cboParityB = new ComboBox();
            cboDataBitsB = new ComboBox();
            cboStopBitsB = new ComboBox();
            txtSendB = new TextBox();
            btnSendB = new Button();
            chkAppendLfB = new CheckBox();
            btnStartStop = new Button();
            btnRefreshPorts = new Button();
            btnSaveLog = new Button();
            btnClearLog = new Button();
            chkShowSpecialCharacters = new CheckBox();
            lblStatus = new Label();
            rtbLog = new RichTextBox();
            var mainLayout = new TableLayoutPanel();
            var settingsLayout = new TableLayoutPanel();
            var portAGroup = new GroupBox();
            var portBGroup = new GroupBox();
            var portASettings = new TableLayoutPanel();
            var portBSettings = new TableLayoutPanel();
            var actions = new FlowLayoutPanel();
            var logGroup = new GroupBox();

            SuspendLayout();

            mainLayout.ColumnCount = 2;
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mainLayout.RowCount = 3;
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 250F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            mainLayout.Dock = DockStyle.Fill;
            mainLayout.Padding = new Padding(8);
            mainLayout.Controls.Add(settingsLayout, 0, 0);
            mainLayout.SetColumnSpan(settingsLayout, 2);
            mainLayout.Controls.Add(actions, 0, 1);
            mainLayout.SetColumnSpan(actions, 2);
            mainLayout.Controls.Add(logGroup, 0, 2);
            mainLayout.SetColumnSpan(logGroup, 2);

            settingsLayout.ColumnCount = 2;
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            settingsLayout.RowCount = 1;
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            settingsLayout.Dock = DockStyle.Fill;
            settingsLayout.Controls.Add(portAGroup, 0, 0);
            settingsLayout.Controls.Add(portBGroup, 1, 0);

            ConfigurePortGroup(portAGroup, portASettings, "Puerto A", cboPortA, cboBaudA, cboParityA, cboDataBitsA, cboStopBitsA, txtSendA, btnSendA, chkAppendLfA);
            ConfigurePortGroup(portBGroup, portBSettings, "Puerto B", cboPortB, cboBaudB, cboParityB, cboDataBitsB, cboStopBitsB, txtSendB, btnSendB, chkAppendLfB);

            btnSendA.Text = "Enviar";
            btnSendA.AutoSize = true;
            btnSendA.Click += btnSendA_Click;
            btnSendB.Text = "Enviar";
            btnSendB.AutoSize = true;
            btnSendB.Click += btnSendB_Click;

            actions.Dock = DockStyle.Fill;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.WrapContents = false;
            actions.Padding = new Padding(0, 4, 0, 0);
            actions.Controls.Add(btnStartStop);
            actions.Controls.Add(btnRefreshPorts);
            actions.Controls.Add(btnSaveLog);
            actions.Controls.Add(btnClearLog);
            actions.Controls.Add(chkShowSpecialCharacters);
            actions.Controls.Add(lblStatus);

            btnStartStop.AutoSize = true;
            btnStartStop.Text = "Iniciar puente";
            btnStartStop.UseVisualStyleBackColor = true;
            btnStartStop.Click += btnStartStop_Click;

            btnRefreshPorts.AutoSize = true;
            btnRefreshPorts.Text = "Actualizar puertos";
            btnRefreshPorts.UseVisualStyleBackColor = true;
            btnRefreshPorts.Click += btnRefreshPorts_Click;

            btnSaveLog.AutoSize = true;
            btnSaveLog.Text = "Guardar registro";
            btnSaveLog.UseVisualStyleBackColor = true;
            btnSaveLog.Click += btnSaveLog_Click;

            btnClearLog.AutoSize = true;
            btnClearLog.Text = "Limpiar";
            btnClearLog.UseVisualStyleBackColor = true;
            btnClearLog.Click += btnClearLog_Click;

            chkShowSpecialCharacters.AutoSize = true;
            chkShowSpecialCharacters.Text = "Mostrar caracteres especiales";
            chkShowSpecialCharacters.UseVisualStyleBackColor = true;
            chkShowSpecialCharacters.CheckedChanged += chkShowSpecialCharacters_CheckedChanged;

            lblStatus.AutoSize = true;
            lblStatus.Text = "Desconectado";
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            lblStatus.Padding = new Padding(8, 7, 0, 0);
            lblStatus.ForeColor = Color.DimGray;

            logGroup.Dock = DockStyle.Fill;
            logGroup.Text = "Comunicación (azul: Puerto A; verde: Puerto B)";
            logGroup.Padding = new Padding(8);
            logGroup.Controls.Add(rtbLog);

            rtbLog.Dock = DockStyle.Fill;
            rtbLog.ReadOnly = true;
            rtbLog.BackColor = SystemColors.Window;
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.DetectUrls = false;
            rtbLog.HideSelection = false;

            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(980, 740);
            MinimumSize = new Size(820, 620);
            Controls.Add(mainLayout);
            Text = "Puente de puertos seriales";
            FormClosing += Form1_FormClosing;
            ResumeLayout(false);
        }

        private static void ConfigurePortGroup(
            GroupBox group,
            TableLayoutPanel settings,
            string title,
            ComboBox port,
            ComboBox baud,
            ComboBox parity,
            ComboBox dataBits,
            ComboBox stopBits,
            TextBox sendText,
            Button sendButton,
            CheckBox appendLf)
        {
            group.Dock = DockStyle.Fill;
            group.Margin = new Padding(4);
            group.Padding = new Padding(8);
            group.Text = title;
            group.Controls.Add(settings);

            settings.ColumnCount = 2;
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 115F));
            settings.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            settings.RowCount = 7;
            for (var row = 0; row < 5; row++)
            {
                settings.RowStyles.Add(new RowStyle(SizeType.Percent, 20F));
            }
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            settings.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            settings.Dock = DockStyle.Fill;
            settings.Padding = new Padding(4, 8, 4, 0);

            AddSetting(settings, "Puerto COM:", port, 0);
            AddSetting(settings, "Velocidad:", baud, 1);
            AddSetting(settings, "Paridad:", parity, 2);
            AddSetting(settings, "Bits de datos:", dataBits, 3);
            AddSetting(settings, "Bits de parada:", stopBits, 4);

            sendText.Dock = DockStyle.Fill;
            sendText.Margin = new Padding(3, 2, 3, 2);
            sendText.PlaceholderText = "Texto para enviar";
            settings.Controls.Add(sendText, 0, 5);
            settings.SetColumnSpan(sendText, 2);

            var sendOptions = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                WrapContents = false,
                Margin = new Padding(0)
            };
            appendLf.Text = "Añadir retorno de carro (\\n)";
            appendLf.AutoSize = true;
            appendLf.Margin = new Padding(3, 5, 12, 0);
            sendOptions.Controls.Add(appendLf);
            sendOptions.Controls.Add(sendButton);
            settings.Controls.Add(sendOptions, 0, 6);
            settings.SetColumnSpan(sendOptions, 2);

            port.DropDownStyle = ComboBoxStyle.DropDown;
            baud.DropDownStyle = ComboBoxStyle.DropDownList;
            baud.Items.AddRange(new object[] { "1200", "2400", "4800", "9600", "14400", "19200", "38400", "57600", "115200", "230400" });
            baud.SelectedItem = "9600";
            parity.DropDownStyle = ComboBoxStyle.DropDownList;
            parity.Items.AddRange(Enum.GetNames<Parity>());
            parity.SelectedItem = nameof(Parity.None);
            dataBits.DropDownStyle = ComboBoxStyle.DropDownList;
            dataBits.Items.AddRange(new object[] { "5", "6", "7", "8" });
            dataBits.SelectedItem = "8";
            stopBits.DropDownStyle = ComboBoxStyle.DropDownList;
            stopBits.Items.AddRange(new object[] { nameof(StopBits.One), nameof(StopBits.OnePointFive), nameof(StopBits.Two) });
            stopBits.SelectedItem = nameof(StopBits.One);
            foreach (var combo in new[] { port, baud, parity, dataBits, stopBits })
            {
                combo.Dock = DockStyle.Fill;
                combo.Margin = new Padding(3, 2, 3, 2);
            }
        }

        private static void AddSetting(TableLayoutPanel layout, string labelText, ComboBox combo, int row)
        {
            var label = new Label
            {
                Text = labelText,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false
            };
            layout.Controls.Add(label, 0, row);
            layout.Controls.Add(combo, 1, row);
        }
    }
}
