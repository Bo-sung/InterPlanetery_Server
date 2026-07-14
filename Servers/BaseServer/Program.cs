using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using BaseServer.Core.Game.Managers;
using BaseServer.Core.Game.Session;
using BaseServer.Database;
using BaseServer.Utils;
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
                // "Server__Host"/"Server__Port" env vars (double-underscore = ':') override
                // appsettings.json when set — the compatibility alias documented in docker-compose.yml.
                string host = Environment.GetEnvironmentVariable("Server__Host") ?? config.GetValue<string>("Server:Host");
                string? portEnv = Environment.GetEnvironmentVariable("Server__Port");
                int port = (portEnv != null && int.TryParse(portEnv, out int envPort))
                    ? envPort
                    : config.GetValue<int>("Server:Port");

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
                        Logger.Log($"[ERROR] Could not resolve host: {host}");
                        return;
                    }
                    localAddr = addresses[0];
                }

                server = new TcpListener(localAddr, port);
                server.Start();

                Logger.Log($"[Server] Listening on {server.LocalEndpoint}");

                // 루프백 주소로 바인딩된 경우 경고 출력
                if (IPAddress.IsLoopback(((IPEndPoint)server.LocalEndpoint).Address))
                {
                    Logger.Log("[WARNING] Server is bound to loopback address (127.0.0.1). External connections will fail.");
                    Logger.Log("[WARNING] Please check 'appsettings.json' and set 'Server:Host' to '0.0.0.0'.");
                }

                Logger.Log("[Server] Waiting for clients to connect...");
                Logger.Log("");

                while (true)
                {
                    TcpClient client = await server.AcceptTcpClientAsync();
                    ClientSession session = new ClientSession(client);
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await session.StartAsync();
                        }
                        catch (Exception e)
                        {
                            Logger.Log($"[Session] Unhandled exception: {e}");
                        }
                    });
                }
            }
            catch (SocketException e)
            {
                Logger.Log($"[Server] SocketException: {e}");
            }
            catch (Exception e)
            {
                Logger.Log($"[Server] Exception: {e}");
            }
            finally
            {
                server?.Stop();
                RoomManager.Instance.Shutdown();
                Logger.Log("[Server] Shutdown complete");
            }
        }

    }
}