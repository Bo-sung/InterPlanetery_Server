using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BaseServer.Core.Game.Managers;
using BaseServer.Core.Game.Session;
using BaseServer.Database;
using Microsoft.Extensions.Configuration;

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

            IConfiguration config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            // CommonLib은 자체적으로 설정을 로드할 것으로 가정합니다.
            // DBManager.Instance.UpdateTable() 호출 시 CommonLib.AppConfig가 사용됩니다.
            DBManager dBManager = DBManager.Instance;
            dBManager.UpdateTable();

            TcpListener server = null;
            try
            {
                string host = config.GetValue<string>("Server:Host");
                int port = config.GetValue<int>("Server:Port");
                
                if (string.IsNullOrEmpty(host))
                {
                    Console.WriteLine("[ERROR] Server:Host is not configured in appsettings.json");
                    return;
                }

                IPAddress[] addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0)
                {
                    Console.WriteLine($"[ERROR] Could not resolve host: {host}");
                    return;
                }
                IPAddress localAddr = addresses[0];

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine($"[Server] Listening on {host}:{port}");
                Console.WriteLine("[Server] Waiting for clients to connect...");
                Console.WriteLine();

                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
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