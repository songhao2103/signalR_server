namespace SignalingServer.Models;

/// <summary>
/// Đại diện cho một người dùng đang kết nối tới Signaling Server.
///
/// LƯU Ý QUAN TRỌNG:
/// - Đây KHÔNG phải là "tài khoản" (không có mật khẩu, không login).
/// - "UserId" ở demo này chỉ là một tên/định danh do client tự đặt khi kết nối
///   (ví dụ người dùng gõ "Alice" vào ô tên trước khi vào phòng).
/// - "ConnectionId" là ID kết nối SignalR, do SignalR tự sinh ra, thay đổi
///   mỗi lần client connect/reconnect. Đây là thứ server dùng để "gửi tin nhắn
///   tới đúng người" (Clients.Client(connectionId).SendAsync(...)).
///
/// Vì sao cần tách UserId và ConnectionId thành 2 field riêng?
/// - ConnectionId sống theo vòng đời kết nối WebSocket (mất khi refresh trang, mất mạng...).
/// - UserId là thứ có ý nghĩa nghiệp vụ ổn định hơn, dùng để người dùng khác "gọi đích danh".
/// - Nhờ tách riêng, khi ConnectionId đổi (reconnect), ta chỉ cần update lại mapping
///   mà không ảnh hưởng tới cách người khác gọi tới UserId đó.
/// </summary>
public class OnlineUser
{
    /// <summary>Định danh do client cung cấp lúc kết nối (ví dụ: tên hiển thị / username tự đặt).</summary>
    public required string UserId { get; set; }

    /// <summary>ConnectionId hiện tại của user này trên SignalR, dùng để định tuyến message.</summary>
    public required string ConnectionId { get; set; }
}
