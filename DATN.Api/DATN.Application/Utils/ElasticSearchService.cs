using DATN.Domain.DTO;
using DATN.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Nest;
using SharedKernel.Application.Utils;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Utils
{
    public class ElasticSearchService
    {
        private readonly ElasticClient _client;
        private readonly string _indexName = "tai_lieu";
        private readonly IConfiguration _config;

        public ElasticSearchService(IConfiguration config)
        {
            _config = config;
            var elasticUrl = _config.GetSection("ElasticSearchUrl")["path"] ?? "http://localhost:9200";

            var settings = new ConnectionSettings(new Uri(elasticUrl))
            .DefaultIndex(_indexName)
            .PrettyJson()
            .DisableDirectStreaming();

            _client = new ElasticClient(settings);
        }
        public ElasticClient Client => _client;

        //Tạo mapping cho index tai_lieu
        public async Task CreateIndexAsync()
        {
            var createIndexResponse = await _client.Indices.CreateAsync("tai_lieu", c => c
                .Map<tai_lieu>(m => m
                    .AutoMap()
                    .Properties(p => p
                        .Text(t => t.Name(n => n.ten).Analyzer("standard"))
                        .Text(t => t.Name(n => n.phong_ban))
                        .Text(t => t.Name(n => n.ContentText))
                        .Keyword(t => t.Name(n => n.Status))
                        .Date(t => t.Name(n => n.IndexedAt))
                    )
                )
            );
        }

        //Index(đẩy dữ liệu tai_lieu lên Elasticsearch)
        public async Task IndexTaiLieuAsync(tai_lieu tl)
        {
            var response = await _client.IndexDocumentAsync(tl);
        }


        //update (nếu đã tồn tại):
        public async Task<IndexResponse> UpsertTaiLieuAsync(tai_lieu tl, string contentText)
        {
            var dto = await ToElastic(tl, contentText);

            var response = await _client.IndexAsync(dto, i =>
                i.Id(dto.Id).Index("tai_lieu")
            );
            return response;
        }

        public async Task<TaiLieuElasticDto> ToElastic(tai_lieu tl, string plainContent)
        {
            // (1) Mã hóa nội dung bằng AES
            string cipher = await HybridEncryption.EncryptStringToStoring(plainContent);
            var ESUrl = _config.GetSection("ElasticSearchUrl")["path"] ?? "http://localhost:9200";
            // (2) Tokenize plaintext
            var tokens = await AnalyzeTextAsync(plainContent, ESUrl);

            // (3) Tạo blind index bằng HMAC
            string blindKey = _config["Security:BlindIndexKey"] ?? ""; // cấu hình trong appsettings
            var encryptedTokens = tokens
                .Select(t => HashToken(blindKey, t))
                .ToList();

            return new TaiLieuElasticDto
            {
                Id = tl.Id,
                ma = tl.ma,
                ten = tl.ten,
                cap_do = tl.cap_do,
                phong_ban = tl.phong_ban,
                isPublic = tl.isPublic,
                FileType = tl.FileType,
                FileSize = tl.FileSize,
                IndexedAt = DateTime.UtcNow,
                ngay_tao = tl.ngay_tao,
                nguoi_tao = tl.nguoi_tao,
                ngay_chinh_sua = tl.ngay_chinh_sua,
                nguoi_chinh_sua = tl.nguoi_chinh_sua,
                thu_muc_id = tl.thu_muc_id,
                ContentText = tl.ContentText,
                eccKeyName = tl.EccKeyName,
                encryptedTokens= encryptedTokens
            };
        }
        //2. Hàm HMAC blind index
        public static string HashToken(string key, string token)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            using var hmac = new HMACSHA256(keyBytes);
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToHexString(hash); // dạng hex
        }
        // Hàm gọi ES để tokenize plaintext
        public static async Task<List<string>> AnalyzeTextAsync(string text, string ElasticSearchUrl)
        {
            var elasticUrl = ElasticSearchUrl ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex("tai_lieu");

            var client = new ElasticClient(settings);
            var analyze = await client.Indices.AnalyzeAsync(a => a
                .Index("tai_lieu")
                .Analyzer("vi_analyzer")   // Analyzer bạn đã tạo
                .Text(text)
            );

            return analyze.Tokens
                          .Select(t => t.Token.ToLower().Trim())
                          .Where(t => t.Length > 1)
                          .Distinct()
                          .ToList();
        }

        public static List<string> AnalyzeText(string text, string ElasticSearchUrl)
        {
            var elasticUrl = ElasticSearchUrl ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex("tai_lieu");

            var client = new ElasticClient(settings);
            var analyze = client.Indices.Analyze(a => a
                .Index("tai_lieu")
                .Analyzer("vi_analyzer")   // Analyzer bạn đã tạo
                .Text(text)
            );

            return analyze.Tokens
                          .Select(t => t.Token.ToLower().Trim())
                          .Where(t => t.Length > 1)
                          .Distinct()
                          .ToList();
        }

        //Tìm kiếm tài liệu (Full Text Search)
        public async Task<List<tai_lieu>> SearchTaiLieuAsync(string keyword)
        {
            var response = await _client.SearchAsync<tai_lieu>(s => s
                .Index("tai_lieu")
                .Query(q => q
                    .MultiMatch(m => m
                        .Query(keyword)
                        .Fields(f => f
                            .Field(ff => ff.ten)
                            .Field(ff => ff.phong_ban)
                            .Field(ff => ff.ContentText)
                        )
                    )
                )
            );

            return response.Documents.ToList();
        }

        

    }
}
