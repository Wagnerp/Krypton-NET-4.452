﻿// *****************************************************************************
// BSD 3-Clause License (https://github.com/ComponentFactory/Krypton/blob/master/LICENSE)
//  © Component Factory Pty Ltd, 2006 - 2016, All rights reserved.
// The software and associated documentation supplied hereunder are the 
//  proprietary information of Component Factory Pty Ltd, 13 Swallows Close, 
//  Mornington, Vic 3931, Australia and are supplied subject to license terms.
// 
//  Modifications by Peter Wagner(aka Wagnerp) & Simon Coghlan(aka Smurf-IV) 2017 - 2020. All rights reserved. (https://github.com/Wagnerp/Krypton-NET-5.452)
//  Version 5.452.0.0  www.ComponentFactory.com
// *****************************************************************************

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using ComponentFactory.Krypton.Toolkit;

namespace ComponentFactory.Krypton.Ribbon
{
    /// <summary>
    /// Process mouse events for a gallery button.
    /// </summary>
    internal class GalleryButtonController : GlobalId,
                                             IMouseController
    {
        #region Instance Fields

        private bool _pressed;
        private bool _mouseOver;
        private NeedPaintHandler _needPaint;
        private readonly Timer _repeatTimer;
        #endregion

        #region Events
        /// <summary>
        /// Occurs when the mouse is used to left click the target.
        /// </summary>
        public event MouseEventHandler Click;
        #endregion

        #region Identity
        /// <summary>
        /// Initialize a new instance of the GalleryButtonController class.
        /// </summary>
        /// <param name="target">Target for state changes.</param>
        /// <param name="needPaint">Delegate for notifying paint requests.</param>
        /// <param name="repeatTimer">Does the button repeat when pressed.</param>
        public GalleryButtonController(ViewBase target,
                                       NeedPaintHandler needPaint,
                                       bool repeatTimer)
        {
            Debug.Assert(target != null);

            Target = target;
            NeedPaint = needPaint;

            if (repeatTimer)
            {
                _repeatTimer = new Timer
                {
                    Interval = 250
                };
                _repeatTimer.Tick += OnRepeatTick;
            }
        }
        #endregion

        #region ForceLeave
        /// <summary>
        /// Force the leaving of the area.
        /// </summary>
        public void ForceLeave()
        {
            if (_mouseOver)
            {
                _pressed = false;
                _mouseOver = false;
                UpdateTargetState(new Point(int.MaxValue, int.MaxValue));
                _repeatTimer?.Stop();
            }
        }
        #endregion

        #region Mouse Notifications
        /// <summary>
        /// Mouse has entered the view.
        /// </summary>
        /// <param name="c">Reference to the source control instance.</param>
        public virtual void MouseEnter(Control c)
        {
            _mouseOver = true;
            UpdateTargetState(c);
        }

        /// <summary>
        /// Mouse has moved inside the view.
        /// </summary>
        /// <param name="c">Reference to the source control instance.</param>
        /// <param name="pt">Mouse position relative to control.</param>
        public virtual void MouseMove(Control c, Point pt)
        {
        }

        /// <summary>
        /// Mouse button has been pressed in the view.
        /// </summary>
        /// <param name="c">Reference to the source control instance.</param>
        /// <param name="pt">Mouse position relative to control.</param>
        /// <param name="button">Mouse button pressed down.</param>
        /// <returns>True if capturing input; otherwise false.</returns>
        public virtual bool MouseDown(Control c, Point pt, MouseButtons button)
        {
            // Only interested in left mouse pressing down
            if (button == MouseButtons.Left)
            {
                _pressed = true;
                UpdateTargetState(pt);

                if (Target.Enabled)
                {
                    OnClick(new MouseEventArgs(MouseButtons.Left, 1, pt.X, pt.Y, 0));
                    _repeatTimer?.Start();
                }
            }

            return false;
        }

        /// <summary>
        /// Mouse button has been released in the view.
        /// </summary>
        /// <param name="c">Reference to the source control instance.</param>
        /// <param name="pt">Mouse position relative to control.</param>
        /// <param name="button">Mouse button released.</param>
        public virtual void MouseUp(Control c, Point pt, MouseButtons button)
        {
            // If the mouse is currently pressed
            if (_pressed)
            {
                _pressed = false;
                UpdateTargetState(pt);
                _repeatTimer?.Stop();
            }
        }

        /// <summary>
        /// Mouse has left the view.
        /// </summary>
        /// <param name="c">Reference to the source control instance.</param>
        /// <param name="next">Reference to view that is next to have the mouse.</param>
        public virtual void MouseLeave(Control c, ViewBase next)
        {
            // Only if mouse is leaving all the children monitored by controller.
            if (!Target.ContainsRecurse(next))
            {
                _pressed = false;
                _mouseOver = false;
                UpdateTargetState(c);
                _repeatTimer?.Stop();
            }
        }

        /// <summary>
        /// Left mouse button double click.
        /// </summary>
        /// <param name="pt">Mouse position relative to control.</param>
        public virtual void DoubleClick(Point pt)
        {
            // Do nothing
        }

        /// <summary>
        /// Should the left mouse down be ignored when present on a visual form border area.
        /// </summary>
        public virtual bool IgnoreVisualFormLeftButtonDown => false;

        #endregion

        #region Public
        /// <summary>
        /// Gets and sets the need paint delegate for notifying paint requests.
        /// </summary>
        public NeedPaintHandler NeedPaint
        {
            get => _needPaint;

            set
            {
                // Warn if multiple sources want to hook their single delegate
                Debug.Assert(((_needPaint == null) && (value != null)) ||
                             ((_needPaint != null) && (value == null)));

                _needPaint = value;
            }
        }

        /// <summary>
        /// Gets access to the associated target of the controller.
        /// </summary>
        public ViewBase Target { get; }

        /// <summary>
        /// Fires the NeedPaint event.
        /// </summary>
        public void PerformNeedPaint()
        {
            OnNeedPaint(false);
        }

        /// <summary>
        /// Fires the NeedPaint event.
        /// </summary>
        /// <param name="needLayout">Does the palette change require a layout.</param>
        public void PerformNeedPaint(bool needLayout)
        {
            OnNeedPaint(needLayout);
        }
        #endregion

        #region Protected
        /// <summary>
        /// Set the correct visual state of the target.
        /// </summary>
        /// <param name="c">Owning control.</param>
        protected void UpdateTargetState(Control c)
        {
            // Check we have a valid control to convert coordinates against
            if ((c != null) && !c.IsDisposed)
            {
                // Ensure control is inside a visible top level form
                Form f = c.FindForm();
                if ((f != null) && f.Visible)
                {
                    UpdateTargetState(c.PointToClient(Control.MousePosition));
                    return;
                }
            }

            UpdateTargetState(new Point(int.MaxValue, int.MaxValue));
        }

        /// <summary>
        /// Set the correct visual state of the target.
        /// </summary>
        /// <param name="pt">Mouse point.</param>
        protected virtual void UpdateTargetState(Point pt)
        {
            // By default the button is in the normal state
            PaletteState newState;

            // If the button is disabled then show as disabled
            if (!Target.Enabled)
            {
                newState = PaletteState.Disabled;
                _repeatTimer?.Stop();
            }
            else
            {
                newState = _mouseOver ? (_pressed ? PaletteState.Pressed : PaletteState.Tracking) : PaletteState.Normal;
            }

            // If state has changed or change in (inside split area)
            if (Target.ElementState != newState)
            {
                // Update target to reflect new state
                Target.ElementState = newState;

                // Redraw to show the change in visual state
                OnNeedPaint(true);
            }
        }

        /// <summary>
        /// Raises the Click event.
        /// </summary>
        /// <param name="e">A MouseEventArgs containing the event data.</param>
        protected virtual void OnClick(MouseEventArgs e)
        {
            Click?.Invoke(Target, e);
        }

        /// <summary>
        /// Raises the NeedPaint event.
        /// </summary>
        /// <param name="needLayout">Does the palette change require a layout.</param>
        protected virtual void OnNeedPaint(bool needLayout)
        {
            _needPaint?.Invoke(this, new NeedLayoutEventArgs(needLayout, Target.ClientRectangle));
        }
        #endregion

        #region Private
        private void OnRepeatTick(object sender, EventArgs e)
        {
            if (Target.Enabled)
            {
                OnClick(new MouseEventArgs(MouseButtons.Left, 1, 0, 0, 0));
            }
            else
            {
                _repeatTimer.Stop();
            }
        }
            
        #endregion
    }
}
