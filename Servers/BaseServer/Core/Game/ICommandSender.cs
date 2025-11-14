using CommonLib.Commands;
using System.Threading.Tasks;

namespace BaseServer.Core.Game
{
    public interface ICommandSender
    {
        Task SendCommandToGame(Command command);
    }
}
