
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CommonLib
{
    /// <summary>
    /// CSV 파일 데이터의 직렬화/역직렬화를 처리하는 범용 클래스
    /// </summary>
    public static class CsvDataManager
    {
        /// <summary>
        /// CSV 파일에서 데이터를 로드하여 지정된 타입의 객체 리스트로 변환합니다.
        /// </summary>
        /// <typeparam name="T">변환할 객체 타입</typeparam>
        /// <param name="filePath">CSV 파일 경로</param>
        /// <param name="parser">string[]을 T 객체로 변환하는 파서 함수</param>
        /// <param name="hasHeader">CSV 파일이 헤더를 포함하는지 여부</param>
        /// <returns>로드된 객체의 IEnumerable</returns>
        public static IEnumerable<T> LoadData<T>(string filePath, Func<string[], T> parser, bool hasHeader = true)
        {
            var lines = File.ReadAllLines(filePath);
            var dataLines = hasHeader ? lines.Skip(1) : lines;

            return dataLines.Select(line => parser(line.Split(',')));
        }

        /// <summary>
        /// 객체 리스트를 CSV 파일로 저장합니다.
        /// </summary>
        /// <typeparam name="T">저장할 객체 타입</typeparam>
        /// <param name="filePath">저장할 CSV 파일 경로</param>
        /// <param name="data">저장할 데이터</param>
        /// <param name="header">CSV 파일의 헤더 문자열</param>
        /// <param name="formatter">T 객체를 CSV 라인(string)으로 변환하는 포맷터 함수</param>
        public static void SaveData<T>(string filePath, IEnumerable<T> data, string header, Func<T, string> formatter)
        {
            var csvBuilder = new StringBuilder();
            csvBuilder.AppendLine(header);

            foreach (var item in data)
            {
                csvBuilder.AppendLine(formatter(item));
            }

            File.WriteAllText(filePath, csvBuilder.ToString());
        }
    }
}
