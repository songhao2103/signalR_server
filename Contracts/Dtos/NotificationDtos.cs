namespace demo_signalR.Contracts.Dtos;

/// <summary>
/// Server push xuống cho Callee khi có người gọi tới (để hiện popup Accept/Reject).
/// </summary>
public class IncomingCallNotification
{
    /// <summary>UserId của người đang gọi tới.</summary>
    public required int FromUserId { get; set; }
}

/// <summary>
/// Server push xuống dùng chung cho các thông báo: "đối phương đã Accept",
/// "đối phương đã Reject", "đối phương đã End call".
/// Hub method riêng biệt (CallAccepted/CallRejected/CallEnded ở phía client JS)
/// sẽ quyết định ý nghĩa, DTO chỉ cần biết ai vừa thực hiện hành động đó.
/// </summary>
public class CallActionNotification
{
    /// <summary>UserId của người vừa thực hiện hành động (Accept/Reject/End).</summary>
    public required int FromUserId { get; set; }
}

/// <summary>Server forward SDP (Offer hoặc Answer) tới đúng người nhận.</summary>
public class SdpMessageNotification
{
    /// <summary>UserId của người đã gửi đoạn SDP này.</summary>
    public required int FromUserId { get; set; }

    /// <summary>Nội dung SDP, giữ nguyên vẹn, server không chỉnh sửa.</summary>
    public required string Sdp { get; set; }
}

/// <summary>Server forward ICE Candidate tới đúng người nhận.</summary>
public class IceCandidateNotification
{
    /// <summary>UserId của người đã gửi ICE Candidate này.</summary>
    public required int FromUserId { get; set; }   

    /// <summary>Nội dung ICE Candidate, giữ nguyên vẹn.</summary>
    public required string Candidate { get; set; }
    public required string SdpMid { get; set; }
    public required int SdpMLineIndex { get; set; }

}

/// <summary>
/// Server báo cho Caller biết cuộc gọi không thể thực hiện được
/// (ví dụ: TargetUserId hiện không online). Không bắt buộc trong yêu cầu gốc,
/// nhưng cần thiết để tránh trường hợp Caller bấm Call rồi... im lặng mãi mãi
/// mà không hiểu vì sao, gây trải nghiệm khó hiểu khi demo.
/// </summary>
public class CallFailedNotification
{
    /// <summary>Lý do cuộc gọi thất bại, hiển thị trực tiếp cho người dùng.</summary>
    public required string Reason { get; set; }
}
