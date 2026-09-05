using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace API.Furnistore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        
        [HttpGet]
        public string saludo()
        {
            return "Hola, este es el test controller";
        }

        [HttpGet("welcome")]
        public string welcome(string name, int age)
        {
            return HtmlEncoder.Default.Encode($"Hola {name}, tu edad es {age}");
        }
    }
}