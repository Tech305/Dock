﻿// Copyright (c) Wiesław Šoltés. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Dock.Avalonia
{
    /// <summary>
    /// Drag behavior.
    /// </summary>
    public sealed class DragBehavior : Behavior<Control>
    {
        private Point _dragStartPoint;
        private bool _pointerPressed;
        private bool _doDragDrop;

        /// <summary>
        /// Minimum horizontal drag distance to initiate drag operation.
        /// </summary>
        public static double MinimumHorizontalDragDistance = 4;

        /// <summary>
        /// Minimum vertical drag distance to initiate drag operation.
        /// </summary>
        public static double MinimumVerticalDragDistance = 4;

        /// <summary>
        /// Define <see cref="Context"/> property.
        /// </summary>
        public static readonly AvaloniaProperty ContextProperty =
            AvaloniaProperty.Register<DragBehavior, object>(nameof(Context));

        /// <summary>
        /// Define <see cref="Handler"/> property.
        /// </summary>
        public static readonly AvaloniaProperty HandlerProperty =
            AvaloniaProperty.Register<DragBehavior, IDragHandler>(nameof(Handler));

        /// <summary>
        /// Define IsEnabled attached property.
        /// </summary>
        public static readonly AvaloniaProperty IsEnabledProperty =
            AvaloniaProperty.RegisterAttached<Control, bool>("IsEnabled", typeof(DragBehavior), true, true, BindingMode.TwoWay);

        /// <summary>
        /// Gets or sets drag behavior context.
        /// </summary>
        public object Context
        {
            get => (object)GetValue(ContextProperty);
            set => SetValue(ContextProperty, value);
        }

        /// <summary>
        /// Gets or sets drag handler.
        /// </summary>
        public IDragHandler Handler
        {
            get => (IDragHandler)GetValue(HandlerProperty);
            set => SetValue(HandlerProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether the given control has drag operation enabled.
        /// </summary>
        /// <param name="control">The control object.</param>
        /// <returns>True if drag operation is enabled.</returns>
        public static bool GetIsEnabled(Control control)
        {
            return (bool)control.GetValue(IsEnabledProperty);
        }

        /// <summary>
        /// Sets IsEnabled attached property.
        /// </summary>
        /// <param name="control">The control object.</param>
        /// <param name="value">The drag operation flag.</param>
        public static void SetIsEnabled(Control control, bool value)
        {
            control.SetValue(IsEnabledProperty, value);
        }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            base.OnAttached();

            AssociatedObject.AddHandler(InputElement.PointerPressedEvent, PointerPressed, RoutingStrategies.Direct | RoutingStrategies.Bubble);
            AssociatedObject.AddHandler(InputElement.PointerReleasedEvent, PointerReleased, RoutingStrategies.Direct | RoutingStrategies.Bubble);
            AssociatedObject.AddHandler(InputElement.PointerMovedEvent, PointerMoved, RoutingStrategies.Direct | RoutingStrategies.Bubble);
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.RemoveHandler(InputElement.PointerPressedEvent, PointerPressed);
            AssociatedObject.RemoveHandler(InputElement.PointerReleasedEvent, PointerReleased);
            AssociatedObject.RemoveHandler(InputElement.PointerMovedEvent, PointerMoved);
        }

        private void PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (GetIsEnabled(AssociatedObject))
            {
                if (e.InputModifiers.HasFlag(InputModifiers.LeftMouseButton))
                {
                    _dragStartPoint = e.GetPosition(AssociatedObject);
                    _pointerPressed = true;
                    _doDragDrop = false;
                }
            }
        }

        private void PointerReleased(object sender, PointerReleasedEventArgs e)
        {
            if (GetIsEnabled(AssociatedObject))
            {
                _pointerPressed = false;
                _doDragDrop = false;
            }
        }

        private async void PointerMoved(object sender, PointerEventArgs e)
        {
            if (GetIsEnabled(AssociatedObject))
            {
                var point = e.GetPosition(AssociatedObject);
                Vector diff = _dragStartPoint - point;
                bool min = (Math.Abs(diff.X) > MinimumHorizontalDragDistance || Math.Abs(diff.Y) > MinimumVerticalDragDistance);
                if (_pointerPressed == true && _doDragDrop == false && min == true)
                {
                    _doDragDrop = true;

                    Handler?.BeforeDragDrop(sender, e, Context);

                    var data = new DataObject();
                    data.Set(DragDataFormats.Context, Context);

                    var effect = DragDropEffects.None;

                    if (e.InputModifiers.HasFlag(InputModifiers.Alt))
                    {
                        effect |= DragDropEffects.Link;
                    }
                    else if (e.InputModifiers.HasFlag(InputModifiers.Shift))
                    {
                        effect |= DragDropEffects.Move;
                    }
                    else if (e.InputModifiers.HasFlag(InputModifiers.Control))
                    {
                        effect |= DragDropEffects.Copy;
                    }
                    else
                    {
                        effect |= DragDropEffects.Move;
                    }

                    var result = await DragDrop.DoDragDrop(e, data, effect);

                    Handler?.AfterDragDrop(sender, e, Context);

                    _pointerPressed = false;
                    _doDragDrop = false;
                }
            }
        }
    }
}
