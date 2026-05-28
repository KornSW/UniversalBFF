using Logging.SmartStandards.EventKindManagement;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel;

namespace TemplateNamespace {

  [Controller()]
  public class DemoController : Controller {

    [HttpGet("/")]
    public ActionResult Index() {

      return Content(
        //HELLO-WORLD-PAGE
        "<!doctype html><html lang=\"en\"><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>Hello World</title><body style=\"margin:0;min-height:100vh;display:grid;place-items:center;background:#f4f6fb;font-family:Arial,sans-serif\"><main style=\"padding:3rem 4rem;border-radius:24px;background:white;box-shadow:0 20px 60px #0002;text-align:center\"><h1 style=\"margin:0;color:#222;font-size:3rem\">Hello World</h1><p style=\"margin:.75rem 0 0;color:#667\">This is the SmartStandards Asp-Template...<br><br><a style=\"color:#667\" href=\"/MyDemoService\">MyDemoService (UJMW)</a><br><br><a style=\"color:#667\" href=\"/docs\">OpenAPI/Swagger</a></p></main></body></html>",
        "text/html"
      );

    }

  }

}
