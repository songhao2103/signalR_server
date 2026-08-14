using SignalingServer.Models;

namespace demo_signalR.Contracts;

/// <summary>
/// Quản lý vòng đời của các cuộc gọi (CallSession) đang diễn ra.
///
/// Vì sao cần Service này tách riêng khỏi Hub?
/// - Logic "cuộc gọi nào đang Ringing, ai đang Connected với ai" là business logic
///   thuần túy, không liên quan gì đến SignalR. Tách ra giúp Hub gọn nhẹ, và logic này
///   có thể unit test độc lập (mock ICallService) mà không cần dựng SignalR TestHost.
/// </summary>
public interface ICallService
{
    /// <summary>
    /// Tạo một cuộc gọi mới ở trạng thái Ringing khi CallerId bấm Call tới CalleeId.
    /// </summary>
    /// <returns>CallSession vừa tạo.</returns>
    CallSession InitiateCall(int callerId, int calleeId);

    /// <summary>
    /// Đánh dấu cuộc gọi mà calleeId đang là người nhận chuyển sang Connected.
    /// </summary>
    /// <returns>CallSession tương ứng, hoặc null nếu không tìm thấy cuộc gọi nào đang Ringing cho calleeId này.</returns>
    CallSession? AcceptCall(int calleeId);

    /// <summary>
    /// Từ chối và xóa cuộc gọi mà calleeId đang là người nhận.
    /// </summary>
    /// <returns>CallSession vừa bị xóa, hoặc null nếu không tìm thấy.</returns>
    CallSession? RejectCall(int calleeId);

    /// <summary>
    /// Kết thúc cuộc gọi mà userId đang tham gia (có thể là Caller hoặc Callee).
    /// </summary>
    /// <returns>CallSession vừa bị xóa, hoặc null nếu userId hiện không ở trong cuộc gọi nào.</returns>
    CallSession? EndCall(int userId);

    /// <summary>
    /// Tìm cuộc gọi hiện tại mà userId đang tham gia (dù là Caller hay Callee), dùng khi
    /// cần dọn dẹp lúc user bị mất kết nối đột ngột (OnDisconnectedAsync).
    /// </summary>
    CallSession? GetActiveSessionForUser(int userId);
}
