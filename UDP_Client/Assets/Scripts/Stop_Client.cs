using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Stop_Client : MonoBehaviour
{
    public void stop()
	{
        GameObject.Find("UDP_Manager").GetComponent<UDP_Client>().CloseListener();
	}
}
