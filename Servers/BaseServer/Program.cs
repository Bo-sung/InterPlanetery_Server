using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommonLib;

namespace BaseServer
{
	class Program
	{
		static async Task Main(string[] _args)
		{
			Console.WriteLine("========================================");
			Console.WriteLine("    Base Server Starting...");
			Console.WriteLine("========================================");
			Console.WriteLine();

			TcpListener server = null;
			try
			{
				int port = 7777;
				IPAddress localAddr = IPAddress.Parse("127.0.0.1");

				server = new TcpListener(localAddr, port);
				server.Start();

				Console.WriteLine($"[Server] Listening on port {port}");
				Console.WriteLine("[Server] Waiting for clients to connect...");
				Console.WriteLine();

				while (true)
				{
					// 클라이언트 연결 대기
					TcpClient client = await server.AcceptTcpClientAsync();

					// 새 세션 생성 및 시작 (비동기로 처리)
					ClientSession session = new ClientSession(client);
					_ = Task.Run(async () => await session.StartAsync());
				}
			}
			catch (SocketException e)
			{
				Console.WriteLine($"[Server] SocketException: {e}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"[Server] Exception: {e}");
			}
			finally
			{
				server?.Stop();
				RoomManager.Instance.Shutdown();
				Console.WriteLine("[Server] Shutdown complete");
			}
		}
	}
}