using DATN.Application.Interfaces;
using DATN.Application.TaiLieu;
using DATN.Application.Utils;
using DATN.Domain.DTO;
using DATN.Infrastructure.Data;
using DocumentFormat.OpenXml.Office2010.Excel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Application.Utils;

namespace DATN.Api.Controllers
{
    [Route("api/quan-ly-tai-lieu")]
    [ApiController]
    public class TaiLieuController : ControllerBase
    {
        private readonly ITaiLieuService _taiLieuService;
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        public TaiLieuController(ITaiLieuService taiLieuService, IConfiguration config, AppDbContext context)
        {
            _taiLieuService = taiLieuService;
            _config = config;
            _context = context;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles([FromForm] uploadedFileInfo request)
        {
            try
            {
                var result = await _taiLieuService.AddTaiLieu(request);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        [HttpGet("get-image")]
        public async Task<IActionResult> GetImage([FromQuery] Guid id)
        {
            var taiLieu = _context.tai_lieu.FirstOrDefault(t => t.Id == id);
            if(taiLieu == null)
            {
                return NotFound("File not found.");
            }
            if (string.IsNullOrWhiteSpace(taiLieu.duong_dan))
                return BadRequest("filePath is required.");

            // Ghép path đầy đủ nếu bạn lưu folder root trong config
            string rootPath = _config["RootFileServer:path"];
            string fullPath = Path.Combine(rootPath, taiLieu.duong_dan);
            if (fullPath.Trim().EndsWith(".encrypt"))
            {
                string appCode = _config.GetSection("AppCode").Value ?? "";
                var vaultUrl = _config.GetSection("Uri")["vault"] + "";
                HybridEncryption.SetAppCode(appCode);
                HybridEncryption.SetVaultUrl(vaultUrl);
                string share = _config.GetSection("RootFileServer")["share"] ?? "";
                var folderShare = Path.Combine(rootPath, share);
                string pvKeyName = $"pvECC_key_{taiLieu.ten}_{taiLieu.EccKeyName}";
                var receiverPrivateKey = await HybridEncryption.GetVaultSecretValue("NHCH", pvKeyName);
                var decrypt = HybridEncryption.DecryptFileToStoring(fullPath, folderShare, receiverPrivateKey);

                fullPath = decrypt.Result.outputFile.ToString();
            }
            if (!System.IO.File.Exists(fullPath))
                return NotFound("File not found.");

            var ext = Path.GetExtension(fullPath).ToLower();
            string contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(fullPath);
            await _taiLieuService.DeletePublicDocs();
            return File(fileBytes, contentType);
        }


        #region api lấy file không trong thư mục
        [HttpGet("get-all")]
        public async Task<PaginatedList<tai_lieu_dto>> GetAll([FromQuery] tai_lieu_dto request)
        {
            try
            {
                var result = await _taiLieuService.GetTaiLieuByPhanQuyen(request);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        
        #region api giải mã string
        [HttpPost("giai-ma")]
        public async Task<string> DecryptContent([FromBody] string DecryptContent)
        {
            try
            {
                var result = await _taiLieuService.DecryptContent(DecryptContent);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api lấy ds người dùng đã chia sẻ tài liệu với mình
        [HttpGet("get-nguoi-dung")]
        public async Task<List<nguoi_dung_dto>> GetAllNguoiDung([FromQuery] Guid nguoi_dung_id)
        {
            try
            {
                var result = await _taiLieuService.GetAllNguoiDungByDocs(nguoi_dung_id);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api lấy pagin file + thư mục theo điều kiện search
        [HttpGet("advanced-search")]
        public async Task<PaginatedList<ResultSearch>> Query([FromQuery] ResultSearchParams requests)
        {
            try
            {
                var result = await _taiLieuService.GetDataSearch(requests);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api download tài liệu
        [HttpGet("download")]
        public async Task<IActionResult> Download([FromQuery] Guid tai_lieu_id)
        {
            try
            {
                var result = await _taiLieuService.HandleDownloadTaiLieu(tai_lieu_id);
                return File(result.Stream, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api download tài liệu
        [HttpGet("get-docs-by-folder")]
        public async Task<PaginatedList<ResultSearch>> GetDocsByFolder([FromQuery] ResultSearchParams requests)
        {
            try
            {
                var result = await _taiLieuService.GetDocsByFolder(requests);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api chia sẻ tài liệu
        [HttpPost("chia-se-tai-lieu")]
        public async Task<string> HandleShareFile(ShareFileParams requests)
        {
            try
            {
                var result = await _taiLieuService.HandleShareFile(requests);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        
        #region api sửa tên tài liệu
        [HttpPut("sua-ten-file")]
        public async Task<string> HandleChangeName(ChangenameParams requests)
        {
            try
            {
                var result = await _taiLieuService.HandleChangeName(requests);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api sửa tên tài liệu
        [HttpDelete("xoa-file")]
        public async Task<string> HandleDeleteFile(Guid id)
        {
            try
            {
                var result = await _taiLieuService.DeleteDocs(id);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api sửa tên tài liệu
        [HttpDelete("xoa-nhieu-file")]
        public async Task<string> HandleDeleteManyFile(List<Guid> ids)
        {
            try
            {
                var result = await _taiLieuService.DeleteManyDocs(ids);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api sửa tên tài liệu
        [HttpGet("preview")]
        public async Task<IActionResult> Preview(Guid id)
        {
            try
            {
                var result = await _taiLieuService.GetDoc(id);
                return File(result.Stream, result.ContentType, result.FileName);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region api lấy file không trong thư mục
        [HttpGet("delete-public-file")]
        public async Task<string> DeletePublicDocs()
        {
            try
            {
                var result = await _taiLieuService.DeletePublicDocs();
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region tạo mật khẩu cho tài liệu
        [HttpPut("set-password")]
        public async Task<string> SetPassword([FromQuery] Guid tai_lieu_id, [FromBody] string password)
        {
            try
            {
                var result = await _taiLieuService.SetPasswordForDocument(password, tai_lieu_id);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
        
        #region Check mật khẩu tài liệu
        [HttpGet("check-password-doc")]
        public async Task<bool> CheckPassword([FromQuery] Guid tai_lieu_id, [FromQuery] string password)
        {
            try
            {
                var result = await _taiLieuService.CheckPassword(password, tai_lieu_id);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion

        #region Check mật khẩu tài liệu
        [HttpGet("check-has-password")]
        public async Task<bool> CheckHasPassword([FromQuery] Guid tai_lieu_id)
        {
            try
            {
                var result = await _taiLieuService.CheckHasPassword(tai_lieu_id);
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        #endregion
    }
}
