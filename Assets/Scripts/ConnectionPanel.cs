using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class ConnectionPanel : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    [SerializeField] UnityTransport unityTransport;

    [Header("UI elements"), Space()]
    [SerializeField] GameObject panel;
    [SerializeField] GameObject disconnectButton;
    [SerializeField] TMP_Text joinCodeText;
    [SerializeField] TMP_InputField joinCodeInputField;

    private async void Start()
    {
        await UnityServices.InitializeAsync();
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async void StartHost()
    {
        string joinCode = await StartHostWithRelay();
        joinCodeText.text = joinCode;
        UpdateUI(string.IsNullOrEmpty(joinCode));
    }

    private async Task<string> StartHostWithRelay(int maxConnections = 3)
    {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        unityTransport.SetRelayServerData(allocation.ToRelayServerData("dtls"));

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
        return networkManager.StartHost() ? joinCode : null;
    }

    public async void StartClient()
    {
        bool started = await StartClientWithRelay(joinCodeInputField.text);
        UpdateUI(!started);
    }

    private async Task<bool> StartClientWithRelay(string joinCode)
    {
        if (joinCode.Length == 0)
            return false;

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        unityTransport.SetRelayServerData(joinAllocation.ToRelayServerData("dtls"));
        return networkManager.StartClient();
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