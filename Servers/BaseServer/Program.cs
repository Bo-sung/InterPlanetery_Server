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
                
                IPAddress localAddr;
                
                // 1. IP 주소 문자열인지 먼저 확인 (0.0.0.0, 127.0.0.1 등)
                if (IPAddress.TryParse(host, out IPAddress parsedIp))
                {
                    // 0.0.0.0인 경우 IPAddress.Any로 변환 (명시적 처리)
                    if (parsedIp.Equals(IPAddress.Parse("0.0.0.0")))
                    {
                        localAddr = IPAddress.Any;
                    }
                    else
                    {
                        localAddr = parsedIp;
                    }
                }
                else
                {
                    // 2. 호스트명인 경우 DNS 조회 (localhost 등)
                    IPAddress[] addresses = Dns.GetHostAddresses(host);
                    if (addresses.Length == 0)
                    {
                        Console.WriteLine($"[ERROR] Could not resolve host: {host}");
                        return;
                    }
                    localAddr = addresses[0];
                }

                server = new TcpListener(localAddr, port);
                server.Start();

                Console.WriteLine($"[Server] Listening on {server.LocalEndpoint}");
                
                // 루프백 주소로 바인딩된 경우 경고 출력
                if (IPAddress.IsLoopback(((IPEndPoint)server.LocalEndpoint).Address))
                {
                    Console.WriteLine("[WARNING] Server is bound to loopback address (127.0.0.1). External connections will fail.");
                    Console.WriteLine("[WARNING] Please check 'appsettings.json' and set 'Server:Host' to '0.0.0.0'.");
                }

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