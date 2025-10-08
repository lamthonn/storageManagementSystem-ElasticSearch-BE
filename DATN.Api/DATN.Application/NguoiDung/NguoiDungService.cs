using backend_v3.Dto.Common;
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

namespace DATN.Application.NguoiDung
{
    public class NguoiDungService : INguoiDung
    {
        private readonly AppDbContext _context;
        private readonly Helper _helper;
        private readonly INhatKyHeThong _logger;


        public NguoiDungService(AppDbContext context, Helper helper, INhatKyHeThong logger)
        {
            _context = context;
            _helper = helper;
            _logger = logger;
        }

        public Task<List<nguoi_dung_dto>> GetAllNguoiDung(string? keySearch)
        {
            try
            {
                var dsNguoiDung = _context.nguoi_dung.AsNoTracking();

                if (keySearch != null)
                {
                    dsNguoiDung = dsNguoiDung.Where(x => x.ten.Contains(keySearch));
                }

                var result = dsNguoiDung.Select(x => new nguoi_dung_dto
                {
                    id = x.Id,
                    tai_khoan = x.tai_khoan,
                    ten = x.ten,
                    ngay_sinh = x.ngay_sinh,
                    email = x.email,
                    so_dien_thoai = x.so_dien_thoai,
                    gioi_tinh = x.gioi_tinh,
                    trang_thai = x.trang_thai,
                }).ToListAsync();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PaginatedList<nguoi_dung_dto>> GetPaginNguoiDung(nguoiDungPaginParams request)
        {
            try
            {
                var userGroups = _context.nguoi_dung.AsNoTracking();
                if (request.keySearch != null)
                {
                    userGroups = userGroups.Where(x => x.ten.Contains(request.keySearch) || x.tai_khoan.Contains(request.keySearch));
                }
                if (request.trang_thai != null)
                {
                    userGroups = userGroups.Where(x => x.trang_thai == request.trang_thai);
                }

                var lstPhongban = _context.danh_muc.Where(x => x.ma_dinh_danh == "danh-muc-phong-ban").Select( x=> new danh_muc_dto
                {
                    Id = x.Id,
                    ma_dinh_danh = x.ma_dinh_danh,
                    ma = x.ma,
                    ten = x.ten,
                });
                var lstND2DM = _context.nguoi_dung_2_danh_muc.AsNoTracking();
                var dataQueryDto = userGroups
                                    .OrderByDescending(x => x.ngay_tao)
                                    .Select(x => new nguoi_dung_dto
                                    {
                                        id = x.Id,
                                        tai_khoan = x.tai_khoan,
                                        ten = x.ten,
                                        gioi_tinh = x.gioi_tinh,
                                        ngay_sinh = x.ngay_sinh,
                                        email = x.email,
                                        so_dien_thoai = x.so_dien_thoai,
                                        trang_thai = x.trang_thai,
                                        ngay_tao = x.ngay_tao,
                                        nguoi_tao = x.nguoi_tao,
                                        ngay_chinh_sua = x.ngay_chinh_sua,
                                        nguoi_chinh_sua = x.nguoi_chinh_sua,
                                        ds_phong_ban = lstPhongban.Where(pb => lstND2DM.Where(nd2dm => nd2dm.nguoi_dung_id == x.Id).Select(a => a.danh_muc_id).Contains(pb.Id)).ToList(),
                                    });
                var result = await PaginatedList<nguoi_dung_dto>.Create(dataQueryDto, request.pageNumber, request.pageSize);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<nguoi_dung_dto> GetNguoiDungyId(Guid id)
        {
            try
            {
                var nguoiDung = await _context.nguoi_dung.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                if (nguoiDung == null) throw new Exception("Người dùng không tồn tại");

                var dsPhongBan = from pb in _context.danh_muc
                                 join ndpb in _context.nguoi_dung_2_danh_muc on pb.Id equals ndpb.danh_muc_id
                                 where ndpb.nguoi_dung_id == id
                                 select new nguoi_dung_2_danh_muc_dto
                                 {
                                     Id = pb.Id,
                                     nguoi_dung_id = id,
                                     danh_muc_id = pb.Id,
                                 };

                var dsPhongBanDto = _context.danh_muc.Where(x => dsPhongBan.Select(dm => dm.danh_muc_id).Contains(x.Id)).Select(x => x.Id);
                var dsNND = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == id).Select(x=> new nguoi_dung_2_nhom_nguoi_dung_dto
                {
                    id = x.id,
                    nguoi_dung_id = x.nguoi_dung_id,
                    nhom_nguoi_dung_id = x.nhom_nguoi_dung_id,
                    mac_dinh = x.mac_dinh,
                    ten = x.nhom_nguoi_dung.ten,
                    ma = x.nhom_nguoi_dung.ma,
                });
                var result = new nguoi_dung_dto
                {
                    id = id,
                    tai_khoan = nguoiDung.tai_khoan,
                    ten = nguoiDung.ten,
                    ngay_sinh = nguoiDung.ngay_sinh,
                    email = nguoiDung.email,
                    so_dien_thoai = nguoiDung.so_dien_thoai,
                    gioi_tinh = nguoiDung.gioi_tinh,
                    trang_thai = nguoiDung.trang_thai,
                    dsPhongBan = dsPhongBan.ToList(),
                    ds_id_phong_ban = dsPhongBanDto.ToList(),
                     ds_nhom_nguoi_dung = dsNND.ToList()
                };

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<nguoi_dung_dto>> GetNguoiDungyIds(List<Guid> ids)
        {
            try
            {
                var result = new List<nguoi_dung_dto>();

                foreach (var id in ids)
                {
                    var nguoiDung = await _context.nguoi_dung.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
                    if (nguoiDung == null) throw new Exception("Người dùng không tồn tại");

                    var dsPhongBan = from pb in _context.danh_muc
                                     join ndpb in _context.nguoi_dung_2_danh_muc on pb.Id equals ndpb.danh_muc_id
                                     where ndpb.nguoi_dung_id == id
                                     select new nguoi_dung_2_danh_muc_dto
                                     {
                                         Id = pb.Id,
                                         danh_muc_id = pb.Id,
                                         nguoi_dung_id = id
                                     };

                    result.Add(new nguoi_dung_dto
                    {
                        id = id,
                        tai_khoan = nguoiDung.tai_khoan,
                        ten = nguoiDung.ten,
                        ngay_sinh = nguoiDung.ngay_sinh,
                        email = nguoiDung.email,
                        so_dien_thoai = nguoiDung.so_dien_thoai,
                        gioi_tinh = nguoiDung.gioi_tinh,
                        trang_thai = nguoiDung.trang_thai,
                        dsPhongBan = dsPhongBan.ToList()
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<nguoi_dung_dto> AddNguoiDung(nguoi_dung_dto request)
        {
            try
            {
                var isdulicate = _context.nguoi_dung.Any((x) => x.tai_khoan == request.mat_khau);
                if(isdulicate == true)
                {
                    throw new Exception("Tài khoản đã tồn tại, vui lòng nhập tài khoản khác");
                }

                // Tạo Salt ngẫu nhiên cho mật khẩu
                byte[] salt = _helper.GenerateSalt();

                // Mã hóa mật khẩu sử dụng PBKDF2 và salt
                string hashPassword = _helper.GetPBKDF2("12345678", salt);

                var newUser = new nguoi_dung
                {
                    Id = Guid.NewGuid(),
                    tai_khoan = request.tai_khoan,
                    mat_khau = hashPassword,
                    salt_code = Convert.ToBase64String(salt), // Lưu Salt vào CSDL
                    ten = request.ten,
                    ngay_sinh = request.ngay_sinh,
                    gioi_tinh = request.gioi_tinh ?? true,
                    email = request.email,
                    trang_thai = request.trang_thai ?? true,
                    so_dien_thoai = request.so_dien_thoai,
                    ngay_tao = DateTime.Now,
                    nguoi_tao = request.tai_khoan,
                };

                var newND2NND = new List<nguoi_dung_2_nhom_nguoi_dung>();
                if (request.ds_nhom_nguoi_dung != null && request.ds_nhom_nguoi_dung.Count > 0)
                {
                    foreach (var nnd in request.ds_nhom_nguoi_dung)
                    {
                        newND2NND.Add(new nguoi_dung_2_nhom_nguoi_dung
                        {
                            id = Guid.NewGuid(),
                            nguoi_dung_id = newUser.Id,
                            nhom_nguoi_dung_id = nnd.nhom_nguoi_dung_id ?? Guid.Empty,
                            mac_dinh = nnd.mac_dinh,
                        });
                    }
                }

                var newND2DM = new List<nguoi_dung_2_danh_muc>();
                if (request.dsPhongBan != null && request.dsPhongBan.Count > 0)
                {
                    foreach (var dm in request.dsPhongBan)
                    {
                        newND2DM.Add(new nguoi_dung_2_danh_muc
                        {
                            Id = Guid.NewGuid(),
                            nguoi_dung_id = newUser.Id,
                            danh_muc_id = dm.danh_muc_id ?? Guid.Empty,
                        });
                    }
                }

                _context.nguoi_dung.Add(newUser);
                _context.SaveChanges();

                _context.nguoi_dung_2_nhom_nguoi_dung.AddRange(newND2NND);
                _context.nguoi_dung_2_danh_muc.AddRange(newND2DM);
                _context.SaveChanges();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Thêm mới người dùng ${newUser.tai_khoan}",
                    command = "PERM_ADD",
                });
                return new nguoi_dung_dto
                {
                    id = newUser.Id,
                    tai_khoan = newUser.tai_khoan,
                    ten = newUser.ten,
                    ngay_sinh = newUser.ngay_sinh,
                    email = newUser.email,
                    gioi_tinh = newUser.gioi_tinh,
                    so_dien_thoai = newUser.so_dien_thoai,
                };

            }
            catch (Exception ex)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_ADD",
                });
                throw new Exception (ex.Message);
            }
        }

        public async Task<string> UpdateNguoiDung(Guid id, nguoi_dung_dto request)
        {
            try
            {
                var user = _context.nguoi_dung.FirstOrDefault(x => x.Id == id);
                if (user == null)
                {
                    throw new Exception("Không tìm thấy thông tin người dùng");
                }

                user.ten = request.ten ?? "";
                user.email = request.email ?? "";
                user.so_dien_thoai = request.so_dien_thoai ?? "";
                user.gioi_tinh = request.gioi_tinh ?? user.gioi_tinh;
                user.ngay_sinh = request.ngay_sinh ?? user.ngay_sinh;

                var ND2NNDOld = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == id);
                if (request.ds_nhom_nguoi_dung != null && request.ds_nhom_nguoi_dung.Count > 0)
                {
                    var addRecord = new List<nguoi_dung_2_nhom_nguoi_dung>();
                    var deleteRecord = new List<nguoi_dung_2_nhom_nguoi_dung>();
                    foreach (var nnd in request.ds_nhom_nguoi_dung)
                    {
                        //db không có, requets có => thêm
                        if (!ND2NNDOld.Any(x => x.nhom_nguoi_dung_id == nnd.nhom_nguoi_dung_id && x.nguoi_dung_id == nnd.nguoi_dung_id))
                        {
                            addRecord.Add(new nguoi_dung_2_nhom_nguoi_dung
                            {
                                id = Guid.NewGuid(),
                                nhom_nguoi_dung_id = nnd.nhom_nguoi_dung_id ?? Guid.Empty,
                                nguoi_dung_id = nnd.nguoi_dung_id ?? Guid.Empty,
                            });
                        }
                    }

                    // Xử lý xóa record
                    foreach (var old in ND2NNDOld)
                    {
                        // db có, request không có => xóa
                        if (!request.ds_nhom_nguoi_dung.Any(x => x.nhom_nguoi_dung_id == old.nhom_nguoi_dung_id && x.nguoi_dung_id == old.nguoi_dung_id))
                        {
                            deleteRecord.Add(old);
                        }
                    }

                    _context.nguoi_dung_2_nhom_nguoi_dung.AddRange(addRecord);
                    _context.nguoi_dung_2_nhom_nguoi_dung.RemoveRange(deleteRecord);
                }

                //cập nhật phòng ban
                var ND2DM = _context.nguoi_dung_2_danh_muc.Where(x => x.nguoi_dung_id == id).ToList();
                if (request.ds_phong_ban != null && request.ds_phong_ban.Count > 0)
                {
                    var addRecord = new List<nguoi_dung_2_danh_muc>();
                    var deleteRecord = new List<nguoi_dung_2_danh_muc>();
                    foreach (var dm in request.ds_phong_ban)
                    {
                        //db không có, requets có => thêm
                        if (!ND2DM.Any(x => x.danh_muc_id == dm.Id && x.nguoi_dung_id == id))
                        {
                            addRecord.Add(new nguoi_dung_2_danh_muc
                            {
                                Id = Guid.NewGuid(),
                                danh_muc_id = dm.Id,
                                nguoi_dung_id = id,
                            });
                        }
                    }

                    // Xử lý xóa record
                    foreach (var old in ND2DM)
                    {
                        // db có, request không có => xóa
                        if (!request.ds_phong_ban.Any(x => x.Id == old.danh_muc_id && old.nguoi_dung_id == id))
                        {
                            deleteRecord.Add(old);
                        }
                    }

                    _context.nguoi_dung_2_danh_muc.AddRange(addRecord);
                    _context.nguoi_dung_2_danh_muc.RemoveRange(deleteRecord);
                }
                _context.SaveChanges();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Sửa thông tin người dùng ${user.ten}",
                    command = "PERM_EDIT",
                });
                return "Cập nhật thông tin người dùng thành công";
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

        public async Task<string> DeleteNguoiDung(Guid id)
        {
            try
            {
                //xóa phân quyền
                var dataPQ = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == id).AsNoTracking();
                _context.nguoi_dung_2_nhom_nguoi_dung.RemoveRange(dataPQ);
                //xóa phòng ban
                var dataPB = _context.nguoi_dung_2_danh_muc.Where(x => x.nguoi_dung_id == id).AsNoTracking();
                _context.nguoi_dung_2_danh_muc.RemoveRange(dataPB);
                //xóa người dùng
                var record = _context.nguoi_dung.FirstOrDefault(x=> x.Id == id);
                if(record != null)
                {
                    _context.nguoi_dung.Remove(record);
                }
                else
                {
                    throw new Exception("Không tìm thấy thông tin người dùng");
                }

                _context.SaveChanges();
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xóa tài khoản ${record.tai_khoan}",
                    command = "PERM_DELETE",
                });
                return ("Xóa người dùng thành công");
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

        public async Task<string> DeleteManyNguoiDung(List<Guid> ids)
        {
            try
            {
                foreach(var id in ids)
                {
                    //xóa phân quyền
                    var dataPQ = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == id).AsNoTracking();
                    _context.nguoi_dung_2_nhom_nguoi_dung.RemoveRange(dataPQ);
                    //xóa phòng ban
                    var dataPB = _context.nguoi_dung_2_danh_muc.Where(x => x.nguoi_dung_id == id).AsNoTracking();
                    _context.nguoi_dung_2_danh_muc.RemoveRange(dataPB);
                    //xóa người dùng
                    var record = _context.nguoi_dung.FirstOrDefault(x => x.Id == id);
                    if (record != null)
                    {
                        _context.nguoi_dung.Remove(record);
                    }
                    else
                    {
                        throw new Exception("Không tìm thấy thông tin người dùng");
                    }

                    _context.SaveChanges();
                }
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xóa nhiều tài khoản",
                    command = "PERM_DELETE",
                });
                return ("Xóa người dùng thành công");
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

        public async Task<List<menu_dto>> GetMenuByNguoiDung(Guid nguoi_dung_id)
        {
            try
            {
                var NND2NND = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == nguoi_dung_id);
                var NNDMacDinh = NND2NND.Any(x => x.mac_dinh == true) ? NND2NND.FirstOrDefault(x => x.mac_dinh == true) : NND2NND.First();
                if (NNDMacDinh != null)
                {
                    var dsMenu2NguoiDung = _context.nhom_nguoi_dung_2_command
                        .Where(x => x.nhom_nguoi_dung_id == NNDMacDinh.nhom_nguoi_dung_id)
                        .GroupBy(m => new { m.nhom_nguoi_dung_id, m.dieu_huong_id })
                        .Select(g => new
                        {
                            g.Key.nhom_nguoi_dung_id,
                            g.Key.dieu_huong_id,
                            CommandIds = g.Select(x=> x.command_id).ToList(),
                            Commands = g.Select(x=> x.command).ToList(),
                        })
                        .ToList();

                    // Lấy thông tin tất cả điều hướng
                    var allMenus = _context.dieu_huong.ToList();

                    // Join 2 bảng
                    var menuWithPermission = (from menu in allMenus
                                              join perm in dsMenu2NguoiDung
                                                  on menu.Id equals perm.dieu_huong_id
                                              select new
                                              {
                                                  menu.Id,
                                                  menu.ten,
                                                  menu.ma,
                                                  menu.mo_ta,
                                                  menu.duong_dan,
                                                  menu.cap_dieu_huong,
                                                  menu.dieu_huong_cap_tren_id,
                                                  menu.so_thu_tu,
                                                  Commands = perm.Commands,
                                                  CommandIds = perm.CommandIds
                                              }).ToList();

                    var result = menuWithPermission.Where(x => x.cap_dieu_huong == 1).Select(x => new menu_dto
                    {
                        key = x.duong_dan.ToString(),
                        label = x.ten,
                        ma_dinh_danh = x.ma.ToString(),
                        Permissions = x.Commands.Any() ? x.Commands : new List<string>(),
                        children = menuWithPermission.Where(child => child.dieu_huong_cap_tren_id == x.Id).Select(childX => new menu_dto
                        {
                            key = childX.duong_dan.ToString(),
                            label = childX.ten,
                            ma_dinh_danh = childX.ma.ToString(),
                            Permissions = childX.Commands.Any() ? childX.Commands : new List<string>(),
                        }).ToList()
                    }).ToList();

                    return result;
                }
                else
                {
                    return new List<menu_dto>();
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task<List<string>> GetPermByNguoiDung(Guid nguoi_dung_id, string ma_dinh_danh)
        {
            try
            {
                var NND2NND = _context.nguoi_dung_2_nhom_nguoi_dung.Where(x => x.nguoi_dung_id == nguoi_dung_id);
                var NNDMacDinh = NND2NND.Any(x => x.mac_dinh == true) ? NND2NND.FirstOrDefault(x => x.mac_dinh == true) : NND2NND.First();
                if (NNDMacDinh != null)
                {
                    var dataQuery = (from nnd2cmd in _context.nhom_nguoi_dung_2_command
                                     join dh in _context.dieu_huong
                                     on nnd2cmd.dieu_huong_id equals dh.Id
                                     where nnd2cmd.nhom_nguoi_dung_id == NNDMacDinh.nhom_nguoi_dung_id && dh.ma == ma_dinh_danh
                                     select nnd2cmd.command).ToList();


                    //var perms = _context.dieu_huong.Where(x => dataQuery.Contains(x.Id)).Select(x=> x.).ToList();
                    return Task.FromResult(dataQuery);
                }

                return Task.FromResult(new List<string>());
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<nguoi_dung_dto>> GetAllNguoiDungByPhongBan(nguoiDungPaginParams? request)
        {
            try
            {
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
