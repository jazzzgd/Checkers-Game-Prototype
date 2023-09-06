using System.Collections;
using UnityEngine;

public class CameraMove : MonoBehaviour
{
    public void MoveToAnotherSide()
    {
        StopAllCoroutines();
        StartCoroutine(RotateCamera());
    }

    private IEnumerator RotateCamera()
    {
        var fieldCenter = new Vector3(3.485f, 0, -3.485f);
        float rotationTime = 1f;

        while (rotationTime > 0)
        {
            transform.RotateAround(fieldCenter, Vector3.up, 180f * Time.deltaTime);
            rotationTime -= Time.deltaTime;
            yield return null;
        }
    }
}