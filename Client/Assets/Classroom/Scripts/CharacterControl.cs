using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NeutronCat.Classroom
{
    public class CharacterControl : MonoBehaviour
    {

        [SerializeField] private float _moveSpeed;
        [SerializeField] private float _rotateSpeed;

        private CharacterController _characterController;
        private Camera _camera;
        private Vector2 _moveInput;
        private Vector2 _mouseDelta;

        void Start()
        {
            _characterController = GetComponent<CharacterController>();
            _camera = Camera.main;

            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update()
        {
            // 获取键盘输入
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                float vertical = 0f;
                float horizontal = 0f;
                
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) vertical += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) vertical -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1f;
                
                _moveInput = new Vector2(horizontal, vertical);
            }

            Vector3 moveDir = transform.forward * _moveInput.y + transform.right * _moveInput.x;
            _characterController.SimpleMove(moveDir * _moveSpeed);

            // 获取鼠标输入
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                _mouseDelta = mouse.delta.ReadValue();
                float yRot = _mouseDelta.x * _rotateSpeed * Time.deltaTime;
                float xRot = _mouseDelta.y * _rotateSpeed * Time.deltaTime;
                transform.Rotate(0, yRot, 0);
                _camera.transform.Rotate(-xRot, 0, 0);

                if (mouse.leftButton.wasPressedThisFrame)
                    Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }
}