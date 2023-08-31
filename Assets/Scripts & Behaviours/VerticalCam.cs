using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteAlways]
[RequireComponent(typeof(Camera))]
public class VerticalCam : MonoBehaviour
{
    private Camera _camera;
    private float _aspectRatio;
    private float _fieldOfView;
    private bool _isOrthographic;
    private float _orthographicSize;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void OnEnable()
    {
        CachePropertiesAndRecalculate();
    }

    private void LateUpdate()
    {
        if (CameraPropertyHasChanged())
            CachePropertiesAndRecalculate();
    }

    private void OnDisable()
    {
        _camera.ResetProjectionMatrix();
    }

    private bool CameraPropertyHasChanged()
    {
        bool hasChanged = (_aspectRatio != _camera.aspect
            || _fieldOfView != _camera.fieldOfView
            || _isOrthographic != _camera.orthographic
            || _orthographicSize != _camera.orthographicSize);

        return hasChanged;
    }

    private void CacheCameraProperties()
    {
        _aspectRatio = _camera.aspect;
        _fieldOfView = _camera.fieldOfView;
        _isOrthographic = _camera.orthographic;
        _orthographicSize = _camera.orthographicSize;
    }

    private void CachePropertiesAndRecalculate()
    {
        CacheCameraProperties();

        if (_camera.orthographic)
            RecalculateOrthographicMatrix();
        else
            RecalculatePerspectiveMatrix();
    }

    private void RecalculatePerspectiveMatrix()
    {
        float near = _camera.nearClipPlane;
        float nearx2 = near * 2.0f;
        float far = _camera.farClipPlane;
        float halfFovRad = _camera.fieldOfView * 0.5f * Mathf.Deg2Rad;

        // This is what aligns the camera horizontally.
        float width = nearx2 * Mathf.Tan(halfFovRad);
        float height = width / _camera.aspect;

        // This is the default behavior.
        //float height = nearx2 * Mathf.Tan(halfFovRad);
        //float width = height * _camera.aspect;

        float a = nearx2 / width;
        float b = nearx2 / height;
        float c = -(far + near) / (far - near);
        float d = -(nearx2 * far) / (far - near);

        Matrix4x4 newProjectionMatrix = new Matrix4x4(
            new Vector4(a, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, b, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, c, -1.0f),
            new Vector4(0.0f, 0.0f, d, 0.0f));

        _camera.projectionMatrix = newProjectionMatrix;
    }

    private void RecalculateOrthographicMatrix()
    {
        // This is what aligns the camera horizontally.
        float width = 2.0f * _camera.orthographicSize;
        float height = width / _camera.aspect;

        // This is the default behavior.
        //float height = 2.0f * _camera.orthographicSize;
        //float width = height * _camera.aspect;

        float near = _camera.nearClipPlane;
        float far = _camera.farClipPlane;
        float a = 2.0f / width;
        float b = 2.0f / height;
        float c = -2.0f / (far - near);
        float d = -(far + near) / (far - near);

        Matrix4x4 newProjectionMatrix = new Matrix4x4(
            new Vector4(a, 0.0f, 0.0f, 0.0f),
            new Vector4(0.0f, b, 0.0f, 0.0f),
            new Vector4(0.0f, 0.0f, c, 0.0f),
            new Vector4(0.0f, 0.0f, d, 1.0f));

        _camera.projectionMatrix = newProjectionMatrix;
    }
}