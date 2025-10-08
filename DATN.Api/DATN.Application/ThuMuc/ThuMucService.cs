using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.ThuMuc
{
    public class ThuMucService : IThuMucService
    {
        private readonly AppDbContext _context;
        private readonly INhatKyHeThong _logger;
        private readonly Helper _helper;

        public ThuMucService(AppDbContext context, INhatKyHeThong logger, Helper helper) 
        {
            _context = context;
            _logger = logger;
            _helper = helper;
        }
        public async Task<thu_muc_dto> AddThuMuc(thu_muc_dto request)
        {
            try
            {
                var curUser = _helper.GetUserInfo().userName ?? "anonymous";    
                var newTM = new thu_muc
                {
                    id = Guid.NewGuid(),
                    ten = request.ten ?? "",
                    thu_muc_cha_id = request.thu_muc_cha_id,
                    nguoi_dung_id = request.nguoi_dung_id ?? Guid.Empty,
                    ngay_tao = DateTime.Now,
                    nguoi_tao = curUser,
                };

                _context.thu_muc.Add(newTM);
                _context.SaveChanges();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Thêm thư mục {newTM.ten}",
                    command = "PERM_ADD",
                });
                return new thu_muc_dto
                {
                    id = newTM.id,
                    ten = newTM.ten,
                    thu_muc_cha_id = newTM.thu_muc_cha_id
                };
            }
            catch (Exception ex) {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_DETELE",
                });
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> DeleteThuMuc(Guid id)
        {
            try
            {
                var data = _context.thu_muc.FirstOrDefault(x => x.id == id);
                if(data == null)
                {
                    throw new Exception("Không tồn tại thư mục");
                }

                _context.thu_muc.Remove(data);
                _context.SaveChanges();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xóa thư mục {data.ten}",
                    command = "PERM_EDIT",
                });
                return "Xóa thành công thư mục";
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

        public async Task<List<thu_muc_dto>> GetAll(Guid nguoi_dung_id)
        {
            try
            {
                var dsThuMuc =_context.thu_muc.Where(x=> x.nguoi_dung_id == nguoi_dung_id && x.thu_muc_cha_id == null);
                var result = dsThuMuc.Select(x => new thu_muc_dto
                {
                    id = x.id,
                    ten = x.ten,
                    nguoi_dung_id = x.nguoi_dung_id,
                    thu_muc_cha_id = x.thu_muc_cha_id,
                    ngay_tao = x.ngay_tao,
                }).OrderBy(x => x.ngay_tao).ToList();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task<List<thu_muc_dto>> GetManyThuMuc(List<Guid> ids)
        {
            try
            {
                var dsThuMuc = _context.thu_muc.Where(x => ids.Contains(x.id));
                var result = dsThuMuc.Select(x => new thu_muc_dto
                {
                    id = x.id,
                    ten = x.ten,
                    nguoi_dung_id = x.nguoi_dung_id,
                    thu_muc_cha_id = x.thu_muc_cha_id,
                    ngay_tao = x.ngay_tao,
                }).ToList();
                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<thu_muc_dto> UpdateThuMuc(Guid id, thu_muc_dto request)
        {
            try
            {
                var curUser = _helper.GetUserInfo().userName ?? "anonymous";
                var thuMuc = _context.thu_muc.FirstOrDefault(x=> x.id == id);
                if(thuMuc != null)
                {
                    var tenCu = thuMuc.ten;
                    thuMuc.ten = request.ten ?? "";

                    _context.thu_muc.Update(thuMuc);
                    _context.SaveChanges();
                    await _logger.AddLog(new nhat_ky_he_thong_dto
                    {
                        loai = 1,
                        detail = $"Cập nhật tên thư mục {tenCu} thành {request.ten}",
                        command = "PERM_EDIT",
                    });
                    return new thu_muc_dto
                    {
                        id = thuMuc.id,
                        ten = thuMuc.ten,
                        thu_muc_cha_id = thuMuc.thu_muc_cha_id
                    };
                }
                else
                {
                    throw new Exception("Không tìm thấy id thư mục");
                }
            }
            catch (Exception ex)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_EDIT",
                });
                throw new Exception(ex.Message);
            }
        }
    }
}
