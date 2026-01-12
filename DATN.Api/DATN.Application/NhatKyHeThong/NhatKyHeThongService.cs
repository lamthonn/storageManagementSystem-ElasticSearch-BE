using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.NhatKyHeThong
{
    public class NhatKyHeThongService : INhatKyHeThong
    {

        private readonly AppDbContext _context;
        private readonly Helper _helper;
        public NhatKyHeThongService(AppDbContext context, Helper helper)
        {
            _context = context;
            _helper = helper;
        }
        public async Task<PaginatedList<nhat_ky_he_thong_dto>> GetAllLog(nhat_ky_he_thong_pagin_param request)
        {
            try
            {
                var dataQuery = _context.nhat_ky_he_thong.AsNoTracking();
                
                if (request.tai_khoan != null)
                {
                    dataQuery = dataQuery.Where(x => x.tai_khoan.Contains(request.tai_khoan));
                }

                if (request.command != null)
                {
                    dataQuery = dataQuery.Where(x => x.command.Contains(request.command));
                }

                if (request.fromDate != null && request.toDate != null)
                {
                    dataQuery = dataQuery.Where(x => x.TimeStamp >= request.fromDate
                                                  && x.TimeStamp <= request.toDate);
                }

                var dataQueryDto = dataQuery.OrderByDescending(x => x.TimeStamp)
                                                    .Select(x => new nhat_ky_he_thong_dto
                                                    {
                                                       id = x.id,
                                                       detail = x.detail,
                                                       TimeStamp = x.TimeStamp,
                                                       command = x.command,
                                                       tai_khoan = x.tai_khoan,
                                                       command_id = x.command_id,
                                                       dieu_huong_id = x.dieu_huong_id,
                                                       level = x.level,
                                                       loai = x.loai,
                                                       ten = x.ten
                                                    });
                var result = await PaginatedList<nhat_ky_he_thong_dto>.Create(dataQueryDto, request.pageNumber, request.pageSize);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<nhat_ky_he_thong_dto> AddLog(nhat_ky_he_thong_dto request)
        {
            var userName = _helper.GetUserInfo().userName ?? "anonymous";
            var cmd = _context.dm_command.FirstOrDefault(x => x.command == request.command);
            try
            {
                if(request != null)
                {
                    var newLog = new nhat_ky_he_thong
                    {
                        id = Guid.NewGuid(),
                        command = request.command ?? "",
                        command_id = cmd != null ? cmd.id : null,
                        loai = request.loai,
                        level = request.level,
                        tai_khoan = userName ?? "anonymous",
                        TimeStamp = DateTime.Now,
                        detail = request.detail
                    };

                    _context.nhat_ky_he_thong.Add(newLog);
                    _context.SaveChanges();

                    return new nhat_ky_he_thong_dto
                    {
                        id = newLog.id,
                        command = newLog.command ?? "",
                        command_id = newLog.command_id,
                        loai = newLog.loai,
                        level = newLog.level,
                        tai_khoan = newLog.tai_khoan,
                        TimeStamp = newLog.TimeStamp,
                        detail = newLog.detail
                    };
                }
                else
                {
                    throw new Exception("không có request log");
                }
            }
            catch (Exception ex) 
            {
                var newLog = new nhat_ky_he_thong
                {
                    id = Guid.NewGuid(),
                    command = request.command ?? "",
                    command_id = cmd != null ? cmd.id : null,
                    loai = request.loai,
                    level = request.level,
                    tai_khoan = userName,
                    TimeStamp = DateTime.Now,
                    detail = ex.Message
                };

                _context.nhat_ky_he_thong.Add(newLog);
                return new nhat_ky_he_thong_dto
                {
                    id = newLog.id,
                    command = newLog.command ?? "",
                    command_id = newLog.command_id,
                    loai = newLog.loai,
                    level = newLog.level,
                    tai_khoan = newLog.tai_khoan,
                    TimeStamp = newLog.TimeStamp,
                    detail = newLog.detail
                };
            }
        }
    }
}
