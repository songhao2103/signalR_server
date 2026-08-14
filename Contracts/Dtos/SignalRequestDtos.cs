namespace demo_signalR.Contracts.Dtos;

/// <summary>
/// Client gửi DTO này để chuyển tiếp SDP (dùng chung cho cả Offer và Answer).
///
/// Vì sao Offer và Answer dùng chung 1 DTO?
/// - Về mặt dữ liệu, cả 2 đều chỉ là 1 chuỗi SDP (Session Description Protocol) cần
///   gửi tới đúng 1 người. Sự khác biệt giữa "đây là Offer" hay "đây là Answer" nằm ở
///   TÊN của Hub method được gọi (SendOffer / SendAnswer), không cần thêm field phân biệt.
/// - "Sdp" ở đây là chuỗi JSON do trình duyệt (RTCPeerConnection) tự sinh ra
///   (kết quả của createOffer()/createAnswer()), server hoàn toàn không đọc/hiểu nội dung này,
///   chỉ đóng vai trò "người đưa thư" - forward nguyên vẹn cho đúng người nhận.
/// </summary>
public class SdpMessageRequest
{
    /// <summary>UserId của người sẽ nhận được đoạn SDP này.</summary>
    public required int TargetUserId { get; set; }

    /// <summary>Nội dung SDP (dạng chuỗi JSON), do trình duyệt sinh ra.</summary>
    public required string Sdp { get; set; }
}

/// <summary>
/// Client gửi DTO này mỗi khi WebRTC engine của trình duyệt tìm được một
/// ICE Candidate mới (một địa chỉ IP:port khả dĩ để kết nối P2P), cần gửi
/// ngay cho đối phương (thường xảy ra nhiều lần liên tiếp trong 1 cuộc gọi).
/// </summary>
public class IceCandidateRequest
{
    /// <summary>UserId của người sẽ nhận ICE Candidate này.</summary>
    public required int TargetUserId { get; set; }

    /// <summary>Nội dung ICE Candidate (dạng chuỗi JSON), do trình duyệt sinh ra.</summary>
    public required string Candidate { get; set; }
    /// <summary>ICE Candidate này thuộc về media section nào trong SDP?</summary>
    public required string SdpMid { get; set; }
    /// <summary>Nó là index của media line (m= section) trong SDP.</summary>
    public required int SdpMLineIndex { get; set; }
}
