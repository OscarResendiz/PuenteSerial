using System.IO.Ports;
using System.Text;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

namespace PuenteSerial
{
    public partial class Form1 : Form
    {
        private static readonly Color PortAColor = Color.DarkBlue;
        private static readonly Color PortBColor = Color.DarkGreen;
        private readonly object _relayLock = new();
        private readonly object _logLock = new();
        private readonly ConcurrentQueue<LogEntry> _pendingLogs = new();
        private const int MaxLogCharacters = 200_000;
        private const int MaxPendingLogs = 2_000;
        private int _pendingLogCount;
        private System.Windows.Forms.Timer? _logTimer;
        private SerialPort? _portA;
        private SerialPort? _portB;
        private SerialDataReceivedEventHandler? _portAHandler;
        private SerialDataReceivedEventHandler? _portBHandler;
        private bool _isRunning;
        private bool _isClosing;
        private volatile bool _showSpecialCharacters;
        private CancellationTokenSource? _bridgeCancellation;
        private Task? _relayTaskA;
        private Task? _relayTaskB;
        private SemaphoreSlim? _signalA;
        private SemaphoreSlim? _signalB;

        public Form1()
        {
            InitializeComponent();
            RefreshPortLists();
            _logTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _logTimer.Tick += (_, _) => FlushPendingLogs();
            _logTimer.Start();
            SetSendEnabled(false);
        }

        private void btnRefreshPorts_Click(object? sender, EventArgs e)
        {
            RefreshPortLists();
        }

        private void btnStartStop_Click(object? sender, EventArgs e)
        {
            if (_isRunning)
            {
                StopBridge();
                return;
            }

            StartBridge();
        }

        private async void btnSendA_Click(object? sender, EventArgs e)
        {
            await SendManualTextAsync(_portA, txtSendA.Text, chkAppendLfA.Checked, "Puerto A", PortAColor);
        }

        private async void btnSendB_Click(object? sender, EventArgs e)
        {
            await SendManualTextAsync(_portB, txtSendB.Text, chkAppendLfB.Checked, "Puerto B", PortBColor);
        }

        private void btnSaveLog_Click(object? sender, EventArgs e)
        {
            FlushPendingLogs();
            using var dialog = new SaveFileDialog
            {
                Title = "Guardar registro de comunicación",
                Filter = "Archivo de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*",
                DefaultExt = "txt",
                AddExtension = true,
                FileName = $"comunicacion_serial_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                File.WriteAllText(dialog.FileName, rtbLog.Text, new UTF8Encoding(true));
                AppendLog($"Registro guardado en: {dialog.FileName}", Color.DimGray);
            }
            catch (IOException ex)
            {
                ShowError($"No se pudo guardar el registro: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                ShowError($"No se pudo guardar el registro: {ex.Message}");
            }
        }

        private void btnClearLog_Click(object? sender, EventArgs e)
        {
            rtbLog.Clear();
        }

        private void chkShowSpecialCharacters_CheckedChanged(object? sender, EventArgs e)
        {
            _showSpecialCharacters = chkShowSpecialCharacters.Checked;
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            _isClosing = true;
            _logTimer?.Stop();
            StopBridge(writeLog: false);
        }

        private void RefreshPortLists()
        {
            try
            {
                var ports = SerialPort.GetPortNames();
                Array.Sort(ports, StringComparer.OrdinalIgnoreCase);
                RefreshPortList(cboPortA, ports);
                RefreshPortList(cboPortB, ports);
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                ShowError($"No se pudieron consultar los puertos seriales: {ex.Message}");
            }
        }

        private static void RefreshPortList(ComboBox combo, string[] ports)
        {
            var selectedPort = combo.Text;
            combo.Items.Clear();
            combo.Items.AddRange(ports);
            combo.Text = selectedPort;
            if (string.IsNullOrWhiteSpace(combo.Text) && ports.Length > 0)
            {
                combo.SelectedIndex = 0;
            }
        }

        private void StartBridge()
        {
            var portNameA = cboPortA.Text.Trim();
            var portNameB = cboPortB.Text.Trim();
            if (string.IsNullOrWhiteSpace(portNameA) || string.IsNullOrWhiteSpace(portNameB))
            {
                ShowError("Selecciona o escribe el nombre de los dos puertos seriales.");
                return;
            }
            if (string.Equals(portNameA, portNameB, StringComparison.OrdinalIgnoreCase))
            {
                ShowError("Selecciona dos puertos seriales diferentes.");
                return;
            }

            SerialPort portA;
            SerialPort portB;
            try
            {
                portA = CreatePort(portNameA, cboBaudA, cboParityA, cboDataBitsA, cboStopBitsA);
                portB = CreatePort(portNameB, cboBaudB, cboParityB, cboDataBitsB, cboStopBitsB);
            }
            catch (ArgumentException ex)
            {
                ShowError($"La configuración de los puertos no es válida: {ex.Message}");
                return;
            }

            try
            {
                portA.Open();
                portB.Open();
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException or ArgumentException or System.ComponentModel.Win32Exception)
            {
                ClosePort(portA);
                ClosePort(portB);
                ShowError($"No se pudieron abrir ambos puertos: {ex.Message}");
                return;
            }

            lock (_relayLock)
            {
                _portA = portA;
                _portB = portB;
                var cancellation = new CancellationTokenSource();
                var signalA = new SemaphoreSlim(0, 1);
                var signalB = new SemaphoreSlim(0, 1);
                var handlerA = new SerialDataReceivedEventHandler((_, _) => SignalData(signalA));
                var handlerB = new SerialDataReceivedEventHandler((_, _) => SignalData(signalB));
                _portAHandler = handlerA;
                _portBHandler = handlerB;
                _bridgeCancellation = cancellation;
                _signalA = signalA;
                _signalB = signalB;
                _isRunning = true;
                portA.DataReceived += handlerA;
                portB.DataReceived += handlerB;
                portA.ReadTimeout = 200;
                portB.ReadTimeout = 200;
                portA.WriteTimeout = 200;
                portB.WriteTimeout = 200;
                _relayTaskA = Task.Run(() => RelayLoop(portA, portB, signalA, cancellation.Token, "Puerto A", "Puerto B", PortAColor));
                _relayTaskB = Task.Run(() => RelayLoop(portB, portA, signalB, cancellation.Token, "Puerto B", "Puerto A", PortBColor));
            }

            btnStartStop.Text = "Detener puente";
            SetSettingsEnabled(false);
            lblStatus.Text = $"Conectado: {portNameA} <-> {portNameB}";
            lblStatus.ForeColor = Color.DarkGreen;
            AppendLog(
                $"Puente iniciado: {portNameA} ({cboBaudA.SelectedItem} baudios) <-> {portNameB} ({cboBaudB.SelectedItem} baudios).",
                Color.DimGray);
            SetSendEnabled(true);

            SignalData(_signalA!);
            SignalData(_signalB!);
        }

        private static SerialPort CreatePort(
            string portName,
            ComboBox baud,
            ComboBox parity,
            ComboBox dataBits,
            ComboBox stopBits)
        {
            return new SerialPort(
                portName,
                int.Parse(baud.SelectedItem?.ToString() ?? "9600"),
                Enum.Parse<Parity>(parity.SelectedItem?.ToString() ?? nameof(Parity.None)),
                int.Parse(dataBits.SelectedItem?.ToString() ?? "8"),
                Enum.Parse<StopBits>(stopBits.SelectedItem?.ToString() ?? nameof(StopBits.One)))
            {
                Encoding = Encoding.ASCII,
                Handshake = Handshake.None
            };
        }

        private void StopBridge(bool writeLog = true)
        {
            SerialPort? portA;
            SerialPort? portB;
            lock (_relayLock)
            {
                if (!_isRunning)
                {
                    return;
                }

                _isRunning = false;
                _bridgeCancellation?.Cancel();
                _bridgeCancellation = null;
                _signalA = null;
                _signalB = null;
                portA = _portA;
                portB = _portB;
                if (portA is not null && _portAHandler is not null)
                {
                    portA.DataReceived -= _portAHandler;
                }
                if (portB is not null && _portBHandler is not null)
                {
                    portB.DataReceived -= _portBHandler;
                }
                _portA = null;
                _portB = null;
                _portAHandler = null;
                _portBHandler = null;
                _relayTaskA = null;
                _relayTaskB = null;
            }

            if (portA is not null)
            {
                ClosePort(portA);
            }
            if (portB is not null)
            {
                ClosePort(portB);
            }

            btnStartStop.Text = "Iniciar puente";
            SetSettingsEnabled(true);
            SetSendEnabled(false);
            lblStatus.Text = "Desconectado";
            lblStatus.ForeColor = Color.DimGray;
            if (writeLog)
            {
                AppendLog("Puente detenido.", Color.DimGray);
            }
        }

        private static void ClosePort(SerialPort port)
        {
            if (port.IsOpen)
            {
                port.Close();
            }
            port.Dispose();
        }

        private static void SignalData(SemaphoreSlim signal)
        {
            try
            {
                signal.Release();
            }
            catch (SemaphoreFullException)
            {
                // Ya hay una señal pendiente; el trabajador leerá todos los bytes disponibles.
            }
        }

        private void RelayLoop(SerialPort source, SerialPort destination, SemaphoreSlim signal,
            CancellationToken cancellation, string sourceName, string destinationName, Color color)
        {
            try
            {
                while (true)
                {
                    signal.WaitAsync(cancellation).GetAwaiter().GetResult();
                    while (!cancellation.IsCancellationRequested && source.IsOpen && source.BytesToRead > 0)
                    {
                        var buffer = new byte[Math.Min(source.BytesToRead, 4096)];
                        var bytesRead = source.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0 || !destination.IsOpen)
                        {
                            break;
                        }

                        lock (_relayLock)
                        {
                            if (destination.IsOpen)
                            {
                                destination.Write(buffer, 0, bytesRead);
                            }
                        }
                        var text = FormatSerialText(buffer, bytesRead, _showSpecialCharacters);
                        AppendLog($"{sourceName} -> {destinationName}: {text}", color, true);
                    }

                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                if (!cancellation.IsCancellationRequested)
                {
                    AppendLog($"Error al transferir de {sourceName} a {destinationName}: {ex.Message}", Color.DarkRed);
                }
            }
        }

        private static string FormatSerialText(byte[] buffer, int count, bool showSpecialCharacters)
        {
            if (!showSpecialCharacters)
            {
                return Encoding.ASCII.GetString(buffer, 0, count);
            }

            var result = new StringBuilder(count);
            for (var index = 0; index < count; index++)
            {
                var value = buffer[index];
                switch (value)
                {
                    case (byte)'\n':
                        result.Append("(\\n)");
                        break;
                    case (byte)'\r':
                        result.Append("(\\r)");
                        break;
                    case (byte)'\t':
                        result.Append("(\\t)");
                        break;
                    case < 32 or 127:
                        result.Append($"(\\x{value:X2})");
                        break;
                    default:
                        result.Append((char)value);
                        break;
                }
            }

            return result.ToString();
        }

        private void SetSendEnabled(bool enabled)
        {
            txtSendA.Enabled = enabled;
            btnSendA.Enabled = enabled;
            chkAppendLfA.Enabled = enabled;
            txtSendB.Enabled = enabled;
            btnSendB.Enabled = enabled;
            chkAppendLfB.Enabled = enabled;
        }

        private async Task SendManualTextAsync(SerialPort? port, string text, bool appendLf, string portName, Color color)
        {
            if (port is null || !port.IsOpen || string.IsNullOrEmpty(text))
            {
                return;
            }

            var data = appendLf ? text + "\n" : text;
            try
            {
                await Task.Run(() =>
                {
                    lock (_relayLock)
                    {
                        if (port.IsOpen)
                        {
                            port.Write(data);
                        }
                    }
                });
                AppendLog($"Manual -> {portName}: {data}", color, addLineBreak: true);
            }
            catch (Exception ex) when (ex is IOException or InvalidOperationException or UnauthorizedAccessException)
            {
                AppendLog($"Error al enviar manualmente a {portName}: {ex.Message}", Color.DarkRed);
            }
        }

        private void SetSettingsEnabled(bool enabled)
        {
            foreach (var control in new Control[]
                     {
                         cboPortA, cboBaudA, cboParityA, cboDataBitsA, cboStopBitsA,
                         cboPortB, cboBaudB, cboParityB, cboDataBitsB, cboStopBitsB,
                         btnRefreshPorts
                     })
            {
                control.Enabled = enabled;
            }
        }

        private void AppendLog(string message, Color color, bool addLineBreak = false)
        {
            if (_isClosing)
            {
                return;
            }

            while (Interlocked.Increment(ref _pendingLogCount) > MaxPendingLogs)
            {
                if (_pendingLogs.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref _pendingLogCount);
                }
            }

            _pendingLogs.Enqueue(new LogEntry($"[{DateTime.Now:HH:mm:ss.fff}] {message}", color, addLineBreak));
        }

        private void FlushPendingLogs()
        {
            if (IsDisposed || Disposing)
            {
                return;
            }

            lock (_logLock)
            {
                SendMessage(rtbLog.Handle, WM_SETREDRAW, IntPtr.Zero, IntPtr.Zero);
                try
                {
                    var displayed = 0;
                    while (displayed++ < 100 && _pendingLogs.TryDequeue(out var entry))
                    {
                        Interlocked.Decrement(ref _pendingLogCount);
                        rtbLog.SelectionStart = rtbLog.TextLength;
                        rtbLog.SelectionLength = 0;
                        rtbLog.SelectionColor = entry.Color;
                        rtbLog.AppendText(entry.Text);
                        if (entry.AddLineBreak && !entry.Text.EndsWith('\n') && !entry.Text.EndsWith('\r'))
                        {
                            rtbLog.AppendText(Environment.NewLine);
                        }
                    }

                    if (rtbLog.TextLength > MaxLogCharacters)
                    {
                        rtbLog.Select(0, rtbLog.TextLength - MaxLogCharacters);
                        rtbLog.SelectedText = string.Empty;
                    }

                    rtbLog.SelectionStart = rtbLog.TextLength;
                    rtbLog.SelectionLength = 0;
                    rtbLog.SelectionColor = rtbLog.ForeColor;
                }
                finally
                {
                    SendMessage(rtbLog.Handle, WM_SETREDRAW, new IntPtr(1), IntPtr.Zero);
                    rtbLog.Invalidate();
                }
                rtbLog.ScrollToCaret();
            }
        }

        private const int WM_SETREDRAW = 0x000B;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int message, IntPtr wParam, IntPtr lParam);

        private readonly record struct LogEntry(string Text, Color Color, bool AddLineBreak);

        private void ShowError(string message)
        {
            AppendLog(message, Color.DarkRed);
            MessageBox.Show(this, message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
