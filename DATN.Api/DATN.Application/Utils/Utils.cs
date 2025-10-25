using MySqlConnector;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace SharedKernel.Application.Utils
{
    public static class Common
    {
        public static async Task<string> RunCommand(string command)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo("cmd.exe", "/c " + command)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    Verb = "runas", // Request admin privileges
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(psi))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    // Chờ quá trình hoàn thành
                    await process.WaitForExitAsync(); // Sử dụng WaitForExitAsync để hỗ trợ bất đồng bộ
                    return process.ExitCode == 0 ? output : ""; // Trả về true nếu lệnh thành công
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi khi chạy lệnh: {ex.Message}");
                return ex.Message;
            }
        }
        public static void RunPowershell(string command)
        {
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = command,
                Verb = "runas", // Request admin privileges
                UseShellExecute = false,
                CreateNoWindow = true
            };
            Process.Start(psi);
        }
        public static async Task<string> RunBashCommand(string command)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Đọc async để tránh deadlock
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0 && !string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Command failed with exit code {process.ExitCode}");
                Console.WriteLine($"Command: {command}");
                Console.WriteLine($"Error: {error}");
            }

            // Trả về output ngay cả khi có exitcode != 0, vì MySQL có thể có warning nhưng vẫn trả về data
            return string.IsNullOrEmpty(output) ? "" : output.Trim();
        }
        public static bool DatabaseChangePassword(string connStr, string mysqlUser, string newPassword, string oldPassword)
        {
            try
            {
                //RunCommand("net stop mysql");
                //RunCommand("mysqld --skip-grant-tables --skip-networking");
                using (var conn = new MySqlConnection(connStr))
                {
                    conn.Open();

                    using (var cmd = new MySqlCommand($"ALTER USER '{mysqlUser}'@'localhost' IDENTIFIED BY '{newPassword}';", conn))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                //RunCommand("net start mysql");
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public static async Task<bool> DecryptFolder()
        {
            return true;
        }
        
        public class LargeSqlFileMerger
        {
            public void MergeLargeSqlFiles(string[] inputFiles, string outputFile, int bufferSize = 81920)
            {
                // Sắp xếp files theo thứ tự mong muốn
                var orderedFiles = inputFiles.OrderBy(f => f).ToArray();

                // Mở file output với FileStream và StreamWriter
                using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize))
                using (var writer = new StreamWriter(outputStream))
                {
                    foreach (var file in orderedFiles)
                    {
                        // Ghi header để phân biệt các file
                        writer.WriteLine($"-- START OF FILE: {Path.GetFileName(file)}");
                        writer.Flush();

                        // Đọc file input với FileStream và StreamReader
                        using (var inputStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize))
                        using (var reader = new StreamReader(inputStream))
                        {
                            char[] buffer = new char[bufferSize];
                            int bytesRead;

                            while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                writer.Write(buffer, 0, bytesRead);
                                writer.Flush(); // Đảm bảo dữ liệu được ghi ngay
                            }
                        }

                        writer.WriteLine($"\n-- END OF FILE: {Path.GetFileName(file)}\n");
                        writer.Flush();
                    }
                }
            }
        }
        public static object? getJValue(object result, String key)
        {
            try
            {
                foreach (var keyValuePair in JObject.Parse(Convert.ToString(result) ?? "{}"))
                {
                    if (key == keyValuePair.Key)
                    {
                        return keyValuePair.Value;
                    }
                }
                return null;
            }
            catch
            {
                object? cellValue = result;
                PropertyInfo[] properties = result.GetType().GetProperties();
                foreach (var property in properties)
                {
                    if (property.Name.Equals(key, StringComparison.OrdinalIgnoreCase) && property != null && property.CanRead)
                    {
                        cellValue = property.GetValue(result, null);
                        return cellValue;
                    }
                }
                return null;
            }
        }
        public static void saveList2JsonFile(string filePath, List<dynamic> lstData)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true // Make the JSON readable
            };

            string jsonString = System.Text.Json.JsonSerializer.Serialize(lstData, options);
            //string jsonString = JsonSerializer.Serialize(list, options);

            File.WriteAllText(filePath, jsonString, new UTF8Encoding()); // Save to file
        }
        public static async Task<string> ImportDatabaseFromJsonFile(string filePath, string strFormat, string connectionString, string tableName)
        {
            string strResult = "";
            if (!string.IsNullOrEmpty(filePath)) filePath = SharedKernel.Application.Utils.Common.NormalizedPathChar(filePath);
            if (System.IO.File.Exists(filePath))
            {
                string jsonData = System.IO.File.ReadAllText(filePath);

                // Parse JSON dynamically
                using (JsonDocument doc = JsonDocument.Parse(jsonData))
                {
                    JsonElement root = doc.RootElement;

                    if (root.ValueKind == JsonValueKind.Array)
                    {
                        List<string> columns = new List<string>();
                        List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();

                        foreach (JsonElement element in root.EnumerateArray())
                        {
                            Dictionary<string, object> rowData = new Dictionary<string, object>();

                            foreach (JsonProperty prop in element.EnumerateObject())
                            {
                                if (!columns.Contains(prop.Name))
                                    columns.Add(prop.Name);

                                rowData[prop.Name] = prop.Value.ValueKind switch
                                {
                                    JsonValueKind.String => prop.Value.GetString(),
                                    JsonValueKind.Number => prop.Value.GetDouble(),
                                    JsonValueKind.True => true,
                                    JsonValueKind.False => false,
                                    JsonValueKind.Null => DBNull.Value,
                                    _ => prop.Value.ToString()
                                };
                            }
                            rows.Add(rowData);
                        }

                        await HybridEncryption.InsertDataIntoMySQL(connectionString, tableName, columns, rows);

                        strResult += string.Format(strFormat, tableName, rows.Count);
                    }
                }
            }
            return strResult;
        }

        public static string CreateUniqueTempDirectory()
        {
            try
            {
                var uniqueTempDir = SharedKernel.Application.Utils.Common.NormalizedPathChar(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
                Directory.CreateDirectory(uniqueTempDir);
                return uniqueTempDir;
            }
            catch
            {
                return Path.GetTempPath();
            }
        }

        public static void CreateTreeDirectory(string rootDriver, string dirPath)
        {
            var treeFolders = dirPath.Replace("/", "\\").Split('\\').ToList();
            var subFolder = rootDriver;
            treeFolders.ForEach(folder =>
            {
                subFolder = SharedKernel.Application.Utils.Common.NormalizedPathChar(Path.Combine(subFolder, folder));
                if (!Directory.Exists(subFolder))
                {
                    try
                    {
                        Directory.CreateDirectory(subFolder);
                    }
                    catch { }
                }
            });
        }

        public static async Task<bool> WaitForBackendAsync(string url, int maxRetries = 50, int delaySeconds = 10)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                Random random = new Random();
                int min = 2;
                int max = 20;

                // Generate a random number between 'min' and 'max' (inclusive)
                int randomNumber = random.Next(min, max + 1);
                // wait truoc de tranh nhieu service goi Vault luc dau lam Vault kg khoi dong dc
                if (i > 0)
                    await Task.Delay(TimeSpan.FromSeconds((i == 0 ? 30 : delaySeconds) + randomNumber));
                else
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds));

                try
                {
                    using HttpClient client = new HttpClient();
                    var response = await client.GetAsync(url);
                    if (response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.InternalServerError || response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        return true;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    // Ignore exceptions (e.g., connection refused)
                }
                Console.WriteLine("CHecking " + url);
            }

            return false;
        }

        /*
        public static async Task<bool> ResetDatabaseTables(IBaseDbContext _context, ILogger _logger, List<string> tables, List<string> addingSQL)
        {
            const int maxRetries = 10;
            bool truncated = false;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var transaction = await _context.BeginTransactionAsync();
                try
                {
                    // Tắt kiểm tra khóa ngoại để tránh lỗi ràng buộc
                    await _context.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");
                    try
                    {
                        foreach (var table in tables)
                        {
                            await _context.ExecuteSqlRawAsync("TRUNCATE TABLE `" + table + "`");
                        }
                        foreach (var sql in addingSQL)
                        {
                            await _context.ExecuteSqlRawAsync(sql);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    finally
                    {
                        // Bật lại kiểm tra khóa ngoại
                        await _context.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
                    }
                    await _context.CommitTransactionAsync();

                    truncated = true;
                    break;
                }
                catch (Exception ex)
                {
                    await _context.RollbackTransactionAsync();
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, $"Lỗi truncate các bảng sau {maxRetries} lần thử: {ex.Message}");
                        throw new InvalidOperationException($"Lỗi truncate các bảng sau {maxRetries} lần thử: {ex.Message}", ex);
                    }
                }

                await Task.Delay(1000 * attempt);
                //}
                return truncated;
            }

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                using var transaction = await _context.BeginTransactionAsync();
                try
                {
                    // Tắt kiểm tra khóa ngoại để tránh lỗi ràng buộc
                    await _context.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 0");
                    try
                    {
                        foreach (var table in tables)
                        {
                            await _context.ExecuteSqlRawAsync("DELETE FROM `" + table + "`");
                        }
                        foreach (var sql in addingSQL)
                        {
                            await _context.ExecuteSqlRawAsync(sql);
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        // Bật lại kiểm tra khóa ngoại
                        await _context.ExecuteSqlRawAsync("SET FOREIGN_KEY_CHECKS = 1");
                    }
                    await _context.CommitTransactionAsync();

                    truncated = true;
                    break;
                }
                catch (Exception ex)
                {
                    await _context.RollbackTransactionAsync();
                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, $"Lỗi truncate các bảng sau {maxRetries} lần thử: {ex.Message}");
                        throw new InvalidOperationException($"Lỗi truncate các bảng sau {maxRetries} lần thử: {ex.Message}", ex);
                    }
                }

                await Task.Delay(1000 * attempt);
                //}
                return truncated;
            }

            return true;
        }
        */
        public static string NormalizedPathChar(string rawPath)
        {
            var normalized = rawPath.Replace('\\', Path.DirectorySeparatorChar);
            return normalized;
        }
        //Export tables
        //Import tables

    }

    public static class NetworkChecker
    {
        public static bool IsConnectedToLANOnly()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni =>
                    ni.OperationalStatus == OperationalStatus.Up &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .ToList();

            foreach (var ni in interfaces)
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                    ni.Description.ToLower().Contains("bluetooth"))
                {
                    // Wi-Fi or Bluetooth found
                    return false;
                }
            }

            try
            {
                using (var httpClient = new HttpClient())
                {
                    var response = httpClient.GetStringAsync("https://api.ipify.org?format=json").Result;
                    return !response.ToString().Contains("ip\":");
                }
            }
            catch (Exception ex) { }

            // Only wired or acceptable interfaces found
            return true;
        }
    }

}
