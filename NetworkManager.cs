using UnityEngine;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.IO;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance;

    private TcpClient socket;
    private NetworkStream stream;
    private Thread receiveThread;
    private bool isConnected = false;
    public GameData lastRecivedData = null;
    public int myPlayerIndex=0;
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ConnectToServer(string ip)
    {
        if (isConnected) return;

        try
        {
            Debug.Log("NetworkManager: Trying to connect to " + ip + "...");
            socket = new TcpClient(ip, 55555); 
            stream = socket.GetStream();
            isConnected = true;

            Debug.Log("NetworkManager: Connected successfully!");

            receiveThread = new Thread(ReceiveData);
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Connection Error: " + e.Message);
        }
    }

    public void SendJson(GameData data)
    {
        if (!isConnected || socket == null)
        {
            Debug.LogError("Cannot send data - Not connected!");
            return;
        }

        try
        {
            string json = JsonUtility.ToJson(data);

            json += "\n";
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            stream.Write(bytes, 0, bytes.Length);

            Debug.Log("NetworkManager Sent: " + json);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Send Error: " + e.Message);
            isConnected = false;
        }
    }

    private void ReceiveData()
    {
        byte[] buffer = new byte[1024];
        while (isConnected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.Log("Server Response: " + response);

                        GameData newData = JsonUtility.FromJson<GameData>(response);
                        lastRecivedData = newData;
                    }
                }
            }
            catch
            {
                isConnected = false;
            }
        }
    }

    void OnApplicationQuit()
    {
        isConnected = false;
        if (socket != null) socket.Close();
        if (receiveThread != null) receiveThread.Abort();
    }
    public void Disconnect() {
        OnApplicationQuit();
    }
}