namespace demo_signalR.Contracts.Dtos;

/// <summary>
/// Client gửi DTO này khi bấm nút "Call" để gọi tới một user khác.
/// </summary>
public class CallUserRequest
{
    /// <summary>UserId của người muốn gọi tới.</summary>
    public required int TargetUserId { get; set; }
}

/// <summary>
/// Dùng chung cho 3 hành động: Accept, Reject, End.
/// Vì cả 3 hành động này về bản chất chỉ cần biết "tôi đang thao tác với ai",
/// không cần thêm payload nào khác, nên gộp chung 1 DTO để tránh dư thừa.
/// Hub method riêng biệt (AcceptCall/RejectCall/EndCall) sẽ quyết định ý nghĩa của request.
/// </summary>
public class CallActionRequest
{
    /// <summary>UserId của đối phương trong cuộc gọi đang thao tác.</summary>
    public required int TargetUserId { get; set; }
}
