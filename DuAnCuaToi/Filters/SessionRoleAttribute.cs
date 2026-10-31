using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;

namespace DuAnCuaToi.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class SessionRoleAttribute : ActionFilterAttribute
    {
        private readonly HashSet<string> _roles;

        public SessionRoleAttribute(params string[] roles)
        {
            _roles = new HashSet<string>(roles, StringComparer.OrdinalIgnoreCase);
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var endpoint = context.HttpContext.GetEndpoint();
            var actionRoleAttr = endpoint?.Metadata.GetMetadata<SessionRoleAttribute>();
            if (actionRoleAttr != null && !ReferenceEquals(actionRoleAttr, this))
            {
                return;
            }

            var session = context.HttpContext.Session;
            var userId = session.GetString("UserId");
            var role = session.GetString("Role");

            // 1. Tự động phục hồi Session từ Claims của Cookie nếu Session bộ nhớ bị tái chế
            if ((string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role)) &&
                context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var claimId = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var claimRole = context.HttpContext.User.FindFirst(ClaimTypes.Role)?.Value;
                var claimName = context.HttpContext.User.FindFirst(ClaimTypes.Name)?.Value;
                var claimFullName = context.HttpContext.User.FindFirst("FullName")?.Value;

                if (!string.IsNullOrWhiteSpace(claimId) && !string.IsNullOrWhiteSpace(claimRole))
                {
                    session.SetString("UserId", claimId);
                    session.SetString("Role", claimRole);
                    if (!string.IsNullOrWhiteSpace(claimName)) session.SetString("Username", claimName);
                    if (!string.IsNullOrWhiteSpace(claimFullName)) session.SetString("UserName", claimFullName);

                    userId = claimId;
                    role = claimRole;
                }
            }

            // 2. Nếu vẫn chưa có thông tin đăng nhập hợp lệ -> Về trang Login
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            // Chuẩn hóa role Admin / AdminGym
            var normalizedRole = role.Equals("AdminGym", StringComparison.OrdinalIgnoreCase) ? "Admin" : role;

            // 3. Kiểm tra quyền truy cập theo vai trò
            if (!_roles.Contains(role) && !_roles.Contains(normalizedRole))
            {
                // Điều hướng về đúng Dashboard tương ứng với vai trò của người dùng kèm thông báo
                var targetController = normalizedRole switch
                {
                    "Admin" => "Admin",
                    "PT" => "PT",
                    "LeTan" => "LeTan",
                    "HoiVien" => "User",
                    _ => "Home"
                };

                if (context.Controller is Controller mvcController)
                {
                    mvcController.TempData["Error"] = "Bạn không có quyền truy cập vào phân hệ này!";
                }

                context.Result = new RedirectToActionResult("Index", targetController, null);
                return;
            }
        }
    }
}
