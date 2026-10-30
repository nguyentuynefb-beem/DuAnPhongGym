using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

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
            var userId = context.HttpContext.Session.GetString("UserId");
            var role = context.HttpContext.Session.GetString("Role");

            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(role))
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            if (!_roles.Contains(role))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}
