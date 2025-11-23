using DATN.Application.Utils;
using DocumentFormat.OpenXml.Office2013.Drawing.ChartStyle;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SharedKernel.Application.Utils;
using System.Reflection;

namespace DATN.Api.Controllers
{
    [Route("api/file/tep-tin")]
    [ApiController]
    public class TepTinController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public TepTinController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        [Route("get-file")]
        public async Task<ActionResult> GetFile([FromQuery] string filePath, [FromQuery] bool downloadByte = true)
        {
            try
            {
                string rootPath = _configuration.GetSection("RootFileServer")["path"] ?? ""; //get cau hinh folder file server
                string userserver = _configuration.GetSection("RootFileServer")["username"] ?? "";
                string pwdserver = _configuration.GetSection("RootFileServer")["pwd"] ?? "";
                string drive = _configuration.GetSection("RootFileServer")["drive"] ?? "";
                NetworkDrive nd = new NetworkDrive();
                nd.MapNetworkDrive(rootPath, drive, userserver, pwdserver);

                string preview = _configuration.GetSection("RootFileServer")["preview"] ?? "";

                if (filePath.IndexOf("/") == 0) filePath = filePath.Substring(1);
                var fullPath = SharedKernel.Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(rootPath, preview, filePath));
                if (!System.IO.File.Exists(fullPath))
                {
                    fullPath = fullPath = ".encrypt";
                    if (!System.IO.File.Exists(fullPath))
                    {
                        throw new Exception("File không tồn tại trên hệ thống");    
                    }
                }

                // giai ma de doc noi dung anh
                try
                {
                    var appCode = _configuration.GetSection("AppCode").Value + "";
                    var vaultUrl = _configuration.GetSection("Uri")["vault"] + "";
                    HybridEncryption.SetAppCode(appCode, vaultUrl);
                    fullPath = (await HybridEncryption.DecryptFileToStoring(fullPath, Path.GetTempPath())).outputFile;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                if (!System.IO.File.Exists(fullPath))
                {
                    var rootPathWWWRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "\\wwwroot";
                    if (rootPathWWWRoot.Contains("\\bin\\Debug\\"))
                    {
                        rootPathWWWRoot = rootPathWWWRoot.Split("\\bin\\Debug\\")[0] + "\\wwwroot";
                    }
                    fullPath = SharedKernel.Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(rootPathWWWRoot, filePath));
                }

                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
                // Retrieve the MIME type based on file extension
                var mimeType = GetMimeTypeByFileExtension(Path.GetExtension(filePath));

                // Calculate the size of the chunk to read
                var buffer = new byte[fileStream.Length];
                fileStream.Seek(0, SeekOrigin.Begin);
                var bytesRead = fileStream.Read(buffer, 0, (int)fileStream.Length);
                // Send the chunk with the total file length
                if (downloadByte == true)
                {
                    return Ok(new
                    {
                        FileName = Path.GetFileName(filePath),
                        TotalLength = fileStream.Length,
                        Offset = 0,
                        ChunkSize = bytesRead,
                        Data = Convert.ToBase64String(buffer) // Optional: encode in Base64 for transport
                    });
                }
                else
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileContent = buffer;
                    // Return the file
                    return File(fileContent, mimeType, fileName);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }
        private string GetMimeTypeByFileExtension(string extension)
        {
            // Simple dictionary of common MIME types; extend as needed
            return extension.ToLower() switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".jpg" => "image/jpeg",
                ".png" => "image/png",
                ".txt" => "text/plain",
                _ => "application/octet-stream", // Default binary type
            };
        }
    }
}
