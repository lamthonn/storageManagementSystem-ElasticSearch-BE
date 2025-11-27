using DATN.Application.Interfaces;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.CauHinhFile
{
    public class CauhinhFileService : ICauHinhFile
    {
        private readonly AppDbContext _context;
        public CauhinhFileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> editConfig(List<cau_hinh_file_dto> dsFE)
        {
            try
            {
                // Lấy dữ liệu cấu hình hiện tại trong DB theo file_type
                var dsDB = await _context.cau_hinh_file.ToListAsync();
                if(dsDB != null && dsDB.Count > 0)
                {
                    var sizeDb = dsDB.FirstOrDefault()!.file_size;
                    var sizeFe = dsFE.FirstOrDefault()!.file_size;
                    var cauHinhUpdate = new List<cau_hinh_file>();
                    if (sizeDb != sizeFe)
                    {
                        foreach (var item in dsDB)
                        {
                            item.file_size = sizeFe;
                            cauHinhUpdate.Add(item);
                        }
                        _context.cau_hinh_file.UpdateRange(cauHinhUpdate);
                    }
                }

                // so sánh cấu hình trong db và cấu hình truyền từ FE về 
                // TH1: cấu hình giống nhau => không cập nhật
                if (dsFE == null || dsFE.Count == 0)
                    return "Không thể không có cấu hình file";
                // TH2: Trường hợp thêm mới
                // 2.1: for qua danh sách cấu hình từ FE truyền về
                var cauHinhAdd = new List<cau_hinh_file>();
                foreach (var fe in dsFE)
                {
                    // 2.2: so sánh với cấu hình trong db
                    var existsInDb = dsDB.Any(db => db.extension_file.Equals(fe.extension_file, StringComparison.OrdinalIgnoreCase));
                    // 2.3: nếu FE truyền về có mà db không có thì thêm mới
                    if (!existsInDb)
                    {
                        var newConfig = new cau_hinh_file
                        {
                            id = Guid.NewGuid(),
                            file_type = fe.file_type,
                            extension_file = fe.extension_file,
                            file_size = fe.file_size
                        };
                        cauHinhAdd.Add(newConfig);
                    }
                }
                _context.cau_hinh_file.AddRange(cauHinhAdd);

                // TH3: Trường hợp xóa
                // 3.1: for qua danh sách cấu hình từ DB
                var cauHinhDelete = new List<cau_hinh_file>();
                foreach (var fe in dsDB)
                {
                    // 3.2: so sánh với cấu hình từ FE truyền về
                    var existsInFE = dsFE.Any(db => db.extension_file!.Equals(fe.extension_file, StringComparison.OrdinalIgnoreCase));
                    // 3.3: nếu BE có mà FE truyền về không có => xóa 
                    if (!existsInFE)
                    {
                        cauHinhDelete.Add(fe);
                    }
                }
                _context.cau_hinh_file.RemoveRange(cauHinhDelete);
                _context.SaveChanges();

                return "Cập nhật cấu hình file thành công";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<List<cau_hinh_file_dto>> getAllConfig()
        {
            try
            {
                var dsCauHinhFile = await (from chf in _context.cau_hinh_file
                                 select new cau_hinh_file_dto
                                 {
                                     id = chf.id,
                                     file_type = chf.file_type,
                                     extension_file = chf.extension_file,
                                     file_size = chf.file_size
                                 }).ToListAsync(new CancellationToken());
                return dsCauHinhFile;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
