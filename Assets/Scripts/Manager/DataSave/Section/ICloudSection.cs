public interface ICloudSection
{
    string Key { get; }
    string SaveJson();          // 중앙이 저장할 "value(JSON 문자열)" 생성
    void LoadJson(string json); // 중앙이 가져온 "value(JSON 문자열)" 적용
}
