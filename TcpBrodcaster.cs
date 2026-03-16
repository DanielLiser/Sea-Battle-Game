using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace HaganaSimulator
{
    public class TcpBroadcaster
    {
        private TcpListener listener;
        private TcpClient connectedClient;
        private StreamWriter writer;

        public event Action<string> OnCommandReceived;

        public void StartServer(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine($"[TCP] Server started on port {port}. Waiting for Python to connect...");

            Task.Run(async () =>
            {
                while (true)
                {
                    connectedClient = await listener.AcceptTcpClientAsync();
                    Console.WriteLine("[TCP] Python client connected successfully!");

                    var stream = connectedClient.GetStream();
                    writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                    StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                    try
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != null)
                        {
                            OnCommandReceived?.Invoke(line);
                        }
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("[TCP] Client disconnected.");
                    }
                }
            });
        }

        public void SendEvent(object data)
        {
            if (writer != null && connectedClient != null && connectedClient.Connected)
            {
                try
                {
                    string json = JsonSerializer.Serialize(data);
                    writer.WriteLine(json);
                }
                catch { Console.WriteLine("FAILED TO SEND JSON"); }
            }
        }
    }
}