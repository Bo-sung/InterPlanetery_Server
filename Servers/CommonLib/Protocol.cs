using System.Text;
using System.Text.Json;

namespace CommonLib
{
    /// <summary>
    /// 크로스-플랫폼 및 네트워크 통신용 프로토콜 클래스
    /// JSON 기반 직렬화로 구조체/클래스 지원
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
        private const byte TYPE_OBJECT = 0x19;  // JSON 직렬화용 객체

        /// <summary>
        /// 프로토콜 타입 (int로 정의해 다양한 값 사용)
        /// </summary>
        public int Type { get; set; }

        /// <summary>
        /// 데이터 저장소
        /// </summary>
        private Dictionary<string, (byte type, object value)> m_data;

        /// <summary>
        /// 타임스탬프
        /// </summary>
        public long Timestamp { get; set; }

        // 기본 생성자
        public Protocol()
        {
            m_data = new Dictionary<string, (byte, object)>();
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        // 타입 지정 생성자
        public Protocol(int _type) : this()
        {
            Type = _type;
        }

        /// <summary>
        /// 파라미터 추가 (기본 타입들 - 메서드 체이닝)
        /// </summary>
        public Protocol AddParam(string _key, byte _value)
        {
            m_data[_key] = (TYPE_BYTE, _value);
            return this;
        }

        public Protocol AddParam(string _key, short _value)
        {
            m_data[_key] = (TYPE_SHORT, _value);
            return this;
        }

        public Protocol AddParam(string _key, int _value)
        {
            m_data[_key] = (TYPE_INT, _value);
            return this;
        }

        public Protocol AddParam(string _key, long _value)
        {
            m_data[_key] = (TYPE_LONG, _value);
            return this;
        }

        public Protocol AddParam(string _key, float _value)
        {
            m_data[_key] = (TYPE_FLOAT, _value);
            return this;
        }

        public Protocol AddParam(string _key, double _value)
        {
            m_data[_key] = (TYPE_DOUBLE, _value);
            return this;
        }

        public Protocol AddParam(string _key, bool _value)
        {
            m_data[_key] = (TYPE_BOOL, _value);
            return this;
        }

        public Protocol AddParam(string _key, string _value)
        {
            m_data[_key] = (TYPE_STRING, _value ?? "");
            return this;
        }

        public Protocol AddParam(string _key, byte[] _value)
        {
            m_data[_key] = (TYPE_BYTES, _value ?? new byte[0]);
            return this;
        }

        /// <summary>
        /// 구조체 추가 (JSON 직렬화)
        /// </summary>
        public Protocol AddStruct<T>(string _key, T _value) where T : struct
        {
            m_data[_key] = (TYPE_OBJECT, _value);
            return this;
        }

        /// <summary>
        /// 클래스/객체 추가 (JSON 직렬화)
        /// </summary>
        public Protocol AddObject<T>(string _key, T _value) where T : class
        {
            m_data[_key] = (TYPE_OBJECT, _value);
            return this;
        }

        /// <summary>
        /// 파라미터 값 가져오기
        /// </summary>
        public T GetParam<T>(string _key, T _defaultValue = default)
        {
            if (!m_data.ContainsKey(_key))
                return _defaultValue;

            try
            {
                object value = m_data[_key].value;

                // JSON 역직렬화된 객체인 경우
                if (value is JsonElement jsonElement)
                {
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
                }

                // 정확한 타입인 경우
                if (value is T typedValue)
                    return typedValue;

                // 기본 타입 변환
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return _defaultValue;
            }
        }

        /// <summary>
        /// 바이트 배열 파라미터 가져오기
        /// </summary>
        public byte[] GetBytes(string _key)
        {
            if (!m_data.ContainsKey(_key))
                return null;
            return m_data[_key].value as byte[];
        }

        /// <summary>
        /// 구조체 파라미터 가져오기
        /// </summary>
        public T GetStruct<T>(string _key) where T : struct
        {
            if (!m_data.ContainsKey(_key))
                return default;

            object value = m_data[_key].value;

            // JSON 역직렬화된 객체인 경우
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            if (value is T structValue)
                return structValue;

            return default;
        }

        /// <summary>
        /// 클래스/객체 파라미터 가져오기
        /// </summary>
        public T GetObject<T>(string _key) where T : class
        {
            if (!m_data.ContainsKey(_key))
                return null;

            object value = m_data[_key].value;

            // JSON 역직렬화된 객체인 경우
            if (value is JsonElement jsonElement)
            {
                return JsonSerializer.Deserialize<T>(jsonElement.GetRawText());
            }

            if (value is T classValue)
                return classValue;

            return null;
        }

        /// <summary>
        /// 파라미터 존재 여부 확인
        /// </summary>
        public bool HasParam(string _key)
        {
            return m_data.ContainsKey(_key);
        }

        /// <summary>
        /// 네트워크 직렬화
        /// 형식: [4바이트 크기][4바이트 타입][8바이트 타임스탬프][2바이트 데이터개수][데이터...]
        /// </summary>
        public byte[] Serialize()
        {
            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(ms))
            {
                // 헤더 (나중에 크기 계산해서 다시 쓸 것)
                writer.Write((int)0);        // 크기 자리 (나중에 계산)
                writer.Write(Type);          // 프로토콜 타입 (int)
                writer.Write(Timestamp);     // 타임스탬프
                writer.Write((ushort)m_data.Count); // 데이터 개수

                // 데이터 직렬화
                foreach (var kvp in m_data)
                {
                    // 키 계산
                    byte[] keyBytes = Encoding.UTF8.GetBytes(kvp.Key);
                    writer.Write((byte)keyBytes.Length);
                    writer.Write(keyBytes);

                    // 타입 및 값 계산
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
                            // JSON으로 직렬화
                            string json = JsonSerializer.Serialize(value);
                            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);
                            writer.Write(jsonBytes.Length);
                            writer.Write(jsonBytes);
                            break;
                    }
                }

                // 크기 자리 계산
                byte[] result = ms.ToArray();
                int totalLength = result.Length - 4;
                BitConverter.GetBytes(totalLength).CopyTo(result, 0);

                return result;
            }
        }

        /// <summary>
        /// 네트워크 역직렬화
        /// </summary>
        public static Protocol Deserialize(byte[] _bytes)
        {
            if (_bytes == null || _bytes.Length < 14) // 최소 헤더 크기
                return null;

            using (MemoryStream ms = new MemoryStream(_bytes))
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
                            // JsonElement로 저장 (나중에 역직렬화할 타입으로 변환)
                            JsonElement jsonElement = JsonSerializer.Deserialize<JsonElement>(json);
                            protocol.m_data[key] = (dataType, jsonElement);
                            break;
                    }
                }

                return protocol;
            }
        }

        /// <summary>
        /// 디버깅용 문자열 표현
        /// </summary>
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



    // ========== 사용 예제 ==========

    // 구조체 예제
    public struct PlayerData
    {
        public string PlayerId { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public int Hp { get; set; }
        public bool IsAlive { get; set; }

        public override string ToString()
        {
            return $"Player({PlayerId}): Pos({X},{Y}), HP={Hp}, Alive={IsAlive}";
        }
    }

    public struct Vector3Data
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        public override string ToString()
        {
            return $"Vector3({X}, {Y}, {Z})";
        }
    }

    // 클래스 예제
    public class GameRoom
    {
        public string RoomId { get; set; }
        public List<string> Players { get; set; }
        public int MaxPlayers { get; set; }
        public bool IsStarted { get; set; }

        public GameRoom()
        {
            Players = new List<string>();
        }

        public override string ToString()
        {
            return $"Room({RoomId}): {Players.Count}/{MaxPlayers}, Started={IsStarted}";
        }
    }

    public class GameSettings
    {
        public string MapName { get; set; }
        public int TimeLimit { get; set; }
        public Dictionary<string, int> Scores { get; set; }

        public GameSettings()
        {
            Scores = new Dictionary<string, int>();
        }

        public override string ToString()
        {
            return $"Settings: Map={MapName}, Time={TimeLimit}s";
        }
    }

    public class ProtocolExample
    {
        public static void Example()
        {
            // 1. 구조체 생성
            PlayerData player1 = new PlayerData
            {
                PlayerId = "player1",
                X = 10.5f,
                Y = 20.3f,
                Hp = 100,
                IsAlive = true
            };

            Vector3Data velocity = new Vector3Data
            {
                X = 1.0f,
                Y = 0.5f,
                Z = 0.0f
            };

            // 2. 클래스 생성
            GameRoom room = new GameRoom
            {
                RoomId = "room_001",
                MaxPlayers = 4,
                IsStarted = false
            };
            room.Players.Add("player1");
            room.Players.Add("player2");

            GameSettings settings = new GameSettings
            {
                MapName = "Desert Arena",
                TimeLimit = 300
            };
            settings.Scores["player1"] = 150;
            settings.Scores["player2"] = 120;

            // 3. 프로토콜 생성 (구조체 + 클래스 혼합)
            Protocol gameState = new Protocol(1001)
                .AddStruct("player1", player1)
                .AddStruct("velocity", velocity)
                .AddObject("room", room)
                .AddObject("settings", settings)
                .AddParam("frameCount", 12345)
                .AddParam("gameName", "MyAwesomeGame");

            Console.WriteLine("Original:");
            Console.WriteLine(gameState.ToString());

            // 4. 직렬화
            byte[] serialized = gameState.Serialize();
            Console.WriteLine($"\nSerialized Size: {serialized.Length} bytes");

            // 5. 역직렬화
            Protocol deserialized = Protocol.Deserialize(serialized);
            Console.WriteLine("\nDeserialized:");

            // 6. 구조체 추출
            PlayerData receivedPlayer = deserialized.GetStruct<PlayerData>("player1");
            Vector3Data receivedVelocity = deserialized.GetStruct<Vector3Data>("velocity");

            // 7. 클래스 추출
            GameRoom receivedRoom = deserialized.GetObject<GameRoom>("room");
            GameSettings receivedSettings = deserialized.GetObject<GameSettings>("settings");

            // 8. 기본 타입 추출
            int frameCount = deserialized.GetParam<int>("frameCount");
            string gameName = deserialized.GetParam<string>("gameName");

            Console.WriteLine($"\nExtracted Values:");
            Console.WriteLine(receivedPlayer.ToString());
            Console.WriteLine(receivedVelocity.ToString());
            Console.WriteLine(receivedRoom.ToString());
            Console.WriteLine(receivedSettings.ToString());
            Console.WriteLine($"Frame: {frameCount}, Game: {gameName}");
        }
    }
}