using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
/// <summary>
    /// TCP 네트워크 클라이언트
    /// </summary>
    public class NetworkClient
    {
        private TcpClient m_client;
        private NetworkStream m_stream;
        private CancellationTokenSource m_cts;
        private bool m_isConnected;

        /// <summary>
        /// 연결 상태
        /// </summary>
        public bool IsConnected => m_isConnected && m_client != null && m_client.Connected;

        /// <summary>
        /// 프로토콜 수신 이벤트
        /// </summary>
        public event Action<Protocol> OnProtocolReceived;

        /// <summary>
        /// 연결 해제 이벤트
        /// </summary>
        public event Action OnDisconnected;

        /// <summary>
        /// 에러 이벤트
        /// </summary>
        public event Action<string> OnError;

        /// <summary>
        /// 서버 연결
        /// </summary>
        public async Task<bool> ConnectAsync(string host, int port)
        {
            try
            {
                Disconnect();

                m_client = new TcpClient();
                m_cts = new CancellationTokenSource();

                await m_client.ConnectAsync(host, port);
                m_stream = m_client.GetStream();
                m_isConnected = true;

                Debug.Log($"[NetworkClient] Connected to {host}:{port}");

                // 수신 루프 시작
                _ = ReceiveLoopAsync();

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Connect failed: {e.Message}");
                OnError?.Invoke($"Connect failed: {e.Message}");
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 서버 연결 해제
        /// </summary>
        public void Disconnect()
        {
            if (!m_isConnected)
                return;

            m_isConnected = false;

            try
            {
                m_cts?.Cancel();
                m_stream?.Close();
                m_client?.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[NetworkClient] Disconnect error: {e.Message}");
            }
            finally
            {
                m_stream = null;
                m_client = null;
                m_cts?.Dispose();
                m_cts = null;
            }

            Debug.Log("[NetworkClient] Disconnected");
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// 프로토콜 전송
        /// </summary>
        public async Task<bool> SendAsync(Protocol protocol)
        {
            if (!IsConnected)
            {
                OnError?.Invoke("Not connected to server");
                return false;
            }

            try
            {
                byte[] data = protocol.Serialize();
                await m_stream.WriteAsync(data, 0, data.Length, m_cts.Token);
                await m_stream.FlushAsync(m_cts.Token);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Send failed: {e.Message}");
                OnError?.Invoke($"Send failed: {e.Message}");
                Disconnect();
                return false;
            }
        }

        /// <summary>
        /// 수신 루프
        /// </summary>
        private async Task ReceiveLoopAsync()
        {
            byte[] headerBuffer = new byte[4];

            try
            {
                while (!m_cts.Token.IsCancellationRequested && m_isConnected)
                {
                    // 1. 패킷 크기 읽기 (4바이트)
                    int bytesRead = await ReadExactAsync(m_stream, headerBuffer, 0, 4, m_cts.Token);
                    if (bytesRead != 4)
                    {
                        Debug.LogWarning("[NetworkClient] Failed to read packet size");
                        break;
                    }

                    int packetSize = BitConverter.ToInt32(headerBuffer, 0);

                    if (packetSize <= 0 || packetSize > 1024 * 1024) // 1MB 제한
                    {
                        Debug.LogError($"[NetworkClient] Invalid packet size: {packetSize}");
                        break;
                    }

                    // 2. 전체 패킷 읽기
                    byte[] packetBuffer = new byte[packetSize + 4]; // 크기 필드 포함
                    Array.Copy(headerBuffer, 0, packetBuffer, 0, 4);

                    bytesRead = await ReadExactAsync(m_stream, packetBuffer, 4, packetSize, m_cts.Token);
                    if (bytesRead != packetSize)
                    {
                        Debug.LogWarning($"[NetworkClient] Failed to read full packet. Expected: {packetSize}, Got: {bytesRead}");
                        break;
                    }

                    // 3. 프로토콜 역직렬화
                    Protocol protocol = Protocol.Deserialize(packetBuffer);
                    if (protocol != null)
                    {
                        // Unity 메인 스레드에서 이벤트 호출
                        UnityMainThreadDispatcher.Instance.Enqueue(() =>
                        {
                            OnProtocolReceived?.Invoke(protocol);
                        });
                    }
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[NetworkClient] Receive loop cancelled");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkClient] Receive error: {e.Message}");
                UnityMainThreadDispatcher.Instance.Enqueue(() =>
                {
                    OnError?.Invoke($"Receive error: {e.Message}");
                });
            }
            finally
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 정확한 바이트 수만큼 읽기
        /// </summary>
        private async Task<int> ReadExactAsync(NetworkStream stream, byte[] buffer, int offset, int count, CancellationToken ct)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = await stream.ReadAsync(buffer, offset + totalRead, count - totalRead, ct);
                if (read == 0)
                    return totalRead; // 연결 종료

                totalRead += read;
            }
            return totalRead;
        }
    }

    /// <summary>
    /// Unity 메인 스레드 디스패처
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher s_instance;
        private readonly Queue<Action> m_executionQueue = new Queue<Action>();

        public static UnityMainThreadDispatcher Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = FindObjectOfType<UnityMainThreadDispatcher>();
                    if (s_instance == null)
                    {
                        GameObject go = new GameObject("UnityMainThreadDispatcher");
                        s_instance = go.AddComponent<UnityMainThreadDispatcher>();
                        DontDestroyOnLoad(go);
                    }
                }
                return s_instance;
            }
        }

        void Update()
        {
            lock (m_executionQueue)
            {
                while (m_executionQueue.Count > 0)
                {
                    m_executionQueue.Dequeue().Invoke();
                }
            }
        }

        public void Enqueue(Action action)
        {
            lock (m_executionQueue)
            {
                m_executionQueue.Enqueue(action);
            }
        }

        void OnDestroy()
        {
            if (s_instance == this)
                s_instance = null;
        }
    }
