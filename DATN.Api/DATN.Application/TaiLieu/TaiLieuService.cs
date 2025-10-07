using DATN.Application.Interfaces;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using DATN.Domain.Entities;
using DATN.Infrastructure.Data;
using DocumentFormat.OpenXml.Office2016.Excel;
using DocumentFormat.OpenXml.Packaging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace DATN.Application.TaiLieu
{
    public class TaiLieuService : ITaiLieuService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly Helper _helper;
        private readonly INhatKyHeThong _logger;
        public TaiLieuService(AppDbContext context, Helper helper, INhatKyHeThong logger, IConfiguration config)
        {
            _context = context;
            _helper = helper;
            _logger = logger;
            _config = config;
        }

        public async Task<IActionResult> AddTaiLieu(uploadedFileInfo request)
        {
            try
            {
                if (request.files == null || request.files.Count == 0) throw new Exception("Không có file nào được upload.");

                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
                string secret = _config.GetSection("RootFileServer")["secret"] ?? "";
                var currentUser = _helper.GetUserInfo().userName;
                if(currentUser == null)
                {
                    throw new Exception("Không lấy được thông tin người dùng.");
                }

                var userInfor = _context.nguoi_dung.FirstOrDefault(x => x.tai_khoan == currentUser);
                var folderPath = Path.Combine(rootPath, currentUser, secret);
                var mucDo = _context.danh_muc.FirstOrDefault(x => x.Id == request.cap_do_id);
                var mucDoNum = mucDo?.ma == "tuyet-mat" ? 3 : (mucDo?.ma == "toi-mat" ? 2 : 1) ;
                // chưa có thư mục thì tạo mới
                if (!Directory.Exists(folderPath))
                {
                    Directory.CreateDirectory(folderPath);
                }

                List<tai_lieu> dsNewDocs = new List<tai_lieu>();
                foreach (var file in request.files)
                {
                    // hash file + check trùng
                    var fileHash = Helper.ComputeFileHash(file);
                    var existingFile = _context.tai_lieu.FirstOrDefault(x => x.FileHash == fileHash);
                    if (existingFile != null)
                    {
                        throw new Exception($"File {file.FileName} đã tồn tại trên hệ thống.");
                    }

                    var filePath = Path.Combine(folderPath, file.FileName ?? "");
                    var pathForDb = Path.Combine(currentUser, secret, file.FileName ?? "");
                    // LƯU FILE LÊN SERVER
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    // MÃ HÓA FILE VỪA LƯU THEO CẤP ĐỘ
                    if(mucDoNum == 3) // tuyệt mật
                    {

                    }
                    else if (mucDoNum == 2) // tối mật
                    {

                    }
                    else // mật
                    {

                    }

                    //lấy plainText
                    string plainText = "";
                    var fileExt = Path.GetExtension(file.FileName)?.ToLower();
                    if(fileExt?.ToLower() == ".docx" || fileExt?.ToLower() == ".doc")
                    {
                        plainText = GetPlainTextDocx(file);
                    }

                    // add vào db
                    var phongbanInfor = _context.danh_muc.FirstOrDefault(x => x.Id == request.phong_ban_id);
                    var newFile = new tai_lieu
                    {
                        Id = Guid.NewGuid(),
                        ma = file.FileName ?? "", // tạo mã ngẫu nhiên
                        ten = file.FileName ?? "",
                        duong_dan = pathForDb,
                        cap_do = mucDoNum,
                        phong_ban = phongbanInfor?.ma ?? "", // cần lấy thông tin phòng ban từ user
                        is_share = false,
                        phien_ban = 1,
                        isPublic = false,
                        FileType = fileExt,
                        FileSize = file.Length,
                        FileHash = fileHash, // cần tính toán hash của file
                        Status = "uploaded",
                        ContentText = plainText ?? "", // cần trích xuất text từ file
                        IndexedAt = null,
                        IndexStatus = null,
                        thu_muc_id = request.thu_muc_id ?? null,
                        ngay_tao = DateTime.Now,
                        nguoi_tao = currentUser,
                        ngay_chinh_sua = DateTime.Now,
                        nguoi_chinh_sua = currentUser,
                    };
                    dsNewDocs.Add(newFile);

                }

                if(dsNewDocs != null && dsNewDocs.Count > 0)
                {
                    _context.AddRange(dsNewDocs);
                    await _context.SaveChangesAsync(new CancellationToken());
                }

                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Upload {request.files.Count} tài liệu",
                    command = "PERM_ADD",
                });
                return new OkObjectResult(dsNewDocs);
            }
            catch (Exception ex)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_ADD",
                });
                throw new Exception(ex.Message);
            }
        }

        //HÀM GET PLAINTEXT FILE WORD
        public string GetPlainTextDocx(IFormFile file)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                var memStream = new MemoryStream();
                file.OpenReadStream().CopyTo(memStream);

                using (var wordDoc = WordprocessingDocument.Open(memStream, false))
                {
                    MainDocumentPart mainDocPart = wordDoc.MainDocumentPart;
                    var body = mainDocPart.Document.Body;
                    foreach (var text in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
                    {
                        sb.AppendLine(text.Text);
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<PaginatedList<tai_lieu_dto>> GetTaiLieuByPhanQuyen(tai_lieu_dto request)
        {
            try
            {
                var currentUser = _helper.GetUserInfo().userName ?? "anonymous";
                var userInfor = _context.nguoi_dung.FirstOrDefault(x => x.tai_khoan == currentUser);

                //tài liệu chia sẻ với tôi
                var ShareData = _context.tai_lieu_2_nguoi_dung.Where(x => x.nguoi_dung_id == userInfor!.Id).Select(x => x.tai_lieu_id);
                // tài liêu của tôi + tài liệu được chia sẻ
                var datas = _context.tai_lieu.Where(x => (x.nguoi_tao == currentUser || ShareData.Contains(x.Id)) && x.thu_muc_id == null).AsNoTracking();
                
                if (datas != null && datas.Count() > 0)
                {
                    //QUERY -- QUERY -- QUERY
                    if (request.FileType != null)
                    {
                        datas = datas.Where(x => x.FileType != null && x.FileType.ToLower().Contains(request.FileType.ToLower()));
                    }

                    var datasDto = datas.Select(x => new tai_lieu_dto
                    {
                        Id = x.Id,
                        cap_do = x.cap_do,
                        ContentText = x.ContentText,
                        duong_dan = x.duong_dan,
                        FileHash = x.FileHash,
                        FileSize = x.FileSize,
                        FileType = x.FileType,
                        IndexStatus = x.IndexStatus,
                        IndexedAt = x.IndexedAt,
                        isPublic = x.isPublic,
                        is_share = x.is_share,
                        ma = x.ma,
                        phien_ban = x.phien_ban,
                        phong_ban = x.phong_ban,
                        Status = x.Status,
                        ten = x.ten,
                        thu_muc_id = x.thu_muc_id,
                        ngay_tao = x.ngay_tao,
                        nguoi_tao = x.nguoi_tao,
                        ngay_chinh_sua = x.ngay_chinh_sua,
                        nguoi_chinh_sua = x.nguoi_chinh_sua,
                    });
                    var result = await PaginatedList<tai_lieu_dto>.Create(datasDto, request.pageNumber, request.pageSize);

                    await _logger.AddLog(new nhat_ky_he_thong_dto
                    {
                        loai = 1,
                        detail = $"Truy cập kho tài liệu",
                        command = "PERM_VIEW",
                    });
                    return result;
                }
                else
                {
                    return new PaginatedList<tai_lieu_dto>(new List<tai_lieu_dto>(), 0, request.pageNumber, request.pageSize);
                }
            }
            catch (Exception ex)
            {
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 2,
                    detail = ex.Message,
                    command = "PERM_VIEW",
                });
                throw new Exception(ex.Message);
            }
        }

        public Task<List<nguoi_dung_dto>> GetAllNguoiDungByDocs(Guid currentUserId)
        {
            try
            {
                var result = new List<nguoi_dung_dto>();
                var currentUser = _context.nguoi_dung.FirstOrDefault(x => x.Id == currentUserId);
                if(currentUser == null)
                {
                    throw new Exception("Không tìm thấy người dùng.");
                }

                //lấy ds tài liệu được chia sẻ với mình
                var ShareData = _context.tai_lieu_2_nguoi_dung.Where(x => x.nguoi_dung_id == currentUser.Id).Select(x => x.tai_lieu_id);
                var ownerDocs = _context.tai_lieu.Where(x => ShareData.Contains(x.Id)).Select(x => x.nguoi_tao);

                //lấy ds người dùng đã chia sẻ tài liệu với mình
                var dsNguoiDung = _context.nguoi_dung.Where(x => ownerDocs.Contains(x.tai_khoan)).Select(x => new nguoi_dung_dto
                {
                    id = x.Id,
                    ten = x.ten,
                    tai_khoan = x.tai_khoan,
                    email = x.email,
                    so_dien_thoai = x.so_dien_thoai,
                }).Distinct().ToList();

                result.AddRange(dsNguoiDung);
                result.Add(new nguoi_dung_dto
                {
                    id = currentUser.Id,
                    ten = $"{currentUser.ten} (Tôi)",
                    tai_khoan = currentUser.tai_khoan,
                    email = currentUser.email,
                    so_dien_thoai = currentUser.so_dien_thoai,
                });


                return Task.FromResult(result);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        public static int GetLoaiTaiLieu(string? fileExt)
        {
            if (fileExt == null) return 0;
            if (fileExt == ".docx" || fileExt == ".doc" || fileExt == ".txt" || fileExt == ".odt")
            {
                return 1; // tài liệu (word)
            }
            else if (fileExt == ".xlsx" || fileExt == ".xls" || fileExt == ".csv" || fileExt == ".ods")
            {
                return 2; // bảng tính (excel)
            }
            else if (fileExt == ".pdf")
            {
                return 3; // pdf
            }
            else if (fileExt == ".jpg" || fileExt == ".jpeg" || fileExt == ".png" || fileExt == ".gif" || fileExt == ".bmp" || fileExt == ".tiff")
            {
                return 4; // hình ảnh
            }
            else
            {
                return 0; // khác
            }
        }
        public async Task<PaginatedList<ResultSearch>> GetDataSearch(ResultSearchParams request)
        {
            try
            {
                var result = new List<ResultSearch>();
                var currentUser = _context.nguoi_dung.FirstOrDefault(x => x.Id == request.current_user_id);
                if(currentUser == null)
                {
                    throw new Exception("Không tìm thấy người dùng.");
                }

                // ds tài liệu
                var dsTaiLieu = _context.tai_lieu.Where(x=> x.nguoi_tao == currentUser.tai_khoan).Select( x => new ResultSearch
                {
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType,
                    
                });
                result.AddRange(dsTaiLieu);

                //ds được chia sẻ với tôi
                var shareDocs = _context.tai_lieu_2_nguoi_dung.Where(x => x.nguoi_dung_id == currentUser.Id).Select(x => x.tai_lieu_id);
                var dsTaiLieuShare = _context.tai_lieu.Where(x => shareDocs.Contains(x.Id)).Select(x => new ResultSearch
                {
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType
                });
                result.AddRange(dsTaiLieuShare);

                // ds thư mục
                var dsThuMuc = _context.thu_muc.Where(x => x.nguoi_tao == currentUser.tai_khoan).Select(x => new ResultSearch
                {
                    is_folder = true,
                    ten = x.ten,
                    ngay_tao = x.ngay_tao,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ten_chu_so_huu = x.nguoi_tao,
                });
                result.AddRange(dsThuMuc);


                // QUERY -- QUERY -- QUERY
                if (request.keySearch != null)
                {
                    result = result.Where(x => x.ten != null && x.ten.ToLower().Contains(request.keySearch.ToLower())).ToList(); //chỉ lấy file
                }
                if (request.loai_tai_lieu != null && request.loai_tai_lieu != 0)
                {
                    if(request.loai_tai_lieu == 5)// thư mục
                    {
                        result = result.Where(x => x.is_folder == true).ToList(); //chỉ lấy thư mục
                    }
                    else
                    {
                        result = result.Where(x => x.loai_tai_lieu == request.loai_tai_lieu && x.is_folder == false).ToList(); //chỉ lấy file
                    }
                }
                if (request.trang_thai != null)
                {
                    result = result.Where(x => (request.trang_thai == 1 ? x.ten_chu_so_huu == currentUser.tai_khoan : (request.trang_thai == 2 ? x.ten_chu_so_huu != currentUser.tai_khoan : true))).ToList();
                }
                if (request.nguoi_dung_id != null)
                {
                    var nguoiDung = _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id);
                    if(nguoiDung != null)
                    {
                        result = result.Where(x => x.ten_chu_so_huu == nguoiDung.tai_khoan).ToList();
                    }
                }
                if (request.keyWord != null)
                {
                    result = result.Where(x => x.plain_text != null && x.plain_text.Contains(request.keyWord)).ToList();
                }
                if (request.ten_muc != null)
                {
                    result = result.Where(x => x.ten != null && x.ten.ToLower().Contains(request.ten_muc.ToLower())).ToList();
                }
                if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                {
                    result = result.Where(x => x.ngay_sua_doi != null && x.ngay_sua_doi >= request.ngay_tao_from.Value.AddHours(7) && x.ngay_sua_doi <= request.ngay_tao_to.Value.AddHours(7)).ToList();
                }
                if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                {
                    result = result.Where(x => x.ngay_sua_doi != null && x.ngay_sua_doi >= request.ngay_chinh_sua_from.Value.AddHours(7) && x.ngay_sua_doi <= request.ngay_chinh_sua_to.Value.AddHours(7)).ToList();
                }

                //sort (folder trước - file sau) && sắp xếp theo ngày sửa đổi gần nhất
                result = result.OrderByDescending(x => x.is_folder).ThenByDescending(x => x.ngay_sua_doi).ToList();
                var resultPagin = await PaginatedList<ResultSearch>.CreateToList(result, request.pageNumber, request.pageSize);

                //ghi log
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Tìm kiếm tài liệu",
                    command = "PERM_VIEW",
                });
                return resultPagin;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        //HÀM GET FILE TYPE
        private string GetContentType(string path)
        {
            var types = new Dictionary<string, string>
            {
                {".pdf", "application/pdf"},
                {".doc", "application/msword"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".txt", "text/plain"}
            };

            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types.GetValueOrDefault(ext, "application/octet-stream");
        }
        public async Task<DownloadResult> HandleDownloadTaiLieu(Guid idTaiLieu)
        {
            try
            {
                //tài liệu
                var taiLieu = _context.tai_lieu.FirstOrDefault(x => x.Id == idTaiLieu);
                if(taiLieu == null)
                {
                    throw new Exception("Không lấy được tài liệu.");
                }
                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
                var filePath = Path.Combine(rootPath, taiLieu.duong_dan);

                if (!System.IO.File.Exists(filePath))
                {
                    throw new Exception("Không tìm thấy file.");
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open))
                {
                    stream.CopyTo(memory);
                }
                memory.Position = 0;

                var contentType = GetContentType(filePath);
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Tải xuống tài liệu ${taiLieu.ten}",
                    command = "PERM_DOWNLOAD",
                });

                return new DownloadResult
                {
                    Stream = memory,
                    ContentType = contentType,
                    FileName = Path.GetFileName(filePath)
                };

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public Task<PaginatedList<ResultSearch>> GetDocsByFolder(Guid folder_id)
        {
            throw new NotImplementedException();
        }
    }
}
