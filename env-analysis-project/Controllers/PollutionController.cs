using Microsoft.AspNetCore.Mvc;
using env_analysis_project.Services;

namespace env_analysis_project.Controllers
{
    public class PollutionController : Controller
    {
        private readonly IPredictionService _service;

        public PollutionController(IPredictionService service) => _service = service;

        [HttpGet]
        public IActionResult Index() => View();

        [HttpPost]
        public IActionResult UploadCsv(IFormFile csvFile)
        {
            if (csvFile == null || csvFile.Length == 0)
            {
                ModelState.AddModelError("csvFile", "Vui lòng chọn một file CSV.");
                return View("Index");
            }

    
            var tempPath = Path.GetTempFileName();
            try
            {
                  
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    csvFile.CopyTo(stream);
                }

            
                var result = _service.UploadAndPredict(tempPath);

                    
                return View("Result", result);
            }
            catch (Exception ex)
            {
            
                ModelState.AddModelError("", $"Lỗi xử lý file hoặc dự đoán: {ex.Message}");
                return View("Index");
            }
            finally
            {
                System.IO.File.Delete(tempPath);
            }
        }
    }
}
