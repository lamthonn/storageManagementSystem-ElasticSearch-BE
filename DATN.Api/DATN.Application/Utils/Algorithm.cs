using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Dynamic;
using System.IO.MemoryMappedFiles;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace SharedKernel.Application.Utils;

public static class MD5algorithm
{
    static public string GetMd5Hash(string input)
    {
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] data = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(input));
            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();
            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }
            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
    static public string GetMd5Hash(string input, string salt)
    {
        var result = GetMd5Hash(GetMd5Hash(GetMd5Hash(input) + salt));
        return result;
    }
    // Verify a hash against a string.
    static public bool VerifyMd5Hash(string input, string hash, string salt)
    {
        var result = GetMd5Hash(GetMd5Hash(GetMd5Hash(input) + salt));
        // Hash the input.
        string hashOfInput = GetMd5Hash(input, salt);
        // Create a StringComparer an compare the hashes.
        StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        if (0 == comparer.Compare(hashOfInput, hash))
        {
            return true;
        }
        else
        {
            return false;
        }
    }
}
public static class SHA256algorithm
{
    public static string GetSHA256Hash(string input)
    {

        SHA256 hashAlgorithm = SHA256.Create();
        byte[] byteValue = System.Text.Encoding.UTF8.GetBytes(input);
        byte[] byteHash = hashAlgorithm.ComputeHash(byteValue);
        return Convert.ToBase64String(byteHash);
    }
}
public static class Salt
{
    public static string GetCode(int lengthOfByte)
    {
        var buffer = RandomNumberGenerator.GetBytes(lengthOfByte);
        string salt = BitConverter.ToString(buffer);
        salt = salt.Replace("-", string.Empty);
        return salt;
    }
}
public static class RandomString
{
    const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    public static string GenerateId(int length)
    {
        var randomBytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        return new string(randomBytes.Select(b => chars[b % chars.Length]).ToArray());
    }
}

public static class HybridEncryption
{
    private static ILogger _iLogger;

    public static void SetILogger(ILogger logger)
    {
        _iLogger = logger;
    }


    /* 
    
1. Elliptic Curve Diffie-Hellman (ECDH) – To securely derive a shared secret.
2. AES-GCM (Symmetric Encryption) – To efficiently encrypt large files.
3️. RSA (Asymmetric Encryption) – To encrypt the AES key securely.

- Fast: AES-GCM encrypts large files quickly.
- Secure: ECDH + RSA ensures only the receiver can decrypt.
- One-Way: The sender can’t decrypt because they don’t have the private keys.

To ensure data integrity and detect if a file has been modified or tampered with, you can:

1. Compute a cryptographic hash (SHA-256 or SHA-512) of the original file before encryption.
2️. Store the hash securely along with the encrypted file.
3️. Recompute the hash after decryption and compare it to detect changes.

Sending:
    - CapMa (Global: pvECC, pbECC, pvRSA, pbRSA) => PM Sender (pbECC, pbRSA) => PM Receiver (pvECC, pvRSA)
Storing:
    - pvEccLocal, pbECCLocal khi init/reset du lieu, empheral khi ma hoa 

    VAULT_URL:      http://192.168.1.165:8200

    CapMa:
    - tao ban ghi theo moi dia phuong, tao cap ma pv, pb
    - nhan CD/MSG
    - giai ma theo pb nhan dc: iv trong dau file, iv trong dau string
    - check hash
    - check MSG
    - show UI reply MSG
    - show UI list folder/file
    - gui thong tin cap ma
    - gui MSG

    App:
    - doc ENV Vault token, url
    - doc Vault pwd DB ket noi DB
    - ma hoa string RW DB
    - ma hoa file
    - doc file: refresh token, encrypt name => file name + data
    - ghi file: ma hoa ten file, data => save encrypt name vao DB

    ImpExpApp:
    - loai app
    - import browse folder CD: giai ma, check hash, doc file cau hinh, doc info init ma hoa luu ENV, Vault, doc json luu DB, ma hoa moi DB
        - CapMa: MSG, CD, pb
        - NHCH: ket qua thi thu json table, pub
        - ChamThi: ket qua cham json table, pub
        - ThiTN: de thi, thi sinh json table, pub
    - export browse folder save: loai app => export data
        - CapMa: id, madp, pv, pb, pub
        - NHCH: json dethi, pdf dethi, diemdung json table, pub
        - ChamThi: CD json table, pub
        - ThiTN: json table, pub

    DB:
    - noi dung cau hoi, tra loi => ma hoa khi W DB, giai ma khi R DB tren BE
    - ID FK => tao bang FK voi ID parent + encrypted ID con => doc cha giai ma con va doc list con tren BE

     */

    // Generate ECC key pair
    public static (byte[] Private, byte[] Public) GenerateECCKey()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return (ecdh.ExportECPrivateKey(), ecdh.ExportSubjectPublicKeyInfo());
    }

    public static (byte[] PrivateKey, byte[] PublicKey) GenerateECCKeyForSign()
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        return (ecdsa.ExportECPrivateKey(), ecdsa.ExportSubjectPublicKeyInfo());
    }

    // Generate RSA key pair
    public static (byte[] Private, byte[] Public) GenerateRSAKey()
    {
        using (RSA rsa = RSA.Create(2048))
        {
            byte[] publicKey = rsa.ExportRSAPublicKey();
            byte[] privateKey = rsa.ExportRSAPrivateKey();
            return (rsa.ExportRSAPrivateKey(), rsa.ExportRSAPublicKey());
        }
    }

    private static int aesBits = 256; // or 128, 192

    public static void SetAesGcmKeySizeBits(int keySize)
    {
        if (!(keySize != 128 && keySize != 192 && keySize != 256)) aesBits = keySize;
    }

    private static byte[] AesGcmCustomKeySizeBits(byte[] sharedSecret, int _keySize)
    {
        // For AES-256, use full SHA256. For AES-128, take first 16 bytes
        using var sha256 = SHA256.Create();
        byte[] fullHash = sha256.ComputeHash(sharedSecret);
        byte[] aesKey = fullHash[..(_keySize / 8)];

        return aesKey;
    }

    // AES-GCM Encryption
    private static byte[] AesGcmEncrypt(byte[] key, byte[] plainText)
    {
        using var aes = new AesGcm(key);
        var nonce = new byte[12];
        RandomNumberGenerator.Fill(nonce);
        var cipherText = new byte[plainText.Length];
        var tag = new byte[16];

        aes.Encrypt(nonce, plainText, cipherText, tag);

        using var ms = new MemoryStream();
        ms.Write(nonce, 0, nonce.Length);
        ms.Write(tag, 0, tag.Length);
        ms.Write(cipherText, 0, cipherText.Length);

        return ms.ToArray();
    }

    // AES-GCM Decryption
    private static byte[] AesGcmDecrypt(byte[] key, byte[] cipherData)
    {
        using var aes = new AesGcm(key);
        var nonce = cipherData[..12];
        var tag = cipherData[12..28];
        var cipherText = cipherData[28..];
        var plainText = new byte[cipherText.Length];

        aes.Decrypt(nonce, cipherText, tag, plainText);
        return plainText;
    }

    private static string GetUnique24CharString(string input)
    {
        if (string.IsNullOrEmpty(input)) return null;
        using (var sha256 = SHA256.Create())
        {
            // Hash the input string
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Convert to Base64 and take first 24 chars (Base64 = compact readable format)
            var base64 = Convert.ToBase64String(bytes);

            // Remove special chars from Base64 to make it file-safe or ID-safe
            var cleaned = base64.Replace("+", "").Replace("/", "").Replace("=", "");

            // Return exactly 24 chars
            return cleaned.Substring(0, 24);
        }
    }
    public static string SimpleEncrypt(string plainText, string token = null)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            byte[] keyBytes = Encoding.UTF8.GetBytes(GetUnique24CharString(token) ?? "GConnect-GConnect-HT-THI");
            byte[] iv = new byte[16]; // Default zero IV (use a random IV for better security)

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;

                using (var encryptor = aes.CreateEncryptor())
                using (var ms = new MemoryStream())
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var writer = new StreamWriter(cs))
                {
                    writer.Write(plainText);
                    writer.Flush();
                    cs.FlushFinalBlock();
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            WriteLogError(GetUnique24CharString(token) ?? "GConnect-GConnect-HT-THI xxxxxxxxxxxxx" + ex.Message, ex);
        }
        return plainText;
    }

    public static string SimpleDecrypt(string plainText, string token = null)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText) || !IsBase64String(plainText)) return plainText;
            byte[] cipherBytes = Convert.FromBase64String(plainText);
            byte[] keyBytes = Encoding.UTF8.GetBytes(GetUnique24CharString(token) ?? "GConnect-GConnect-HT-THI");
            byte[] iv = new byte[16]; // Default zero IV (use actual IV if available)

            using (Aes aes = Aes.Create())
            {
                aes.Key = keyBytes;
                aes.IV = iv;
                aes.Padding = PaddingMode.PKCS7;

                using (var decryptor = aes.CreateDecryptor())
                using (var ms = new MemoryStream(cipherBytes))
                using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                using (var reader = new StreamReader(cs))
                {
                    return reader.ReadToEnd();
                }
            }
        }
        catch (Exception ex)
        {
            WriteLogError(ex.Message, ex);
        }
        return plainText;
    }

    // Luu ten file base64 khong co ky tu dac biet cua base64
    public static string Base64ToFileName(string base64)
    {
        // \/:*?<>|
        return base64.Replace("/", "{1}").Replace("+", "{2}").Replace("\\", "{3}").Replace(":", "{4}").Replace("*", "{5}").Replace("?", "{6}").Replace("<", "{7}").Replace(">", "{8}").Replace("|", "{9}");
    }

    // Khoi phuc lai ten file luu thanh base64
    public static string FileNameFromBase64(string fileName)
    {
        // \/:*?<>|
        return fileName.Replace("{1}", "/").Replace("{2}", "+").Replace("{3}", "\\").Replace("{4}", ":").Replace("{5}", "*").Replace("{6}", "?").Replace("{7}", "<").Replace("{8}", ">").Replace("{9}", "|");
    }

    // tach hash ra file moi, neu file cu van co hash thi giu + ma hoa = ECC cu
    private static async Task<string> PrependBytesToFileAsync(string filePath, byte[] hash, string tick)
    {
        if (!string.IsNullOrEmpty(filePath)) filePath = Application.Utils.Common.NormalizedPathChar(filePath);
        string tempFile = filePath + ".temp";

        using (var output = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None, 5 * 1024 * 1024))
        {
            // Write the hash first
            output.Write(hash, 0, hash.Length);

            // Append the original file content
            using (var input = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 5 * 1024 * 1024))
            {
                input.CopyTo(output);
            }
        }

        // Replace the original file with the temp file
        if (File.Exists(filePath)) System.IO.File.Delete(filePath);

        // new file Name co hash
        var fileNameByte = UTF8Encoding.UTF8.GetBytes(Path.GetFileName(filePath).Replace("." + tick, ""));
        var hashAndName = hash.ToList();
        hashAndName.AddRange(fileNameByte.ToList());
        var decryptFileExtension = Convert.ToBase64String(hashAndName.ToArray());
        // ma hoa ten file voi hash
        //filePath = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace("." + tick, "." + Base64ToFileName(SimpleEncrypt(decryptFileExtension)))));
        //yc moi bo khoi ten file
        filePath = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileName(filePath).Replace("." + tick, ".encrypt")));
        //System.IO.File.WriteAllText(filePath.Replace(".encrypt", ".hash"), Base64ToFileName(SimpleEncrypt(decryptFileExtension)));
        try
        {
            File.Move(tempFile, filePath, true);
        }
        catch (Exception ex)
        {
            WriteLogError(ex.Message, ex);
        }
        if (File.Exists(tempFile)) HybridEncryption.DeleteSecureFile(tempFile);

        return filePath;
    }
    // ----------------------

    public static async Task<string> SetEnvironmentSecretValue(string name, string value)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = "setx " + name + " \"" + (value) + "\"  ",
            Verb = "runas", // Request admin privileges
            UseShellExecute = false
        };
        Process.Start(psi);
        return "";
    }

    public static async Task<string> GetEnvironmentSecretValue(string name)
    {
        // get from Environment
        // descrypt simple
        return SimpleDecrypt(Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User));
    }

    public static async Task<string> GetVaultToken()
    {
        // get from Environment
        // descrypt simple
        string token = SimpleDecrypt(Environment.GetEnvironmentVariable("VAULT_TOKEN", EnvironmentVariableTarget.User));

        return token;
    }

    private static ConcurrentDictionary<string, string> vaultDict = new ConcurrentDictionary<string, string>();
    private static object lockObject = new object();

    public static void InitVaultService()
    {
        var appCode = "VAULT";
        var key = "pvECCLocal";
        string jsonString0 = "";
        // doc tu file
        var rootPathWWWRoot = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location) + "\\data";
        if (rootPathWWWRoot.Contains("\\bin\\Debug\\"))
        {
            rootPathWWWRoot = rootPathWWWRoot.Split("\\bin\\Debug\\")[0] + "\\data";
        }
        if (!System.IO.Directory.Exists(rootPathWWWRoot))
        {
            System.IO.Directory.CreateDirectory(rootPathWWWRoot);
        }
        rootPathWWWRoot = Application.Utils.Common.NormalizedPathChar(rootPathWWWRoot);
        Console.WriteLine(rootPathWWWRoot);
        var filePath = System.IO.Directory.GetFiles(rootPathWWWRoot, appCode + "." + key + ".vault").FirstOrDefault() ?? Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPathWWWRoot, (appCode + "." + key + ".vault")));
        if (!System.IO.File.Exists(filePath))
        {
            var senderKeyPair = HybridEncryption.GenerateECCKey();

            lock (lockObject)
            {
                vaultDict.TryAdd(appCode + "." + "pvECCLocal", Convert.ToBase64String(senderKeyPair.Private).ToString());
                vaultDict.TryAdd(appCode + "." + "pbECCLocal", Convert.ToBase64String(senderKeyPair.Public).ToString());
            }

            if (true)
            {
                key = "pvECCLocal";
                var payload = new
                {
                    data = new { value = HybridEncryption.SimpleEncrypt(Convert.ToBase64String(senderKeyPair.Private).ToString()) }
                };
                var json = JsonConvert.SerializeObject(payload);
                filePath = System.IO.Directory.GetFiles(rootPathWWWRoot, appCode + "." + key + ".vault").FirstOrDefault() ?? Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPathWWWRoot, (appCode + "." + key + ".vault")));
                System.IO.File.WriteAllText(filePath, json, Encoding.UTF8); // Write JSON to file
            }
            if (true)
            {
                key = "pbECCLocal";
                var payload = new
                {
                    data = new { value = HybridEncryption.SimpleEncrypt(Convert.ToBase64String(senderKeyPair.Public).ToString()) }
                };
                var json = JsonConvert.SerializeObject(payload);
                filePath = System.IO.Directory.GetFiles(rootPathWWWRoot, appCode + "." + key + ".vault").FirstOrDefault() ?? Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPathWWWRoot, (appCode + "." + key + ".vault")));
                System.IO.File.WriteAllText(filePath, json, Encoding.UTF8); // Write JSON to file
            }
        }
        else
        {
            string token = GetVaultToken().Result; // Replace with your token

            key = "pvECCLocal";
            if (!vaultDict.ContainsKey(appCode + "." + key))
            {
                filePath = System.IO.Directory.GetFiles(rootPathWWWRoot, appCode + "." + key + ".vault.*").FirstOrDefault() ?? Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPathWWWRoot, (appCode + "." + key + ".vault")));

                string jsonString = System.IO.File.ReadAllText(filePath, Encoding.UTF8); // Read JSON from file
                var jsonData = JsonConvert.DeserializeObject<dynamic>(jsonString);
                lock (lockObject)
                {
                    try
                    {
                        vaultDict[appCode + "." + key] = SimpleDecrypt(jsonData["data"]["data"].ToObject<Dictionary<string, string>>()["value"]);
                    }
                    catch (Exception ex0)
                    {
                        try
                        {
                            vaultDict[appCode + "." + key] = SimpleDecrypt(jsonData["data"].ToObject<Dictionary<string, string>>()["value"]);
                        }
                        catch (Exception ex1)
                        {
                            var jsonDataFake = JsonConvert.DeserializeObject<DataVault>(jsonString);
                            if (jsonDataFake != null) vaultDict[appCode + "." + key] = SimpleDecrypt(jsonDataFake.data + "");
                        }
                    }
                }
            }
            key = "pbECCLocal";
            if (!vaultDict.ContainsKey(appCode + "." + key))
            {
                filePath = System.IO.Directory.GetFiles(rootPathWWWRoot, appCode + "." + key + ".vault.*").FirstOrDefault() ?? Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPathWWWRoot, (appCode + "." + key + ".vault")));

                string jsonString = System.IO.File.ReadAllText(filePath, Encoding.UTF8); // Read JSON from file
                var jsonData = JsonConvert.DeserializeObject<dynamic>(jsonString);
                lock (lockObject)
                {
                    try
                    {
                        vaultDict[appCode + "." + key] = SimpleDecrypt(jsonData["data"]["data"].ToObject<Dictionary<string, string>>()["value"]);
                    }
                    catch (Exception ex0)
                    {
                        try
                        {
                            vaultDict[appCode + "." + key] = SimpleDecrypt(jsonData["data"].ToObject<Dictionary<string, string>>()["value"]);
                        }
                        catch (Exception ex1)
                        {
                            var jsonDataFake = JsonConvert.DeserializeObject<DataVault>(jsonString);
                            if (jsonDataFake != null) vaultDict[appCode + "." + key] = SimpleDecrypt(jsonDataFake.data + "");
                        }
                    }
                }
            }
        }
    }

    // get tu API + giai ma, hoac tu ConcurrentDict
    public static async Task<string> GetVaultSecretValue(string app, string name, bool debug = false)
    {
        if (vaultDict.ContainsKey(app + "." + name))
        {
            vaultDict.TryGetValue(app + "." + name, out string val);
            return SimpleDecrypt(val);
        }
        string token = await GetVaultToken(); // Replace with your token

        lock (lockObject)
        {
            VaultUrl = "http://gconnect-host.hopto.org:58200";
            // 1. Vault Server Address (Ensure it's accessible from Docker)
            string vaultUrl = VaultUrl + "/v1/GConnect/data/" + app + "." + name;

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("X-Vault-Token", token);

            try
            {
                var response = client.GetAsync(vaultUrl).Result;
                string result = response.Content.ReadAsStringAsync().Result;
                //{"request_id":"dcbc2551-cbd3-ddc3-01c9-fb7e101d7bd3","lease_id":"","renewable":false,"lease_duration":0,"data":{"data":{"dbpwd":"password"},"metadata":{"created_time":"2025-03-02T08:17:17.123315057Z","custom_metadata":null,"deletion_time":"","destroyed":false,"version":1}},"wrap_info":null,"warnings":null,"auth":null,"mount_type":"kv"}
                var jsonData = JsonConvert.DeserializeObject<dynamic>(result);
                try
                {
                    vaultDict.TryAdd(app + "." + name, SimpleDecrypt(jsonData["data"]["data"].ToObject<Dictionary<string, string>>()["value"]));
                    vaultDict.TryGetValue(app + "." + name, out string val);
                    return val;
                }
                catch (Exception ex0)
                {
                    WriteLogError(app + " - " + name + " - " + ex0.Message + JsonConvert.SerializeObject(jsonData), ex0);
                    if (debug == true) return ex0.Message + ex0.StackTrace;
                    var jsonDataFake = JsonConvert.DeserializeObject<DataVault>(result);
                    if (jsonDataFake == null) return "";
                    vaultDict.TryAdd(app + "." + name, SimpleDecrypt(jsonDataFake.data + ""));
                    vaultDict.TryGetValue(app + "." + name, out string val);
                    return val;
                }

            }
            catch (Exception ex)
            {
                WriteLogError(app + " - " + name + " - " + ex.Message, ex);
                if (debug == true) return ex.Message + ex.StackTrace;
                if (vaultDict.ContainsKey(app + "." + name))
                {
                    vaultDict.TryGetValue(app + "." + name, out string val);
                    return val;
                }
                else
                    return null;
            }
        }
    }


    //name tên của key
    public static async Task<string> SetVaultSecretValue(string app, string name, string key, object value)
    {
        // 1. Vault Server Address (Ensure it's accessible from Docker)
        string vaultUrl = VaultUrl + "/v1/GConnect/data/" + app + "." + name;

        string token = await GetVaultToken(); // Replace with your token

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        dynamic data = JsonConvert.DeserializeObject("{\"" + key + "\":null}");
        data[key] = SimpleEncrypt(value.ToString());
        vaultDict[app + "." + name] = SimpleEncrypt(value.ToString());
        var payload = new
        {
            data = data
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(vaultUrl, content);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    public static async Task<string> SetVaultSecretValue(string app, string name, object value)
    {
        if (value == null) value = "";
        string key = name;

        // 1. Vault Server Address (Ensure it's accessible from Docker)
        string vaultUrl = VaultUrl + "/v1/GConnect/data/" + app + "." + name;

        string token = await GetVaultToken(); // Replace with your token

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        vaultDict[app + "." + name] = SimpleEncrypt(value.ToString());
        dynamic data = JsonConvert.DeserializeObject("{\"" + key + "\":null}");
        data[key] = SimpleEncrypt(value.ToString());
        var payload = new
        {
            data = new { value = SimpleEncrypt(value.ToString()) }
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(vaultUrl, content);
        var result = await response.Content.ReadAsStringAsync();
        return result;
    }

    public static async Task<string> SetVaultSecretValue(string app, string name, string key, byte[] value)
    {
        // 1. Vault Server Address (Ensure it's accessible from Docker)
        string vaultUrl = VaultUrl + "/v1/GConnect/data/" + app + "." + name;

        string token = await GetVaultToken(); // Replace with your token

        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("X-Vault-Token", token);

        dynamic data = new ExpandoObject();
        data[key] = SimpleEncrypt(Convert.ToBase64String(value));
        var payload = new
        {
            data = data
        };

        var json = JsonConvert.SerializeObject(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(vaultUrl, content);
        return await response.Content.ReadAsStringAsync();
    }

    private static void CreateTreeDirectory(string rootDirectory)
    {
        try
        {
            // KHÔNG convert path trên Linux - giữ nguyên dấu /
            // Path.DirectorySeparatorChar tự động xử lý cross-platform

            // Normalize path theo OS hiện tại
            string normalizedPath;
            if (Path.DirectorySeparatorChar == '/')
            {
                // Linux/Unix - giữ nguyên /
                normalizedPath = rootDirectory;
            }
            else
            {
                // Windows - convert / sang \
                normalizedPath = rootDirectory.Replace("/", "\\");
            }

            Console.WriteLine($"[DEBUG] CreateTreeDirectory: normalizedPath = {normalizedPath}");
            Console.WriteLine($"[DEBUG] Directory.Exists({normalizedPath}) = {Directory.Exists(normalizedPath)}");

            if (normalizedPath.StartsWith(@"\\") && Path.DirectorySeparatorChar == '\\')
            {
                // UNC path trên Windows: \\server\share\...
                var parts = normalizedPath.Substring(2).Split('\\').ToList();
                if (parts.Count < 2)
                {
                    Console.WriteLine("Đường dẫn UNC không hợp lệ.");
                    return;
                }
                string uncRoot = @"\\" + parts[0] + "\\" + parts[1];
                string subFolder = uncRoot;
                for (int i = 2; i < parts.Count; i++)
                {
                    subFolder = Path.Combine(subFolder, parts[i]);
                    if (!Directory.Exists(subFolder))
                    {
                        try
                        {
                            Directory.CreateDirectory(subFolder);
                            Console.WriteLine($"Tạo thư mục: {subFolder}");
                        }
                        catch (Exception ex)
                        {
                            WriteLogError($"Không thể tạo thư mục: {subFolder} - Lỗi: {ex.Message}", ex);
                            Console.WriteLine($"Không thể tạo thư mục: {subFolder} - Lỗi: {ex.Message}");
                        }
                    }
                }
            }
            else
            {
                // Local path - cross platform
                if (!Directory.Exists(normalizedPath))
                {
                    Console.WriteLine($"[DEBUG] Tạo thư mục: {normalizedPath}");
                    Directory.CreateDirectory(normalizedPath);
                    Console.WriteLine($"[DEBUG] Đã tạo thư mục thành công.");
                }
                else
                {
                    Console.WriteLine($"[DEBUG] Thư mục đã tồn tại.");
                }
            }
        }
        catch (Exception ex)
        {
            WriteLogError($"Lỗi khi tạo cây thư mục: {ex.Message}", ex);
            Console.WriteLine($"[ERROR] Lỗi khi tạo cây thư mục: {ex.Message}");
            Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
            throw; // Quan trọng: throw để biết lỗi sớm
        }
    }
    //private static void CreateTreeDirectory(string rootDirectory)
    //{
    //    try
    //    {
    //        var normalizedPath = rootDirectory.Replace("/", "\\");
    //        if (normalizedPath.StartsWith(@"\\"))
    //        {
    //            // UNC path: \\server\share\...
    //            // Cần tách riêng phần \\server\share
    //            var parts = normalizedPath.Substring(2).Split('\\').ToList();
    //            if (parts.Count < 2)
    //            {
    //                Console.WriteLine("Đường dẫn UNC không hợp lệ.");
    //                return;
    //            }

    //            string uncRoot = @"\\" + parts[0] + "\\" + parts[1]; // \\192.168.1.170\SHARE_ALL
    //            string subFolder = uncRoot;

    //            for (int i = 2; i < parts.Count; i++)
    //            {
    //                subFolder = Application.Utils.Common.NormalizedPathChar(Path.Combine(subFolder, parts[i]));
    //                if (!Directory.Exists(subFolder))
    //                {
    //                    try
    //                    {
    //                        Directory.CreateDirectory(subFolder);
    //                        Console.WriteLine($"Tạo thư mục: {subFolder}");
    //                    }
    //                    catch (Exception ex)
    //                    {
    //                        WriteLogError($"Không thể tạo thư mục: {subFolder} - Lỗi: {ex.Message}", ex);
    //                        Console.WriteLine($"Không thể tạo thư mục: {subFolder} - Lỗi: {ex.Message}");
    //                    }
    //                }
    //            }
    //        }
    //        else
    //        {
    //            // Local path
    //            if (!Directory.Exists(normalizedPath))
    //            {
    //                Directory.CreateDirectory(normalizedPath);
    //            }
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        WriteLogError($"Lỗi khi tạo cây thư mục: {ex.Message}", ex);
    //        Console.WriteLine($"Lỗi khi tạo cây thư mục: {ex.Message}");
    //    }
    //}

    // moi dung them file.hash + XOR, cu la hash extension + ECC
    public static (byte[] hash, string decryptedFileName, int bitSize) DecryptHashedFileName(string filePath)
    {
        // cu hash extension
        if (Path.GetExtension(filePath).Substring(1).Length > 10)
        {
            var decryptedByteFileName = Convert.FromBase64String(SimpleDecrypt(FileNameFromBase64(Path.GetExtension(filePath).Substring(1)))); // 1 = dau . trong extension
            // 32 byte hash
            byte[] hash = decryptedByteFileName[..32];

            var decryptedFileName = Path.GetFileName(filePath).Replace(".128" + Path.GetExtension(filePath), "").Replace(".192" + Path.GetExtension(filePath), "").Replace(".256" + Path.GetExtension(filePath), "").Replace(Path.GetExtension(filePath), "");
            var bitSize = 256;
            if (Path.GetFileName(filePath).IndexOf(".128" + Path.GetExtension(filePath)) > 0) bitSize = 128;
            if (Path.GetFileName(filePath).IndexOf(".192" + Path.GetExtension(filePath)) > 0) bitSize = 192;

            return (hash, decryptedFileName, bitSize);
        }
        else
        {
            var bitSize = 256;
            byte[] hash = new byte[] { };
            var decryptedFileName = filePath.Replace(".encrypt", "");
            if (System.IO.File.Exists(filePath.Replace(".encrypt", ".hash")))
            {
                var hashEncrypted = System.IO.File.ReadAllText(filePath.Replace(".encrypt", ".hash"));
                var decryptedByteFileName = Convert.FromBase64String(SimpleDecrypt(FileNameFromBase64(hashEncrypted)));
                // 32 byte hash
                hash = decryptedByteFileName[..32];
                var fileName = UTF8Encoding.UTF8.GetString(decryptedByteFileName[32..]);

                decryptedFileName = filePath.Replace(".encrypt", "");
                if (Path.GetFileName(decryptedFileName) != fileName)
                {
                    hash[0] = 0;
                    hash[1] = 0;
                    hash[2] = 0;
                }
            }

            return (hash, decryptedFileName, bitSize);
        }
    }

    const int BUFFER_SIZE = 4 * 1024 * 1024;
    private static string AppCode = "";
    private static string VaultUrl = "";
    private static bool CryptoStoringBySIMDXOR = !true;

    // --------------------
    public static void SetAppCode(string appCode, string? vaultUrl = null)
    {
        AppCode = appCode;
        if (!string.IsNullOrEmpty(vaultUrl)) VaultUrl = vaultUrl;
    }

    public static void SetVaultUrl(string vaultUrl)
    {
        VaultUrl = vaultUrl;
    }

    public static void SetCryptoStoringBySIMDXOR(bool cryptoByDISM)
    {
        CryptoStoringBySIMDXOR = cryptoByDISM;
    }


    public static async Task<string> DecryptConnectionString(string appCode, string vaultUrl, string connectionStr, string firstDbPwd)
    {
        VaultUrl = vaultUrl;
        if (connectionStr.Contains("{pwd}"))
        {
            try
            {
                var dbPwd = await HybridEncryption.GetVaultSecretValue(appCode, "dbpwd");
                if (dbPwd == null || dbPwd == "")
                    return connectionStr.Replace("{pwd}", SimpleDecrypt(firstDbPwd));
                else
                    return connectionStr.Replace("{pwd}", dbPwd);
            }
            catch
            {
                return connectionStr.Replace("{pwd}", SimpleDecrypt(firstDbPwd));
            }
        }
        else return connectionStr;
    }

    // ---------------------
    public static async Task<string> FakeEncryptStringToStoring(string plainText)
    {
        return plainText;
    }
    public static async Task<string> EncryptStringToStoring(string plainText, string publickey = null, bool debug = false)
    {
        if (CryptoStoringBySIMDXOR == true)
        {
            try
            {
                if (string.IsNullOrEmpty(plainText)) return plainText;
                // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
                byte xorKey = RandomNumberGenerator.GetBytes(1)[0]; // Random key

                var data = Encoding.UTF8.GetBytes(plainText);

                ObfuscateInPlace(data, xorKey);

                // Prepend key
                var result = new byte[data.Length + 1];
                result[0] = xorKey;
                Buffer.BlockCopy(data, 0, result, 1, data.Length);

                return Convert.ToBase64String(result.ToArray());
            }
            catch (Exception ex)
            {
                WriteLogError($"{ex.Message}", ex);
                if (debug == true)
                {
                    return ex.Message + ex.StackTrace;
                }
                //throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
                return plainText;
            }
        }
        else
        {
            try
            {
                if (string.IsNullOrEmpty(plainText)) return plainText;
                // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
                var receiverPublicKey = Convert.FromBase64String(publickey ?? await GetVaultSecretValue(AppCode, "pbECCLocal"));
                using var senderEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                byte[] ephemeralPublicKey = senderEcdh.ExportSubjectPublicKeyInfo();  //

                using var receiverPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                receiverPublic.ImportSubjectPublicKeyInfo(receiverPublicKey, out _);
                byte[] aesKey = senderEcdh.DeriveKeyMaterial(receiverPublic.PublicKey);

                byte[] encryptedData = AesGcmEncrypt(aesKey, System.Text.Encoding.UTF8.GetBytes(plainText));

                // Store ephemeralPublicKey with encrypted data
                using var ms = new MemoryStream();
                ms.Write(ephemeralPublicKey, 0, ephemeralPublicKey.Length);
                ms.Write(encryptedData, 0, encryptedData.Length);
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                WriteLogError($"{ex.Message}", ex);
                if (debug == true)
                {
                    return ex.Message + ex.StackTrace;
                }
                //throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
                return plainText;
            }
        }
    }

    public static async Task<string> DecryptStringToStoring(string encryptedText, string privatekey = null)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;
        if (CryptoStoringBySIMDXOR == true)
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText) || !IsBase64String(encryptedText) || encryptedText.Length <= 1) return encryptedText;
                // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
                // giai ma dung khoa cua Local

                var data = Convert.FromBase64String(encryptedText);

                byte xorKey = data[0];

                var actualData = new byte[data.Length - 1];
                Buffer.BlockCopy(data, 1, actualData, 0, actualData.Length);

                ObfuscateInPlace(actualData, xorKey);

                var decryptedText = System.Text.Encoding.UTF8.GetString(actualData);
                return decryptedText;
            }
            catch (Exception ex)
            {
                WriteLogError($"Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
                //throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
                return encryptedText;
            }
        }
        else
        {
            try
            {
                if (string.IsNullOrEmpty(encryptedText) || !IsBase64String(encryptedText) || encryptedText.Length < 91) return encryptedText;
                // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
                // giai ma dung khoa cua Local
                var receiverPrivateKey = Convert.FromBase64String(privatekey ?? await GetVaultSecretValue("NHCH", "pvECCLocal"));

                using var receiverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                receiverEcdh.ImportECPrivateKey(receiverPrivateKey, out _);

                var encryptedData = Convert.FromBase64String(encryptedText);
                byte[] ephemeralPublicKey = encryptedData[..91]; // First 65 bytes are the ephemeral key
                byte[] cipherText = encryptedData[91..];

                using var senderPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                senderPublic.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
                byte[] aesKey = receiverEcdh.DeriveKeyMaterial(senderPublic.PublicKey);

                byte[] decryptedData = AesGcmDecrypt(aesKey, cipherText);
                var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedData);
                return decryptedText;
            }
            catch (Exception ex)
            {
                WriteLogError($"Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
                //throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
                return encryptedText;
            }
        }
    }

    public static async Task<string> EncryptStringToTransmiting(string plainText)
    {
        try
        {
            if (string.IsNullOrEmpty(plainText)) return plainText;
            // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
            var privateKey = Convert.FromBase64String(await GetVaultSecretValue(AppCode, "pvECCGlobal"));
            var publicKeyCapMa = Convert.FromBase64String(await GetVaultSecretValue("CapMa", "pbECCGlobal"));

            using var senderEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderEcdh.ImportECPrivateKey(privateKey, out _);
            byte[] ephemeralPublicKey = senderEcdh.ExportSubjectPublicKeyInfo();

            using var receiverPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverPublic.ImportSubjectPublicKeyInfo(publicKeyCapMa, out _);
            byte[] aesKey = senderEcdh.DeriveKeyMaterial(receiverPublic.PublicKey);

            byte[] encryptedData = AesGcmEncrypt(aesKey, System.Text.Encoding.UTF8.GetBytes(plainText));

            // Store ephemeralPublicKey with encrypted data
            using var ms = new MemoryStream();
            ms.Write(ephemeralPublicKey, 0, ephemeralPublicKey.Length);
            ms.Write(encryptedData, 0, encryptedData.Length);
            return Convert.ToBase64String(ms.ToArray());
        }
        catch (Exception ex)
        {
            WriteLogError($"Sai khóa mã hóa hoặc dữ liệu trống", ex);
            throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
        }
    }

    public static async Task<string> DecryptStringToTransmiting(string encryptedText)
    {
        try
        {
            if (string.IsNullOrEmpty(encryptedText) || !IsBase64String(encryptedText) || encryptedText.Length < 91) return encryptedText;
            // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
            // giai ma dung khoa cua Local
            var privateKey = Convert.FromBase64String(await GetVaultSecretValue("CapMa", "pvECCGlobal"));

            using var receiverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverEcdh.ImportECPrivateKey(privateKey, out _);

            var encryptedData = Convert.FromBase64String(encryptedText);
            byte[] ephemeralPublicKey = encryptedData[..91]; // First 65 bytes are the ephemeral key
            byte[] cipherText = encryptedData[91..];

            using var senderPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderPublic.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
            byte[] aesKey = receiverEcdh.DeriveKeyMaterial(senderPublic.PublicKey);

            byte[] decryptedData = AesGcmDecrypt(aesKey, cipherText);
            var decryptedText = System.Text.Encoding.UTF8.GetString(decryptedData);
            return decryptedText;
        }
        catch (Exception ex)
        {
            WriteLogError($"Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
            throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
        }
    }

    // Global => khoa trao doi giua cac PM do Cap Ma gui Transiting
    // Local => khoa noi bo PM tu tao dung de Storing

    // mac dinh: PM ma hoa => Cap Ma giai ma, hoac key moi 
    public static async Task<string> EncryptFileToTransmiting(string filePath, string toFolder)
    {
        var privateKey = await GetVaultSecretValue(AppCode, "pvECCGlobal");
        var publicKeyCapMa = await GetVaultSecretValue("CapMa", "pbECCGlobal");

        return await EncryptFileToTransmiting(filePath, toFolder, privateKey, publicKeyCapMa);
    }

    public static async Task<string> EncryptFileToTransmiting(string filePath, string toFolder, string? privateKeyString = null, string? publicKeyString = null)
    {
        try
        {
            if ((new FileInfo(filePath)).Length == 0) return filePath;
            var tick = DateTime.Now.Ticks + "";
            var privateKey = Convert.FromBase64String(privateKeyString ?? await GetVaultSecretValue(AppCode, "pvECCGlobal"));
            var publicKeyCapMa = Convert.FromBase64String(publicKeyString ?? await GetVaultSecretValue("CapMa", "pbECCGlobal"));

            //yc bo khoi ten file
            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(filePath) + "." + tick));
            var inputFile = filePath;

            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }

            using var senderEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderEcdh.ImportECPrivateKey(privateKey, out _);
            byte[] ephemeralPublicKey = senderEcdh.ExportSubjectPublicKeyInfo();

            using var receiverPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverPublic.ImportSubjectPublicKeyInfo(publicKeyCapMa, out _);
            byte[] aesKey = AesGcmCustomKeySizeBits(senderEcdh.DeriveKeyMaterial(receiverPublic.PublicKey), aesBits);

            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var sha256 = SHA256.Create();

            var maxParallel = 1;
            int chunkSize = BUFFER_SIZE;
            if (chunkSize > inputStream.Length) chunkSize = (int)inputStream.Length;

            outputStream.WriteByte(aesBits == 128 ? (byte)0 : aesBits == 192 ? (byte)1 : (byte)2);
            outputStream.Write(ephemeralPublicKey, 0, ephemeralPublicKey.Length);

            long totalLength = inputStream.Length;
            int chunkIndex = 0;
            object writeLock = new(); // for thread-safe writing to output

            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            var tasks = new List<Task>();

            while (inputStream.Position < totalLength)
            {
                byte[] buffer = new byte[chunkSize];
                int bytesRead = await inputStream.ReadAsync(buffer, 0, chunkSize);

                if (bytesRead == 0) break;

                int currentIndex = chunkIndex++;
                byte[] chunkData = buffer[..bytesRead]; // avoid excess memory
                tasks.Add(Task.Run(() =>
                {
                    // Generate unique nonce per chunk
                    byte[] nonce = new byte[12];
                    RandomNumberGenerator.Fill(nonce);

                    byte[] ciphertext = new byte[bytesRead];
                    byte[] tag = new byte[16];

                    using var aes = new AesGcm(aesKey);
                    aes.Encrypt(nonce, chunkData, ciphertext, tag);

                    byte[] chunkLength = BitConverter.GetBytes(ciphertext.Length);

                    lock (writeLock)
                    {
                        outputStream.Write(chunkLength);
                        outputStream.Write(nonce);
                        outputStream.Write(tag);
                        outputStream.Write(ciphertext);

                        sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
                    }
                }));

                if (tasks.Count >= maxParallel)
                {
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);

            inputStream.Close();
            await outputStream.FlushAsync();
            outputStream.Close();
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            // hash to data and filenam
            outputFile = await PrependBytesToFileAsync(outputFile, sha256.Hash, tick);

            return outputFile;
        }
        catch (Exception ex)
        {
            WriteLogError($"Sai khóa mã hóa hoặc dữ liệu trống", ex);
            throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
        }
    }

    public static async Task<string> EncryptFileToTransmitingOld(string filePath, string toFolder, string? privateKeyString = null, string? publicKeyString = null)
    {
        try
        {
            if ((new FileInfo(filePath)).Length == 0) return filePath;
            var tick = DateTime.Now.Ticks + "";
            var privateKey = Convert.FromBase64String(privateKeyString ?? await GetVaultSecretValue(AppCode, "pvECCGlobal"));
            var publicKeyCapMa = Convert.FromBase64String(publicKeyString ?? await GetVaultSecretValue("CapMa", "pbECCGlobal"));

            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(filePath) + "." + aesBits + "." + tick));
            var inputFile = filePath;

            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }

            using var senderEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderEcdh.ImportECPrivateKey(privateKey, out _);
            byte[] ephemeralPublicKey = senderEcdh.ExportSubjectPublicKeyInfo();

            using var receiverPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverPublic.ImportSubjectPublicKeyInfo(publicKeyCapMa, out _);
            byte[] aesKey = AesGcmCustomKeySizeBits(senderEcdh.DeriveKeyMaterial(receiverPublic.PublicKey), aesBits);

            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

            outputStream.Write(ephemeralPublicKey, 0, ephemeralPublicKey.Length);

            int chunkSize = BUFFER_SIZE;
            byte[] buffer = new byte[chunkSize];
            int bytesRead;

            using var aes = new AesGcm(aesKey);
            byte[] nonce = new byte[12];
            RandomNumberGenerator.Fill(nonce);
            outputStream.Write(nonce, 0, nonce.Length);

            using var sha256 = SHA256.Create();
            while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
            {
                byte[] ciphertext = new byte[bytesRead];
                byte[] tag = new byte[16];

                aes.Encrypt(nonce, buffer.AsSpan(0, bytesRead), ciphertext, tag);
                outputStream.Write(tag, 0, tag.Length);
                outputStream.Write(ciphertext, 0, ciphertext.Length);

                sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0); // ciphertext cho nhanh
            }
            inputStream.Close();
            await outputStream.FlushAsync();
            outputStream.Close();
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            // hash to data and filenam
            outputFile = await PrependBytesToFileAsync(outputFile, sha256.Hash, tick);

            return outputFile;
        }
        catch (Exception ex)
        {
            WriteLogError($"Sai khóa mã hóa hoặc dữ liệu trống", ex);
            throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
        }
    }

    // mac dinh: PM ma hoa => Cap Ma giai ma, hoac key moi, public key gui di o trong file
    public static async Task<(string outputFile, bool validHash)> DecryptFileToTransmiting(string filePath, string toFolder)
    {
        var privateKey = await GetVaultSecretValue("CapMa", "pvECCGlobal");
        return await DecryptFileToTransmiting(filePath, toFolder, privateKey);
    }

    public static async Task<(string outputFile, bool validHash)> DecryptFileToTransmiting(string filePath, string toFolder, string? privateKeyString = null)
    {
        try
        {
            if ((new FileInfo(filePath)).Length < 91) return (filePath, true);
            var privateKey = Convert.FromBase64String(privateKeyString ?? await GetVaultSecretValue("CapMa", "pvECCGlobal"));

            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }
            if (!string.IsNullOrEmpty(filePath)) filePath = Application.Utils.Common.NormalizedPathChar(filePath);
            var decyptFileNameResult = DecryptHashedFileName(filePath);
            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(decyptFileNameResult.decryptedFileName)));
            var outputDir = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
                SharedKernel.Application.Utils.HybridEncryption.WriteLogError($"Created directory: {outputDir}");
            }
            if (File.Exists(outputFile)) File.Delete(outputFile);

            using var receiverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverEcdh.ImportECPrivateKey(privateKey, out _);

            using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);
            using var sha256 = SHA256.Create();

            var maxParallel = 1;

            // Đọc 32 byte đầu: hash thật sự trong nội dung
            byte[] hashFromFile = new byte[32];
            inputStream.Read(hashFromFile, 0, 32);

            byte[] sizeOfKey = new byte[1];
            inputStream.Read(sizeOfKey, 0, 1);
            decyptFileNameResult.bitSize = sizeOfKey[0] == 0 ? 128 : sizeOfKey[0] == 1 ? 192 : 256;

            // Đọc khóa công khai tạm 91
            byte[] ephemeralPublicKey = new byte[91];
            inputStream.Read(ephemeralPublicKey, 0, ephemeralPublicKey.Length);

            using var senderPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderPublic.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
            byte[] aesKey = AesGcmCustomKeySizeBits(receiverEcdh.DeriveKeyMaterial(senderPublic.PublicKey), decyptFileNameResult.bitSize);

            var chunks = new List<(long Offset, int Length, int Index)>();
            int chunkIndex = 0;
            long pos = 32 + 1 + 91; // hash + size + key

            // Step 1: Pre-scan to gather chunk offsets (for parallel safety)
            while (pos < inputStream.Length)
            {
                inputStream.Position = pos;

                byte[] lenBytes = new byte[4];
                await inputStream.ReadAsync(lenBytes, 0, 4);
                int dataLen = BitConverter.ToInt32(lenBytes, 0);

                long chunkTotalSize = 4 + 12 + 16 + dataLen;
                chunks.Add((pos, dataLen, chunkIndex++));

                pos += chunkTotalSize;
            }

            // Step 2: Parallel decryption
            var options = new ParallelOptions { MaxDegreeOfParallelism = maxParallel };

            await Parallel.ForEachAsync(chunks, options, async (chunk, _) =>
            {
                (long offset, int length, int index) = chunk;

                byte[] lenBytes = new byte[4];
                byte[] nonce = new byte[12];
                byte[] tag = new byte[16];
                byte[] ciphertext = new byte[length];

                lock (inputStream) inputStream.Position = offset;

                using var chunkStream = new MemoryStream();
                lock (inputStream)
                {
                    inputStream.Read(lenBytes, 0, 4);
                    inputStream.Read(nonce, 0, 12);
                    inputStream.Read(tag, 0, 16);
                    inputStream.Read(ciphertext, 0, length);
                }

                byte[] plaintext = new byte[length];

                using var aes = new AesGcm(aesKey);

                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                await outputStream.WriteAsync(plaintext, 0, plaintext.Length);

                sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
            });

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            await outputStream.FlushAsync();

            // Tính toán giá trị hash sau khi giải mã
            byte[] actualHash = sha256.Hash!;

            bool validHash = actualHash.SequenceEqual(hashFromFile);// && actualHash.SequenceEqual(decyptFileNameResult.hash);

            return (outputFile, validHash);
        }
        catch (Exception ex)
        {
            WriteLogError($"Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
            throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
        }
    }

    public static async Task<(string outputFile, bool validHash)> DecryptFileToTransmitingOld(string filePath, string toFolder, string? privateKeyString = null)
    {
        try
        {
            if (!string.IsNullOrEmpty(filePath)) filePath = Application.Utils.Common.NormalizedPathChar(filePath);
            if ((new FileInfo(filePath)).Length < 91) return (filePath, true);
            var privateKey = Convert.FromBase64String(privateKeyString ?? await GetVaultSecretValue("CapMa", "pvECCGlobal"));

            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }

            var decyptFileNameResult = DecryptHashedFileName(filePath);
            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(decyptFileNameResult.decryptedFileName)));

            if (File.Exists(outputFile)) File.Delete(outputFile);

            using var receiverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            receiverEcdh.ImportECPrivateKey(privateKey, out _);

            using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

            using var sha256 = SHA256.Create();

            // Đọc 32 byte đầu: hash thật sự trong nội dung
            byte[] hashFromFile = new byte[32];
            inputStream.Read(hashFromFile, 0, 32);

            // Đọc khóa công khai tạm thời
            byte[] ephemeralPublicKey = new byte[91];
            inputStream.Read(ephemeralPublicKey, 0, ephemeralPublicKey.Length);

            using var senderPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
            senderPublic.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
            byte[] aesKey = AesGcmCustomKeySizeBits(receiverEcdh.DeriveKeyMaterial(senderPublic.PublicKey), decyptFileNameResult.bitSize);

            using var aes = new AesGcm(aesKey);
            int chunkSize = BUFFER_SIZE + 4 + 12 + 16;
            byte[] buffer = new byte[chunkSize];
            int bytesRead;
            bool firstError = true;

            while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
            {
                try
                {
                    if (true)
                    {
                        // Đọc nonce
                        byte[] len = buffer[..4];
                        byte[] nonce = buffer[4..(4 + 12)];
                        byte[] tag = buffer[(4 + 12)..(4 + 12 + 16)];
                        byte[] ciphertext = buffer[(4 + 12 + 16)..bytesRead];
                        byte[] plaintext = new byte[ciphertext.Length];

                        aes.Decrypt(nonce, ciphertext, tag, plaintext);
                        outputStream.Write(plaintext, 0, plaintext.Length);
                        await outputStream.FlushAsync();

                        sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
                    }

                    // xu ly 2 block cuoi cung bi ghi sai vi tri
                    int lastLength = int.Parse("" + (inputStream.Length - inputStream.Position));
                    if (lastLength < chunkSize * 3)
                    {
                        byte[] bufferEnd = new byte[lastLength];

                        inputStream.Read(bufferEnd, 0, bufferEnd.Length);

                        // Đọc nonce
                        byte[] len = bufferEnd[..4];
                        byte[] nonce = bufferEnd[4..(4 + 12)];
                        byte[] tag = bufferEnd[(4 + 12)..(4 + 12 + 16)];
                        byte[] ciphertext = bufferEnd[(4 + 12 + 16)..BUFFER_SIZE]; // BUFFER_SIZE => du 1 chunkSize
                        byte[] plaintext = new byte[ciphertext.Length];

                        try
                        {
                            aes.Decrypt(nonce, ciphertext, tag, plaintext);
                            outputStream.Write(plaintext, 0, plaintext.Length);
                            await outputStream.FlushAsync();

                            sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);

                            // block cuoi not full
                            var startLastBlock = bufferEnd.Length - chunkSize + 4 + 12 + 16;

                            // Đọc nonce
                            len = bufferEnd[(startLastBlock)..(startLastBlock + 4)];
                            nonce = bufferEnd[(startLastBlock + 4)..(startLastBlock + 4 + 12)];
                            tag = bufferEnd[(startLastBlock + 4 + 12)..(startLastBlock + 4 + 12 + 16)];
                            ciphertext = bufferEnd[(startLastBlock + 4 + 12 + 16)..(startLastBlock + lastLength - chunkSize)];
                            plaintext = new byte[ciphertext.Length];

                            aes.Decrypt(nonce, ciphertext, tag, plaintext);
                            outputStream.Write(plaintext, 0, plaintext.Length);
                            await outputStream.FlushAsync();

                            sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
                        }
                        catch
                        {
                            // Đọc nonce
                            len = bufferEnd[..4];
                            nonce = bufferEnd[4..(4 + 12)];
                            tag = bufferEnd[(4 + 12)..(4 + 12 + 16)];
                            ciphertext = bufferEnd[(4 + 12 + 16)..(bufferEnd.Length - chunkSize)]; // not full chunkSize
                            plaintext = new byte[ciphertext.Length];

                            try
                            {
                                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                                outputStream.Write(plaintext, 0, plaintext.Length);
                                await outputStream.FlushAsync();

                                sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);

                                // block cuoi full
                                var startLastBlock = bufferEnd.Length - chunkSize;// + 4 + 12 + 16;

                                // Đọc nonce
                                len = bufferEnd[(startLastBlock)..(startLastBlock + 4)];
                                nonce = bufferEnd[(startLastBlock + 4)..(startLastBlock + 4 + 12)];
                                tag = bufferEnd[(startLastBlock + 4 + 12)..(startLastBlock + 4 + 12 + 16)];
                                ciphertext = bufferEnd[(startLastBlock + 4 + 12 + 16)..];
                                plaintext = new byte[ciphertext.Length];

                                aes.Decrypt(nonce, ciphertext, tag, plaintext);
                                outputStream.Write(plaintext, 0, plaintext.Length);
                                await outputStream.FlushAsync();

                                sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
                            }
                            catch
                            {

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    if (inputStream.Position <= chunkSize)
                    {
                        if (firstError) // giai ma file cu 16Kb
                        {
                            chunkSize = 16 * 1024 + 16;
                            buffer = new byte[chunkSize];
                            inputStream.Position = 32 + 91 + 12; // from hash + key + nonce
                        }
                        else
                        {
                            throw ex;
                        }
                        firstError = false;
                    }
                    else
                    {
                        WriteLogError(ex.Message, ex);
                    }
                }
            }

            await outputStream.FlushAsync();
            outputStream.Close();

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            // Tính toán giá trị hash sau khi giải mã
            byte[] actualHash = sha256.Hash!;

            bool validHash = actualHash.SequenceEqual(hashFromFile) && actualHash.SequenceEqual(decyptFileNameResult.hash);

            return (outputFile, validHash);
        }
        catch (Exception ex)
        {
            WriteLogError("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
            throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
        }
    }

    // ~ encypt/decrypt fastest
    private static void ObfuscateInPlace(byte[] data, byte key)
    {
        int vectorSize = Vector<byte>.Count;
        int length = data.Length;
        int chunkSize = 1024 * 1024; // 1MB per thread is a good default

        // Vector of the key byte
        var keyVector = new Vector<byte>(key);

        Parallel.For(0, (length + chunkSize - 1) / chunkSize, chunkIndex =>
        {
            int start = chunkIndex * chunkSize;
            int end = Math.Min(start + chunkSize, length);

            int i = start;

            // SIMD loop
            for (; i <= end - vectorSize; i += vectorSize)
            {
                var vec = new Vector<byte>(data, i);
                (vec ^ keyVector).CopyTo(data, i);
            }

            // Remaining bytes
            for (; i < end; i++)
            {
                data[i] ^= key;
            }
        });
    }

    // neu 
    public static async Task<string> EncryptFileToStoring(string filePath, string toFolder, string? pvKey_name, string? pbKey_name)
    {
        if (CryptoStoringBySIMDXOR == true)
        {
            try
            {
                Console.WriteLine("[START] EncryptFileToStoring");

                if ((new FileInfo(filePath)).Length == 0)
                {
                    Console.WriteLine("File rỗng -> bỏ qua");
                    return filePath;
                }

                var tick = DateTime.Now.Ticks + "";
                byte xorKey = RandomNumberGenerator.GetBytes(1)[0];
                Console.WriteLine($"Tạo khóa XOR = {xorKey}");

                var fileName = Path.GetFileName(filePath);
                var outputFile = Application.Utils.Common.NormalizedPathChar(
                    System.IO.Path.Combine(toFolder, Path.GetFileName(fileName) + "." + tick)
                );
                var inputFile = filePath;

                if (!Directory.Exists(toFolder))
                {
                    Console.WriteLine($"Tạo thư mục {toFolder}");
                    CreateTreeDirectory(toFolder);
                }

                using var sha256 = SHA256.Create();

                if (!true) // branch dùng MemoryMappedFile
                {
                    Console.WriteLine("Sử dụng MemoryMappedFile mode");

                    long fileLength = new FileInfo(inputFile).Length;
                    int chunkSize = BUFFER_SIZE;

                    using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        outputStream.SetLength(fileLength + 1);
                        outputStream.Seek(0, SeekOrigin.Begin);
                        outputStream.Write(new byte[] { xorKey }, 0, 1);
                        Console.WriteLine("Đã ghi XOR key vào đầu file output");
                    }

                    using (var mmf = MemoryMappedFile.CreateFromFile(inputFile, FileMode.Open, null, fileLength, MemoryMappedFileAccess.Read))
                    using (var outputStream = new FileStream(outputFile, FileMode.Open, FileAccess.Write, FileShare.None))
                    {
                        for (long offset = 0; offset < fileLength; offset += chunkSize)
                        {
                            long remaining = fileLength - offset;
                            int size = (int)Math.Min(chunkSize, remaining);
                            Console.WriteLine($"Đọc chunk offset={offset}, size={size}");

                            using (var accessor = mmf.CreateViewAccessor(offset, size, MemoryMappedFileAccess.Read))
                            {
                                byte[] buffer = new byte[size];

                                for (int i = 0; i < size; i++)
                                {
                                    byte b = accessor.ReadByte(i);
                                    buffer[i] = (byte)(b ^ xorKey);
                                }

                                sha256.TransformBlock(buffer, 0, buffer.Length, null, 0);

                                outputStream.Seek(offset, SeekOrigin.Begin);
                                outputStream.Write(buffer, 0, size);
                            }
                        }
                    }
                }
                else // branch dùng FileStream thông thường
                {
                    Console.WriteLine("Sử dụng FileStream mode");

                    using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                    outputStream.Write(new byte[] { xorKey }, 0, 1);
                    Console.WriteLine("Đã ghi XOR key vào đầu file output");

                    int chunkSize = BUFFER_SIZE;
                    byte[] buffer = new byte[chunkSize];
                    int bytesRead;
                    int chunkCount = 0;

                    while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                    {
                        chunkCount++;
                        Console.WriteLine($"Chunk {chunkCount}: bytesRead={bytesRead}");

                        ObfuscateInPlace(buffer, xorKey);
                        outputStream.Write(buffer, 0, bytesRead);

                        sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    }

                    Console.WriteLine("Hoàn tất ghi file output");
                    await outputStream.FlushAsync();
                }

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Console.WriteLine("Hoàn tất SHA256");

                outputFile = await PrependBytesToFileAsync(outputFile, sha256.Hash, tick);
                Console.WriteLine("Đã prepend hash vào file");

                Console.WriteLine("[END] EncryptFileToStoring");
                return outputFile;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[ERROR] Sai khóa mã hóa hoặc dữ liệu trống");
                WriteLogError("Sai khóa mã hóa hoặc dữ liệu trống", ex);
                throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống", ex);
            }
        }
        else
        {
            try
            {
                Console.WriteLine($"[DEBUG] Bắt đầu mã hóa file: {filePath}");
                if ((new FileInfo(filePath)).Length == 0)
                {
                    Console.WriteLine("[DEBUG] File rỗng -> bỏ qua.");
                    return filePath;
                }

                var tick = DateTime.Now.Ticks + "";
                Console.WriteLine($"[DEBUG] Tick: {tick}");

                // dung cap key Local voi ephemeral Key, dinh kem ephemeral public Key trong data
                var receiverPublicKey = Convert.FromBase64String(await GetVaultSecretValue("NHCH", pbKey_name ?? ""));
                Console.WriteLine($"[DEBUG] Receiver PublicKey length: {receiverPublicKey.Length}");

                var fileName = Path.GetFileName(filePath);
                var outputFile = Application.Utils.Common.NormalizedPathChar(
                    Path.Combine(toFolder, Path.GetFileName(fileName) + "." + tick)
                );
                Console.WriteLine($"[DEBUG] OutputFile: {outputFile}");

                if (!Directory.Exists(toFolder))
                {
                    Console.WriteLine($"[DEBUG] Tạo thư mục đích: {toFolder}");
                    CreateTreeDirectory(toFolder);
                }

                using var senderEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                byte[] ephemeralPublicKey = senderEcdh.ExportSubjectPublicKeyInfo();
                Console.WriteLine($"[DEBUG] Ephemeral PublicKey length: {ephemeralPublicKey.Length}");

                using var receiverPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                receiverPublic.ImportSubjectPublicKeyInfo(receiverPublicKey, out _);
                byte[] aesKey = senderEcdh.DeriveKeyMaterial(receiverPublic.PublicKey);
                Console.WriteLine($"[DEBUG] AES Key length: {aesKey.Length}");

                using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                outputStream.Write(ephemeralPublicKey, 0, ephemeralPublicKey.Length);
                Console.WriteLine("[DEBUG] Đã ghi Ephemeral PublicKey vào file output.");

                int chunkSize = BUFFER_SIZE;
                byte[] buffer = new byte[chunkSize];
                int bytesRead;

                using var aes = new AesGcm(aesKey);
                byte[] nonce = new byte[12]; // 96-bit nonce
                RandomNumberGenerator.Fill(nonce);
                outputStream.Write(nonce, 0, nonce.Length);
                Console.WriteLine("[DEBUG] Nonce đã sinh và ghi vào file.");

                using var sha256 = SHA256.Create();
                while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                {
                    Console.WriteLine($"[DEBUG] Đọc {bytesRead} bytes từ input.");

                    byte[] ciphertext = new byte[bytesRead];
                    byte[] tag = new byte[16];

                    aes.Encrypt(nonce, buffer.AsSpan(0, bytesRead), ciphertext, tag);
                    outputStream.Write(tag, 0, tag.Length);
                    outputStream.Write(ciphertext, 0, ciphertext.Length);

                    sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
                    Console.WriteLine($"[DEBUG] Đã ghi {ciphertext.Length} bytes ciphertext + {tag.Length} bytes tag.");
                }

                inputStream.Close();
                await outputStream.FlushAsync();
                outputStream.Close();

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
                Console.WriteLine($"[DEBUG] SHA256 Hash length: {sha256.Hash?.Length}");

                outputFile = await PrependBytesToFileAsync(outputFile, sha256.Hash, tick);
                Console.WriteLine($"[DEBUG] Đã prepend hash vào file output. OutputFile cuối: {outputFile}");

                return outputFile;
            }
            catch (Exception ex)
            {
                WriteLogError("Sai khóa mã hóa hoặc dữ liệu trống", ex);
                Console.WriteLine($"Loi nay", ex.Message);
                throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống", ex);
            }

        }
    }

    public static async Task<(string outputFile, bool validHash)> DecryptFileToStoring(string filePath, string toFolder)
    {
        var receiverPrivateKey = await GetVaultSecretValue(AppCode, "pvECCLocal");

        var result = await DecryptFileToStoring(filePath, toFolder, receiverPrivateKey);

        return result;
    }

    public static async Task<(string outputFile, bool validHash)> DecryptFileToStoring(string filePath, string toFolder, string privateKey)
    {
        if (CryptoStoringBySIMDXOR == true && Path.GetExtension(filePath).Length < 10)
        {
            try
            {
                if ((new FileInfo(filePath)).Length <= 1) return (filePath, true); // 1 xorByte

                if (!Directory.Exists(toFolder))
                {
                    CreateTreeDirectory(toFolder);
                }

                var decyptFileNameResult = DecryptHashedFileName(filePath);
                var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(decyptFileNameResult.decryptedFileName)));

                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                using var sha256 = SHA256.Create();

                // Đọc 32 byte đầu: hash thật sự trong nội dung
                byte[] hashFromFile = new byte[32];
                inputStream.Read(hashFromFile, 0, 32);

                // Đọc khóa công khai tạm thời
                byte[] xorKeyFile = new byte[1];
                inputStream.Read(xorKeyFile, 0, xorKeyFile.Length);
                byte xorKey = xorKeyFile[0];

                int chunkSize = BUFFER_SIZE;
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                bool firstError = true;

                while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                {
                    sha256.TransformBlock(buffer, 0, buffer.Length, null, 0); // ciphertext cho nhanh

                    ObfuscateInPlace(buffer, xorKey);

                    outputStream.Write(buffer, 0, buffer.Length);
                }
                inputStream.Close();
                await outputStream.FlushAsync();
                outputStream.Close();

                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                // Tính toán giá trị hash sau khi giải mã
                byte[] actualHash = sha256.Hash!;

                bool validHash = actualHash.SequenceEqual(hashFromFile);// && actualHash.SequenceEqual(decyptFileNameResult.hash);

                return (outputFile, validHash); // ✅ Giải mã thành công
            }
            catch (Exception ex)
            {
                WriteLogError("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
                throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
            }
        }
        else
        {
            try
            {
                if ((new FileInfo(filePath)).Length < 91) return (filePath, true);
                var receiverPrivateKey = !string.IsNullOrEmpty(privateKey) && !string.IsNullOrWhiteSpace(privateKey) ? Convert.FromBase64String(privateKey) : Convert.FromBase64String(await GetVaultSecretValue(AppCode, "pvECCLocal"));

                if (!Directory.Exists(toFolder))
                {
                    CreateTreeDirectory(toFolder);
                }

                var decyptFileNameResult = DecryptHashedFileName(filePath);
                var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(decyptFileNameResult.decryptedFileName)));

                if (File.Exists(outputFile))
                {
                    File.Delete(outputFile);
                }

                using var receiverEcdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                receiverEcdh.ImportECPrivateKey(receiverPrivateKey, out _);

                using var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

                using var sha256 = SHA256.Create();

                // Đọc 32 byte đầu: hash thật sự trong nội dung
                byte[] hashFromFile = new byte[32];
                inputStream.Read(hashFromFile, 0, 32);

                // Đọc khóa công khai tạm thời
                byte[] ephemeralPublicKey = new byte[91];
                inputStream.Read(ephemeralPublicKey, 0, ephemeralPublicKey.Length);

                using var senderPublic = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
                senderPublic.ImportSubjectPublicKeyInfo(ephemeralPublicKey, out _);
                byte[] aesKey = receiverEcdh.DeriveKeyMaterial(senderPublic.PublicKey);

                byte[] nonce = new byte[12];
                inputStream.Read(nonce, 0, nonce.Length);

                using var aes = new AesGcm(aesKey);
                int chunkSize = BUFFER_SIZE + 16;
                byte[] buffer = new byte[chunkSize];
                int bytesRead;
                bool firstError = true;

                while ((bytesRead = inputStream.Read(buffer, 0, chunkSize)) > 0)
                {
                    try
                    {
                        byte[] tag = buffer[..16];
                        byte[] ciphertext = buffer[16..bytesRead];
                        byte[] plaintext = new byte[ciphertext.Length];

                        aes.Decrypt(nonce, ciphertext, tag, plaintext); // lỗi tại đây nếu sai tag

                        outputStream.Write(plaintext, 0, plaintext.Length);
                        sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0); // ciphertext cho nhanh
                    }
                    catch (Exception ex)
                    {
                        WriteLogError(ex.Message, ex);
                        if (firstError) // giai ma file cu 16Kb
                        {
                            chunkSize = 16 * 1024 + 16;
                            buffer = new byte[chunkSize];
                            inputStream.Position = 32 + 91 + 12; // from hash + key + nonce
                        }
                        else
                        {
                            throw ex;
                        }
                        firstError = false;
                    }
                }
                inputStream.Close();
                await outputStream.FlushAsync();
                outputStream.Close();
                sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

                // Tính toán giá trị hash sau khi giải mã
                byte[] actualHash = sha256.Hash!;

                bool validHash = actualHash.SequenceEqual(hashFromFile);// && actualHash.SequenceEqual(decyptFileNameResult.hash);

                return (outputFile, validHash); // ✅ Giải mã thành công
            }
            catch (Exception ex)
            {
                WriteLogError("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
                throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
            }
        }
    }

    public static async Task<(string outputFile, bool validHash)> EncryptFileToSimple(string filePath, string toFolder)
    {
        try
        {
            if ((new FileInfo(filePath)).Length < 91) return (filePath, true);
            var tick = DateTime.Now.Ticks + "";
            var fileName = Path.GetFileName(filePath);
            // ma hoa file name + content
            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(fileName) + "." + tick));
            var inputFile = filePath;

            // Kiểm tra nếu thư mục chưa tồn tại thì tạo thư mục
            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }

            using var inputStream = new FileStream(inputFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write);

            var plaintext = new byte[inputStream.Length];
            inputStream.Read(plaintext, 0, plaintext.Length);

            var ciphertext = Convert.FromBase64String(SimpleEncrypt(Convert.ToBase64String(plaintext)));

            outputStream.Write(ciphertext, 0, ciphertext.Length);
            using var sha256 = SHA256.Create();

            sha256.TransformBlock(ciphertext, 0, ciphertext.Length, null, 0);
            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

            outputFile = await PrependBytesToFileAsync(outputFile, sha256.Hash, tick);

            return (outputFile, true); // ✅ Giải mã thành công
        }
        catch (Exception ex)
        {
            WriteLogError("Sai khóa mã hóa hoặc dữ liệu trống", ex);
            throw new Exception("Sai khóa mã hóa hoặc dữ liệu trống");
        }
    }

    public static async Task<string> DecryptFileToSimple(string filePath, string toFolder)
    {
        try
        {
            if ((new FileInfo(filePath)).Length == 0) return filePath;
            // Kiểm tra nếu thư mục chưa tồn tại thì tạo thư mục
            if (!Directory.Exists(toFolder))
            {
                CreateTreeDirectory(toFolder);
            }

            var fileName = FileNameFromBase64(Path.GetFileName(filePath));

            // Giải mã file name và content
            var outputFile = Application.Utils.Common.NormalizedPathChar(System.IO.Path.Combine(toFolder, Path.GetFileName(fileName).Replace(Path.GetExtension(fileName), "")));

            var encryptedFile = filePath;

            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }

            // Đọc và giải mã tệp
            using (var inputStream = new FileStream(encryptedFile, FileMode.Open, FileAccess.Read))
            using (var outputStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write))
            {
                var ciphertext = new byte[inputStream.Length];
                inputStream.Read(ciphertext, 0, ciphertext.Length);

                // Giải mã nội dung
                var plaintext = Convert.FromBase64String(SimpleDecrypt(Convert.ToBase64String(ciphertext)));

                // Ghi nội dung đã giải mã vào tệp đầu ra
                outputStream.Write(plaintext, 0, plaintext.Length);
            }

            return outputFile;
        }
        catch (Exception ex)
        {
            WriteLogError("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống", ex);
            throw new Exception("Dữ liệu giải mã lỗi hoặc sai khóa giải mã hoặc dữ liệu trống");
        }
    }

    public static async Task<string> DecryptUploadedFile(IFormFile file, bool transmit)
    {
        // Get system temp directory
        var tempDir = Application.Utils.Common.CreateUniqueTempDirectory();

        // Generate unique file name to avoid conflicts
        var uploadsFolder = Application.Utils.Common.NormalizedPathChar(Path.Combine(tempDir, Path.GetRandomFileName()));

        CreateTreeDirectory(uploadsFolder); // Ensure the folder exists

        var filePath = Application.Utils.Common.NormalizedPathChar(Path.Combine(uploadsFolder, file.FileName));

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // giai ma file
        if (transmit == true)
        {
            var outputDecrypted = await HybridEncryption.DecryptFileToTransmiting(filePath, uploadsFolder);
            if (!outputDecrypted.validHash) throw new Exception("Wrong hash number, file have been changed.");
            return System.IO.File.ReadAllText(outputDecrypted.outputFile);
        }
        else return HybridEncryption.SimpleDecrypt(System.IO.File.ReadAllText(filePath));
    }

    public static string DropGlobalTempTable(string tempTable)
    {
        var dropTableSql = $"DROP TABLE {tempTable};";

        return dropTableSql;
    }

    private static void CreateTempTable(MySqlConnection conn, string targetTable, string tempTable)
    {
        var createTableSql = $@"
BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
        BEGIN TRANSACTION;

        SELECT TOP 0 *
        INTO {tempTable}
        FROM {targetTable};

        COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 2627 -- Duplicate PK
        PRINT 'Duplicate Key Error';
    ELSE
        THROW;
END CATCH;
    ";
        using (var cmd = new MySqlCommand(createTableSql, conn))
        {
            cmd.ExecuteNonQuery();
        }
    }

    public static string GenerateMergeSql(string targetTable, string tempTable, string[] keyColumns, string[] members)
    {
        // where NULL = NULL not working => NULL=NULL or (is NULL AND is NULL)
        var onConditions = string.Join(" AND ", keyColumns.Select(key => $"(Target.{key} = Source.{key} OR (Target.{key} IS NULL AND Source.{key} IS NULL))"));

        var updateSet = string.Join(", ", members.Where(col => col.ToLower() != "id" && col.ToLower() != "nguoi_tao" && col.ToLower() != "ngay_tao").Select(col => col.ToLower() == "ngay_chinh_sua" ? $"Target.{col} = GETDATE()" : $"Target.{col} = Source.{col}").ToList());

        var insertColumnList = string.Join(", ", members.Where(col => col.ToLower() != "nguoi_chinh_sua" && col.ToLower() != "ngay_chinh_sua").Select(col => col));
        var insertValueList = string.Join(", ", members.Where(col => col.ToLower() != "nguoi_chinh_sua" && col.ToLower() != "ngay_chinh_sua").Select(col => col.ToLower() == "ngay_tao" ? "GETDATE()" : col.ToLower() == "ngay_chinh_sua" ? "NULL" : $"Source.{col}").ToList());
        //col.ToLower() == "ngay_tao" ? "GETDATE()" : col.ToLower() == "ngay_chinh_sua" ? "NULL" : 

        var mergeSql = $@"
BEGIN TRY
    SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
    BEGIN TRANSACTION;

    --MERGE INTO {targetTable} WITH (ROWLOCK, UPDLOCK) AS Target
    --    USING (SELECT * FROM {tempTable} WITH (ROWLOCK, UPDLOCK)) AS Source
    MERGE INTO {targetTable} AS Target
        USING {tempTable} AS Source
        ON Target.id = Source.id OR ({onConditions})
        WHEN MATCHED THEN
            UPDATE SET {updateSet}
        WHEN NOT MATCHED THEN
            INSERT ({insertColumnList})
            VALUES ({insertValueList});

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 2627 -- Duplicate PK
        PRINT 'Duplicate Key Error';
    ELSE
        THROW;
END CATCH;
    ";

        return mergeSql;
    }

    public static async Task InsertDataIntoMySQL(string connStr, string tableName, List<string> columns, List<Dictionary<string, object>> rows)
    {
        using (MySqlConnection conn = new MySqlConnection(connStr))
        {
            conn.Open();

            // Create table if not exists
            string createTableQuery = $"CREATE TABLE IF NOT EXISTS {tableName} (";
            foreach (string col in columns)
            {
                createTableQuery += $"{col} TEXT, ";
            }
            createTableQuery = createTableQuery.TrimEnd(',', ' ') + ");";
            using (MySqlCommand createCmd = new MySqlCommand(createTableQuery, conn))
            {
                createCmd.ExecuteNonQuery();
            }
            using (MySqlCommand cmd = new MySqlCommand())
            {
                cmd.Connection = conn;

                using (var transaction = conn.BeginTransaction())
                {
                    cmd.Transaction = transaction;

                    // Disable foreign key constraints
                    cmd.CommandText = "SET FOREIGN_KEY_CHECKS=0;";
                    cmd.ExecuteNonQuery();

                    // Insert data dynamically
                    foreach (var row in rows)
                    {
                        string columnNames = string.Join(", ", columns);
                        string paramNames = string.Join(", ", columns.ConvertAll(c => $"@{c}"));

                        string query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({paramNames}) " +
                                       $"ON DUPLICATE KEY UPDATE {string.Join(", ", columns.ConvertAll(c => $"{c} = VALUES({c})"))};";

                        cmd.Parameters.Clear();
                        foreach (string col in columns)
                        {
                            cmd.Parameters.AddWithValue($"@{col}", row[col] ?? DBNull.Value);
                        }
                        try
                        {
                            // Insert or Update operation
                            cmd.CommandText = query;
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            WriteLogError(query, ex);
                        }
                    }

                    // Enable foreign key constraints again
                    cmd.CommandText = "SET FOREIGN_KEY_CHECKS=1;";
                    cmd.ExecuteNonQuery();

                    transaction.Commit(); // Ensure all changes are applied
                }
            }
        }
    }

    
    public static async Task ReadPrivateKeys(string appCode)
    {
        string keyFileName = $"{appCode}-ks.json";

        // giai ma file
        var outputDecrypt = HybridEncryption.SimpleDecrypt(System.IO.File.ReadAllText(keyFileName));
        // load file vao json
        var transmitKeys = JsonConvert.DeserializeObject<(string AppCode, string PrivateKeyECC, string PrivateKeyRSA)>(outputDecrypt);
        if (appCode != transmitKeys.AppCode)
        {
            throw new Exception("File is not for App " + appCode);
        }
        else
        {
            await HybridEncryption.SetVaultSecretValue(appCode, "pvECCGlobal", (transmitKeys.PrivateKeyECC));
            await HybridEncryption.SetVaultSecretValue(appCode, "pvRSAGlobal", (transmitKeys.PrivateKeyRSA));
        }
    }

    public static async Task SavePrivateKeysFile(string appCode, string rootPath)
    {
        var PrivateKeyECC = Convert.FromBase64String(await HybridEncryption.GetVaultSecretValue(appCode, "pvECCGlobal"));
        var PrivateKeyRSA = Convert.FromBase64String(await HybridEncryption.GetVaultSecretValue(appCode, "pvRSAGlobal"));

        var result = new { AppCode = appCode, PrivateKeyECC, PrivateKeyRSA };

        // Convert to JSON
        string json = JsonConvert.SerializeObject(result, Formatting.Indented);

        // Save JSON to file
        string fileName = $"{appCode}-ks.json";
        await System.IO.File.WriteAllTextAsync(Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPath, fileName)), HybridEncryption.SimpleEncrypt(json));
    }

    public static async Task SetupPrivateKeysFile(string appCode, string rootPath)
    {
        string keyFileName = Application.Utils.Common.NormalizedPathChar(Path.Combine(rootPath, $"{appCode}-ks.json"));
        // giai ma file
        var outputDecrypt = HybridEncryption.SimpleDecrypt(System.IO.File.ReadAllText(keyFileName));
        // load file vao json
        var transmitKeys = JsonConvert.DeserializeObject<(string AppCode, string PrivateKeyECC, string PrivateKeyRSA)>(outputDecrypt);
        if (appCode != transmitKeys.AppCode)
        {
            throw new Exception("File is not for App " + appCode);
        }
        else
        {
            await HybridEncryption.SetVaultSecretValue(appCode, "pvECCGlobal", (transmitKeys.PrivateKeyECC));
            await HybridEncryption.SetVaultSecretValue(appCode, "pvRSAGlobal", (transmitKeys.PrivateKeyRSA));
        }
    }

    public static async Task<string> FormatConnectionString(string appCode, IConfiguration _config)
    {
        var connectionString = _config.GetConnectionString(appCode) ?? _config.GetConnectionString("default") ?? "no -defined";
        var vaultUrl = _config.GetSection("Uri")["vault"] + "";
        var firstDbPwd = _config.GetConnectionString("firstDbPwd") ?? "";
        try
        {
            return await HybridEncryption.DecryptConnectionString(appCode, vaultUrl, connectionString, firstDbPwd);
        }
        catch (Exception ex)
        {
            WriteLogError(connectionString, ex);
            return connectionString;
        }
    }

    public static string SignWithECC(string data, byte[] privateKeyBytes)
    {
        using (ECDsa privateKey = ECDsa.Create())
        {
            privateKey.ImportECPrivateKey(privateKeyBytes, out _); // Nhập khóa riêng từ byte[]
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = privateKey.SignData(dataBytes, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(signatureBytes); // Trả về chữ ký dạng Base64
        }
    }

    // ✅ Xác minh chữ ký bằng khóa công khai ECC (byte[])
    public static bool VerifyWithECC(string data, string signatureBase64, byte[] publicKeyBytes)
    {
        using (ECDsa publicKey = ECDsa.Create())
        {
            publicKey.ImportSubjectPublicKeyInfo(publicKeyBytes, out _); // Nhập khóa công khai từ byte[]
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] signatureBytes = Convert.FromBase64String(signatureBase64);
            return publicKey.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256);
        }
    }

    // No Confusing Characters: Avoids ambiguous characters like 0, O, I, l, 1
    // Includes Special Characters: For better security
    public static string GeneratePassword()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%";
        int length = new Random().Next(0, 2) == 0 ? 10 : 10; // Randomly choose 8 or 10

        using (var rng = new RNGCryptoServiceProvider())
        {
            return new string(Enumerable.Range(0, length)
                .Select(_ => chars[GetRandomNumber(rng, chars.Length)])
                .ToArray());
        }
    }

    private static int GetRandomNumber(RNGCryptoServiceProvider rng, int max)
    {
        byte[] buffer = new byte[4];
        rng.GetBytes(buffer);
        return (int)(BitConverter.ToUInt32(buffer, 0) % max);
    }

    public static void DeleteSecureFile(string filePath, int overwritePasses = 3)
    {
        if (!File.Exists(filePath))
            return;

        var fileInfo = new FileInfo(filePath);
        long length = fileInfo.Length;

        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Write))
        {
            byte[] buffer = new byte[5 * 1024 * 1024];
            var rng = RandomNumberGenerator.Create();

            for (int pass = 0; pass < overwritePasses; pass++)
            {
                fs.Seek(0, SeekOrigin.Begin);
                long remaining = length;

                while (remaining > 0)
                {
                    rng.GetBytes(buffer);
                    int toWrite = (int)Math.Min(buffer.Length, remaining);
                    fs.Write(buffer, 0, toWrite);
                    remaining -= toWrite;
                }

                fs.Flush(true); // force flush to disk
            }
        }

        // Remove file metadata and delete
        File.SetAttributes(filePath, FileAttributes.Normal);
        File.Delete(filePath);
    }
    public static async Task<bool> RunCommand(string command)
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
            process.WaitForExit(); // Sử dụng WaitForExitAsync để hỗ trợ bất đồng bộ
            if (error.ToLower().Contains("error"))
            {
                WriteLogError(error);
                throw new Exception(error);
            }
            return process.ExitCode == 0; // Trả về true nếu lệnh thành công
        }
    }
    public static void WriteLogError(string msg, Exception ex = null)
    {
        var exNew = new Exception(msg);
        msg += exNew.StackTrace;
        if (_iLogger != null)
        {
            if (ex != null)
                _iLogger.LogError(ex, msg);
            else
                _iLogger.LogError(msg);
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
    public static async Task<bool> RunBashCommand(string command)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "/bin/bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        await process.WaitForExitAsync();
        // Chờ quá trình hoàn thành
        process.WaitForExit(); // Sử dụng WaitForExitAsync để hỗ trợ bất đồng bộ
        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        Console.WriteLine(error);
        Console.WriteLine(output);
        if (error.ToLower().Contains("error"))
        {
            WriteLogError(error);
            throw new Exception(error);
        }
        return process.ExitCode == 0;
    }
    public static bool IsBase64String(string s)
    {
        try
        {
            if (string.IsNullOrEmpty(s) && string.IsNullOrWhiteSpace(s)) return false;

            Span<byte> buffer = new Span<byte>(new byte[s.Length]);
            return Convert.TryFromBase64String(s, buffer, out _);
        }
        catch (Exception ex)
        {
            WriteLogError(ex.Message, ex);
            return false;
        }
    }

    public static async Task<Dictionary<string, string>> BatchDecryptAsync(IEnumerable<string> encryptedStrings, CancellationToken cancellationToken, string privatekey = null)
    {
        var cache = new Dictionary<string, string>();

        async Task<string> DecryptMultipleTimesAsync(string input)
        {
            string current = input;
            while (!string.IsNullOrEmpty(current) && HybridEncryption.IsBase64String(current))
            {
                try
                {
                    string decrypted = privatekey != null ? await HybridEncryption.DecryptStringToStoring(current, privatekey) : await HybridEncryption.DecryptStringToStoring(current);
                    if (decrypted == current || (!HybridEncryption.IsBase64String(decrypted) && !decrypted.Contains("MFk"))) // Nếu không thay đổi hoặc không phải Base64, dừng
                    {
                        return decrypted;
                    }
                    current = decrypted; // Tiếp tục giải mã với chuỗi mới
                }
                catch (Exception ex)
                {
                    WriteLogError(ex.Message, ex);
                    return current; // Trả về chuỗi hiện tại nếu lỗi
                }
            }
            return current;
        }

        var tasks = encryptedStrings
            .Where(s => !string.IsNullOrEmpty(s) && !cache.ContainsKey(s))
            .Select(async s =>
            {
                var decrypted = await DecryptMultipleTimesAsync(s);
                return (s, decrypted);
            })
            .ToList();

        var results = await Task.WhenAll(tasks);
        foreach (var (encrypted, decrypted) in results)
        {
            cache[encrypted] = decrypted;
        }

        return cache;
    }

}
public class DataVault
{
    public DataVault data { get; set; }
    public string value { get; set; }
}
