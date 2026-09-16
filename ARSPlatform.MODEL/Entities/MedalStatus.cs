namespace ARSPlatform.MODEL.Entities
{
    /// <summary>
    /// Trạng thái hiển thị / sử dụng của một bản ghi UserMedal đối với người dùng.
    /// Admin có thể tắt (Inactive) huy hiệu của 1 user mà không cần xóa hẳn huy hiệu khỏi hệ thống.
    /// </summary>
    public enum MedalStatus
    {
        Active = 1,
        Inactive = 2
    }
}
