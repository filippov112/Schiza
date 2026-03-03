using Schiza.Domain.Logger;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiza.Services.Logger
{
    /// <summary>
    /// Сообщение
    /// </summary>
    /// <param name="type">Info,Warning,Error,Debug</param>
    /// <param name="message">Текст сообщения</param>
    /// <param name="sender">Отправитель</param>
    public class LogMessage(string type, string message, string sender, DateTime session_time): ILog
    {
        public LogType Type { get; set; } = type switch
        {
            "Warning" => LogType.Warning,
            "Error" => LogType.Error,
            "Debug" => LogType.Debug,
            _ => LogType.Info,
        };
        public string Message { get; set; } = message;
        public string Sender { get; set; } = sender;
        public DateTime Time { get; set; } = DateTime.Now;
        public DateTime Session { get; set; } = session_time;
    }
}
