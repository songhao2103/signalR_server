namespace SignalingServer.Models;

/// <summary>
/// Trạng thái của một cuộc gọi tại một thời điểm.
/// Enum này giúp CallService biết được cuộc gọi đang ở bước nào,
/// từ đó chặn được các hành vi vô lý (ví dụ: Accept một cuộc gọi đã bị Reject).
/// </summary>
public enum CallStatus
{
    /// <summary>Bên gọi đã bấm Call, đang chờ bên kia Accept/Reject.</summary>
    Ringing,

    /// <summary>Bên nhận đã Accept, 2 bên đang trong quá trình trao đổi SDP/ICE và/hoặc đang nói chuyện.</summary>
    Connected,

    /// <summary>Cuộc gọi đã kết thúc (do Reject, do End, hoặc do một bên rớt mạng).</summary>
    Ended
}
