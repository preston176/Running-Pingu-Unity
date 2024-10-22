using Cinemachine;
using System.Collections.Generic;
using UnityEngine;

// NOTE: highest priority camera is set to 10
public class CameraManager : MonoBehaviour
{
    private static List<CinemachineVirtualCamera> cameras = new List<CinemachineVirtualCamera>();

    public static CinemachineVirtualCamera ActiveCamera = null;

    public static bool IsActiveCamera(CinemachineVirtualCamera camera)
    {
        return camera == ActiveCamera;
    }

    public static void SwitchCamera(CinemachineVirtualCamera newCamera)
    {
        newCamera.Priority = 10;
        ActiveCamera = newCamera;

        foreach (CinemachineVirtualCamera cam in cameras)
        {
            if (cam != newCamera)
            {
                cam.Priority = 0;
            }
        }
    }

    public static void Register(CinemachineVirtualCamera camera)
    {
        cameras.Add(camera);
    }

    public static void Unregister(CinemachineVirtualCamera camera)
    {
        cameras.Remove(camera);
    }
}