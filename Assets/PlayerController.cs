using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public NetworkClient networkClient;

    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.Translate(new Vector3(3 * Time.deltaTime, 0, 0), Space.World);
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.Translate(new Vector3(0, 0, 3 * Time.deltaTime), Space.World);
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.Translate(new Vector3(-3 * Time.deltaTime, 0, 0), Space.World);
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.Translate(new Vector3(0, 0, -3 * Time.deltaTime), Space.World);
        }
        if(Input.anyKey)
        {
            //Debug.Log("Sending Player Position");
            networkClient.SendPosition(transform.position);
        }
    }
}
