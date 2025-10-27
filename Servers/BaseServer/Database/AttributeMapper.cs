using System.Collections.Concurrent;
using System.Reflection;
using CommonLib.TableData;
using MySql.Data.MySqlClient;

namespace BaseServer.Database
{
    public static class AttributeMapper
    {
        // 매핑 정보를 캐싱하여 성능 향상
        private static readonly ConcurrentDictionary<Type, Dictionary<string, string>> _mappingCache =
            new ConcurrentDictionary<Type, Dictionary<string, string>>();

        // 레코드 객체로 변환하는 메서드
        public static T MapToRecord<T>(this MySqlDataReader reader) where T : class
        {
            Type type = typeof(T);

            // 레코드 타입인지 확인
            if (!IsRecord(type))
            {
                throw new ArgumentException($"Type {type.Name} is not a record", nameof(T));
            }

            // 매핑 정보 캐싱 - 한 번만 생성하고 재사용
            var mappings = _mappingCache.GetOrAdd(type, t => {
                var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                var ctor = t.GetConstructors()[0];
                var parameters = ctor.GetParameters();

                foreach (var param in parameters)
                {
                    // DbColumn 속성이 있는지 확인
                    var attribute = param.GetCustomAttribute<DbColumnAttribute>();
                    if (attribute != null)
                    {
                        // 속성이 정의한 컬럼명으로 매핑
                        result[attribute.ColumnName] = param.Name;
                    }
                    else
                    {
                        // 속성이 없으면 매개변수명을 그대로 사용 (대소문자 무시)
                        result[param.Name] = param.Name;
                    }
                }

                return result;
            });

            // 생성자 정보 및 매개변수 준비
            var constructor = type.GetConstructors()[0];
            var ctorParams = constructor.GetParameters();
            var args = new object[ctorParams.Length];

            // 각 매개변수에 대해 매핑된 값 할당
            for (int i = 0; i < ctorParams.Length; i++)
            {
                var param = ctorParams[i];
                bool found = false;

                // 모든 DB 필드를 검사하여 매핑된 필드 찾기
                for (int j = 0; j < reader.FieldCount; j++)
                {
                    string fieldName = reader.GetName(j);

                    // 매핑 정보에서 일치하는 매개변수 찾기
                    if (mappings.TryGetValue(fieldName, out string mappedParamName) &&
                        string.Equals(mappedParamName, param.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        found = true;

                        if (reader.IsDBNull(j))
                        {
                            // NULL 값 처리
                            args[i] = param.ParameterType.IsValueType ?
                                Activator.CreateInstance(param.ParameterType) : null;
                        }
                        else
                        {
                            try
                            {
                                // 값 변환 시도
                                args[i] = Convert.ChangeType(reader.GetValue(j), param.ParameterType);
                            }
                            catch (Exception ex)
                            {
                                // 변환 실패 시 기본값 사용
                                args[i] = param.ParameterType.IsValueType ?
                                    Activator.CreateInstance(param.ParameterType) : null;
                                // 선택적으로 로그 기록 또는 예외 처리
                            }
                        }
                        break;
                    }
                }

                // 해당 필드를 찾지 못한 경우 기본값 사용
                if (!found)
                {
                    args[i] = param.ParameterType.IsValueType ?
                        Activator.CreateInstance(param.ParameterType) : null;
                }
            }

            // 레코드 객체 생성 및 반환
            return (T)constructor.Invoke(args);
        }

        // 주어진 타입이 레코드인지 확인
        private static bool IsRecord(Type type)
        {
            // C# 9.0 이상의 레코드 타입 확인 방법
            return type.GetMethod("<Clone>$") != null ||
                   type.GetProperties().Any(p => p.Name == "EqualityContract" && p.PropertyType == typeof(Type));
        }
    }
}