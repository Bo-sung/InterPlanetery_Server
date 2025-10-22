
using System.Collections.Generic;

namespace CommonLib
{
    public class GameDBRepository
    {
        public GameDBRepository()
        {
            // 생성자에서 더 이상 DBExecutor가 필요하지 않습니다.
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
