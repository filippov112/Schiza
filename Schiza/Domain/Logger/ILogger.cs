using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiza.Domain.Logger
{
    /// <summary>
    /// Сервис логирования событий системы
    /// </summary>
    public interface ILogger
    {
        void Log(string message, string type, string sender);
        List<ILog> GetLogs(int last_count);
        List<ILog> GetLogs(DateTime from_time, DateTime? session_time = null);
    }
}
