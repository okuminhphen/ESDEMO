# Kế hoạch: tài khoản, sản phẩm, mua hàng và thanh toán giả lập

## Trạng thái và quy tắc thực hiện

- Trạng thái: đang triển khai backend theo từng phần được yêu cầu; model, migration, bootstrap Identity, auth backend và CRUD sản phẩm cho Admin đã được triển khai.
- Chỉ bắt đầu code chức năng khi người dùng nói "proceed" hoặc yêu cầu triển khai rõ ràng.
- Giữ file này trong quá trình triển khai; cập nhật tiến độ bằng checklist.
- Chỉ xóa `plan.md` khi toàn bộ phạm vi đã hoàn thành, kiểm thử đạt và nội dung cần duy trì đã chuyển sang README/docs.
- Làm việc trên `develop`; sau mỗi phần công việc hoàn tất và kiểm tra phù hợp, tự commit bằng tiếng Anh.
- Không tự push. Người dùng tự push, trừ khi yêu cầu push rõ ràng cho lần đó.
- Không tự merge `develop` vào `main`.

## Tiến độ hiện tại: backend

- [x] Định nghĩa Product, Order, OrderItem, PaymentAttempt, Notification và enum trạng thái.
- [x] ApplicationUser/Identity, RefreshSession và OutboxMessage ở Infrastructure.
- [x] IdentityDbContext, DbSet, Fluent API, quan hệ, index, check constraint và concurrency xmin.
- [x] Chuẩn bị Identity stores và EF design-time package.
- [x] Test model và ràng buộc trên PostgreSQL tạm; không thay đổi database esdemo.
- [x] Tạo/review migration InitialSchema và apply vào database esdemo local.
- [x] Seed idempotent role Admin/Customer và một Admin cấu hình từ `.env` local.
- [x] Auth backend: DTO + validation, MediatR register/login/refresh/logout/me, JWT, role policy, lockout và refresh rotation/reuse.
- [x] Kiểm thử: 29/29 test backend đạt; thử Admin local và HTTP 413 trên Kestrel. README/docs đã cập nhật; auth không cần migration mới.
- [x] CRUD sản phẩm cho Admin: DTO + validation HTTP/MediatR, repository theo feature, phân trang/tìm kiếm, tạo/sửa/xóa mềm và kiểm tra version xmin.
- [x] Hoàn tất kiểm thử và review CRUD sản phẩm cho Admin: 6 luồng tích hợp PostgreSQL bao phủ quyền, validation, CRUD, phân trang/tìm kiếm, SKU conflict và concurrent/stale write; toàn bộ backend đạt 35/35 test.
- [ ] API danh sách/chi tiết sản phẩm công khai, đơn hàng và thanh toán.
- Frontend/BFF, API sản phẩm công khai/đơn hàng/thanh toán và Worker chưa triển khai. Xác minh email, quên mật khẩu và MFA còn chờ; đăng ký hiện chưa yêu cầu email đã xác minh. Tiếp tục giữ plan.md.
- Chi tiết model và giới hạn hiện tại: [docs/database-model.md](docs/database-model.md).
- Hợp đồng API quản lý sản phẩm và cách thử: [docs/products.md](docs/products.md).

## 1. Phạm vi và giả định

Ứng dụng bán hàng nhỏ dùng Next.js, ASP.NET Core, Clean Architecture, CQRS/MediatR, EF Core, PostgreSQL và RabbitMQ. PostgreSQL/RabbitMQ chạy qua Docker Compose hiện có.

Phạm vi đầu tiên:

- Hai role: `Admin`, `Customer`.
- Đăng ký, đăng nhập, refresh token, đăng xuất và lấy thông tin tài khoản hiện tại.
- Admin thêm, sửa, ngừng bán sản phẩm.
- Customer xem sản phẩm, mua ngay, thanh toán giả lập và xem lịch sử mua của mình.
- Transaction bảo vệ việc ghi nhận thanh toán, cập nhật đơn và tồn kho.
- Outbox/RabbitMQ xử lý thông báo mua thành công bất đồng bộ.

Giả định đề xuất, có thể điều chỉnh trước khi triển khai:

- Cho phép khách chưa đăng nhập xem sản phẩm; mua hàng yêu cầu Customer đã đăng nhập.
- Mua ngay một sản phẩm mỗi đơn, chưa có giỏ hàng. Vẫn có `OrderItems` để lưu chi tiết.
- Dùng VND; chỉ chấp nhận số tiền nguyên, không có phần thập phân.
- Có tồn kho; chưa giữ hàng khi tạo đơn, kiểm tra và trừ kho lúc thanh toán.
- Admin chưa có chức năng mua hàng hoặc xem toàn bộ đơn hàng trong phạm vi này.
- Xóa sản phẩm là soft delete/ngừng bán để bảo toàn lịch sử đơn hàng.

Phần sản phẩm đã triển khai trong backend:

- Tất cả endpoint `/api/admin/products` yêu cầu Admin; có list/detail/create/update/delete.
- DTO tạo/sửa chứa SKU, tên, mô tả, giá VND nguyên, tồn kho và trạng thái bán; giá/tồn kho/trạng thái phải được gửi rõ ràng. SKU được trim và đổi uppercase; unique bao gồm cả sản phẩm đã xóa mềm.
- DTO trả về có `version`; update/delete yêu cầu version vừa đọc, dữ liệu cũ trả 409 để client tải lại trước khi sửa.
- List phân trang mặc định 20, tối đa 100; tìm kiếm SKU/tên không phân biệt hoa thường, lọc isActive và tùy chọn includeDeleted. Mặc định không trả sản phẩm đã xóa mềm nhưng vẫn trả sản phẩm ngừng bán.
- Xóa mềm giữ dòng dữ liệu; GET chi tiết cho Admin vẫn đọc được, PUT sản phẩm đã xóa trả 404. Chưa có khôi phục hoặc xóa vĩnh viễn.
- Dùng schema hiện tại; mỗi lệnh ghi một SaveChanges nguyên tử, chưa cần RabbitMQ cho CRUD sản phẩm.

## 2. Ma trận quyền

| Chức năng | Chưa đăng nhập | Customer | Admin |
| --- | --- | --- | --- |
| Đăng ký, đăng nhập | Có | Có | Có |
| Xem sản phẩm đang bán | Có | Có | Có |
| Mua và thanh toán giả lập | Không | Có | Không |
| Xem lịch sử/chi tiết đơn của mình | Không | Có | Không |
| Thêm, sửa, ngừng bán sản phẩm | Không | Không | Có |

- Public registration luôn tạo Customer. Không nhận role từ request đăng ký.
- Admin đầu tiên được tạo qua lệnh bootstrap riêng, lấy thông tin bí mật từ cấu hình; không có mật khẩu mặc định commit vào repo.
- Backend thực thi quyền trên từng endpoint. Route guard/ẩn nút ở frontend chỉ phục vụ giao diện.
- Quyền sở hữu đơn lấy từ danh tính đã xác thực; không tin `UserId` client gửi.

## 3. Authentication và bảo mật

### Quản lý tài khoản và mật khẩu

- Dùng ASP.NET Core Identity quản lý user, role, password hash, lockout và token hỗ trợ tài khoản.
- Dùng Identity PasswordHasher với PBKDF2; salt được thư viện xử lý trong định dạng hash.
- Không tự mã hóa mật khẩu, không lưu plaintext, không băm chồng bcrypt lên PBKDF2, không tự thêm cột salt.
- Cấu hình chính sách mật khẩu và mức chi phí hashing phù hợp sau khi kiểm tra hướng dẫn hiện hành và đo trên môi trường chạy.
- Email chuẩn hóa có ràng buộc unique; xử lý trường hợp đăng ký đồng thời.
- Rate limiting đăng nhập/đăng ký/refresh, lockout tạm khi đăng nhập sai nhiều lần, thông báo lỗi đăng nhập chung.

### JWT và phiên đăng nhập

- JWT access token sống ngắn, đề xuất 10 phút, thời hạn cấu hình được.
- Kiểm tra chữ ký, thuật toán cho phép, issuer, audience và thời hạn; token chỉ chứa claim cần thiết.
- JWT ký không đồng nghĩa mã hóa nội dung; không nhúng dữ liệu bí mật vào payload.
- Refresh token ngẫu nhiên đủ mạnh; DB chỉ lưu hash, thời hạn, token family và trạng thái thu hồi.
- Rotate refresh token khi sử dụng, thực hiện nguyên tử và xử lý reuse/concurrent refresh rõ ràng.
- Logout thu hồi refresh session. JWT access token đã cấp có thể còn hiệu lực đến khi hết hạn nếu không có cơ chế kiểm tra thu hồi bổ sung.
- Làm rõ chính sách khi khóa tài khoản/đổi role: short-lived JWT có độ trễ; thao tác nhạy cảm cần kiểm tra trạng thái hiện tại nếu yêu cầu thu hồi ngay.

### Next.js và cấu hình

- Dùng BFF mỏng: browser gọi Next.js; Next.js giữ access/refresh token phía server và gọi .NET.
- Browser chỉ giữ session cookie opaque; thiết kế session store phía server, có thể dùng PostgreSQL để chưa cần thêm Redis.
- Cookie HttpOnly, Secure khi dùng HTTPS, SameSite phù hợp; có chống CSRF cho request thay đổi trạng thái.
- Không lưu JWT/refresh token trong localStorage hoặc sessionStorage.
- Không cache dữ liệu cá nhân dùng chung giữa các user; bảo vệ route và xử lý 401/403 rõ ràng.
- BFF không nhận URL upstream tùy ý từ client; chỉ gọi các endpoint backend đã định nghĩa.
- Secrets local ở `.env` đã ignore; production dùng environment/secret manager. `.env.example` chỉ có placeholder an toàn.
- Không log password, token, cookie hay request body nhạy cảm; không trả stack trace ở production.
- CORS chỉ chứa origin cho phép nếu có browser gọi trực tiếp API; CORS không thay thế authentication/authorization.

Trước khi triển khai công khai, cần xác minh email, quên/reset mật khẩu và MFA cho Admin. Các phần này phải được chốt khi triển khai, không được coi là đã có chỉ vì dùng Identity.

## 4. Luồng mua hàng và thanh toán

1. Customer chọn sản phẩm và bấm Mua.
2. Backend đọc sản phẩm đang bán, lấy giá từ DB và tạo Order ở trạng thái PendingPayment.
3. Lưu snapshot tên, giá và số lượng trong OrderItems; tổng tiền do backend tính.
4. Checkout hiển thị tổng tiền và ô nhập số tiền thanh toán giả lập.
5. Customer xác nhận; backend kiểm tra quyền sở hữu, trạng thái đơn, thời hạn, số tiền, tình trạng sản phẩm và tồn kho.
6. Chỉ chấp nhận số tiền nhập bằng tổng đơn. Từ chối số âm, số thập phân với VND, số tiền sai hoặc vượt giới hạn.
7. Trong một transaction: ghi thanh toán thành công, đổi đơn sang Paid, trừ kho và ghi OrderPaid vào Outbox.
8. Trả kết quả và cho phép xem ngay lịch sử mua từ DB.
9. Worker gửi event qua RabbitMQ; consumer tạo thông báo mua thành công.

Quy tắc:

- Frontend không được quyết định giá, tổng tiền hay trạng thái thanh toán.
- Idempotency key cho thao tác tạo đơn/thanh toán; request retry không được tạo tác động lặp. Khi key được dùng lại với payload khác phải từ chối.
- Mỗi đơn chỉ thanh toán thành công một lần, kể cả khi dùng các idempotency key khác nhau.
- Không giữ transaction mở trong lúc người dùng nhập tiền hoặc gọi mạng.
- Giao diện ghi rõ Thanh toán giả lập; không thu thập thông tin thẻ và không thể hiện đã nhận tiền thật.
- Mock provider có cấu hình bật rõ ràng và được chặn trong môi trường thanh toán thật.
- Chưa giữ kho nên đơn PendingPayment không đảm bảo còn hàng khi thanh toán; thông báo lỗi này rõ ràng.
- Chính sách đơn hết hạn và điều kiện ngừng bán sau khi tạo đơn phải được xử lý nhất quán.

## 5. Thiết kế dữ liệu

| Thực thể/bảng | Trường chính và trách nhiệm |
| --- | --- |
| Users | Identity user: Id, normalized email, PasswordHash, display name, trạng thái tài khoản, lockout/security fields |
| Roles, UserRoles | Hai role Admin/Customer và liên kết user-role theo Identity |
| RefreshSessions | UserId, TokenHash, FamilyId, CreatedAt, ExpiresAt, RevokedAt, thông tin token thay thế |
| Products | Id, Sku, Name, Description, Price, StockQuantity, IsActive, DeletedAt, CreatedAt, UpdatedAt, concurrency version |
| Orders | Id, OrderNumber, UserId, Status, TotalAmount, Currency, CreatedAt, ExpiresAt, PaidAt, concurrency version |
| OrderItems | Id, OrderId, ProductId, ProductNameSnapshot, UnitPrice, Quantity |
| PaymentAttempts | Id, OrderId, EnteredAmount, Status, Provider=Mock, IdempotencyKey, CreatedAt, CompletedAt, FailureCode |
| OutboxMessages | Id, EventType, Payload, OccurredAt, ProcessedAt, RetryCount, thông tin lỗi/retry |
| Notifications | Id, UserId, OrderId, SourceEventId, nội dung, CreatedAt, ReadAt |

Identity có thể sinh thêm bảng claim/token/external login theo cấu hình. BFF cần session store phía server; chốt bảng/session integration khi triển khai authentication, không dùng biến global trong process để lưu phiên production.

Quan hệ:

```text
User --< Order --< OrderItem >-- Product
              --< PaymentAttempt
User --< RefreshSession
User --< Notification
```

Ràng buộc:

- Email chuẩn hóa, SKU, OrderNumber có unique constraint phù hợp.
- Tiền dùng .NET decimal/PostgreSQL numeric, VND không có phần thập phân; không dùng float/double.
- Giá/tồn kho không âm, Quantity > 0; kiểm tra giới hạn số tiền và số lượng.
- Unique constraint/index bảo vệ mỗi đơn chỉ có một PaymentAttempt thành công.
- Phạm vi unique idempotency key gắn với user/thao tác; lưu đủ thông tin đối chiếu request.
- Index Orders(UserId, CreatedAt) cho lịch sử có phân trang.
- Notification.SourceEventId unique cho consumer hiện tại để chống tạo trùng.
- Không cascade delete sản phẩm làm mất OrderItems; giữ snapshot lịch sử sau khi sửa/xóa mềm sản phẩm.
- Thời gian lưu UTC, định danh và foreign key nhất quán.
- Không cần PurchaseHistory riêng: truy vấn Orders đã Paid và OrderItems.

## 6. Transaction và cạnh tranh dữ liệu

Transaction thanh toán bao gồm:

```text
PaymentAttempt thành công
+ Order -> Paid
+ Trừ tồn kho
+ OutboxMessage(OrderPaid)
```

- Một SaveChanges của EF Core vốn có transaction. Dùng explicit transaction khi nghiệp vụ có nhiều SaveChanges hoặc lệnh cập nhật riêng cần cùng nguyên tử.
- Hai người mua món cuối: dùng cập nhật tồn kho có điều kiện hoặc concurrency control; chỉ một người thành công.
- Hai request thanh toán cùng đơn: khóa/concurrency check trạng thái đơn và unique constraint bảo vệ ở DB.
- Phối hợp explicit transaction với execution strategy retry hiện có của EF Core; retry phải không gây thanh toán hoặc trừ kho lặp.
- PaymentAttempt thất bại cần chính sách ghi nhận riêng nếu transaction thành công bị rollback; không để lỗi làm đơn thành Paid.
- Stock reservation có thời hạn là phần mở rộng sau, chưa triển khai trong phạm vi đầu.

## 7. RabbitMQ và Outbox

```text
API transaction -> PostgreSQL: Order Paid + Outbox
                                      |
Worker đọc Outbox -> RabbitMQ: OrderPaid
                                      |
Consumer -> Notification
```

- MediatR dispatch command/query trong process; RabbitMQ vận chuyển integration event giữa các process.
- Không dùng RabbitMQ để quyết định thanh toán thành công hoặc làm nguồn duy nhất của lịch sử mua.
- RabbitMQ ngừng tạm: Outbox vẫn giữ event; đơn Paid và lịch sử vẫn đọc được.
- Publisher dùng confirms, xử lý unroutable message và chỉ đánh dấu sent sau xác nhận phù hợp.
- Queue/message bền vững, manual acknowledgment sau khi xử lý thành công.
- Retry có giới hạn/backoff, dead-letter queue cho lỗi kéo dài; có log/khả năng theo dõi lỗi.
- Consumer idempotent vì event có thể giao lại; dùng SourceEventId và ghi notification nguyên tử.
- Worker có thể crash sau publish trước khi đánh dấu Outbox: chấp nhận phát lại, consumer không tạo thông báo trùng.
- Chốt cơ chế claim Outbox an toàn nếu chạy nhiều worker.
- Có thể thêm ESDEMO.Worker dùng chung solution/database; chưa cần microservices.
- Email xác nhận đơn có thể thêm consumer sau khi có email provider.

## 8. Tổ chức code

```text
Application/
  Auth/Commands/          # Register, Login, RefreshToken, Logout
  Auth/Queries/           # GetCurrentUser
  Products/Commands/      # CreateProduct, UpdateProduct, DeleteProduct
  Products/Queries/       # GetProducts, GetProductById
  Orders/Commands/        # CreateOrder
  Orders/Queries/         # GetMyOrders, GetMyOrderById
  Payments/Commands/      # PayOrderMock
```

- Domain: Product, Order, OrderItem và quy tắc nghiệp vụ; không tham chiếu Identity/EF/RabbitMQ.
- Application: MediatR request/handler, validation và abstraction cần cho use case.
- Infrastructure: Identity adapter, JWT, EF mapping/migrations, persistence, Outbox và RabbitMQ implementation.
- API: controller, HTTP contracts, authentication/authorization policy, exception handling và logging.
- Worker: chạy Outbox publisher và consumer qua các implementation Infrastructure.
- Giữ controller mỏng, truyền CancellationToken; validation HTTP không thay thế validation nghiệp vụ.
- Next.js: login/register, danh sách/chi tiết sản phẩm, trang Admin, checkout, lịch sử/chi tiết đơn riêng và thông báo.

## 9. Thứ tự triển khai

- [x] 1. Chốt auth backend: JWT 10 phút, refresh 7 ngày với rotation/reuse; email verification, password recovery, MFA và BFF để bước sau. Chi tiết trong docs/authentication.md.
- [x] 2a. Backend: Identity, migrations user/role/session, bootstrap Admin, DTO/validation, register/login/refresh/logout/me và role policies.
- [ ] 2b. Frontend/BFF, giao diện auth, email verification, password recovery và MFA.
- [x] 3a. Product entity/migration, Admin CRUD/soft delete, DTO/validation, tìm kiếm/phân trang và concurrency.
- [ ] 3b. Danh sách/chi tiết sản phẩm công khai và giao diện sản phẩm/Admin.
- [ ] 4. Order/OrderItems, mua ngay, snapshot giá, checkout và lịch sử/chi tiết đơn có ownership check.
- [ ] 5. Mock payment, transaction, idempotency, concurrency và xử lý hết hàng/hết hạn.
- [ ] 6. Outbox, Worker, RabbitMQ, consumer notification, retry và dead-letter handling.
- [ ] 7. Kiểm thử toàn luồng, rà bảo mật theo phạm vi, cập nhật README/docs/.env.example/CI nếu cần.
- [ ] 8. Sau khi hoàn thành toàn bộ: chuyển thông tin lâu dài vào docs, xóa plan.md và commit; không push tự động.

## 10. Tiêu chí kiểm thử và nghiệm thu

- Đăng ký luôn ra Customer, không thể truyền role Admin; email trùng được xử lý cả khi request đồng thời.
- Password không lưu plaintext; token hết hạn/sai chữ ký/sai issuer/audience bị từ chối.
- Refresh rotation/reuse/logout hoạt động đúng; cookie/session/CSRF được kiểm tra.
- Customer gọi API quản lý sản phẩm bị từ chối; chỉ frontend guard là chưa đạt.
- User A không xem hoặc thanh toán đơn User B, kể cả tự thay ID trong URL/payload.
- Giá/tổng tiền client sửa bị bỏ qua hoặc từ chối; thanh toán sai số tiền không ghi thành công.
- Hai request thanh toán/retry không làm trừ kho hoặc ghi nhận thành công hai lần.
- Hai người mua món cuối chỉ có một người thành công; tồn kho không âm.
- Lỗi giữa transaction không để lại đơn Paid với dữ liệu thanh toán/tồn kho lệch.
- Sửa/ngừng bán sản phẩm không làm thay đổi snapshot của đơn đã mua.
- RabbitMQ gián đoạn rồi khởi động lại không mất event; nhận event trùng không tạo notification trùng.
- Lịch sử có phân trang và chỉ chứa dữ liệu của user hiện tại.
- Frontend xử lý loading/error/401/403, không cache chéo dữ liệu tài khoản.
- Backend build/test, frontend lint/build và Compose validation đạt với cấu hình mẫu phù hợp; test integration dùng DB/broker thử nghiệm, không phá dữ liệu local của người dùng.
- README/docs hướng dẫn đủ migration, bootstrap Admin, API/frontend/Worker, secrets và cách thử mock payment.

## Nguồn tham khảo

- ASP.NET Core Identity: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity-configuration?view=aspnetcore-10.0
- JWT bearer validation: https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication?view=aspnetcore-10.0
- OWASP Password Storage: https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
- OWASP Session Management: https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html
- EF Core transactions: https://learn.microsoft.com/en-us/ef/core/saving/transactions
- RabbitMQ reliability: https://www.rabbitmq.com/docs/reliability
