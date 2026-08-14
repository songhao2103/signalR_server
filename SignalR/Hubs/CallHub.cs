using demo_signalR.Contracts;
using demo_signalR.Contracts.Dtos;
using Microsoft.AspNetCore.SignalR;

namespace SignalingServer.SignalR.Hubs;

/// <summary>
/// Hub trung tâm cho toàn bộ luồng signaling của WebRTC.
///
/// NGUYÊN TẮC THIẾT KẾ QUAN TRỌNG NHẤT CỦA FILE NÀY:
/// Hub KHÔNG chứa business logic. Mỗi method trong Hub chỉ làm 3 việc:
///   1. Xác định "tôi (Context.ConnectionId) đang là UserId nào?" qua IConnectionManager.
///   2. Gọi đúng 1 method của Service (IConnectionManager / ICallService) để xử lý logic.
///   3. Dựa trên kết quả trả về, quyết định gửi (hoặc không gửi) notification cho
///      đúng người, qua Clients.Client(connectionId).SendAsync(...).
///
/// Nhờ vậy, mọi quy tắc nghiệp vụ (ví dụ "chỉ được Accept khi đang Ringing") nằm gọn
/// trong CallService - có thể unit test độc lập, và có thể tái sử dụng nếu sau này
/// đổi sang transport khác ngoài SignalR.
/// </summary>
public class CallHub : Hub
{
    private readonly IConnectionManager _connectionManager;
    private readonly ICallService _callService;
    private readonly ILogger<CallHub> _logger;

    // Dependency Injection: ASP.NET Core tự động inject 2 Service này vào mỗi khi
    // tạo instance của Hub (Hub được tạo mới cho MỖI lần gọi method - đây là hành vi
    // mặc định của SignalR, nên KHÔNG được lưu state trực tiếp trên field của Hub -
    // đó cũng là lý do toàn bộ state phải nằm trong Service dạng Singleton).
    public CallHub(IConnectionManager connectionManager, ICallService callService, ILogger<CallHub> logger)
    {
        _connectionManager = connectionManager;
        _callService = callService;
        _logger = logger;
    }

    // ================================================================
    // ĐĂNG KÝ / NGẮT KẾT NỐI
    // ================================================================
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();

        var userId = httpContext?
            .Request.Query["userId"]
            .ToString();

        int.TryParse(userId, out var userIdInt);

        if(userIdInt != 0)
        {
            _connectionManager.AddConnection(
                                                userIdInt,
                                                Context.ConnectionId
                                            );
            await Register(userIdInt);
            await base.OnConnectedAsync();
        }      
    }

    /// <summary>
    /// Client PHẢI gọi method này ngay sau khi kết nối SignalR thành công, để server
    /// biết "connection này tương ứng với UserId nào" (vì demo không có Authentication,
    /// nên không có cách nào khác để server biết danh tính - client tự khai báo).
    /// </summary>
    public Task Register(int userId)
    {
        _connectionManager.AddConnection(userId, Context.ConnectionId);
        _logger.LogInformation("User '{UserId}' registered with connection '{ConnectionId}'", userId, Context.ConnectionId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Được SignalR tự động gọi khi 1 client ngắt kết nối (đóng tab, mất mạng, tự close...).
    /// Nhiệm vụ: dọn dẹp connection khỏi ConnectionManager, VÀ nếu user này đang trong
    /// 1 cuộc gọi dang dở, phải báo cho đối phương biết "cuộc gọi đã kết thúc" - nếu không
    /// đối phương sẽ bị treo mãi ở màn hình gọi mà không hiểu chuyện gì xảy ra.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _connectionManager.RemoveConnectionByConnectionId(Context.ConnectionId);

        if (userId is not null)
        {
            _logger.LogInformation("User '{UserId}' disconnected", userId);

            // Nếu user vừa rớt mạng đang có cuộc gọi dang dở, chủ động kết thúc cuộc gọi
            // đó và báo cho đối phương - tái sử dụng ĐÚNG logic EndCall (không viết logic
            // riêng), đảm bảo tính nhất quán.
            var endedSession = _callService.EndCall(userId.Value);
            if (endedSession is not null)
            {
                var otherUserId = endedSession.CallerId == userId ? endedSession.CalleeId : endedSession.CallerId;
                await NotifyUserIfOnline(otherUserId, "CallEnded", new CallActionNotification { FromUserId = userId.Value });
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    // ================================================================
    // THIẾT LẬP / KẾT THÚC CUỘC GỌI
    // ================================================================

    /// <summary>Client bấm nút "Call" -> gọi method này để bắt đầu gọi tới TargetUserId.</summary>
    public async Task CallUser(CallUserRequest request)
    {
        var callerId = GetCurrentUserIdOrThrow();

        if (!_connectionManager.IsOnline(request.TargetUserId))
        {
            // Người muốn gọi hiện không online -> báo ngay cho Caller, không tạo CallSession.
            await Clients.Caller.SendAsync("CallFailed", new CallFailedNotification
            {
                Reason = $"User '{request.TargetUserId}' is not online."
            });
            return;
        }

        _callService.InitiateCall(callerId, request.TargetUserId);

        await NotifyUserIfOnline(request.TargetUserId, "IncomingCall", new IncomingCallNotification
        {
            FromUserId = callerId
        });
    }

    /// <summary>Client bấm nút "Accept" -> gọi method này.</summary>
    public async Task AcceptCall(CallActionRequest request)
    {
        var calleeId = GetCurrentUserIdOrThrow();

        var session = _callService.AcceptCall(calleeId);
        if (session is null)
        {
            // Không có cuộc gọi hợp lệ nào đang Ringing cho user này (có thể Caller đã
            // hủy trước đó, hoặc double-click) -> không làm gì thêm.
            return;
        }

        // Báo cho Caller biết Callee đã Accept -> phía Caller sẽ bắt đầu tạo Offer (SDP).
        await NotifyUserIfOnline(session.CallerId, "CallAccepted", new CallActionNotification
        {
            FromUserId = calleeId
        });
    }

    /// <summary>Client bấm nút "Reject" -> gọi method này.</summary>
    public async Task RejectCall(CallActionRequest request)
    {
        var calleeId = GetCurrentUserIdOrThrow();

        var session = _callService.RejectCall(calleeId);
        if (session is null)
        {
            return;
        }

        await NotifyUserIfOnline(session.CallerId, "CallRejected", new CallActionNotification
        {
            FromUserId = calleeId
        });
    }

    /// <summary>
    /// Client bấm nút "End Call" -> gọi method này. Dùng chung cho cả 2 phía
    /// (Caller hoặc Callee đều có thể là người chủ động kết thúc cuộc gọi).
    /// </summary>
    public async Task EndCall(CallActionRequest request)
    {
        var userId = GetCurrentUserIdOrThrow();

        var session = _callService.EndCall(userId);
        if (session is null)
        {
            return;
        }

        var otherUserId = session.CallerId == userId ? session.CalleeId : session.CallerId;
        await NotifyUserIfOnline(otherUserId, "CallEnded", new CallActionNotification
        {
            FromUserId = userId
        });
    }

    // ================================================================
    // TRAO ĐỔI SDP (OFFER / ANSWER) VÀ ICE CANDIDATE
    // Server hoàn toàn KHÔNG đọc/hiểu nội dung Sdp/Candidate, chỉ forward nguyên vẹn.
    // ================================================================

    /// <summary>Forward SDP Offer từ Caller sang Callee (gọi sau khi nhận CallAccepted).</summary>
    public async Task SendOffer(SdpMessageRequest request)
    {
        var fromUserId = GetCurrentUserIdOrThrow();

        await NotifyUserIfOnline(request.TargetUserId, "ReceiveOffer", new SdpMessageNotification
        {
            FromUserId = fromUserId,
            Sdp = request.Sdp
        });
    }

    /// <summary>Forward SDP Answer từ Callee ngược lại cho Caller.</summary>
    public async Task SendAnswer(SdpMessageRequest request)
    {
        var fromUserId = GetCurrentUserIdOrThrow();

        await NotifyUserIfOnline(request.TargetUserId, "ReceiveAnswer", new SdpMessageNotification
        {
            FromUserId = fromUserId,
            Sdp = request.Sdp
        });
    }

    /// <summary>
    /// Forward 1 ICE Candidate. Method này sẽ được gọi NHIỀU LẦN liên tiếp trong 1 cuộc gọi
    /// (mỗi khi trình duyệt tìm thêm được 1 candidate mới), khác với Offer/Answer chỉ gửi 1 lần.
    /// </summary>
    public async Task SendIceCandidate(IceCandidateRequest request)
    {
        var fromUserId = GetCurrentUserIdOrThrow();

        await NotifyUserIfOnline(request.TargetUserId, "ReceiveIceCandidate", new IceCandidateNotification
        {
            FromUserId = fromUserId,
            Candidate = request.Candidate,
            SdpMid = request.SdpMid,
            SdpMLineIndex = request.SdpMLineIndex
        });
    }

    // ================================================================
    // HELPER PRIVATE - dùng nội bộ, không phải business logic
    // ================================================================

    /// <summary>
    /// Lấy UserId của người đang gọi method hiện tại (dựa trên ConnectionId).
    /// Ném lỗi nếu user chưa gọi Register() - đây là lỗi lập trình phía client
    /// (gọi nhầm thứ tự), nên throw thẳng để dev phát hiện sớm khi phát triển,
    /// thay vì âm thầm bỏ qua.
    /// </summary>
    private int GetCurrentUserIdOrThrow()
    {
        var userId = _connectionManager.GetUserId(Context.ConnectionId);
        if (userId is null)
        {
            throw new HubException("You must call Register(userId) before using this method.");
        }
        return userId.Value;
    }

    /// <summary>
    /// Gửi 1 message SignalR tới đúng user (qua ConnectionId hiện tại của họ), nếu
    /// user đó vẫn đang online. Nếu user đã offline (ví dụ vừa rớt mạng ngay lúc này),
    /// im lặng bỏ qua thay vì lỗi - đây là hành vi hợp lý cho demo, không cần cơ chế
    /// retry/queue phức tạp.
    /// </summary>
    private async Task NotifyUserIfOnline(int userId, string method, object payload)
    {
        var connectionId = _connectionManager.GetConnectionId(userId);
        if (connectionId is null)
        {
            _logger.LogWarning("Cannot notify '{UserId}' - user is not online.", userId);
            return;
        }

        await Clients.Client(connectionId).SendAsync(method, payload);
    }
}
