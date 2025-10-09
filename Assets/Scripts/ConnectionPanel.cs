using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class ConnectionPanel : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] UnityTransport unityTransport;

    [Header("UI elements"), Space()]
    [SerializeField] GameObject panel;
    [SerializeField] GameObject disconnectButton;
    [SerializeField] TMP_InputField addressField;

    public void StartHost()
    {
        unityTransport.SetConnectionData("", 7777);
        networkManager.StartHost();
        UpdateUI(false);
    }

    public void StartClient()
    {
        unityTransport.SetConnectionData(addressField.text, 7777);
        if (networkManager.StartClient())
            UpdateUI(false);
    }

    public void Disconnect()
    {
        networkManager.Shutdown(true);
        UpdateUI(true);
    }

    private void UpdateUI(bool openPanel)
    {
        panel.SetActive(openPanel);
        disconnectButton.SetActive(!openPanel);
    }
}
