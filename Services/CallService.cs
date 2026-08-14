using System.Collections.Concurrent;
using demo_signalR.Contracts;
using SignalingServer.Models;

namespace SignalingServer.Services;

/// <summary>
/// Triển khai ICallService, lưu các cuộc gọi đang hoạt động trong bộ nhớ.
///
/// CÁCH LƯU TRỮ:
/// Dùng 1 ConcurrentDictionary&lt;string, CallSession&gt; với KEY là UserId của
/// BẤT KỲ người tham gia nào (cả Caller lẫn Callee), cùng trỏ tới CHUNG một
/// instance CallSession. Ví dụ cuộc gọi giữa "alice" và "bob" sẽ tạo 2 entry:
///     _sessionsByParticipant["alice"] -> session
///     _sessionsByParticipant["bob"]   -> session
/// Cả 2 entry cùng trỏ tới 1 object CallSession duy nhất.
///
/// Vì sao thiết kế vậy?
/// - Cũng giống ConnectionManager ở Bước 4: cần tra cứu O(1) theo userId bất kỳ,
///   vì AcceptCall/RejectCall được gọi với calleeId, còn EndCall và việc dọn dẹp
///   lúc disconnect có thể xảy ra với userId là Caller HOẶC Callee.
/// - Giới hạn của demo: mỗi user chỉ tham gia được TỐI ĐA 1 cuộc gọi tại một thời điểm
///   (đúng yêu cầu "gọi 1-1", chưa cần gọi nhóm/nhiều cuộc gọi song song) - đây là lý do
///   1 userId chỉ cần trỏ tới 1 CallSession, không cần List&lt;CallSession&gt;.
/// </summary>
public class CallService : ICallService
{
    private readonly ConcurrentDictionary<int, CallSession> _sessionsByParticipant = new();

    public CallSession InitiateCall(int callerId, int calleeId)
    {
        var session = new CallSession
        {
            CallerId = callerId,
            CalleeId = calleeId,
            Status = CallStatus.Ringing
        };

        // Đăng ký session dưới cả 2 khóa để tra cứu O(1) từ phía nào cũng được.
        _sessionsByParticipant[callerId] = session;
        _sessionsByParticipant[calleeId] = session;

        return session;
    }

    public CallSession? AcceptCall(int calleeId)
    {
        if (!_sessionsByParticipant.TryGetValue(calleeId, out var session))
        {
            return null; // Không có cuộc gọi nào liên quan tới calleeId này.
        }

        // Kiểm tra chặt: đúng là calleeId đang ở vai trò "người được gọi" (không phải
        // đang là Caller của một cuộc gọi khác), và cuộc gọi đang ở trạng thái Ringing
        // (chưa bị Accept/Reject/End trước đó - tránh xử lý trùng do double-click, race...).
        if (session.CalleeId != calleeId || session.Status != CallStatus.Ringing)
        {
            return null;
        }

        session.Status = CallStatus.Connected;
        return session;
    }

    public CallSession? RejectCall(int calleeId)
    {
        if (!_sessionsByParticipant.TryGetValue(calleeId, out var session))
        {
            return null;
        }

        if (session.CalleeId != calleeId || session.Status != CallStatus.Ringing)
        {
            return null;
        }

        RemoveSession(session);
        return session;
    }

    public CallSession? EndCall(int userId)
    {
        if (!_sessionsByParticipant.TryGetValue(userId, out var session))
        {
            return null; // userId hiện không tham gia cuộc gọi nào.
        }

        RemoveSession(session);
        session.Status = CallStatus.Ended;
        return session;
    }

    public CallSession? GetActiveSessionForUser(int userId)
    {
        return _sessionsByParticipant.TryGetValue(userId, out var session) ? session : null;
    }

    /// <summary>
    /// Xóa 1 CallSession khỏi dictionary dưới cả 2 khóa (CallerId và CalleeId).
    /// Tách thành method riêng vì logic này lặp lại ở cả RejectCall và EndCall.
    /// </summary>
    private void RemoveSession(CallSession session)
    {
        _sessionsByParticipant.TryRemove(session.CallerId, out _);
        _sessionsByParticipant.TryRemove(session.CalleeId, out _);
    }
}
