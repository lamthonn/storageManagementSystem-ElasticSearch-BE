using DATN.Domain.Entities;
using Nest;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DATN.Application.Utils
{
    public class ElasticSearchService
    {
        private readonly ElasticClient _client;
        private readonly string _indexName = "tai_lieu";
        public ElasticSearchService()
        {
            var settings = new ConnectionSettings(new Uri("http://localhost:9200"))
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
        public async Task<IndexResponse> UpsertTaiLieuAsync(tai_lieu tl)
        {
            var response = await _client.IndexAsync(tl, i => i.Id(tl.Id).Index("tai_lieu"));
            return response;
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
