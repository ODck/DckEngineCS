using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Veldrid;

namespace Dck.Engine.Graphics.Application
{
    public class Input
    {
        private readonly HashSet<Key> _currentlyPressedKeys = new();

        private readonly HashSet<MouseButton> _currentlyPressedMouseButtons = new();
        private readonly HashSet<Key> _newPressedKeys = new();
        private readonly HashSet<MouseButton> _newPressedMouseButtons = new();

        private InputSnapshot _frameSnapshot;
        public Vector2 MousePosition;

        public bool GetKey(Key key)
        {
            return _currentlyPressedKeys.Contains(key);
        }

        public bool GetKeyDown(Key key)
        {
            return _newPressedKeys.Contains(key);
        }

        public bool GetMouseButton(MouseButton button)
        {
            return _currentlyPressedMouseButtons.Contains(button);
        }

        public bool GetMouseButtonDown(MouseButton button)
        {
            return _newPressedMouseButtons.Contains(button);
        }

        [SuppressMessage("ReSharper", "ForCanBeConvertedToForeach")]
        public void UpdateFrameInput(InputSnapshot snapshot)
        {
            _frameSnapshot = snapshot;
            _newPressedKeys.Clear();
            _newPressedMouseButtons.Clear();

            MousePosition = snapshot.MousePosition;
            //For instead of foreach to avoid heap allocation
            for (var index = 0; index < snapshot.KeyEvents.Count; index++)
            {
                var snapshotKeyEvent = snapshot.KeyEvents[index];
                if (snapshotKeyEvent.Down)
                    KeyDown(snapshotKeyEvent.Key);
                else
                    KeyUp(snapshotKeyEvent.Key);
            }

            //For instead of foreach to avoid heap allocation
            for (var index = 0; index < snapshot.MouseEvents.Count; index++)
            {
                var snapshotMouseEvent = snapshot.MouseEvents[index];
                if (snapshotMouseEvent.Down)
                    MouseDown(snapshotMouseEvent.MouseButton);
                else
                    MouseUp(snapshotMouseEvent.MouseButton);
            }
        }

        private void KeyDown(Key key)
        {
            if (_currentlyPressedKeys.Add(key))
                _newPressedKeys.Add(key);
        }

        private void KeyUp(Key key)
        {
            _currentlyPressedKeys.Remove(key);
            _newPressedKeys.Remove(key);
        }

        private void MouseDown(MouseButton button)
        {
            if (_currentlyPressedMouseButtons.Add(button))
                _newPressedMouseButtons.Add(button);
        }

        private void MouseUp(MouseButton button)
        {
            _currentlyPressedMouseButtons.Remove(button);
            _newPressedMouseButtons.Remove(button);
        }
    }
}