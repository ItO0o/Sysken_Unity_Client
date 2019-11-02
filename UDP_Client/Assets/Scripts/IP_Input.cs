using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IP_Input : MonoBehaviour {
    public string ip;
    public int id;

    public void StartNetWork() {
        ip = GameObject.Find("IP_InputField").GetComponent<InputField>().text;
        id = int.Parse(GameObject.Find("ID_InputField").GetComponent<InputField>().text);

        GameObject.Find("UDP_Manager").AddComponent<UDP_Client>();
    }

}
