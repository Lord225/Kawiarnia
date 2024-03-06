using System.Collections;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public bool ShouldMouseWorkOffScreen = true;
    public bool UseMouse;

    //Settings
    public int TresholdSizeInPixels;

    public float MinHeight;
    public float speed_mouse;
    public float speed_keyboard;
    public float mouseWheelSpeed;
    public float positiveWheelBoost;
    public float RotationSpeed;
    public float RotationDrag;

    [Range(0, 1)]
    public float drag;
    public float MaxHeight;

    public float TargetRotation;
    public float TargetHeight;

    public AnimationCurve movPower;
    public AnimationCurve ScrollPower;
    public AnimationCurve Angle;


    public bool isMouseBlocked { get; private set; }
    public float height { get; private set; }

    private Vector3 MousePos;
    private float HeightMovmentSpeedBoost = 0;
    private float accy = 0;

    private float CurrentRotation;
    private float RotationVelocity;
    private float RotationAnchor;
    private Vector3 MouseAnchor;
    private Vector3 acc = new Vector3(0, 0, 0);
    private Vector2 mouseScroll;
    private Rect Treshold;
    private Transform CameraPos;

    Quaternion rotation;

    private void Start()
    {
        CameraPos = GetComponentsInChildren<Transform>()[1];
        UpdateTreshold();
        rotation = Quaternion.identity;
    }

    void Update()
    {
        MousePos = Input.mousePosition;
        mouseScroll = Input.mouseScrollDelta;
        VerticalMovment();

        if (UseMouse)
        {
            if (!Treshold.Contains(MousePos))
            {
                if (!isMouseBlocked)
                {
                    CameraMouseBehavior();
                }
            }
        }
        CameraKeyBoardBehavior();
        CameraRotation();

        Vector3 Position = new Vector3(acc.z * transform.forward.x, 0, acc.z * transform.forward.z);
        Vector3 Position2 = new Vector3(acc.x * transform.right.x, 0, acc.x * transform.right.z);

        transform.position += Position;
        transform.position += Position2;

        CameraPos.localPosition = new Vector3(CameraPos.localPosition.x, height, -height - 3);

        acc *= drag;
    }

    void UpdateTreshold()
    {
        Treshold.x = TresholdSizeInPixels;
        Treshold.y = TresholdSizeInPixels;

        Treshold.width = Screen.width - 2 * (float)TresholdSizeInPixels;
        Treshold.height = Screen.height - 2 * (float)TresholdSizeInPixels;
    }

    void VerticalMovment()
    {
        if (mouseScroll.y > 0)
        {
            mouseScroll.y *= mouseScroll.y * mouseScroll.y * positiveWheelBoost;
        }
        TargetHeight += mouseScroll.y * mouseWheelSpeed * ScrollPower.Evaluate(Mathf.InverseLerp(MinHeight, MaxHeight, TargetHeight));

        TargetHeight = Mathf.Clamp(TargetHeight, MinHeight, MaxHeight);
        height = Mathf.SmoothDamp(height, TargetHeight, ref accy, 0.3f, 30f, Time.deltaTime);
        HeightMovmentSpeedBoost = Mathf.Lerp(1.0f, 5.0f, Mathf.InverseLerp(MinHeight, MaxHeight, TargetHeight));
    }
    void CameraKeyBoardBehavior()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        if (x != 0 || z != 0)
        {
            isMouseBlocked = true;
            acc.x += x * speed_keyboard * Time.deltaTime * HeightMovmentSpeedBoost;
            acc.z += z * speed_keyboard * Time.deltaTime * HeightMovmentSpeedBoost;
        }
        else
        {
            isMouseBlocked = false;
        }
    }



    void CameraRotation()
    {
        if (Input.GetKey(KeyCode.E)) //------>
        {
            TargetRotation += RotationSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.Q))//<-------
        {
            TargetRotation -= RotationSpeed * Time.deltaTime;
        }
        if (Input.GetMouseButtonDown(2))
        {
            isMouseBlocked = true;
            MouseAnchor = Input.mousePosition;
            RotationAnchor = TargetRotation;
        }
        if (Input.GetMouseButton(2))
        {
            var traveledDis = MouseAnchor - Input.mousePosition;
            TargetRotation = traveledDis.x * 0.15f + RotationAnchor;
        }
        if (Mathf.Abs(RotationVelocity) < 0.001f)
        {
            isMouseBlocked = false;
        }

        CurrentRotation = Mathf.SmoothDamp(CurrentRotation, TargetRotation, ref RotationVelocity, 0.1f, 10000.0f, Time.deltaTime);

        Quaternion _rotation = Quaternion.Euler(Angle.Evaluate(Mathf.InverseLerp(MinHeight, MaxHeight, height)), 0, 0);

        transform.rotation = Quaternion.Euler(0, CurrentRotation, 0); ;
        CameraPos.localRotation = _rotation;
    }

    void CameraMouseBehavior()
    {
        int MouseRelativeToMid_x = (int)Input.mousePosition.x - Screen.width / 2;
        int MouseRelativeToMid_y = (int)Input.mousePosition.y - Screen.height / 2;


        if (MouseRelativeToMid_x < 0 && Input.mousePosition.x < Treshold.xMin) //L
        {
            acc.x = -GetTransformedDis(Input.mousePosition.x);
        }
        else if (MouseRelativeToMid_x > 0 && Input.mousePosition.x > Treshold.xMax) //R
        {
            acc.x = GetTransformedDis(Screen.width - Input.mousePosition.x);
        }

        if (MouseRelativeToMid_y < 0 && Input.mousePosition.y < Treshold.yMin) //D
        {
            acc.z = -GetTransformedDis(Input.mousePosition.y);
        }
        else if (MouseRelativeToMid_y > 0 && Input.mousePosition.y > Treshold.yMax) //U
        {
            acc.z = GetTransformedDis(Screen.height - Input.mousePosition.y);
        }
    }
    float GetTransformedDis(float dis)
    {
        if (ShouldMouseWorkOffScreen && dis < 0)
        {
            return 0;
        }
        return movPower.Evaluate(1.0f - (float)dis / TresholdSizeInPixels) * speed_mouse * HeightMovmentSpeedBoost * Time.deltaTime;
    }

}