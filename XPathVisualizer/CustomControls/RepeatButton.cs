// RepeatButton.cs
// ------------------------------------------------------------------
//
// a Windows Forms button that repeats if you hold it down.
//
// ------------------------------------------------------------------
//
// Part of XpathVisualizer, and licensed under the Ms-PL as part of
// XpathVisualizer.
//
// Copyright (c) 2009 by Dino Chiesa
// All rights reserved!
//
// ------------------------------------------------------------------


using System.Windows.Forms;
using System.ComponentModel;  // for Category, Description attributes

namespace Ionic.WinForms
{
    /// <summary>
    /// A specialized Windows.Forms.Button that offers repeat click
    /// behavior while the button is held down.
    /// </summary>
    public class RepeatButton : Button
    {
        private int _tickCount;
        private Timer _timer;

        /// <summary>
        /// The number of intervals to delay, during initial press, before
        /// doing the auto-click thing.
        /// </summary>
        [Category("Data"),Description("Sets or gets the number of tick intervals to delay, before starting the repeat")]
        public int DelayTicks
        {
            get; set;
        }


        /// <summary>
        /// The interval in milliseconds on which ticks will occur, while the button is held down.
        /// </summary>
        [Category("Data"),Description("The interval in milliseconds on which ticks will occur, while the button is held down")]
        public int Interval
        {
            get
            {
                return Timer.Interval;
            }
            set
            {
                Timer.Interval = value;
            }
        }


        private Timer Timer
        {
            get
            {
                if (_timer == null)
                {
                    _timer = new Timer();
                    _timer.Tick += new System.EventHandler(OnTimer);
                    _timer.Enabled = false;
                }
                return _timer;
            }
        }

        protected override void OnMouseDown(MouseEventArgs me)
        {
            base.OnMouseDown(me);

            // turn on the timer
            Timer.Enabled = true;
            _tickCount = 0;
        }

        protected override void OnMouseUp(MouseEventArgs me)
        {
            // turn off the timer
            Timer.Enabled = false;
            base.OnMouseUp(me);
        }

        private void OnTimer(object sender, System.EventArgs e)
        {
            _tickCount++;

            // fire off a click on each timer tick,
            // after the initial delay.
            if (_tickCount > DelayTicks)
                OnClick(System.EventArgs.Empty);
        }
    }

}