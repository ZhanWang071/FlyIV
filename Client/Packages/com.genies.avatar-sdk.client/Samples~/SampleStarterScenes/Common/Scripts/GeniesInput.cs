using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Genies.Sdk.Samples.Common
{
    public class GeniesInputs : MonoBehaviour
    {
        public Vector2 Move
        {
            get => _move;
            set => _move = value;
        }

        public Vector2 Look
        {
            get => _look;
            set => _look = value;
        }

        public bool Jump
        {
            get => _jump;
            set => _jump = value;
        }

        public bool Sprint
        {
            get => _sprint;
            set => _sprint = value;
        }

        public bool AnalogMovement
        {
            get => _analogMovement;
            set => _analogMovement = value;
        }

        public bool CursorLocked
        {
            get => _cursorLocked;
            set => _cursorLocked = value;
        }

        public bool CursorInputForLook
        {
            get => _cursorInputForLook;
            set => _cursorInputForLook = value;
        }

        [field: SerializeField] public bool EnableTouchControls { get; private set; } = true;
        

        [Header("New Input System")]
        [SerializeField]
        private InputActionReference _moveAction;

        [SerializeField] private InputActionReference _lookAction;

        [SerializeField] private InputActionReference _jumpAction;

        [SerializeField] private InputActionReference _sprintAction;

        [Header("Character Input Values (Read-Only)")] [SerializeField]
        private Vector2 _move;

        [SerializeField] private Vector2 _look;

        [SerializeField] private bool _jump;

        [SerializeField] private bool _sprint;

        [Header("Movement Settings (Read-Only)")] [SerializeField]
        private bool _analogMovement;

        [Header("Mouse Cursor Settings (Read-Only)")] [SerializeField]
        private bool _cursorLocked = true;

        [SerializeField] private bool _cursorInputForLook = true;
        private bool _inputInitialized = false;

        public event Action<bool> SprintToggled;
        public void MoveInput(Vector2 newMoveDirection)
        {
            Move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            Look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            Jump = newJumpState;
        }

        public void SprintInput(bool newSprintState, bool isTouchControls = true)
        {
            if (EnableTouchControls && isTouchControls)
            {
                Sprint = !Sprint;
            }
            else
            {
                Sprint = newSprintState;
                SprintToggled?.Invoke(newSprintState);
            }
        }

        private void OnEnable()
        {
            InitializeInputActions();
        }

        private void InitializeInputActions()
        {
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.Enable();
                if (!_inputInitialized)
                {
                    _moveAction.action.started += OnMoveActionPerformed;
                    _moveAction.action.performed += OnMoveActionPerformed;
                    _moveAction.action.canceled += OnMoveActionPerformed;
                }
            }
            else
            {
                Debug.LogWarning("Move Action is not assigned in the Inspector.", this);
            }

            if (_lookAction != null && _lookAction.action != null)
            {
                _lookAction.action.Enable();
            }
            else
            {
                Debug.LogWarning("Look Action is not assigned in the Inspector.", this);
            }

            if (_jumpAction != null && _jumpAction.action != null)
            {
                _jumpAction.action.Enable();
                if (!_inputInitialized)
                {
                    _jumpAction.action.started += OnJumpActionPerformed;
                }
            }
            else
            {
                Debug.LogWarning("Jump Action is not assigned in the Inspector.", this);
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.Enable();
                if (!_inputInitialized)
                {
                    _sprintAction.action.started += OnSprintActionPerformed;
                    _sprintAction.action.performed += OnSprintActionPerformed;
                    _sprintAction.action.canceled += OnSprintActionPerformed;
                }
            }
            else
            {
                Debug.LogWarning("Sprint Action is not assigned in the Inspector.", this);
            }

            _inputInitialized = true;
        }

        private void OnSprintActionPerformed(InputAction.CallbackContext obj)
        {
            SprintInput(_sprintAction.action.IsPressed(), false);
        }

        private void OnJumpActionPerformed(InputAction.CallbackContext obj)
        {
            JumpInput(_jumpAction.action.IsPressed());
        }

        private void OnMoveActionPerformed(InputAction.CallbackContext obj)
        {
            MoveInput(_moveAction.action.ReadValue<Vector2>());
        }

        private void OnDisable()
        {
            _inputInitialized = false;
            // Unsubscribe from events to prevent memory leaks
            if (_moveAction != null && _moveAction.action != null)
            {
                _moveAction.action.started -= OnMoveActionPerformed;
                _moveAction.action.performed -= OnMoveActionPerformed;
                _moveAction.action.canceled -= OnMoveActionPerformed;
                _moveAction.action.Disable();
            }

            if (_lookAction != null && _lookAction.action != null)
            {
                _lookAction.action.Disable();
            }

            if (_jumpAction != null && _jumpAction.action != null)
            {
                _jumpAction.action.started -= OnJumpActionPerformed;
                _jumpAction.action.Disable();
            }

            if (_sprintAction != null && _sprintAction.action != null)
            {
                _sprintAction.action.started -= OnSprintActionPerformed;
                _sprintAction.action.performed -= OnSprintActionPerformed;
                _sprintAction.action.canceled -= OnSprintActionPerformed;
                _sprintAction.action.Disable();
            }
        }
    }
}
