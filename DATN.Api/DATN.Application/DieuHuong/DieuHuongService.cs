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
using System.Threading;
using System.Threading.Tasks;

namespace DATN.Application.DieuHuong
{

    public class DieuHuongService : IDieuHuong
    {
        private readonly AppDbContext _context;
        private readonly Helper _helper;

        public DieuHuongService(AppDbContext context, Helper helper)
        {
            _context = context;
            _helper = helper;
        }

        public async Task<string> DongBoMenu(List<dieu_huong_dto> request)
        {
            //try
            //{
            //    int stt = 1;
            //    int cap = 1;
            //    var commands = await _context.dm_command.AsNoTracking().ToListAsync();
            //    var userName = _helper.GetUserInfo().userName ?? "anonymous";
            //    var menuDb = await _context.dieu_huong.Where(c => c.cap_dieu_huong == cap).ToListAsync();
            //    foreach (var item in request)
            //    {
            //        var mangTen = item.duong_dan?.Split("/") ?? new string[] { };
            //        var dieuHuong = _context.dieu_huong.FirstOrDefault(c => c.ma == mangTen[mangTen.Length - 1]);

            //        //thêm mới điều hướng
            //        if (dieuHuong == null)
            //        {
            //            var id = Guid.NewGuid();
            //            var obj = new dieu_huong()
            //            {
            //                Id = id,
            //                ma = mangTen[mangTen.Length - 1],
            //                ten = item.ten ?? "",
            //                duong_dan = item.duong_dan ?? "",
            //                so_thu_tu = stt,
            //                cap_dieu_huong = cap,
            //                ngay_tao = DateTime.Now,
            //                nguoi_tao = userName,
            //            };
            //            _context.dieu_huong.Add(obj);

            //            //add command
            //            if (item.permission?.Count() > 0)
            //            {
            //                await TaoDieuHuongCommand(id, item.permission, commands);
            //            }
            //            if (item.danh_sach_dieu_huong_con != null)
            //            {
            //                await TaoMenuConAsync(id, item.danh_sach_dieu_huong_con, cap, commands);
            //            }   
            //        }
            //        else
            //        {
            //            // sửa
            //            dieuHuong.duong_dan = item.duong_dan ?? "";
            //            dieuHuong.ma = mangTen[mangTen.Length - 1];
            //            dieuHuong.ten = item.ten ?? "";
            //            dieuHuong.so_thu_tu = stt;
            //            dieuHuong.dieu_huong_cap_tren_id = null;
            //            dieuHuong.cap_dieu_huong = cap;
            //            dieuHuong.ngay_chinh_sua = DateTime.Now;
            //            dieuHuong.nguoi_chinh_sua = userName;
            //            _context.dieu_huong.Update(dieuHuong);

            //            //update command

            //        }
            //        stt++;
            //        menuDb = menuDb.Where(c => c.duong_dan != item.duong_dan).ToList();
            //    }
            //    if (menuDb.Count > 0)
            //    {
            //        foreach (var item in menuDb)
            //        {
            //            await XoaMenuThua(item);
            //        }
            //    }

            //    _context.SaveChanges();
            //    return "Đồng bộ thành công!";
            //}
            //catch (Exception ex) { 
            //    throw new Exception(ex.Message);
            //}

            return null;
        }

        public async Task TaoDieuHuongCommand(Guid id, List<string> commandDieuHuong, List<dm_command> commands)
        {
            var commandsXoa = await _context.dieu_huong_2_command.Where(c => id == c.dieu_huong_id).ToListAsync();
            _context.dieu_huong_2_command.RemoveRange(commandsXoa);
            foreach (var item in commandDieuHuong)
            {
                var command = commands.FirstOrDefault(c => c.command == item);
                if (command != null)
                {
                    var entity = new dieu_huong_2_command()
                    {
                        Id = Guid.NewGuid(),
                        command_id = command?.id,
                        dieu_huong_id = id,
                        command = command?.command
                    };
                    await _context.dieu_huong_2_command.AddAsync(entity);
                }
                else
                {
                    command = new dm_command()
                    {
                        id = Guid.NewGuid(),
                        ten = item,
                        command = item,
                    };
                    await _context.dm_command.AddAsync(command);
                    var entity = new dieu_huong_2_command()
                    {
                        Id = Guid.NewGuid(),
                        command_id = command?.id,
                        dieu_huong_id = id,
                        command = command?.command
                    };
                    await _context.dieu_huong_2_command.AddAsync(entity);
                }
            }
        }

        public async Task XoaMenuThua(dieu_huong menu)
        {
            var menuDb = await _context.dieu_huong.Where(c => c.dieu_huong_cap_tren_id == menu.Id).ToListAsync();
            if (menuDb.Count() > 0)
            {
                foreach (var item in menuDb)
                {
                    await XoaMenuThua(item);
                }
            }
            var commands = await _context.dieu_huong_2_command.Where(c => menu.Id == c.dieu_huong_id).ToListAsync();
            _context.dieu_huong_2_command.RemoveRange(commands);
            var nhomNguoiDungCommmand = await _context.nhom_nguoi_dung_2_command.Where(c => c.dieu_huong_id == menu.Id).ToListAsync();
            _context.nhom_nguoi_dung_2_command.RemoveRange(nhomNguoiDungCommmand);
            var nhomNguoiDungDieuHuong = await _context.nhom_nguoi_dung_2_dieu_huong.Where(c => c.dieu_huong_id == menu.Id).ToListAsync();
            _context.nhom_nguoi_dung_2_dieu_huong.RemoveRange(nhomNguoiDungDieuHuong);
            _context.dieu_huong.Remove(menu);
        }

        public async Task TaoMenuConAsync(Guid idCha, List<dieu_huong_dto> menuCon, int cap, List<dm_command> commands)
        {
            var stt = 1;
            cap += 1;
            if (menuCon.Any())
            {

            }
        }
        public Task<List<dieu_huong_dto>> GetMenu()
        {
            try
            {
                var menu = _context.dieu_huong.AsNoTracking().ToList();
                var menuDto = menu.Where(c => c.cap_dieu_huong == 1).Select(c => new dieu_huong_dto()
                {
                    Id = c.Id,
                    ten = c.ten,
                    ma = c.ma,
                    duong_dan = c.duong_dan,
                    so_thu_tu = c.so_thu_tu,
                    cap_dieu_huong = c.cap_dieu_huong,
                    dieu_huong_cap_tren_id = c.dieu_huong_cap_tren_id,
                    mo_ta = c.mo_ta,
                    permission = _context.dieu_huong_2_command.Where(x=> x.dieu_huong_id == c.Id)?.Select(p => new command_dto
                    {
                        command = p.command,
                        id = p.command_id,
                        ten = p.dm_command.ten
                    }).OrderBy(x => x.ten).ToList() ?? new List<command_dto>(),
                    danh_sach_dieu_huong_con = menu.Where(d => d.dieu_huong_cap_tren_id == c.Id).Select(d => new dieu_huong_dto()
                    {
                        Id = d.Id,
                        ma = d.ma,
                        ten = d.ten,
                        duong_dan = d.duong_dan,
                        so_thu_tu = d.so_thu_tu,
                        dieu_huong_cap_tren_id = d.dieu_huong_cap_tren_id,
                        mo_ta = d.mo_ta,
                        cap_dieu_huong = d.cap_dieu_huong,
                        permission = _context.dieu_huong_2_command.Where(x => x.dieu_huong_id == d.Id)?.Select(p => new command_dto
                        {
                            command = p.command,
                            id = p.command_id,
                            ten = p.dm_command.ten
                        }).OrderBy(x=> x.ten).ToList() ?? new List<command_dto>(),
                    }).OrderBy(d => d.so_thu_tu).ToList(),
                }).OrderBy(c => c.so_thu_tu).ToList();

                return Task.FromResult(menuDto);
            }
            catch (Exception ex) {
                throw new Exception(ex.Message);
            }
        }
    }
}
