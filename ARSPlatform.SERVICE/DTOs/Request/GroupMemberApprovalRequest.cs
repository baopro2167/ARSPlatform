namespace ARSPlatform.SERVICE.DTOs.Request
{
    /// <summary>
    /// Request dành riêng cho Lecturer duyệt / từ chối một sinh viên đã có row trong GroupMembers.
    /// Chỉ cho phép thay đổi ActivityStatus và RequestNote, các field khác bị bỏ qua.
    /// </summary>
    public class GroupMemberApprovalRequest
    {
        /// <summary>
        /// Trạng thái mới:
        ///   - "JOINED"   : Lecturer đồng ý cho sinh viên vào nhóm
        ///   - "REJECTED" : Lecturer từ chối
        ///   - "PENDING"  : Chuyển về trạng thái chờ (nếu cần)
        ///   - "LEFT"     : Sinh viên rời nhóm
        /// </summary>
        public string? ActivityStatus { get; set; }

        /// <summary>
        /// Ghi chú lý do duyệt / từ chối do Lecturer nhập, sẽ được lưu vào cột RequestNote
        /// và kèm theo notification gửi tới Student.
        /// </summary>
        public string? RequestNote { get; set; }
    }
}
