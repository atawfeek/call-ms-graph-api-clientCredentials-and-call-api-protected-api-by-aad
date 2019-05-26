using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using TodoListService.Models;

namespace TodoListService.Controllers
{
    [Authorize]
    [ApiVersion("2.0")]
    [Route("api/[controller]")]
    public class TodoListController : Controller
    {
        static ConcurrentBag<TodoItem> todoStore = new ConcurrentBag<TodoItem>();

        // GET: api/values
        [HttpGet]
        public JsonResult Get()
        {
            todoStore.Add(new TodoItem()
            {
                Owner = "Me",
                Title = "Go to work"
            });

            return Json(todoStore);
        }
    }
}
