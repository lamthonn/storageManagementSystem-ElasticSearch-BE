
using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.DanhMucCapDo
{
    public class DanhMucCapDoService : IDanhMucCapDo
    {
        private readonly AppDbContext _context;
        private readonly INhatKyHeThong _logger;

        public DanhMucCapDoService(AppDbContext context, INhatKyHeThong logger)
        {
            _context = context;
            _logger = logger;
        }
        public async Task<PaginatedList<danh_muc_dto>> GetAll(danh_muc_dto  request)
        {
            try { 

                var dsDanhMuc = _context.danh_muc.Where(x => x.ma_dinh_danh == "danh-muc-cap-do");

                if(request.keySearch != null)
                {
                    dsDanhMuc = dsDanhMuc.Where(x => x.ten.Contains(request.keySearch) || x.ma.Contains(request.keySearch) || x.mo_ta.Contains(request.keySearch));
                }
                if(request.ngay_tao != null)
                {
                    dsDanhMuc = dsDanhMuc.Where(x => x.ngay_tao == request.ngay_tao.Value.Date);
                }
                if(request.trang_thai != null)
                {
                    dsDanhMuc = dsDanhMuc.Where(x => x.trang_thai == request.trang_thai);
                }

                var dataQueryDto = dsDanhMuc
                    .OrderByDescending(x => x.ngay_tao)
                    .Select(x => new danh_muc_dto
                    {
                        Id = x.Id,
                        ma_dinh_danh = x.ma_dinh_danh,
                        ma = x.ma,
                        ten = x.ten,
                        mo_ta = x.mo_ta,
                        cap_do = x.cap_do,
                        trang_thai = x.trang_thai,
                        ngay_tao = x.ngay_tao,
                        nguoi_tao = x.nguoi_tao,
                        ngay_chinh_sua = x.ngay_chinh_sua,
                        nguoi_chinh_sua = x.nguoi_chinh_sua,
                    });
                var result = await PaginatedList<danh_muc_dto>.Create(dataQueryDto, request.pageNumber, request.pageSize);
                return result;
            }
            catch (Exception ex) { throw new Exception($"Lỗi khi lấy dữ liệu Cấp độ: {ex.Message + ex.StackTrace}"); }}

        public async Task<danh_muc_dto> GetById(Guid id)
        {
            try
            {
                var entity = await _context.danh_muc.FindAsync(id);
                if (entity == null) return null;

                return new danh_muc_dto
                {
                    Id = entity.Id,
                    ma_dinh_danh = entity.ma_dinh_danh,
                    ma = entity.ma,
                    ten = entity.ten,
                    mo_ta = entity.mo_ta,
                    cap_do = entity.cap_do,
                    trang_thai = entity.trang_thai,
                    ngay_tao = entity.ngay_tao,
                    nguoi_tao = entity.nguoi_tao,
                    ngay_chinh_sua = entity.ngay_chinh_sua,
                    nguoi_chinh_sua = entity.nguoi_chinh_sua
                };
            }
            catch (Exception ex) { throw new Exception(ex.Message); }
        }
        
        public async Task<danh_muc_dto> Create(danh_muc_dto obj)
        {
            try
            {
                var exitDanhMuc = _context.danh_muc.Any(x => x.ma == obj.ma && x.ma_dinh_danh == "danh-muc-cap-do");
                if (exitDanhMuc) throw new Exception("Mã danh mục đã tồn tại!");
                var entity = new danh_muc
                {
                    Id = Guid.NewGuid(),
                    ma_dinh_danh = "danh-muc-cap-do",
                    ma = obj.ma,
                    ten = obj.ten,
                    mo_ta = obj.mo_ta,
                    cap_do = obj.cap_do,
                    trang_thai = obj.trang_thai ?? true,
                    ngay_tao = DateTime.Now,
                    nguoi_tao = obj.nguoi_tao
                };

                _context.danh_muc.Add(entity);
                await _context.SaveChangesAsync();

                obj.Id = entity.Id;
                obj.ngay_tao = entity.ngay_tao;
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Thêm mới cấp độ \"{entity.ten}\"",
                    command = "PERM_ADD",
                });
                return obj;
            }
            catch (Exception ex) {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_ADD",
                });
                throw new Exception(ex.Message); 
            }
        }
        public async Task<danh_muc_dto> Update(danh_muc_dto obj)
        {
            try
            {
                var entity = await _context.danh_muc.FindAsync(obj.Id);
                if (entity == null) return null;

                entity.ten = obj.ten;
                entity.ma = obj.ma;
                entity.mo_ta = obj.mo_ta;
                entity.cap_do = obj.cap_do;
                entity.trang_thai = obj.trang_thai ?? true;
                entity.ngay_chinh_sua = DateTime.Now;
                entity.nguoi_chinh_sua = obj.nguoi_chinh_sua;

                _context.danh_muc.Update(entity);
                await _context.SaveChangesAsync();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Sửa thông tin cấp độ \"{entity.ten}\"",
                    command = "PERM_EDIT",
                });
                return obj;
            }
            catch (Exception ex) {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_EDIT",
                });
                throw new Exception(ex.Message); 
            }
        }

        public async Task<bool> Delete(Guid id)
        {
            try
            {
                var entity = await _context.danh_muc.FindAsync(id);
                if (entity == null) return false;

                _context.danh_muc.Remove(entity);
                await _context.SaveChangesAsync();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xóa thông tin cấp độ \"{entity.ten}\"",
                    command = "PERM_DELETE",
                });
                return true;
            }
            catch (Exception ex) {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_DELETE",
                });
                throw new Exception(ex.Message); 
            }
        }

        public async Task<bool> DeleteAny(List<Guid> ids)
        {
            try
            {
                var entities = await _context.danh_muc
                    .Where(x => ids.Contains(x.Id)) // thay Id bằng tên cột khoá chính của bảng danh_muc
                    .ToListAsync();

                if (entities == null || entities.Count == 0)
                    return false;

                _context.danh_muc.RemoveRange(entities);
                await _context.SaveChangesAsync();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xóa nhiều cấp độ ",
                    command = "PERM_DELETE",
                });
                return true;
            }
            catch (Exception ex)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_DELETE",
                });
                throw new Exception(ex.Message);
            }
        }

    }
}
