using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VVVV.Utils.IO;

namespace mp.essentials
{
    public static class MpString
    {
        public static string RemoveDiacritics(this string text)
        {
            return string.Concat(
                text.Normalize(NormalizationForm.FormD)
                    .Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) !=
                                 UnicodeCategory.NonSpacingMark)
            ).Normalize(NormalizationForm.FormC);
        }

        public static Tuple<int, int, int> LineRangeFromCharIndex(this string input, int charid)
        {
            int linestart = 0;
            int lineend = 0;
            while (true)
            {
                lineend = input.IndexOfAny(new [] {'\r','\n'}, lineend)+1;
                if(charid >= linestart && charid < lineend) break;
                linestart = lineend;
            }
            var linelength = lineend - linestart;
            //if (lineend < input.Length) linelength++;
            return new Tuple<int, int, int>(linestart, lineend, linelength);
        }
    }

    public class AccumulatingMouseClick
    {
        public MouseButtons Button { get; private set; }
        public int ClickCount { get; private set; }
        public bool Pressed { get; private set; }
        public bool DoubleClick { get; private set; }
        public Stopwatch TimeSinceClicked { get; set; } = new Stopwatch();

        public AccumulatingMouseClick(MouseButtons button)
        {
            Button = button;
            TimeSinceClicked.Start();
        }
        public void Reset()
        {
            ClickCount = 0;
            DoubleClick = false;
        }
        public void Press()
        {
            ClickCount++;
            Pressed = true;
        }

        public void Release()
        {
            if (ClickCount == 0)
                Pressed = false;
            if (TimeSinceClicked.Elapsed.TotalSeconds < 0.18)
                DoubleClick = true;
            TimeSinceClicked.Restart();
        }
    }
    public class AccumulatingMouseObserver : IObserver<MouseNotification>
    {
        private IDisposable unsubscriber;

        public MouseNotification LastNotification { get; set; }
        public Func<MouseNotification, bool> Filter { get; set; }
        public int AccumulatedXDelta => AccCurrPos.X - AccHoldPos.X;
        public int AccumulatedYDelta => AccCurrPos.Y - AccHoldPos.Y;

        public int AccumulatedWheelDelta { get; private set; } = 0;
        public int AccumulatedHorizontalWheelDelta { get; private set; } = 0;

        public Dictionary<MouseButtons, AccumulatingMouseClick> MouseClicks { get; private set; }

        private Point AccCurrPos;
        private Point AccHoldPos = new Point(0,0);

        public AccumulatingMouseObserver()
        {
            MouseClicks = new Dictionary<MouseButtons, AccumulatingMouseClick>
            {
                { MouseButtons.Left, new AccumulatingMouseClick(MouseButtons.Left) },
                { MouseButtons.Right, new AccumulatingMouseClick(MouseButtons.Right) },
                { MouseButtons.Middle, new AccumulatingMouseClick(MouseButtons.Middle) },
                { MouseButtons.XButton1, new AccumulatingMouseClick(MouseButtons.XButton1) },
                { MouseButtons.XButton2, new AccumulatingMouseClick(MouseButtons.XButton2) }
            };
        }

        public void OnNext(MouseNotification value)
        {
            switch (value.Kind)
            {
                case MouseNotificationKind.MouseWheel:
                    var wn = (MouseWheelNotification) value;
                    AccumulatedWheelDelta += wn.WheelDelta;
                    break;
                case MouseNotificationKind.MouseHorizontalWheel:
                    var hwn = (MouseHorizontalWheelNotification)value;
                    AccumulatedHorizontalWheelDelta += hwn.WheelDelta;
                    break;
                case MouseNotificationKind.MouseMove:
                    AccCurrPos = value.Position;
                    break;
                case MouseNotificationKind.MouseDown:
                    var mdn = (MouseButtonNotification) value;
                    MouseClicks[mdn.Buttons].Press();
                    break;
                case MouseNotificationKind.MouseUp:
                    var mun = (MouseButtonNotification)value;
                    MouseClicks[mun.Buttons].Release();
                    break;
            }
            if (Filter == null)
            {
                LastNotification = value;
            }
            else if(Filter(value))
            {
                LastNotification = value;
            }
        }

        public void ResetAccumulation()
        {
            AccHoldPos = new Point(AccCurrPos.X, AccCurrPos.Y);
            AccumulatedWheelDelta = 0;
            AccumulatedHorizontalWheelDelta = 0;
            foreach (var button in MouseClicks.Values)
            {
                button.Reset();
            }
        }

        public void OnError(Exception error) { }

        public void OnCompleted() { }

        public void SubscribeTo(IObservable<MouseNotification> mouse)
        {
            unsubscriber = mouse?.Subscribe(this);
        }
        public void Unsubscribe()
        {
            unsubscriber?.Dispose();
        }
    }

    public class AccumulatingKeyPress : ICloneable
    {
        public Keys KeyCode { get; set; }
        public bool Pressed { get; set; }
        public int Count { get; set; }
        public object Clone()
        {
            return new AccumulatingKeyPress()
            {
                KeyCode = KeyCode,
                Pressed = Pressed,
                Count = Count
            };
        }
    }
    public class AccumulatingKeyboardObserver : IObserver<KeyNotification>
    {
        private IDisposable unsubscriber;
        public Dictionary<Keys, AccumulatingKeyPress> Keypresses { get; } = new Dictionary<Keys, AccumulatingKeyPress>();

        public void OnNext(KeyNotification value)
        {
            switch (value.Kind)
            {
                case KeyNotificationKind.KeyDown:
                    var kdn = (KeyDownNotification) value;
                    if (Keypresses.ContainsKey(kdn.KeyCode))
                    {
                        Keypresses[kdn.KeyCode].Count++;
                        Keypresses[kdn.KeyCode].Pressed = true;
                    }
                    else
                    {
                        Keypresses.Add(kdn.KeyCode, new AccumulatingKeyPress()
                        {
                            KeyCode = kdn.KeyCode,
                            Count = 1,
                            Pressed = true
                        });
                    }
                    break;
                case KeyNotificationKind.KeyUp:
                    var kun = (KeyUpNotification)value;
                    if (Keypresses.ContainsKey(kun.KeyCode))
                    {
                        Keypresses[kun.KeyCode].Pressed = false;
                    }
                    break;
            }
        }

        public void ResetAccumulation()
        {
            var removables = (from kvp in Keypresses where !kvp.Value.Pressed select kvp.Key).ToArray();
            for (int i = 0; i < removables.Length; i++)
            {
                if (Keypresses.ContainsKey(removables[i]))
                    Keypresses.Remove(removables[i]);
            }
            foreach (var key in Keypresses.Values)
            {
                key.Count = 0;
            }
        }

        public void OnError(Exception error) { }

        public void OnCompleted() { }

        public void SubscribeTo(IObservable<KeyNotification> keyboard)
        {
            unsubscriber = keyboard?.Subscribe(this);
        }
        public void Unsubscribe()
        {
            unsubscriber?.Dispose();
        }
    }
}
