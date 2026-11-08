using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace SkyQueue
{
    /// <summary>
    /// Event-driven tap detection for mouse, pen and <b>every</b> touch point. Press events are queued as they arrive, so a
    /// very quick tap whose press and release land in the same frame is never missed (polling <c>wasPressedThisFrame</c>
    /// can miss those), and a second finger is not ignored while the first is still down (<c>Pointer/press</c> only
    /// follows the primary touch). The position is read from the exact control that was pressed.
    /// Consumers drain the queue once per frame, keeping gameplay on the main-loop update.
    /// </summary>
    public sealed class TapInput : IDisposable
    {
        const int MaxQueued = 8;

        readonly InputAction press;
        readonly Queue<Vector2> pending = new Queue<Vector2>(MaxQueued);

        public TapInput()
        {
            // PassThrough: a Button action follows a single "driving" control and would ignore a second finger while the first is held.
            press = new InputAction("TapPress", InputActionType.PassThrough);
            press.AddBinding("<Touchscreen>/touch*/press");   // each finger separately
            press.AddBinding("<Mouse>/leftButton");
            press.AddBinding("<Pen>/tip");
            press.performed += OnPress;
            press.Enable();
        }

        void OnPress(InputAction.CallbackContext ctx)
        {
            if (!ctx.ReadValueAsButton()) return;   // pass-through also reports releases; only presses are taps
            Vector2 position;
            var touch = ctx.control.parent as TouchControl;
            if (touch != null) position = touch.position.ReadValue();
            else
            {
                var pointer = ctx.control.device as Pointer;
                if (pointer == null) return;
                position = pointer.position.ReadValue();
            }
            if (pending.Count >= MaxQueued) pending.Dequeue();
            pending.Enqueue(position);
        }

        public bool TryDequeue(out Vector2 screenPosition)
        {
            if (pending.Count == 0) { screenPosition = default(Vector2); return false; }
            screenPosition = pending.Dequeue();
            return true;
        }

        public void Clear() { pending.Clear(); }

        public void Dispose()
        {
            press.performed -= OnPress;
            press.Dispose();
        }
    }
}
