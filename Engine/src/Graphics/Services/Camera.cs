using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Dck.Engine.Graphics.Application;
using Dck.Engine.Logging;
using Newtonsoft.Json;
using Veldrid;

namespace Dck.Engine.Graphics.Services
{
    public class Camera
    {
        public delegate Camera Factory(uint width, uint height);

        private readonly Input _input;
        private float _far = 1000F;

        private Vector3 _lookDirection = new(0, -.3f, -1f);
        private float _near = 1F;
        private float _pitch;

        private Vector3 _position = new(0, 3, 0);

        private Vector2 _previousMousePos;

        private float _windowHeight;
        private float _windowWidth;

        private float _yaw;

        public Camera(Input input, float width, float height)
        {
            _input = input;
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
            UpdateViewMatrix();
        }

        public Matrix4x4 ViewMatrix { get; private set; }

        public Matrix4x4 ProjectionMatrix { get; set; }

        public Vector3 Position
        {
            get => _position;
            set
            {
                _position = value;
                UpdateViewMatrix();
            }
        }

        public float FarDistance
        {
            get => _far;
            set
            {
                _far = value;
                UpdatePerspectiveMatrix();
            }
        }

        public float FieldOfView { get; } = 1F;

        public float NearDistance
        {
            get => _near;
            set
            {
                _near = value;
                UpdatePerspectiveMatrix();
            }
        }

        public float AspectRatio => _windowWidth / _windowHeight;

        public float Yaw
        {
            get => _yaw;
            set
            {
                _yaw = value;
                UpdateViewMatrix();
            }
        }

        public float Pitch
        {
            get => _pitch;
            set
            {
                _pitch = value;
                UpdateViewMatrix();
            }
        }

        public float MoveSpeed { get; set; } = 10.0f;
        public Vector3 Forward => GetLookDir();

        public event Action<Matrix4x4> ProjectionChanged;
        public event Action<Matrix4x4> ViewChanged;

        public void Update(float deltaSeconds)
        {
            var sprintFactor = _input.GetKey(Key.ControlLeft)
                ? 0.1f
                : _input.GetKey(Key.ShiftLeft)
                    ? 2.5f
                    : 1f;
            var motionDir = Vector3.Zero;
            if (_input.GetKey(Key.A)) motionDir += -Vector3.UnitX;
            if (_input.GetKey(Key.D)) motionDir += Vector3.UnitX;
            if (_input.GetKey(Key.W)) motionDir += -Vector3.UnitZ;
            if (_input.GetKey(Key.S)) motionDir += Vector3.UnitZ;
            if (_input.GetKey(Key.Q)) motionDir += -Vector3.UnitY;
            if (_input.GetKey(Key.E)) motionDir += Vector3.UnitY;
            
            if (motionDir != Vector3.Zero)
            {
                var lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
                motionDir = Vector3.Transform(motionDir, lookRotation);
                _position += motionDir * MoveSpeed * sprintFactor * deltaSeconds;
                UpdateViewMatrix();
            }

            var mouseDelta = _input.MousePosition - _previousMousePos;
            _previousMousePos = _input.MousePosition;

            if (_input.GetMouseButton(MouseButton.Left) || _input.GetMouseButton(MouseButton.Right))
            {
                Yaw += -mouseDelta.X * 0.01f;
                Pitch += -mouseDelta.Y * 0.01f;
                Pitch = Math.Clamp(Pitch, -1.55f, 1.55f);

                UpdateViewMatrix();
            }
        }

        public void WindowResized(float width, float height)
        {
            _windowWidth = width;
            _windowHeight = height;
            UpdatePerspectiveMatrix();
        }

        private void UpdatePerspectiveMatrix()
        {
            ProjectionMatrix =
                Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView, _windowWidth / _windowHeight, _near, _far);
            ProjectionChanged?.Invoke(ProjectionMatrix);
        }

        private void UpdateViewMatrix()
        {
            var lookDir = GetLookDir();
            _lookDirection = lookDir;
            ViewMatrix = Matrix4x4.CreateLookAt(_position, _position + _lookDirection, Vector3.UnitY);
            ViewChanged?.Invoke(ViewMatrix);
        }

        private Vector3 GetLookDir()
        {
            var lookRotation = Quaternion.CreateFromYawPitchRoll(Yaw, Pitch, 0f);
            var lookDir = Vector3.Transform(-Vector3.UnitZ, lookRotation);
            return lookDir;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CameraInfo
    {
        public Vector3 CameraPosition_WorldSpace;
        private readonly float _padding1;
        public Vector3 CameraLookDirection;
        private readonly float _padding2;
    }
}