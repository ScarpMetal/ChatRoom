using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace CSCI251Project3.Controllers
{
    public class RoomController : Controller
    {
        // GET: /Message
        public IActionResult Index()
        {
            Console.WriteLine("Did not use Mapping");
            return View();
        }
        // GET: /Message/Room
        public IActionResult Room()
        {
            
            return View();
        }
    }
}
