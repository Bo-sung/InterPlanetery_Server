using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using ChatClientWPF.Utils;

namespace CommonLib
{
    /// <summary>
    /// 기존 Protocol 객체를 기반으로 한 네트워크 응답 클래스
    /// </summary>
    public class NetworkResponse
    {
        private const string RESULT_KEY = "0";
        private const string MESSAGE_KEY = "1";

        private readonly Protocol receivedProtocol;
        private readonly Dictionary<string, object> sendParameters;

        public readonly StateCode resultCode;
        public readonly bool isSuccess;
        public readonly Protocol originalProtocol;

        /// <summary>
        /// Protocol 객체로부터 NetworkResponse 생성
        /// </summary>
        public NetworkResponse(Protocol receivedProtocol, Dictionary<string, object> sendParameters = null)
        {
            this.receivedProtocol = receivedProtocol ?? throw new ArgumentNullException(nameof(receivedProtocol));
            this.sendParameters = sendParameters ?? new Dictionary<string, object>();
            this.originalProtocol = receivedProtocol;

            // 결과 코드 파싱 - status 키를 우선 확인
            int resultKey = -1; // 초기값을 명확하게 설정

            // 1. status 키 확인 (서버 응답의 표준 키)
            var statusValue = receivedProtocol.GetParam<object>("status");
            if (statusValue != null)
            {
                if (statusValue is byte byteValue)
                    resultKey = byteValue;
                else if (statusValue is int intValue)
                    resultKey = intValue;
                else if (statusValue is long longValue)
                    resultKey = (int)longValue;
            }

            // 2. RESULT_KEY("0")로 시도 (하위 호환성)
            if (resultKey == -1)
            {
                var resultValue = receivedProtocol.GetParam<object>(RESULT_KEY);
                if (resultValue != null)
                {
                    if (resultValue is byte byteVal)
                        resultKey = byteVal;
                    else if (resultValue is int intVal)
                        resultKey = intVal;
                    else if (resultValue is long longVal)
                        resultKey = (int)longVal;
                }
            }

            // 3. 여전히 파싱 실패면 기본값 사용
            if (resultKey == -1)
            {
                resultKey = (int)StateCode.SUCCESS;
            }

            resultCode = (StateCode)resultKey;
            isSuccess = resultCode == StateCode.SUCCESS;
        }

        /// <summary>
        /// 성공 응답 생성 (팩토리 메서드)
        /// </summary>
        public static NetworkResponse CreateSuccess(Dictionary<string, object> data = null)
        {
            var protocol = new Protocol((int)ProtocolType.RESPONSE);
            protocol.AddParam("status", (byte)StateCode.SUCCESS);
            
            if (data != null)
            {
                foreach (var kvp in data)
                {
                    protocol.AddParam(kvp.Key, kvp.Value);
                }
            }

            return new NetworkResponse(protocol);
        }

        /// <summary>
        /// 실패 응답 생성 (팩토리 메서드)
        /// </summary>
        public static NetworkResponse CreateFailure(StateCode stateCode, string message = "")
        {
            var protocol = new Protocol((int)ProtocolType.RESPONSE);
            protocol.AddParam("status", (byte)stateCode);
            protocol.AddParam("message", string.IsNullOrEmpty(message) ? stateCode.ToString() : message);

            return new NetworkResponse(protocol);
        }

        /// <summary>
        /// 매개변수 값 가져오기 (Protocol의 GetParam 위임)
        /// </summary>
        public T GetParam<T>(string key)
        {
            return receivedProtocol.GetParam<T>(key);
        }

        /// <summary>
        /// 구조체 값 가져오기 (Protocol의 GetStruct 위임)
        /// </summary>
        public T GetStruct<T>(string key) where T : struct
        {
            return receivedProtocol.GetStruct<T>(key);
        }

        /// <summary>
        /// 객체/클래스 가져오기 (Protocol의 GetObject 위임)
        /// </summary>
        public T GetObject<T>(string key) where T : class
        {
            return receivedProtocol.GetObject<T>(key);
        }

        /// <summary>
        /// 패킷 객체로 변환
        /// </summary>
        public T GetPacket<T>() where T : IPacketResponse<NetworkResponse>, new()
        {
            T packet = new T();
            packet.Initialize(this);
            return packet;
        }

        /// <summary>
        /// 특정 키의 데이터를 패킷 객체로 변환
        /// </summary>
        public T GetPacket<T>(string key) where T : IPacketResponse<NetworkResponse>, new()
        {
            // 중첩된 데이터를 별도 Protocol로 파싱
            var nestedData = GetParam<Dictionary<string, object>>(key);
            if (nestedData != null)
            {
                var nestedProtocol = new Protocol(receivedProtocol.Type);
                foreach (var kvp in nestedData)
                {
                    nestedProtocol.AddParam(kvp.Key, kvp.Value);
                }
                
                var nestedResponse = new NetworkResponse(nestedProtocol, sendParameters);
                T packet = new T();
                packet.Initialize(nestedResponse);
                return packet;
            }

            // 데이터가 없으면 현재 응답으로 초기화
            T emptyPacket = new T();
            emptyPacket.Initialize(this);
            return emptyPacket;
        }

        /// <summary>
        /// 패킷 배열로 변환
        /// </summary>
        public T[] GetPacketArray<T>(string key) where T : IPacketResponse<NetworkResponse>, new()
        {
            var arrayData = GetParam<object[]>(key);
            if (arrayData == null) return new T[0];

            T[] packets = new T[arrayData.Length];
            for (int i = 0; i < packets.Length; i++)
            {
                if (arrayData[i] is Dictionary<string, object> itemData)
                {
                    var itemProtocol = new Protocol(receivedProtocol.Type);
                    foreach (var kvp in itemData)
                    {
                        itemProtocol.AddParam(kvp.Key, kvp.Value);
                    }
                    
                    var itemResponse = new NetworkResponse(itemProtocol, sendParameters);
                    packets[i] = new T();
                    packets[i].Initialize(itemResponse);
                }
                else
                {
                    packets[i] = new T();
                    packets[i].Initialize(this);
                }
            }

            return packets;
        }

        /// <summary>
        /// 키 존재 여부 확인
        /// </summary>
        public bool ContainsKey(string key)
        {
            return receivedProtocol.GetParam<object>(key) != null;
        }

        /// <summary>
        /// 값이 null인지 확인
        /// </summary>
        public bool IsNull(string key)
        {
            return receivedProtocol.GetParam<object>(key) == null;
        }

        /// <summary>
        /// 전송된 매개변수 가져오기
        /// </summary>
        public T GetSentParam<T>(string key)
        {
            if (sendParameters.TryGetValue(key, out object value))
            {
                if (value is T directValue)
                    return directValue;
                try
                {
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // 변환 실패 시 기본값 반환
                }
            }
            return default(T);
        }

        /// <summary>
        /// 결과 코드에 따른 메시지 표시
        /// </summary>
        public void ShowResultCode()
        {
            if (resultCode == StateCode.SUCCESS)
                return;

            string message = GetParam<string>("message");
            if (string.IsNullOrEmpty(message))
                message = resultCode.ToString();

            // 특별한 처리가 필요한 경우들
            switch (resultCode)
            {
                case StateCode.AUTH_FAILURE:
                    Logger.LogError($"[인증 실패] {message}");
                    break;
                case StateCode.ACCESS_DENY:
                    Logger.LogError($"[접근 거부] {message}");
                    break;
                case StateCode.NO_RESOURCE:
                    Logger.LogWarning($"[리소스 없음] {message}");
                    break;
                case StateCode.SERVER_ERROR:
                    Logger.LogError($"[서버 오류] {message}");
                    break;
                default:
                    Logger.LogError($"[{resultCode}] {message}");
                    break;
            }
        }

        /// <summary>
        /// Protocol 객체로 변환
        /// </summary>
        public Protocol ToProtocol()
        {
            return receivedProtocol;
        }

        /// <summary>
        /// JSON 문자열로 변환
        /// </summary>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(new
            {
                resultCode = resultCode.ToString(),
                isSuccess = isSuccess,
                timestamp = receivedProtocol.Timestamp,
                type = receivedProtocol.Type,
                data = GetAllParameters()
            }, Formatting.Indented);
        }

        /// <summary>
        /// 모든 매개변수 가져오기
        /// </summary>
        private Dictionary<string, object> GetAllParameters()
        {
            var result = new Dictionary<string, object>();
            
            // Protocol의 내부 _parameters에 접근하기 위한 우회 방법
            // 실제로는 Protocol 클래스에 GetAllParameters() 메서드를 추가하는 것이 좋음
            try
            {
                var json = JsonConvert.SerializeObject(receivedProtocol);
                var protocolData = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
                return protocolData ?? new Dictionary<string, object>();
            }
            catch
            {
                return new Dictionary<string, object>();
            }
        }

        /// <summary>
        /// 디버그 정보 출력
        /// </summary>
        public override string ToString()
        {
            return $"NetworkResponse [Success: {isSuccess}, Code: {resultCode}, Type: {receivedProtocol.Type}]";
        }
    }

    /// <summary>
    /// NetworkResponse 기반 패킷 인터페이스
    /// </summary>
    public interface IPacketResponse<T>
    {
        void Initialize(T response);
    }

    /// <summary>
    /// 기존 Protocol을 확장하여 NetworkResponse와 호환성 제공
    /// </summary>
    public static class ProtocolExtensions
    {
        /// <summary>
        /// Protocol을 NetworkResponse로 변환
        /// </summary>
        public static NetworkResponse ToNetworkResponse(this Protocol protocol, Dictionary<string, object> sendParameters = null)
        {
            return new NetworkResponse(protocol, sendParameters);
        }

        /// <summary>
        /// Response Protocol 생성 헬퍼
        /// </summary>
        public static Protocol CreateResponse(int responseId, StateCode status, string message = "")
        {
            var protocol = new Protocol((int)ProtocolType.RESPONSE);
            protocol.AddParam("protoId", responseId);
            protocol.AddParam("status", (byte)status);
            protocol.AddParam("message", string.IsNullOrEmpty(message) ? status.ToString() : message);
            return protocol;
        }

        /// <summary>
        /// Protocol에 모든 매개변수 가져오기 메서드 추가
        /// </summary>
        public static Dictionary<string, object> GetAllParameters(this Protocol protocol)
        {
            // 리플렉션을 사용하여 private _parameters 필드에 접근
            var field = typeof(Protocol).GetField("_parameters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (field != null)
            {
                var parameters = field.GetValue(protocol) as Dictionary<string, object>;
                return new Dictionary<string, object>(parameters ?? new Dictionary<string, object>());
            }
            
            return new Dictionary<string, object>();
        }
    }
}