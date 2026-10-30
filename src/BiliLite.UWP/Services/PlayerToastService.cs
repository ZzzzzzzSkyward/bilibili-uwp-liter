using BiliLite.Controls;
using FontAwesome5;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BiliLite.Services
{
    /// <summary>
    /// 播放器浮层提示。每个 PlayerControl 各持一个实例，按 key 复用浮层，普通 key 自动隐藏，按住类 key 常显直到关闭。
    /// </summary>
    public sealed class PlayerToastService : IDisposable
    {
        public const string Volume = "Volume";
        public const string Brightness = "Brightness";
        public const string Progress = "Progress";
        public const string Msg = "Msg";
        public const string Accelerating = "Accelerating";
        public const string Speed = "Speed";

        private static readonly TimeSpan AutoHideInterval = TimeSpan.FromSeconds(1.5);

        private const double ScaleReferenceHeight = 720;
        private const double MinScale = 0.75;
        private const double MaxScale = 1.8;
        private const double TopOffsetRatio = 0.12;

        private readonly Panel _host;
        private readonly StackPanel _stack;
        private readonly Dictionary<string, OverlayEntry> _entries = new Dictionary<string, OverlayEntry>(StringComparer.Ordinal);
        private bool _disposed;

        public PlayerToastService(Panel host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }
            _host = host;
            _stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Top,
                IsHitTestVisible = false
            };
            _host.Children.Add(_stack);
            _host.SizeChanged += Host_SizeChanged;
            ApplyLayout();
        }

        /// <summary>
        /// 显示浮层并重播超时，<paramref name="key"/> 相同的浮层只保留一条且文本被替换
        /// </summary>
        public void Show(string key, string text)
        {
            Show(key, text, GetDefaultIcon(key));
        }

        public void Show(string key, string text, EFontAwesomeIcon icon)
        {
            if (_disposed || string.IsNullOrEmpty(key))
            {
                return;
            }
            var entry = EnsureEntry(key);
            entry.Toast.SetContent(text, icon);
            entry.Toast.ShowToast();
            if (entry.Held)
            {
                StopTimer(entry);
                return;
            }
            RestartTimer(entry);
        }

        /// <summary>
        /// 常显浮层，不启动自动隐藏，直到 <see cref="KeepClose(string)"/>
        /// </summary>
        public void KeepStart(string key, string text)
        {
            KeepStart(key, text, GetDefaultIcon(key));
        }

        public void KeepStart(string key, string text, EFontAwesomeIcon icon)
        {
            if (_disposed || string.IsNullOrEmpty(key))
            {
                return;
            }
            var entry = EnsureEntry(key);
            entry.Held = true;
            StopTimer(entry);
            entry.Toast.SetContent(text, icon);
            entry.Toast.ShowToast();
        }

        public void KeepClose(string key)
        {
            if (_disposed || string.IsNullOrEmpty(key))
            {
                return;
            }
            if (!_entries.TryGetValue(key, out var entry))
            {
                return;
            }
            entry.Held = false;
            StopTimer(entry);
            entry.Toast.HideToast();
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            _host.SizeChanged -= Host_SizeChanged;
            foreach (var entry in _entries.Values)
            {
                entry.Timer.Stop();
                entry.Timer.Tick -= entry.OnTick;
                entry.Toast.HideToast();
            }
            _entries.Clear();
            _stack.Children.Clear();
            _host.Children.Remove(_stack);
        }

        private OverlayEntry EnsureEntry(string key)
        {
            if (_entries.TryGetValue(key, out var existing))
            {
                return existing;
            }
            var toast = new PlayerToast();
            toast.ApplyScale(CurrentScale);
            _stack.Children.Add(toast);
            var entry = new OverlayEntry(toast);
            entry.Timer.Tick += entry.OnTick;
            _entries.Add(key, entry);
            return entry;
        }

        private void RestartTimer(OverlayEntry entry)
        {
            entry.Timer.Stop();
            entry.Timer.Start();
        }

        private static void StopTimer(OverlayEntry entry)
        {
            entry.Timer.Stop();
        }

        private double CurrentScale
        {
            get
            {
                var height = _host.ActualHeight;
                if (height <= 0)
                {
                    return 1;
                }
                return Math.Max(MinScale, Math.Min(height / ScaleReferenceHeight, MaxScale));
            }
        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ApplyLayout();
        }

        private void ApplyLayout()
        {
            _stack.Margin = new Thickness(0, _host.ActualHeight * TopOffsetRatio, 0, 0);
            var scale = CurrentScale;
            foreach (var entry in _entries.Values)
            {
                entry.Toast.ApplyScale(scale);
            }
        }

        private static EFontAwesomeIcon GetDefaultIcon(string key)
        {
            switch (key)
            {
                case Volume: return EFontAwesomeIcon.Solid_VolumeUp;
                case Brightness: return EFontAwesomeIcon.Solid_Sun;
                case Progress: return EFontAwesomeIcon.Solid_Clock;
                case Msg: return EFontAwesomeIcon.Solid_InfoCircle;
                case Accelerating: return EFontAwesomeIcon.Solid_Forward;
                case Speed: return EFontAwesomeIcon.Solid_TachometerAlt;
                default: return EFontAwesomeIcon.None;
            }
        }

        private sealed class OverlayEntry
        {
            public OverlayEntry(PlayerToast toast)
            {
                Toast = toast;
                Timer = new DispatcherTimer { Interval = AutoHideInterval };
            }

            public PlayerToast Toast { get; }
            public DispatcherTimer Timer { get; }
            public bool Held { get; set; }

            public void OnTick(object sender, object e)
            {
                Timer.Stop();
                Toast.HideToast();
            }
        }
    }
}
