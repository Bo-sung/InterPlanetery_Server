using CommonLib.TableData;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BaseServer.Database;

namespace BaseServer.Database
{
    /// <summary>
    /// WPF 클라이언트용 DB Repository
    /// BaseServer의 DB_Table과 유사한 기능 제공
    /// </summary>
    public class GameDBRepository
    {
        private DB_Table? _tableCache;
        private readonly object _cacheLock = new object();

        public GameDBRepository()
        {
            // 초기화 시 테이블 캐시 생성
            _tableCache = new DB_Table();
        }

        /// <summary>
        /// 테이블 캐시 반환 (MapService에서 사용)
        /// </summary>
        public DB_Table? GetTableCache()
        {
            lock (_cacheLock)
            {
                // 캐시가 비어있으면 DB에서 로드
                if (_tableCache == null || _tableCache.Map_info.Count == 0)
                {
                    UpdateTableCache();
                }
                return _tableCache;
            }
        }

        /// <summary>
        /// DB에서 테이블 데이터를 새로 로드
        /// </summary>
        public void UpdateTableCache()
        {
            lock (_cacheLock)
            {
                if (_tableCache == null)
                {
                    _tableCache = new DB_Table();
                }
                // AppConfig에서 연결 문자열 가져오기
                string connectionString = CommonLib.AppConfig.Instance.DatabaseConnectionString;
                _tableCache.UpdateTable(connectionString);
            }
        }

        public string GetFleetTypesQuery()
        {
            // QueryManager에서 SQL 쿼리 문자열을 가져와 그대로 반환합니다.
            return QueryManager.GetQuery("FleetMapper.getFleetTypes");
        }

        // 예시: ID로 특정 함대 타입을 가져오는 쿼리를 반환하는 함수

        public string GetFleetTypeByIdQuery(int id)
        {
            string sql = QueryManager.GetQuery("FleetMapper.getFleetTypeById");
            // 간단한 파라미터 치환 로직이 필요할 수 있습니다.
            return sql.Replace("@id", id.ToString());
        }

        public string GetFleetInfo(int id)
        {
            string sql = QueryManager.GetQuery("TableMapper.getFleetInfo");
            // 간단한 파라미터 치환 로직이 필요할 수 있습니다.
            return sql.Replace("@id", id.ToString());
        }
    }
}