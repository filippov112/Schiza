using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Schiza.Domain.Logger
{
    public interface ILogsRepository
    {
        public void Save(IEnumerable<ILog> log);
        public void Delete(DateTime? from, DateTime? to);
        public List<ILog> Get(DateTime? from, DateTime? to);
        public List<ILog> GetSession(DateTime? from, DateTime? to);
    }
}
