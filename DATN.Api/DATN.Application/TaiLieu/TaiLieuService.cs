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
using Nest;
using SharedKernel.Application.Utils;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using static System.Net.Mime.MediaTypeNames;

namespace DATN.Application.TaiLieu
{
    public class TaiLieuService : ITaiLieuService
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly Helper _helper;
        private readonly INhatKyHeThong _logger;
        private readonly ElasticSearchService _elastic;
        private readonly ElasticClient _client;

        public TaiLieuService(AppDbContext context, Helper helper, INhatKyHeThong logger, IConfiguration config, ElasticSearchService elastic, ElasticClient client)
        {
            _context = context;
            _helper = helper;
            _logger = logger;
            _config = config;
            _elastic = elastic;

            var elasticUrl = _config.GetSection("ElasticSearchUrl")["path"] ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(elasticUrl))
            .DefaultIndex("tai_lieu")
            .PrettyJson()
            .DisableDirectStreaming();

            _client = new ElasticClient(settings);
        }
        public ElasticClient Client => _client;
        public async Task<IActionResult> AddTaiLieu(uploadedFileInfo request)
        {
            try
            {
                if (request.files == null || request.files.Count == 0) throw new Exception("Không có file nào được upload.");

                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
                string ElasticSearchUrl = _config.GetSection("ElasticSearchUrl")["path"] ?? "";
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

                    // MÃ HÓA FILE VỪA LƯU THEO CẤP ĐỘ (đặt quy trình mã hóa ở đây)
                    string appCode = _config.GetSection("AppCode").Value ?? "";
                    var vaultUrl = _config.GetSection("Uri")["vault"] + "";
                    HybridEncryption.SetAppCode(appCode);
                    HybridEncryption.SetVaultUrl(vaultUrl);

                    // B1: tạo 1 cặp key ECC
                    (byte[] PrivateKeyECC, byte[] PublicKeyECC) = HybridEncryption.GenerateECCKey();
                    // B2: Lưu 2 key ECC vào vault
                    // 2 key này lưu vào db (chỉ lưu phần khác. VD: đặt tên là ...{random})
                    var guidKeyName = Guid.NewGuid().ToString();
                    string pvKeyName = $"pvECC_key_{file.FileName}_{guidKeyName}";
                    string pbKeyName = $"pbECC_key_{file.FileName}_{guidKeyName}";
                    var res_pv = HybridEncryption.SetVaultSecretValue(appCode, pvKeyName, Convert.ToBase64String(PrivateKeyECC));
                    var res_pb = HybridEncryption.SetVaultSecretValue(appCode, pbKeyName, Convert.ToBase64String(PublicKeyECC));
                    // B3: tạo key AES + B4: mã hóa file bằng AES + B5: mã hóa AES key bằng ECC public key + B6: key AES sau khi mã hóa add vào file đã mã hóa + B7: lưu file đã mã hóa lên server + đổi định dạng file thành .Encrypt (ngăn mở file)
                    var outputFile = HybridEncryption.EncryptFileToStoring(filePath, folderPath, pvKeyName, pbKeyName);
                    // B4: xóa file gốc
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                        pathForDb = pathForDb + ".encrypt";
                    }
                    if (mucDoNum == 3) // tuyệt mật
                    {

                    }
                    else if (mucDoNum == 2) // tối mật
                    {
                        // sử dụng mã hóa AES để mã hóa file + mã hóa key AES bằng ECC public key
                    }
                    else // mật
                    {
                        // sử dụng hàm băm SHA256 để mã hóa file
                    }

                    //lấy plainText
                    string plainText = "";
                    var (htmlCH, _dicImageByte_CH) = ("", new Dictionary<string, byte[]>());
                    var fileExt = Path.GetExtension(file.FileName)?.ToLower();
                    if(fileExt?.ToLower() == ".docx" || fileExt?.ToLower() == ".doc")
                    {
                        plainText = GetPlainTextDocx(file);
                        // lấy html
                        (htmlCH, _dicImageByte_CH) = handleXmlWord.handleReadXml(file.OpenReadStream());

                        // lưu ảnh 
                        var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                        htmlDoc.LoadHtml(htmlCH);
                        var bodyNode = htmlDoc.DocumentNode;

                        var imgNodes = bodyNode.SelectNodes("//img");
                        var lstImg = new List<Dictionary<string, string>>();
                        List<Dictionary<string, string>> imgDict = new List<Dictionary<string, string>>();

                        foreach (var img in imgNodes)
                        {
                            var style = img.GetAttributeValue("style", "");
                            var newStyle = style.TrimEnd(new char[] { ';', ' ' });

                            if (string.IsNullOrEmpty(newStyle))
                            {
                                newStyle = "display:inline-flex";
                            }
                            else if (!newStyle.Contains("display:inline-flex"))
                            {
                                newStyle += ";display:inline-flex";
                            }

                            img.SetAttributeValue("style", newStyle);
                            (var outerXml, lstImg) = handleXmlWord.CreateImgPath(img.OuterHtml, _config);
                            imgDict.AddRange(lstImg);
                            var newImgNode = HtmlAgilityPack.HtmlNode.CreateNode(outerXml);
                            img.ParentNode.ReplaceChild(newImgNode, img);
                        }
                        htmlCH = htmlDoc.DocumentNode.OuterHtml;
                        handleXmlWord.SaveImgToServer(imgDict, _config);

                        // mã hóa plaintext
                        plainText = await HybridEncryption.EncryptStringToStoring(plainText);
                        // mã hóa HTML
                        htmlCH = await HybridEncryption.EncryptStringToStoring(htmlCH);
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
                        EccKeyName = guidKeyName,
                        htmlContent = htmlCH
                    };
                    dsNewDocs.Add(newFile);

                }

                if(dsNewDocs != null && dsNewDocs.Count > 0)
                {
                    _context.tai_lieu.AddRange(dsNewDocs);
                    await _context.SaveChangesAsync(new CancellationToken());

                    var client = new ElasticClient();
                    foreach (var doc in dsNewDocs)
                    {
                        var response = await _elastic.UpsertTaiLieuAsync(doc);

                        if (response.IsValid)
                        {
                            doc.IndexStatus = "success";
                            doc.IndexedAt = DateTime.UtcNow;
                        }
                        else
                        {
                            doc.IndexStatus = "error";
                        }
                    }
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
                        htmlContent = x.htmlContent,
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

        private static string[] GetExtensionsByLoaiTaiLieu(int loai)
        {
            return loai switch
            {
                1 => new[] { ".doc", ".docx", ".txt", ".odt" },              // Word
                2 => new[] { ".xls", ".xlsx", ".csv", ".ods" },              // Excel
                3 => new[] { ".pdf" },                                       // PDF
                4 => new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff" }, // Ảnh
                _ => Array.Empty<string>()
            };
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
                var dsTaiLieu = await _client.SearchAsync<tai_lieu>(s => s
                    .Index("tai_lieu")
                    .Query(q =>
                        q.Bool(b =>
                            {
                                var mustQueries = new List<QueryContainer>();
                                var mustNotQueries = new List<QueryContainer>();
                                
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                mustNotQueries.Add(q.Exists(e => e.Field(f => f.thu_muc_id.Suffix("keyword"))));

                                if (request.keySearch != null)
                                {
                                    mustQueries.Add(q.Wildcard(w => w
                                         .Field(f => f.ten.Suffix("keyword"))
                                         .Value($"*{request.keySearch}*") // không ToLower()
                                         .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                     ));
                                    mustQueries.Add(q.Match(m => m
                                        .Field(f => f.ContentText)
                                        .Query(request.keySearch)
                                    ));
                                }

                                if(request.loai_tai_lieu != null)
                                {
                                    if(request.loai_tai_lieu == 5)//thư mục
                                    {
                                        mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                                    }
                                    else
                                    {
                                        var extensions = GetExtensionsByLoaiTaiLieu(request.loai_tai_lieu.Value);
                                        if (extensions.Length > 0)
                                        {
                                            mustQueries.Add(q.Terms(t => t
                                                .Field(f => f.FileType.Suffix("keyword"))
                                                .Terms(extensions)
                                            ));
                                        }
                                    }
                                }

                                if (request.trang_thai != null)
                                {
                                    if (request.trang_thai == 1)
                                    {
                                        mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                    }
                                    else
                                    {
                                        if(request.trang_thai == 2)
                                        {
                                            mustQueries.Add(q.Bool(b => b
                                                .MustNot(m => m
                                                    .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                                )
                                            ));
                                        }
                                    }
                                }

                                if (request.nguoi_dung_id != null)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                        _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                    )));
                                }

                                if (request.keyWord != null)
                                {
                                    mustQueries.Add(q.Match(m => m
                                        .Field(f => f.ContentText)
                                        .Query(request.keyWord)
                                    ));
                                }

                                if (request.ten_muc != null)
                                {
                                    mustQueries.Add(q.Wildcard(w => w
                                         .Field(f => f.ten.Suffix("keyword"))
                                         .Value($"*{request.ten_muc}*") // không ToLower()
                                         .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                     ));
                                }

                                if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                                {
                                    mustQueries.Add(q.DateRange(dr => dr
                                        .Field(f => f.ngay_tao)
                                        .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                        .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                    ));
                                }

                                if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                                {
                                    mustQueries.Add(q.DateRange(dr => dr
                                        .Field(f => f.ngay_chinh_sua)
                                        .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                        .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                    ));
                                }

                                return b.Must(mustQueries.ToArray())
                                    .MustNot(mustNotQueries.ToArray());
                            }
                        )
                    )
                );


                result.AddRange(dsTaiLieu.Documents.Select(x => new ResultSearch
                {
                    id = x.Id,
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType,
                    html_content = x.htmlContent
                }).ToList());

                //ds được chia sẻ với tôi
                var shareDocs = _context.tai_lieu_2_nguoi_dung.Where(x => x.nguoi_dung_id == currentUser.Id).Select(x => x.tai_lieu_id);
                var dsTaiLieuShare = await _client.SearchAsync<tai_lieu>(s => s
                    .Index("tai_lieu")
                    .Query(q =>
                        q.Bool(b =>
                        {
                            var mustQueries = new List<QueryContainer>();
                            var mustNotQueries = new List<QueryContainer>();

                            // mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                            // mustNotQueries.Add(q.Exists(e => e.Field(f => f.thu_muc_id.Suffix("keyword"))));
                            if (shareDocs != null && shareDocs.Any())
                            {
                                mustQueries.Add(q.Terms(t => t
                                    .Field(f => f.Id.Suffix("keyword"))
                                    .Terms(shareDocs)
                                ));
                            }
                            else
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                            }

                            if (request.keySearch != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.keySearch}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keySearch)
                                ));
                            }

                            if (request.loai_tai_lieu != null)
                            {
                                if (request.loai_tai_lieu == 5)//thư mục
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                                }
                                else
                                {
                                    var extensions = GetExtensionsByLoaiTaiLieu(request.loai_tai_lieu.Value);
                                    if (extensions.Length > 0)
                                    {
                                        mustQueries.Add(q.Terms(t => t
                                            .Field(f => f.FileType.Suffix("keyword"))
                                            .Terms(extensions)
                                        ));
                                    }
                                }
                            }

                            if (request.trang_thai != null)
                            {
                                if (request.trang_thai == 1)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                }
                                else
                                {
                                    if (request.trang_thai == 2)
                                    {
                                        mustQueries.Add(q.Bool(b => b
                                            .MustNot(m => m
                                                .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                            )
                                        ));
                                    }
                                }
                            }

                            if (request.nguoi_dung_id != null)
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                    _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                )));
                            }

                            if (request.keyWord != null)
                            {
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keyWord)
                                ));
                            }

                            if (request.ten_muc != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.ten_muc}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_tao)
                                    .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                ));
                            }

                            if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_chinh_sua)
                                    .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                ));
                            }

                            return b.Must(mustQueries.ToArray())
                                .MustNot(mustNotQueries.ToArray());
                        }
                        )
                    )
                );
                result.AddRange(dsTaiLieuShare.Documents.Select(x => new ResultSearch
                {
                    id = x.Id,
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType,
                    html_content = x.htmlContent
                }).ToList());

                // ds thư mục
                var dsThuMuc = await _client.SearchAsync<thu_muc>(s => s
                    .Index("thu_muc")
                    .Query(q =>
                        q.Bool(b =>
                        {
                            var mustQueries = new List<QueryContainer>();
                            var mustNotQueries = new List<QueryContainer>();

                            mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan.ToString())));
                            mustNotQueries.Add(q.Exists(e => e.Field(f => f.thu_muc_cha_id.Suffix("keyword"))));
                            if (request.keySearch != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.keySearch}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.loai_tai_lieu != null)
                            {
                                if (request.loai_tai_lieu == 1 || request.loai_tai_lieu == 2 || request.loai_tai_lieu == 3|| request.loai_tai_lieu == 4)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.id.Suffix("keyword")).Value("___no_result___")));
                                }
                                if (request.loai_tai_lieu == 5)//thư mục
                                {
                                   
                                }
                            }

                            if (request.trang_thai != null)
                            {
                                if (request.trang_thai == 1)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                }
                                else
                                {
                                    if (request.trang_thai == 2)
                                    {
                                        mustQueries.Add(q.Bool(b => b
                                            .MustNot(m => m
                                                .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                            )
                                        ));
                                    }
                                }
                            }

                            if (request.nguoi_dung_id != null)
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                    _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                )));
                            }

                            if (request.ten_muc != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.ten_muc}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_tao)
                                    .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                ));
                            }

                            if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_chinh_sua)
                                    .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                ));
                            }

                            return b.Must(mustQueries.ToArray())
                                .MustNot(mustNotQueries.ToArray());
                        }
                        )
                    )
                );
                result.AddRange(dsThuMuc.Documents.Select(x => new ResultSearch
                {
                    id = x.id,
                    is_folder = true,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    ten_chu_so_huu = x.nguoi_tao,
                }).ToList());

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
                string share = _config.GetSection("RootFileServer")["share"] ?? "";
                var filePath = Path.Combine(rootPath, taiLieu.duong_dan);
                var folderShare = Path.Combine(rootPath, share);
                var encryptFile = Path.Combine(rootPath, taiLieu.duong_dan + ".encrypt");

                if (!System.IO.File.Exists(filePath))
                {
                    string appCode = _config.GetSection("AppCode").Value ?? "";
                    var vaultUrl = _config.GetSection("Uri")["vault"] + "";
                    HybridEncryption.SetAppCode(appCode);
                    HybridEncryption.SetVaultUrl(vaultUrl);

                    string pvKeyName = $"pvECC_key_{taiLieu.ten}_{taiLieu.EccKeyName}";
                    var receiverPrivateKey = await HybridEncryption.GetVaultSecretValue("NHCH", pvKeyName);
                    var decrypt = HybridEncryption.DecryptFileToStoring(encryptFile, folderShare, receiverPrivateKey);
                    filePath = encryptFile;
                    if (!System.IO.File.Exists(filePath)) 
                    {
                        throw new Exception("File không tồn tại trên hệ thống.");
                    }
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
                await DeletePublicDocs();
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

        public async Task<PaginatedList<ResultSearch>> GetDocsByFolder(ResultSearchParams request)
        {
            try
            {
                if(request.thu_muc_id == null)
                {
                    throw new Exception("Chưa chọn thư mục");
                }
                // var docs = _context.tai_lieu.Where(x => x.thu_muc_id == request.thu_muc_id).ToList();
                // var folders = _context.thu_muc.Where(x => x.thu_muc_cha_id == request.thu_muc_id).ToList();
                var result = new List<ResultSearch>();
                var currentUser = _context.nguoi_dung.FirstOrDefault(x => x.Id == request.current_user_id);
                if (currentUser == null)
                {
                    throw new Exception("Không tìm thấy người dùng.");
                }

                // ds tài liệu
                var dsTaiLieu = await _client.SearchAsync<tai_lieu>(s => s
                    .Index("tai_lieu")
                    .Query(q =>
                        q.Bool(b =>
                        {
                            var mustQueries = new List<QueryContainer>();
                            var mustNotQueries = new List<QueryContainer>();

                            mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                            mustQueries.Add(q.Term(t => t.Field(f => f.thu_muc_id.Suffix("keyword")).Value(request.thu_muc_id)));

                            if (request.keySearch != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.keySearch}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keySearch)
                                ));
                            }

                            if (request.loai_tai_lieu != null)
                            {
                                if (request.loai_tai_lieu == 5)//thư mục
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                                }
                                else
                                {
                                    var extensions = GetExtensionsByLoaiTaiLieu(request.loai_tai_lieu.Value);
                                    if (extensions.Length > 0)
                                    {
                                        mustQueries.Add(q.Terms(t => t
                                            .Field(f => f.FileType.Suffix("keyword"))
                                            .Terms(extensions)
                                        ));
                                    }
                                }
                            }

                            if (request.trang_thai != null)
                            {
                                if (request.trang_thai == 1)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                }
                                else
                                {
                                    if (request.trang_thai == 2)
                                    {
                                        mustQueries.Add(q.Bool(b => b
                                            .MustNot(m => m
                                                .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                            )
                                        ));
                                    }
                                }
                            }

                            if (request.nguoi_dung_id != null)
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                    _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                )));
                            }

                            if (request.keyWord != null)
                            {
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keyWord)
                                ));
                            }

                            if (request.ten_muc != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.ten_muc}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_tao)
                                    .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                ));
                            }

                            if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_chinh_sua)
                                    .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                ));
                            }

                            return b.Must(mustQueries.ToArray())
                                .MustNot(mustNotQueries.ToArray());
                        }
                        )
                    )
                );
                result.AddRange(dsTaiLieu.Documents.Select(x => new ResultSearch
                {
                    id = x.Id,
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType,
                }).ToList());

                //ds được chia sẻ với tôi
                var shareDocs = _context.tai_lieu_2_nguoi_dung.Where(x => x.nguoi_dung_id == currentUser.Id).Select(x => x.tai_lieu_id);
                var dsTaiLieuShare = await _client.SearchAsync<tai_lieu>(s => s
                    .Index("tai_lieu")
                    .Query(q =>
                        q.Bool(b =>
                        {
                            var mustQueries = new List<QueryContainer>();
                            var mustNotQueries = new List<QueryContainer>();

                            // mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                            // mustNotQueries.Add(q.Exists(e => e.Field(f => f.thu_muc_id.Suffix("keyword"))));
                           
                            if (shareDocs != null && shareDocs.Any())
                            {
                                mustQueries.Add(q.Terms(t => t
                                   .Field(f => f.Id.Suffix("keyword"))
                                   .Terms(shareDocs)
                               ));
                                mustQueries.Add(q.Term(t => t.Field(f => f.thu_muc_id.Suffix("keyword")).Value(request.thu_muc_id)));
                            }
                            else
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                            }


                            if (request.keySearch != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.keySearch}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keySearch)
                                ));
                            }

                            if (request.loai_tai_lieu != null)
                            {
                                if (request.loai_tai_lieu == 5)//thư mục
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.Id.Suffix("keyword")).Value("___no_result___")));
                                }
                                else
                                {
                                    var extensions = GetExtensionsByLoaiTaiLieu(request.loai_tai_lieu.Value);
                                    if (extensions.Length > 0)
                                    {
                                        mustQueries.Add(q.Terms(t => t
                                            .Field(f => f.FileType.Suffix("keyword"))
                                            .Terms(extensions)
                                        ));
                                    }
                                }
                            }

                            if (request.trang_thai != null)
                            {
                                if (request.trang_thai == 1)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                }
                                else
                                {
                                    if (request.trang_thai == 2)
                                    {
                                        mustQueries.Add(q.Bool(b => b
                                            .MustNot(m => m
                                                .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                            )
                                        ));
                                    }
                                }
                            }

                            if (request.nguoi_dung_id != null)
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                    _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                )));
                            }

                            if (request.keyWord != null)
                            {
                                mustQueries.Add(q.Match(m => m
                                    .Field(f => f.ContentText)
                                    .Query(request.keyWord)
                                ));
                            }

                            if (request.ten_muc != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.ten_muc}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_tao)
                                    .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                ));
                            }

                            if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_chinh_sua)
                                    .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                ));
                            }

                            return b.Must(mustQueries.ToArray())
                                .MustNot(mustNotQueries.ToArray());
                        }
                        )
                    )
                );
                result.AddRange(dsTaiLieuShare.Documents.Select(x => new ResultSearch
                {
                    id = x.Id,
                    is_folder = false,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    kich_co_tep = x.FileSize,
                    ten_chu_so_huu = x.nguoi_tao,
                    loai_tai_lieu = GetLoaiTaiLieu(x.FileType),
                    plain_text = x.ContentText,
                    extension = x.FileType,
                }).ToList());

                // ds thư mục
                var dsThuMuc = await _client.SearchAsync<thu_muc>(s => s
                    .Index("thu_muc")
                    .Query(q =>
                        q.Bool(b =>
                        {
                            var mustQueries = new List<QueryContainer>();
                            var mustNotQueries = new List<QueryContainer>();

                            mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan.ToString())));
                            mustQueries.Add(q.Term(t => t.Field(f => f.thu_muc_cha_id.Suffix("keyword")).Value(request.thu_muc_id)));
                            if (request.keySearch != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.keySearch}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.loai_tai_lieu != null)
                            {
                                if (request.loai_tai_lieu == 1 || request.loai_tai_lieu == 1 || request.loai_tai_lieu == 2 || request.loai_tai_lieu == 3 || request.loai_tai_lieu == 4)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.id.Suffix("keyword")).Value("___no_result___")));
                                }
                                if (request.loai_tai_lieu == 5)//thư mục
                                {

                                }
                            }

                            if (request.trang_thai != null)
                            {
                                if (request.trang_thai == 1)
                                {
                                    mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan)));
                                }
                                else
                                {
                                    if (request.trang_thai == 2)
                                    {
                                        mustQueries.Add(q.Bool(b => b
                                            .MustNot(m => m
                                                .Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(currentUser.tai_khoan))
                                            )
                                        ));
                                    }
                                }
                            }

                            if (request.nguoi_dung_id != null)
                            {
                                mustQueries.Add(q.Term(t => t.Field(f => f.nguoi_tao.Suffix("keyword")).Value(
                                    _context.nguoi_dung.FirstOrDefault(x => x.Id == request.nguoi_dung_id)!.tai_khoan
                                )));
                            }

                            if (request.ten_muc != null)
                            {
                                mustQueries.Add(q.Wildcard(w => w
                                     .Field(f => f.ten.Suffix("keyword"))
                                     .Value($"*{request.ten_muc}*") // không ToLower()
                                     .CaseInsensitive(true) // giữ nếu ES >= 7.10
                                 ));
                            }

                            if (request.ngay_tao_from != null && request.ngay_tao_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_tao)
                                    .GreaterThanOrEquals(request.ngay_tao_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_tao_to.Value.AddHours(7))
                                ));
                            }

                            if (request.ngay_chinh_sua_from != null && request.ngay_chinh_sua_to != null)
                            {
                                mustQueries.Add(q.DateRange(dr => dr
                                    .Field(f => f.ngay_chinh_sua)
                                    .GreaterThanOrEquals(request.ngay_chinh_sua_from.Value.AddHours(7))
                                    .LessThanOrEquals(request.ngay_chinh_sua_to.Value.AddHours(7))
                                ));
                            }

                            return b.Must(mustQueries.ToArray())
                                .MustNot(mustNotQueries.ToArray());
                        }
                        )
                    )
                );
                result.AddRange(dsThuMuc.Documents.Select(x => new ResultSearch
                {
                    id = x.id,
                    is_folder = true,
                    ten = x.ten,
                    ngay_sua_doi = x.ngay_chinh_sua ?? x.ngay_tao,
                    ngay_tao = x.ngay_tao,
                    ten_chu_so_huu = x.nguoi_tao,
                }).ToList());


                // QUERY -- QUERY -- QUERY
                if (request.keySearch != null)
                {
                    result = result.Where(x => x.ten != null && x.ten.ToLower().Contains(request.keySearch.ToLower())).ToList(); //chỉ lấy file
                }
                if (request.loai_tai_lieu != null && request.loai_tai_lieu != 0)
                {
                    if (request.loai_tai_lieu == 5)// thư mục
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
                    if (nguoiDung != null)
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

                return resultPagin;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            } 
        }

        public async Task<string> HandleShareFile(ShareFileParams request)
        {
            try
            {
                var TL2ND = _context.tai_lieu_2_nguoi_dung.Where(x => x.tai_lieu_id == request.tai_lieu_id).ToList();
                if (TL2ND != null)
                {
                    var dsAdd = new List<tai_lieu_2_nguoi_dung>();
                    var dsDelete = new List<tai_lieu_2_nguoi_dung>();

                    var dsNguoiDungTrongDb = TL2ND.Select(x => x.nguoi_dung_id).ToList();
                    var dsNguoiDungRequest = request.ds_nguoi_dung ?? new List<Guid>();

                    // ❌ Có trong DB nhưng không có trong request => xóa
                    dsDelete = TL2ND
                        .Where(x => !dsNguoiDungRequest.Contains(x.nguoi_dung_id))
                        .ToList();

                    // ✅ Có trong request nhưng không có trong DB => thêm
                    var dsThemMoi = dsNguoiDungRequest
                        .Where(id => !dsNguoiDungTrongDb.Contains(id))
                        .ToList();

                    foreach (var id in dsThemMoi)
                    {
                        dsAdd.Add(new tai_lieu_2_nguoi_dung
                        {
                            tai_lieu_id = request.tai_lieu_id, // thay bằng id tài liệu của bạn
                            nguoi_dung_id = id
                        });
                    }

                    // Sau đó có thể xử lý
                    if (dsDelete.Any())
                        _context.tai_lieu_2_nguoi_dung.RemoveRange(dsDelete);

                    if (dsAdd.Any())
                        await _context.tai_lieu_2_nguoi_dung.AddRangeAsync(dsAdd);

                    await _context.SaveChangesAsync();
                }

                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"",
                    command = "PERM_SHARE",
                });

                return "OK";
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        public async Task<string> HandleChangeName(ChangenameParams request)
        {
            try
            {
                var doc = await _context.tai_lieu.FirstOrDefaultAsync(x => x.Id == request.tai_lieu_id);
                if (doc == null)
                    throw new Exception("Không tồn tại tài liệu!");

                // Lấy đường dẫn gốc từ config
                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
                string secret = _config.GetSection("RootFileServer")["secret"] ?? "";
                var currentUser = _helper.GetUserInfo().userName;
                if (currentUser == null)
                {
                    throw new Exception("Không lấy được thông tin người dùng.");
                }

                // Lấy thông tin đường dẫn cũ
                string oldRelativePath = doc.duong_dan; // vd: admin\secret\use_case_diagram.pdf
                string oldFullPath = Path.Combine(rootPath, oldRelativePath);

                // Tách thư mục và phần mở rộng
                string directory = Path.GetDirectoryName(oldRelativePath) ?? "";
                string extension = Path.GetExtension(oldRelativePath);
                string newFileName = $"{request.new_name}{extension}";

                // Tạo đường dẫn mới
                string newRelativePath = Path.Combine(directory, newFileName + ".encrypt");
                string newFullPath = Path.Combine(rootPath, newRelativePath);

                // Đảm bảo thư mục tồn tại
                if (!Directory.Exists(Path.Combine(rootPath, directory)))
                {
                    Directory.CreateDirectory(Path.Combine(rootPath, directory));
                }

                // Đổi tên file thật trên server (nếu tồn tại)
                if (File.Exists(oldFullPath))
                {
                    File.Move(oldFullPath, newFullPath, true); // true = ghi đè nếu trùng tên
                }
                else
                {
                    if(File.Exists(oldFullPath + ".encrypt"))
                    {
                        File.Move(oldFullPath + ".encrypt", newFullPath, true);
                    }
                    else
                    {
                        throw new Exception($"Không tìm thấy file tại: {oldFullPath}");
                    }
                }

                // Cập nhật DB
                doc.ten = request.new_name + extension;
                doc.duong_dan = newRelativePath.Replace("\\", "/"); // dùng "/" để đồng nhất

                _context.tai_lieu.Update(doc);
                await _context.SaveChangesAsync();
                var updateResponse = await _client.UpdateAsync<tai_lieu>(doc.Id.ToString(), u => u
                    .Index("tai_lieu")
                    .Doc(new tai_lieu
                    {
                        ten = doc.ten,
                        duong_dan = doc.duong_dan
                    })
                );

                if (!updateResponse.IsValid)
                {
                    Console.WriteLine($"[ES] Cập nhật thất bại: {updateResponse.ServerError?.Error.Reason}");
                }
                return "Đổi tên tài liệu và file trên server thành công!";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi đổi tên tài liệu: {ex.Message}");
            }
        }


        public async Task<string> DeleteDocs(Guid id)
        {
            try
            {
                // 1️⃣ Lấy thông tin tài liệu
                var doc = await _context.tai_lieu.FirstOrDefaultAsync(x => x.Id == id);
                if (doc == null)
                    throw new Exception("Không tìm thấy tài liệu cần xóa!");

                // 2️⃣ Lấy đường dẫn gốc từ cấu hình
                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";

                // 3️⃣ Xác định đường dẫn thật trên server
                string fullPath = Path.Combine(rootPath, doc.duong_dan);
                string fullPathEncrypt = Path.Combine(rootPath, doc.duong_dan + ".encrypt");

                // 4️⃣ Xóa file thật trên ổ đĩa (nếu có)
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }
                
                if (File.Exists(fullPathEncrypt))
                {
                    File.Delete(fullPathEncrypt);
                }

                // 5️⃣ Xóa bản ghi trong database
                _context.tai_lieu.Remove(doc);
                await _context.SaveChangesAsync();

                // Xóa trong Elasticsearch
                var esResponse = await _client.DeleteAsync<tai_lieu>(id, d => d.Index("tai_lieu"));

                if (!esResponse.IsValid)
                {
                    // Có thể log lại lỗi nhưng KHÔNG throw để tránh rollback DB
                    Console.WriteLine($"⚠️ Không thể xóa trong Elasticsearch: {esResponse.OriginalException?.Message}");
                }

                return "Xóa tài liệu thành công!";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa tài liệu: {ex.Message}");
            }
        }
        
        public async Task<string> DeletePublicDocs()
        {
            string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
            string share = _config.GetSection("RootFileServer")["share"] ?? "";
            
            string folderShare = Path.Combine(rootPath, share);
            if (!Directory.Exists(folderShare))
            {
                 return ("Thư mục chia sẻ không tồn tại trên server.");
            }

            try
            {
                // Lấy toàn bộ file trong folderShare và xóa hết
                var files = Directory.GetFiles(folderShare);
                foreach (var file in files)
                {
                    File.Delete(file);
                }

                //Nếu muốn xóa cả thư mục con:
                var dirs = Directory.GetDirectories(folderShare);
                foreach (var dir in dirs)
                {
                    Directory.Delete(dir, true);
                }

                return "Đã xóa tất cả file trong thư mục.";
            }
            catch (Exception ex)
            {
                return $"Lỗi khi xóa file: {ex.Message}";
            }
        }

        public async Task<string> DeleteManyDocs(List<Guid> ids)
        {
            try
            {
                if (ids == null || !ids.Any())
                    throw new Exception("Danh sách ID tài liệu trống!");

                var docs = await _context.tai_lieu.Where(x => ids.Contains(x.Id)).ToListAsync();
                if (docs == null || docs.Count == 0)
                    throw new Exception("Không tìm thấy tài liệu nào để xóa!");

                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";

                foreach (var doc in docs)
                {
                    string fullPath = Path.Combine(rootPath, doc.duong_dan);
                    string fullPathEncrypt = Path.Combine(rootPath, doc.duong_dan + ".encrypt");

                    if (File.Exists(fullPath))
                    {
                        try
                        {
                            File.Delete(fullPath);
                        }
                        catch (Exception fileEx)
                        {
                            // Không dừng toàn bộ tiến trình, chỉ log lỗi file
                            throw new Exception($"Không thể xóa file {fullPath}: {fileEx.Message}");
                        }
                    }
                    if (File.Exists(fullPathEncrypt))
                    {
                        try
                        {
                            File.Delete(fullPathEncrypt);
                        }
                        catch (Exception fileEx)
                        {
                            // Không dừng toàn bộ tiến trình, chỉ log lỗi file
                            throw new Exception($"Không thể xóa file {fullPath}: {fileEx.Message}");
                        }
                    }
                }

                // 4️⃣ Xóa các bản ghi trong DB
                _context.tai_lieu.RemoveRange(docs);
                await _context.SaveChangesAsync();

                var bulkResponse = await _client.BulkAsync(b => b
                    .Index("tai_lieu")
                    .DeleteMany(docs, (op, doc) => op.Id(doc.Id.ToString()))
                );

                if (bulkResponse.Errors)
                {
                    // Nếu có lỗi khi xóa trong ES → ghi chi tiết log
                    var errorItems = string.Join(", ",
                        bulkResponse.ItemsWithErrors.Select(i => $"{i.Id}:{i.Error.Reason}"));
                    Console.WriteLine($"[ES] Lỗi khi xóa document: {errorItems}");
                }

                return $"Đã xóa thành công {docs.Count} tài liệu!";
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi khi xóa nhiều tài liệu: {ex.Message}");
            }
        }

        public async Task<DownloadResult> GetDoc(Guid id)
        {
            try
            {
                // 1️⃣ Lấy thông tin tài liệu
                var taiLieu = _context.tai_lieu.FirstOrDefault(x => x.Id == id);
                if (taiLieu == null)
                {
                    throw new Exception("Không lấy được tài liệu.");
                }

                // 2️⃣ Đường dẫn file
                string rootPath = _config.GetSection("RootFileServer")["path"] ?? "";
                var filePath = Path.Combine(rootPath, taiLieu.duong_dan);
                var filePathEncrypt = Path.Combine(rootPath, taiLieu.duong_dan + ".encrypt");

                if (!System.IO.File.Exists(filePath))
                {
                    if (System.IO.File.Exists(filePathEncrypt))
                    {
                        filePath = filePathEncrypt;
                    }
                    else
                    {
                        throw new Exception("Không tìm thấy file.");
                    }
                }
                // giải mã file nếu là file mã hóa
                if (filePath.EndsWith(".encrypt"))
                {
                    string appCode = _config.GetSection("AppCode").Value ?? "";
                    var vaultUrl = _config.GetSection("Uri")["vault"] + "";
                    HybridEncryption.SetAppCode(appCode);
                    HybridEncryption.SetVaultUrl(vaultUrl);
                    string share = _config.GetSection("RootFileServer")["share"] ?? "";
                    var folderShare = Path.Combine(rootPath, share);
                    string pvKeyName = $"pvECC_key_{taiLieu.ten}_{taiLieu.EccKeyName}";
                    var receiverPrivateKey = await HybridEncryption.GetVaultSecretValue("NHCH", pvKeyName);
                    var decrypt = HybridEncryption.DecryptFileToStoring(filePath, folderShare, receiverPrivateKey);
                    filePath = decrypt.Result.outputFile;
                }

                // 3️⃣ Đọc file vào memory stream
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                // 4️⃣ Lấy ContentType
                var contentType = GetContentType(filePath);

                // 5️⃣ Ghi nhật ký hành động (nếu cần)
                await _logger.AddLog(new nhat_ky_he_thong_dto
                {
                    loai = 1,
                    detail = $"Xem trước tài liệu {taiLieu.ten}",
                    command = "PERM_PREVIEW",
                });
                await DeletePublicDocs();

                // 6️⃣ Trả kết quả
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

        public async Task<string> DecryptContent(string content)
        {
            try
            {
                var newString = HybridEncryption.DecryptStringToStoring(content);
                return newString.Result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
    }
}
