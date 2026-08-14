using demo_signalR.Contracts;
using SignalingServer.Services;
using SignalingServer.SignalR.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ====================================================================
// 1) ĐĂNG KÝ SIGNALR
// ====================================================================
// AddSignalR() đăng ký toàn bộ hạ tầng cần thiết để chạy Hub (quản lý
// kết nối WebSocket, serialize/deserialize message dạng JSON giữa các
// Hub method và client, v.v...). Không cần cấu hình thêm gì cho demo.
builder.Services.AddSignalR();

// ====================================================================
// 2) ĐĂNG KÝ DEPENDENCY INJECTION CHO CÁC SERVICE
// ====================================================================
// Dùng AddSingleton (không phải AddScoped/AddTransient) vì:
// - State (danh sách user online, các cuộc gọi đang diễn ra) PHẢI được
//   chia sẻ giữa MỌI Hub instance và MỌI request trong suốt vòng đời
//   ứng dụng - đúng bản chất "1 instance duy nhất" của Singleton.
// - Nếu dùng Scoped, mỗi lần gọi Hub method sẽ tạo 1 instance
//   ConnectionManager/CallService MỚI (rỗng), làm mất hết state ngay lập tức.
//
// Đăng ký qua INTERFACE (IConnectionManager, ICallService) chứ không phải
// class cụ thể - đây là Dependency Inversion: CallHub chỉ biết tới interface,
// không biết/không phụ thuộc implementation. Muốn thay implementation khác
// (ví dụ dùng Redis để scale nhiều instance sau này) chỉ cần sửa 2 dòng dưới đây,
// không phải sửa CallHub.
builder.Services.AddSingleton<IConnectionManager, ConnectionManager>();
builder.Services.AddSingleton<ICallService, CallService>();

// ====================================================================
// 3) CẤU HÌNH CORS
// ====================================================================
// Frontend (React + Vite) chạy trên origin khác (mặc định http://localhost:5173),
// khác với Backend (mặc định https://localhost:7000 hoặc tương tự) -> đây là
// request "cross-origin", trình duyệt sẽ chặn nếu server không khai báo CORS.
//
// LƯU Ý QUAN TRỌNG: SignalR mặc định gửi credentials (cookie/token) kèm theo
// kết nối WebSocket, nên bắt buộc phải dùng AllowCredentials(). Mà theo chuẩn
// CORS, khi đã AllowCredentials() thì KHÔNG được phép dùng AllowAnyOrigin()
// (trình duyệt sẽ từ chối) - bắt buộc phải khai báo origin cụ thể qua WithOrigins().
const string CorsPolicyName = "AllowFrontend";

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(CorsPolicyName, policy =>
//    {
//        policy
//            .WithOrigins("http://localhost:5173", "https://localhost:5173", ) // origin mặc định của Vite dev server
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

// Thứ tự middleware quan trọng: UseCors() phải được gọi TRƯỚC khi Map Hub,
// để mọi request/kết nối WebSocket tới Hub đều được áp dụng policy CORS.
app.UseCors(CorsPolicyName);

// Endpoint kiểm tra server sống hay không (health check thủ công, mở thẳng
// trên trình duyệt: http://localhost:5000/ để test nhanh không cần Postman).
app.MapGet("/", () => "Signaling Server is running.");

// ====================================================================
// 4) MAP HUB VÀO ROUTE
// ====================================================================
// Từ giờ, client React sẽ kết nối SignalR tới địa chỉ: http://localhost:<port>/callhub
app.MapHub<CallHub>("/callhub");

app.Run();
