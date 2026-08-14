namespace SignalingServer.Models;

/// <summary>
/// Đại diện cho một cuộc gọi 1-1 đang diễn ra (hoặc vừa kết thúc) giữa 2 user.
///
/// Vì sao cần Model này thay vì chỉ forward message qua lại?
/// - Để server biết "ai đang gọi cho ai" tại một thời điểm, phục vụ việc:
///   + Chặn trường hợp A đang gọi B thì A lại gọi thêm C (nếu muốn giới hạn 1 cuộc gọi/lúc).
///   + Khi A mất kết nối đột ngột, server biết cần báo cho B là "cuộc gọi đã kết thúc".
/// - Đây là state tối thiểu, KHÔNG lưu lịch sử (không có thời gian bắt đầu/kết thúc,
///   không ghi log ra đâu cả) đúng theo yêu cầu "chưa cần lưu lịch sử".
/// </summary>
public class CallSession
{
    /// <summary>UserId của người thực hiện cuộc gọi (bấm nút Call).</summary>
    public required int CallerId { get; set; }

    /// <summary>UserId của người nhận cuộc gọi (thấy popup Accept/Reject).</summary>
    public required int CalleeId { get; set; }

    /// <summary>Trạng thái hiện tại của cuộc gọi.</summary>
    public CallStatus Status { get; set; } = CallStatus.Ringing;
}
