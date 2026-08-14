namespace demo_signalR.Contracts;

/// <summary>
/// Quản lý danh sách User đang online và mapping UserId &lt;-&gt; ConnectionId.
///
/// Vì sao cần interface này?
/// - Hub không nên tự cầm ConcurrentDictionary và tự xử lý logic thêm/xóa/tìm kiếm.
///   Việc đó thuộc về tầng Service, Hub chỉ nên "hỏi" Service qua interface này.
/// - Interface hóa giúp sau này có thể thay đổi cách lưu trữ (ví dụ chuyển sang Redis
///   khi scale nhiều instance) mà không phải sửa Hub.
/// </summary>
public interface IConnectionManager
{
    /// <summary>
    /// Đăng ký một user vừa kết nối vào hệ thống.
    /// Nếu UserId đã tồn tại (ví dụ user mở tab mới / reconnect), ConnectionId cũ sẽ bị ghi đè.
    /// </summary>
    void AddConnection(int userId, string connectionId);

    /// <summary>
    /// Gỡ bỏ một connection khi client ngắt kết nối (đóng tab, mất mạng...).
    /// Được gọi từ OnDisconnectedAsync của Hub.
    /// </summary>
    /// <returns>UserId tương ứng với connectionId vừa gỡ, hoặc null nếu không tìm thấy.</returns>
    int? RemoveConnectionByConnectionId(string connectionId);

    /// <summary>Tìm ConnectionId hiện tại của một UserId. Trả về null nếu user không online.</summary>
    string? GetConnectionId(int userId);

    /// <summary>Tìm UserId tương ứng với một ConnectionId. Trả về null nếu không tìm thấy.</summary>
    int? GetUserId(string connectionId);

    /// <summary>Kiểm tra một UserId có đang online hay không.</summary>
    bool IsOnline(int userId);
}
