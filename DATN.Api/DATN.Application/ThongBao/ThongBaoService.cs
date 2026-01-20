using DATN.Application.Interfaces;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.ThongBao
{
    public class ThongBaoService : IThongBaoService
    {

        private readonly AppDbContext _context;
        public ThongBaoService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<thong_bao_respone_dto>> GetThongBaosByUser(Guid userId)
        {
            try
            {
                var dataUser = await _context.nguoi_dung.FirstOrDefaultAsync(x => x.Id == userId);
                var allData = await _context.thong_bao.Where(x => x.nguoi_nhan == (dataUser != null ? dataUser.tai_khoan : "")).ToListAsync();

                var result = allData.Select(x => new thong_bao_respone_dto
                {
                    id = x.id,
                    title = $"Từ {x.nguoi_gui}",
                    content = x.noi_dung,
                    createdAt = x.ngay_gui,
                    isRead = x.da_xem,
                }).ToList();

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> GuiThongBao(thong_bao_request thongBaoDto)
        {
            try
            {
                if (thongBaoDto.nguoi_gui == null)
                {
                    return "Không tìm thấy thông tin người gửi!";
                }
                
                if (thongBaoDto.nguoi_nhan == null)
                {
                    return "Không tìm thấy thông tin người nhận!";
                }

                if (thongBaoDto.tai_lieu_id == Guid.Empty)
                {
                    return "Không tìm thấy tài liệu!";
                }
                var newNotification = new thong_bao
                {
                    id = Guid.NewGuid(),
                    da_xem = false,
                    ngay_gui = DateTime.Now,
                    tai_lieu_id = thongBaoDto.tai_lieu_id,
                    noi_dung = thongBaoDto.noi_dung,
                    nguoi_gui = thongBaoDto.nguoi_gui,
                    nguoi_nhan = thongBaoDto.nguoi_nhan,
                    tieu_de = $"Từ {thongBaoDto.nguoi_gui}",
                    ngay_tao = DateTime.Now,
                    nguoi_tao = thongBaoDto.nguoi_gui
                };

                await _context.thong_bao.AddAsync(newNotification);
                await _context.SaveChangesAsync(new CancellationToken());

                return "Gửi thông báo thành công";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task<string> CapNhatTrangThai(Guid thong_bao_id)
        {
            throw new NotImplementedException();
        }

        public Task<string> HandleOkShareFile(respone_thong_bao_dto res)
        {
            try
            {
                var thongBao = _context.thong_bao.FirstOrDefault(x => x.id == res.thong_bao_id);

                if(thongBao != null)
                {
                    var nguoiGui = _context.nguoi_dung.FirstOrDefault(x => x.tai_khoan == thongBao.nguoi_gui);
                    if (nguoiGui != null)
                    {
                        var newFileToShare = new tai_lieu_2_nguoi_dung
                        {
                            Id = Guid.NewGuid(),
                            nguoi_dung_id = nguoiGui.Id,
                            tai_lieu_id = thongBao.tai_lieu_id ?? Guid.Empty,
                            isAccess = true
                        };

                        _context.tai_lieu_2_nguoi_dung.Add(newFileToShare);
                    }
                    thongBao.da_xem = true;
                    _context.thong_bao.Update(thongBao);

                    _context.SaveChanges();
                    return Task.FromResult("Chia sẻ tài liệu thành công");
                }
                else
                {
                    return Task.FromResult("Không tìm thấy người nhận");
                }

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task<string> HandleCancelShareFile(respone_thong_bao_dto res)
        {
            try
            {
                var thongBao = _context.thong_bao.FirstOrDefault(x => x.id == res.thong_bao_id);
                if (thongBao != null)
                {
                    thongBao.da_xem = true;
                    _context.thong_bao.Update(thongBao);
                }

                _context.SaveChanges();
                return Task.FromResult("Từ chối chia sẻ");
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
