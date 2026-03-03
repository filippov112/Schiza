using Schiza.Domain.Logger;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Schiza.Services.Logger
{
    /// <summary>
    /// Сервис логгирования
    /// </summary>
    public class LogService<T>: ILogger
        where T : ILog
    {
        private readonly ILogsRepository _logsRepository;
        private readonly object _lock = new();
        private readonly int _batch_size;
        private readonly int _cash_size;
        private readonly ObservableCollection<ILog> log_cash;
        public DateTime SessionTime { get; } // Идентификатор сессии


        public LogService(ILogsRepository logsRepository, int batch_size, int cash_size)
        {
            SessionTime = DateTime.Now;
            _logsRepository = logsRepository;
            _cash_size = Math.Max(1, Math.Max(cash_size, batch_size));
            _batch_size = Math.Max(1, batch_size);
        }


        /// <summary>
        /// Логгировать сообщение
        /// </summary>
        /// <param name="message">Текст сообщения</param>
        /// <param name="sender">Отправитель</param>
        /// <param name="type">Тип (Info, Warning, Error, Debug)</param>
        public void Log(string message, string sender = "Unknown", string type = "Info")
        {
            var m = new LogMessage(type, message, sender, SessionTime);
            lock (_lock)
            {
                log_cash.Add(m);
            }
        }

        public IEnumerable<LogMessage> GetRecentMessages()
        {
            lock (_lock)
            {
                return _recentMessages.ToList();
            }
        }

        public void ClearCurrentSessionLogs()
        {
            lock (_lock)
            {
                _logMessages.Clear();
                _recentMessages.Clear();
            }
        }

        public void ClearAllLogsFromDatabase()
        {
            if (_dbContext != null)
            {
                try
                {
                    _dbContext.LogMessages.RemoveRange(_dbContext.LogMessages);
                    _dbContext.SaveChanges();
                }
                catch
                {
                    // Логируем ошибку очистки БД
                }
            }
        }

        public List<LogMessage> GetLogsFromDatabase(DateTime? startTime = null, DateTime? endTime = null)
        {
            if (_dbContext == null)
            {
                return new List<LogMessage>();
            }

            try
            {
                var query = _dbContext.LogMessages.AsQueryable();

                if (startTime.HasValue)
                {
                    query = query.Where(l => l.Timestamp >= startTime.Value);
                }

                if (endTime.HasValue)
                {
                    query = query.Where(l => l.Timestamp <= endTime.Value);
                }

                var entities = query.ToList();

                return entities.Select(e => new LogMessage(
                    Enum.Parse<LogMessageType>(e.MessageType),
                    e.Message,
                    e.Sender)
                {
                    Timestamp = e.Timestamp,
                    SessionId = e.SessionId
                }).ToList();
            }
            catch
            {
                return new List<LogMessage>();
            }
        }

        public List<ILog> GetLogs(int last_count)
        {
            throw new NotImplementedException();
        }

        public List<ILog> GetLogs(DateTime from_time, DateTime? session_time = null)
        {
            throw new NotImplementedException();
        }
    }


}
