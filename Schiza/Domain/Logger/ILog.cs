using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiza.Domain.Logger
{
    /// <summary>
    /// Сообщение логгера
    /// </summary>
    public interface ILog
    {
        string Message { get; }
        LogType Type { get; }
        string Sender { get; }
        DateTime Time { get; }
        DateTime Session { get; }
    }
}
