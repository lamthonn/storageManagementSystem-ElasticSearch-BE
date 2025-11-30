using DATN.Domain.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Infrastructure.Data
{
    public class DbContextInitial
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;
        public DbContextInitial(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task InitialiseAsync()
        {
            try
            {
                // Run migration
                await _context.Database.MigrateAsync();
                // check ES + tạo index
                await CreateTaiLieuIndexAsync();
                await CreateThuMucIndexAsync();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message + ex.StackTrace);
            }
        }

        public async Task CreateTaiLieuIndexAsync()
        {
            var elasticUrl = _configuration.GetSection("ElasticSearchUrl")["path"] ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex("tai_lieu");

            var client = new ElasticClient(settings);

            var exists = await client.Indices.ExistsAsync("tai_lieu");
            if (exists.Exists) return;

            var createIndexResponse = await client.Indices.CreateAsync("tai_lieu", c => c
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .AsciiFolding("vn_folding", af => af
                                .PreserveOriginal(false)
                            )
                            .EdgeNGram("vn_edge_ngram", ng => ng
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                        .Analyzers(an => an
                            .Custom("vi_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "vn_folding")
                            )
                            .Custom("vi_prefix", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "vn_folding", "vn_edge_ngram")
                            )
                        )
                    )
                )
                .Map<TaiLieuElasticDto>(m => m
                    .Properties(p => p

                        // ========== BLIND INDEX FIELD ==========
                        .Keyword(k => k
                            .Name("encryptedTokens")   // phải THẲNG keyword
                        )

                        // ========== CipherText AES ==========
                        .Keyword(k => k
                            .Name("contentText")
                        )

                        // ========== Tên file ==========
                        .Text(t => t
                            .Name(x => x.ten)
                            .Analyzer("vi_analyzer")
                            .SearchAnalyzer("vi_analyzer")
                            .Fields(f => f
                                .Text(tt => tt
                                    .Name("prefix")
                                    .Analyzer("vi_prefix")
                                )
                            )
                        )

                        // ========== Metadata ==========
                        .Keyword(k => k.Name(x => x.Id))
                        .Keyword(k => k.Name(x => x.phong_ban))
                        .Number(n => n.Name(x => x.cap_do).Type(NumberType.Integer))
                        .Boolean(b => b.Name(x => x.isPublic))
                        .Keyword(k => k.Name(x => x.FileType))
                        .Number(n => n.Name(x => x.FileSize).Type(NumberType.Long))
                        .Date(d => d.Name(x => x.IndexedAt))
                    )
                )
            );

            if (!createIndexResponse.IsValid)
                throw new Exception(createIndexResponse.DebugInformation);
        }

        public async Task CreateThuMucIndexAsync()
        {
            var indexName = "thu_muc";
            var elasticUrl = _configuration.GetSection("ElasticSearchUrl")["path"] ?? "http://localhost:9200";
            var settings = new ConnectionSettings(new Uri(elasticUrl))
                .DefaultIndex(indexName);

            var client = new ElasticClient(settings);

            var exists = await client.Indices.ExistsAsync(indexName);
            if (exists.Exists) return;

            var response = await client.Indices.CreateAsync(indexName, c => c
                .Settings(s => s
                    .Analysis(a => a
                        .TokenFilters(tf => tf
                            .AsciiFolding("vn_folding", af => af
                                .PreserveOriginal(false)
                            )
                            .EdgeNGram("vn_edge_ngram", ng => ng
                                .MinGram(2)
                                .MaxGram(20)
                            )
                        )
                        .Analyzers(an => an
                            .Custom("vi_analyzer", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "vn_folding")
                            )
                            .Custom("vi_prefix", ca => ca
                                .Tokenizer("standard")
                                .Filters("lowercase", "vn_folding", "vn_edge_ngram")
                            )
                        )
                    )
                )
                .Map<ThuMucElasticDto>(m => m
                    .Properties(p => p
                        .Keyword(k => k.Name(x => x.id))
                        .Text(t => t
                            .Name(x => x.ten)
                            .Analyzer("vi_analyzer")
                            .Fields(f => f
                                .Text(tt => tt
                                    .Name("prefix")
                                    .Analyzer("vi_prefix")
                                )
                            )
                        )
                        .Keyword(k => k.Name(x => x.thu_muc_cha_id))
                        .Keyword(k => k.Name(x => x.nguoi_dung_id))
                    )
                )
            );

            if (!response.IsValid)
                throw new Exception(response.DebugInformation);
        }

    }
}
