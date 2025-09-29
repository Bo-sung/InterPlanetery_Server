using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TestServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            TcpListener server = null;
            try
            {
                int port = 7777;
                IPAddress localAddr = IPAddress.Parse("127.0.0.1");

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine("Server started on port 7777...");

                while (true)
                {
                    Console.WriteLine("Waiting for player 1...");
                    TcpClient player1 = await server.AcceptTcpClientAsync();
                    Console.WriteLine("Player 1 connected! Waiting for player 2...");

                    Console.WriteLine("Waiting for player 2...");
                    TcpClient player2 = await server.AcceptTcpClientAsync();
                    Console.WriteLine("Player 2 connected! Starting game session.");

                    // 두 명의 플레이어가 연결되면 게임 세션 처리를 위한 새 작업을 시작합니다.
                    // 'await'으로 기다리지 않으므로, 메인 루프는 즉시 다음 플레이어들을 기다릴 수 있습니다.
                    _ = HandleGameSessionAsync(player1, player2);
                }
            }
            catch (SocketException e)
            {
                Console.WriteLine($"SocketException: {e}");
            }
            finally
            {
                server?.Stop();
            }
        }

        static async Task HandleGameSessionAsync(TcpClient player1, TcpClient player2)
        {
            try
            {
                // 각 플레이어의 메시지를 다른 플레이어에게 중계하는 두 개의 작업을 생성합니다.
                var relay1to2 = RelayDataAsync(player1, player2, "P1 -> P2");
                var relay2to1 = RelayDataAsync(player2, player1, "P2 -> P1");

                // 두 중계 작업 중 하나라도 끝나면 (예: 한쪽 클라이언트 연결이 끊어지면) 세션을 종료합니다.
                await Task.WhenAny(relay1to2, relay2to1);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Game session error: {e.Message}");
            }
            finally
            {
                Console.WriteLine("Game session ended. Closing connections.");
                player1.Close();
                player2.Close();
            }
        }

        static async Task RelayDataAsync(TcpClient sourceClient, TcpClient destinationClient, string direction)
        {
            NetworkStream sourceStream = sourceClient.GetStream();
            NetworkStream destinationStream = destinationClient.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                // 소스 클라이언트로부터 데이터를 읽어 목적지 클라이언트로 전송합니다.
                while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                {
                    string data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Relaying data ({direction}): {data}");
                    await destinationStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception e)
            {
                // 스트림이 닫혔거나 네트워크 오류 발생 시 예외를 처리합니다.
                Console.WriteLine($"Error during relay ({direction}): {e.Message}");
            }
            finally
            {
                // 연결이 끊어졌음을 알립니다.
                Console.WriteLine($"Client disconnected in relay ({direction}).");
            }
        }
    }
}