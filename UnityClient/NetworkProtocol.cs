using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace InterPlanetary.Network
{
    /// <summary>
    /// 크로스-플랫폼 네트워크 통신용 프로토콜 클래스 (Unity 버전)
    /// JSON 기반 직렬화로 구조체/클래스 지원 (Newtonsoft.Json 사용)
    /// </summary>
    public class Protocol
    {
        // 데이터 타입 정의자
        private const byte TYPE_BYTE = 0x10;
        private const byte TYPE_SHORT = 0x11;
        private const byte TYPE_INT = 0x12;
        private const byte TYPE_LONG = 0x13;
        private const byte TYPE_FLOAT = 0x14;
        private const byte TYPE_DOUBLE = 0x15;
        private const byte TYPE_BOOL = 0x16;
        private const byte TYPE_STRING = 0x17;
        private const byte TYPE_BYTES = 0x18;
        private const byte TYPE_OBJECT = 0x19;

        /// <summary>
        /// 프로토콜 타입
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public long Timestamp { get; set; }

        /// <summary>
        /// 데이터 저장소
        /// </summary>
        private Dictionary<string, (byte type, object value)> m_data;

        public Protocol()
        {
            m_data = new Dictionary<string, (byte, object)>();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public Protocol(int type) : this()
        {
            Type = type;
        }

        #region Add Parameters

        public Protocol AddParam(string key, byte value)
        {
            m_data[key] = (TYPE_BYTE, value);
            return this;
        }

        public Protocol AddParam(string key, short value)
        {
            m_data[key] = (TYPE_SHORT, value);
            return this;
        }

        public Protocol AddParam(string key, int value)
        {
            m_data[key] = (TYPE_INT, value);
            return this;
        }

        public Protocol AddParam(string key, long value)
        {
            m_data[key] = (TYPE_LONG, value);
            return this;
        }

        public Protocol AddParam(string key, float value)
        {
            m_data[key] = (TYPE_FLOAT, value);
            return this;
        }

        public Protocol AddParam(string key, double value)
        {
            m_data[key] = (TYPE_DOUBLE, value);
            return this;
        }

        public Protocol AddParam(string key, bool value)
        {
            m_data[key] = (TYPE_BOOL, value);
            return this;
        }

        public Protocol AddParam(string key, string value)
        {
            m_data[key] = (TYPE_STRING, value ?? "");
            return this;
        }

        public Protocol AddParam(string key, byte[] value)
        {
            m_data[key] = (TYPE_BYTES, value ?? new byte[0]);
            return this;
        }

        public Protocol AddObject<T>(string key, T value)
        {
            m_data[key] = (TYPE_OBJECT, value);
            return this;
        }

        #endregion

        #region Get Parameters

        public T GetParam<T>(string key, T defaultValue = default)
        {
            if (!m_data.ContainsKey(key))
                return defaultValue;

            try
            {
                object value = m_data[key].value;

                // JSON 문자열에서 역직렬화 (Newtonsoft.Json)
                if (value is string jsonStr && typeof(T) != typeof(string))
                {
                    return JsonConvert.DeserializeObject<T>(jsonStr);
                }

                // JToken에서 역직렬화
                if (value is JToken jToken)
                {
                    return jToken.ToObject<T>();
                }

                // 타입 매칭
                if (value is T typedValue)
                    return typedValue;

                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to get param '{key}': {e.Message}");
                return defaultValue;
            }
        }

        public byte[] GetBytes(string key)
        {
            if (!m_data.ContainsKey(key))
                return null;
            return m_data[key].value as byte[];
        }

        public T GetStruct<T>(string key) where T : struct
        {
            if (!m_data.ContainsKey(key))
                return default;

            object value = m_data[key].value;

            // JSON 문자열에서 역직렬화 (Newtonsoft.Json)
            if (value is string jsonStr)
            {
                return JsonConvert.DeserializeObject<T>(jsonStr);
            }

            // JToken에서 역직렬화
            if (value is JToken jToken)
            {
                return jToken.ToObject<T>();
            }

            if (value is T structValue)
                return structValue;

            return default;
        }

        public T GetObject<T>(string key) where T : class
        {
            if (!m_data.ContainsKey(key))
                return default;

            object value = m_data[key].value;

            // JSON 문자열에서 역직렬화 (Newtonsoft.Json)
            if (value is string jsonStr)
            {
                return JsonConvert.DeserializeObject<T>(jsonStr);
            }

            // JToken에서 역직렬화
            if (value is JToken jToken)
            {
                return jToken.ToObject<T>();
            }

            if (value is T typedValue)
                return typedValue;

            return default;
        }

        public bool HasParam(string key)
        {
            return m_data.ContainsKey(key);
        }

        #endregion

        #region Serialization

        /// <summary>
        /// 네트워크 직렬화
        /// 형식: [4바이트 크기][4바이트 타입][8바이트 타임스탬프][2바이트 데이터개수][데이터...]
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // 헤더
                writer.Write((int)0);        // 크기 (나중에 계산)
                writer.Write(Type);          // 프로토콜 타입
                writer.Write(Timestamp);     // 타임스탬프
                writer.Write((ushort)m_data.Count); // 데이터 개수

                // 데이터 직렬화
                foreach (var kvp in m_data)
                {
                    // 키
                    byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                    writer.Write((byte)keyBytes.Length);
                    writer.Write(keyBytes);

                    // 타입 및 값
                    byte dataType = kvp.Value.type;
                    object value = kvp.Value.value;
                    writer.Write(dataType);

                    switch (dataType)
                    {
                        case TYPE_BYTE:
                            writer.Write((byte)value);
                            break;
                        case TYPE_SHORT:
                            writer.Write((short)value);
                            break;
                        case TYPE_INT:
                            writer.Write((int)value);
                            break;
                        case TYPE_LONG:
                            writer.Write((long)value);
                            break;
                        case TYPE_FLOAT:
                            writer.Write((float)value);
                            break;
                        case TYPE_DOUBLE:
                            writer.Write((double)value);
                            break;
                        case TYPE_BOOL:
                            writer.Write((bool)value);
                            break;
                        case TYPE_STRING:
                            byte[] strBytes = Encoding.UTF8.GetBytes((string)value);
                            writer.Write((ushort)strBytes.Length);
                            writer.Write(strBytes);
                            break;
                        case TYPE_BYTES:
                            byte[] bytes = (byte[])value;
                            writer.Write(bytes.Length);
                            writer.Write(bytes);
                            break;
                        case TYPE_OBJECT:
                            string json = JsonConvert.SerializeObject(value);
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                            writer.Write(jsonBytes.Length);
                            writer.Write(jsonBytes);
                            break;
                    }
                }

                // 크기 계산
                byte[] result = ms.ToArray();
                int totalLength = result.Length - 4;
                BitConverter.GetBytes(totalLength).CopyTo(result, 0);

                return result;
            }
        }

        /// <summary>
        /// 네트워크 역직렬화
        /// </summary>
        public static Protocol Deserialize(byte[] bytes)
        {
            if (bytes == null || bytes.Length < 14)
                return null;

            using (MemoryStream ms = new MemoryStream(bytes))
            using (BinaryReader reader = new BinaryReader(ms))
            {
                // 헤더 읽기
                int length = reader.ReadInt32();
                int type = reader.ReadInt32();
                long timestamp = reader.ReadInt64();
                ushort dataCount = reader.ReadUInt16();

                Protocol protocol = new Protocol(type)
                {
                    Timestamp = timestamp
                };

                // 데이터 읽기
                for (int i = 0; i < dataCount; i++)
                {
                    // 키 읽기
                    byte keyLength = reader.ReadByte();
                    byte[] keyBytes = reader.ReadBytes(keyLength);
                    string key = Encoding.UTF8.GetString(keyBytes);

                    // 타입 및 값 읽기
                    byte dataType = reader.ReadByte();

                    switch (dataType)
                    {
                        case TYPE_BYTE:
                            protocol.m_data[key] = (dataType, reader.ReadByte());
                            break;
                        case TYPE_SHORT:
                            protocol.m_data[key] = (dataType, reader.ReadInt16());
                            break;
                        case TYPE_INT:
                            protocol.m_data[key] = (dataType, reader.ReadInt32());
                            break;
                        case TYPE_LONG:
                            protocol.m_data[key] = (dataType, reader.ReadInt64());
                            break;
                        case TYPE_FLOAT:
                            protocol.m_data[key] = (dataType, reader.ReadSingle());
                            break;
                        case TYPE_DOUBLE:
                            protocol.m_data[key] = (dataType, reader.ReadDouble());
                            break;
                        case TYPE_BOOL:
                            protocol.m_data[key] = (dataType, reader.ReadBoolean());
                            break;
                        case TYPE_STRING:
                            ushort strLength = reader.ReadUInt16();
                            byte[] strBytes = reader.ReadBytes(strLength);
                            protocol.m_data[key] = (dataType, Encoding.UTF8.GetString(strBytes));
                            break;
                        case TYPE_BYTES:
                            int bytesLength = reader.ReadInt32();
                            byte[] bytesData = reader.ReadBytes(bytesLength);
                            protocol.m_data[key] = (dataType, bytesData);
                            break;
                        case TYPE_OBJECT:
                            int jsonLength = reader.ReadInt32();
                            byte[] jsonBytes = reader.ReadBytes(jsonLength);
                            string json = Encoding.UTF8.GetString(jsonBytes);
                            protocol.m_data[key] = (dataType, json);
                            break;
                    }
                }

                return protocol;
            }
        }

        #endregion

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[Protocol] Type: {Type}, Timestamp: {Timestamp}");
            foreach (var kvp in m_data)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value.value} (Type: 0x{kvp.Value.type:X2})");
            }
            return sb.ToString();
        }
    }
}
