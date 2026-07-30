using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

[assembly: AssemblyTitle("灵动窗口控制器")]
[assembly: AssemblyDescription("Windows 窗口停靠与浏览器画中画助手")]
[assembly: AssemblyCompany("ylv01")]
[assembly: AssemblyProduct("灵动窗口控制器")]
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace WindowDockTool
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Native.EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal static class Native
    {
        internal const uint GA_ROOT = 2;
        internal const int GWL_EXSTYLE = -20;
        internal const long WS_EX_LAYERED = 0x00080000L;
        internal const long WS_EX_TOPMOST = 0x00000008L;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOZORDER = 0x0004;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal const int SW_MINIMIZE = 6;
        internal const int SW_RESTORE = 9;
        internal const uint LWA_ALPHA = 0x00000002;

        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        internal static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        internal delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            internal int X;
            internal int Y;

            internal POINT(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;
        }

        [DllImport("user32.dll")]
        internal static extern IntPtr WindowFromPoint(POINT point);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetAncestor(IntPtr hwnd, uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowTextLength(IntPtr hwnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr hwnd, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out RECT rect);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(
            IntPtr hwnd,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hwnd, int command);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        private static extern int SetWindowLong32(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hwnd, int index, IntPtr value);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetLayeredWindowAttributes(
            IntPtr hwnd,
            uint colorKey,
            byte alpha,
            uint flags);

        [DllImport("user32.dll", EntryPoint = "SetProcessDpiAwarenessContext")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr context);

        [DllImport("user32.dll", EntryPoint = "SetProcessDPIAware")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessDPIAware();

        internal static long GetExtendedStyle(IntPtr hwnd)
        {
            if (IntPtr.Size == 8)
            {
                return GetWindowLongPtr64(hwnd, GWL_EXSTYLE).ToInt64();
            }
            return GetWindowLong32(hwnd, GWL_EXSTYLE);
        }

        internal static void SetExtendedStyle(IntPtr hwnd, long style)
        {
            if (IntPtr.Size == 8)
            {
                SetWindowLongPtr64(hwnd, GWL_EXSTYLE, new IntPtr(style));
            }
            else
            {
                SetWindowLong32(hwnd, GWL_EXSTYLE, unchecked((int)style));
            }
        }

        internal static string Title(IntPtr hwnd)
        {
            int length = GetWindowTextLength(hwnd);
            if (length <= 0)
            {
                return String.Empty;
            }

            StringBuilder builder = new StringBuilder(length + 1);
            GetWindowText(hwnd, builder, builder.Capacity);
            return builder.ToString();
        }

        internal static uint ProcessId(IntPtr hwnd)
        {
            uint processId;
            GetWindowThreadProcessId(hwnd, out processId);
            return processId;
        }

        internal static string ProcessName(uint processId)
        {
            try
            {
                return Process.GetProcessById((int)processId).ProcessName;
            }
            catch
            {
                return "未知进程";
            }
        }

        internal static List<WindowInfo> VisibleWindows(uint excludedProcessId)
        {
            List<WindowInfo> windows = new List<WindowInfo>();
            EnumWindows(delegate(IntPtr hwnd, IntPtr unused)
            {
                if (!IsWindowVisible(hwnd))
                {
                    return true;
                }

                string title = Title(hwnd).Trim();
                uint pid = ProcessId(hwnd);
                if (title.Length == 0 || pid == 0 || pid == excludedProcessId)
                {
                    return true;
                }

                windows.Add(new WindowInfo(hwnd, title, pid, ProcessName(pid)));
                return true;
            }, IntPtr.Zero);

            windows.Sort(delegate(WindowInfo left, WindowInfo right)
            {
                return String.Compare(
                    left.Title,
                    right.Title,
                    StringComparison.CurrentCultureIgnoreCase);
            });
            return windows;
        }

        internal static void EnableDpiAwareness()
        {
            try
            {
                if (SetProcessDpiAwarenessContext(new IntPtr(-4)))
                {
                    return;
                }
            }
            catch
            {
            }

            try
            {
                SetProcessDPIAware();
            }
            catch
            {
            }
        }
    }

    internal sealed class WindowInfo
    {
        internal WindowInfo(IntPtr handle, string title, uint processId, string processName)
        {
            Handle = handle;
            Title = title;
            ProcessId = processId;
            ProcessName = processName;
        }

        internal IntPtr Handle { get; private set; }
        internal string Title { get; private set; }
        internal uint ProcessId { get; private set; }
        internal string ProcessName { get; private set; }

        public override string ToString()
        {
            string text = Title.Length > 54 ? Title.Substring(0, 51) + "..." : Title;
            return text + " — " + ProcessName + " (" + ProcessId + ")";
        }
    }

    internal sealed class MainForm : Form
    {
        private readonly uint _ownProcessId;
        private IntPtr _target = IntPtr.Zero;
        private IntPtr _hover = IntPtr.Zero;
        private Rectangle _highlight = Rectangle.Empty;
        private bool _picking;
        private bool _updating;

        private Label _targetText;
        private Label _status;
        private Button _picker;
        private ComboBox _windows;
        private NumericUpDown _area;
        private ComboBox _ratio;
        private ComboBox _anchor;
        private NumericUpDown _margin;
        private NumericUpDown _x;
        private NumericUpDown _y;
        private NumericUpDown _width;
        private NumericUpDown _height;
        private CheckBox _topMost;
        private TrackBar _opacity;
        private Label _opacityText;

        internal MainForm()
        {
            _ownProcessId = (uint)Process.GetCurrentProcess().Id;
            Text = "灵动窗口控制器 v1.2";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(760, 710);
            MinimumSize = new Size(720, 660);
            Font = new Font("Microsoft YaHei UI", 9F);
            BackColor = Color.FromArgb(246, 248, 251);
            ForeColor = Color.FromArgb(36, 46, 58);
            TopMost = true;
            AutoScroll = true;
            BuildUi();
            RefreshWindows();
            FormClosing += delegate { ClearHighlight(); };
        }

        private void BuildUi()
        {
            Label title = new Label();
            title.Text = "灵动窗口控制器";
            title.Font = new Font(Font.FontFamily, 18F, FontStyle.Bold);
            title.ForeColor = Color.White;
            title.BackColor = Color.FromArgb(25, 43, 64);
            title.Location = new Point(0, 0);
            title.Size = new Size(744, 70);
            title.Padding = new Padding(20, 16, 0, 0);
            title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            Controls.Add(title);

            GroupBox selection = Group("1. 选择窗口", 20, 86, 704, 145);
            _picker = PrimaryButton("⊕ 拖到窗口", 18, 29, 140, 42);
            _picker.Cursor = Cursors.SizeAll;
            _picker.MouseDown += PickerDown;
            _picker.MouseMove += PickerMove;
            _picker.MouseUp += PickerUp;
            selection.Controls.Add(_picker);

            _targetText = new Label();
            _targetText.Text = "把准星拖到浏览器、画中画或其他程序窗口上。";
            _targetText.Location = new Point(174, 27);
            _targetText.Size = new Size(500, 49);
            selection.Controls.Add(_targetText);

            _windows = new ComboBox();
            _windows.DropDownStyle = ComboBoxStyle.DropDownList;
            _windows.Location = new Point(18, 88);
            _windows.Size = new Size(474, 28);
            selection.Controls.Add(_windows);

            Button refresh = SecondaryButton("刷新列表", 502, 86, 84, 30);
            refresh.Click += delegate { RefreshWindows(); };
            selection.Controls.Add(refresh);

            Button choose = SecondaryButton("选中", 596, 86, 78, 30);
            choose.Click += delegate
            {
                WindowInfo info = _windows.SelectedItem as WindowInfo;
                if (info != null)
                {
                    SelectTarget(info.Handle);
                }
            };
            selection.Controls.Add(choose);

            GroupBox preset = Group("2. 面积比例与画中画", 20, 243, 704, 145);
            preset.Controls.Add(TextLabel("面积 %", 18, 34));
            _area = Number(0.5M, 100M, 10M, 1, 74, 30, 75);
            preset.Controls.Add(_area);

            preset.Controls.Add(TextLabel("宽高比", 180, 34));
            _ratio = DropDown(new object[] { "16:9", "4:3", "21:9", "1:1", "当前比例" }, 0, 239, 30, 115);
            preset.Controls.Add(_ratio);

            preset.Controls.Add(TextLabel("停靠", 382, 34));
            _anchor = DropDown(
                new object[] { "左上", "上中", "右上", "左中", "居中", "右中", "左下", "下中", "右下" },
                8,
                426,
                30,
                88);
            preset.Controls.Add(_anchor);

            preset.Controls.Add(TextLabel("边距", 18, 82));
            _margin = Number(0, 500, 12, 0, 61, 78, 70);
            preset.Controls.Add(_margin);

            Button pip = PrimaryButton("定位画中画 → 右下角", 265, 73, 190, 38);
            pip.BackColor = Color.FromArgb(24, 145, 103);
            pip.Click += DockPictureInPicture;
            preset.Controls.Add(pip);

            Button apply = PrimaryButton("应用比例与停靠", 468, 73, 206, 38);
            apply.Click += delegate { ApplyRectangle(CalculatePreset()); };
            preset.Controls.Add(apply);

            GroupBox manual = Group("3. 自定义区域", 20, 400, 704, 135);
            _x = LabeledNumber(manual, "X", 18, -50000, 50000, 0);
            _y = LabeledNumber(manual, "Y", 162, -50000, 50000, 0);
            _width = LabeledNumber(manual, "宽", 306, 50, 30000, 608);
            _height = LabeledNumber(manual, "高", 468, 50, 30000, 342);

            Button read = SecondaryButton("读取当前", 18, 79, 96, 31);
            read.Click += delegate { ReadRectangle(); };
            manual.Controls.Add(read);

            Button applyManual = SecondaryButton("应用数值", 124, 79, 96, 31);
            applyManual.Click += delegate
            {
                ApplyRectangle(new Rectangle(
                    Decimal.ToInt32(_x.Value),
                    Decimal.ToInt32(_y.Value),
                    Decimal.ToInt32(_width.Value),
                    Decimal.ToInt32(_height.Value)));
            };
            manual.Controls.Add(applyManual);

            Button selectArea = PrimaryButton("在屏幕上拖出窗口区域", 455, 75, 219, 37);
            selectArea.Click += SelectArea;
            manual.Controls.Add(selectArea);

            GroupBox effects = Group("4. 窗口效果", 20, 547, 704, 105);
            _topMost = new CheckBox();
            _topMost.Text = "目标窗口置顶";
            _topMost.AutoSize = true;
            _topMost.Location = new Point(18, 35);
            _topMost.CheckedChanged += ToggleTopMost;
            effects.Controls.Add(_topMost);

            effects.Controls.Add(TextLabel("透明度", 18, 73));
            _opacity = new TrackBar();
            _opacity.Minimum = 20;
            _opacity.Maximum = 100;
            _opacity.Value = 100;
            _opacity.TickFrequency = 10;
            _opacity.Location = new Point(78, 57);
            _opacity.Size = new Size(360, 42);
            _opacity.Scroll += ChangeOpacity;
            effects.Controls.Add(_opacity);

            _opacityText = TextLabel("100%", 444, 73);
            effects.Controls.Add(_opacityText);

            Button minimize = SecondaryButton("最小化目标", 484, 28, 92, 30);
            minimize.Click += delegate
            {
                if (EnsureTarget())
                {
                    Native.ShowWindow(_target, Native.SW_MINIMIZE);
                }
            };
            effects.Controls.Add(minimize);

            Button restore = SecondaryButton("恢复目标", 586, 28, 88, 30);
            restore.Click += delegate
            {
                if (EnsureTarget())
                {
                    Native.ShowWindow(_target, Native.SW_RESTORE);
                }
            };
            effects.Controls.Add(restore);

            _status = new Label();
            _status.Text = "状态：请先选择窗口。";
            _status.Location = new Point(25, 662);
            _status.Size = new Size(680, 24);
            _status.ForeColor = Color.FromArgb(77, 89, 103);
            Controls.Add(_status);
        }

        private GroupBox Group(string text, int x, int y, int width, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            group.BackColor = Color.White;
            group.Font = new Font(Font, FontStyle.Bold);
            Controls.Add(group);
            return group;
        }

        private Button PrimaryButton(string text, int x, int y, int width, int height)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new Point(x, y);
            button.Size = new Size(width, height);
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = Color.FromArgb(28, 117, 188);
            button.ForeColor = Color.White;
            button.Font = new Font(Font, FontStyle.Bold);
            return button;
        }

        private Button SecondaryButton(string text, int x, int y, int width, int height)
        {
            Button button = PrimaryButton(text, x, y, width, height);
            button.BackColor = Color.White;
            button.ForeColor = Color.FromArgb(51, 64, 79);
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = Color.FromArgb(190, 200, 211);
            return button;
        }

        private Label TextLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.AutoSize = true;
            label.Location = new Point(x, y);
            label.Font = Font;
            return label;
        }

        private NumericUpDown Number(
            decimal min,
            decimal max,
            decimal value,
            int decimals,
            int x,
            int y,
            int width)
        {
            NumericUpDown number = new NumericUpDown();
            number.Minimum = min;
            number.Maximum = max;
            number.Value = value;
            number.DecimalPlaces = decimals;
            number.Increment = decimals > 0 ? 0.5M : 1M;
            number.Location = new Point(x, y);
            number.Width = width;
            number.Font = Font;
            return number;
        }

        private NumericUpDown LabeledNumber(
            Control parent,
            string text,
            int x,
            decimal min,
            decimal max,
            decimal value)
        {
            parent.Controls.Add(TextLabel(text, x, 38));
            NumericUpDown number = Number(min, max, value, 0, x + 28, 34, 103);
            parent.Controls.Add(number);
            return number;
        }

        private ComboBox DropDown(object[] items, int selected, int x, int y, int width)
        {
            ComboBox combo = new ComboBox();
            combo.DropDownStyle = ComboBoxStyle.DropDownList;
            combo.Items.AddRange(items);
            combo.SelectedIndex = selected;
            combo.Location = new Point(x, y);
            combo.Width = width;
            combo.Font = Font;
            return combo;
        }

        private void RefreshWindows()
        {
            _windows.BeginUpdate();
            _windows.Items.Clear();
            foreach (WindowInfo window in Native.VisibleWindows(_ownProcessId))
            {
                _windows.Items.Add(window);
            }
            _windows.EndUpdate();
            if (_windows.Items.Count > 0)
            {
                _windows.SelectedIndex = 0;
            }
        }

        private void PickerDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            _picking = true;
            _picker.Capture = true;
            _picker.Text = "拖到目标后松开";
            Cursor = Cursors.Cross;
        }

        private void PickerMove(object sender, MouseEventArgs e)
        {
            if (!_picking)
            {
                return;
            }

            Point cursor = Control.MousePosition;
            IntPtr child = Native.WindowFromPoint(new Native.POINT(cursor.X, cursor.Y));
            IntPtr root = child == IntPtr.Zero
                ? IntPtr.Zero
                : Native.GetAncestor(child, Native.GA_ROOT);
            if (root != IntPtr.Zero && Native.ProcessId(root) == _ownProcessId)
            {
                root = IntPtr.Zero;
            }

            if (root != _hover)
            {
                ClearHighlight();
                _hover = root;
                DrawHighlight(root);
            }
        }

        private void PickerUp(object sender, MouseEventArgs e)
        {
            if (!_picking || e.Button != MouseButtons.Left)
            {
                return;
            }

            IntPtr selected = _hover;
            _picking = false;
            _picker.Capture = false;
            _picker.Text = "⊕ 拖到窗口";
            Cursor = Cursors.Default;
            ClearHighlight();
            _hover = IntPtr.Zero;

            if (selected != IntPtr.Zero)
            {
                SelectTarget(selected);
            }
        }

        private void DrawHighlight(IntPtr hwnd)
        {
            Native.RECT rect;
            if (hwnd != IntPtr.Zero && Native.GetWindowRect(hwnd, out rect))
            {
                _highlight = Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
                ControlPaint.DrawReversibleFrame(
                    _highlight,
                    Color.DeepSkyBlue,
                    FrameStyle.Thick);
            }
        }

        private void ClearHighlight()
        {
            if (!_highlight.IsEmpty)
            {
                ControlPaint.DrawReversibleFrame(
                    _highlight,
                    Color.DeepSkyBlue,
                    FrameStyle.Thick);
                _highlight = Rectangle.Empty;
            }
        }

        private void SelectTarget(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero || !Native.IsWindow(hwnd))
            {
                Error("目标窗口无效或已经关闭。");
                return;
            }

            _target = hwnd;
            uint pid = Native.ProcessId(hwnd);
            _targetText.Text = Native.Title(hwnd) + Environment.NewLine
                + Native.ProcessName(pid) + " · PID " + pid
                + " · HWND 0x" + hwnd.ToInt64().ToString("X");
            ReadRectangle();

            _updating = true;
            _topMost.Checked =
                (Native.GetExtendedStyle(hwnd) & Native.WS_EX_TOPMOST) != 0;
            _updating = false;
            Status("已选中目标窗口。");
        }

        private bool EnsureTarget()
        {
            if (_target == IntPtr.Zero || !Native.IsWindow(_target))
            {
                Error("请先选择一个有效窗口。");
                return false;
            }
            return true;
        }

        private void ReadRectangle()
        {
            if (!EnsureTarget())
            {
                return;
            }

            Native.RECT rect;
            if (Native.GetWindowRect(_target, out rect))
            {
                SetNumber(_x, rect.Left);
                SetNumber(_y, rect.Top);
                SetNumber(_width, Math.Max(50, rect.Right - rect.Left));
                SetNumber(_height, Math.Max(50, rect.Bottom - rect.Top));
            }
        }

        private void SetNumber(NumericUpDown control, int value)
        {
            decimal number = value;
            number = Math.Max(control.Minimum, Math.Min(control.Maximum, number));
            control.Value = number;
        }

        private Rectangle CalculatePreset()
        {
            Screen screen = Screen.FromHandle(_target);
            Rectangle work = screen.WorkingArea;
            double fraction = Decimal.ToDouble(_area.Value) / 100D;
            double ratio = SelectedRatio();
            double desiredArea = work.Width * (double)work.Height * fraction;
            int height = (int)Math.Round(Math.Sqrt(desiredArea / ratio));
            int width = (int)Math.Round(height * ratio);
            int margin = Decimal.ToInt32(_margin.Value);

            int maxWidth = Math.Max(50, work.Width - margin * 2);
            int maxHeight = Math.Max(50, work.Height - margin * 2);
            if (width > maxWidth || height > maxHeight)
            {
                double scale = Math.Min(maxWidth / (double)width, maxHeight / (double)height);
                width = Math.Max(50, (int)Math.Floor(width * scale));
                height = Math.Max(50, (int)Math.Floor(height * scale));
            }

            int column = _anchor.SelectedIndex % 3;
            int row = _anchor.SelectedIndex / 3;
            int x = column == 0
                ? work.Left + margin
                : column == 1
                    ? work.Left + (work.Width - width) / 2
                    : work.Right - margin - width;
            int y = row == 0
                ? work.Top + margin
                : row == 1
                    ? work.Top + (work.Height - height) / 2
                    : work.Bottom - margin - height;
            return new Rectangle(x, y, width, height);
        }

        private double SelectedRatio()
        {
            if (_ratio.SelectedIndex == 1) return 4D / 3D;
            if (_ratio.SelectedIndex == 2) return 21D / 9D;
            if (_ratio.SelectedIndex == 3) return 1D;
            if (_ratio.SelectedIndex == 4)
            {
                Native.RECT rect;
                if (Native.GetWindowRect(_target, out rect))
                {
                    int width = rect.Right - rect.Left;
                    int height = rect.Bottom - rect.Top;
                    if (width > 0 && height > 0)
                    {
                        return width / (double)height;
                    }
                }
            }
            return 16D / 9D;
        }

        private void ApplyRectangle(Rectangle rectangle)
        {
            if (!EnsureTarget())
            {
                return;
            }

            if (CoversScreen(_target))
            {
                MessageBox.Show(
                    this,
                    "网页全屏窗口由浏览器强制占满显示器。\r\n"
                    + "请按 Esc 退出全屏，在视频上连续右键两次并选择“画中画”，"
                    + "然后点击绿色按钮。",
                    "请使用浏览器画中画",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            ApplyRectangleNow(_target, rectangle);
        }

        private void ApplyRectangleNow(IntPtr hwnd, Rectangle rectangle)
        {
            Native.ShowWindow(hwnd, Native.SW_RESTORE);
            bool success = Native.SetWindowPos(
                hwnd,
                IntPtr.Zero,
                rectangle.X,
                rectangle.Y,
                rectangle.Width,
                rectangle.Height,
                Native.SWP_NOZORDER | Native.SWP_NOACTIVATE | Native.SWP_SHOWWINDOW);
            if (!success)
            {
                Error("设置窗口失败，Windows 错误码：" + Marshal.GetLastWin32Error());
                return;
            }

            SetNumber(_x, rectangle.X);
            SetNumber(_y, rectangle.Y);
            SetNumber(_width, rectangle.Width);
            SetNumber(_height, rectangle.Height);
            Status("已应用 " + rectangle.Width + " × " + rectangle.Height + "。");
        }

        private bool CoversScreen(IntPtr hwnd)
        {
            Native.RECT rect;
            if (!Native.GetWindowRect(hwnd, out rect))
            {
                return false;
            }

            Rectangle bounds = Screen.FromHandle(hwnd).Bounds;
            const int tolerance = 12;
            return rect.Left <= bounds.Left + tolerance
                && rect.Top <= bounds.Top + tolerance
                && rect.Right >= bounds.Right - tolerance
                && rect.Bottom >= bounds.Bottom - tolerance;
        }

        private void DockPictureInPicture(object sender, EventArgs e)
        {
            if (!EnsureTarget())
            {
                return;
            }

            WindowInfo pip = FindPictureInPicture();
            if (pip == null)
            {
                MessageBox.Show(
                    this,
                    "没有检测到画中画窗口。\r\n\r\n"
                    + "1. 按 Esc 退出网页全屏。\r\n"
                    + "2. 在视频画面上连续右键两次。\r\n"
                    + "3. 在浏览器菜单中选择“画中画”。\r\n"
                    + "4. 再点击本按钮。",
                    "需要先开启画中画",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            SelectTarget(pip.Handle);
            Rectangle rectangle = CalculatePreset();
            ApplyRectangleNow(_target, rectangle);
            Native.SetWindowPos(
                _target,
                Native.HWND_TOPMOST,
                0,
                0,
                0,
                0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
            _updating = true;
            _topMost.Checked = true;
            _updating = false;
            Status("画中画已置顶并停靠到指定位置。");
        }

        private WindowInfo FindPictureInPicture()
        {
            string selectedProcess = Native.ProcessName(Native.ProcessId(_target));
            WindowInfo best = null;
            int bestScore = Int32.MinValue;

            foreach (WindowInfo window in Native.VisibleWindows(_ownProcessId))
            {
                Native.RECT rect;
                if (!Native.GetWindowRect(window.Handle, out rect))
                {
                    continue;
                }

                int width = rect.Right - rect.Left;
                int height = rect.Bottom - rect.Top;
                if (width < 160 || height < 90)
                {
                    continue;
                }

                Screen screen = Screen.FromHandle(window.Handle);
                bool compact = width <= screen.Bounds.Width * 0.8
                    && height <= screen.Bounds.Height * 0.8;
                double ratio = width / (double)height;
                bool videoRatio = ratio >= 1.2 && ratio <= 2.6;
                bool topMost =
                    (Native.GetExtendedStyle(window.Handle) & Native.WS_EX_TOPMOST) != 0;
                bool sameProcess = String.Equals(
                    window.ProcessName,
                    selectedProcess,
                    StringComparison.OrdinalIgnoreCase);
                string title = window.Title.ToLowerInvariant();
                bool titleMatch = title.Contains("画中画")
                    || title.Contains("picture in picture")
                    || title.Contains("picture-in-picture");

                if (!titleMatch && !(sameProcess && topMost && compact && videoRatio))
                {
                    continue;
                }

                int score = (titleMatch ? 100 : 0)
                    + (sameProcess ? 40 : 0)
                    + (topMost ? 25 : 0)
                    + (compact ? 15 : 0)
                    + (videoRatio ? 10 : 0);
                if (score > bestScore)
                {
                    best = window;
                    bestScore = score;
                }
            }

            return best;
        }

        private void SelectArea(object sender, EventArgs e)
        {
            if (!EnsureTarget())
            {
                return;
            }

            Hide();
            Rectangle selected = Rectangle.Empty;
            try
            {
                using (RegionSelector selector = new RegionSelector())
                {
                    if (selector.ShowDialog() == DialogResult.OK)
                    {
                        selected = selector.SelectedBounds;
                    }
                }
            }
            finally
            {
                Show();
                Activate();
            }

            if (!selected.IsEmpty)
            {
                ApplyRectangle(selected);
            }
        }

        private void ToggleTopMost(object sender, EventArgs e)
        {
            if (_updating || !EnsureTarget())
            {
                return;
            }

            Native.SetWindowPos(
                _target,
                _topMost.Checked ? Native.HWND_TOPMOST : Native.HWND_NOTOPMOST,
                0,
                0,
                0,
                0,
                Native.SWP_NOMOVE | Native.SWP_NOSIZE | Native.SWP_NOACTIVATE);
        }

        private void ChangeOpacity(object sender, EventArgs e)
        {
            _opacityText.Text = _opacity.Value + "%";
            if (!EnsureTarget())
            {
                return;
            }

            long style = Native.GetExtendedStyle(_target);
            if ((style & Native.WS_EX_LAYERED) == 0)
            {
                Native.SetExtendedStyle(_target, style | Native.WS_EX_LAYERED);
            }

            byte alpha = (byte)Math.Round(255D * _opacity.Value / 100D);
            Native.SetLayeredWindowAttributes(_target, 0, alpha, Native.LWA_ALPHA);
        }

        private void Status(string text)
        {
            _status.Text = "状态：" + text;
            _status.ForeColor = Color.FromArgb(77, 89, 103);
        }

        private void Error(string text)
        {
            _status.Text = "提示：" + text;
            _status.ForeColor = Color.FromArgb(190, 56, 56);
        }
    }

    internal sealed class RegionSelector : Form
    {
        private bool _dragging;
        private Point _start;
        private Rectangle _selection;

        internal RegionSelector()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = SystemInformation.VirtualScreen;
            BackColor = Color.Black;
            Opacity = 0.25;
            TopMost = true;
            ShowInTaskbar = false;
            Cursor = Cursors.Cross;
            KeyPreview = true;
            DoubleBuffered = true;
        }

        internal Rectangle SelectedBounds { get; private set; }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            base.OnKeyDown(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _start = e.Location;
                _selection = Rectangle.Empty;
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_dragging)
            {
                _selection = Normalize(_start, e.Location);
                Invalidate();
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (_dragging && e.Button == MouseButtons.Left)
            {
                _dragging = false;
                Capture = false;
                _selection = Normalize(_start, e.Location);
                if (_selection.Width >= 50 && _selection.Height >= 50)
                {
                    SelectedBounds = new Rectangle(
                        Bounds.Left + _selection.Left,
                        Bounds.Top + _selection.Top,
                        _selection.Width,
                        _selection.Height);
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (Font font = new Font("Microsoft YaHei UI", 16F, FontStyle.Bold))
            using (Brush brush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(
                    "拖出目标窗口区域；按 Esc 取消",
                    font,
                    brush,
                    30,
                    30);
            }

            if (!_selection.IsEmpty)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 4F))
                {
                    e.Graphics.DrawRectangle(pen, _selection);
                }
            }
        }

        private static Rectangle Normalize(Point start, Point end)
        {
            return Rectangle.FromLTRB(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Max(start.X, end.X),
                Math.Max(start.Y, end.Y));
        }
    }
}
