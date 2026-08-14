using System.Collections.Concurrent;
using demo_signalR.Contracts;

namespace SignalingServer.Services;

/// <summary>
/// Triển khai IConnectionManager, lưu trạng thái user online trong bộ nhớ (in-memory).
///
/// Vì sao KHÔNG dùng static class để lưu state?
/// - Static state là "global mutable state" - rất khó test (không thể mock/reset giữa các
///   test case), khó kiểm soát vòng đời, và vi phạm nguyên tắc Dependency Injection
///   (ẩn dependency thay vì khai báo tường minh qua constructor).
/// - Thay vào đó, class này được đăng ký làm Singleton trong DI container (ở Program.cs,
///   Bước 7). Về hiệu quả runtime thì tương đương static (chỉ có 1 instance sống suốt
///   vòng đời ứng dụng), nhưng vẫn giữ được khả năng inject interface, dễ thay thế/mock
///   khi viết unit test.
///
/// Vì sao dùng ConcurrentDictionary thay vì Dictionary thường?
/// - SignalR Hub xử lý nhiều connection đồng thời trên nhiều thread khác nhau.
///   Nhiều client có thể connect/disconnect/gọi nhau cùng lúc, nếu dùng Dictionary
///   thường sẽ có nguy cơ race condition / exception khi ghi đồng thời.
///   ConcurrentDictionary được thiết kế an toàn cho việc đọc/ghi đa luồng.
///
/// Vì sao cần 2 dictionary (2 chiều) thay vì 1?
/// - Ta cần tra cứu theo cả 2 hướng với độ phức tạp O(1):
///     userId          -> connectionId  (khi A muốn gọi B, cần tìm ConnectionId của B)
///     connectionId     -> userId        (khi 1 connection bị disconnect, cần biết đó là UserId nào)
///   Nếu chỉ giữ 1 chiều, chiều còn lại sẽ phải duyệt toàn bộ dictionary (O(n)) - chấp nhận
///   được với demo nhỏ nhưng không phải thói quen tốt, và OnDisconnectedAsync bị gọi khá
///   thường xuyên nên nên có lookup O(1).
/// </summary>
public class ConnectionManager : IConnectionManager
{
    private readonly ConcurrentDictionary<int, string> _userIdToConnectionId = new();
    private readonly ConcurrentDictionary<string, int> _connectionIdToUserId = new();

    public void AddConnection(int userId, string connectionId)
    {
        // Trường hợp user cũ đã có 1 connection khác (ví dụ mở lại tab, mất mạng rồi
        // reconnect...): phải dọn mapping cũ trong _connectionIdToUserId trước,
        // nếu không dictionary này sẽ tồn đọng 1 entry "rác" trỏ tới cùng userId,
        // gây rò rỉ bộ nhớ nhẹ và có thể gây nhầm lẫn khi tra cứu ngược.
        if (_userIdToConnectionId.TryGetValue(userId, out var oldConnectionId) &&
            oldConnectionId != connectionId)
        {
            _connectionIdToUserId.TryRemove(oldConnectionId, out _);
        }

        // Ghi đè (nếu đã tồn tại) hoặc thêm mới (nếu chưa tồn tại) - dùng indexer
        // thay vì TryAdd vì ta CHỦ ĐỘNG muốn ghi đè trong trường hợp reconnect.
        _userIdToConnectionId[userId] = connectionId;
        _connectionIdToUserId[connectionId] = userId;
    }

    public int? RemoveConnectionByConnectionId(string connectionId)
    {
        if (!_connectionIdToUserId.TryRemove(connectionId, out var userId))
        {
            // Không tìm thấy connectionId này trong hệ thống (có thể đã bị xóa trước đó).
            return null;
        }

        // Chỉ xóa mapping userId -> connectionId nếu nó vẫn đang trỏ đúng connectionId
        // vừa bị remove. Lý do: nếu user đã reconnect bằng connectionId MỚI trước khi
        // sự kiện disconnect của connectionId CŨ được xử lý (race condition hiếm gặp
        // nhưng có thể xảy ra), ta không được xóa nhầm mapping mới.
        if (_userIdToConnectionId.TryGetValue(userId, out var currentConnectionId) &&
            currentConnectionId == connectionId)
        {
            _userIdToConnectionId.TryRemove(userId, out _);
        }

        return userId;
    }

    public string? GetConnectionId(int userId)
    {
        return _userIdToConnectionId.TryGetValue(userId, out var connectionId) ? connectionId : null;
    }

    public int? GetUserId(string connectionId)
    {
        return _connectionIdToUserId.TryGetValue(connectionId, out var userId) ? userId : null;
    }

    public bool IsOnline(int userId)
    {
        return _userIdToConnectionId.ContainsKey(userId);
    }
}
