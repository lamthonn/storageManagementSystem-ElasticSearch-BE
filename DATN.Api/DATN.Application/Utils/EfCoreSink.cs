using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Core;
using Serilog.Events;
using DATN.Infrastructure.Data;
using DATN.Domain.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace DATN.Application.Utils
{
    public class EfCoreSink : ILogEventSink
    {
        private readonly IServiceProvider _serviceProvider;
        public EfCoreSink(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                logEvent.Properties.TryGetValue("command_id", out var commandIdValue);
                logEvent.Properties.TryGetValue("dieu_huong_id", out var dieuHuongIdValue);
                logEvent.Properties.TryGetValue("loai", out var loai);

                var log = new nhat_ky_he_thong
                {
                    id = Guid.NewGuid(),
                    command = logEvent.MessageTemplate.Text,
                    tai_khoan = logEvent.Properties.ContainsKey("User")
                                    ? logEvent.Properties["User"].ToString().Trim('"')
                                    : null,
                    level = MapLogLevel(logEvent.Level),
                    TimeStamp = logEvent.Timestamp.UtcDateTime,
                    detail = logEvent.RenderMessage(),
                    command_id = commandIdValue != null ? Guid.Parse(commandIdValue.ToString().Trim('"')) : null,
                    dieu_huong_id = dieuHuongIdValue != null ? Guid.Parse(dieuHuongIdValue.ToString().Trim('"')) : null,
                    loai = (logEvent.Properties.ContainsKey("loai") ? (logEvent.Properties["loai"] as ScalarValue)?.Value as int? : null)
                };

                db.nhat_ky_he_thong.Add(log);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing log to MySQL: " + ex.Message);
            }
        }
        private int MapLogLevel(LogEventLevel level)
        {
            return level switch
            {
                LogEventLevel.Information => 1, // info
                LogEventLevel.Warning => 2,     // warning
                LogEventLevel.Error => 3,       // error
                LogEventLevel.Debug => 4,       // debug
                _ => 0
            };
        }
    }
}


// sử dụng trong controller
// Log.ForContext("command_id", cmdId)
//   .ForContext("dieu_huong_id", dhId)
//   .ForContext("loai", loai)
//   .Information("Thực hiện xử lý dữ liệu");
