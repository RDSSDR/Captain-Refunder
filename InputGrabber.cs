using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CR
{
    public class InputGrabber : MonoBehaviour
    {
        public bool sprintPressed = false;
        public bool sprintWasPressed = false;

        void Awake()
        {
            Debug.Log("InputGrabber: Awake called.");
        }

        void Destroy()
        {
            Debug.Log("InputGrabber: Destroy called.");
        }

        void Update()
        {
            if (!GM_Hub.isInHub || !Program.MetaUiOpen())
            {
                //Debug.Log("InputGrabber: Not in hub or MetaUi is not open");
                return;
            }
            sprintPressed = Input.GetKeyDown(KeyCode.LeftShift);
            if (sprintPressed)
            {
                Debug.Log($"InputGrabber: Sprint key released, toggling subtract mode.");
                Program.subtractMode = !Program.subtractMode;
                Program.RefreshButtonsText();
            }
        }
    }
}
